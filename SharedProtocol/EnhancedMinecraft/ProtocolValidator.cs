using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace SharedProtocol.EnhancedMinecraft;

/// <summary>
/// Provides lightweight validation to ensure the generated EnhancedMinecraft protobuf contracts
/// are wired into the runtime registry and expose the mandatory fields required by the server.
/// </summary>
public static class ProtocolValidator
{
    private static readonly MinecraftMessageType[] RequiredMessages =
    {
        MinecraftMessageType.PlayerStateUpdate,
        MinecraftMessageType.PlayerActionRequest,
        MinecraftMessageType.PlayerActionResponse,
        MinecraftMessageType.ChunkDataRequest,
        MinecraftMessageType.ChunkDataResponse,
        MinecraftMessageType.ChunkUnloadNotification,
        MinecraftMessageType.ChunkUnloadAcknowledge,
        MinecraftMessageType.BlockChangeNotification,
        MinecraftMessageType.EntitySpawn,
        MinecraftMessageType.EntityDespawn,
        MinecraftMessageType.TimeUpdate,
        MinecraftMessageType.WeatherChange,
        MinecraftMessageType.SoundEffect,
        MinecraftMessageType.ParticleEffect
    };

    public static void ValidateEnhancedContracts()
    {
        ProtoFingerprint.AssertDescriptorFingerprint();

        foreach (var message in RequiredMessages)
        {
            ProtocolRegistry.EnsureRegistered(message);
        }

        ValidateRegistryDescriptors();
        ValidateRegistryCoverage();
        ValidateRegistryPrototypes();
        ValidateParserBindings();
        ValidateChunkDescriptor();
        ValidateChunkRequestAndResponseDescriptors();
        ValidateWorldControlDescriptors();
        ProtoDiagnostics.AssertRegistryClean();
        ProtocolRegistry.ValidateBindings();
    }

    private static void ValidateChunkDescriptor()
    {
        var descriptor = RequireDescriptor(nameof(ChunkData));
        EnsureFields(descriptor, "chunk_x", "chunk_z", "block_data", "biome_data", "light_data", "generation_timestamp", "entities", "tile_entities");
    }

    private static void ValidateChunkRequestAndResponseDescriptors()
    {
        var request = RequireDescriptor(nameof(ChunkLoadRequest));
        EnsureFields(request, "chunk_positions", "view_distance");

        var response = RequireDescriptor(nameof(ChunkLoadResponse));
        EnsureFields(response, "chunks", "total_requested", "total_sent");
    }

    private static void ValidateWorldControlDescriptors()
    {
        var worldInfo = RequireDescriptor(nameof(WorldInfo));
        EnsureFields(worldInfo, "world_name", "world_seed", "world_type", "default_game_mode", "hardcore_mode", "world_time", "day_time", "weather", "spawn_point", "difficulty", "world_border");

        var weather = RequireDescriptor(nameof(WeatherInfo));
        EnsureFields(weather, "weather_type", "duration_ticks", "intensity", "thundering");
        var weatherUpdate = RequireDescriptor(nameof(WeatherUpdateBroadcast));
        EnsureFields(weatherUpdate, "weather");

        var worldBorder = RequireDescriptor(nameof(WorldBorder));
        EnsureFields(worldBorder, "center", "diameter", "target_diameter", "time_to_target", "warning_distance", "warning_time", "damage_per_block", "damage_buffer");

        var timeUpdate = RequireDescriptor(nameof(TimeUpdateBroadcast));
        EnsureFields(timeUpdate, "world_time", "day_time");

        var unloadNotification = RequireDescriptor(nameof(ChunkUnloadNotification));
        EnsureFields(unloadNotification, "player_id", "chunk_x", "chunk_z", "reason", "view_distance", "timestamp_ms");

        var unloadAck = RequireDescriptor(nameof(ChunkUnloadAck));
        EnsureFields(unloadAck, "chunk_x", "chunk_z", "accepted", "remaining_chunks", "note");
    }

    private static MessageDescriptor RequireDescriptor(string messageName)
    {
        var descriptor = EnhancedMinecraftGameReflection.Descriptor
            .MessageTypes
            .FirstOrDefault(d => d.Name == messageName);

        if (descriptor == null)
        {
            throw new InvalidOperationException(
                $"EnhancedMinecraftProtocol.{messageName} descriptor is missing. Regenerate proto assets.");
        }

        return descriptor;
    }

    private static void EnsureFields(MessageDescriptor descriptor, params string[] fieldNames)
    {
        foreach (string fieldName in fieldNames)
        {
            if (descriptor.FindFieldByName(fieldName) == null)
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraftProtocol.{descriptor.Name} descriptor missing required field '{fieldName}'. Regenerate proto assets.");
            }
        }
    }

    private static void ValidateRegistryPrototypes()
    {
        string expectedNamespace = typeof(ChunkLoadResponse).Namespace ?? string.Empty;

        foreach (var messageType in ProtocolRegistry.RegisteredMessageTypes)
        {
            if (!ProtocolRegistry.TryCreatePrototype(messageType, out IMessage? prototype))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft registry binding for '{messageType}' resolved to a null prototype. Regenerate protobuf assets so using references point at generated classes.");
            }

            var descriptor = prototype.Descriptor;
            if (descriptor == null)
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{messageType}' is missing descriptor metadata. Ensure generated protobuf assemblies are referenced and up to date.");
            }

            string prototypeNamespace = descriptor.ClrType?.Namespace ?? prototype.GetType().Namespace ?? string.Empty;
            if (!string.Equals(prototypeNamespace, expectedNamespace, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{messageType}' was generated into namespace '{prototypeNamespace}', expected '{expectedNamespace}'. Check using directives or regenerate protobuf assets so server and Unity share the same namespace.");
            }

            Assembly expectedAssembly = typeof(EnhancedMinecraftGameReflection).Assembly;
            Assembly prototypeAssembly = prototype.GetType().Assembly;
            Assembly descriptorAssembly = descriptor.ClrType?.Assembly ?? prototypeAssembly;

            if (!ReferenceEquals(prototypeAssembly, expectedAssembly) || !ReferenceEquals(descriptorAssembly, expectedAssembly))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{messageType}' is loaded from assembly '{prototypeAssembly.GetName().Name}', expected '{expectedAssembly.GetName().Name}'. Check project references and regenerate protobuf assets so server and Unity share the same generated assembly.");
            }

            string fileName = descriptor.File?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{messageType}' is missing a file descriptor reference. Ensure the generated protobuf classes are included in SharedProtocol and Unity exports.");
            }
        }
    }

    private static void ValidateParserBindings()
    {
        foreach (var binding in ProtocolRegistry.RegisteredDescriptors)
        {
            if (!ProtocolRegistry.TryCreatePrototype(binding.MessageType, out IMessage? prototype))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft registry binding for '{binding.MessageType}' resolved to a null prototype. Regenerate protobuf assets so using references point at generated classes.");
            }

            Type prototypeType = prototype.GetType();
            PropertyInfo? parserProperty = prototypeType.GetProperty("Parser", BindingFlags.Public | BindingFlags.Static);
            if (parserProperty?.GetValue(null) is not MessageParser parser)
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.DescriptorName}' is missing a static Parser. Ensure the generated protobuf classes are referenced by both server and client builds.");
            }

            Assembly expectedAssembly = typeof(EnhancedMinecraftGameReflection).Assembly;
            if (!ReferenceEquals(prototypeType.Assembly, expectedAssembly) || !ReferenceEquals(parser.GetType().Assembly, expectedAssembly))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.DescriptorName}' resolved from assembly '{prototypeType.Assembly.GetName().Name}', expected '{expectedAssembly.GetName().Name}'. Update using directives or regenerate protobuf assets so both server and Unity share the same generated DLL.");
            }

            IMessage parsedInstance;
            try
            {
                parsedInstance = parser.ParseFrom(ByteString.Empty);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.DescriptorName}' cannot parse an empty payload. Verify the generated Parser and using directives are in sync with proto/*.proto.", ex);
            }

            if (!ReferenceEquals(parsedInstance.Descriptor, prototype.Descriptor))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.DescriptorName}' parsed descriptor does not match registry prototype. Regenerate protobuf assets so server and Unity share the same generated types.");
            }
        }
    }

    private static void ValidateRegistryDescriptors()
    {
        var descriptor = EnhancedMinecraftGameReflection.Descriptor;
        string descriptorPackage = descriptor.Package ?? string.Empty;

        var registeredNames = new HashSet<string>(
            ProtocolRegistry.RegisteredDescriptors.Select(binding => binding.DescriptorName),
            StringComparer.Ordinal);

        foreach (var binding in ProtocolRegistry.RegisteredDescriptors)
        {
            var messageDescriptor = descriptor.MessageTypes.FirstOrDefault(d => d.Name == binding.DescriptorName);
            if (messageDescriptor == null)
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft descriptor '{binding.DescriptorName}' is missing from generated protobufs. Regenerate proto assets so registry bindings stay valid.");
            }

            string package = messageDescriptor.File?.Package ?? string.Empty;
            if (!string.Equals(package, descriptorPackage, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.DescriptorName}' is generated with package '{package}', expected '{descriptorPackage}'. Regenerate protobuf assets so using references resolve to the correct namespace.");
            }
        }

        foreach (var declared in descriptor.MessageTypes)
        {
            if (!registeredNames.Contains(declared.Name))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft generated message '{declared.Name}' is not registered in ProtocolRegistry. Add a binding so protobuf references are validated consistently on client and server.");
            }
        }
    }

    private static void ValidateRegistryCoverage()
    {
        var registeredDescriptorTypes = new HashSet<MinecraftMessageType>(
            ProtocolRegistry.RegisteredDescriptors.Select(binding => binding.MessageType));

        foreach (var messageType in ProtocolRegistry.RegisteredMessageTypes)
        {
            if (!registeredDescriptorTypes.Contains(messageType))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft message '{messageType}' is registered without a descriptor binding. Ensure generated protobuf classes are referenced and ProtocolRegistry bindings include both parser and descriptor entries.");
            }
        }
    }
}
