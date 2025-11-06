using System;
using System.Collections.Generic;
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
    private static readonly IReadOnlyDictionary<MinecraftMessageType, Func<IMessage>> _prototypes =
        new Dictionary<MinecraftMessageType, Func<IMessage>>
        {
            { MinecraftMessageType.ChunkDataRequest, () => new ChunkLoadRequest() },
            { MinecraftMessageType.ChunkDataResponse, () => new ChunkLoadResponse() },
            { MinecraftMessageType.ChunkUnloadNotification, () => new ChunkUnloadNotification() },
            { MinecraftMessageType.ChunkUnloadAcknowledge, () => new ChunkUnloadAck() }
        };

    /// <summary>
    /// Returns <c>true</c> if the message type is backed by a generated protobuf contract.
    /// </summary>
    public static bool IsRegistered(MinecraftMessageType messageType) =>
        _prototypes.ContainsKey(messageType);

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
        if (_prototypes.TryGetValue(messageType, out var factory))
        {
            prototype = factory();
            return true;
        }

        prototype = default!;
        return false;
    }

    /// <summary>
    /// Enumerates the currently registered message types. Useful for auditing regeneration output.
    /// </summary>
    public static IEnumerable<MinecraftMessageType> RegisteredMessageTypes => _prototypes.Keys;
}
