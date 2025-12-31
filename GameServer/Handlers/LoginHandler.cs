using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GameCommon;
using GameServerApp.Database;
using GameServerApp.Models;
using GameServerApp.Systems;
using SharedProtocol;

namespace GameServerApp.Handlers
{
    /// <summary>
    /// Handles client login requests and authentication
    /// </summary>
    public class LoginHandler : MessageHandler<LoginRequest>
    {
        private readonly DatabaseHelper _database;
        private readonly SessionManager _sessions;
        private readonly Rooms.RoomManager _rooms;
        private readonly InventorySystem _inventorySystem;
        private readonly EntitySyncService _entitySync;
        
        // Supported client versions
        private readonly HashSet<string> _supportedVersions = new() { "1.0.0", "1.0.1" };

        public LoginHandler(
            DatabaseHelper database, 
            SessionManager sessions, 
            Rooms.RoomManager rooms, 
            InventorySystem inventorySystem, 
            EntitySyncService entitySync) : base(MessageType.LoginRequest)
        {
            _database = database;
            _sessions = sessions;
            _rooms = rooms;
            _inventorySystem = inventorySystem;
            _entitySync = entitySync;
        }

        protected override async Task HandleAsync(Session session, LoginRequest message)
        {
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(message.Username) || string.IsNullOrWhiteSpace(message.Password))
                {
                    await SendLoginFailure(session, "Please enter username and password.");
                    return;
                }

                // Client version check
                if (!string.IsNullOrEmpty(message.ClientVersion) && !_supportedVersions.Contains(message.ClientVersion))
                {
                    await SendLoginFailure(session, $"Unsupported client version: {message.ClientVersion}");
                    return;
                }

                // Check if user is already logged in
                if (_sessions.GetSession(message.Username) != null)
                {
                    await SendLoginFailure(session, "User is already logged in.");
                    return;
                }

                // User authentication (creates new user if not exists)
                if (!await AuthenticateUser(message.Username, message.Password))
                {
                    await SendLoginFailure(session, "Invalid username or password.");
                    return;
                }

                // Session token generation
                var sessionToken = GenerateSessionToken();
                session.SessionToken = sessionToken;
                session.UserName = message.Username;
                
                // Player data initialization
                var character = await GetOrCreateCharacter(message.Username);
                var playerInventory = await _inventorySystem.GetPlayerInventoryAsync(message.Username);
                var inventorySummary = BuildInventorySummary(playerInventory);
                var inventorySnapshot = _inventorySystem.CreateSlotSnapshot(playerInventory);
                
                // Player info creation
                var playerInfo = new PlayerInfo
                {
                    PlayerId = character.Name, // Using name as ID for now
                    Username = character.Name,
                    Position = new SharedProtocol.Vector3((float)character.X, (float)character.Y, 0),
                    Level = 1,
                    Health = 100,
                    MaxHealth = 100,
                    Inventory = inventorySummary
                };
                
                session.PlayerInfo = playerInfo;
                
                // Session registration
                _sessions.Add(session);

                if (!_rooms.AssignPlayerToRoom(session.UserName!, Rooms.RoomManager.DefaultLobbyId))
                {
                    _sessions.Remove(session);
                    await SendLoginFailure(session, "Failed to join lobby. Please try again.");
                    return;
                }

                var lobby = _rooms.GetRoom(Rooms.RoomManager.DefaultLobbyId);
                if (lobby != null)
                {
                    _sessions.UpdatePlayerWorld(session.UserName!, lobby.WorldId, 0, 0);
                }
                
                // Login success response
                var response = new LoginResponse 
                { 
                    Success = true, 
                    Message = $"Welcome {message.Username}!",
                    SessionToken = sessionToken,
                    PlayerInfo = playerInfo
                };
                
                await session.SendAsync(MessageType.LoginResponse, response);

                var inventoryBroadcast = new InventoryUpdateBroadcast
                {
                    PlayerId = session.UserName!,
                    UpdatedSlots = inventorySnapshot,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await session.SendAsync(MessageType.InventoryUpdateBroadcast, inventoryBroadcast);

                try
                {
                    await _entitySync.SendSpawnSnapshotsAsync(session);
                }
                catch (Exception syncEx)
                {
                    Console.WriteLine($"[EntitySync] Failed to publish spawn snapshots for {session.UserName}: {syncEx.Message}");
                }

                Console.WriteLine($"User '{message.Username}' logged in successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error for user '{message.Username}': {ex.Message}");
                await SendLoginFailure(session, "Login failed due to server error.");
            }
        }

        /// <summary>
        /// Sends login failure response to client
        /// </summary>
        private async Task SendLoginFailure(Session session, string errorMessage)
        {
            var response = new LoginResponse { Success = false, Message = errorMessage };
            await session.SendAsync(MessageType.LoginResponse, response);
            Console.WriteLine($"Login failed: {errorMessage}");
        }

        private static List<InventoryItem> BuildInventorySummary(PlayerInventory inventory)
        {
            if (inventory == null)
            {
                return new List<InventoryItem>();
            }

            return inventory.Slots
                .Where(slot => !slot.IsEmpty())
                .GroupBy(slot => slot.ItemId)
                .Select(group => new InventoryItem
                {
                    ItemId = int.TryParse(group.Key, out var parsedId) ? parsedId : 0,
                    ItemName = group.Key,
                    Quantity = group.Sum(slot => slot.Amount)
                })
                .ToList();
        }

        /// <summary>
        /// Authenticates user credentials using salted hash
        /// </summary>
        private async Task<bool> AuthenticateUser(string username, string password)
        {
            try
            {
                var character = await _database.GetPlayerByNameAsync(username);
                
                if (character == null)
                {
                    var hashedPassword = HashPassword(password, GenerateSalt());
                    var salt = GenerateSalt();
                    
                    var newCharacter = new Character(username, 0, 100, 0)
                    {
                        PasswordHash = HashPassword(password, salt),
                        Salt = salt
                    };
                    
                    await _database.SavePlayerAsync(newCharacter);
                    return true;
                }
                
                var computedHash = HashPassword(password, character.Salt);
                return computedHash == character.PasswordHash;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Authentication error for {username}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets or creates character data
        /// </summary>
        private async Task<Character> GetOrCreateCharacter(string username)
        {
            var character = await _database.GetPlayerByNameAsync(username);
            
            if (character != null)
            {
                character.UpdateLastLogin();
                await _database.SavePlayerAsync(character);
                return character;
            }
            
            var newCharacter = new Character(username, 0, 100, 0);
            await _database.SavePlayerAsync(newCharacter);
            return newCharacter;
        }
        
        /// <summary>
        /// Computes password hash
        /// </summary>
        private string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var saltedPassword = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha256.ComputeHash(saltedPassword);
            return Convert.ToBase64String(hash);
        }
        
        /// <summary>
        /// Generates random salt
        /// </summary>
        private string GenerateSalt()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[16];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Generates secure session token
        /// </summary>
        private string GenerateSessionToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}
