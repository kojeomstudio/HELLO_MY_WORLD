using System;
using System.Net.Sockets;
using System.Text;

namespace KojeomNet.FrameWork.Soruces
{
    /// <summary>
    /// 레벨(씬) 로딩을 P2P 방식으로 동기화하기 위한 기본 클래스.
    /// </summary>
    public class P2PLevelSynchronizer
    {
        private readonly PeerToPeerNetwork _network;

        /// <summary>다른 피어가 레벨 로드를 요청했을 때 발생.</summary>
        public Action<TcpClient, string> OnLevelLoadRequested;

        public P2PLevelSynchronizer(PeerToPeerNetwork network)
        {
            _network = network;
            _network.OnMessageReceived += HandleMessage;
        }

        /// <summary>
        /// 레벨 로드를 모든 피어에게 통지한다.
        /// </summary>
        public void BroadcastLevelLoad(string levelName)
        {
            byte[] payload = Encoding.UTF8.GetBytes(levelName ?? string.Empty);
            P2PMessage msg = new P2PMessage
            {
                MessageType = P2PMessageType.LevelLoad,
                Payload = payload
            };
            _network.Broadcast(msg.ToBytes());
        }

        private void HandleMessage(TcpClient client, byte[] data)
        {
            P2PMessage message = P2PMessage.FromBytes(data);
            if (message.MessageType != P2PMessageType.LevelLoad)
                return;
            string levelName = Encoding.UTF8.GetString(message.Payload);
            OnLevelLoadRequested?.Invoke(client, levelName);
        }
    }
}
