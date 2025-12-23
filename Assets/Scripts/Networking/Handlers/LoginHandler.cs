using Game.Auth;
using Networking.Core;

namespace Networking.Handlers
{
    /// <summary>
    /// Handles login requests on the client side and forwards to server.
    /// </summary>
    public class LoginHandler
    {
        private readonly INetworkTransport _transport;

        public LoginHandler(INetworkTransport transport)
        {
            _transport = transport;
        }

        /// <summary>Send login request through transport with proper protobuf serialization.</summary>
        public void SendLogin(string user, string password)
        {
            var req = new LoginRequest { Username = user, Password = password };

            using var memoryStream = new System.IO.MemoryStream();
            req.WriteTo(memoryStream);
            var payload = memoryStream.ToArray();

            var typeBytes = System.BitConverter.GetBytes((int)ClientMessageType.LoginRequest);
            var framed = new byte[typeBytes.Length + payload.Length];
            System.Buffer.BlockCopy(typeBytes, 0, framed, 0, typeBytes.Length);
            System.Buffer.BlockCopy(payload, 0, framed, typeBytes.Length, payload.Length);

            _transport.Send(new System.ArraySegment<byte>(framed));
        }
    }
}
