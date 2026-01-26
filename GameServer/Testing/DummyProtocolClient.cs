using System;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EnhancedMinecraftProtocol;
using GameCommon.World;
using Google.Protobuf;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;
using CommonVector3Int = MinecraftGame.Common.Vector3Int;

namespace GameServerApp.Testing
{
    /// <summary>
    /// Minimal EnhancedMinecraftProtocol round-trip client for packet generation/validation tests.
    /// </summary>
    public sealed record ProtocolRoundTripResult(
        MinecraftMessageType MessageType,
        int PayloadSize,
        string Descriptor,
        string Signature,
        byte[] Payload);

    public static class DummyProtocolClient
    {
        /// <summary>
        /// Builds a framed TimeUpdate packet and verifies deserialize/serialize paths locally.
        /// </summary>
        public static ProtocolRoundTripResult BuildTimeUpdateRoundTrip()
        {
            ProtocolRegistry.ValidateBindings();
            ProtoRuntime.EnsureInitialized();
            ProtoFingerprint.AssertDescriptorFingerprint();

            var messageType = MinecraftMessageType.TimeUpdate;
            var message = new TimeUpdateBroadcast
            {
                WorldTime = 1200,
                DayTime = 6000
            };

            ProtocolRegistry.EnsureRegistered(messageType);
            var payload = message.ToByteArray();
            var parsed = TimeUpdateBroadcast.Parser.ParseFrom(payload);

            if (parsed.WorldTime != message.WorldTime || parsed.DayTime != message.DayTime)
            {
                throw new InvalidOperationException("[DummyProtocolClient] Parsed payload does not match source message.");
            }

            return new ProtocolRoundTripResult(
                messageType,
                payload.Length,
                TimeUpdateBroadcast.Descriptor.FullName,
                SharedFeatureCatalog.HydrologySignature,
                BuildFrame(messageType, payload));
        }

        /// <summary>
        /// Builds a framed ChunkLoadRequest packet covering multiple positions for registry validation.
        /// </summary>
        public static ProtocolRoundTripResult BuildChunkLoadRequestRoundTrip()
        {
            ProtocolRegistry.ValidateBindings();
            ProtoRuntime.EnsureInitialized();
            ProtoFingerprint.AssertDescriptorFingerprint();

            var messageType = MinecraftMessageType.ChunkDataRequest;
            var request = new ChunkLoadRequest
            {
                ViewDistance = 6
            };

            request.ChunkPositions.Add(new CommonVector3Int { X = 0, Y = 0, Z = 0 });
            request.ChunkPositions.Add(new CommonVector3Int { X = 1, Y = 0, Z = -1 });
            request.ChunkPositions.Add(new CommonVector3Int { X = -2, Y = 0, Z = 3 });

            ProtocolRegistry.EnsureRegistered(messageType);
            var payload = request.ToByteArray();
            var parsed = ChunkLoadRequest.Parser.ParseFrom(payload);

            if (parsed.ViewDistance != request.ViewDistance || parsed.ChunkPositions.Count != request.ChunkPositions.Count)
            {
                throw new InvalidOperationException("[DummyProtocolClient] Parsed chunk-load request does not match source message.");
            }

            return new ProtocolRoundTripResult(
                messageType,
                payload.Length,
                ChunkLoadRequest.Descriptor.FullName,
                SharedFeatureCatalog.HydrologySignature,
                BuildFrame(messageType, payload));
        }

        /// <summary>
        /// Sends a framed payload to a running server. No response parsing is performed.
        /// </summary>
        public static async Task<ProtocolRoundTripResult> SendAsync(string host = "127.0.0.1", int port = 9000, CancellationToken token = default)
        {
            return await SendRoundTripAsync(BuildTimeUpdateRoundTrip, host, port, token);
        }

        /// <summary>
        /// Sends a chunk-load request frame to a running server.
        /// </summary>
        public static Task<ProtocolRoundTripResult> SendChunkRequestAsync(string host = "127.0.0.1", int port = 9000, CancellationToken token = default)
        {
            return SendRoundTripAsync(BuildChunkLoadRequestRoundTrip, host, port, token);
        }

        private static byte[] BuildFrame(MinecraftMessageType messageType, byte[] payload)
        {
            Span<byte> header = stackalloc byte[6];
            BinaryPrimitives.WriteInt32BigEndian(header.Slice(0, 4), payload.Length);
            BinaryPrimitives.WriteInt16BigEndian(header.Slice(4, 2), (short)messageType);

            var frame = new byte[header.Length + payload.Length];
            header.CopyTo(frame.AsSpan(0, header.Length));
            payload.CopyTo(frame.AsSpan(header.Length));
            return frame;
        }

        private static async Task<ProtocolRoundTripResult> SendRoundTripAsync(Func<ProtocolRoundTripResult> builder, string host, int port, CancellationToken token)
        {
            var roundTrip = builder();

            using var client = new TcpClient();
            await client.ConnectAsync(host, port);

            using var stream = client.GetStream();
            await stream.WriteAsync(roundTrip.Payload.AsMemory(0, roundTrip.Payload.Length), token);
            await stream.FlushAsync(token);

            return roundTrip;
        }
    }
}
