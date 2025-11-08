using System;
using System.Linq;
using EnhancedMinecraftProtocol;
using Minecraft.World;
using SharedProtocol;

namespace Minecraft.Core
{
    internal static class EnhancedChunkPayloadBridge
    {
        public static EnhancedChunkMetadata Decode(ChunkDataResponseMessage response, Action<string>? logWarning = null)
        {
            if (response.EnhancedPayload == null || response.EnhancedPayload.Length == 0)
            {
                return EnhancedChunkMetadata.Empty;
            }

            try
            {
                var envelope = ChunkLoadResponse.Parser.ParseFrom(response.EnhancedPayload);
                var chunk = envelope.Chunks.FirstOrDefault(c => c.ChunkX == response.ChunkX && c.ChunkZ == response.ChunkZ)
                            ?? envelope.Chunks.FirstOrDefault();

                if (chunk == null)
                {
                    logWarning?.Invoke($"Enhanced payload missing chunk entry for ({response.ChunkX}, {response.ChunkZ}).");
                    return EnhancedChunkMetadata.Empty;
                }

                if (response.CompressedBlockData?.Length > 0 &&
                    chunk.BlockData.Length > 0 &&
                    response.CompressedBlockData.Length != chunk.BlockData.Length)
                {
                    logWarning?.Invoke(
                        $"Enhanced chunk block data mismatch for ({response.ChunkX}, {response.ChunkZ}) " +
                        $"shared={response.CompressedBlockData.Length} proto={chunk.BlockData.Length}.");
                }

                return new EnhancedChunkMetadata(
                    chunk.GenerationTimestamp,
                    envelope.TotalRequested,
                    envelope.TotalSent,
                    chunk.BlockData.Length,
                    chunk.BiomeData.Length,
                    chunk);
            }
            catch (Exception ex)
            {
                logWarning?.Invoke($"Failed to parse enhanced chunk payload ({ex.Message}).");
                return EnhancedChunkMetadata.Empty;
            }
        }
    }
}
