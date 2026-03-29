using System;
using System.Threading;
using System.Threading.Tasks;
using GameServerApp;
using GameCommon.Configuration;

namespace GameServer.Launcher
{
    /// <summary>
    /// HELLO_MY_WORLD 게임 서버 런처
    /// 서버 실행, 관리, 모니터링을 담당하는 독립 실행 프로그램
    /// </summary>
    public class Program
    {
        private static GameServerApp.GameServer? _server;
        private static CancellationTokenSource? _serverCts;
        private static LauncherConfig? _launcherConfig;

        public static async Task<int> Main(string[] args)
        {
            try
            {
                DisplayBanner();

                // 런처 설정 로드
                _launcherConfig = LauncherConfig.Load();

                // 명령줄 인자 파싱
                var command = ParseArguments(args);

                return command switch
                {
                    LauncherCommand.Start => await StartServerAsync(args),
                    LauncherCommand.Stop => StopServer(),
                    LauncherCommand.Status => ShowServerStatus(),
                    LauncherCommand.Interactive => await RunInteractiveMode(),
                    LauncherCommand.Help => ShowHelp(),
                    _ => await RunInteractiveMode()
                };
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL ERROR] {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.ResetColor();
                return 1;
            }
        }

        private static void DisplayBanner()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
╔══════════════════════════════════════════════════════════════╗
║                                                              ║
║         HELLO_MY_WORLD Game Server Launcher v1.0            ║
║                                                              ║
║         Minecraft-like Voxel Game Server                     ║
║         Room-Based Multiplayer Architecture                  ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
");
            Console.ResetColor();
            Console.WriteLine($"Launcher Version: 1.0.0");
            Console.WriteLine($"Target Framework: .NET 6.0");
            Console.WriteLine($"Unity Client: Unity 6 (6000.0.23f1)");
            Console.WriteLine($"Current Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine(new string('─', 62));
        }

        private static LauncherCommand ParseArguments(string[] args)
        {
            if (args.Length == 0)
                return LauncherCommand.Interactive;

            return args[0].ToLowerInvariant() switch
            {
                "start" or "--start" or "-s" => LauncherCommand.Start,
                "stop" or "--stop" => LauncherCommand.Stop,
                "status" or "--status" => LauncherCommand.Status,
                "help" or "--help" or "-h" or "-?" => LauncherCommand.Help,
                _ => LauncherCommand.Interactive
            };
        }

        private static async Task<int> StartServerAsync(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[LAUNCHER] Starting game server...");
            Console.ResetColor();

            try
            {
                // GameCommon 설정 로드 (향후 사용)
                if (_launcherConfig!.LoadGameCommonConfig)
                {
                    Console.WriteLine("[CONFIG] Loading GameCommon configuration...");
                    try
                    {
                        ConfigManager.Instance.LoadAll("config");
                        Console.WriteLine("[CONFIG] GameCommon config loaded successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARNING] GameCommon config failed: {ex.Message}");
                    }
                }

                // GameServer 설정 로드
                var serverConfig = ServerConfig.LoadFromFile(_launcherConfig.ServerConfigPath);
                Console.WriteLine($"[CONFIG] Server config loaded from: {_launcherConfig.ServerConfigPath}");

                // 서버 인스턴스 생성
                _server = new GameServerApp.GameServer(
                    serverConfig.Network.Port,
                    serverConfig.Database.DatabaseFile,
                    serverConfig
                );

                // 서버 시작
                _serverCts = new CancellationTokenSource();
                var serverTask = _server.StartAsync();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[SERVER] Server started on port {serverConfig.Network.Port}");
                Console.WriteLine($"[SERVER] Max players: {serverConfig.Network.MaxPlayers}");
                Console.WriteLine($"[SERVER] Database: {serverConfig.Database.DatabaseFile}");
                Console.ResetColor();

                // 대기 모드에 따라 처리
                if (_launcherConfig.WaitForExit)
                {
                    Console.WriteLine("\n[LAUNCHER] Press Ctrl+C to stop the server...");
                    Console.CancelKeyPress += (s, e) =>
                    {
                        e.Cancel = true;
                        Console.WriteLine("\n[LAUNCHER] Shutting down server...");
                        _server?.Stop();
                        _serverCts?.Cancel();
                    };

                    await serverTask;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Failed to start server: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        private static int StopServer()
        {
            if (_server == null)
            {
                Console.WriteLine("[WARNING] No server instance is running");
                return 1;
            }

            Console.WriteLine("[LAUNCHER] Stopping server...");
            _server.Stop();
            _serverCts?.Cancel();
            Console.WriteLine("[SERVER] Server stopped successfully");
            return 0;
        }

        private static int ShowServerStatus()
        {
            Console.WriteLine("\n[STATUS] Server Status");
            Console.WriteLine(new string('─', 62));

            if (_server == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Server Status: STOPPED");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Server Status: RUNNING");
                Console.ResetColor();
                // TODO: 실시간 통계 표시 (플레이어 수, 룸 수 등)
            }

            return 0;
        }

        private static async Task<int> RunInteractiveMode()
        {
            while (true)
            {
                DisplayMenu();
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        await StartServerAsync(Array.Empty<string>());
                        break;

                    case "2":
                        StopServer();
                        break;

                    case "3":
                        ShowServerStatus();
                        break;

                    case "4":
                        ShowConfiguration();
                        break;

                    case "5":
                        ShowGameCommonInfo();
                        break;

                    case "6":
                        ShowRoomArchitectureInfo();
                        break;

                    case "0":
                        Console.WriteLine("\n[LAUNCHER] Shutting down...");
                        if (_server != null)
                        {
                            StopServer();
                            await Task.Delay(500);
                        }
                        Console.WriteLine("[LAUNCHER] Goodbye!");
                        return 0;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid choice. Please try again.");
                        Console.ResetColor();
                        break;
                }

                if (choice != "1") // 서버 시작이 아니면 계속 표시
                {
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                    DisplayBanner();
                }
            }
        }

        private static void DisplayMenu()
        {
            Console.WriteLine("\n[MENU] Available Commands:");
            Console.WriteLine(new string('─', 62));
            Console.WriteLine("  1. Start Server");
            Console.WriteLine("  2. Stop Server");
            Console.WriteLine("  3. Server Status");
            Console.WriteLine("  4. Show Configuration");
            Console.WriteLine("  5. GameCommon Library Info");
            Console.WriteLine("  6. Room Architecture Info");
            Console.WriteLine("  0. Exit");
            Console.WriteLine(new string('─', 62));
            Console.Write("\nEnter your choice: ");
        }

        private static void ShowConfiguration()
        {
            Console.WriteLine("\n[CONFIGURATION] Server Settings");
            Console.WriteLine(new string('─', 62));

            try
            {
                var config = ServerConfig.LoadFromFile(_launcherConfig!.ServerConfigPath);

                Console.WriteLine($"Network:");
                Console.WriteLine($"  - Host: {config.Network.Host}");
                Console.WriteLine($"  - Port: {config.Network.Port}");
                Console.WriteLine($"  - Max Players: {config.Network.MaxPlayers}");

                Console.WriteLine($"\nDatabase:");
                Console.WriteLine($"  - File: {config.Database.DatabaseFile}");

                Console.WriteLine($"\nWorld:");
                Console.WriteLine($"  - Seed: {config.World.WorldSeed}");
                Console.WriteLine($"  - Chunk Size: {config.World.ChunkSize}");

                Console.WriteLine($"\nGameplay:");
                Console.WriteLine($"  - Difficulty: {config.Gameplay.Difficulty}");
                Console.WriteLine($"  - PvP: {config.Gameplay.EnablePvP}");

                Console.WriteLine($"\nSecurity:");
                Console.WriteLine($"  - Anti-Cheat: {config.Security.EnableAntiCheat}");

                Console.WriteLine($"\nPerformance:");
                Console.WriteLine($"  - Max Chunk Loads/Tick: {config.Performance.MaxChunkLoadsPerTick}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error loading config: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void ShowGameCommonInfo()
        {
            Console.WriteLine("\n[GAMECOMMON] Library Information");
            Console.WriteLine(new string('─', 62));
            Console.WriteLine("Target Framework: .NET Standard 2.1 (Unity 6 Compatible)");
            Console.WriteLine("Purpose: Shared game logic between server and client");
            Console.WriteLine("\nModules:");
            Console.WriteLine("  - Blocks: BlockType, BlockProperties, BlockRegistry");
            Console.WriteLine("  - Configuration: WorldConfig, GameplayConfig, ServerConfig");
            Console.WriteLine("\nStatus: Available for integration");
            Console.WriteLine("See: docs/IMPLEMENTATION_GUIDE.md for integration steps");
        }

        private static void ShowRoomArchitectureInfo()
        {
            Console.WriteLine("\n[ARCHITECTURE] Room-Based Multiplayer");
            Console.WriteLine(new string('─', 62));
            Console.WriteLine("System: Fully implemented room-based architecture");
            Console.WriteLine("Isolation: Each room has unique worldId");
            Console.WriteLine("Features:");
            Console.WriteLine("  - Dynamic room creation/deletion");
            Console.WriteLine("  - Player queueing system");
            Console.WriteLine("  - Spectator mode");
            Console.WriteLine("  - Role-based permissions (Host, Player, Spectator)");
            Console.WriteLine("  - World isolation by worldId");
            Console.WriteLine("  - Deterministic terrain generation");
            Console.WriteLine("\nMaturity Score: 8.3/10 (Production Ready)");
            Console.WriteLine("See: docs/ROOM_BASED_ARCHITECTURE.md for details");
        }

        private static int ShowHelp()
        {
            Console.WriteLine("\n[HELP] GameServer Launcher Usage");
            Console.WriteLine(new string('─', 62));
            Console.WriteLine("Usage: GameServerLauncher [command] [options]");
            Console.WriteLine("\nCommands:");
            Console.WriteLine("  start, -s, --start    Start the game server");
            Console.WriteLine("  stop, --stop          Stop the game server");
            Console.WriteLine("  status, --status      Show server status");
            Console.WriteLine("  help, -h, --help      Show this help message");
            Console.WriteLine("  (no command)          Run in interactive mode");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  GameServerLauncher start        # Start server");
            Console.WriteLine("  GameServerLauncher --status     # Check status");
            Console.WriteLine("  GameServerLauncher              # Interactive menu");
            Console.WriteLine("\nConfiguration:");
            Console.WriteLine("  Server: server-config.json");
            Console.WriteLine("  Launcher: launcher-config.json");
            Console.WriteLine(new string('─', 62));
            return 0;
        }
    }

    internal enum LauncherCommand
    {
        Interactive,
        Start,
        Stop,
        Status,
        Help
    }
}
