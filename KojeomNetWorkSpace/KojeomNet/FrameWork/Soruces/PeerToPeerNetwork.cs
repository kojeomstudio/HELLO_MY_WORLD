using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using KojeomNet.FrameWork.Soruces;

namespace KojeomNet.FrameWork.Soruces
{
    /// <summary>
    /// 간단한 P2P 통신을 위한 네트워크 관리 클래스.
    /// 각 피어는 동시에 서버와 클라이언트 역할을 수행할 수 있다.
    /// </summary>
    public class PeerToPeerNetwork
    {
        /// <summary>연결된 피어들의 목록.</summary>
        private readonly List<TcpClient> _peers = new List<TcpClient>();
        private readonly object _lock = new object();

        /// <summary>수신된 데이터 처리 이벤트.</summary>
        public Action<TcpClient, byte[]> OnMessageReceived;
        /// <summary>초기 핸드셰이크가 완료되었을 때 발생.</summary>
        public Action<TcpClient, string> OnHandshakeReceived;

        private TcpListener _listener;
        private readonly string _localIdentifier;

        public PeerToPeerNetwork(string localIdentifier = "Peer")
        {
            _localIdentifier = localIdentifier ?? "Peer";
        }

        /// <summary>
        /// 지정한 포트에서 다른 피어의 접속을 대기한다.
        /// </summary>
        public void StartListening(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            // 비동기 방식으로 클라이언트 접속을 기다린다.
            _listener.BeginAcceptTcpClient(OnClientAccepted, null);
            Logger.SimpleConsoleWriteLineNoFileInfo($"P2P listening on port {port}");
        }

        /// <summary>
        /// 다른 피어에 접속한다.
        /// </summary>
        public void ConnectToPeer(string host, int port)
        {
            TcpClient client = new TcpClient();
            client.BeginConnect(host, port, ar =>
            {
                try
                {
                    client.EndConnect(ar);
                    lock (_lock) _peers.Add(client);
                    BeginReceive(client);
                    Logger.SimpleConsoleWriteLineNoFileInfo($"Connected to peer {host}:{port}");
                    SendHandshake(client);
                }
                catch (Exception ex)
                {
                    Logger.SimpleConsoleWriteLineNoFileInfo($"Connect failed: {ex.Message}");
                }
            }, null);
        }

        /// <summary>
        /// 피어로부터의 접속을 처리한다.
        /// </summary>
        private void OnClientAccepted(IAsyncResult ar)
        {
            try
            {
                TcpClient client = _listener.EndAcceptTcpClient(ar);
                lock (_lock) _peers.Add(client);
                BeginReceive(client);
                Logger.SimpleConsoleWriteLineNoFileInfo("Peer connected");
                SendHandshake(client);
            }
            finally
            {
                // 다음 접속을 계속 대기.
                _listener.BeginAcceptTcpClient(OnClientAccepted, null);
            }
        }

        /// <summary>
        /// 지정한 피어로 메시지를 전송한다.
        /// </summary>
        public void Send(TcpClient client, byte[] data)
        {
            if (client == null || data == null) return;
            if (!client.Connected) { Disconnect(client); return; }

            NetworkStream stream = client.GetStream();
            try
            {
                stream.BeginWrite(data, 0, data.Length, ar =>
                {
                    try { stream.EndWrite(ar); }
                    catch { Disconnect(client); }
                }, null);
            }
            catch
            {
                Disconnect(client);
            }
        }

        /// <summary>
        /// 연결된 모든 피어에게 메시지를 전송한다.
        /// </summary>
        public void Broadcast(byte[] data)
        {
            lock (_lock)
            {
                foreach (var peer in _peers.ToArray())
                {
                    Send(peer, data);
                }
            }
        }

        /// <summary>
        /// 지정한 피어로부터 데이터를 비동기적으로 수신한다.
        /// </summary>
        private void BeginReceive(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            stream.BeginRead(buffer, 0, buffer.Length, ar =>
            {
                try
                {
                    int bytes = stream.EndRead(ar);
                    if (bytes <= 0)
                    {
                        Disconnect(client);
                        return;
                    }
                    byte[] received = new byte[bytes];
                    Buffer.BlockCopy(buffer, 0, received, 0, bytes);

                    try
                    {
                        P2PMessage message = P2PMessage.FromBytes(received);
                        if (message.MessageType == P2PMessageType.Handshake)
                        {
                            string id = Encoding.UTF8.GetString(message.Payload);
                            OnHandshakeReceived?.Invoke(client, id);
                        }
                        else
                        {
                            OnMessageReceived?.Invoke(client, received);
                        }
                    }
                    catch
                    {
                        OnMessageReceived?.Invoke(client, received);
                    }

                    BeginReceive(client);
                }
                catch
                {
                    Disconnect(client);
                }
            }, null);
        }

        /// <summary>
        /// 피어 연결을 종료하고 목록에서 제거한다.
        /// </summary>
        private void Disconnect(TcpClient client)
        {
            lock (_lock) _peers.Remove(client);
            try { client.Close(); } catch { }
            Logger.SimpleConsoleWriteLineNoFileInfo("Peer disconnected");
        }

        /// <summary>
        /// 새로 연결된 피어에게 로컬 식별자를 전송한다.
        /// </summary>
        private void SendHandshake(TcpClient client)
        {
            if (client == null) return;
            byte[] payload = Encoding.UTF8.GetBytes(_localIdentifier ?? string.Empty);
            P2PMessage msg = new P2PMessage
            {
                MessageType = P2PMessageType.Handshake,
                Payload = payload
            };
            Send(client, msg.ToBytes());
        }
    }
}
