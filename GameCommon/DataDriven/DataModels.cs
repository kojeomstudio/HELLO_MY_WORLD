using System;
using System.Collections.Generic;

namespace GameCommon.DataDriven
{
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

    public class RecipeDataContainer
    {
        public List<RecipeData> Recipes { get; set; } = new();
    }

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

    public class DrinkData
    {
        public string ItemId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public float Hydration { get; set; }
        public float SaturationModifier { get; set; } = 0.5f;
        public float DrinkTime { get; set; } = 1.6f;
        public List<string> Effects { get; set; } = new();
    }

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
}
