using System;
using System.Net.Sockets;

namespace KojeomNet.FrameWork.Soruces
{
    /// <summary>
    /// 캐릭터의 위치를 P2P 네트워크 상에서 동기화하는 기본 클래스.
    /// </summary>
    public class P2PCharacterSynchronizer
    {
        private readonly PeerToPeerNetwork _network;

        /// <summary>수신한 위치 정보를 전달하는 이벤트.</summary>
        public Action<TcpClient, float, float, float> OnPositionReceived;

        public P2PCharacterSynchronizer(PeerToPeerNetwork network)
        {
            _network = network;
            _network.OnMessageReceived += HandleMessage;
        }

        /// <summary>
        /// 현재 캐릭터 위치를 모든 피어에게 전송한다.
        /// </summary>
        public void BroadcastPosition(float x, float y, float z)
        {
            byte[] payload = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(x), 0, payload, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(y), 0, payload, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(z), 0, payload, 8, 4);
            P2PMessage msg = new P2PMessage
            {
                MessageType = P2PMessageType.CharacterMovement,
                Payload = payload
            };
            _network.Broadcast(msg.ToBytes());
        }

        private void HandleMessage(TcpClient client, byte[] data)
        {
            P2PMessage message = P2PMessage.FromBytes(data);
            if (message.MessageType != P2PMessageType.CharacterMovement)
                return;
            float x = BitConverter.ToSingle(message.Payload, 0);
            float y = BitConverter.ToSingle(message.Payload, 4);
            float z = BitConverter.ToSingle(message.Payload, 8);
            OnPositionReceived?.Invoke(client, x, y, z);
        }
    }
}
