using System;
using System.Collections.Generic;
using System.Linq;
using EnhancedMinecraftProtocol;
using Google.Protobuf;

namespace SharedProtocol.EnhancedMinecraft;

/// <summary>
/// Central registry that links <see cref="MinecraftMessageType"/> values with the generated
/// EnhancedMinecraft protobuf message prototypes. This provides a single source of truth
/// so both the server and client can verify that regenerated contracts are wired in correctly.
/// </summary>
public sealed record ProtocolBindingDiagnostic(
    MinecraftMessageType MessageType,
    string DescriptorName,
    string DescriptorPackage,
    string ClrType);

public static class ProtocolRegistry
{
    private sealed record ProtocolBinding(MinecraftMessageType MessageType, string DescriptorName, Func<IMessage> Factory);

    private static readonly ProtocolBinding[] Bindings =
    {
        new(MinecraftMessageType.PlayerStateUpdate, nameof(EnhancedMinecraftProtocol.PlayerInfo), () => new EnhancedMinecraftProtocol.PlayerInfo()),
        new(MinecraftMessageType.PlayerActionRequest, nameof(EnhancedMinecraftProtocol.PlayerActionRequest), () => new EnhancedMinecraftProtocol.PlayerActionRequest()),
        new(MinecraftMessageType.PlayerActionResponse, nameof(EnhancedMinecraftProtocol.PlayerActionResponse), () => new EnhancedMinecraftProtocol.PlayerActionResponse()),
        new(MinecraftMessageType.ChunkDataRequest, nameof(EnhancedMinecraftProtocol.ChunkLoadRequest), () => new EnhancedMinecraftProtocol.ChunkLoadRequest()),
        new(MinecraftMessageType.ChunkDataResponse, nameof(EnhancedMinecraftProtocol.ChunkLoadResponse), () => new EnhancedMinecraftProtocol.ChunkLoadResponse()),
        new(MinecraftMessageType.ChunkUnloadNotification, nameof(EnhancedMinecraftProtocol.ChunkUnloadNotification), () => new EnhancedMinecraftProtocol.ChunkUnloadNotification()),
        new(MinecraftMessageType.ChunkUnloadAcknowledge, nameof(EnhancedMinecraftProtocol.ChunkUnloadAck), () => new EnhancedMinecraftProtocol.ChunkUnloadAck()),
        new(MinecraftMessageType.BlockChangeNotification, nameof(EnhancedMinecraftProtocol.BlockChangeBroadcast), () => new EnhancedMinecraftProtocol.BlockChangeBroadcast()),
        new(MinecraftMessageType.EntitySpawn, nameof(EnhancedMinecraftProtocol.EntitySpawnBroadcast), () => new EnhancedMinecraftProtocol.EntitySpawnBroadcast()),
        new(MinecraftMessageType.EntityDespawn, nameof(EnhancedMinecraftProtocol.EntityDespawnBroadcast), () => new EnhancedMinecraftProtocol.EntityDespawnBroadcast()),
        new(MinecraftMessageType.TimeUpdate, nameof(EnhancedMinecraftProtocol.TimeUpdateBroadcast), () => new EnhancedMinecraftProtocol.TimeUpdateBroadcast()),
        new(MinecraftMessageType.WeatherChange, nameof(EnhancedMinecraftProtocol.WeatherUpdateBroadcast), () => new EnhancedMinecraftProtocol.WeatherUpdateBroadcast()),
        new(MinecraftMessageType.SoundEffect, nameof(EnhancedMinecraftProtocol.SoundEffect), () => new EnhancedMinecraftProtocol.SoundEffect()),
        new(MinecraftMessageType.ParticleEffect, nameof(EnhancedMinecraftProtocol.ParticleEffect), () => new EnhancedMinecraftProtocol.ParticleEffect())
    };

    private static readonly IReadOnlyDictionary<MinecraftMessageType, ProtocolBinding> BindingsByType =
        Bindings.ToDictionary(binding => binding.MessageType);

    private static readonly HashSet<MinecraftMessageType> OptionalMessageTypes = new()
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

    /// <summary>
    /// Returns <c>true</c> if the message type is backed by a generated protobuf contract.
    /// </summary>
    public static bool IsRegistered(MinecraftMessageType messageType) =>
        BindingsByType.ContainsKey(messageType);

    public static IReadOnlyCollection<MinecraftMessageType> GetUnregisteredRequiredMessages()
    {
        var all = Enum.GetValues(typeof(MinecraftMessageType)).Cast<MinecraftMessageType>();
        return all
            .Where(type => !IsRegistered(type) && !OptionalMessageTypes.Contains(type))
            .ToArray();
    }

    public static IReadOnlyCollection<MinecraftMessageType> GetOptionalMessagesWithoutBindings()
    {
        return ProtocolValidator.GetOptionalMessages()
            .Where(type => !IsRegistered(type))
            .ToArray();
    }

    public static bool IsOptionalMessageType(MinecraftMessageType messageType) =>
        OptionalMessageTypes.Contains(messageType);

    /// <summary>
    /// Throws if the provided message type is not registered. This is useful for early validation
    /// during handler registration, ensuring stale IDL changes are caught in development.
    /// </summary>
    public static void EnsureRegistered(MinecraftMessageType messageType)
    {
        if (!IsRegistered(messageType))
        {
            throw new InvalidOperationException(
                $"EnhancedMinecraft protocol message '{messageType}' is not registered. " +
                "Regenerate protobuf assets or update ProtocolRegistry to include the new contract.");
        }
    }

    /// <summary>
    /// Attempts to create a fresh message instance for diagnostics or reflection driven workflows.
    /// </summary>
    public static bool TryCreatePrototype(MinecraftMessageType messageType, out IMessage prototype)
    {
        if (BindingsByType.TryGetValue(messageType, out var binding))
        {
            prototype = binding.Factory();
            return true;
        }

        prototype = default!;
        return false;
    }

    /// <summary>
    /// Enumerates the currently registered message types. Useful for auditing regeneration output.
    /// </summary>
    public static IEnumerable<MinecraftMessageType> RegisteredMessageTypes => BindingsByType.Keys;

    internal static IEnumerable<(MinecraftMessageType MessageType, string DescriptorName)> RegisteredDescriptors =>
        Bindings.Select(binding => (binding.MessageType, binding.DescriptorName));

    /// <summary>
    /// Returns generated protobuf message descriptors that are currently not mapped by ProtocolRegistry.
    /// This helps validate that newly generated contracts are intentionally optional or still pending wiring.
    /// </summary>
    public static IReadOnlyCollection<string> GetGeneratedDescriptorsWithoutBindings()
    {
        var generated = EnhancedMinecraftGameReflection.Descriptor?.MessageTypes
            .Select(descriptor => descriptor.Name)
            .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
        var bound = Bindings
            .Select(binding => binding.DescriptorName)
            .ToHashSet(StringComparer.Ordinal);

        return generated
            .Where(name => !bound.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns generated descriptor names for diagnostics and report generation.
    /// </summary>
    public static IReadOnlyCollection<string> GetGeneratedDescriptorNames()
    {
        return EnhancedMinecraftGameReflection.Descriptor?.MessageTypes
            .Select(descriptor => descriptor.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Returns per-binding descriptor diagnostics to help track registry-to-protobuf mapping drift.
    /// </summary>
    public static IReadOnlyCollection<ProtocolBindingDiagnostic> GetBindingDiagnostics()
    {
        return Bindings
            .Select(binding =>
            {
                var prototype = binding.Factory();
                var descriptor = prototype.Descriptor;
                return new ProtocolBindingDiagnostic(
                    binding.MessageType,
                    binding.DescriptorName,
                    descriptor?.File?.Package ?? string.Empty,
                    descriptor?.ClrType?.FullName ?? string.Empty);
            })
            .OrderBy(diagnostic => diagnostic.MessageType.ToString(), StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Returns protocol binding coverage against generated EnhancedMinecraft descriptors.
    /// </summary>
    public static (int BoundDescriptors, int GeneratedDescriptors) GetBindingCoverage()
    {
        int generated = EnhancedMinecraftGameReflection.Descriptor?.MessageTypes.Count ?? 0;
        int bound = Bindings.Length;
        return (bound, generated);
    }

    public static void ValidateBindings()
    {
        ProtoFingerprint.AssertDescriptorFingerprint();

        if (EnhancedMinecraftGameReflection.Descriptor == null)
        {
            throw new InvalidOperationException(
                "EnhancedMinecraftGameReflection.Descriptor is null. Ensure generated Google.Protobuf DTOs are referenced and initialized before registering bindings.");
        }

        var duplicateDescriptors = Bindings
            .GroupBy(binding => binding.DescriptorName, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToArray();
        if (duplicateDescriptors.Length > 0)
        {
            throw new InvalidOperationException(
                "EnhancedMinecraft protocol registry has duplicate descriptor bindings: " +
                string.Join(", ", duplicateDescriptors.Select(group => group.Key)) +
                ". Update ProtocolRegistry so each MinecraftMessageType maps to a distinct generated DTO.");
        }

        var descriptorNames = EnhancedMinecraftGameReflection.Descriptor?.MessageTypes
            .Select(descriptor => descriptor.Name)
            .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
        if (descriptorNames.Count == 0)
        {
            throw new InvalidOperationException(
                "EnhancedMinecraft descriptor set is empty. Ensure Assets/Generated/Protobuf/EnhancedMinecraftGame.cs is referenced and protoc outputs are up to date.");
        }

        foreach (var binding in Bindings)
        {
            var prototype = binding.Factory();
            var descriptorName = prototype.Descriptor?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(descriptorName))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.MessageType}' is missing a descriptor. Regenerate protobuf assets.");
            }

            if (!descriptorNames.Contains(binding.DescriptorName))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.MessageType}' expects generated descriptor '{binding.DescriptorName}' but it is missing from EnhancedMinecraftGameReflection. Regenerate protoc outputs or update using directives so the registry binds to the current generated DTOs.");
            }

            if (!string.Equals(binding.DescriptorName, descriptorName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract mismatch for {binding.MessageType}: expected '{binding.DescriptorName}' but generated '{descriptorName}'. Regenerate protobuf assets so SharedProtocol and Unity stay aligned.");
            }

            string expectedPackage = EnhancedMinecraftGameReflection.Descriptor?.Package ?? string.Empty;
            var descriptorFile = prototype.Descriptor?.File;
            if (descriptorFile == null)
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.MessageType}' has no descriptor file. Ensure Google.Protobuf generated DTOs are referenced via using directives.");
            }

            var parser = prototype.Descriptor?.Parser;
            if (parser == null)
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.MessageType}' is missing a parser. Regenerate protobuf DTOs so ProtocolRegistry factories return the generated message types.");
            }

            string actualPackage = descriptorFile.Package ?? string.Empty;
            if (!string.Equals(actualPackage, expectedPackage, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                $"EnhancedMinecraft contract '{binding.MessageType}' is sourced from package '{actualPackage}', expected '{expectedPackage}'. Regenerate protobuf DTOs or fix using directives so registry bindings point at the current protoc output.");
            }
        }

        EnsureRequiredBindings();

        var optionalMissing = GetUnregisteredOptionalTypes().ToArray();
        if (optionalMissing.Length > 0)
        {
            Console.WriteLine(
                "[Proto][INFO] Optional EnhancedMinecraft message bindings missing: " +
                string.Join(", ", optionalMissing) +
                ". Register bindings or regenerate DTOs if these packets become required.");
        }
    }

    public static bool TryResolveContractType(MinecraftMessageType messageType, out Type? contractType)
    {
        contractType = null;
        if (BindingsByType.TryGetValue(messageType, out var binding))
        {
            var prototype = binding.Factory();
            contractType = prototype.Descriptor?.ClrType;
            return contractType != null;
        }

        return false;
    }

    /// <summary>
    /// Returns unregistered message types (required + optional) to help audits.
    /// </summary>
    public static IEnumerable<MinecraftMessageType> GetUnregisteredMessageTypes()
    {
        return Enum.GetValues<MinecraftMessageType>()
            .Where(messageType => !IsRegistered(messageType));
    }

    /// <summary>
    /// Returns only the optional message types that are not bound to generated DTOs.
    /// </summary>
    public static IEnumerable<MinecraftMessageType> GetUnregisteredOptionalTypes()
    {
        return OptionalMessageTypes.Where(messageType => !IsRegistered(messageType));
    }

    /// <summary>
    /// Throws if any required (non-optional) message type is missing from the registry.
    /// </summary>
    public static void EnsureRequiredBindings()
    {
        var missing = Enum.GetValues<MinecraftMessageType>()
            .Where(messageType => !OptionalMessageTypes.Contains(messageType) && !IsRegistered(messageType))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "EnhancedMinecraft protocol registry is missing required bindings: " +
                string.Join(", ", missing) +
                ". Regenerate protoc DTOs or update ProtocolRegistry.");
        }
    }
}
