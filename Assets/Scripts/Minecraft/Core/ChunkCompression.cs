using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Minecraft.Core
{
    /// <summary>
    /// Utility helpers for decoding chunk block payloads sent by the server.
    /// Handles optional GZip compression and validates output size.
    /// </summary>
    internal static class ChunkCompression
    {
        private const int ChunkBlockCount = 16 * 16 * 256;

        public static byte[] DecodeBlocks(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
            {
                return new byte[ChunkBlockCount];
            }

            byte[] rawData;
            if (LooksLikeGZip(compressedData))
            {
                try
                {
                    rawData = DecompressGZip(compressedData);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to decompress chunk data: {ex.Message}");
                    rawData = new byte[ChunkBlockCount];
                }
            }
            else
            {
                rawData = CopyBuffer(compressedData);
            }

            if (rawData.Length != ChunkBlockCount)
            {
                Array.Resize(ref rawData, ChunkBlockCount);
            }

            return rawData;
        }

        private static bool LooksLikeGZip(byte[] data)
        {
            return data.Length > 2 && data[0] == 0x1F && data[1] == 0x8B;
        }

        private static byte[] DecompressGZip(byte[] data)
        {
            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream(ChunkBlockCount);
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] CopyBuffer(byte[] source)
        {
            var buffer = new byte[source.Length];
            Buffer.BlockCopy(source, 0, buffer, 0, source.Length);
            return buffer;
        }
    }
}
