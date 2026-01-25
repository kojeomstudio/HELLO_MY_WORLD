using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Google.Protobuf;
using GameServerApp.Configuration;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;

namespace GameServerApp.Network
{
    /// <summary>
    /// Central protobuf packet gateway that enforces descriptor validation,
    /// size limits, and optional compression/encryption based on JSON config.
    /// </summary>
    public sealed class EnhancedProtocolHandler
    {
        private readonly NetworkConfiguration _networkConfig;
        private readonly ProtocolStatistics _stats = new();

        public EnhancedProtocolHandler(DataDrivenConfigManager configManager)
        {
            if (configManager == null) throw new ArgumentNullException(nameof(configManager));

            _networkConfig = configManager.GetConfiguration<NetworkConfiguration>();
            ProtocolRegistry.ValidateBindings();
            ProtoRuntime.EnsureInitialized();
            ProtocolValidator.ValidateEnhancedContracts();
            ProtoFingerprint.AssertDescriptorFingerprint();
            ProtoDiagnostics.AssertRegistryClean();
            ProtoDiagnostics.LogSummary();
            ProtocolStandardization.ValidateProtocolImplementation();
        }

        public ProtocolStatistics Statistics => _stats;

        public bool TryDeserialize(MinecraftMessageType messageType, byte[] payload, out IMessage? message)
        {
            message = null;
            if (!ValidateSize(payload))
            {
                return false;
            }

            if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype))
            {
                ProtoDiagnostics.LogMissingBinding(messageType);
                return false;
            }

            try
            {
                var buffer = MaybeDecompress(payload);
                message = prototype.Descriptor.Parser.ParseFrom(buffer);
                BumpRx(buffer.Length, messageType);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Protocol] Failed to parse {messageType}: {ex.Message}");
                return false;
            }
        }

        public bool TrySerialize(MinecraftMessageType messageType, IMessage message, out byte[] payload)
        {
            payload = Array.Empty<byte>();

            if (message == null)
            {
                return false;
            }

            try
            {
                ProtocolRegistry.EnsureRegistered(messageType);
                var raw = message.ToByteArray();
                var framed = MaybeCompress(raw);

                if (!ValidateSize(framed))
                {
                    return false;
                }

                payload = framed;
                BumpTx(framed.Length, messageType);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Protocol] Failed to serialize {messageType}: {ex.Message}");
                return false;
            }
        }

        private bool ValidateSize(ReadOnlySpan<byte> payload)
        {
            if (payload.Length > _networkConfig.ConnectionSettings.MaxPacketSize)
            {
                Console.WriteLine($"[Protocol] Packet dropped: {payload.Length} bytes exceeds max {_networkConfig.ConnectionSettings.MaxPacketSize}");
                return false;
            }

            return true;
        }

        private byte[] MaybeCompress(byte[] payload)
        {
            if (!_networkConfig.ConnectionSettings.EnableCompression ||
                payload.Length < _networkConfig.ConnectionSettings.CompressionThreshold)
            {
                return payload;
            }

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
            {
                gzip.Write(payload, 0, payload.Length);
            }
            return output.ToArray();
        }

        private byte[] MaybeDecompress(byte[] payload)
        {
            if (!_networkConfig.ConnectionSettings.EnableCompression)
            {
                return payload;
            }

            try
            {
                using var input = new MemoryStream(payload);
                using var gzip = new GZipStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                gzip.CopyTo(output);
                return output.ToArray();
            }
            catch
            {
                // If payload is not compressed, fall back to raw bytes.
                return payload;
            }
        }

        private void BumpTx(int byteCount, MinecraftMessageType messageType)
        {
            _stats.TotalPacketsSent++;
            _stats.TotalBytesSent += byteCount;
            _stats.PacketTypeCounts.TryGetValue(messageType, out var count);
            _stats.PacketTypeCounts[messageType] = count + 1;
        }

        private void BumpRx(int byteCount, MinecraftMessageType messageType)
        {
            _stats.TotalPacketsReceived++;
            _stats.TotalBytesReceived += byteCount;
            _stats.PacketTypeCounts.TryGetValue(messageType, out var count);
            _stats.PacketTypeCounts[messageType] = count + 1;
        }
    }

    public sealed class ProtocolStatistics
    {
        public long TotalPacketsReceived { get; set; }
        public long TotalPacketsSent { get; set; }
        public long TotalBytesReceived { get; set; }
        public long TotalBytesSent { get; set; }
        public Dictionary<MinecraftMessageType, long> PacketTypeCounts { get; } = new();
    }
}
