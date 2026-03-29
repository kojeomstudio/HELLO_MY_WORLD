using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minecraft.Core
{
    /// <summary>
    /// Block definition data structure
    /// </summary>
    [Serializable]
    public class BlockDefinition
    {
        public int Id;
        public string Name;
        public string DisplayName;
        public bool Solid;
        public bool Transparent;
        public bool LightPassing;
        public bool CanBreak;
        public bool CanPlace;
        public bool HasGravity;
        public int LightLevel;
        public float Resistance;
        public float Hardness;
        public BlockTexture Texture;
        public List<ItemDrop> Drops;
        public string Tool;
        public BlockSounds Sounds;
    }

    [Serializable]
    public class BlockTexture
    {
        public string Top;
        public string Bottom;
        public string Sides;
    }

    [Serializable]
    public class ItemDrop
    {
        public string Item;
        public int Count;
        public float Chance;
    }

    [Serializable]
    public class BlockSounds
    {
        public string Break;
        public string Place;
        public string Step;
    }

    [Serializable]
    public class ToolDefinition
    {
        public string Name;
        public float Efficiency;
        public List<string> CanHarvest;
    }

    [Serializable]
    public class BlocksData
    {
        public Dictionary<string, BlockDefinition> BlockTypes = new();
        public Dictionary<string, ToolDefinition> ToolTypes = new();
    }

    /// <summary>
    /// Data-driven block manager that loads and provides access to block definitions
    /// </summary>
    public static class BlockDataManager
    {
        private static BlocksData _blocksData;
        private static Dictionary<int, BlockDefinition> _blocksById = new();
        private static Dictionary<string, BlockDefinition> _blocksByName = new();
        private static readonly string BlocksDataPath = Path.Combine(Application.streamingAssetsPath, "blocks.json");

        /// <summary>
        /// Gets the loaded blocks data
        /// </summary>
        public static BlocksData Blocks
        {
            get
            {
                if (_blocksData == null)
                {
                    LoadBlocksData();
                }
                return _blocksData;
            }
        }

        /// <summary>
        /// Loads blocks data from JSON file
        /// </summary>
        public static void LoadBlocksData()
        {
            try
            {
                if (File.Exists(BlocksDataPath))
                {
                    string jsonContent = File.ReadAllText(BlocksDataPath);
                    _blocksData = JsonUtility.FromJson<BlocksData>(jsonContent);
                    
                    // Build lookup dictionaries
                    _blocksById.Clear();
                    _blocksByName.Clear();
                    
                    foreach (var kvp in _blocksData.BlockTypes)
                    {
                        var block = kvp.Value;
                        _blocksById[block.Id] = block;
                        _blocksByName[block.Name] = block;
                    }
                    
                    Debug.Log($"[BlockDataManager] Loaded {_blocksData.BlockTypes.Count} block types and {_blocksData.ToolTypes.Count} tool types");
                }
                else
                {
                    Debug.LogWarning($"[BlockDataManager] Blocks data file not found at {BlocksDataPath}");
                    _blocksData = new BlocksData();
                    SaveBlocksData(); // Save default data
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlockDataManager] Failed to load blocks data: {ex.Message}");
                _blocksData = new BlocksData();
            }
        }

        /// <summary>
        /// Saves current blocks data to JSON file
        /// </summary>
        public static void SaveBlocksData()
        {
            try
            {
                string jsonContent = JsonUtility.ToJson(_blocksData, true);
                Directory.CreateDirectory(Path.GetDirectoryName(BlocksDataPath));
                File.WriteAllText(BlocksDataPath, jsonContent);
                Debug.Log($"[BlockDataManager] Saved blocks data to {BlocksDataPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlockDataManager] Failed to save blocks data: {ex.Message}");
            }
        }

        /// <summary>
        /// Reloads blocks data from file
        /// </summary>
        public static void ReloadBlocksData()
        {
            _blocksData = null;
            LoadBlocksData();
        }

        /// <summary>
        /// Gets a block definition by ID
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>Block definition or null if not found</returns>
        public static BlockDefinition GetBlockById(int blockId)
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            _blocksById.TryGetValue(blockId, out var block);
            return block;
        }

        /// <summary>
        /// Gets a block definition by name
        /// </summary>
        /// <param name="blockName">The block name</param>
        /// <returns>Block definition or null if not found</returns>
        public static BlockDefinition GetBlockByName(string blockName)
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            _blocksByName.TryGetValue(blockName, out var block);
            return block;
        }

        /// <summary>
        /// Gets a tool definition by name
        /// </summary>
        /// <param name="toolName">The tool name</param>
        /// <returns>Tool definition or null if not found</returns>
        public static ToolDefinition GetToolByName(string toolName)
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            _blocksData.ToolTypes.TryGetValue(toolName, out var tool);
            return tool;
        }

        /// <summary>
        /// Checks if a tool can harvest a block
        /// </summary>
        /// <param name="toolName">The tool name</param>
        /// <param name="blockName">The block name</param>
        /// <returns>True if the tool can harvest the block</returns>
        public static bool CanToolHarvestBlock(string toolName, string blockName)
        {
            var tool = GetToolByName(toolName);
            var block = GetBlockByName(blockName);
            
            if (tool == null || block == null)
            {
                return false;
            }
            
            // None tool can only harvest blocks that don't require a specific tool
            if (toolName == "None")
            {
                return block.Tool == "None";
            }
            
            // Check if the tool can harvest this block type
            return tool.CanHarvest.Contains(block.Name) && block.Tool == toolName;
        }

        /// <summary>
        /// Gets the break time for a block with a specific tool
        /// </summary>
        /// <param name="blockName">The block name</param>
        /// <param name="toolName">The tool name</param>
        /// <param name="toolEfficiency">The tool efficiency multiplier</param>
        /// <returns>Break time in seconds</returns>
        public static float GetBreakTime(string blockName, string toolName, float toolEfficiency = 1.0f)
        {
            var block = GetBlockByName(blockName);
            var tool = GetToolByName(toolName);
            
            if (block == null || block.Hardness <= 0)
            {
                return 0f;
            }
            
            float baseTime = block.Hardness * 1.5f;
            
            if (tool != null && CanToolHarvestBlock(toolName, blockName))
            {
                baseTime /= tool.Efficiency * toolEfficiency;
            }
            else
            {
                // Wrong tool or no tool - much slower
                baseTime *= 3.33f;
            }
            
            return baseTime;
        }

        /// <summary>
        /// Gets the drops for a block when broken with a specific tool
        /// </summary>
        /// <param name="blockName">The block name</param>
        /// <param name="toolName">The tool name</param>
        /// <returns>List of item drops</returns>
        public static List<ItemDrop> GetBlockDrops(string blockName, string toolName)
        {
            var block = GetBlockByName(blockName);
            
            if (block == null || !block.CanBreak)
            {
                return new List<ItemDrop>();
            }
            
            // If the tool can't harvest the block, return empty drops (except for some special cases)
            if (!CanToolHarvestBlock(toolName, blockName) && block.Tool != "None")
            {
                return new List<ItemDrop>();
            }
            
            return block.Drops;
        }

        /// <summary>
        /// Checks if a block is solid
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>True if the block is solid</returns>
        public static bool IsBlockSolid(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.Solid ?? false;
        }

        /// <summary>
        /// Checks if a block is transparent
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>True if the block is transparent</returns>
        public static bool IsBlockTransparent(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.Transparent ?? false;
        }

        /// <summary>
        /// Checks if light can pass through a block
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>True if light can pass through</returns>
        public static bool DoesLightPassThrough(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.LightPassing ?? false;
        }

        /// <summary>
        /// Gets the light level emitted by a block
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>Light level (0-15)</returns>
        public static int GetBlockLightLevel(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.LightLevel ?? 0;
        }

        /// <summary>
        /// Checks if a block has gravity
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>True if the block has gravity</returns>
        public static bool DoesBlockHaveGravity(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.HasGravity ?? false;
        }

        /// <summary>
        /// Gets all block definitions
        /// </summary>
        /// <returns>All block definitions</returns>
        public static IEnumerable<BlockDefinition> GetAllBlocks()
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            return _blocksData.BlockTypes.Values;
        }

        /// <summary>
        /// Gets all tool definitions
        /// </summary>
        /// <returns>All tool definitions</returns>
        public static IEnumerable<ToolDefinition> GetAllTools()
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            return _blocksData.ToolTypes.Values;
        }
    }
}using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Minecraft.Core
{
    /// <summary>
    /// Block definition data structure
    /// </summary>
    [Serializable]
    public class BlockDefinition
    {
        public int Id;
        public string Name;
        public string DisplayName;
        public bool Solid;
        public bool Transparent;
        public bool LightPassing;
        public bool CanBreak;
        public bool CanPlace;
        public bool HasGravity;
        public int LightLevel;
        public float Resistance;
        public float Hardness;
        public BlockTexture Texture;
        public List<ItemDrop> Drops;
        public string Tool;
        public BlockSounds Sounds;
    }

    [Serializable]
    public class BlockTexture
    {
        public string Top;
        public string Bottom;
        public string Sides;
    }

    [Serializable]
    public class ItemDrop
    {
        public string Item;
        public int Count;
        public float Chance;
    }

    [Serializable]
    public class BlockSounds
    {
        public string Break;
        public string Place;
        public string Step;
    }

    [Serializable]
    public class ToolDefinition
    {
        public string Name;
        public float Efficiency;
        public List<string> CanHarvest;
    }

    [Serializable]
    public class BlocksData
    {
        public Dictionary<string, BlockDefinition> BlockTypes = new();
        public Dictionary<string, ToolDefinition> ToolTypes = new();
    }

    /// <summary>
    /// Data-driven block manager that loads and provides access to block definitions
    /// </summary>
    public static class BlockDataManager
    {
        private static BlocksData _blocksData;
        private static Dictionary<int, BlockDefinition> _blocksById = new();
        private static Dictionary<string, BlockDefinition> _blocksByName = new();
        private static readonly string BlocksDataPath = Path.Combine(Application.streamingAssetsPath, "blocks.json");

        /// <summary>
        /// Gets the loaded blocks data
        /// </summary>
        public static BlocksData Blocks
        {
            get
            {
                if (_blocksData == null)
                {
                    LoadBlocksData();
                }
                return _blocksData;
            }
        }

        /// <summary>
        /// Loads blocks data from JSON file
        /// </summary>
        public static void LoadBlocksData()
        {
            try
            {
                if (File.Exists(BlocksDataPath))
                {
                    string jsonContent = File.ReadAllText(BlocksDataPath);
                    _blocksData = JsonUtility.FromJson<BlocksData>(jsonContent);
                    
                    // Build lookup dictionaries
                    _blocksById.Clear();
                    _blocksByName.Clear();
                    
                    foreach (var kvp in _blocksData.BlockTypes)
                    {
                        var block = kvp.Value;
                        _blocksById[block.Id] = block;
                        _blocksByName[block.Name] = block;
                    }
                    
                    Debug.Log($"[BlockDataManager] Loaded {_blocksData.BlockTypes.Count} block types and {_blocksData.ToolTypes.Count} tool types");
                }
                else
                {
                    Debug.LogWarning($"[BlockDataManager] Blocks data file not found at {BlocksDataPath}");
                    _blocksData = new BlocksData();
                    SaveBlocksData(); // Save default data
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlockDataManager] Failed to load blocks data: {ex.Message}");
                _blocksData = new BlocksData();
            }
        }

        /// <summary>
        /// Saves current blocks data to JSON file
        /// </summary>
        public static void SaveBlocksData()
        {
            try
            {
                string jsonContent = JsonUtility.ToJson(_blocksData, true);
                Directory.CreateDirectory(Path.GetDirectoryName(BlocksDataPath));
                File.WriteAllText(BlocksDataPath, jsonContent);
                Debug.Log($"[BlockDataManager] Saved blocks data to {BlocksDataPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BlockDataManager] Failed to save blocks data: {ex.Message}");
            }
        }

        /// <summary>
        /// Reloads blocks data from file
        /// </summary>
        public static void ReloadBlocksData()
        {
            _blocksData = null;
            LoadBlocksData();
        }

        /// <summary>
        /// Gets a block definition by ID
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>Block definition or null if not found</returns>
        public static BlockDefinition GetBlockById(int blockId)
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            _blocksById.TryGetValue(blockId, out var block);
            return block;
        }

        /// <summary>
        /// Gets a block definition by name
        /// </summary>
        /// <param name="blockName">The block name</param>
        /// <returns>Block definition or null if not found</returns>
        public static BlockDefinition GetBlockByName(string blockName)
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            _blocksByName.TryGetValue(blockName, out var block);
            return block;
        }

        /// <summary>
        /// Gets a tool definition by name
        /// </summary>
        /// <param name="toolName">The tool name</param>
        /// <returns>Tool definition or null if not found</returns>
        public static ToolDefinition GetToolByName(string toolName)
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            _blocksData.ToolTypes.TryGetValue(toolName, out var tool);
            return tool;
        }

        /// <summary>
        /// Checks if a tool can harvest a block
        /// </summary>
        /// <param name="toolName">The tool name</param>
        /// <param name="blockName">The block name</param>
        /// <returns>True if the tool can harvest the block</returns>
        public static bool CanToolHarvestBlock(string toolName, string blockName)
        {
            var tool = GetToolByName(toolName);
            var block = GetBlockByName(blockName);
            
            if (tool == null || block == null)
            {
                return false;
            }
            
            // None tool can only harvest blocks that don't require a specific tool
            if (toolName == "None")
            {
                return block.Tool == "None";
            }
            
            // Check if the tool can harvest this block type
            return tool.CanHarvest.Contains(block.Name) && block.Tool == toolName;
        }

        /// <summary>
        /// Gets the break time for a block with a specific tool
        /// </summary>
        /// <param name="blockName">The block name</param>
        /// <param name="toolName">The tool name</param>
        /// <param name="toolEfficiency">The tool efficiency multiplier</param>
        /// <returns>Break time in seconds</returns>
        public static float GetBreakTime(string blockName, string toolName, float toolEfficiency = 1.0f)
        {
            var block = GetBlockByName(blockName);
            var tool = GetToolByName(toolName);
            
            if (block == null || block.Hardness <= 0)
            {
                return 0f;
            }
            
            float baseTime = block.Hardness * 1.5f;
            
            if (tool != null && CanToolHarvestBlock(toolName, blockName))
            {
                baseTime /= tool.Efficiency * toolEfficiency;
            }
            else
            {
                // Wrong tool or no tool - much slower
                baseTime *= 3.33f;
            }
            
            return baseTime;
        }

        /// <summary>
        /// Gets the drops for a block when broken with a specific tool
        /// </summary>
        /// <param name="blockName">The block name</param>
        /// <param name="toolName">The tool name</param>
        /// <returns>List of item drops</returns>
        public static List<ItemDrop> GetBlockDrops(string blockName, string toolName)
        {
            var block = GetBlockByName(blockName);
            
            if (block == null || !block.CanBreak)
            {
                return new List<ItemDrop>();
            }
            
            // If the tool can't harvest the block, return empty drops (except for some special cases)
            if (!CanToolHarvestBlock(toolName, blockName) && block.Tool != "None")
            {
                return new List<ItemDrop>();
            }
            
            return block.Drops;
        }

        /// <summary>
        /// Checks if a block is solid
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>True if the block is solid</returns>
        public static bool IsBlockSolid(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.Solid ?? false;
        }

        /// <summary>
        /// Checks if a block is transparent
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>True if the block is transparent</returns>
        public static bool IsBlockTransparent(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.Transparent ?? false;
        }

        /// <summary>
        /// Checks if light can pass through a block
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>True if light can pass through</returns>
        public static bool DoesLightPassThrough(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.LightPassing ?? false;
        }

        /// <summary>
        /// Gets the light level emitted by a block
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>Light level (0-15)</returns>
        public static int GetBlockLightLevel(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.LightLevel ?? 0;
        }

        /// <summary>
        /// Checks if a block has gravity
        /// </summary>
        /// <param name="blockId">The block ID</param>
        /// <returns>True if the block has gravity</returns>
        public static bool DoesBlockHaveGravity(int blockId)
        {
            var block = GetBlockById(blockId);
            return block?.HasGravity ?? false;
        }

        /// <summary>
        /// Gets all block definitions
        /// </summary>
        /// <returns>All block definitions</returns>
        public static IEnumerable<BlockDefinition> GetAllBlocks()
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            return _blocksData.BlockTypes.Values;
        }

        /// <summary>
        /// Gets all tool definitions
        /// </summary>
        /// <returns>All tool definitions</returns>
        public static IEnumerable<ToolDefinition> GetAllTools()
        {
            if (_blocksData == null)
            {
                LoadBlocksData();
            }
            
            return _blocksData.ToolTypes.Values;
        }
    }
}
}
