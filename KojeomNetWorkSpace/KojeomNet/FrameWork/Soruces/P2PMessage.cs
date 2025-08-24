using System;

namespace KojeomNet.FrameWork.Soruces
{
    /// <summary>
    /// 기본 P2P 메시지 형식과 직렬화 유틸리티를 제공한다.
    /// </summary>
    public class P2PMessage
    {
        /// <summary>메시지 종류.</summary>
        public P2PMessageType MessageType { get; set; }
        /// <summary>메시지 페이로드.</summary>
        public byte[] Payload { get; set; }

        /// <summary>
        /// 메시지를 전송 가능한 바이트 배열로 변환한다.
        /// 형식: [1바이트 메시지타입][나머지 페이로드]
        /// </summary>
        public byte[] ToBytes()
        {
            if (Payload == null) Payload = Array.Empty<byte>();
            byte[] data = new byte[1 + Payload.Length];
            data[0] = (byte)MessageType;
            Buffer.BlockCopy(Payload, 0, data, 1, Payload.Length);
            return data;
        }

        /// <summary>
        /// 수신된 바이트 배열을 메시지 객체로 변환한다.
        /// </summary>
        public static P2PMessage FromBytes(byte[] data)
        {
            if (data == null || data.Length < 1)
                throw new ArgumentException("잘못된 P2P 메시지 데이터입니다.");
            P2PMessageType type = (P2PMessageType)data[0];
            byte[] payload = new byte[data.Length - 1];
            Buffer.BlockCopy(data, 1, payload, 0, payload.Length);
            return new P2PMessage { MessageType = type, Payload = payload };
        }
    }

    /// <summary>
    /// P2P 메시지 타입 정의.
    /// </summary>
    public enum P2PMessageType : byte
    {
        /// <summary>초기 연결시 서로의 식별 정보를 교환하기 위한 메시지.</summary>
        Handshake = 0,
        CharacterMovement = 1,
        LevelLoad = 2
    }
}
