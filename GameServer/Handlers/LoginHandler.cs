using System;
using GameServerApp.Models;
using GameServerApp.Database;
using GameServerApp.Systems;
using SharedProtocol;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GameServerApp.Handlers;

/// <summary>
/// 濡쒓렇???붿껌??泥섎━?섎뒗 ?몃뱾??
/// ?ъ슜???몄쬆, ?몄뀡 ?앹꽦, ?뚮젅?댁뼱 ?곗씠??濡쒕뱶瑜??대떦?⑸땲??
/// </summary>
public class LoginHandler : MessageHandler<LoginRequest>
{
    private readonly DatabaseHelper _database;
    private readonly SessionManager _sessions;
    private readonly Rooms.RoomManager _rooms;
    private readonly InventorySystem _inventorySystem;
    private readonly EntitySyncService _entitySync;
    
    // 吏?먮릺???대씪?댁뼵??踰꾩쟾 紐⑸줉
    private readonly HashSet<string> _supportedVersions = new() { "1.0.0", "1.0.1" };

    public LoginHandler(DatabaseHelper database, SessionManager sessions, Rooms.RoomManager rooms, InventorySystem inventorySystem, EntitySyncService entitySync) : base(MessageType.LoginRequest)
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
            // ?낅젰 寃利?
            if (string.IsNullOrWhiteSpace(message.Username) || string.IsNullOrWhiteSpace(message.Password))
            {
                await SendLoginFailure(session, "?ъ슜?먮챸怨?鍮꾨?踰덊샇瑜??낅젰?댁＜?몄슂.");
                return;
            }

            // ?대씪?댁뼵??踰꾩쟾 ?뺤씤
            if (!string.IsNullOrEmpty(message.ClientVersion) && !_supportedVersions.Contains(message.ClientVersion))
            {
                await SendLoginFailure(session, $"吏?먰븯吏 ?딅뒗 ?대씪?댁뼵??踰꾩쟾?낅땲?? {message.ClientVersion}");
                return;
            }

            // ?대? 濡쒓렇?몃맂 ?ъ슜?먯씤吏 ?뺤씤
            if (_sessions.GetSession(message.Username) != null)
            {
                await SendLoginFailure(session, "?대? 濡쒓렇?몃맂 ?ъ슜?먯엯?덈떎.");
                return;
            }

            // ?ъ슜???몄쬆 (?ㅼ젣 ?섍꼍?먯꽌???댁떆??鍮꾨?踰덊샇? 鍮꾧탳?댁빞 ??
            if (!await AuthenticateUser(message.Username, message.Password))
            {
                await SendLoginFailure(session, "?섎せ???ъ슜?먮챸 ?먮뒗 鍮꾨?踰덊샇?낅땲??");
                return;
            }

            // ?몄뀡 ?좏겙 ?앹꽦
            var sessionToken = GenerateSessionToken();
            session.SessionToken = sessionToken;
            session.UserName = message.Username;
            
            // ?뚮젅?댁뼱 ?곗씠??濡쒕뱶 ?먮뒗 ?앹꽦
            var character = await GetOrCreateCharacter(message.Username);
            var playerInventory = await _inventorySystem.GetPlayerInventoryAsync(message.Username);
            var inventorySummary = BuildInventorySummary(playerInventory);
            var inventorySnapshot = _inventorySystem.CreateSlotSnapshot(playerInventory);
            
            // ?뚮젅?댁뼱 ?뺣낫 ?앹꽦
            var playerInfo = new PlayerInfo
            {
                PlayerId = character.Name, // ?ㅼ젣濡쒕뒗 GUID ?깆쓣 ?ъ슜
                Username = character.Name,
                Position = new SharedProtocol.Vector3((float)character.X, (float)character.Y, 0),
                Level = 1,
                Health = 100,
                MaxHealth = 100,
                Inventory = inventorySummary
            };
            
            session.PlayerInfo = playerInfo;
            
            // ?몄뀡 ?깅줉
            _sessions.Add(session);

            if (!_rooms.AssignPlayerToRoom(session.UserName!, Rooms.RoomManager.DefaultLobbyId))
            {
                _sessions.Remove(session);
                await SendLoginFailure(session, "濡쒕퉬???낆옣?????놁뒿?덈떎. ?좎떆 ???ㅼ떆 ?쒕룄?댁＜?몄슂.");
                return;
            }

            var lobby = _rooms.GetRoom(Rooms.RoomManager.DefaultLobbyId);
            if (lobby != null)
            {
                _sessions.UpdatePlayerWorld(session.UserName!, lobby.WorldId, 0, 0);
            }
            
            // 濡쒓렇???깃났 ?묐떟
            var response = new LoginResponse 
            { 
                Success = true, 
                Message = $"?섏쁺?⑸땲?? {message.Username}??",
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
            await SendLoginFailure(session, "濡쒓렇??泥섎━ 以??ㅻ쪟媛 諛쒖깮?덉뒿?덈떎.");
        }
    }

    /// <summary>
    /// 濡쒓렇???ㅽ뙣 ?묐떟??蹂대깄?덈떎.
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
    /// ?ъ슜?먮? ?몄쬆?⑸땲?? ?댁떆??鍮꾨?踰덊샇? ?뷀듃瑜??ъ슜??蹂댁븞 ?몄쬆.
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
    /// 罹먮┃???뺣낫瑜?媛?몄삤嫄곕굹 ?덈줈 ?앹꽦?⑸땲??
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
    /// 鍮꾨?踰덊샇瑜??댁떆?뷀빀?덈떎.
    /// </summary>
    private string HashPassword(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = Encoding.UTF8.GetBytes(password + salt);
        var hash = sha256.ComputeHash(saltedPassword);
        return Convert.ToBase64String(hash);
    }
    
    /// <summary>
    /// ?쒕뜡 ?뷀듃瑜??앹꽦?⑸땲??
    /// </summary>
    private string GenerateSalt()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[16];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 蹂댁븞 ?몄뀡 ?좏겙???앹꽦?⑸땲??
    /// </summary>
    private string GenerateSessionToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}

