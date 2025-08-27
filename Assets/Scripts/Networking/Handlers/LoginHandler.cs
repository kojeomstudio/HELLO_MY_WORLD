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

        /// <summary>Send login request through transport.</summary>
        public void SendLogin(string user, string password)
        {
            var req = new LoginRequest { Username = user, Password = password };
            // serialization placeholder
            _transport.Send(new System.ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(req.Username)));
        }
    }
}
