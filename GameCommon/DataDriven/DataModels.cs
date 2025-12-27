using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameCommon.DataDriven
{
    // Block data model
    public class BlockData
    {
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public float Hardness { get; set; }
        public bool IsTransparent { get; set; }
        public bool IsSolid { get; set; }
        public bool IsLightEmitter { get; set; }
        public int LightLevel { get; set; }
        public bool IsWater { get; set; }
        public bool IsLava { get; set; }
        public string TextureName { get; set; } = string.Empty;
        public string[] TextureNames { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Item data model
    public class ItemData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int MaxStackSize { get; set; } = 64;
        public int Value { get; set; }
        public string TextureName { get; set; } = string.Empty;
        public bool IsPlaceable { get; set; }
        public bool IsConsumable { get; set; }
        public bool IsTool { get; set; }
        public float Durability { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Recipe data model
    public class RecipeData
    {
        public string RecipeId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = "crafting"; // crafting, smelting, brewing
        public List<RecipeIngredient> Ingredients { get; set; } = new();
        public List<RecipeResult> Results { get; set; } = new();
        public int CraftingTime { get; set; }
        public bool RequiresCraftingTable { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class RecipeIngredient
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class RecipeResult
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Food data model
    public class FoodData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Nutrition { get; set; }
        public float SaturationModifier { get; set; } = 0.5f;
        public bool IsMeat { get; set; }
        public bool IsAlwaysEdible { get; set; }
        public float EatTime { get; set; } = 1.6f;
        public List<string> Effects { get; set; } = new();
    }

    // Drink data model
    public class DrinkData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Hydration { get; set; }
        public float SaturationModifier { get; set; } = 0.5f;
        public float DrinkTime { get; set; } = 1.6f;
        public List<string> Effects { get; set; } = new();
    }

    // Effect data model
    public class EffectData
    {
        public string Type { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int Amplifier { get; set; }
        public bool IsAmbient { get; set; }
        public bool ShowParticles { get; set; }
        public bool ShowIcon { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Biome data model
    public class BiomeData
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Rainfall { get; set; }
        public string[] TopBlocks { get; set; } = Array.Empty<string>();
        public string[] FillBlocks { get; set; } = Array.Empty<string>();
        public string[] UnderwaterBlocks { get; set; } = Array.Empty<string>();
        public Dictionary<string, float> Features { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Entity data model
    public class EntityData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public EntityType Type { get; set; }
        public int Health { get; set; }
        public float Speed { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int Damage { get; set; }
        public float AttackRange { get; set; }
        public float DetectionRange { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public enum EntityType
    {
        Player,
        PassiveMob,
        HostileMob,
        NeutralMob,
        UtilityMob,
        Item,
        Projectile
    }

    // Container classes for JSON serialization
    public class BlockDataContainer
    {
        public List<BlockData> Blocks { get; set; } = new();
    }

    public class ItemsDataContainer
    {
        public List<ItemData> Items { get; set; } = new();
    }

    public class RecipeDataContainer
    {
        public List<RecipeData> Recipes { get; set; } = new();
    }

    public class HungerConfigContainer
    {
        public Dictionary<string, FoodData> FoodItems { get; set; } = new();
        public Dictionary<string, DrinkData> DrinkItems { get; set; } = new();
        public Dictionary<string, EffectData> Effects { get; set; } = new();
    }

    public class WorldConfigContainer
    {
        public List<BiomeData> Biomes { get; set; } = new();
    }
}
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GameCommon.DataDriven
{
    // Block data model
    public class BlockData
    {
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public float Hardness { get; set; }
        public bool IsTransparent { get; set; }
        public bool IsSolid { get; set; }
        public bool IsLightEmitter { get; set; }
        public int LightLevel { get; set; }
        public bool IsWater { get; set; }
        public bool IsLava { get; set; }
        public string TextureName { get; set; } = string.Empty;
        public string[] TextureNames { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Item data model
    public class ItemData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int MaxStackSize { get; set; } = 64;
        public int Value { get; set; }
        public string TextureName { get; set; } = string.Empty;
        public bool IsPlaceable { get; set; }
        public bool IsConsumable { get; set; }
        public bool IsTool { get; set; }
        public float Durability { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Recipe data model
    public class RecipeData
    {
        public string RecipeId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = "crafting"; // crafting, smelting, brewing
        public List<RecipeIngredient> Ingredients { get; set; } = new();
        public List<RecipeResult> Results { get; set; } = new();
        public int CraftingTime { get; set; }
        public bool RequiresCraftingTable { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class RecipeIngredient
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class RecipeResult
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Food data model
    public class FoodData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Nutrition { get; set; }
        public float SaturationModifier { get; set; } = 0.5f;
        public bool IsMeat { get; set; }
        public bool IsAlwaysEdible { get; set; }
        public float EatTime { get; set; } = 1.6f;
        public List<string> Effects { get; set; } = new();
    }

    // Drink data model
    public class DrinkData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Hydration { get; set; }
        public float SaturationModifier { get; set; } = 0.5f;
        public float DrinkTime { get; set; } = 1.6f;
        public List<string> Effects { get; set; } = new();
    }

    // Effect data model
    public class EffectData
    {
        public string Type { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int Amplifier { get; set; }
        public bool IsAmbient { get; set; }
        public bool ShowParticles { get; set; }
        public bool ShowIcon { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Biome data model
    public class BiomeData
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Rainfall { get; set; }
        public string[] TopBlocks { get; set; } = Array.Empty<string>();
        public string[] FillBlocks { get; set; } = Array.Empty<string>();
        public string[] UnderwaterBlocks { get; set; } = Array.Empty<string>();
        public Dictionary<string, float> Features { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Entity data model
    public class EntityData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public EntityType Type { get; set; }
        public int Health { get; set; }
        public float Speed { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int Damage { get; set; }
        public float AttackRange { get; set; }
        public float DetectionRange { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public enum EntityType
    {
        Player,
        PassiveMob,
        HostileMob,
        NeutralMob,
        UtilityMob,
        Item,
        Projectile
    }

    // Container classes for JSON serialization
    public class BlockDataContainer
    {
        public List<BlockData> Blocks { get; set; } = new();
    }

    public class ItemsDataContainer
    {
        public List<ItemData> Items { get; set; } = new();
    }

    public class RecipeDataContainer
    {
        public List<RecipeData> Recipes { get; set; } = new();
    }

    public class HungerConfigContainer
    {
        public Dictionary<string, FoodData> FoodItems { get; set; } = new();
        public Dictionary<string, DrinkData> DrinkItems { get; set; } = new();
        public Dictionary<string, EffectData> Effects { get; set; } = new();
    }

    public class WorldConfigContainer
    {
        public List<BiomeData> Biomes { get; set; } = new();
    }
}
}
using System.Text.Json.Serialization;

namespace GameCommon.DataDriven
{
    // Block data model
    public class BlockData
    {
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public float Hardness { get; set; }
        public bool IsTransparent { get; set; }
        public bool IsSolid { get; set; }
        public bool IsLightEmitter { get; set; }
        public int LightLevel { get; set; }
        public bool IsWater { get; set; }
        public bool IsLava { get; set; }
        public string TextureName { get; set; } = string.Empty;
        public string[] TextureNames { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Item data model
    public class ItemData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int MaxStackSize { get; set; } = 64;
        public int Value { get; set; }
        public string TextureName { get; set; } = string.Empty;
        public bool IsPlaceable { get; set; }
        public bool IsConsumable { get; set; }
        public bool IsTool { get; set; }
        public float Durability { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Recipe data model
    public class RecipeData
    {
        public string RecipeId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = "crafting"; // crafting, smelting, brewing
        public List<RecipeIngredient> Ingredients { get; set; } = new();
        public List<RecipeResult> Results { get; set; } = new();
        public int CraftingTime { get; set; }
        public bool RequiresCraftingTable { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class RecipeIngredient
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class RecipeResult
    {
        public string ItemId { get; set; } = string.Empty;
        public int Count { get; set; } = 1;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Food data model
    public class FoodData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Nutrition { get; set; }
        public float SaturationModifier { get; set; } = 0.5f;
        public bool IsMeat { get; set; }
        public bool IsAlwaysEdible { get; set; }
        public float EatTime { get; set; } = 1.6f;
        public List<string> Effects { get; set; } = new();
    }

    // Drink data model
    public class DrinkData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Hydration { get; set; }
        public float SaturationModifier { get; set; } = 0.5f;
        public float DrinkTime { get; set; } = 1.6f;
        public List<string> Effects { get; set; } = new();
    }

    // Effect data model
    public class EffectData
    {
        public string Type { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int Duration { get; set; }
        public int Amplifier { get; set; }
        public bool IsAmbient { get; set; }
        public bool ShowParticles { get; set; }
        public bool ShowIcon { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Biome data model
    public class BiomeData
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float Rainfall { get; set; }
        public string[] TopBlocks { get; set; } = Array.Empty<string>();
        public string[] FillBlocks { get; set; } = Array.Empty<string>();
        public string[] UnderwaterBlocks { get; set; } = Array.Empty<string>();
        public Dictionary<string, float> Features { get; set; } = new();
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // Entity data model
    public class EntityData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public EntityType Type { get; set; }
        public int Health { get; set; }
        public float Speed { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public int Damage { get; set; }
        public float AttackRange { get; set; }
        public float DetectionRange { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public enum EntityType
    {
        Player,
        PassiveMob,
        HostileMob,
        NeutralMob,
        UtilityMob,
        Item,
        Projectile
    }

    // Container classes for JSON serialization
    public class BlockDataContainer
    {
        public List<BlockData> Blocks { get; set; } = new();
    }

    public class ItemsDataContainer
    {
        public List<ItemData> Items { get; set; } = new();
    }

    public class RecipeDataContainer
    {
        public List<RecipeData> Recipes { get; set; } = new();
    }

    public class HungerConfigContainer
    {
        public Dictionary<string, FoodData> FoodItems { get; set; } = new();
        public Dictionary<string, DrinkData> DrinkItems { get; set; } = new();
        public Dictionary<string, EffectData> Effects { get; set; } = new();
    }

    public class WorldConfigContainer
    {
        public List<BiomeData> Biomes { get; set; } = new();
    }
}
        HostileMob,
        NeutralMob,
        UtilityMob
    }
    
    public class EntityData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public EntityType Type { get; set; }
        public float Health { get; set; }
        public float Speed { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Damage { get; set; }
        public float DetectionRange { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
    }
    #endregion
}
using System.Text.Json.Serialization;

namespace GameCommon.DataDriven
{
    #region Block Data
    public class BlockData
    {
        public int Type { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public float Hardness { get; set; }
        public float Resistance { get; set; }
        public bool IsTransparent { get; set; }
        public bool IsFluid { get; set; }
        public bool AffectedByGravity { get; set; }
        public string RequiredTool { get; set; }
        public int RequiredToolLevel { get; set; }
        public int LightLevel { get; set; }
        public List<ItemDrop> Drops { get; set; }
        public bool ConductsRedstone { get; set; }
        public bool IsPowerSource { get; set; }
    }
    
    public class ItemDrop
    {
        public string ItemId { get; set; }
        public float Chance { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
    }
    
    public class BlockDataContainer
    {
        public List<BlockData> Blocks { get; set; }
    }
    #endregion
    
    #region Item Data
    public class ItemData
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string CategoryId { get; set; }
        public string Rarity { get; set; }
        public int MaxStackSize { get; set; }
        public float Nutrition { get; set; }
        public float Hydration { get; set; }
        public string ToolType { get; set; }
        public float ToolStrength { get; set; }
        public int Durability { get; set; }
        public int MaxDurability { get; set; }
        public string RepairItem { get; set; }
        public int Value { get; set; }
        public float Weight { get; set; }
        public bool CanEnchant { get; set; }
        public List<string> EnchantableTypes { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
    }
    
    public class ItemsDataContainer
    {
        public List<ItemData> Items { get; set; }
    }
    #endregion
    
    #region Recipe Data
    public class RecipeData
    {
        public string RecipeId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public int RequiredLevel { get; set; }
        public int ExperienceCost { get; set; }
        public List<RecipeIngredient> Ingredients { get; set; }
        public List<RecipeResult> Results { get; set; }
        public float CraftingTime { get; set; }
        public string CraftingStation { get; set; }
    }
    
    public class RecipeIngredient
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public int Metadata { get; set; }
    }
    
    public class RecipeResult
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public int Metadata { get; set; }
    }
    
    public class RecipeDataContainer
    {
        public List<RecipeData> Recipes { get; set; }
    }
    #endregion
    
    #region Food Data
    public class FoodData
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public float Nutrition { get; set; }
        public float Hydration { get; set; }
        public float Saturation { get; set; }
        public int StackSize { get; set; }
        public string Rarity { get; set; }
        public string Category { get; set; }
        public List<EffectData> Effects { get; set; }
    }
    
    public class HungerConfigContainer
    {
        public Dictionary<string, FoodData> FoodItems { get; set; }
        public Dictionary<string, DrinkData> DrinkItems { get; set; }
        public Dictionary<string, EffectData> Effects { get; set; }
    }
    #endregion
    
    #region Drink Data
    public class DrinkData
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public float Nutrition { get; set; }
        public float Hydration { get; set; }
        public float Saturation { get; set; }
        public int StackSize { get; set; }
        public string Rarity { get; set; }
        public string Category { get; set; }
        public List<EffectData> Effects { get; set; }
    }
    #endregion
    
    #region Effect Data
    public class EffectData
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public float Duration { get; set; }
        public float Amplifier { get; set; }
    }
    #endregion
    
    #region Biome Data
    public class BiomeData
    {
        public string Name { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float HeightVariation { get; set; }
        public List<string> Blocks { get; set; }
        public List<string> Features { get; set; }
        public float MobSpawnChance { get; set; }
        public Dictionary<string, float> BlockWeights { get; set; }
    }
    
    public class WorldConfigContainer
    {
        public List<BiomeData> Biomes { get; set; }
    }
    #endregion
    
    #region Entity Data
    public enum EntityType
    {
        Player,
        PassiveMob,
        HostileMob,
        NeutralMob,
        UtilityMob
    }
    
    public class EntityData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public EntityType Type { get; set; }
        public float Health { get; set; }
        public float Speed { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Damage { get; set; }
        public float DetectionRange { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
    }
    #endregion
}
}

namespace GameCommon.DataDriven
{
    #region Block Data
    public class BlockData
    {
        public int Type { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public float Hardness { get; set; }
        public float Resistance { get; set; }
        public bool IsTransparent { get; set; }
        public bool IsFluid { get; set; }
        public bool AffectedByGravity { get; set; }
        public string RequiredTool { get; set; }
        public int RequiredToolLevel { get; set; }
        public int LightLevel { get; set; }
        public List<ItemDrop> Drops { get; set; }
        public bool ConductsRedstone { get; set; }
        public bool IsPowerSource { get; set; }
    }
    
    public class ItemDrop
    {
        public string ItemId { get; set; }
        public float Chance { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
    }
    
    public class BlockDataContainer
    {
        public List<BlockData> Blocks { get; set; }
    }
    #endregion
    
    #region Item Data
    public class ItemData
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string CategoryId { get; set; }
        public string Rarity { get; set; }
        public int MaxStackSize { get; set; }
        public float Nutrition { get; set; }
        public float Hydration { get; set; }
        public string ToolType { get; set; }
        public float ToolStrength { get; set; }
        public int Durability { get; set; }
        public int MaxDurability { get; set; }
        public string RepairItem { get; set; }
        public int Value { get; set; }
        public float Weight { get; set; }
        public bool CanEnchant { get; set; }
        public List<string> EnchantableTypes { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
    }
    
    public class ItemsDataContainer
    {
        public List<ItemData> Items { get; set; }
    }
    #endregion
    
    #region Recipe Data
    public class RecipeData
    {
        public string RecipeId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public int RequiredLevel { get; set; }
        public int ExperienceCost { get; set; }
        public List<RecipeIngredient> Ingredients { get; set; }
        public List<RecipeResult> Results { get; set; }
        public float CraftingTime { get; set; }
        public string CraftingStation { get; set; }
    }
    
    public class RecipeIngredient
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public int Metadata { get; set; }
    }
    
    public class RecipeResult
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public int Metadata { get; set; }
    }
    
    public class RecipeDataContainer
    {
        public List<RecipeData> Recipes { get; set; }
    }
    #endregion
    
    #region Food Data
    public class FoodData
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public float Nutrition { get; set; }
        public float Hydration { get; set; }
        public float Saturation { get; set; }
        public int StackSize { get; set; }
        public string Rarity { get; set; }
        public string Category { get; set; }
        public List<EffectData> Effects { get; set; }
    }
    
    public class HungerConfigContainer
    {
        public Dictionary<string, FoodData> FoodItems { get; set; }
        public Dictionary<string, DrinkData> DrinkItems { get; set; }
        public Dictionary<string, EffectData> Effects { get; set; }
    }
    #endregion
    
    #region Drink Data
    public class DrinkData
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public float Nutrition { get; set; }
        public float Hydration { get; set; }
        public float Saturation { get; set; }
        public int StackSize { get; set; }
        public string Rarity { get; set; }
        public string Category { get; set; }
        public List<EffectData> Effects { get; set; }
    }
    #endregion
    
    #region Effect Data
    public class EffectData
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public float Duration { get; set; }
        public float Amplifier { get; set; }
    }
    #endregion
    
    #region Biome Data
    public class BiomeData
    {
        public string Name { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float HeightVariation { get; set; }
        public List<string> Blocks { get; set; }
        public List<string> Features { get; set; }
        public float MobSpawnChance { get; set; }
        public Dictionary<string, float> BlockWeights { get; set; }
    }
    
    public class WorldConfigContainer
    {
        public List<BiomeData> Biomes { get; set; }
    }
    #endregion
    
    #region Entity Data
    public enum EntityType
    {
        Player,
        PassiveMob,
        HostileMob,
        NeutralMob,
        UtilityMob
    }
    
    public class EntityData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public EntityType Type { get; set; }
        public float Health { get; set; }
        public float Speed { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Damage { get; set; }
        public float DetectionRange { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
    }
    #endregion
}

namespace GameCommon.DataDriven
{
    #region Block Data
    public class BlockData
    {
        public int Type { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public float Hardness { get; set; }
        public float Resistance { get; set; }
        public bool IsTransparent { get; set; }
        public bool IsFluid { get; set; }
        public bool AffectedByGravity { get; set; }
        public string RequiredTool { get; set; }
        public int RequiredToolLevel { get; set; }
        public int LightLevel { get; set; }
        public List<ItemDrop> Drops { get; set; }
        public bool ConductsRedstone { get; set; }
        public bool IsPowerSource { get; set; }
    }
    
    public class ItemDrop
    {
        public string ItemId { get; set; }
        public float Chance { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
    }
    
    public class BlockDataContainer
    {
        public List<BlockData> Blocks { get; set; }
    }
    #endregion
    
    #region Item Data
    public class ItemData
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string CategoryId { get; set; }
        public string Rarity { get; set; }
        public int MaxStackSize { get; set; }
        public float Nutrition { get; set; }
        public float Hydration { get; set; }
        public string ToolType { get; set; }
        public float ToolStrength { get; set; }
        public int Durability { get; set; }
        public int MaxDurability { get; set; }
        public string RepairItem { get; set; }
        public int Value { get; set; }
        public float Weight { get; set; }
        public bool CanEnchant { get; set; }
        public List<string> EnchantableTypes { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
    }
    
    public class ItemsDataContainer
    {
        public List<ItemData> Items { get; set; }
    }
    #endregion
    
    #region Recipe Data
    public class RecipeData
    {
        public string RecipeId { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public int RequiredLevel { get; set; }
        public int ExperienceCost { get; set; }
        public List<RecipeIngredient> Ingredients { get; set; }
        public List<RecipeResult> Results { get; set; }
        public float CraftingTime { get; set; }
        public string CraftingStation { get; set; }
    }
    
    public class RecipeIngredient
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public int Metadata { get; set; }
    }
    
    public class RecipeResult
    {
        public string ItemId { get; set; }
        public int Quantity { get; set; }
        public int Metadata { get; set; }
    }
    
    public class RecipeDataContainer
    {
        public List<RecipeData> Recipes { get; set; }
    }
    #endregion
    
    #region Food Data
    public class FoodData
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public float Nutrition { get; set; }
        public float Hydration { get; set; }
        public float Saturation { get; set; }
        public int StackSize { get; set; }
        public string Rarity { get; set; }
        public string Category { get; set; }
        public List<EffectData> Effects { get; set; }
    }
    
    public class HungerConfigContainer
    {
        public Dictionary<string, FoodData> FoodItems { get; set; }
        public Dictionary<string, DrinkData> DrinkItems { get; set; }
        public Dictionary<string, EffectData> Effects { get; set; }
    }
    #endregion
    
    #region Drink Data
    public class DrinkData
    {
        public string ItemId { get; set; }
        public string DisplayName { get; set; }
        public float Nutrition { get; set; }
        public float Hydration { get; set; }
        public float Saturation { get; set; }
        public int StackSize { get; set; }
        public string Rarity { get; set; }
        public string Category { get; set; }
        public List<EffectData> Effects { get; set; }
    }
    #endregion
    
    #region Effect Data
    public class EffectData
    {
        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public float Duration { get; set; }
        public float Amplifier { get; set; }
    }
    #endregion
    
    #region Biome Data
    public class BiomeData
    {
        public string Name { get; set; }
        public float Temperature { get; set; }
        public float Humidity { get; set; }
        public float HeightVariation { get; set; }
        public List<string> Blocks { get; set; }
        public List<string> Features { get; set; }
        public float MobSpawnChance { get; set; }
        public Dictionary<string, float> BlockWeights { get; set; }
    }
    
    public class WorldConfigContainer
    {
        public List<BiomeData> Biomes { get; set; }
    }
    #endregion
    
    #region Entity Data
    public enum EntityType
    {
        Player,
        PassiveMob,
        HostileMob,
        NeutralMob,
        UtilityMob
    }
    
    public class EntityData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public EntityType Type { get; set; }
        public float Health { get; set; }
        public float Speed { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Damage { get; set; }
        public float DetectionRange { get; set; }
        public Dictionary<string, object> CustomProperties { get; set; }
    }
    #endregion
}
