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
    public sealed record ProtoProbePacketDiagnostic(
        string MessageType,
        bool IsOptional,
        bool IsRegistered,
        bool PrototypeResolved,
        bool RoundTripOk,
        string DescriptorName,
        string DescriptorPackage,
        string ErrorMessage);

    public sealed record ProtoRegistryReferenceSummary(
        IReadOnlyCollection<string> GeneratedDescriptors,
        IReadOnlyCollection<string> RegisteredMessageTypes,
        IReadOnlyCollection<string> UnboundRequiredGeneratedDescriptors,
        IReadOnlyCollection<string> UnboundGeneratedDescriptors,
        IReadOnlyCollection<ProtocolBindingDiagnostic> BindingDiagnostics,
        IReadOnlyCollection<ProtocolTypeConsistencyDiagnostic> TypeConsistencyDiagnostics);

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
        string ProfileHydrologySignature,
        bool ProfileHydrologyMatchesShared,
        int RegisteredCount,
        int GeneratedDescriptorCount,
        int BoundDescriptorCount,
        int UnboundRequiredDescriptorCount,
        IReadOnlyCollection<string> UnboundGeneratedDescriptors,
        IReadOnlyCollection<string> UnboundRequiredGeneratedDescriptors,
        string ReportPath,
        string ReferenceReportPath,
        string ProfileHash,
        int ProfileVersion,
        string ProfilePath,
        ProtoRegistryReferenceSummary RegistryReferences,
        IReadOnlyCollection<ProtoProbePacketDiagnostic> PacketDiagnostics);

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
        public bool FailOnHydrologySignatureMismatch { get; set; } = true;
        public int MinMapControlProfileVersion { get; set; } = 46;
        public bool FailOnMapControlVersionRegression { get; set; } = true;
        public bool FailOnRequiredTypeDrift { get; set; } = true;
        public int MaxNetworkProbePackets { get; set; } = 4;
        public string? OutputReportPath { get; set; } = "reports/proto_probe_report.json";
        public string? ReferenceReportPath { get; set; } = "config/proto_reference_report.json";
        public string? WorldMapControlProfilePath { get; set; } = "config/world_map_control_profile.json";
        public string[] Packets { get; set; } = new[]
        {
            "PlayerStateUpdate",
            "PlayerActionRequest",
            "PlayerActionResponse",
            "ChunkDataRequest",
            "ChunkDataResponse",
            "ChunkUnloadNotification",
            "ChunkUnloadAcknowledge",
            "BlockChangeNotification",
            "TimeUpdate",
            "WeatherChange"
        };

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
            ProtocolValidator.ValidateEnhancedContracts();
            ProtoDiagnostics.AssertFingerprint();
            ProtoDiagnostics.AssertRegistryClean();
            bool profileHydrologyMatchesShared = true;
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
                    profileHydrologyMatchesShared = false;
                    Console.WriteLine($"[ProtoProbe][WARN] Hydrology signature mismatch between profile ({sharedProfile.HydrologySignature}) and shared catalog ({SharedFeatureCatalog.HydrologySignature}).");
                }

                if (sharedProfile != null && sharedProfile.Version <= 0)
                {
                    Console.WriteLine("[ProtoProbe][WARN] World-map control profile version is missing or invalid.");
                }

                if (sharedProfile != null &&
                    sharedProfile.Version < Math.Max(1, settings.MinMapControlProfileVersion))
                {
                    string message =
                        $"[ProtoProbe][WARN] World-map control profile version regression detected (profile={sharedProfile.Version}, required={settings.MinMapControlProfileVersion}).";
                    if (settings.FailOnMapControlVersionRegression)
                    {
                        throw new InvalidOperationException(message);
                    }

                    Console.WriteLine(message);
                }
            }

            var registeredPackets = ProtocolRegistry.RegisteredMessageTypes
                .Select(type => type.ToString())
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string descriptorFingerprint = ProtoFingerprint.ComputeFingerprint();
            var generatedDescriptorNames = ProtocolRegistry.GetGeneratedDescriptorNames()
                .ToHashSet(StringComparer.Ordinal);
            string expectedDescriptorPackage = EnhancedMinecraftGameReflection.Descriptor?.Package ?? string.Empty;
            string expectedDescriptorFileName = EnhancedMinecraftGameReflection.Descriptor?.Name ?? string.Empty;
            probeNetwork |= settings.ProbeNetwork;
            var validatedPackets = new List<string>();
            var requiredProbeMissing = new List<string>();
            var optionalProbeMissing = new List<string>();
            var missingPrototypePackets = new List<string>();
            var packetDiagnostics = new List<ProtoProbePacketDiagnostic>();
            var networkProbePayloads = new List<byte[]>();
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
                    packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                        packetName,
                        IsOptional: false,
                        IsRegistered: false,
                        PrototypeResolved: false,
                        RoundTripOk: false,
                        DescriptorName: string.Empty,
                        DescriptorPackage: string.Empty,
                        ErrorMessage: "Unknown message type in config."));
                    continue;
                }

                packetsToProbe.Add(messageType);
            }

            foreach (var messageType in packetsToProbe)
            {
                bool isOptional = ProtocolRegistry.IsOptionalMessageType(messageType);
                if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype) || prototype == null)
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Missing prototype for '{messageType}'. Regenerate protobuf DTOs or update ProtocolRegistry bindings.");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (isOptional)
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }

                    packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                        messageType.ToString(),
                        isOptional,
                        IsRegistered: ProtocolRegistry.IsRegistered(messageType),
                        PrototypeResolved: false,
                        RoundTripOk: false,
                        DescriptorName: string.Empty,
                        DescriptorPackage: string.Empty,
                        ErrorMessage: "Prototype not resolved from ProtocolRegistry."));
                    continue;
                }

                var descriptorParser = prototype.Descriptor?.Parser;
                string descriptorName = prototype.Descriptor?.Name ?? string.Empty;
                string descriptorFullName = prototype.Descriptor?.FullName ?? string.Empty;
                string descriptorPackage = prototype.Descriptor?.File?.Package ?? string.Empty;
                string descriptorSourceName = prototype.Descriptor?.File?.Name ?? string.Empty;
                if (string.IsNullOrWhiteSpace(descriptorName))
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Descriptor metadata missing for '{messageType}'.");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (isOptional)
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }

                    packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                        messageType.ToString(),
                        isOptional,
                        IsRegistered: true,
                        PrototypeResolved: true,
                        RoundTripOk: false,
                        DescriptorName: descriptorName,
                        DescriptorPackage: descriptorPackage,
                        ErrorMessage: "Descriptor metadata missing."));
                    continue;
                }

                if (!generatedDescriptorNames.Contains(descriptorName))
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Descriptor '{descriptorName}' for '{messageType}' is not present in generated reflection descriptor set.");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (isOptional)
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }

                    packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                        messageType.ToString(),
                        isOptional,
                        IsRegistered: true,
                        PrototypeResolved: true,
                        RoundTripOk: false,
                        DescriptorName: descriptorName,
                        DescriptorPackage: descriptorPackage,
                        ErrorMessage: "Descriptor name not found in generated descriptor set."));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(expectedDescriptorFileName) &&
                    !string.IsNullOrWhiteSpace(descriptorSourceName) &&
                    !string.Equals(descriptorSourceName, expectedDescriptorFileName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Descriptor source mismatch for '{messageType}': actual='{descriptorSourceName}', expected='{expectedDescriptorFileName}'.");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (isOptional)
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }

                    packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                        messageType.ToString(),
                        isOptional,
                        IsRegistered: true,
                        PrototypeResolved: true,
                        RoundTripOk: false,
                        DescriptorName: descriptorName,
                        DescriptorPackage: descriptorPackage,
                        ErrorMessage: $"Descriptor source mismatch ({descriptorSourceName} != {expectedDescriptorFileName})."));
                    continue;
                }

                if (!ReferenceEquals(prototype.GetType().Assembly, typeof(EnhancedMinecraftGameReflection).Assembly))
                {
                    string actualAssembly = prototype.GetType().Assembly.GetName().Name ?? string.Empty;
                    string expectedAssembly = typeof(EnhancedMinecraftGameReflection).Assembly.GetName().Name ?? string.Empty;
                    Console.WriteLine($"[ProtoProbe][WARN] Descriptor assembly mismatch for '{messageType}': actual='{actualAssembly}', expected='{expectedAssembly}'.");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (isOptional)
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }

                    packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                        messageType.ToString(),
                        isOptional,
                        IsRegistered: true,
                        PrototypeResolved: true,
                        RoundTripOk: false,
                        DescriptorName: descriptorName,
                        DescriptorPackage: descriptorPackage,
                        ErrorMessage: $"Descriptor assembly mismatch ({actualAssembly} != {expectedAssembly})."));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(expectedDescriptorPackage) &&
                    !string.Equals(descriptorPackage, expectedDescriptorPackage, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Descriptor package mismatch for '{messageType}': actual='{descriptorPackage}', expected='{expectedDescriptorPackage}'.");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (isOptional)
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }

                    packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                        messageType.ToString(),
                        isOptional,
                        IsRegistered: true,
                        PrototypeResolved: true,
                        RoundTripOk: false,
                        DescriptorName: descriptorName,
                        DescriptorPackage: descriptorPackage,
                        ErrorMessage: "Descriptor package mismatch."));
                    continue;
                }

                if (descriptorParser == null)
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Descriptor parser missing for '{messageType}'.");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (isOptional)
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }

                    packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                        messageType.ToString(),
                        isOptional,
                        IsRegistered: true,
                        PrototypeResolved: true,
                        RoundTripOk: false,
                        DescriptorName: descriptorName,
                        DescriptorPackage: descriptorPackage,
                        ErrorMessage: "Descriptor parser missing."));
                    continue;
                }

                try
                {
                    var bytes = prototype.ToByteArray();
                    var parsed = descriptorParser.ParseFrom(bytes);
                    if (parsed != null &&
                        parsed.Descriptor != null &&
                        string.Equals(parsed.Descriptor.FullName, descriptorFullName, StringComparison.Ordinal))
                    {
                        validatedPackets.Add(messageType.ToString());
                        if (bytes.Length > 0)
                        {
                            networkProbePayloads.Add(bytes);
                        }

                        packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                            messageType.ToString(),
                            isOptional,
                            IsRegistered: true,
                            PrototypeResolved: true,
                            RoundTripOk: true,
                            DescriptorName: descriptorName,
                            DescriptorPackage: descriptorPackage,
                            ErrorMessage: string.Empty));
                    }
                    else
                    {
                        missingPrototypePackets.Add(messageType.ToString());
                        if (isOptional)
                        {
                            optionalProbeMissing.Add(messageType.ToString());
                        }
                        else
                        {
                            requiredProbeMissing.Add(messageType.ToString());
                        }

                        packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                            messageType.ToString(),
                            isOptional,
                            IsRegistered: true,
                            PrototypeResolved: true,
                            RoundTripOk: false,
                            DescriptorName: descriptorName,
                            DescriptorPackage: descriptorPackage,
                            ErrorMessage: parsed == null
                                ? "Descriptor parser returned null payload."
                                : $"Descriptor full-name mismatch ({descriptorFullName} != {parsed.Descriptor?.FullName ?? "<null>"})."));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProtoProbe][WARN] Round-trip failed for '{messageType}': {ex.Message}");
                    missingPrototypePackets.Add(messageType.ToString());
                    if (isOptional)
                    {
                        optionalProbeMissing.Add(messageType.ToString());
                    }
                    else
                    {
                        requiredProbeMissing.Add(messageType.ToString());
                    }

                    packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                        messageType.ToString(),
                        isOptional,
                        IsRegistered: true,
                        PrototypeResolved: true,
                        RoundTripOk: false,
                        DescriptorName: descriptorName,
                        DescriptorPackage: descriptorPackage,
                        ErrorMessage: ex.Message));
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
                packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                    nameof(ChunkLoadRequest),
                    IsOptional: false,
                    IsRegistered: true,
                    PrototypeResolved: true,
                    RoundTripOk: true,
                    DescriptorName: ChunkLoadRequest.Descriptor.Name,
                    DescriptorPackage: ChunkLoadRequest.Descriptor.File.Package,
                    ErrorMessage: string.Empty));
            }
            else
            {
                packetDiagnostics.Add(new ProtoProbePacketDiagnostic(
                    nameof(ChunkLoadRequest),
                    IsOptional: false,
                    IsRegistered: true,
                    PrototypeResolved: true,
                    RoundTripOk: false,
                    DescriptorName: ChunkLoadRequest.Descriptor.Name,
                    DescriptorPackage: ChunkLoadRequest.Descriptor.File.Package,
                    ErrorMessage: "Sample chunk request round-trip failed."));
            }

            networkProbePayloads.Insert(0, payload);
            int maxProbePackets = Math.Max(1, settings.MaxNetworkProbePackets);
            var selectedProbePayloads = networkProbePayloads
                .Where(bytes => bytes.Length > 0)
                .Take(maxProbePackets)
                .ToArray();
            if (selectedProbePayloads.Length == 0)
            {
                selectedProbePayloads = new[] { payload };
            }

            bool networkAttempted = false;
            bool networkOk = false;
            string networkError = string.Empty;
            var unboundGeneratedDescriptors = ProtocolRegistry.GetGeneratedDescriptorsWithoutBindings();
            var unboundRequiredGeneratedDescriptors = ProtocolRegistry.GetGeneratedRequiredDescriptorsWithoutBindings();

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
                        foreach (var probePayload in selectedProbePayloads)
                        {
                            byte[] lengthPrefix = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(probePayload.Length));
                            await stream.WriteAsync(lengthPrefix, cancellationToken);
                            await stream.WriteAsync(probePayload, cancellationToken);
                        }
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
            requiredMissing.AddRange(
                unboundRequiredGeneratedDescriptors.Select(name => $"descriptor:{name}"));
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
            string profileHydrologySignature = sharedProfile?.HydrologySignature ?? string.Empty;
            profileHydrologyMatchesShared =
                string.IsNullOrWhiteSpace(profileHydrologySignature) ||
                string.Equals(profileHydrologySignature, hydrologySignature, StringComparison.OrdinalIgnoreCase);
            string profileHash = sharedProfile?.ProfileHash ?? string.Empty;
            int profileVersion = sharedProfile?.Version ?? 0;
            var coverage = ProtocolRegistry.GetBindingCoverage();
            var typeConsistencyDiagnostics = ProtocolRegistry.BuildTypeConsistencyDiagnostics();
            var requiredTypeDrift = typeConsistencyDiagnostics
                .Where(diagnostic => diagnostic.HasEnhancedType && diagnostic.HasLegacyType && !diagnostic.LegacyTypeMatches && !diagnostic.IsOptional)
                .Select(diagnostic => diagnostic.MessageType.ToString())
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (requiredTypeDrift.Length > 0)
            {
                Console.WriteLine("[ProtoProbe][WARN] Required legacy/enhanced type drift detected: " + string.Join(", ", requiredTypeDrift));
                if (settings.FailOnRequiredTypeDrift)
                {
                    requiredMissing.AddRange(requiredTypeDrift.Select(name => $"type-drift:{name}"));
                }
            }

            if (!profileHydrologyMatchesShared && settings.FailOnHydrologySignatureMismatch)
            {
                requiredMissing.Add("profile:HydrologySignatureMismatch");
            }

            requiredMissing = requiredMissing
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (requiredMissing.Count > 0)
            {
                Console.WriteLine($"[ProtoProbe][WARN] Required protocol bindings missing: {string.Join(", ", requiredMissing)}");
            }

            var registryReferenceSummary = new ProtoRegistryReferenceSummary(
                ProtocolRegistry.GetGeneratedDescriptorNames(),
                registeredPackets,
                unboundRequiredGeneratedDescriptors,
                unboundGeneratedDescriptors,
                ProtocolRegistry.GetBindingDiagnostics(),
                typeConsistencyDiagnostics);

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
                profileHydrologySignature,
                profileHydrologyMatchesShared,
                registeredCount,
                coverage.GeneratedDescriptors,
                coverage.BoundDescriptors,
                unboundRequiredGeneratedDescriptors.Count,
                unboundGeneratedDescriptors,
                unboundRequiredGeneratedDescriptors,
                reportPath,
                referenceReportPath,
                profileHash,
                profileVersion,
                profilePath,
                registryReferenceSummary,
                packetDiagnostics);

            Console.WriteLine(
                $"[ProtoProbe] Hydrology={hydrologySignature} Registered={registeredCount} " +
                $"Validated={validatedPackets.Count} Missing={requiredMissing.Count} MissingPrototype={missingPrototypePackets.Count} OptionalMissing={optionalMissing.Count} " +
                $"Coverage={coverage.BoundDescriptors}/{coverage.GeneratedDescriptors} UnboundGenerated={unboundGeneratedDescriptors.Count} UnboundRequired={unboundRequiredGeneratedDescriptors.Count} PacketDiagnostics={packetDiagnostics.Count} " +
                $"ProfileHydrologyMatch={profileHydrologyMatchesShared} " +
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
