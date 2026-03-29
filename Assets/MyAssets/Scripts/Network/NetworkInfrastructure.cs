using System;
using System.Net;
using System.Net.Sockets;
using KojeomNet.Client.Network;

namespace KojeomNet.Client.NetworkInfrastructure
{
    /// <summary>
    /// Minimal network service wrapper used by lightweight client peers.
    /// </summary>
    public class NetworkServiceManager
    {
        public void Start()
        {
            // Placeholder for Unity-side network service initialization.
        }

        public void Stop()
        {
            // Placeholder for Unity-side network service teardown.
        }
    }

    /// <summary>
    /// Connector class for establishing network connections.
    /// </summary>
    public class Connector
    {
        private readonly NetworkServiceManager _serviceManager;

        public Action<UserToken> OnConnectedHandler { get; set; }

        public Connector(NetworkServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        public void Connect(IPEndPoint endpoint)
        {
            try
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var userToken = new UserToken(socket);
                socket.BeginConnect(endpoint, ConnectCallback, userToken);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Connection failed: {ex.Message}");
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            var userToken = (UserToken)ar.AsyncState;
            try
            {
                userToken.Socket.EndConnect(ar);
                OnConnectedHandler?.Invoke(userToken);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Connection callback failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// User token representing a network connection.
    /// </summary>
    public class UserToken
    {
        public Socket Socket { get; private set; }
        public IPeer Peer { get; private set; }

        public UserToken(Socket socket)
        {
            Socket = socket;
        }

        public void SetPeer(IPeer peer)
        {
            Peer = peer;
        }

        public void OnConnected()
        {
            // Connection established.
        }

        public void Send(ArraySegment<byte> data)
        {
            try
            {
                if (Socket.Connected)
                {
                    Socket.Send(data.Array, data.Offset, data.Count, SocketFlags.None);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Send failed: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            try
            {
                Socket?.Close();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Disconnect failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Interface for network peer communication.
    /// </summary>
    public interface IPeer
    {
        void OnMessage(CPacket msg);
        void OnRemoved();
        void Send(CPacket msg);
        void Disconnect();
    }

    /// <summary>
    /// Remote server peer implementation.
    /// </summary>
    public class RemoteServerPeer : IPeer
    {
        public UserToken UserTokenInstance { get; }

        public RemoteServerPeer(UserToken token)
        {
            UserTokenInstance = token;
            UserTokenInstance.SetPeer(this);
        }

        public void OnMessage(CPacket msg)
        {
            UnityEngine.Debug.Log("Received message from server");
        }

        public void OnRemoved()
        {
            UnityEngine.Debug.Log("Server connection removed");
        }

        public void Send(CPacket msg)
        {
            msg.RecordSize();
            UserTokenInstance.Send(new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
        }

        public void Disconnect()
        {
            UserTokenInstance.Disconnect();
        }
    }
}
