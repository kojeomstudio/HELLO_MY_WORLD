using System;
using System.Collections.Generic;
using SharedBlockType = GameCommon.Blocks.BlockType;

namespace GameServerApp.Models
{
    /// <summary>
    /// Server-local block IDs and protocol/shared block IDs conversion layer.
    /// Keeps existing server storage IDs intact while normalizing network payloads.
    /// </summary>
    public static class BlockTypeProtocolMapper
    {
        private const ushort MaxLegacyServerBlockTypeId = (ushort)BlockType.Clay;

        private static readonly Dictionary<BlockType, ushort> ServerToProtocolMap = new()
        {
            [BlockType.Air] = (ushort)SharedBlockType.Air,
            [BlockType.Stone] = (ushort)SharedBlockType.Stone,
            [BlockType.Grass] = (ushort)SharedBlockType.Grass,
            [BlockType.Dirt] = (ushort)SharedBlockType.Dirt,
            [BlockType.Cobblestone] = (ushort)SharedBlockType.Cobblestone,
            [BlockType.Wood] = (ushort)SharedBlockType.Wood,
            [BlockType.Leaves] = (ushort)SharedBlockType.Leaves,
            [BlockType.Sand] = (ushort)SharedBlockType.Sand,
            [BlockType.Water] = (ushort)SharedBlockType.Water,
            [BlockType.Lava] = (ushort)SharedBlockType.Lava,
            [BlockType.Bedrock] = (ushort)SharedBlockType.Bedrock,
            [BlockType.CoalOre] = (ushort)SharedBlockType.CoalOre,
            [BlockType.IronOre] = (ushort)SharedBlockType.IronOre,
            [BlockType.GoldOre] = (ushort)SharedBlockType.GoldOre,
            [BlockType.DiamondOre] = (ushort)SharedBlockType.DiamondOre,
            [BlockType.TallGrass] = (ushort)SharedBlockType.TallGrass,
            [BlockType.DeadBush] = (ushort)SharedBlockType.DeadBush,
            [BlockType.Ice] = (ushort)SharedBlockType.Ice,
            [BlockType.Snow] = (ushort)SharedBlockType.Snow,
            [BlockType.Cloud] = (ushort)SharedBlockType.Air,
            [BlockType.Clay] = (ushort)SharedBlockType.Clay
        };

        private static readonly Dictionary<ushort, BlockType> ProtocolToServerMap = BuildProtocolToServerMap();

        public static bool TryProtocolToServer(int rawBlockType, out BlockType serverBlockType)
        {
            serverBlockType = BlockType.Air;
            if (rawBlockType < 0 || rawBlockType > ushort.MaxValue)
            {
                return false;
            }

            ushort protocolId = (ushort)rawBlockType;
            if (ProtocolToServerMap.TryGetValue(protocolId, out serverBlockType))
            {
                return true;
            }

            if (Enum.IsDefined(typeof(BlockType), protocolId))
            {
                serverBlockType = (BlockType)protocolId;
                return true;
            }

            return false;
        }

        public static ushort ToProtocol(BlockType serverBlockType)
        {
            if (ServerToProtocolMap.TryGetValue(serverBlockType, out ushort protocolId))
            {
                return protocolId;
            }

            return (ushort)SharedBlockType.Air;
        }

        public static ushort ToProtocol(int rawBlockType)
        {
            if (TryProtocolToServer(rawBlockType, out BlockType serverBlockType))
            {
                return ToProtocol(serverBlockType);
            }

            return (ushort)SharedBlockType.Air;
        }

        public static byte[] ConvertChunkBlockDataToProtocol(ReadOnlySpan<byte> serverBlockData)
        {
            if (serverBlockData.Length % 2 != 0)
            {
                throw new FormatException("Chunk block data payload must be 2-byte aligned.");
            }

            var protocolBlockData = new byte[serverBlockData.Length];
            for (int index = 0; index < serverBlockData.Length; index += 2)
            {
                ushort serverId = (ushort)(serverBlockData[index] | (serverBlockData[index + 1] << 8));
                BlockType serverBlockType = serverId <= MaxLegacyServerBlockTypeId
                    ? (BlockType)serverId
                    : BlockType.Air;

                ushort protocolId = ToProtocol(serverBlockType);
                protocolBlockData[index] = (byte)(protocolId & 0xFF);
                protocolBlockData[index + 1] = (byte)((protocolId >> 8) & 0xFF);
            }

            return protocolBlockData;
        }

        private static Dictionary<ushort, BlockType> BuildProtocolToServerMap()
        {
            var map = new Dictionary<ushort, BlockType>();
            foreach (var pair in ServerToProtocolMap)
            {
                if (!map.ContainsKey(pair.Value))
                {
                    map[pair.Value] = pair.Key;
                }
            }

            return map;
        }
    }
}
