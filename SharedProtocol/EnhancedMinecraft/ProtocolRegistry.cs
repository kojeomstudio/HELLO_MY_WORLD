using System;
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

    /// <summary>
    /// Returns <c>true</c> if the message type is backed by a generated protobuf contract.
    /// </summary>
    public static bool IsRegistered(MinecraftMessageType messageType) =>
        BindingsByType.ContainsKey(messageType);

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

    public static void ValidateBindings()
    {
        foreach (var binding in Bindings)
        {
            var prototype = binding.Factory();
            var descriptorName = prototype.Descriptor?.Name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(descriptorName))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract '{binding.MessageType}' is missing a descriptor. Regenerate protobuf assets.");
            }

            if (!string.Equals(binding.DescriptorName, descriptorName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"EnhancedMinecraft contract mismatch for {binding.MessageType}: expected '{binding.DescriptorName}' but generated '{descriptorName}'. Regenerate protobuf assets so SharedProtocol and Unity stay aligned.");
            }
        }
    }
}
