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
using ProtoBuf;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;

namespace GameServerApp.Testing
{
    public sealed class DummyProtocolProbeSettings
    {
        private static readonly string[] DefaultPackets =
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

        public int MinMapControlProfileVersion { get; set; } = SharedFeatureCatalog.MapControlProfileVersion;

        public bool FailOnMapControlVersionRegression { get; set; } = true;

        public bool FailOnRequiredTypeDrift { get; set; } = true;

        public bool FailOnReferenceReportDrift { get; set; } = true;

        public bool FailOnDescriptorCoverageRegression { get; set; } = true;

        public double MinDescriptorCoverageRatio { get; set; } = 0.25;

        public bool FailOnGeneratedRequiredDescriptorGap { get; set; } = true;

        public bool FailOnGeneratedSourceTimestampDrift { get; set; } = true;

        public string ProtoSourceDirectory { get; set; } = "proto";

        public string GeneratedProtobufDirectory { get; set; } = "Assets/Generated/Protobuf";

        public string OutputReportPath { get; set; } = "reports/proto_probe_report.json";

        public string ReferenceReportPath { get; set; } = "config/proto_reference_report.json";

        public string WorldMapControlProfilePath { get; set; } = "config/world_map_control_profile.json";

        public List<string> Packets { get; set; } = new(DefaultPackets);

        public void Normalize()
        {
            Host = string.IsNullOrWhiteSpace(Host) ? "127.0.0.1" : Host.Trim();
            Port = Math.Clamp(Port <= 0 ? 9000 : Port, 1, 65535);
            ConnectTimeoutMs = Math.Clamp(ConnectTimeoutMs <= 0 ? 750 : ConnectTimeoutMs, 100, 120000);
            ReceiveTimeoutMs = Math.Clamp(ReceiveTimeoutMs <= 0 ? 750 : ReceiveTimeoutMs, 100, 120000);
            RoundTripCount = Math.Clamp(RoundTripCount <= 0 ? 3 : RoundTripCount, 1, 64);
            MaxNetworkProbePackets = Math.Clamp(MaxNetworkProbePackets <= 0 ? 4 : MaxNetworkProbePackets, 1, 128);
            MinMapControlProfileVersion = Math.Max(
                SharedFeatureCatalog.MapControlProfileVersion,
                Math.Max(1, MinMapControlProfileVersion));
            MinDescriptorCoverageRatio = Math.Clamp(MinDescriptorCoverageRatio, 0.0, 1.0);
            ProtoSourceDirectory = string.IsNullOrWhiteSpace(ProtoSourceDirectory)
                ? "proto"
                : ProtoSourceDirectory;
            GeneratedProtobufDirectory = string.IsNullOrWhiteSpace(GeneratedProtobufDirectory)
                ? "Assets/Generated/Protobuf"
                : GeneratedProtobufDirectory;
            OutputReportPath = string.IsNullOrWhiteSpace(OutputReportPath)
                ? "reports/proto_probe_report.json"
                : OutputReportPath;
            ReferenceReportPath = string.IsNullOrWhiteSpace(ReferenceReportPath)
                ? "config/proto_reference_report.json"
                : ReferenceReportPath;
            WorldMapControlProfilePath = string.IsNullOrWhiteSpace(WorldMapControlProfilePath)
                ? "config/world_map_control_profile.json"
                : WorldMapControlProfilePath;

            Packets = (Packets ?? new List<string>())
                .Where(packet => !string.IsNullOrWhiteSpace(packet))
                .Select(packet => packet.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (Packets.Count == 0)
            {
                Packets.AddRange(DefaultPackets);
            }
        }

        public static DummyProtocolProbeSettings Load(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    var defaults = new DummyProtocolProbeSettings();
                    defaults.Normalize();
                    return defaults;
                }

                var json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<DummyProtocolProbeSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

                loaded ??= new DummyProtocolProbeSettings();
                loaded.Normalize();
                return loaded;
            }
            catch
            {
                var defaults = new DummyProtocolProbeSettings();
                defaults.Normalize();
                return defaults;
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

        public List<string> LegacyFallbackValidatedPackets { get; } = new();

        public double DescriptorCoverageRatio { get; set; }

        public List<string> MissingGeneratedRequiredDescriptors { get; } = new();

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
        private readonly string _settingsBaseDirectory;

        private DummyProtocolClient(DummyProtocolProbeSettings settings, string settingsBaseDirectory)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settingsBaseDirectory = string.IsNullOrWhiteSpace(settingsBaseDirectory)
                ? Directory.GetCurrentDirectory()
                : settingsBaseDirectory;
        }

        public static DummyProtocolClient CreateFromConfig(string settingsPath)
        {
            string resolvedSettingsPath = string.IsNullOrWhiteSpace(settingsPath)
                ? string.Empty
                : Path.GetFullPath(settingsPath);
            string settingsBaseDirectory = string.IsNullOrWhiteSpace(resolvedSettingsPath)
                ? Directory.GetCurrentDirectory()
                : Path.GetDirectoryName(resolvedSettingsPath) ?? Directory.GetCurrentDirectory();
            var settings = DummyProtocolProbeSettings.Load(resolvedSettingsPath);
            return new DummyProtocolClient(settings, settingsBaseDirectory);
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
            ValidateGeneratedProtobufFreshnessGuard();
            ValidateReferenceReportGuard();
            var (boundDescriptors, generatedDescriptors) = ProtocolRegistry.GetBindingCoverage();
            result.DescriptorCoverageRatio = generatedDescriptors > 0
                ? boundDescriptors / (double)generatedDescriptors
                : 1.0;
            double minCoverageRatio = Math.Clamp(Settings.MinDescriptorCoverageRatio, 0.0, 1.0);
            if (Settings.FailOnDescriptorCoverageRegression && result.DescriptorCoverageRatio < minCoverageRatio)
            {
                throw new InvalidOperationException(
                    $"Proto descriptor coverage regression detected (coverage={result.DescriptorCoverageRatio:F3}, required={minCoverageRatio:F3}, bound={boundDescriptors}, generated={generatedDescriptors}).");
            }

            var requiredDescriptorGap = ProtocolRegistry.GetGeneratedRequiredDescriptorsWithoutBindings()
                .OrderBy(item => item, StringComparer.Ordinal)
                .ToArray();
            result.MissingGeneratedRequiredDescriptors.AddRange(requiredDescriptorGap);
            if (Settings.FailOnGeneratedRequiredDescriptorGap && requiredDescriptorGap.Length > 0)
            {
                throw new InvalidOperationException(
                    "Generated required descriptor bindings are missing: " +
                    string.Join(", ", requiredDescriptorGap));
            }

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
                    if (TryCreateLegacyOptionalPayload(packetType, out byte[] legacyPayload))
                    {
                        result.ValidatedPackets.Add(packetType.ToString());
                        result.LegacyFallbackValidatedPackets.Add(packetType.ToString());
                        payloads.Add((packetType, legacyPayload));
                    }
                    else
                    {
                        result.MissingPrototypePackets.Add(packetType.ToString());
                    }

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

                    int roundTripCount = Math.Max(1, Settings.RoundTripCount);
                    byte[] payload = prototype.ToByteArray();
                    IMessage? parsed = null;
                    byte[] currentPayload = payload;
                    bool roundTripValid = true;

                    for (int round = 0; round < roundTripCount; round++)
                    {
                        parsed = parser.ParseFrom(currentPayload);
                        if (!string.Equals(parsed?.Descriptor?.FullName, descriptor.FullName, StringComparison.Ordinal))
                        {
                            roundTripValid = false;
                            break;
                        }

                        currentPayload = parsed.ToByteArray();
                    }

                    if (!roundTripValid || parsed == null)
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

        private static bool TryCreateLegacyOptionalPayload(MinecraftMessageType messageType, out byte[] payload)
        {
            payload = Array.Empty<byte>();

            if (!ProtocolRegistry.IsOptionalMessageType(messageType))
            {
                return false;
            }

            payload = messageType switch
            {
                MinecraftMessageType.InventoryUpdate => SerializeLegacyPayload(new InventoryUpdateBroadcast
                {
                    PlayerId = "dummy-player",
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    UpdatedSlots =
                    {
                        new InventorySlotData
                        {
                            SlotIndex = 0,
                            ItemId = "stone",
                            Amount = 1,
                            ItemData = "{}"
                        }
                    }
                }),
                MinecraftMessageType.EntityUpdate => SerializeLegacyPayload(new EntityUpdateMessage
                {
                    EntityId = "dummy-entity",
                    Position = new Vector3D(0, 64, 0),
                    Rotation = new Vector3D(0, 0, 0),
                    Velocity = new Vector3D(0, 0, 0),
                    Health = 20f,
                    UpdateFlags = new EntityUpdateFlags
                    {
                        PositionUpdated = true
                    }
                }),
                MinecraftMessageType.ContainerOpen => SerializeLegacyPayload(new ContainerOpenRequestMessage
                {
                    Position = new Vector3I(0, 64, 0),
                    ContainerType = ContainerType.Chest
                }),
                MinecraftMessageType.ContainerClose => SerializeLegacyPayload(new ContainerCloseRequestMessage
                {
                    ContainerId = 1
                }),
                MinecraftMessageType.ContainerUpdate => SerializeLegacyPayload(new ContainerUpdateRequestMessage
                {
                    ContainerId = 1,
                    ForceFullSync = false,
                    ClientSnapshotHash = string.Empty,
                    SlotUpdates =
                    {
                        new SlotUpdate
                        {
                            Slot = 0,
                            Item = new InventoryItemInfo
                            {
                                ItemId = 1,
                                ItemName = "stone",
                                Quantity = 1,
                                ItemType = SharedProtocol.ItemType.Block
                            },
                            ItemIdentifier = "stone"
                        }
                    }
                }),
                _ => Array.Empty<byte>()
            };

            return payload.Length > 0;
        }

        private static byte[] SerializeLegacyPayload<T>(T message) where T : class
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, message);
            return stream.ToArray();
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
                    Settings.FailOnReferenceReportDrift,
                    Settings.FailOnDescriptorCoverageRegression,
                    Settings.MinDescriptorCoverageRatio,
                    Settings.FailOnGeneratedRequiredDescriptorGap,
                    Settings.FailOnGeneratedSourceTimestampDrift,
                    Settings.ProtoSourceDirectory,
                    Settings.GeneratedProtobufDirectory,
                    Settings.ProbeNetwork
                },
                totals = new
                {
                    packetsRequested = packetTypes.Count,
                    requiredTargets,
                    requiredRoundTripOk,
                    validated = result.ValidatedPackets.Count,
                    legacyFallbackValidated = result.LegacyFallbackValidatedPackets.Count,
                    missingRequired = result.MissingRequiredPackets.Count,
                    missingPrototype = result.MissingPrototypePackets.Count,
                    optionalUnregistered = result.OptionalUnregistered.Count,
                    descriptorCoverageRatio = result.DescriptorCoverageRatio,
                    missingGeneratedRequiredDescriptors = result.MissingGeneratedRequiredDescriptors.Count,
                    result.RoundTripOk,
                    result.NetworkProbeAttempted,
                    result.NetworkProbeOk
                },
                validatedPackets = result.ValidatedPackets,
                legacyFallbackValidatedPackets = result.LegacyFallbackValidatedPackets,
                missingRequiredPackets = result.MissingRequiredPackets,
                missingPrototypePackets = result.MissingPrototypePackets,
                optionalUnregistered = result.OptionalUnregistered,
                missingGeneratedRequiredDescriptors = result.MissingGeneratedRequiredDescriptors,
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

        private string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string settingsRelativePath = Path.GetFullPath(Path.Combine(_settingsBaseDirectory, path));
            if (File.Exists(settingsRelativePath) || Directory.Exists(settingsRelativePath))
            {
                return settingsRelativePath;
            }

            return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
        }

        private void ValidateReferenceReportGuard()
        {
            if (!Settings.FailOnReferenceReportDrift || string.IsNullOrWhiteSpace(Settings.ReferenceReportPath))
            {
                return;
            }

            string resolvedReferencePath = ResolvePath(Settings.ReferenceReportPath);
            if (!File.Exists(resolvedReferencePath))
            {
                return;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };
                var json = File.ReadAllText(resolvedReferencePath);
                var reference = JsonSerializer.Deserialize<ProtoReferenceReportSnapshot>(json, options);
                if (reference == null)
                {
                    return;
                }

                string baselineFingerprint = ProtoFingerprint.DescriptorFingerprint;
                string computedFingerprint = ProtoFingerprint.ComputeFingerprint();

                if (!string.IsNullOrWhiteSpace(reference.DescriptorFingerprint) &&
                    !string.Equals(reference.DescriptorFingerprint, baselineFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Proto reference baseline fingerprint drift detected (reference={reference.DescriptorFingerprint}, runtime={baselineFingerprint}).");
                }

                if (!string.IsNullOrWhiteSpace(reference.ComputedFingerprint) &&
                    !string.Equals(reference.ComputedFingerprint, computedFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Proto reference computed fingerprint drift detected (reference={reference.ComputedFingerprint}, runtime={computedFingerprint}).");
                }

                if (reference.MissingRegistrations != null && reference.MissingRegistrations.Length > 0)
                {
                    throw new InvalidOperationException(
                        "Proto reference report contains missing registrations: " +
                        string.Join(", ", reference.MissingRegistrations));
                }

                if (reference.UnregisteredMessageTypes != null && reference.UnregisteredMessageTypes.Length > 0)
                {
                    throw new InvalidOperationException(
                        "Proto reference report contains unregistered enum message types: " +
                        string.Join(", ", reference.UnregisteredMessageTypes));
                }

                if (reference.Registered != null && reference.Registered.Length > 0)
                {
                    var referenceRegisteredTypes = new HashSet<string>(
                        reference.Registered
                            .Select(item => item.MessageType ?? string.Empty)
                            .Where(item => !string.IsNullOrWhiteSpace(item)),
                        StringComparer.Ordinal);
                    var runtimeRegisteredTypes = ProtocolRegistry.RegisteredMessageTypes
                        .Select(item => item.ToString())
                        .OrderBy(item => item, StringComparer.Ordinal)
                        .ToArray();
                    var missingRegisteredTypes = runtimeRegisteredTypes
                        .Where(item => !referenceRegisteredTypes.Contains(item))
                        .ToArray();
                    if (missingRegisteredTypes.Length > 0)
                    {
                        throw new InvalidOperationException(
                            "Proto reference report is missing runtime registered message types: " +
                            string.Join(", ", missingRegisteredTypes));
                    }
                }

                if (reference.DeclaredMessages != null && reference.DeclaredMessages.Length > 0)
                {
                    var declaredDescriptors = new HashSet<string>(
                        reference.DeclaredMessages.Where(item => !string.IsNullOrWhiteSpace(item)),
                        StringComparer.Ordinal);
                    var requiredDescriptorNames = ProtocolRegistry.GetBindingDiagnostics()
                        .Select(item => item.DescriptorName)
                        .Where(item => !string.IsNullOrWhiteSpace(item))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(item => item, StringComparer.Ordinal)
                        .ToArray();
                    var missingDeclaredDescriptors = requiredDescriptorNames
                        .Where(item => !declaredDescriptors.Contains(item))
                        .ToArray();
                    if (missingDeclaredDescriptors.Length > 0)
                    {
                        throw new InvalidOperationException(
                            "Proto reference report is missing declared descriptor names required by registry: " +
                            string.Join(", ", missingDeclaredDescriptors));
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to parse proto reference report '{resolvedReferencePath}': {ex.Message}", ex);
            }
        }

        private void ValidateGeneratedProtobufFreshnessGuard()
        {
            if (!Settings.FailOnGeneratedSourceTimestampDrift)
            {
                return;
            }

            string protoDirectory = ResolvePath(Settings.ProtoSourceDirectory);
            string generatedDirectory = ResolvePath(Settings.GeneratedProtobufDirectory);
            string[] expectedFiles = ProtoDiagnostics.BuildExpectedGeneratedFileNames(
                protoDirectory,
                new[] { "Common.cs", "EnhancedMinecraftGame.cs", "GameAuth.cs" });
            ProtoDiagnostics.AssertGeneratedSourceFreshness(
                protoDirectory,
                generatedDirectory,
                expectedFiles);
        }

        private sealed class ProtoReferenceReportSnapshot
        {
            public string DescriptorFingerprint { get; set; } = string.Empty;

            public string ComputedFingerprint { get; set; } = string.Empty;

            public string[] MissingRegistrations { get; set; } = Array.Empty<string>();

            public string[] UnregisteredMessageTypes { get; set; } = Array.Empty<string>();

            public string[] DeclaredMessages { get; set; } = Array.Empty<string>();

            public ProtoReferenceRegisteredSnapshot[] Registered { get; set; } = Array.Empty<ProtoReferenceRegisteredSnapshot>();
        }

        private sealed class ProtoReferenceRegisteredSnapshot
        {
            public string MessageType { get; set; } = string.Empty;

            public string PrototypeName { get; set; } = string.Empty;
        }
    }
}
