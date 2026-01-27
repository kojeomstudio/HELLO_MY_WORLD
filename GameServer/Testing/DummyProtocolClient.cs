using System;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnhancedMinecraftProtocol;
using GameCommon.World;
using CommonVector3Int = MinecraftGame.Common.Vector3Int;
using Google.Protobuf;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;

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
            AuditProtocolRegistry();

            var messageType = MinecraftMessageType.TimeUpdate;
            var message = new TimeUpdateBroadcast
            {
                WorldTime = 12000,
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
                $"{SharedFeatureCatalog.HydrologySignature}:{ProtoFingerprint.ComputeFingerprint()}",
                BuildFrame(messageType, payload));
        }

        /// <summary>
        /// Builds a framed ChunkLoadRequest packet covering multiple positions for registry validation.
        /// </summary>
        public static ProtocolRoundTripResult BuildChunkLoadRequestRoundTrip()
        {
            AuditProtocolRegistry();

            var messageType = MinecraftMessageType.ChunkDataRequest;
            var request = new ChunkLoadRequest { ViewDistance = 6 };
            request.ChunkPositions.Add(new CommonVector3Int { X = 0, Y = 0, Z = 0 });
            request.ChunkPositions.Add(new CommonVector3Int { X = 1, Y = 0, Z = 0 });
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
                $"{SharedFeatureCatalog.HydrologySignature}:{ProtoFingerprint.ComputeFingerprint()}",
                BuildFrame(messageType, payload));
        }

        /// <summary>
        /// Builds a framed BlockChangeNotification packet to validate item/block contracts.
        /// </summary>
        public static ProtocolRoundTripResult BuildBlockChangeRoundTrip()
        {
            AuditProtocolRegistry();

            var messageType = MinecraftMessageType.BlockChangeNotification;
            var broadcast = new BlockChangeBroadcast
            {
                Position = new CommonVector3Int { X = 4, Y = 64, Z = 4 },
                OldBlockId = 1,
                NewBlockId = 2,
                Metadata = 0,
                PlayerId = "dummy-tester",
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Reason = ChangeReason.PlayerBreak
            };

            broadcast.Drops.Add(new ItemStack
            {
                ItemId = 2,
                ItemName = "stone",
                Count = 1,
                Durability = 0,
                MaxDurability = 0
            });

            ProtocolRegistry.EnsureRegistered(messageType);
            var payload = broadcast.ToByteArray();
            var parsed = BlockChangeBroadcast.Parser.ParseFrom(payload);

            if (parsed.NewBlockId != broadcast.NewBlockId ||
                parsed.Position == null ||
                parsed.Position.X != broadcast.Position.X)
            {
                throw new InvalidOperationException("[DummyProtocolClient] Parsed block-change payload does not match source message.");
            }

            return new ProtocolRoundTripResult(
                messageType,
                payload.Length,
                BlockChangeBroadcast.Descriptor.FullName,
                $"{SharedFeatureCatalog.HydrologySignature}:{ProtoFingerprint.ComputeFingerprint()}",
                BuildFrame(messageType, payload));
        }

        /// <summary>
        /// Sends a framed payload to a running server. No response parsing is performed.
        /// </summary>
        public static Task<ProtocolRoundTripResult> SendAsync(string host = "127.0.0.1", int port = 9000, CancellationToken token = default)
        {
            return SendRoundTripAsync(BuildTimeUpdateRoundTrip, host, port, token);
        }

        /// <summary>
        /// Sends a chunk-load request frame to a running server.
        /// </summary>
        public static Task<ProtocolRoundTripResult> SendChunkRequestAsync(string host = "127.0.0.1", int port = 9000, CancellationToken token = default)
        {
            return SendRoundTripAsync(BuildChunkLoadRequestRoundTrip, host, port, token);
        }

        /// <summary>
        /// Builds a 6-byte frame header: 4-byte big-endian length + 2-byte big-endian message type.
        /// </summary>
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

        /// <summary>
        /// Sends a round-trip frame to a running server and returns the result.
        /// </summary>
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

        private static void AuditProtocolRegistry()
        {
            ProtocolRegistry.ValidateBindings();
            ProtocolValidator.ValidateEnhancedContracts();
            ProtoRuntime.EnsureInitialized();
            ProtoFingerprint.AssertDescriptorFingerprint();
            ProtoDiagnostics.AssertRegistryClean();
            ProtocolRegistry.EnsureRequiredBindings();

            var optionalMissing = ProtocolRegistry.GetUnregisteredOptionalTypes().ToArray();
            if (optionalMissing.Length > 0)
            {
                Console.WriteLine($"[DummyProtocolClient] Optional proto bindings missing: {string.Join(", ", optionalMissing)}");
            }
        }
    }

    /// <summary>
    /// Entry point for the dummy protocol client. Sends a time update and chunk-load request.
    /// </summary>
    public static class DummyProtocolClientMain
    {
        public static async Task RunAsync(string[] args)
        {
            string host = "127.0.0.1";
            int port = 9000;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--host" when i + 1 < args.Length:
                        host = args[i + 1];
                        break;
                    case "--port" when i + 1 < args.Length:
                        int.TryParse(args[i + 1], out port);
                        break;
                }
            }

            Console.WriteLine($"[DummyClient] Sending proto frames to {host}:{port}");
            var blockChange = DummyProtocolClient.BuildBlockChangeRoundTrip();
            Console.WriteLine($"[DummyClient] Built {blockChange.MessageType} ({blockChange.PayloadSize} bytes) sig={blockChange.Signature}");

            var timeResult = await DummyProtocolClient.SendAsync(host, port);
            Console.WriteLine($"[DummyClient] Sent {timeResult.MessageType} ({timeResult.PayloadSize} bytes) sig={timeResult.Signature}");

            var chunkResult = await DummyProtocolClient.SendChunkRequestAsync(host, port);
            Console.WriteLine($"[DummyClient] Sent {chunkResult.MessageType} ({chunkResult.PayloadSize} bytes) sig={chunkResult.Signature}");
        }
    }
}
