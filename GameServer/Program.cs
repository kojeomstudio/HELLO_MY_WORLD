using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameServerApp
{
    /// <summary>
    /// 게임 서버 애플리케이션의 메인 진입점
    /// 서버 실행 또는 테스트 클라이언트 실행을 선택할 수 있습니다.
    /// </summary>
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Hello My World Game Server ===");
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1. Start Server");
            Console.WriteLine("2. Run Test Client");
            Console.WriteLine("3. Exit");
            
            while (true)
            {
                Console.Write("Enter your choice (1-3): ");
                var choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        await RunServerAsync();
                        break;
                        
                    case "2":
                        await TestClient.RunTestSuiteAsync();
                        break;
                        
                    case "3":
                        Console.WriteLine("Goodbye!");
                        return;
                        
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1, 2, or 3.");
                        continue;
                }
                
                Console.WriteLine("\nPress any key to return to menu...");
                Console.ReadKey();
                Console.Clear();
                
                Console.WriteLine("=== Hello My World Game Server ===");
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Start Server");
                Console.WriteLine("2. Run Test Client");
                Console.WriteLine("3. Exit");
            }
        }
        
        /// <summary>
        /// 게임 서버를 실행합니다.
        /// </summary>
        private static async Task RunServerAsync()
        {
            var server = new GameServer();
            
            // Ctrl+C 처리를 위한 CancellationToken 설정
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nShutdown signal received. Stopping server...");
            };
            
            try
            {
                // 서버 시작 (백그라운드 작업)
                var serverTask = server.StartAsync();
                
                Console.WriteLine("Server is running. Press Ctrl+C to stop.");
                
                // 종료 신호 대기
                try
                {
                    await Task.Delay(-1, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // 예상된 예외 (Ctrl+C)
                }
                
                // 서버 안전 종료
                await server.StopAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
        }
    }
}
