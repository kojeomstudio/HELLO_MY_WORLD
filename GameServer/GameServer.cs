using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using GameServerApp.Database;
using GameServerApp.Handlers;
using GameServerApp.World;
using SharedProtocol;
using System.Collections.Concurrent;

namespace GameServerApp
{
    public class GameServer
    {
        private readonly TcpListener _listener;
        private readonly DatabaseHelper _database;
        private readonly MessageDispatcher _dispatcher;
        private readonly MinecraftMessageDispatcher _minecraftDispatcher;
        private readonly SessionManager _sessions;
        private readonly Rooms.RoomManager _rooms;
        private readonly WorldManager _worldManager;
        private readonly Timer _maintenanceTimer;
        private readonly ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _rateCounters = new();
        private readonly ServerConfig _config;
        private bool _isRunning;

        public GameServer(int port = 9000, string databaseFile = "minecraft_game.db")
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _database = new DatabaseHelper(databaseFile);
            _dispatcher = new MessageDispatcher();
            _sessions = new SessionManager();
            _rooms = new Rooms.RoomManager(_sessions);
            _worldManager = new WorldManager(_database);
            _minecraftDispatcher = new MinecraftMessageDispatcher(_dispatcher);
            _config = ServerConfig.LoadFromFile();
            
            RegisterMessageHandlers();
            
            _maintenanceTimer = new Timer(PerformMaintenance, null, 
                TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }
        
        private void RegisterMessageHandlers()
        {
            // Authentication & Session Management
            _dispatcher.Register(new LoginHandler(_database, _sessions, _rooms));
            
            // Player Movement & Positioning (Enhanced Minecraft-style)
            //_dispatcher.Register(new PlayerMoveHandler(_database, _sessions, _worldManager));
            _dispatcher.Register(new MovementHandler(_database, _sessions));
            
            // World & Block Management (Server-Synchronized)
            //_dispatcher.Register(new ChunkHandler(_database, _sessions, _worldManager));
            _dispatcher.Register(new WorldBlockHandler(_database, _sessions, _worldManager, _rooms));
            
            // Game Mechanics & Interactions
            var inventorySystem = new InventorySystem(_database);
            var craftingSystem = new CraftingSystem(inventorySystem);
            var healthSystem = new GameServerApp.Systems.HealthAndHungerSystem(_database, _sessions);
            
            _dispatcher.Register(new InventoryHandler(_database, _sessions));
            _dispatcher.Register(new CraftingHandler(_database, _sessions, craftingSystem));
            _dispatcher.Register(new RecipeListHandler(_database, _sessions, craftingSystem));
            _dispatcher.Register(new HealthHandler(_database, _sessions, healthSystem));
            _dispatcher.Register(new RespawnHandler(_database, _sessions, healthSystem));
            
            // Communication & Network
            _dispatcher.Register(new ChatHandler(_database, _sessions, _rooms));
            _dispatcher.Register(new PingHandler(_database, _sessions));
            
            // === 마인크래프트 전용 핸들러 등록 ===
            RegisterMinecraftHandlers();
            
            Console.WriteLine($"Registered {_dispatcher.HandlerCount} base handlers + {_minecraftDispatcher.HandlerCount} minecraft handlers");
        }

        /// <summary>
        /// 마인크래프트 전용 핸들러들을 등록합니다.
        /// </summary>
        private void RegisterMinecraftHandlers()
        {
            // 마인크래프트 전용 메시지 핸들러들을 기본 디스패처에 등록
            _dispatcher.Register(new MinecraftPlayerActionHandler(_database, _sessions, _worldManager, _minecraftDispatcher));
            _dispatcher.Register(new MinecraftChunkHandler(_database, _sessions, _worldManager));
            
            Console.WriteLine("=== Minecraft Enhanced Features Enabled ===");
            Console.WriteLine("✓ Advanced Block Breaking System");
            Console.WriteLine("✓ Procedural Chunk Generation");
            Console.WriteLine("✓ Real-time Block Synchronization");
            Console.WriteLine("✓ Entity Management System");
            Console.WriteLine("✓ Biome-based World Generation");
            Console.WriteLine("✓ Item Drop & Pickup System");
            Console.WriteLine("===========================================");
        }

        public async Task StartAsync()
        {
            _isRunning = true;
            _listener.Start();
            var port = ((_listener.LocalEndpoint as IPEndPoint)?.Port) ?? 0;
            
            Console.WriteLine($"=== Minecraft Game Server Started ===");
            Console.WriteLine($"Port: {port}");
            Console.WriteLine($"Database: Initialized with enhanced schema");
            Console.WriteLine($"World Manager: Ready for chunk generation");
            Console.WriteLine($"Session Management: Enhanced with player state tracking");
            Console.WriteLine($"======================================");

            try
            {
                while (_isRunning)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Server listener disposed.");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            _maintenanceTimer?.Dispose();
            _sessions?.Dispose();
            Console.WriteLine("Server stopped.");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
            Console.WriteLine($"Client connected from {clientEndpoint}");
            
            var session = new Session(client);

            try
            {
                while (_isRunning && client.Connected)
                {
                    var (type, message) = await session.ReceiveAsync();

                    // 간단한 세션별 메시지 레이트 리미팅 (초당 최대 N개)
                    if (_config.Security.EnableRateLimiting && !string.IsNullOrEmpty(session.UserName))
                    {
                        if (IsRateLimited(session.UserName!, _config.Security.MaxMessagesPerSecond))
                        {
                            Console.WriteLine($"Rate limit exceeded by {session.UserName}. Dropping message {type}.");
                            continue; // 메시지를 드롭하고 다음 루프로 진행
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(session.UserName))
                    {
                        _sessions.UpdateHeartbeat(session.UserName);
                    }
                    
                    await _dispatcher.DispatchAsync(session, type, message);
                }
            }
            catch (Exception ex)
            {
                if (_isRunning)
                {
                    Console.WriteLine($"Client {session.UserName ?? clientEndpoint} disconnected: {ex.Message}");
                }
            }
            finally
            {
                // 룸에서 제거
                if (!string.IsNullOrEmpty(session.UserName))
                {
                    _rooms.RemovePlayer(session.UserName);
                }
                if (!string.IsNullOrEmpty(session.UserName))
                {
                    await SavePlayerDataOnDisconnect(session);
                }
                
                _sessions.Remove(session);
                
                try
                {
                    client.Close();
                }
                catch { }
                
                Console.WriteLine($"Cleaned up session for {session.UserName ?? clientEndpoint}");
            }
        }

        private bool IsRateLimited(string userName, int maxPerSecond)
        {
            var now = DateTime.UtcNow;
            var key = userName;
            var window = now.AddSeconds(-1);

            _rateCounters.AddOrUpdate(key,
                addValueFactory: _ => (1, now),
                updateValueFactory: (_, entry) =>
                {
                    // 같은 1초 윈도우 안이면 카운트 증가, 아니면 새 윈도우 시작
                    if (entry.WindowStart > window)
                    {
                        var newCount = entry.Count + 1;
                        return (newCount, entry.WindowStart);
                    }
                    return (1, now);
                });

            if (_rateCounters.TryGetValue(key, out var updated))
            {
                return updated.Count > maxPerSecond;
            }
            return false;
        }

        private async Task SavePlayerDataOnDisconnect(Session session)
        {
            try
            {
                var playerState = _sessions.GetPlayerState(session.UserName!);
                if (playerState != null && session.PlayerInfo != null)
                {
                    var character = new Models.Character(session.UserName!, 
                        playerState.Position.X, playerState.Position.Y, playerState.Position.Z)
                    {
                        Health = playerState.Health,
                        Level = playerState.Level,
                        LastLoginAt = DateTime.UtcNow
                    };
                    
                    await _database.SavePlayerAsync(character);
                    Console.WriteLine($"Saved player data for {session.UserName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving player data for {session.UserName}: {ex.Message}");
            }
        }

        private async void PerformMaintenance(object? state)
        {
            try
            {
                Console.WriteLine("Performing server maintenance...");
                
                await _worldManager.SaveModifiedChunksAsync();
                
                _worldManager.UnloadOldChunks(TimeSpan.FromMinutes(30));
                
                var onlinePlayers = _sessions.OnlinePlayerCount;
                Console.WriteLine($"Maintenance complete. Online players: {onlinePlayers}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during maintenance: {ex.Message}");
            }
        }
    }
}
