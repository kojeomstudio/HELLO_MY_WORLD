using System;
using System.Collections.Generic;
using System.Text;

namespace KojeomNet.Client.Network
{
    /// <summary>
    /// Lightweight packet helper for ad-hoc client/server messages.
    /// This is intentionally isolated from the legacy KojeomNet CPacket to avoid type collisions.
    /// </summary>
    public class CPacket
    {
        private readonly List<byte> _buffer = new List<byte>();
        private int _position;
        private readonly short _protocolId;

        public byte[] Buffer => _buffer.ToArray();
        public int Position => _position;

        private CPacket(short protocolId)
        {
            _protocolId = protocolId;
            Push(protocolId);
        }

        public static CPacket Create(short protocolId)
        {
            return new CPacket(protocolId);
        }

        public void Push(byte value)
        {
            _buffer.Add(value);
            _position++;
        }

        public void Push(short value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            _buffer.AddRange(bytes);
            _position += 2;
        }

        public void Push(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            _buffer.AddRange(bytes);
            _position += 4;
        }

        public void Push(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Push((short)0);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(value);
            Push((short)bytes.Length);
            _buffer.AddRange(bytes);
            _position += 2 + bytes.Length;
        }

        public byte Popbyte()
        {
            if (_position >= _buffer.Count)
            {
                throw new InvalidOperationException("Buffer underflow");
            }

            return _buffer[_position++];
        }

        public short PopInt16()
        {
            if (_position + 2 > _buffer.Count)
            {
                throw new InvalidOperationException("Buffer underflow");
            }

            var bytes = new[] { _buffer[_position], _buffer[_position + 1] };
            _position += 2;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToInt16(bytes, 0);
        }

        public int PopInt32()
        {
            if (_position + 4 > _buffer.Count)
            {
                throw new InvalidOperationException("Buffer underflow");
            }

            var bytes = new[]
            {
                _buffer[_position],
                _buffer[_position + 1],
                _buffer[_position + 2],
                _buffer[_position + 3]
            };
            _position += 4;

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToInt32(bytes, 0);
        }

        public string PopString()
        {
            var length = PopInt16();
            if (length <= 0)
            {
                return string.Empty;
            }

            if (_position + length > _buffer.Count)
            {
                throw new InvalidOperationException("Buffer underflow");
            }

            var bytes = new byte[length];
            Array.Copy(_buffer.ToArray(), _position, bytes, 0, length);
            _position += length;

            return Encoding.UTF8.GetString(bytes);
        }

        public short PopProtocolID()
        {
            return _protocolId;
        }

        public void RecordSize()
        {
            // Record packet size at the beginning for network transmission
            var size = (short)(_buffer.Count - 2); // Exclude protocol ID from size
            var sizeBytes = BitConverter.GetBytes(size);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(sizeBytes);
            }

            // Insert size at the beginning (after protocol ID)
            _buffer.InsertRange(2, sizeBytes);
            _position += 2;
        }
    }
}
