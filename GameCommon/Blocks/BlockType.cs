namespace GameCommon.Blocks
{
    /// <summary>
    /// 통합 블록 타입 정의 (서버-클라이언트 공유)
    /// 중복 제거: GameServer/Models/BlockData.cs + MapGeneratorLib/...
    /// </summary>
    public enum BlockType
    {
        // 공기 및 기본
        Air = 0,
        Stone = 1,
        Grass = 2,
        Dirt = 3,
        Cobblestone = 4,
        WoodPlanks = 5,
        Sapling = 6,
        Bedrock = 7,

        // 액체
        Water = 8,
        StationaryWater = 9,
        Lava = 10,
        StationaryLava = 11,

        // 자연 블록
        Sand = 12,
        Gravel = 13,
        GoldOre = 14,
        IronOre = 15,
        CoalOre = 16,
        Wood = 17,
        Leaves = 18,
        Sponge = 19,
        Glass = 20,

        // 광물
        LapisOre = 21,
        LapisBlock = 22,
        Dispenser = 23,
        Sandstone = 24,
        NoteBlock = 25,

        // 침대 (2블록)
        BedBlock = 26,

        // 레일
        PoweredRail = 27,
        DetectorRail = 28,
        StickyPiston = 29,

        // 식물
        Web = 30,
        TallGrass = 31,
        DeadBush = 32,
        Piston = 33,
        PistonHead = 34,

        // 양모 (16색)
        WhiteWool = 35,
        OrangeWool = 36,
        MagentaWool = 37,
        LightBlueWool = 38,
        YellowWool = 39,
        LimeWool = 40,
        PinkWool = 41,
        GrayWool = 42,
        LightGrayWool = 43,
        CyanWool = 44,
        PurpleWool = 45,
        BlueWool = 46,
        BrownWool = 47,
        GreenWool = 48,
        RedWool = 49,
        BlackWool = 50,

        // 꽃
        Flower = 37,
        Rose = 38,
        BrownMushroom = 39,
        RedMushroom = 40,

        // 금속
        GoldBlock = 41,
        IronBlock = 42,
        DoubleSlab = 43,
        Slab = 44,
        Brick = 45,
        TNT = 46,
        Bookshelf = 47,
        MossStone = 48,
        Obsidian = 49,

        // 특수 블록
        Torch = 50,
        Fire = 51,
        MobSpawner = 52,
        WoodStairs = 53,
        Chest = 54,
        RedstoneWire = 55,
        DiamondOre = 56,
        DiamondBlock = 57,
        CraftingTable = 58,
        Wheat = 59,
        Farmland = 60,
        Furnace = 61,
        BurningFurnace = 62,
        SignPost = 63,
        WoodDoor = 64,
        Ladder = 65,
        Rail = 66,
        CobblestoneStairs = 67,
        WallSign = 68,
        Lever = 69,
        StonePressurePlate = 70,
        IronDoor = 71,
        WoodPressurePlate = 72,
        RedstoneOre = 73,
        GlowingRedstoneOre = 74,
        RedstoneTorchOff = 75,
        RedstoneTorchOn = 76,
        StoneButton = 77,
        Snow = 78,
        Ice = 79,
        SnowBlock = 80,
        Cactus = 81,
        Clay = 82,
        SugarCane = 83,
        Jukebox = 84,
        Fence = 85,
        Pumpkin = 86,
        Netherrack = 87,
        SoulSand = 88,
        Glowstone = 89,
        Portal = 90,
        JackOLantern = 91,
        CakeBlock = 92,
        RedstoneRepeaterOff = 93,
        RedstoneRepeaterOn = 94,

        // 추가 블록 (확장)
        LockedChest = 95,
        Trapdoor = 96,

        // 네더
        NetherBrick = 112,
        NetherFence = 113,
        NetherBrickStairs = 114,

        // 엔드
        EndStone = 121,

        // 확장 (최대 256)
        // ...
    }
}
