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
        ProtoFingerprint.AssertDescriptorFingerprint();

        foreach (var message in RequiredMessages)
        {
            ProtocolRegistry.EnsureRegistered(message);
        }

        ValidateChunkDescriptor();
        ValidateChunkRequestAndResponseDescriptors();
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
}
