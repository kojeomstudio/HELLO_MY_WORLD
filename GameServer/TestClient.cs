using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using SharedProtocol;

namespace GameServerApp
{
    /// <summary>
    /// 게임 서버 테스트를 위한 간단한 클라이언트
    /// 서버의 기본 기능들을 테스트하고 연결 상태를 확인합니다.
    /// </summary>
    public class TestClient
    {
        private readonly string _serverAddress;
        private readonly int _serverPort;
        private Session _session;
        private TcpClient _tcpClient;

        public TestClient(string serverAddress = "127.0.0.1", int serverPort = 9000)
        {
            _serverAddress = serverAddress;
            _serverPort = serverPort;
        }

        /// <summary>
        /// 서버에 연결합니다.
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                Console.WriteLine($"Connecting to server at {_serverAddress}:{_serverPort}...");
                
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(_serverAddress, _serverPort);
                _session = new Session(_tcpClient);
                
                Console.WriteLine("Successfully connected to server!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 서버와의 연결을 끊습니다.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                _session?.Dispose();
                _tcpClient?.Close();
                Console.WriteLine("Disconnected from server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during disconnect: {ex.Message}");
            }
        }

        /// <summary>
        /// 로그인 테스트를 수행합니다.
        /// </summary>
        public async Task TestLoginAsync(string username, string password)
        {
            try
            {
                Console.WriteLine($"Testing login for user: {username}");
                
                // 로그인 요청 전송
                var loginRequest = new LoginRequest
                {
                    Username = username,
                    Password = password,
                    ClientVersion = "1.0.0"
                };
                
                await _session.SendAsync(MessageType.LoginRequest, loginRequest);
                Console.WriteLine("Login request sent.");
                
                // 응답 수신
                var (responseType, responseMessage) = await _session.ReceiveAsync();
                
                if (responseType == MessageType.LoginResponse && responseMessage is LoginResponse loginResponse)
                {
                    if (loginResponse.Success)
                    {
                        Console.WriteLine($"✓ Login successful: {loginResponse.Message}");
                        if (loginResponse.PlayerInfo != null)
                        {
                            var pos = loginResponse.PlayerInfo.Position;
                            Console.WriteLine($"  Player position: ({pos?.X:F2}, {pos?.Y:F2}, {pos?.Z:F2})");
                            Console.WriteLine($"  Level: {loginResponse.PlayerInfo.Level}");
                            Console.WriteLine($"  Health: {loginResponse.PlayerInfo.Health}/{loginResponse.PlayerInfo.MaxHealth}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"✗ Login failed: {loginResponse.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"✗ Unexpected response type: {responseType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Login test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 이동 테스트를 수행합니다.
        /// </summary>
        public async Task TestMoveAsync(float x, float y, float z)
        {
            try
            {
                Console.WriteLine($"Testing move to ({x:F2}, {y:F2}, {z:F2})");
                
                // 이동 요청 전송
                var moveRequest = new MoveRequest
                {
                    TargetPosition = new SharedProtocol.Vector3 { X = x, Y = y, Z = z },
                    MovementSpeed = 5.0f
                };
                
                await _session.SendAsync(MessageType.MoveRequest, moveRequest);
                Console.WriteLine("Move request sent.");
                
                // 응답 수신
                var (responseType, responseMessage) = await _session.ReceiveAsync();
                
                if (responseType == MessageType.MoveResponse && responseMessage is MoveResponse moveResponse)
                {
                    if (moveResponse.Success && moveResponse.NewPosition != null)
                    {
                        var pos = moveResponse.NewPosition;
                        Console.WriteLine($"✓ Move successful: New position ({pos.X:F2}, {pos.Y:F2}, {pos.Z:F2})");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Move failed");
                    }
                }
                else
                {
                    Console.WriteLine($"✗ Unexpected response type: {responseType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Move test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 채팅 테스트를 수행합니다.
        /// </summary>
        public async Task TestChatAsync(string message)
        {
            try
            {
                Console.WriteLine($"Testing chat message: {message}");
                
                // 채팅 요청 전송
                var chatRequest = new ChatRequest
                {
                    Message = message,
                    Type = (int)ChatType.Global
                };
                
                await _session.SendAsync(MessageType.ChatRequest, chatRequest);
                Console.WriteLine("Chat request sent.");
                
                // 응답 수신 (여러 메시지가 올 수 있음)
                for (int i = 0; i < 2; i++) // 응답과 브로드캐스트
                {
                    try
                    {
                        var (responseType, responseMessage) = await _session.ReceiveAsync();
                        
                        if (responseType == MessageType.ChatResponse && responseMessage is ChatResponse chatResponse)
                        {
                            if (chatResponse.Success)
                            {
                                Console.WriteLine($"✓ Chat sent successfully");
                            }
                            else
                            {
                                Console.WriteLine($"✗ Chat failed: {chatResponse.ErrorMessage}");
                            }
                        }
                        else if (responseType == MessageType.ChatMessage && responseMessage is ChatMessage chatMessage)
                        {
                            Console.WriteLine($"✓ Chat broadcast received: [{(ChatType)chatMessage.Type}] {chatMessage.SenderName}: {chatMessage.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Chat response error: {ex.Message}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Chat test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 핑 테스트를 수행합니다.
        /// </summary>
        public async Task TestPingAsync()
        {
            try
            {
                Console.WriteLine("Testing ping...");
                
                var startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                
                // 핑 요청 전송
                var pingRequest = new PingRequest
                {
                    ClientTimestamp = startTime
                };
                
                await _session.SendAsync(MessageType.PingRequest, pingRequest);
                
                // 응답 수신
                var (responseType, responseMessage) = await _session.ReceiveAsync();
                
                if (responseType == MessageType.PingResponse && responseMessage is PingResponse pingResponse)
                {
                    var latency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - pingResponse.ClientTimestamp;
                    Console.WriteLine($"✓ Ping successful: {latency}ms latency");
                }
                else
                {
                    Console.WriteLine($"✗ Unexpected response type: {responseType}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Ping test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 전체 테스트 스위트를 실행합니다.
        /// </summary>
        public static async Task RunTestSuiteAsync()
        {
            var testClient = new TestClient();
            
            try
            {
                Console.WriteLine("=== Game Server Test Suite ===\n");
                
                // 1. 연결 테스트
                if (!await testClient.ConnectAsync())
                {
                    Console.WriteLine("Connection test failed. Cannot proceed with other tests.");
                    return;
                }
                
                await Task.Delay(100); // 연결 안정화 대기
                
                // 2. 로그인 테스트
                await testClient.TestLoginAsync("test", "password");
                await Task.Delay(100);
                
                // 3. 이동 테스트
                await testClient.TestMoveAsync(10.5f, 20.3f, 0f);
                await Task.Delay(100);
                
                // 4. 채팅 테스트
                await testClient.TestChatAsync("Hello from test client!");
                await Task.Delay(100);
                
                // 5. 핑 테스트
                await testClient.TestPingAsync();
                await Task.Delay(100);
                
                Console.WriteLine("\n=== Test Suite Completed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test suite error: {ex.Message}");
            }
            finally
            {
                testClient.Disconnect();
            }
        }
    }
}
