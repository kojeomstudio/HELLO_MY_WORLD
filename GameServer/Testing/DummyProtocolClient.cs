using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GameCommon.World;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;

namespace GameServerApp.Testing
{
    public sealed record ProtoProbeResult(
        bool RoundTripOk,
        string DescriptorName,
        bool NetworkProbeAttempted,
        bool NetworkProbeOk,
        string NetworkError,
        IReadOnlyCollection<string> ValidatedPackets,
        IReadOnlyCollection<string> MissingRequiredPackets,
        IReadOnlyCollection<string> MissingPrototypePackets,
        IReadOnlyCollection<string> OptionalUnregistered,
        IReadOnlyCollection<string> RegisteredPackets,
        string DescriptorFingerprint,
        string HydrologySignature,
        int RegisteredCount,
        int GeneratedDescriptorCount,
        int BoundDescriptorCount,
        IReadOnlyCollection<string> UnboundGeneratedDescriptors,
        string ReportPath,
        string ReferenceReportPath,
        string ProfileHash,
        int ProfileVersion,
        string ProfilePath);

    public sealed class DummyProtocolClientSettings
    {
        public string Host { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 9000;
        public int ConnectTimeoutMs { get; set; } = 750;
        public int ReceiveTimeoutMs { get; set; } = 750;
        public int RoundTripCount { get; set; } = 1;
        public bool ProbeNetwork { get; set; } = false;
        public bool ValidateAllKnownPackets { get; set; } = true;
        public bool IncludeOptionalMessages { get; set; } = false;
        public string? OutputReportPath { get; set; } = "reports/proto_probe_report.json";
        public string? ReferenceReportPath { get; set; } = "config/proto_reference_report.json";
        public string? WorldMapControlProfilePath { get; set; } = "config/world_map_control_profile.json";
        public string[] Packets { get; set; } = new[] { "ChunkDataRequest", "ChunkUnloadNotification", "TimeUpdate" };

        public static DummyProtocolClientSettings Load(string path)
        {
            if (!File.Exists(path))
            {
                return new DummyProtocolClientSettings();
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<DummyProtocolClientSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            }) ?? new DummyProtocolClientSettings();
        }
    }

    /// <summary>
    /// Lightweight dummy client for exercising protobuf packet encode/decode and optional TCP probes.
    /// Does not assume a full login pipeline; it only verifies registry wiring and basic round-trip.
    /// </summary>
    public sealed class DummyProtocolClient
    {
        private readonly DummyProtocolClientSettings settings;

        public DummyProtocolClient(DummyProtocolClientSettings settings)
        {
            this.settings = settings;
        }

        public DummyProtocolClientSettings Settings => settings;

        public static DummyProtocolClient CreateFromConfig(string path) =>
            new DummyProtocolClient(DummyProtocolClientSettings.Load(path));

        public async Task<ProtoProbeResult> RunAsync(bool probeNetwork, CancellationToken cancellationToken)
        {
            ProtocolRegistry.ValidateBindings();
            ProtoDiagnostics.AssertFingerprint();
            ProtoDiagnostics.AssertRegistryClean();
            WorldMapControlProfile? sharedProfile = null;
            string profilePath = string.IsNullOrWhiteSpace(settings.WorldMapControlProfilePath)
                ? string.Empty
                : Path.GetFullPath(settings.WorldMapControlProfilePath);
            if (!string.IsNullOrWhiteSpace(profilePath))
            {
                sharedProfile = WorldMapControlProfileUtility.Load(profilePath);
                if (sharedProfile != null && string.IsNullOrWhiteSpace(sharedProfile.ProfileHash))
                {
                    sharedProfile.ProfileHash = WorldMapControlProfileUtility.ComputeHash(sharedProfile);
                }

                if (sharedProfile != null &&
                    !string.Equals(sharedProfile.HydrologySignature, SharedFeatureCatalog.HydrologySignature, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Hydrology signature mismatch between profile ({sharedProfile.HydrologySignature}) and shared catalog ({SharedFeatureCatalog.HydrologySignature}).");
                }
            }

            var registeredPackets = ProtocolRegistry.RegisteredMessageTypes
                .Select(type => type.ToString())
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string descriptorFingerprint = ProtoFingerprint.ComputeFingerprint();
            probeNetwork |= settings.ProbeNetwork;
            var validatedPackets = new List<string>();
            var requiredProbeMissing = new List<string>();
            var optionalProbeMissing = new List<string>();
            var missingPrototypePackets = new List<string>();
            var packetsToProbe = new HashSet<MinecraftMessageType>();

            if (settings.ValidateAllKnownPackets)
            {
                packetsToProbe.UnionWith(ProtocolRegistry.RegisteredMessageTypes);
                if (settings.IncludeOptionalMessages)
                {
                    packetsToProbe.UnionWith(ProtocolRegistry.GetOptionalMessagesWithoutBindings());
                }
            }

            foreach (var packetName in settings.Packets ?? Array.Empty<string>())
            {
                if (!Enum.TryParse(packetName, ignoreCase: true, out MinecraftMessageType messageType))
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Unknown packet '{packetName}' in config. Skipping.");
                    continue;
                }

                packetsToProbe.Add(messageType);
            }

            foreach (var messageType in packetsToProbe)
            {
                if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype) || prototype == null)
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Missing prototype for '{messageType}'. Regenerate protobuf DTOs or update ProtocolRegistry bindings.");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (ProtocolRegistry.IsOptionalMessageType(messageType))
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }
                    continue;
                }

                var descriptorParser = prototype.Descriptor?.Parser;
                if (descriptorParser == null)
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Descriptor parser missing for '{messageType}'.");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (ProtocolRegistry.IsOptionalMessageType(messageType))
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }
                    continue;
                }

                try
                {
                    var bytes = prototype.ToByteArray();
                    var parsed = descriptorParser.ParseFrom(bytes);
                    if (parsed != null)
                    {
                        validatedPackets.Add(messageType.ToString());
                    }
                    else
                    {
                        missingPrototypePackets.Add(messageType.ToString());
                        if (ProtocolRegistry.IsOptionalMessageType(messageType))
                        {
                            optionalProbeMissing.Add(messageType.ToString());
                        }
                        else
                        {
                            requiredProbeMissing.Add(messageType.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Round-trip failed for '{messageType}': {ex.Message}");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (ProtocolRegistry.IsOptionalMessageType(messageType))
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }
                }
            }

            var sampleRequest = new ChunkLoadRequest
            {
                ViewDistance = Math.Max(1, settings.RoundTripCount)
            };
            sampleRequest.ChunkPositions.Add(new global::MinecraftGame.Common.Vector3Int
            {
                X = 0,
                Y = 0,
                Z = 0
            });

            byte[] payload = sampleRequest.ToByteArray();
            bool roundTripOk = ChunkLoadRequest.Parser.ParseFrom(payload) != null;
            if (roundTripOk)
            {
                validatedPackets.Add(nameof(ChunkLoadRequest));
            }

            bool networkAttempted = false;
            bool networkOk = false;
            string networkError = string.Empty;

            if (probeNetwork)
            {
                networkAttempted = true;
                try
                {
                    using var client = new TcpClient();
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(settings.ConnectTimeoutMs);

                    await client.ConnectAsync(settings.Host, settings.Port, cts.Token);
                    client.ReceiveTimeout = settings.ReceiveTimeoutMs;
                    client.SendTimeout = settings.ConnectTimeoutMs;

                    await using NetworkStream stream = client.GetStream();
                    for (int i = 0; i < Math.Max(1, settings.RoundTripCount); i++)
                    {
                        byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(payload.Length));
                        await stream.WriteAsync(lengthPrefix, cancellationToken);
                        await stream.WriteAsync(payload, cancellationToken);
                    }

                    networkOk = true;
                }
                catch (Exception ex)
                {
                    networkError = ex.Message;
                }
            }

            var requiredMissing = ProtocolRegistry.GetUnregisteredRequiredMessages()
                .Select(type => type.ToString())
                .ToList();
            requiredMissing.AddRange(requiredProbeMissing);
            requiredMissing = requiredMissing
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var optionalMissing = ProtocolRegistry.GetOptionalMessagesWithoutBindings()
                .Select(type => type.ToString())
                .ToList();
            optionalMissing.AddRange(optionalProbeMissing);
            optionalMissing = optionalMissing
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            missingPrototypePackets = missingPrototypePackets
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (requiredMissing.Count > 0)
            {
                Console.WriteLine($"[ProtoProbe][WARN] Required protocol bindings missing: {string.Join(", ", requiredMissing)}");
            }

            if (optionalMissing.Count > 0)
            {
                Console.WriteLine($"[ProtoProbe][INFO] Optional protocol bindings not registered: {string.Join(", ", optionalMissing)}");
            }
            if (missingPrototypePackets.Count > 0)
            {
                Console.WriteLine($"[ProtoProbe][WARN] Prototype resolution failed for: {string.Join(", ", missingPrototypePackets)}");
            }
            var reportPath = string.IsNullOrWhiteSpace(settings.OutputReportPath)
                ? string.Empty
                : Path.GetFullPath(settings.OutputReportPath);
            var referenceReportPath = string.IsNullOrWhiteSpace(settings.ReferenceReportPath)
                ? string.Empty
                : Path.GetFullPath(settings.ReferenceReportPath);
            int registeredCount = registeredPackets.Length;
            string hydrologySignature = SharedFeatureCatalog.HydrologySignature;
            string profileHash = sharedProfile?.ProfileHash ?? string.Empty;
            int profileVersion = sharedProfile?.Version ?? 0;
            var coverage = ProtocolRegistry.GetBindingCoverage();
            var unboundGeneratedDescriptors = ProtocolRegistry.GetGeneratedDescriptorsWithoutBindings();

            var result = new ProtoProbeResult(
                roundTripOk,
                ChunkLoadRequest.Descriptor.FullName,
                networkAttempted,
                networkOk,
                networkError,
                validatedPackets.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                requiredMissing,
                missingPrototypePackets,
                optionalMissing,
                registeredPackets,
                descriptorFingerprint,
                hydrologySignature,
                registeredCount,
                coverage.GeneratedDescriptors,
                coverage.BoundDescriptors,
                unboundGeneratedDescriptors,
                reportPath,
                referenceReportPath,
                profileHash,
                profileVersion,
                profilePath);

            Console.WriteLine(
                $"[ProtoProbe] Hydrology={hydrologySignature} Registered={registeredCount} " +
                $"Validated={validatedPackets.Count} Missing={requiredMissing.Count} MissingPrototype={missingPrototypePackets.Count} OptionalMissing={optionalMissing.Count} " +
                $"Coverage={coverage.BoundDescriptors}/{coverage.GeneratedDescriptors} UnboundGenerated={unboundGeneratedDescriptors.Count} " +
                $"ProfileV={profileVersion} ProfileHash={(string.IsNullOrWhiteSpace(profileHash) ? "<none>" : profileHash[..Math.Min(8, profileHash.Length)])} " +
                $"DescriptorFingerprint={descriptorFingerprint}");

            if (!string.IsNullOrWhiteSpace(reportPath))
            {
                try
                {
                    var directory = Path.GetDirectoryName(reportPath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(reportPath, JsonSerializer.Serialize(result, options));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Failed to write probe report to '{reportPath}': {ex.Message}");
                }
            }

            if (!string.IsNullOrWhiteSpace(referenceReportPath))
            {
                try
                {
                    var directory = Path.GetDirectoryName(referenceReportPath);
                    if (!string.IsNullOrWhiteSpace(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    ProtoDiagnostics.WriteReportToFile(referenceReportPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Failed to write proto reference report to '{referenceReportPath}': {ex.Message}");
                }
            }

            return result;
        }
    }
}
