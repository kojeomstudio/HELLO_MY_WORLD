using System;
using EnhancedMinecraftProtocol;
using Google.Protobuf;

namespace SharedProtocol.EnhancedMinecraft;

/// <summary>
/// Helper utilities that bridge server-side chunk data into the generated protobuf contracts.
/// This allows consumers to validate payloads against the authoritative IDL before wiring them
/// into the network stack.
/// </summary>
public static class ChunkPayloadBuilder
{
    static ChunkPayloadBuilder()
    {
        ProtocolValidator.ValidateEnhancedContracts();
    }

    public static ChunkData BuildChunkData(
        int chunkX,
        int chunkZ,
        ReadOnlySpan<byte> compressedBlockData,
        ReadOnlySpan<byte> biomeData,
        long generationTimestamp)
    {
        var chunk = new ChunkData
        {
            ChunkX = chunkX,
            ChunkZ = chunkZ,
            BlockData = ByteString.CopyFrom(compressedBlockData),
            BiomeData = ByteString.CopyFrom(biomeData),
            LightData = ByteString.Empty,
            GenerationTimestamp = generationTimestamp
        };

        return chunk;
    }

    public static ChunkLoadResponse BuildLoadResponse(
        int chunkX,
        int chunkZ,
        ReadOnlySpan<byte> compressedBlockData,
        ReadOnlySpan<byte> biomeData,
        long generationTimestamp,
        int totalRequested = 1)
    {
        var chunk = BuildChunkData(chunkX, chunkZ, compressedBlockData, biomeData, generationTimestamp);
        var response = new ChunkLoadResponse
        {
            TotalRequested = totalRequested,
            TotalSent = 1
        };
        response.Chunks.Add(chunk);
        return response;
    }

    /// <summary>
    /// Serialises the response to ensure every required field matches the generated contract.
    /// A <see cref="FormatException"/> is thrown if the payload cannot be serialised.
    /// </summary>
    public static void ValidateChunkPayload(
        int chunkX,
        int chunkZ,
        ReadOnlySpan<byte> compressedBlockData,
        ReadOnlySpan<byte> biomeData)
    {
        var response = BuildLoadResponse(
            chunkX,
            chunkZ,
            compressedBlockData,
            biomeData,
            generationTimestamp: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        try
        {
            // Force materialisation to catch descriptor or field mismatches early.
            _ = response.CalculateSize();
        }
        catch (Exception ex)
        {
            throw new FormatException(
                $"Failed to validate EnhancedMinecraftProtocol chunk payload for [{chunkX}, {chunkZ}].",
                ex);
        }
    }
}
