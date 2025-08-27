using System;
using System.Threading.Tasks;

namespace Networking.Core
{
    /// <summary>
    /// Abstraction for swappable network transports (UTP, NGO, KojeomNet)
    /// </summary>
    public interface INetworkTransport
    {
        /// <summary>Connect to remote endpoint.</summary>
        Task ConnectAsync(string address, int port);

        /// <summary>Send raw payload to remote peer.</summary>
        void Send(ArraySegment<byte> payload);

        /// <summary>Event raised when a payload is received.</summary>
        event Action<ArraySegment<byte>> Received;
    }
}
