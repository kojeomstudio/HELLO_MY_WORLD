using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnhancedMinecraftProtocol;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using SharedProtocol;

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

    private static readonly HashSet<MinecraftMessageType> OptionalMessages = new()
    {
        MinecraftMessageType.MultiBlockChange,
        MinecraftMessageType.InventoryUpdate,
        MinecraftMessageType.ItemUse,
        MinecraftMessageType.ItemDrop,
        MinecraftMessageType.ItemPickup,
        MinecraftMessageType.EntityUpdate,
        MinecraftMessageType.EntityInteract,
        MinecraftMessageType.ContainerOpen,
        MinecraftMessageType.ContainerClose,
        MinecraftMessageType.ContainerUpdate
    };

    internal static IReadOnlyCollection<MinecraftMessageType> GetOptionalMessages() => OptionalMessages;

    internal static bool IsOptionalMessage(MinecraftMessageType messageType) => OptionalMessages.Contains(messageType);

    public static void ValidateEnhancedContracts()
    {
        ProtoFingerprint.AssertDescriptorFingerprint();

        foreach (var message in RequiredMessages)
        {
            ProtocolRegistry.EnsureRegistered(message);
        }

        ValidateUniqueBindings();
        ValidateRegistryDescriptors();
        ValidateRequiredDescriptorBindings();
        ValidateDescriptorFiles();
        ValidatePrototypeDescriptorFiles();
        ValidateDescriptorAssemblies();
        ValidateDescriptorOrigins();
        ValidateDescriptorNamespaces();
        ValidateRegistryCoverage();
        ValidateRegistryPrototypes();
        ValidateRegistryBindingNames();
        ValidateParserBindings();
        ValidateChunkDescriptor();
        ValidateChunkRequestAndResponseDescriptors();
        ValidateActionDescriptors();
        ValidatePlayerStateDescriptors();
        ValidateWorldControlDescriptors();
        ValidateServerStatusDescriptors();
        ValidateEntityDescriptors();
        ValidateEnumBindings();
        ValidateOptionalDescriptorVisibility();
        ProtoDiagnostics.AssertRegistryClean();
        ProtocolRegistry.ValidateBindings();
    }

    public static void ValidateHandlerBindings(MinecraftMessageDispatcher dispatcher)
    {
        if (dispatcher == null) throw new ArgumentNullException(nameof(dispatcher));

        foreach (var messageType in ProtocolRegistry.RegisteredMessageTypes)
        {
            bool hasHandler = dispatcher.TryGetHandlerContract(messageType, out var handlerContract);
            bool hasDescriptor = ProtocolRegistry.TryResolveContractType(messageType, out var contractType);

            if (hasHandler && hasDescriptor && handlerContract != null && contractType != null && !contractType.IsAssignableFrom(handlerContract))
            {
                throw new InvalidOperationException(
                    $"Handler for '{messageType}' expects {handlerContract.Name} but EnhancedMinecraft registry exposes '{contractType.Name}'. Regenerate protobuf assets or update the handler contract.");
            }

            if (!hasHandler && !IsOptionalMessage(messageType))
            {
                Console.WriteLine($"[Proto][WARN] EnhancedMinecraft packet '{messageType}' is registered but has no handler. Add a handler or mark it optional.");
            }

            if (hasHandler && !hasDescriptor && !IsOptionalMessage(messageType))
            {
                Console.WriteLine($"[Proto][WARN] Handler registered for '{messageType}' without a generated EnhancedMinecraft binding. Regenerate protobuf assets or update ProtocolRegistry.");
            }
        }
    }

    private static void ValidateRequiredDescriptorBindings()
    {
        foreach (var messageType in RequiredMessages)
        {
            if (!ProtocolRegistry.RegisteredDescriptors.Any(binding => binding.MessageType == messageType))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft message '{messageType}' is missing a descriptor binding. Regenerate protobuf assets so generated classes are reachable via using directives and ProtocolRegistry.");
            }

            if (!ProtocolRegistry.TryCreatePrototype(messageType, out var prototype))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft registry could not create a prototype for '{messageType}'. Ensure generated protobuf classes are referenced and ProtocolRegistry factories return the generated types.");
            }

            string? descriptorFile = prototype.Descriptor?.File?.Name;
            string? expectedFile = EnhancedMinecraftGameReflection.Descriptor?.Name;
            if (!string.IsNullOrWhiteSpace(descriptorFile) &&
                !string.IsNullOrWhiteSpace(expectedFile) &&
                !string.Equals(descriptorFile, expectedFile, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft message '{messageType}' resolved from descriptor '{descriptorFile}', expected '{expectedFile}'. Update using directives or regenerate protobuf assets so server and Unity reference the same descriptor file.");
            }
        }
    }

    /// <summary>
    /// Lightweight guard to ensure a handler's message type matches the registered EnhancedMinecraft contract.
    /// Used by UnifiedMessageHandler to fail fast when protobuf bindings drift.
    /// </summary>
    public static void ValidateMessageContract<TMessage>(MinecraftMessageType messageType)
    {
        ProtoFingerprint.AssertDescriptorFingerprint();

        if (!ProtocolRegistry.TryResolveContractType(messageType, out var contractType) || contractType == null)
        {
            throw new InvalidOperationException($"[Proto] No EnhancedMinecraft binding registered for '{messageType}'. Run protoc and refresh ProtocolRegistry.");
        }

        if (!contractType.IsAssignableFrom(typeof(TMessage)))
        {
            throw new InvalidOperationException($"[Proto] Handler expects {typeof(TMessage).Name} but registry exposes {contractType.Name} for '{messageType}'. Regenerate bindings or update the handler contract.");
        }
    }

    /// <summary>
    /// Targeted chunk contract validation for handlers that need to fail fast on stale protobuf bindings.
    /// </summary>
    public static void ValidateChunkContracts()
    {
        ProtoFingerprint.AssertDescriptorFingerprint();
        ValidateRegistryDescriptors();
        ValidateRegistryPrototypes();
        ValidateRegistryBindingNames();
        ValidateChunkDescriptor();
        ValidateChunkRequestAndResponseDescriptors();
        ValidateChunkUnloadDescriptors();
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

    private static void ValidateChunkUnloadDescriptors()
    {
        var unloadNotification = RequireDescriptor(nameof(ChunkUnloadNotification));
        EnsureFields(unloadNotification, "player_id", "chunk_x", "chunk_z", "reason", "view_distance", "timestamp_ms");

        var unloadAck = RequireDescriptor(nameof(ChunkUnloadAck));
        EnsureFields(unloadAck, "chunk_x", "chunk_z", "accepted", "remaining_chunks", "note");
    }

    private static void ValidateActionDescriptors()
    {
        var actionRequest = RequireDescriptor(nameof(PlayerActionRequest));
        EnsureFields(actionRequest, "action", "target_position", "face", "cursor_position", "used_item", "sequence", "action_data");

        var actionResponse = RequireDescriptor(nameof(PlayerActionResponse));
        EnsureFields(actionResponse, "success", "message", "sequence", "result");

        var actionResult = RequireDescriptor(nameof(ActionResult));
        EnsureFields(actionResult, "updated_items", "applied_effects", "health_change", "hunger_change", "experience_change", "particle_effect", "sound_effect");
    }

    private static void ValidatePlayerStateDescriptors()
    {
        var playerInfo = RequireDescriptor(nameof(PlayerInfo));
        EnsureFields(
            playerInfo,
            "player_id",
            "username",
            "position",
            "rotation",
            "level",
            "experience",
            "experience_progress",
            "health",
            "max_health",
            "hunger",
            "max_hunger",
            "saturation",
            "game_mode",
            "inventory",
            "selected_slot",
            "active_effects",
            "stats");

        var stats = RequireDescriptor(nameof(PlayerStats));
        EnsureFields(stats, "blocks_mined", "blocks_placed", "distance_walked", "monsters_killed", "deaths", "play_time_ticks");

        var inventory = RequireDescriptor(nameof(PlayerInventory));
        EnsureFields(
            inventory,
            "main_inventory",
            "hotbar",
            "helmet",
            "chestplate",
            "leggings",
            "boots",
            "offhand",
            "crafting_result",
            "crafting_input");

        var slot = RequireDescriptor(nameof(InventorySlot));
        EnsureFields(slot, "slot_id", "item_stack");

        var itemStack = RequireDescriptor(nameof(ItemStack));
        EnsureFields(itemStack, "item_id", "item_name", "count", "durability", "max_durability", "enchantments", "nbt_data", "item_type", "rarity");
    }

    private static void ValidateWorldControlDescriptors()
    {
        var worldInfo = RequireDescriptor(nameof(WorldInfo));
        EnsureFields(worldInfo, "world_name", "world_seed", "world_type", "default_game_mode", "hardcore_mode", "world_time", "day_time", "weather", "spawn_point", "difficulty", "world_border");

        var weather = RequireDescriptor(nameof(WeatherInfo));
        EnsureFields(weather, "weather_type", "duration_ticks", "intensity", "thundering");
        var weatherUpdate = RequireDescriptor(nameof(WeatherUpdateBroadcast));
        EnsureFields(weatherUpdate, "weather", "change_timestamp");

        var worldBorder = RequireDescriptor(nameof(WorldBorder));
        EnsureFields(worldBorder, "center", "diameter", "target_diameter", "time_to_target", "warning_distance", "warning_time", "damage_per_block", "damage_buffer");

        var timeUpdate = RequireDescriptor(nameof(TimeUpdateBroadcast));
        EnsureFields(timeUpdate, "world_time", "day_time");

        var unloadNotification = RequireDescriptor(nameof(ChunkUnloadNotification));
        EnsureFields(unloadNotification, "player_id", "chunk_x", "chunk_z", "reason", "view_distance", "timestamp_ms");

        var unloadAck = RequireDescriptor(nameof(ChunkUnloadAck));
        EnsureFields(unloadAck, "chunk_x", "chunk_z", "accepted", "remaining_chunks", "note");
    }

    private static void ValidateServerStatusDescriptors()
    {
        var status = RequireDescriptor(nameof(ServerStatusResponse));
        EnsureFields(
            status,
            "server_version",
            "protocol_version",
            "online_players",
            "max_players",
            "server_tps",
            "server_uptime",
            "motd",
            "world_info",
            "container_hash_mismatches",
            "total_tracked_chunks",
            "active_chunk_residency_players",
            "peak_chunks_per_player",
            "busiest_chunk_player",
            "total_deaths",
            "total_respawns",
            "deaths_last_ten_minutes");
    }

    private static void ValidateEntityDescriptors()
    {
        var entityData = RequireDescriptor(nameof(EntityData));
        EnsureFields(entityData, "entity_id", "entity_type", "position", "rotation", "velocity", "health", "max_health", "metadata");

        var entitySpawn = RequireDescriptor(nameof(EntitySpawnBroadcast));
        EnsureFields(entitySpawn, "entity", "spawn_reason");

        var entityDespawn = RequireDescriptor(nameof(EntityDespawnBroadcast));
        EnsureFields(entityDespawn, "entity_id", "reason");
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
        var prototypeTypes = new Dictionary<Type, MinecraftMessageType>();

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

            Type prototypeType = prototype.GetType();
            if (prototypeTypes.TryGetValue(prototypeType, out var existingBinding) && existingBinding != messageType)
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contracts '{existingBinding}' and '{messageType}' are bound to the same CLR type '{prototypeType.FullName}'. Regenerate protobuf assets so each message uses its own generated class and using directive.");
            }

            prototypeTypes[prototypeType] = messageType;

            string prototypeNamespace = descriptor.ClrType?.Namespace ?? prototypeType.Namespace ?? string.Empty;
            if (!string.Equals(prototypeNamespace, expectedNamespace, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{messageType}' was generated into namespace '{prototypeNamespace}', expected '{expectedNamespace}'. Check using directives or regenerate protobuf assets so server and Unity share the same namespace.");
            }

            Assembly expectedAssembly = typeof(EnhancedMinecraftGameReflection).Assembly;
            Assembly prototypeAssembly = prototypeType.Assembly;
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
            if (!ReferenceEquals(prototypeType.Assembly, expectedAssembly))
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

        // Do not require every generated protobuf message to have a ProtocolRegistry binding.
        // Only network-level packets (mapped to MinecraftMessageType) must be registered; many
        // helper contracts (e.g., PlayerStats, nested metadata messages) are referenced through
        // those packet roots and should not be forced into the registry.
    }

    private static void ValidateDescriptorFiles()
    {
        string expectedFile = EnhancedMinecraftGameReflection.Descriptor.Name ?? string.Empty;
        foreach (var binding in ProtocolRegistry.RegisteredDescriptors)
        {
            var messageDescriptor = RequireDescriptor(binding.DescriptorName);
            string descriptorFile = messageDescriptor.File?.Name ?? string.Empty;

            if (string.IsNullOrWhiteSpace(descriptorFile) || string.IsNullOrWhiteSpace(expectedFile))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.DescriptorName}' is missing file metadata. Regenerate protobuf assets so using directives bind to the generated DTOs.");
            }

            if (!string.Equals(descriptorFile, expectedFile, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.DescriptorName}' resolved from '{descriptorFile}', expected '{expectedFile}'. Regenerate protobuf assets so server and Unity reference the same generated file.");
            }
        }
    }

    private static void ValidatePrototypeDescriptorFiles()
    {
        string expectedFile = EnhancedMinecraftGameReflection.Descriptor.Name ?? string.Empty;
        foreach (var binding in ProtocolRegistry.RegisteredDescriptors)
        {
            if (!ProtocolRegistry.TryCreatePrototype(binding.MessageType, out IMessage prototype))
            {
                continue;
            }

            string descriptorFile = prototype.Descriptor?.File?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(descriptorFile))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.MessageType}' did not expose a descriptor file. Regenerate protobuf assets and ensure using directives point at the generated DTOs.");
            }

            if (!string.IsNullOrWhiteSpace(expectedFile) &&
                !string.Equals(descriptorFile, expectedFile, StringComparison.OrdinalIgnoreCase))
            {
                string mismatch = $"EnhancedMinecraft contract '{binding.MessageType}' resolved from descriptor file '{descriptorFile}', expected '{expectedFile}'.";
                if (IsOptionalMessage(binding.MessageType))
                {
                    Console.WriteLine($"[Proto][WARN] {mismatch} Regenerate protobuf assets and update using references before promoting this packet to required.");
                }
                else
                {
                    throw new InvalidOperationException($"{mismatch} Regenerate protobuf assets so server and Unity share the same generated EnhancedMinecraft DTOs.");
                }
            }
        }
    }

    private static void ValidateDescriptorAssemblies()
    {
        Assembly expectedAssembly = typeof(EnhancedMinecraftGameReflection).Assembly;
        foreach (var binding in ProtocolRegistry.RegisteredDescriptors)
        {
            if (!ProtocolRegistry.TryCreatePrototype(binding.MessageType, out IMessage prototype))
            {
                continue;
            }

            Assembly? actualAssembly = prototype.Descriptor?.ClrType?.Assembly;
            if (actualAssembly != null && expectedAssembly != null && !ReferenceEquals(actualAssembly, expectedAssembly))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.MessageType}' resolved from assembly '{actualAssembly.GetName().Name}', expected '{expectedAssembly.GetName().Name}'. Regenerate protobuf assets so using directives bind to the generated DTOs from the current build.");
            }
        }
    }

    private static void ValidateDescriptorOrigins()
    {
        Assembly expectedAssembly = typeof(EnhancedMinecraftGameReflection).Assembly;
        string? expectedFile = EnhancedMinecraftGameReflection.Descriptor?.Name;

        foreach (var messageType in ProtocolRegistry.RegisteredMessageTypes)
        {
            if (!ProtocolRegistry.TryResolveContractType(messageType, out var contractType) || contractType == null)
            {
                continue;
            }

            if (!ReferenceEquals(contractType.Assembly, expectedAssembly))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{messageType}' resolved from assembly '{contractType.Assembly.GetName().Name}', expected '{expectedAssembly.GetName().Name}'. Regenerate protobuf assets or update using directives so server and Unity share the generated assembly.");
            }

            var descriptor = contractType.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as MessageDescriptor;
            string? descriptorFile = descriptor?.File?.Name;
            if (!string.IsNullOrWhiteSpace(expectedFile) &&
                !string.IsNullOrWhiteSpace(descriptorFile) &&
                !string.Equals(descriptorFile, expectedFile, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{messageType}' resolved from descriptor '{descriptorFile}', expected '{expectedFile}'. Regenerate protobuf assets so both server and Unity reference the same generated file.");
            }
        }
    }

    private static void ValidateDescriptorNamespaces()
    {
        string expectedNamespace = typeof(EnhancedMinecraftGameReflection).Namespace ?? "EnhancedMinecraftProtocol";
        foreach (var binding in ProtocolRegistry.RegisteredDescriptors)
        {
            if (!ProtocolRegistry.TryResolveContractType(binding.MessageType, out var contractType) || contractType == null)
            {
                continue;
            }

            string contractNamespace = contractType.Namespace ?? string.Empty;
            if (string.IsNullOrWhiteSpace(contractNamespace) || !contractNamespace.Contains(expectedNamespace, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.MessageType}' resolved from namespace '{contractNamespace}', expected to include '{expectedNamespace}'. Regenerate protobuf assets or update using directives so generated DTOs remain reachable.");
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

    private static void ValidateRegistryBindingNames()
    {
        var duplicateDescriptors = ProtocolRegistry.RegisteredDescriptors
            .GroupBy(binding => binding.DescriptorName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateDescriptors.Length > 0)
        {
            throw new InvalidOperationException(
                $"EnhancedMinecraft registry maps multiple message types to the same descriptor: {string.Join(", ", duplicateDescriptors)}. Update ProtocolRegistry so each packet targets a unique generated contract.");
        }

        foreach (var binding in ProtocolRegistry.RegisteredDescriptors)
        {
            if (!ProtocolRegistry.TryCreatePrototype(binding.MessageType, out IMessage? prototype))
            {
                continue;
            }

            string descriptorName = prototype.Descriptor?.Name ?? string.Empty;
            if (!string.Equals(binding.DescriptorName, descriptorName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft registry binding for '{binding.MessageType}' expected descriptor '{binding.DescriptorName}', resolved '{descriptorName}'. Regenerate protobuf assets or update using directives so generated contracts stay aligned.");
            }
        }
    }

    private static void ValidateEnumBindings()
    {
        var missing = new List<MinecraftMessageType>();

        foreach (var messageType in Enum.GetValues(typeof(MinecraftMessageType)).Cast<MinecraftMessageType>())
        {
            if (OptionalMessages.Contains(messageType))
            {
                continue;
            }

            if (!ProtocolRegistry.IsRegistered(messageType))
            {
                missing.Add(messageType);
            }
        }

        if (missing.Count > 0)
        {
            string joined = string.Join(", ", missing);
            throw new InvalidOperationException(
                $"EnhancedMinecraft protocol registry is missing bindings for: {joined}. Add ProtocolRegistry entries or mark them optional so generated protobuf classes remain reachable via using directives.");
        }
    }

    private static void ValidateOptionalDescriptorVisibility()
    {
        foreach (var messageType in OptionalMessages)
        {
            bool generated = EnhancedMinecraftGameReflection.Descriptor
                .MessageTypes
                .Any(d => d.Name == messageType.ToString());

            if (!generated)
            {
                Console.WriteLine($"[Proto][WARN] Optional EnhancedMinecraft packet '{messageType}' is not present in generated descriptors. Run protoc to keep optional bindings reachable when promoted to required.");
            }
        }
    }

    private static void ValidateUniqueBindings()
    {
        var duplicate = ProtocolRegistry.RegisteredDescriptors
            .GroupBy(binding => binding.DescriptorName, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate != null)
        {
            string messageTypes = string.Join(", ", duplicate.Select(binding => binding.MessageType));
            throw new InvalidOperationException(
                $"EnhancedMinecraft descriptor '{duplicate.Key}' is bound to multiple message types ({messageTypes}). Update ProtocolRegistry so each protobuf contract maps to a single message type and using directive.");
        }
    }
}
