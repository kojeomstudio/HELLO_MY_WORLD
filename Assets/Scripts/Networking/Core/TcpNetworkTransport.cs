using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Networking.Core
{
    /// <summary>
    /// TCP 기반 네트워크 전송 구현체
    /// Unity 클라이언트와 서버 간의 TCP 통신을 담당합니다.
    /// </summary>
    public class TcpNetworkTransport : INetworkTransport, IDisposable
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isConnected = false;
        
        /// <summary>페이로드 수신 이벤트</summary>
        public event Action<ArraySegment<byte>> Received;
        
        /// <summary>연결 상태 변경 이벤트</summary>
        public event Action<bool> ConnectionStatusChanged;

        public bool IsConnected => _isConnected && _client?.Connected == true;

        /// <summary>
        /// 서버에 비동기적으로 연결합니다.
        /// </summary>
        public async Task ConnectAsync(string address, int port)
        {
            try
            {
                await DisconnectAsync();
                
                _cancellationTokenSource = new CancellationTokenSource();
                _client = new TcpClient();
                
                Debug.Log($"Connecting to {address}:{port}...");
                
                await _client.ConnectAsync(address, port);
                _stream = _client.GetStream();
                _isConnected = true;
                
                Debug.Log($"Successfully connected to {address}:{port}");
                ConnectionStatusChanged?.Invoke(true);
                
                // 수신 루프 시작
                _ = StartReceiveLoopAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect to {address}:{port}: {ex.Message}");
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(false);
                throw;
            }
        }

        /// <summary>
        /// 서버와의 연결을 끊습니다.
        /// </summary>
        public async Task DisconnectAsync()
        {
            try
            {
                _isConnected = false;
                _cancellationTokenSource?.Cancel();
                
                if (_stream != null)
                {
                    _stream.Close();
                    _stream.Dispose();
                    _stream = null;
                }
                
                if (_client != null)
                {
                    _client.Close();
                    _client.Dispose();
                    _client = null;
                }
                
                ConnectionStatusChanged?.Invoke(false);
                Debug.Log("Disconnected from server");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error during disconnect: {ex.Message}");
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// 서버로 페이로드를 전송합니다.
        /// </summary>
        public void Send(ArraySegment<byte> payload)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("Cannot send: not connected to server");
                return;
            }

            try
            {
                // 길이 헤더 추가 (4바이트)
                var lengthBytes = BitConverter.GetBytes(payload.Count);
                _stream.Write(lengthBytes, 0, lengthBytes.Length);
                _stream.Write(payload.Array, payload.Offset, payload.Count);
                _stream.Flush();
                
                // Debug.Log($"Sent {payload.Count} bytes to server");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to send data: {ex.Message}");
                _isConnected = false;
                ConnectionStatusChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// 서버로부터 데이터를 수신하는 비동기 루프
        /// </summary>
        private async Task StartReceiveLoopAsync(CancellationToken cancellationToken)
        {
            Debug.Log("Started receive loop");
            
            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    // 길이 헤더 읽기 (4바이트)
                    var lengthBuffer = await ReadExactAsync(4, cancellationToken);
                    if (lengthBuffer == null) break;
                    
                    var length = BitConverter.ToInt32(lengthBuffer, 0);
                    
                    // 데이터 크기 검증
                    if (length <= 0 || length > 1024 * 1024) // 1MB 제한
                    {
                        Debug.LogError($"Invalid payload length: {length}");
                        break;
                    }
                    
                    // 페이로드 읽기
                    var payloadBuffer = await ReadExactAsync(length, cancellationToken);
                    if (payloadBuffer == null) break;
                    
                    // Unity 메인 스레드에서 이벤트 발생
                    UnityMainThread.Execute(() => 
                    {
                        try
                        {
                            Received?.Invoke(new ArraySegment<byte>(payloadBuffer));
                        }
                        catch (Exception ex)\n                        {\n                            Debug.LogError($\"Error in Received event handler: {ex.Message}\");\n                        }\n                    });\n                }\n            }\n            catch (OperationCanceledException)\n            {\n                Debug.Log(\"Receive loop cancelled\");\n            }\n            catch (Exception ex)\n            {\n                Debug.LogError($\"Receive loop error: {ex.Message}\");\n            }\n            finally\n            {\n                Debug.Log(\"Receive loop ended\");\n                if (_isConnected)\n                {\n                    _isConnected = false;\n                    UnityMainThread.Execute(() => ConnectionStatusChanged?.Invoke(false));\n                }\n            }\n        }\n\n        /// <summary>\n        /// 지정된 크기만큼 데이터를 정확히 읽습니다.\n        /// </summary>\n        private async Task<byte[]> ReadExactAsync(int size, CancellationToken cancellationToken)\n        {\n            var buffer = new byte[size];\n            int totalRead = 0;\n            \n            while (totalRead < size && !cancellationToken.IsCancellationRequested)\n            {\n                var read = await _stream.ReadAsync(buffer, totalRead, size - totalRead, cancellationToken);\n                if (read == 0)\n                {\n                    Debug.Log(\"Server disconnected (read 0 bytes)\");\n                    return null;\n                }\n                totalRead += read;\n            }\n            \n            return totalRead == size ? buffer : null;\n        }\n\n        /// <summary>\n        /// 리소스를 정리합니다.\n        /// </summary>\n        public void Dispose()\n        {\n            _ = DisconnectAsync();\n        }\n    }\n\n    /// <summary>\n    /// Unity 메인 스레드에서 작업을 실행하기 위한 헬퍼 클래스\n    /// </summary>\n    public static class UnityMainThread\n    {\n        private static readonly System.Collections.Concurrent.ConcurrentQueue<Action> _actions = new();\n        private static bool _initialized = false;\n\n        /// <summary>\n        /// Unity 메인 스레드에서 액션을 실행하도록 예약합니다.\n        /// </summary>\n        public static void Execute(Action action)\n        {\n            if (action == null) return;\n            \n            if (Application.isPlaying)\n            {\n                _actions.Enqueue(action);\n                \n                if (!_initialized)\n                {\n                    _initialized = true;\n                    // 메인 스레드에서 큐를 처리할 MonoBehaviour 컴포넌트를 생성\n                    var go = new GameObject(\"UnityMainThreadDispatcher\");\n                    go.AddComponent<UnityMainThreadDispatcher>();\n                    UnityEngine.Object.DontDestroyOnLoad(go);\n                }\n            }\n        }\n        \n        /// <summary>\n        /// 큐에 있는 모든 액션을 실행합니다. (MonoBehaviour.Update에서 호출)\n        /// </summary>\n        internal static void ProcessQueue()\n        {\n            while (_actions.TryDequeue(out var action))\n            {\n                try\n                {\n                    action();\n                }\n                catch (Exception ex)\n                {\n                    Debug.LogError($\"Error executing main thread action: {ex.Message}\");\n                }\n            }\n        }\n    }\n\n    /// <summary>\n    /// Unity 메인 스레드에서 큐를 처리하는 MonoBehaviour\n    /// </summary>\n    public class UnityMainThreadDispatcher : MonoBehaviour\n    {\n        private void Update()\n        {\n            UnityMainThread.ProcessQueue();\n        }\n    }\n}