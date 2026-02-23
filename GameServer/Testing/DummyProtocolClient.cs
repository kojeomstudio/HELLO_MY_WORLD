using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EnhancedMinecraftProtocol;
using GameCommon.World;
using Google.Protobuf;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;

namespace GameServerApp.Testing
{
    public sealed class DummyProtocolProbeSettings
    {
        public string Host { get; set; } = "127.0.0.1";

        public int Port { get; set; } = 9000;

        public int ConnectTimeoutMs { get; set; } = 750;

        public int ReceiveTimeoutMs { get; set; } = 750;

        public int RoundTripCount { get; set; } = 3;

        public int MaxNetworkProbePackets { get; set; } = 4;

        public bool ProbeNetwork { get; set; } = false;

        public bool ValidateAllKnownPackets { get; set; } = true;

        public bool IncludeOptionalMessages { get; set; } = true;

        public bool RequireRequiredPacketCoverage { get; set; } = true;

        public bool FailOnHydrologySignatureMismatch { get; set; } = true;

        public int MinMapControlProfileVersion { get; set; } = 53;

        public bool FailOnMapControlVersionRegression { get; set; } = true;

        public bool FailOnRequiredTypeDrift { get; set; } = true;

        public string OutputReportPath { get; set; } = "reports/proto_probe_report.json";

        public string ReferenceReportPath { get; set; } = "config/proto_reference_report.json";

        public string WorldMapControlProfilePath { get; set; } = "config/world_map_control_profile.json";

        public List<string> Packets { get; set; } = new()
        {
            "PlayerStateUpdate",
            "PlayerActionRequest",
            "PlayerActionResponse",
            "ChunkDataRequest",
            "ChunkDataResponse",
            "BlockChangeNotification",
            "ChunkUnloadNotification",
            "ChunkUnloadAcknowledge",
            "TimeUpdate",
            "WeatherChange",
            "SoundEffect",
            "ParticleEffect",
            "EntitySpawn",
            "EntityDespawn"
        };

        public static DummyProtocolProbeSettings Load(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return new DummyProtocolProbeSettings();
                }

                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<DummyProtocolProbeSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                return loaded ?? new DummyProtocolProbeSettings();
            }
            catch
            {
                return new DummyProtocolProbeSettings();
            }
        }
    }

    public sealed class DummyProtocolProbeResult
    {
        public bool RoundTripOk { get; set; }

        public string DescriptorName { get; set; } = string.Empty;

        public List<string> ValidatedPackets { get; } = new();

        public List<string> MissingRequiredPackets { get; } = new();

        public List<string> MissingPrototypePackets { get; } = new();

        public List<string> OptionalUnregistered { get; } = new();

        public string ReportPath { get; set; } = string.Empty;

        public string ReferenceReportPath { get; set; } = string.Empty;

        public bool NetworkProbeAttempted { get; set; }

        public bool NetworkProbeOk { get; set; }

        public string NetworkError { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dummy protocol probe client used by GameServer Program --proto-probe path.
    /// Performs protobuf round-trip and optional network smoke checks.
    /// </summary>
    public sealed class DummyProtocolClient
    {
        public DummyProtocolProbeSettings Settings { get; }

        private DummyProtocolClient(DummyProtocolProbeSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public static DummyProtocolClient CreateFromConfig(string settingsPath)
        {
            return new DummyProtocolClient(DummyProtocolProbeSettings.Load(settingsPath));
        }

        public async Task<DummyProtocolProbeResult> RunAsync(bool probeNetwork, CancellationToken cancellationToken)
        {
            var result = new DummyProtocolProbeResult
            {
                DescriptorName = EnhancedMinecraftGameReflection.Descriptor?.Name ?? string.Empty
            };

            ProtoRuntime.EnsureInitialized();
            ProtoFingerprint.AssertDescriptorFingerprint();
            ProtocolRegistry.ValidateBindings();
            ProtocolValidator.ValidateEnhancedContracts();
            ProtocolStandardization.ValidateProtocolImplementation();

            ValidateProfileGuards();

            var missingRequired = ProtocolRegistry.GetUnregisteredRequiredMessages()
                .Select(item => item.ToString())
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToList();
            result.MissingRequiredPackets.AddRange(missingRequired);

            result.OptionalUnregistered.AddRange(
                ProtocolRegistry.GetOptionalMessagesWithoutBindings()
                    .Select(item => item.ToString())
                    .OrderBy(item => item, StringComparer.Ordinal));

            if (Settings.FailOnRequiredTypeDrift)
            {
                var drift = ProtocolRegistry.BuildTypeConsistencyDiagnostics()
                    .Where(item => item.HasEnhancedType && item.HasLegacyType && !item.LegacyTypeMatches)
                    .ToArray();
                if (drift.Length > 0)
                {
                    throw new InvalidOperationException(
                        "Protocol type drift detected: " +
                        string.Join(", ", drift.Select(item => $"{item.MessageType}(legacy={item.LegacyClrType}, enhanced={item.EnhancedClrType})")));
                }
            }

            var packetTypes = ResolvePacketTypes();
            if (Settings.RequireRequiredPacketCoverage)
            {
                var missingCoverage = ProtocolRegistry.GetMissingRequiredInSelection(packetTypes)
                    .Select(item => item.ToString())
                    .OrderBy(item => item, StringComparer.Ordinal)
                    .ToArray();
                if (missingCoverage.Length > 0)
                {
                    throw new InvalidOperationException(
                        "Required packet coverage is incomplete for proto probe: " +
                        string.Join(", ", missingCoverage));
                }
            }

            int requiredTargets = packetTypes.Count(type => !ProtocolRegistry.IsOptionalMessageType(type));
            int requiredRoundTripOk = 0;
            var payloads = new List<(MinecraftMessageType Type, byte[] Payload)>();

            foreach (var packetType in packetTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!ProtocolRegistry.TryCreatePrototype(packetType, out IMessage prototype) || prototype == null)
                {
                    result.MissingPrototypePackets.Add(packetType.ToString());
                    continue;
                }

                try
                {
                    var descriptor = prototype.Descriptor;
                    if (descriptor == null)
                    {
                        result.MissingPrototypePackets.Add(packetType.ToString());
                        continue;
                    }

                    var parser = descriptor.Parser;
                    if (parser == null)
                    {
                        result.MissingPrototypePackets.Add(packetType.ToString());
                        continue;
                    }

                    byte[] payload = prototype.ToByteArray();
                    var parsed = parser.ParseFrom(payload);
                    if (!string.Equals(parsed?.Descriptor?.FullName, descriptor.FullName, StringComparison.Ordinal))
                    {
                        result.MissingPrototypePackets.Add(packetType.ToString());
                        continue;
                    }

                    result.ValidatedPackets.Add(packetType.ToString());
                    payloads.Add((packetType, payload));
                    if (!ProtocolRegistry.IsOptionalMessageType(packetType))
                    {
                        requiredRoundTripOk++;
                    }
                }
                catch
                {
                    result.MissingPrototypePackets.Add(packetType.ToString());
                }
            }

            result.RoundTripOk = requiredRoundTripOk >= requiredTargets;

            string reportPath = ResolvePath(Settings.OutputReportPath);
            WriteProbeReport(reportPath, result, packetTypes, requiredTargets, requiredRoundTripOk);
            result.ReportPath = reportPath;

            string referencePath = ResolvePath(Settings.ReferenceReportPath);
            ProtoDiagnostics.WriteReportToFile(referencePath);
            result.ReferenceReportPath = referencePath;

            bool shouldProbeNetwork = probeNetwork || Settings.ProbeNetwork;
            result.NetworkProbeAttempted = shouldProbeNetwork;
            if (shouldProbeNetwork)
            {
                var (ok, error) = await ProbeNetworkAsync(payloads, cancellationToken).ConfigureAwait(false);
                result.NetworkProbeOk = ok;
                result.NetworkError = error;
            }

            return result;
        }

        private void ValidateProfileGuards()
        {
            if (string.IsNullOrWhiteSpace(Settings.WorldMapControlProfilePath))
            {
                return;
            }

            string resolvedProfilePath = ResolvePath(Settings.WorldMapControlProfilePath);
            if (!File.Exists(resolvedProfilePath))
            {
                return;
            }

            var profile = WorldMapControlProfileUtility.Load(resolvedProfilePath);
            if (profile == null)
            {
                return;
            }

            if (Settings.FailOnHydrologySignatureMismatch &&
                !string.Equals(profile.HydrologySignature, SharedFeatureCatalog.HydrologySignature, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Hydrology signature mismatch (profile={profile.HydrologySignature}, shared={SharedFeatureCatalog.HydrologySignature}).");
            }

            if (Settings.FailOnMapControlVersionRegression &&
                profile.Version < Math.Max(1, Settings.MinMapControlProfileVersion))
            {
                throw new InvalidOperationException(
                    $"Map control profile version regression (profile={profile.Version}, required={Settings.MinMapControlProfileVersion}).");
            }
        }

        private List<MinecraftMessageType> ResolvePacketTypes()
        {
            var packets = new List<MinecraftMessageType>();

            if (Settings.ValidateAllKnownPackets)
            {
                var all = Enum.GetValues(typeof(MinecraftMessageType))
                    .Cast<MinecraftMessageType>();
                packets.AddRange(all);
            }
            else
            {
                foreach (var packetName in Settings.Packets)
                {
                    if (Enum.TryParse(packetName, ignoreCase: true, out MinecraftMessageType parsed))
                    {
                        packets.Add(parsed);
                    }
                }
            }

            if (!Settings.IncludeOptionalMessages)
            {
                packets = packets
                    .Where(type => !ProtocolRegistry.IsOptionalMessageType(type))
                    .ToList();
            }

            if (packets.Count == 0)
            {
                packets.AddRange(ProtocolRegistry.RegisteredMessageTypes);
            }

            return packets
                .Distinct()
                .OrderBy(type => (int)type)
                .ToList();
        }

        private async Task<(bool Ok, string Error)> ProbeNetworkAsync(
            IReadOnlyList<(MinecraftMessageType Type, byte[] Payload)> payloads,
            CancellationToken cancellationToken)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(Settings.Host, Settings.Port);
                var timeoutTask = Task.Delay(Math.Max(100, Settings.ConnectTimeoutMs), cancellationToken);
                var completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                if (completed == timeoutTask)
                {
                    return (false, "Connect timeout");
                }

                await connectTask.ConfigureAwait(false);
                using var stream = client.GetStream();
                stream.ReadTimeout = Math.Max(100, Settings.ReceiveTimeoutMs);
                stream.WriteTimeout = Math.Max(100, Settings.ReceiveTimeoutMs);

                int sendCount = Math.Min(Math.Max(1, Settings.MaxNetworkProbePackets), payloads.Count);
                for (int i = 0; i < sendCount; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var packet = payloads[i];
                    await WritePacketAsync(stream, packet.Type, packet.Payload, cancellationToken).ConfigureAwait(false);
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static async Task WritePacketAsync(
            NetworkStream stream,
            MinecraftMessageType messageType,
            byte[] payload,
            CancellationToken cancellationToken)
        {
            int bodyLength = Math.Max(0, payload?.Length ?? 0) + sizeof(int);
            byte[] lengthBytes = BitConverter.GetBytes(bodyLength);
            byte[] typeBytes = BitConverter.GetBytes((int)messageType);

            await stream.WriteAsync(lengthBytes.AsMemory(0, lengthBytes.Length), cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(typeBytes.AsMemory(0, typeBytes.Length), cancellationToken).ConfigureAwait(false);
            if (payload != null && payload.Length > 0)
            {
                await stream.WriteAsync(payload.AsMemory(0, payload.Length), cancellationToken).ConfigureAwait(false);
            }

            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private void WriteProbeReport(
            string outputPath,
            DummyProtocolProbeResult result,
            IReadOnlyCollection<MinecraftMessageType> packetTypes,
            int requiredTargets,
            int requiredRoundTripOk)
        {
            var payload = new
            {
                timestampUtc = DateTime.UtcNow.ToString("o"),
                descriptor = result.DescriptorName,
                settings = new
                {
                    Settings.Host,
                    Settings.Port,
                    Settings.RoundTripCount,
                    Settings.MaxNetworkProbePackets,
                    Settings.ValidateAllKnownPackets,
                    Settings.IncludeOptionalMessages,
                    Settings.RequireRequiredPacketCoverage,
                    Settings.MinMapControlProfileVersion,
                    Settings.ProbeNetwork
                },
                totals = new
                {
                    packetsRequested = packetTypes.Count,
                    requiredTargets,
                    requiredRoundTripOk,
                    validated = result.ValidatedPackets.Count,
                    missingRequired = result.MissingRequiredPackets.Count,
                    missingPrototype = result.MissingPrototypePackets.Count,
                    optionalUnregistered = result.OptionalUnregistered.Count,
                    result.RoundTripOk,
                    result.NetworkProbeAttempted,
                    result.NetworkProbeOk
                },
                validatedPackets = result.ValidatedPackets,
                missingRequiredPackets = result.MissingRequiredPackets,
                missingPrototypePackets = result.MissingPrototypePackets,
                optionalUnregistered = result.OptionalUnregistered,
                networkError = result.NetworkError
            };

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(outputPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }
    }
}
