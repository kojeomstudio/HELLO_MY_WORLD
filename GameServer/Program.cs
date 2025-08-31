using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameServerApp
{
    /// <summary>
    /// Enhanced Minecraft-style game server with complete client-server architecture.
    /// Replaces P2P networking with centralized server authority and protobuf communication.
    /// </summary>
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Display server architecture information
            DisplayServerInfo();
            
            // Check if we should run in server-only mode
            if (args.Length > 0 && args[0] == "--server")
            {
                return await ServerLauncher.Main(args);
            }
            
            Console.WriteLine("\nChoose an option:");
            Console.WriteLine("1. Start Enhanced Minecraft Server");
            Console.WriteLine("2. Run Test Client");
            Console.WriteLine("3. Server Configuration");
            Console.WriteLine("4. Exit");
            
            while (true)
            {
                Console.Write("\nEnter your choice (1-4): ");
                var choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        await RunEnhancedServerAsync();
                        break;
                        
                    case "2":
                        await TestClient.RunTestSuiteAsync();
                        break;
                        
                    case "3":
                        DisplayConfigurationMenu();
                        break;
                        
                    case "4":
                        Console.WriteLine("Goodbye!");
                        return 0;
                        
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, 3, or 4.");
                        continue;
                }
                
                Console.WriteLine("\nPress any key to return to menu...");
                Console.ReadKey();
                Console.Clear();
                DisplayServerInfo();
                Console.WriteLine("\nChoose an option:");
                Console.WriteLine("1. Start Enhanced Minecraft Server");
                Console.WriteLine("2. Run Test Client");
                Console.WriteLine("3. Server Configuration");
                Console.WriteLine("4. Exit");
            }
        }
        
        private static void DisplayServerInfo()
        {
            Console.WriteLine("===== Minecraft-Like Game Server Architecture =====");
            Console.WriteLine("• Client-Server Architecture (P2P removed)");
            Console.WriteLine("• Google Protocol Buffers for communication");
            Console.WriteLine("• Enhanced SQLite database with full game state");
            Console.WriteLine("• Real-time chunk generation and synchronization");
            Console.WriteLine("• Session management with player persistence");
            Console.WriteLine("• Anti-cheat and server-side validation");
            Console.WriteLine("=============================================");
        }
        
        /// <summary>
        /// Runs the enhanced Minecraft-style server with full client-server architecture.
        /// </summary>
        private static async Task RunEnhancedServerAsync()
        {
            try
            {
                Console.WriteLine("\n=== Starting Enhanced Minecraft Server ===");
                
                var config = ServerConfig.LoadFromFile();
                var server = new GameServer(config.Network.Port, config.Database.DatabaseFile);
                
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                    Console.WriteLine("\n=== Shutdown Signal Received ===");
                    server.Stop();
                };
                
                var serverTask = server.StartAsync();
                
                Console.WriteLine("\n=== Server Commands ===");
                Console.WriteLine("Type 'help' for available commands");
                Console.WriteLine("Type 'stop' or press Ctrl+C to shutdown");
                Console.WriteLine("========================");
                
                // Server command loop
                _ = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var input = Console.ReadLine();
                            if (string.IsNullOrEmpty(input)) continue;
                            
                            await ProcessServerCommand(input.Trim().ToLower(), server);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Command error: {ex.Message}");
                        }
                    }
                });
                
                try
                {
                    await Task.Delay(-1, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected cancellation
                }
                
                Console.WriteLine("Server shutting down gracefully...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private static async Task ProcessServerCommand(string command, GameServer server)
        {
            switch (command)
            {
                case "help":
                    Console.WriteLine("\nAvailable Commands:");
                    Console.WriteLine("  help     - Show this help message");
                    Console.WriteLine("  stop     - Stop the server");
                    Console.WriteLine("  status   - Show server status");
                    Console.WriteLine("  players  - List online players");
                    Console.WriteLine("  config   - Show current configuration");
                    break;
                    
                case "stop":
                    server.Stop();
                    break;
                    
                case "status":
                    Console.WriteLine($"Server Status: Running");
                    Console.WriteLine($"Architecture: Client-Server (was P2P)");
                    Console.WriteLine($"Protocol: Google Protocol Buffers");
                    Console.WriteLine($"Database: SQLite with enhanced schema");
                    break;
                    
                case "config":
                    var config = ServerConfig.LoadFromFile();
                    Console.WriteLine($"\nCurrent Configuration:");
                    Console.WriteLine($"  Network Port: {config.Network.Port}");
                    Console.WriteLine($"  Max Connections: {config.Network.MaxConnections}");
                    Console.WriteLine($"  Database File: {config.Database.DatabaseFile}");
                    Console.WriteLine($"  World Seed: {config.World.WorldSeed}");
                    break;
                    
                default:
                    Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                    break;
            }
        }
        
        private static void DisplayConfigurationMenu()
        {
            Console.WriteLine("\n=== Server Configuration ===");
            var config = ServerConfig.LoadFromFile();
            
            Console.WriteLine($"Network Settings:");
            Console.WriteLine($"  Port: {config.Network.Port}");
            Console.WriteLine($"  Max Connections: {config.Network.MaxConnections}");
            Console.WriteLine($"  Timeout: {config.Network.ConnectionTimeoutMinutes} minutes");
            
            Console.WriteLine($"\nWorld Settings:");
            Console.WriteLine($"  Default World: {config.World.DefaultWorldName}");
            Console.WriteLine($"  World Seed: {config.World.WorldSeed}");
            Console.WriteLine($"  Chunk Load Radius: {config.World.ChunkLoadRadius}");
            
            Console.WriteLine($"\nGameplay Settings:");
            Console.WriteLine($"  Max Players: {config.Gameplay.MaxPlayersPerWorld}");
            Console.WriteLine($"  PvP Enabled: {config.Gameplay.EnablePvP}");
            Console.WriteLine($"  Flying Enabled: {config.Gameplay.EnableFlying}");
            
            Console.WriteLine($"\nSecurity Settings:");
            Console.WriteLine($"  Authentication Required: {config.Security.RequireAuthentication}");
            Console.WriteLine($"  Session Timeout: {config.Security.SessionTimeoutHours} hours");
            Console.WriteLine($"  Anti-cheat: {config.Security.EnableAntiCheat}");
            
            Console.WriteLine("\nConfiguration file: server-config.json");
            Console.WriteLine("Edit the file and restart the server to apply changes.");
        }
    }
}
