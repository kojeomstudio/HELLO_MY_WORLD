namespace MinecraftGame.Common
{
    /// <summary>
    /// Shared block identifiers that can be referenced by both server and client-side tools.
    /// </summary>
    public enum BlockType : ushort
    {
        Air = 0,
        Stone = 1,
        Grass = 2,
        Dirt = 3,
        Cobblestone = 4,
        Wood = 5,
        Bedrock = 7,
        Water = 8,
        WaterSource = 9,
        Lava = 10,
        Sand = 12,
        Gravel = 13,
        GoldOre = 14,
        IronOre = 15,
        CoalOre = 16,
        Log = 17,
        Leaves = 18,
        Glass = 20,
        Sandstone = 24,
        TallGrass = 31,
        DeadBush = 32,
        Wool = 35,
        MossyCobblestone = 48,
        Obsidian = 49,
        Chest = 54,
        DiamondOre = 56,
        DiamondBlock = 57,
        Farmland = 60,
        Snow = 78,
        Ice = 79,
        SnowBlock = 80,
        Clay = 82,
        Pumpkin = 86,
        Cloud = 95
    }

    /// <summary>
    /// Shared gameplay item identifiers for inventory/tooling workflows.
    /// </summary>
    public enum ItemType : ushort
    {
        None = 0,
        Block = 1,
        Tool = 2,
        Weapon = 3,
        Armor = 4,
        Food = 5,
        Material = 6,
        Misc = 7
    }
}
