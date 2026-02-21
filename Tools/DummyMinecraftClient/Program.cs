using System.Net.Sockets;
using System.Text.Json;
using GameCommon.World;
using Google.Protobuf;
using SharedProtocol;
using SharedProtocol.EnhancedMinecraft;
using EnhancedMinecraftProtocol;

namespace DummyMinecraftClient;

public sealed class DummyClientConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9000;
    public int ConnectTimeoutMs { get; set; } = 1500;
    public int ReceiveTimeoutMs { get; set; } = 1500;
    public bool ProbeNetwork { get; set; } = false;
    public int MaxPacketsToSend { get; set; } = 6;
    public bool StrictRequiredBindings { get; set; } = true;
    public bool FailOnHydrologySignatureMismatch { get; set; } = true;
    public int MinMapControlProfileVersion { get; set; } = 50;
    public bool FailOnMapControlVersionRegression { get; set; } = true;
    public string WorldMapControlProfilePath { get; set; } = "config/world_map_control_profile.json";
    public bool IncludeOptionalMessages { get; set; } = false;
    public string[] Packets { get; set; } = new[]
    {
        "PlayerStateUpdate",
        "ChunkDataRequest",
        "ChunkDataResponse",
        "ChunkUnloadNotification",
        "TimeUpdate",
        "WeatherChange",
        "SoundEffect",
        "ParticleEffect"
    };

    public static DummyClientConfig Load(string path)
    {
        if (!File.Exists(path))
        {
            return new DummyClientConfig();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<DummyClientConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        }) ?? new DummyClientConfig();
    }
}

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string configPath = "config/dummy_minecraft_client.json";
        bool forceNetworkProbe = false;
        bool? includeOptionalOverride = null;
        bool? strictRequiredOverride = null;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--config":
                case "-c":
                    if (i + 1 < args.Length)
                    {
                        configPath = args[++i];
                    }
                    break;
                case "--network":
                    forceNetworkProbe = true;
                    break;
                case "--include-optional":
                    includeOptionalOverride = true;
                    break;
                case "--required-only":
                    includeOptionalOverride = false;
                    break;
                case "--strict-required-bindings":
                    strictRequiredOverride = true;
                    break;
                case "--no-strict-required-bindings":
                    strictRequiredOverride = false;
                    break;
            }
        }

        var config = DummyClientConfig.Load(configPath);
        if (includeOptionalOverride.HasValue)
        {
            config.IncludeOptionalMessages = includeOptionalOverride.Value;
        }

        if (strictRequiredOverride.HasValue)
        {
            config.StrictRequiredBindings = strictRequiredOverride.Value;
        }

        bool probeNetwork = forceNetworkProbe || config.ProbeNetwork;

        Console.WriteLine("=== Dummy Minecraft Client (Protocol Probe) ===");
        Console.WriteLine($"Config: {Path.GetFullPath(configPath)}");
        Console.WriteLine($"Mode: IncludeOptional={config.IncludeOptionalMessages}, StrictRequiredBindings={config.StrictRequiredBindings}, ProbeNetwork={probeNetwork}");

        string resolvedProfilePath = string.IsNullOrWhiteSpace(config.WorldMapControlProfilePath)
            ? string.Empty
            : Path.GetFullPath(config.WorldMapControlProfilePath);
        if (!string.IsNullOrWhiteSpace(resolvedProfilePath))
        {
            var profile = WorldMapControlProfileUtility.Load(resolvedProfilePath);
            if (profile != null)
            {
                bool signatureMatch = string.Equals(
                    profile.HydrologySignature,
                    SharedFeatureCatalog.HydrologySignature,
                    StringComparison.OrdinalIgnoreCase);
                Console.WriteLine(
                    $"Profile: {resolvedProfilePath} (version={profile.Version}, hash={profile.ProfileHash}, hydrology={profile.HydrologySignature}, shared={SharedFeatureCatalog.HydrologySignature})");
                if (!signatureMatch && config.FailOnHydrologySignatureMismatch)
                {
                    Console.WriteLine("[ERROR] Hydrology signature mismatch detected and fail-fast is enabled.");
                    return 1;
                }

                if (profile.Version < Math.Max(1, config.MinMapControlProfileVersion) && config.FailOnMapControlVersionRegression)
                {
                    Console.WriteLine($"[ERROR] Map control profile version regression detected (profile={profile.Version}, required={config.MinMapControlProfileVersion}).");
                    return 1;
                }
            }
        }

        ProtoRuntime.EnsureInitialized();
        ProtoFingerprint.AssertDescriptorFingerprint();
        ProtocolRegistry.ValidateBindings();
        var missingRequiredBindings = ProtocolRegistry.GetUnregisteredRequiredMessages().ToArray();
        var missingOptionalBindings = ProtocolRegistry.GetOptionalMessagesWithoutBindings()
            .Select(type => type.ToString())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var typeDrift = ProtocolRegistry.BuildTypeConsistencyDiagnostics()
            .Where(item => item.HasEnhancedType && item.HasLegacyType && !item.LegacyTypeMatches)
            .OrderBy(item => item.MessageType.ToString(), StringComparer.Ordinal)
            .ToArray();
        var unboundRequiredDescriptors = ProtocolRegistry.GetGeneratedRequiredDescriptorsWithoutBindings()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var unboundGeneratedDescriptors = ProtocolRegistry.GetGeneratedDescriptorsWithoutBindings()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var generatedDescriptorNames = ProtocolRegistry.GetGeneratedDescriptorNames()
            .ToHashSet(StringComparer.Ordinal);
        string expectedDescriptorPackage = EnhancedMinecraftGameReflection.Descriptor?.Package ?? string.Empty;
        string expectedDescriptorFileName = EnhancedMinecraftGameReflection.Descriptor?.Name ?? string.Empty;

        if (missingRequiredBindings.Length > 0)
        {
            Console.WriteLine("[WARN] Missing required protocol bindings: " + string.Join(", ", missingRequiredBindings));
            if (config.StrictRequiredBindings)
            {
                Console.WriteLine("[ERROR] Strict mode enabled; aborting dummy client run.");
                return 1;
            }
        }

        if (missingOptionalBindings.Length > 0)
        {
            Console.WriteLine("[INFO] Optional protocol bindings not registered: " + string.Join(", ", missingOptionalBindings));
        }

        if (typeDrift.Length > 0)
        {
            Console.WriteLine("[INFO] Legacy/Enhanced type drift entries: " +
                              string.Join(", ", typeDrift.Select(item =>
                                  $"{item.MessageType}(legacy={item.LegacyClrType}, enhanced={item.EnhancedClrType}, optional={item.IsOptional})")));
        }

        if (unboundGeneratedDescriptors.Length > 0)
        {
            Console.WriteLine("[INFO] Generated descriptors without registry bindings: " + string.Join(", ", unboundGeneratedDescriptors));
        }

        if (unboundRequiredDescriptors.Length > 0)
        {
            Console.WriteLine("[WARN] Required generated descriptors missing registry bindings: " + string.Join(", ", unboundRequiredDescriptors));
            if (config.StrictRequiredBindings)
            {
                Console.WriteLine("[ERROR] Strict mode enabled; aborting due to required descriptor binding gaps.");
                return 1;
            }
        }

        var packetTypes = ResolvePackets(config.Packets);
        if (config.IncludeOptionalMessages)
        {
            packetTypes.AddRange(ProtocolRegistry.GetOptionalMessagesWithoutBindings());
            packetTypes = packetTypes.Distinct().ToList();
        }

        int requiredTargets = packetTypes.Count(type => !ProtocolRegistry.IsOptionalMessageType(type));
        int optionalTargets = packetTypes.Count - requiredTargets;
        int roundTripOk = 0;
        int requiredRoundTripOk = 0;
        int optionalRoundTripOk = 0;
        var payloads = new List<(MinecraftMessageType Type, byte[] Payload)>();

        foreach (var messageType in packetTypes)
        {
            if (!ProtocolRegistry.TryCreatePrototype(messageType, out IMessage prototype) || prototype == null)
            {
                Console.WriteLine($"[WARN] Prototype missing: {messageType}");
                continue;
            }

            try
            {
                var descriptor = prototype.Descriptor;
                string descriptorName = descriptor?.Name ?? string.Empty;
                string descriptorPackage = descriptor?.File?.Package ?? string.Empty;
                string descriptorFullName = descriptor?.FullName ?? string.Empty;
                string descriptorSourceName = descriptor?.File?.Name ?? string.Empty;
                if (descriptor == null || string.IsNullOrWhiteSpace(descriptorName))
                {
                    Console.WriteLine($"[WARN] Descriptor missing: {messageType}");
                    continue;
                }

                if (!generatedDescriptorNames.Contains(descriptorName))
                {
                    Console.WriteLine($"[WARN] Descriptor not found in generated reflection set: {messageType} ({descriptorName})");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(expectedDescriptorFileName) &&
                    !string.IsNullOrWhiteSpace(descriptorSourceName) &&
                    !string.Equals(descriptorSourceName, expectedDescriptorFileName, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"[WARN] Descriptor source mismatch: {messageType} (actual={descriptorSourceName}, expected={expectedDescriptorFileName})");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(expectedDescriptorPackage) &&
                    !string.Equals(descriptorPackage, expectedDescriptorPackage, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[WARN] Descriptor package mismatch: {messageType} (actual={descriptorPackage}, expected={expectedDescriptorPackage})");
                    continue;
                }

                string actualAssembly = prototype.GetType().Assembly.GetName().Name ?? string.Empty;
                string expectedAssembly = typeof(EnhancedMinecraftGameReflection).Assembly.GetName().Name ?? string.Empty;
                if (!string.Equals(actualAssembly, expectedAssembly, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[WARN] Descriptor assembly mismatch: {messageType} (actual={actualAssembly}, expected={expectedAssembly})");
                    continue;
                }

                byte[] payload = prototype.ToByteArray();
                var parser = descriptor.Parser;
                if (parser == null)
                {
                    Console.WriteLine($"[WARN] Parser missing: {messageType}");
                    continue;
                }

                var parsed = parser.ParseFrom(payload);
                if (parsed?.Descriptor == null ||
                    !string.Equals(parsed.Descriptor.FullName, descriptorFullName, StringComparison.Ordinal))
                {
                    Console.WriteLine($"[WARN] Descriptor full-name mismatch after round-trip: {messageType} ({descriptorFullName} -> {parsed?.Descriptor?.FullName ?? "<null>"})");
                    continue;
                }

                roundTripOk++;
                if (ProtocolRegistry.IsOptionalMessageType(messageType))
                {
                    optionalRoundTripOk++;
                }
                else
                {
                    requiredRoundTripOk++;
                }
                payloads.Add((messageType, payload));
                Console.WriteLine($"[OK] {messageType} round-trip ({payload.Length} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] {messageType} round-trip failed: {ex.Message}");
            }
        }

        Console.WriteLine($"Round-trip result: total={roundTripOk}/{packetTypes.Count}, required={requiredRoundTripOk}/{requiredTargets}, optional={optionalRoundTripOk}/{optionalTargets}");

        bool networkOk = true;
        if (probeNetwork)
        {
            networkOk = await ProbeNetworkAsync(config, payloads);
        }

        bool roundTripPassed = requiredRoundTripOk == requiredTargets;
        if (!roundTripPassed)
        {
            Console.WriteLine("[WARN] Some required packet round-trips failed. Check registry/prototype mappings.");
        }

        return roundTripPassed && networkOk && missingRequiredBindings.Length == 0 && unboundRequiredDescriptors.Length == 0 ? 0 : 1;
    }

    private static List<MinecraftMessageType> ResolvePackets(IEnumerable<string> packetNames)
    {
        var types = new List<MinecraftMessageType>();
        foreach (var packetName in packetNames)
        {
            if (Enum.TryParse(packetName, true, out MinecraftMessageType messageType))
            {
                types.Add(messageType);
            }
            else
            {
                Console.WriteLine($"[WARN] Unknown packet in config: {packetName}");
            }
        }

        if (types.Count == 0)
        {
            types.AddRange(ProtocolRegistry.RegisteredMessageTypes);
        }

        return types.Distinct().ToList();
    }

    private static async Task<bool> ProbeNetworkAsync(DummyClientConfig config, List<(MinecraftMessageType Type, byte[] Payload)> payloads)
    {
        Console.WriteLine($"Network probe: {config.Host}:{config.Port}");

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(config.Host, config.Port);
            var timeoutTask = Task.Delay(Math.Max(100, config.ConnectTimeoutMs));
            var completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
            if (completed == timeoutTask)
            {
                Console.WriteLine("[WARN] Connect timeout");
                return false;
            }

            await connectTask.ConfigureAwait(false);
            using var stream = client.GetStream();
            stream.ReadTimeout = Math.Max(100, config.ReceiveTimeoutMs);
            stream.WriteTimeout = Math.Max(100, config.ReceiveTimeoutMs);

            int sendCount = Math.Min(Math.Max(1, config.MaxPacketsToSend), payloads.Count);
            for (int i = 0; i < sendCount; i++)
            {
                var packet = payloads[i];
                await WritePacketAsync(stream, (int)packet.Type, packet.Payload).ConfigureAwait(false);
                Console.WriteLine($"[NET-SEND] {packet.Type} ({packet.Payload.Length} bytes)");
            }

            var header = new byte[8];
            if (stream.DataAvailable)
            {
                int read = await stream.ReadAsync(header, 0, header.Length).ConfigureAwait(false);
                if (read == header.Length)
                {
                    int responseType = BitConverter.ToInt32(header, 0);
                    int responseLength = BitConverter.ToInt32(header, 4);
                    Console.WriteLine($"[NET-RECV] type={responseType}, length={responseLength}");
                }
            }

            Console.WriteLine("[OK] Network probe completed");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Network probe failed: {ex.Message}");
            return false;
        }
    }

    private static async Task WritePacketAsync(NetworkStream stream, int messageType, byte[] payload)
    {
        byte[] typeBytes = BitConverter.GetBytes(messageType);
        byte[] lengthBytes = BitConverter.GetBytes(payload.Length);
        await stream.WriteAsync(typeBytes, 0, typeBytes.Length).ConfigureAwait(false);
        await stream.WriteAsync(lengthBytes, 0, lengthBytes.Length).ConfigureAwait(false);
        if (payload.Length > 0)
        {
            await stream.WriteAsync(payload, 0, payload.Length).ConfigureAwait(false);
        }

        await stream.FlushAsync().ConfigureAwait(false);
    }
}
