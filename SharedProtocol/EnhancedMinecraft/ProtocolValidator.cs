using System;
using System.Linq;
using EnhancedMinecraftProtocol;
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
        MinecraftMessageType.ChunkDataRequest,
        MinecraftMessageType.ChunkDataResponse,
        MinecraftMessageType.ChunkUnloadNotification,
        MinecraftMessageType.ChunkUnloadAcknowledge
    };

    public static void ValidateEnhancedContracts()
    {
        foreach (var message in RequiredMessages)
        {
            ProtocolRegistry.EnsureRegistered(message);
        }

        ValidateChunkDescriptor();
    }

    private static void ValidateChunkDescriptor()
    {
        MessageDescriptor? chunkDescriptor = EnhancedMinecraftGameReflection.Descriptor
            .MessageTypes
            .FirstOrDefault(descriptor => descriptor.Name == nameof(ChunkData));

        if (chunkDescriptor == null)
        {
            throw new InvalidOperationException(
                "EnhancedMinecraftProtocol.ChunkData descriptor is missing. Regenerate proto assets.");
        }

        foreach (string fieldName in new[] { "block_data", "biome_data" })
        {
            if (chunkDescriptor.FindFieldByName(fieldName) == null)
            {
                throw new InvalidOperationException(
                    $"ChunkData descriptor missing required field '{fieldName}'. Regenerate proto assets.");
            }
        }
    }
}
