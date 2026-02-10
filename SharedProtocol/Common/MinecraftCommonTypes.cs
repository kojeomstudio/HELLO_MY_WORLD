using System;
using System.Collections.Generic;

namespace MinecraftGame.Common
{
    /// <summary>
    /// Common types and enums shared between client and server
    /// This DLL contains all shared protocol-independent types
    /// </summary>

    #region Block Types

    /// <summary>
    /// Block type identifiers
    /// </summary>
    public enum BlockType : byte
    {
        Air = 0,
        Stone = 1,
        Grass = 2,
        Dirt = 3,
        Cobblestone = 4,
        WoodenPlanks = 5,
        Sapling = 6,
        Bedrock = 7,
        Water = 8,
        StationaryWater = 9,
        Lava = 10,
        StationaryLava = 11,
        Sand = 12,
        Gravel = 13,
        GoldOre = 14,
        IronOre = 15,
        CoalOre = 16,
        Log = 17,
        Leaves = 18,
        Sponge = 19,
        Glass = 20,
        LapisOre = 21,
        LapisBlock = 22,
        Dispenser = 23,
        Sandstone = 24,
        NoteBlock = 25,
        Bed = 26,
        PoweredRail = 27,
        DetectorRail = 28,
        StickyPiston = 29,
        Web = 30,
        TallGrass = 31,
        DeadBush = 32,
        Piston = 33,
        PistonHead = 34,
        Wool = 35,
        PistonExtension = 36,
        YellowFlower = 37,
        RedFlower = 38,
        BrownMushroom = 39,
        RedMushroom = 40,
        GoldBlock = 41,
        IronBlock = 42,
        DoubleStoneSlab = 43,
        StoneSlab = 44,
        BrickBlock = 45,
        TNT = 46,
        Bookshelf = 47,
        MossyCobblestone = 48,
        Obsidian = 49,
        Torch = 50,
        Fire = 51,
        MobSpawner = 52,
        OakWoodStairs = 53,
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
        WoodenDoor = 64,
        Ladder = 65,
        Rail = 66,
        CobblestoneStairs = 67,
        WallSign = 68,
        Lever = 69,
        StonePressurePlate = 70,
        IronDoor = 71,
        WoodenPressurePlate = 72,
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
        Cake = 92,
        RedstoneRepeaterOff = 93,
        RedstoneRepeaterOn = 94,
        StainedGlass = 95,
        Trapdoor = 96,
        MonsterEgg = 97,
        StoneBricks = 98,
        BrownMushroomBlock = 99,
        RedMushroomBlock = 100,
        IronBars = 101,
        GlassPane = 102,
        Melon = 103,
        PumpkinStem = 104,
        MelonStem = 105,
        Vine = 106,
        FenceGate = 107,
        BrickStairs = 108,
        StoneBrickStairs = 109,
        Mycelium = 110,
        LilyPad = 111,
        NetherBrick = 112,
        NetherBrickFence = 113,
        NetherBrickStairs = 114,
        NetherWart = 115,
        EnchantingTable = 116,
        BrewingStand = 117,
        Cauldron = 118,
        EndPortal = 119,
        EndPortalFrame = 120,
        EndStone = 121,
        DragonEgg = 122,
        RedstoneLampOff = 123,
        RedstoneLampOn = 124,
        DoubleWoodenSlab = 125,
        WoodenSlab = 126,
        Cocoa = 127,
        SandstoneStairs = 128,
        EmeraldOre = 129,
        EnderChest = 130,
        TripwireHook = 131,
        Tripwire = 132,
        EmeraldBlock = 133,
        SpruceWoodStairs = 134,
        BirchWoodStairs = 135,
        JungleWoodStairs = 136,
        CommandBlock = 137,
        Beacon = 138,
        CobblestoneWall = 139,
        FlowerPot = 140,
        Carrots = 141,
        Potatoes = 142,
        WoodenButton = 143,
        Skull = 144,
        Anvil = 145,
        TrappedChest = 146,
        LightWeightedPressurePlate = 147,
        HeavyWeightedPressurePlate = 148,
        ComparatorOff = 149,
        ComparatorOn = 150,
        DaylightDetector = 151,
        RedstoneBlock = 152,
        QuartzOre = 153,
        Hopper = 154,
        QuartzBlock = 155,
        QuartzStairs = 156,
        ActivatorRail = 157,
        Dropper = 158,
        StainedHardenedClay = 159,
        StainedGlassPane = 160,
        Leaves2 = 161,
        Log2 = 162,
        StainedGlass2 = 163,
        StainedGlassPane2 = 164,
        StainedHardenedClay2 = 165,
        Stone2 = 166,
        PackedIce = 167,
        DoublePlant = 168,
        StandingBanner = 169,
        WallBanner = 170,
        DaylightDetectorInverted = 171,
        RedSandstone = 172,
        RedSandstoneStairs = 173,
        DoubleStoneSlab2 = 174,
        StoneSlab2 = 175,
        FenceGate2 = 176,
        SpruceFenceGate = 177,
        BirchFenceGate = 178,
        JungleFenceGate = 179,
        DarkOakFenceGate = 180,
        AcaciaFenceGate = 181,
        SpruceDoor = 182,
        BirchDoor = 183,
        JungleDoor = 184,
        AcaciaDoor = 185,
        DarkOakDoor = 186,
        EndRod = 197,
    }

    #endregion

    #region Item Types

    /// <summary>
    /// Item type identifiers
    /// </summary>
    public enum ItemType : byte
    {
        None = 0,
        IronShovel = 1,
        IronPickaxe = 2,
        IronAxe = 3,
        FlintAndSteel = 4,
        Apple = 5,
        Bow = 6,
        Arrow = 7,
        Coal = 8,
        Charcoal = 9,
        Diamond = 10,
        IronIngot = 11,
        GoldIngot = 12,
        IronSword = 13,
        WoodenSword = 14,
        WoodenShovel = 15,
        WoodenPickaxe = 16,
        WoodenAxe = 17,
        StoneSword = 18,
        StoneShovel = 19,
        StonePickaxe = 20,
        StoneAxe = 21,
        DiamondSword = 22,
        DiamondShovel = 23,
        DiamondPickaxe = 24,
        DiamondAxe = 25,
        Stick = 26,
        Bowl = 27,
        MushroomStew = 28,
        GoldenSword = 29,
        GoldenShovel = 30,
        GoldenPickaxe = 31,
        GoldenAxe = 32,
        String = 33,
        Feather = 34,
        Gunpowder = 35,
        WoodenHoe = 36,
        StoneHoe = 37,
        IronHoe = 38,
        DiamondHoe = 39,
        GoldenHoe = 40,
        Seeds = 41,
        Wheat = 42,
        Bread = 43,
        LeatherHelmet = 44,
        LeatherChestplate = 45,
        LeatherLeggings = 46,
        LeatherBoots = 47,
        ChainmailHelmet = 48,
        ChainmailChestplate = 49,
        ChainmailLeggings = 50,
        ChainmailBoots = 51,
        IronHelmet = 52,
        IronChestplate = 53,
        IronLeggings = 54,
        IronBoots = 55,
        DiamondHelmet = 56,
        DiamondChestplate = 57,
        DiamondLeggings = 58,
        DiamondBoots = 59,
        GoldenHelmet = 60,
        GoldenChestplate = 61,
        GoldenLeggings = 62,
        GoldenBoots = 63,
        Flint = 64,
        Porkchop = 65,
        CookedPorkchop = 66,
        Painting = 67,
        GoldenApple = 68,
        Sign = 69,
        WoodenDoor = 70,
        Bucket = 71,
        WaterBucket = 72,
        LavaBucket = 73,
        Minecart = 74,
        Saddle = 75,
        IronDoor = 76,
        Redstone = 77,
        Snowball = 78,
        Boat = 79,
        Leather = 80,
        MilkBucket = 81,
        Brick = 82,
        ClayBall = 83,
        SugarCane = 84,
        Paper = 85,
        Book = 86,
        Slimeball = 87,
        StorageMinecart = 88,
        PoweredMinecart = 89,
        Egg = 90,
        Compass = 91,
        FishingRod = 92,
        Clock = 93,
        GlowstoneDust = 94,
        Fish = 95,
        CookedFish = 96,
        Dye = 97,
        Bone = 98,
        Sugar = 99,
        Cake = 100,
        Bed = 101,
        Repeater = 102,
        Cookie = 103,
        Map = 104,
        Shears = 105,
        Melon = 106,
        PumpkinSeeds = 107,
        MelonSeeds = 108,
        Beef = 109,
        CookedBeef = 110,
        RawChicken = 111,
        CookedChicken = 112,
        RottenFlesh = 113,
        EnderPearl = 114,
        BlazeRod = 115,
        GhastTear = 116,
        GoldNugget = 117,
        NetherWart = 118,
        Potion = 119,
        GlassBottle = 120,
        SpiderEye = 121,
        FermentedSpiderEye = 122,
        BlazePowder = 123,
        MagmaCream = 124,
        BrewingStandItem = 125,
        CauldronItem = 126,
        EyeOfEnder = 127,
        SpeckledMelon = 128,
        MonsterEgg = 129,
        ExpBottle = 130,
        FireCharge = 131,
        BookAndQuill = 132,
        WrittenBook = 133,
        Emerald = 134,
        ItemFrame = 135,
        FlowerPot = 136,
        Carrot = 137,
        Potato = 138,
        BakedPotato = 139,
        PoisonousPotato = 140,
        GoldenCarrot = 141,
        Skull = 142,
        CarrotOnAStick = 143,
        NetherStar = 144,
        PumpkinPie = 145,
        Fireworks = 146,
        FireworkCharge = 147,
        EnchantedBook = 148,
        Comparator = 149,
        NetherBrick = 150,
        NetherQuartz = 151,
        MinecartWithTNT = 152,
        HopperMinecart = 153,
        PrismarineShard = 154,
        PrismarineCrystals = 155,
        Rabbit = 156,
        CookedRabbit = 157,
        RabbitStew = 158,
        RabbitFoot = 159,
        RabbitHide = 160,
        ArmorStand = 161,
        IronHorseArmor = 162,
        GoldenHorseArmor = 163,
        DiamondHorseArmor = 164,
        Lead = 165,
        NameTag = 166,
        CommandBlockMinecart = 167,
        Mutton = 168,
        CookedMutton = 169,
        Banner = 170,
        SpruceDoor = 171,
        BirchDoor = 172,
        JungleDoor = 173,
        AcaciaDoor = 174,
        DarkOakDoor = 175,
        EndCrystal = 176,
        ChorusFruit = 177,
        ChorusFruitPopped = 178,
        Beetroot = 179,
        BeetrootSeeds = 180,
        BeetrootSoup = 181,
        DragonBreath = 182,
        SplashPotion = 183,
        LingeringPotion = 184,
        TippedArrow = 185,
        Shield = 186,
        Elytra = 187,
        SpruceBoat = 188,
        BirchBoat = 189,
        JungleBoat = 190,
        AcaciaBoat = 191,
        DarkOakBoat = 192,
        TotemOfUndying = 193,
        ShulkerShell = 194,
        IronNugget = 195,
        Record13 = 196,
        RecordCat = 197,
        RecordBlocks = 198,
        RecordChirp = 199,
        RecordFar = 200,
        RecordMall = 201,
        RecordMellohi = 202,
        RecordStal = 203,
        RecordStrad = 204,
        RecordWard = 205,
        Record11 = 206,
        RecordWait = 207,
    }

    #endregion

    #region Game Modes

    /// <summary>
    /// Game mode types
    /// </summary>
    public enum GameMode : byte
    {
        Survival = 0,
        Creative = 1,
        Adventure = 2,
        Spectator = 3
    }

    #endregion

    #region Dimensions

    /// <summary>
    /// World dimension types
    /// </summary>
    public enum Dimension : int
    {
        Overworld = 0,
        Nether = -1,
        End = 1
    }

    #endregion

    #region Biome Types

    /// <summary>
    /// Biome type identifiers
    /// </summary>
    public enum BiomeType : byte
    {
        Ocean = 0,
        Plains = 1,
        Desert = 2,
        Mountains = 3,
        Forest = 4,
        Taiga = 5,
        Swamp = 6,
        River = 7,
        Nether = 8,
        TheEnd = 9,
        FrozenOcean = 10,
        FrozenRiver = 11,
        SnowyTundra = 12,
        SnowyMountains = 13,
        MushroomFields = 14,
        MushroomFieldShore = 15,
        Beach = 16,
        DesertHills = 17,
        ForestHills = 18,
        TaigaHills = 19,
        MountainsEdge = 20,
        Jungle = 21,
        JungleHills = 22,
        JungleEdge = 23,
        DeepOcean = 24,
        StoneShore = 25,
        BirchForest = 26,
        BirchForestHills = 27,
        DarkForest = 28,
        SnowyBeach = 29,
        BirchForestMountains = 30,
        SnowyTaiga = 31,
        SnowyTaigaHills = 32,
        GiantTreeTaiga = 33,
        GiantTreeTaigaHills = 34,
        Mountains = 35,
        GravellyMountains = 36,
        FlowerForest = 37,
        IceSpikes = 38,
        SunflowerPlains = 129,
        DesertLakes = 130,
        GravellyMountainsPlus = 131,
        ShatteredSavanna = 132,
        ShatteredSavannaPlateau = 133,
        ErodedBadlands = 134,
        ModifiedBadlandsPlateau = 135,
        ModifiedWoodedBadlandsPlateau = 136,
        BambooJungle = 137,
        BambooJungleHills = 138,
        SoulSandValley = 139,
        CrimsonForest = 140,
        WarpedForest = 141,
        BasaltDeltas = 142,
    }

    #endregion

    #region Sound Types

    /// <summary>
    /// Sound effect types
    /// </summary>
    public enum SoundType : ushort
    {
        None = 0,
        // Block sounds
        BlockStoneBreak = 1,
        BlockStonePlace = 2,
        BlockStoneHit = 3,
        BlockStoneStep = 4,
        BlockWoodBreak = 5,
        BlockWoodPlace = 6,
        BlockWoodHit = 7,
        BlockWoodStep = 8,
        BlockGrassBreak = 9,
        BlockGrassPlace = 10,
        BlockGrassHit = 11,
        BlockGrassStep = 12,
        BlockGravelBreak = 13,
        BlockGravelPlace = 14,
        BlockGravelHit = 15,
        BlockGravelStep = 16,
        BlockSandBreak = 17,
        BlockSandPlace = 18,
        BlockSandHit = 19,
        BlockSandStep = 20,
        BlockGlassBreak = 21,
        BlockGlassPlace = 22,
        BlockGlassHit = 23,
        BlockClothBreak = 24,
        BlockClothPlace = 25,
        BlockClothHit = 26,
        BlockTntBreak = 27,
        BlockTntPlace = 28,
        BlockTntHit = 29,
        BlockWaterSplash = 30,
        BlockWaterAmbient = 31,
        BlockWaterFlow = 32,
        BlockLavaSplash = 33,
        BlockLavaAmbient = 34,
        BlockLavaFlow = 35,
        // Footstep sounds
        FootstepStone = 100,
        FootstepWood = 101,
        FootstepGrass = 102,
        FootstepGravel = 103,
        FootstepSand = 104,
        FootstepSnow = 105,
        FootstepGlass = 106,
        FootstepMetal = 107,
        // Entity sounds
        EntityPlayerHurt = 200,
        EntityPlayerDeath = 201,
        EntityMobHurt = 202,
        EntityMobDeath = 203,
        EntityAmbient = 204,
        EntityStep = 205,
        // Item sounds
        ItemPickup = 300,
        ItemDrop = 301,
        ItemEquip = 302,
        ItemBreak = 303,
        // Weather sounds
        WeatherRain = 400,
        WeatherThunder = 401,
        // Music
        MusicMenu = 500,
        MusicGame = 501,
        MusicCreative = 502,
        MusicNether = 503,
        MusicEnd = 504,
    }

    #endregion

    #region Sound Categories

    /// <summary>
    /// Sound effect categories for volume control
    /// </summary>
    public enum SoundCategory : byte
    {
        Master = 0,
        SndMusic = 1,
        SndRecord = 2,
        SndWeather = 3,
        SndBlock = 4,
        SndHostile = 5,
        SndNeutral = 6,
        SndPlayer = 7,
        SndAmbient = 8,
        SndVoice = 9,
    }

    #endregion

    #region Particle Types

    /// <summary>
    /// Particle effect types
    /// </summary>
    public enum ParticleType : byte
    {
        None = 0,
        Explode = 1,
        LargeExplosion = 2,
        HugeExplosion = 3,
        FireworksSpark = 4,
        Bubble = 5,
        Splash = 6,
        Wake = 7,
        Suspended = 8,
        Depthsuspend = 9,
        Crit = 10,
        MagicCrit = 11,
        Smoke = 12,
        LargeSmoke = 13,
        Spell = 14,
        InstantSpell = 15,
        MobSpell = 16,
        MobSpellAmbient = 17,
        WitchMagic = 18,
        DripWater = 19,
        DripLava = 20,
        AngryVillager = 21,
        HappyVillager = 22,
        TownAura = 23,
        Note = 24,
        Portal = 25,
        EnchantmentTable = 26,
        Flame = 27,
        Lava = 28,
        Footstep = 29,
        Cloud = 30,
        Redstone = 31,
        Snowballpoof = 32,
        SnowShovel = 33,
        Slime = 34,
        Heart = 35,
        Barrier = 36,
        IconCrack = 37,
        BlockCrack = 38,
        BlockDust = 39,
        Droplet = 40,
        Take = 41,
        MobAppearance = 42,
        DragonBreath = 43,
        EndRod = 44,
        DamageIndicator = 45,
        SweepAttack = 46,
        FallingDust = 47,
        TotemOfUndying = 48,
        Spit = 49,
    }

    #endregion

    #region Chat Types

    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatType : byte
    {
        ChatGlobal = 0,
        ChatTeam = 1,
        ChatPrivate = 2,
        ChatSystem = 3,
        ChatAnnouncement = 4,
        ChatEmote = 5,
    }

    #endregion

    #region Weather Types

    /// <summary>
    /// Weather types
    /// </summary>
    public enum WeatherType : byte
    {
        Clear = 0,
        Rain = 1,
        Thunder = 2,
        Snow = 3,
    }

    #endregion

    #region Entity Types

    /// <summary>
    /// Entity type identifiers
    /// </summary>
    public enum EntityType : ushort
    {
        // Passive mobs
        Chicken = 1,
        Cow = 2,
        Pig = 3,
        Sheep = 4,
        Wolf = 5,
        Ocelot = 6,
        Horse = 7,
        Rabbit = 8,
        Villager = 9,
        // Hostile mobs
        Zombie = 10,
        Skeleton = 11,
        Spider = 12,
        Creeper = 13,
        Enderman = 14,
        Witch = 15,
        Slime = 16,
        Ghast = 17,
        Blaze = 18,
        PigZombie = 19,
        Silverfish = 20,
        CaveSpider = 21,
        // Bosses
        EnderDragon = 100,
        Wither = 101,
        // Projectiles
        Arrow = 200,
        Snowball = 201,
        Egg = 202,
        Fireball = 203,
        SmallFireball = 204,
        WitherSkull = 205,
        DragonFireball = 206,
        // Items
        Item = 300,
        XpOrb = 301,
        // Vehicles
        Minecart = 400,
        Boat = 401,
        // Hanging entities
        Painting = 500,
        ItemFrame = 501,
        ArmorStand = 502,
    }

    #endregion

    #region Player Actions

    /// <summary>
    /// Player action types
    /// </summary>
    public enum PlayerAction : byte
    {
        None = 0,
        StartDestroyBlock = 1,
        AbortDestroyBlock = 2,
        StopDestroyBlock = 3,
        DropAllItems = 4,
        DropOneItem = 5,
        ReleaseUseItem = 6,
        SwapHandItems = 7,
    }

    #endregion

    #region Block Faces

    /// <summary>
    /// Block face directions
    /// </summary>
    public enum BlockFace : byte
    {
        Bottom = 0,
        Top = 1,
        North = 2,
        South = 3,
        West = 4,
        East = 5,
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// 3D integer vector
    /// </summary>
    public struct Vector3Int
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3Int Zero => new Vector3Int(0, 0, 0);
        public static Vector3Int One => new Vector3Int(1, 1, 1);

        public override string ToString() => $"({X}, {Y}, {Z})";

        public override bool Equals(object obj)
        {
            if (obj is Vector3Int other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(Vector3Int left, Vector3Int right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3Int left, Vector3Int right)
        {
            return !(left == right);
        }

        public static Vector3Int operator +(Vector3Int left, Vector3Int right)
        {
            return new Vector3Int(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Vector3Int operator -(Vector3Int left, Vector3Int right)
        {
            return new Vector3Int(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }
    }

    /// <summary>
    /// 3D floating point vector
    /// </summary>
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 Zero => new Vector3(0, 0, 0);
        public static Vector3 One => new Vector3(1, 1, 1);
        public static Vector3 Up => new Vector3(0, 1, 0);
        public static Vector3 Down => new Vector3(0, -1, 0);
        public static Vector3 Forward => new Vector3(0, 0, 1);
        public static Vector3 Back => new Vector3(0, 0, -1);
        public static Vector3 Left => new Vector3(-1, 0, 0);
        public static Vector3 Right => new Vector3(1, 0, 0);

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public float LengthSquared => X * X + Y * Y + Z * Z;

        public Vector3 Normalized()
        {
            float len = Length;
            if (len > 0.0001f)
            {
                return new Vector3(X / len, Y / len, Z / len);
            }
            return Zero;
        }

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";

        public override bool Equals(object obj)
        {
            if (obj is Vector3 other)
            {
                return Math.Abs(X - other.X) < 0.0001f &&
                       Math.Abs(Y - other.Y) < 0.0001f &&
                       Math.Abs(Z - other.Z) < 0.0001f;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return !(left == right);
        }

        public static Vector3 operator +(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Vector3 operator -(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Vector3 operator *(Vector3 left, float scalar)
        {
            return new Vector3(left.X * scalar, left.Y * scalar, left.Z * scalar);
        }

        public static Vector3 operator *(float scalar, Vector3 right)
        {
            return new Vector3(scalar * right.X, scalar * right.Y, scalar * right.Z);
        }

        public static Vector3 operator /(Vector3 left, float scalar)
        {
            return new Vector3(left.X / scalar, left.Y / scalar, left.Z / scalar);
        }

        public static float Dot(Vector3 left, Vector3 right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        public static Vector3 Cross(Vector3 left, Vector3 right)
        {
            return new Vector3(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X);
        }
    }

    /// <summary>
    /// 2D floating point vector
    /// </summary>
    public struct Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Zero => new Vector2(0, 0);
        public static Vector2 One => new Vector2(1, 1);

        public float Length => (float)Math.Sqrt(X * X + Y * Y);
        public float LengthSquared => X * X + Y * Y;

        public Vector2 Normalized()
        {
            float len = Length;
            if (len > 0.0001f)
            {
                return new Vector2(X / len, Y / len);
            }
            return Zero;
        }

        public override string ToString() => $"({X:F2}, {Y:F2})";

        public static Vector2 operator +(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X + right.X, left.Y + right.Y);
        }

        public static Vector2 operator -(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }

        public static Vector2 operator *(Vector2 left, float scalar)
        {
            return new Vector2(left.X * scalar, left.Y * scalar);
        }

        public static float Dot(Vector2 left, Vector2 right)
        {
            return left.X * right.X + left.Y * right.Y;
        }
    }

    /// <summary>
    /// Quaternion for rotation
    /// </summary>
    public struct Quaternion
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Quaternion Identity => new Quaternion(0, 0, 0, 1);

        public static Quaternion Euler(float x, float y, float z)
        {
            float cx = (float)Math.Cos(x * 0.5f);
            float sx = (float)Math.Sin(x * 0.5f);
            float cy = (float)Math.Cos(y * 0.5f);
            float sy = (float)Math.Sin(y * 0.5f);
            float cz = (float)Math.Cos(z * 0.5f);
            float sz = (float)Math.Sin(z * 0.5f);

            return new Quaternion(
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz);
        }

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2}, {W:F2})";
    }

    /// <summary>
    /// Axis-aligned bounding box
    /// </summary>
    public struct BoundingBox
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public Vector3 Center => (Min + Max) * 0.5f;
        public Vector3 Size => Max - Min;

        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        public bool Intersects(BoundingBox other)
        {
            return Min.X <= other.Max.X && Max.X >= other.Min.X &&
                   Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
                   Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
        }
    }

    /// <summary>
    /// Color structure (RGBA)
    /// </summary>
    public struct Color
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }

        public Color(float r, float g, float b, float a = 1.0f)
        {
            R = Math.Clamp(r, 0.0f, 1.0f);
            G = Math.Clamp(g, 0.0f, 1.0f);
            B = Math.Clamp(b, 0.0f, 1.0f);
            A = Math.Clamp(a, 0.0f, 1.0f);
        }

        public static Color White => new Color(1, 1, 1);
        public static Color Black => new Color(0, 0, 0);
        public static Color Red => new Color(1, 0, 0);
        public static Color Green => new Color(0, 1, 0);
        public static Color Blue => new Color(0, 0, 1);
        public static Color Yellow => new Color(1, 1, 0);
        public static Color Cyan => new Color(0, 1, 1);
        public static Color Magenta => new Color(1, 0, 1);
        public static Color Transparent => new Color(0, 0, 0, 0);

        public uint ToARGB()
        {
            byte a = (byte)(A * 255);
            byte r = (byte)(R * 255);
            byte g = (byte)(G * 255);
            byte b = (byte)(B * 255);
            return (uint)((a << 24) | (r << 16) | (g << 8) | b);
        }

        public override string ToString() => $"RGBA({R:F2}, {G:F2}, {B:F2}, {A:F2})";
    }

    #endregion

    #region Constants

    /// <summary>
    /// Game constants
    /// </summary>
    public static class GameConstants
    {
        // World dimensions
        public const int ChunkSize = 16;
        public const int ChunkHeight = 256;
        public const int ChunkSectionHeight = 16;
        public const int WorldHeight = 256;
        public const int SeaLevel = 64;
        public const int BedrockHeight = 0;

        // Time
        public const int DayLengthTicks = 24000;
        public const int DayTimeTicks = 12000;
        public const int NightTimeTicks = 12000;
        public const int TickRate = 20;

        // Player
        public const float PlayerHeight = 1.8f;
        public const float PlayerWidth = 0.6f;
        public const float PlayerEyeHeight = 1.62f;
        public const float PlayerReachDistance = 4.5f;
        public const float DefaultHealth = 20.0f;
        public const float MaxHealth = 20.0f;
        public const float DefaultHunger = 20.0f;
        public const float MaxHunger = 20.0f;
        public const float DefaultSaturation = 5.0f;
        public const float MaxSaturation = 20.0f;

        // Inventory
        public const int HotbarSlots = 9;
        public const int InventorySlots = 27;
        public const int ArmorSlots = 4;
        public const int OffhandSlot = 1;
        public const int TotalPlayerSlots = HotbarSlots + InventorySlots + ArmorSlots + OffhandSlot;
        public const int MaxStackSize = 64;

        // View distance
        public const int MinimumViewDistance = 2;
        public const int DefaultViewDistance = 8;
        public const int MaximumViewDistance = 32;

        // Network
        public const int DefaultPort = 7777;
        public const int ProtocolVersion = 1;
        public const int KeepAliveIntervalMs = 15000;
        public const int ConnectionTimeoutMs = 30000;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Common helper methods
    /// </summary>
    public static class GameHelpers
    {
        /// <summary>
        /// Clamp a value between min and max
        /// </summary>
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        /// <summary>
        /// Linear interpolation
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0.0f, 1.0f);
        }

        /// <summary>
        /// Map a value from one range to another
        /// </summary>
        public static float Map(float value, float inMin, float inMax, float outMin, float outMax)
        {
            return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
        }

        /// <summary>
        /// Convert block position to chunk position
        /// </summary>
        public static Vector3Int BlockToChunk(Vector3Int blockPos)
        {
            return new Vector3Int(
                blockPos.X >> 4,
                blockPos.Y >> 4,
                blockPos.Z >> 4);
        }

        /// <summary>
        /// Convert chunk position to block position
        /// </summary>
        public static Vector3Int ChunkToBlock(Vector3Int chunkPos)
        {
            return new Vector3Int(
                chunkPos.X << 4,
                chunkPos.Y << 4,
                chunkPos.Z << 4);
        }

        /// <summary>
        /// Get block position within a chunk
        /// </summary>
        public static Vector3Int GetLocalBlockPosition(Vector3Int blockPos)
        {
            return new Vector3Int(
                blockPos.X & 0xF,
                blockPos.Y & 0xF,
                blockPos.Z & 0xF);
        }

        /// <summary>
        /// Calculate distance between two positions
        /// </summary>
        public static float Distance(Vector3 a, Vector3 b)
        {
            return (a - b).Length;
        }

        /// <summary>
        /// Calculate squared distance between two positions
        /// </summary>
        public static float DistanceSquared(Vector3 a, Vector3 b)
        {
            return (a - b).LengthSquared;
        }
    }

    #endregion
}
using System.Collections.Generic;

namespace MinecraftGame.Common
{
    /// <summary>
    /// Common types and enums shared between client and server
    /// This DLL contains all shared protocol-independent types
    /// </summary>

    #region Block Types

    /// <summary>
    /// Block type identifiers
    /// </summary>
    public enum BlockType : byte
    {
        Air = 0,
        Stone = 1,
        Grass = 2,
        Dirt = 3,
        Cobblestone = 4,
        WoodenPlanks = 5,
        Sapling = 6,
        Bedrock = 7,
        Water = 8,
        StationaryWater = 9,
        Lava = 10,
        StationaryLava = 11,
        Sand = 12,
        Gravel = 13,
        GoldOre = 14,
        IronOre = 15,
        CoalOre = 16,
        Log = 17,
        Leaves = 18,
        Sponge = 19,
        Glass = 20,
        LapisOre = 21,
        LapisBlock = 22,
        Dispenser = 23,
        Sandstone = 24,
        NoteBlock = 25,
        Bed = 26,
        PoweredRail = 27,
        DetectorRail = 28,
        StickyPiston = 29,
        Web = 30,
        TallGrass = 31,
        DeadBush = 32,
        Piston = 33,
        PistonHead = 34,
        Wool = 35,
        PistonExtension = 36,
        YellowFlower = 37,
        RedFlower = 38,
        BrownMushroom = 39,
        RedMushroom = 40,
        GoldBlock = 41,
        IronBlock = 42,
        DoubleStoneSlab = 43,
        StoneSlab = 44,
        BrickBlock = 45,
        TNT = 46,
        Bookshelf = 47,
        MossyCobblestone = 48,
        Obsidian = 49,
        Torch = 50,
        Fire = 51,
        MobSpawner = 52,
        OakWoodStairs = 53,
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
        WoodenDoor = 64,
        Ladder = 65,
        Rail = 66,
        CobblestoneStairs = 67,
        WallSign = 68,
        Lever = 69,
        StonePressurePlate = 70,
        IronDoor = 71,
        WoodenPressurePlate = 72,
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
        Cake = 92,
        RedstoneRepeaterOff = 93,
        RedstoneRepeaterOn = 94,
        StainedGlass = 95,
        Trapdoor = 96,
        MonsterEgg = 97,
        StoneBricks = 98,
        BrownMushroomBlock = 99,
        RedMushroomBlock = 100,
        IronBars = 101,
        GlassPane = 102,
        Melon = 103,
        PumpkinStem = 104,
        MelonStem = 105,
        Vine = 106,
        FenceGate = 107,
        BrickStairs = 108,
        StoneBrickStairs = 109,
        Mycelium = 110,
        LilyPad = 111,
        NetherBrick = 112,
        NetherBrickFence = 113,
        NetherBrickStairs = 114,
        NetherWart = 115,
        EnchantingTable = 116,
        BrewingStand = 117,
        Cauldron = 118,
        EndPortal = 119,
        EndPortalFrame = 120,
        EndStone = 121,
        DragonEgg = 122,
        RedstoneLampOff = 123,
        RedstoneLampOn = 124,
        DoubleWoodenSlab = 125,
        WoodenSlab = 126,
        Cocoa = 127,
        SandstoneStairs = 128,
        EmeraldOre = 129,
        EnderChest = 130,
        TripwireHook = 131,
        Tripwire = 132,
        EmeraldBlock = 133,
        SpruceWoodStairs = 134,
        BirchWoodStairs = 135,
        JungleWoodStairs = 136,
        CommandBlock = 137,
        Beacon = 138,
        CobblestoneWall = 139,
        FlowerPot = 140,
        Carrots = 141,
        Potatoes = 142,
        WoodenButton = 143,
        Skull = 144,
        Anvil = 145,
        TrappedChest = 146,
        LightWeightedPressurePlate = 147,
        HeavyWeightedPressurePlate = 148,
        ComparatorOff = 149,
        ComparatorOn = 150,
        DaylightDetector = 151,
        RedstoneBlock = 152,
        QuartzOre = 153,
        Hopper = 154,
        QuartzBlock = 155,
        QuartzStairs = 156,
        ActivatorRail = 157,
        Dropper = 158,
        StainedHardenedClay = 159,
        StainedGlassPane = 160,
        Leaves2 = 161,
        Log2 = 162,
        StainedGlass2 = 163,
        StainedGlassPane2 = 164,
        StainedHardenedClay2 = 165,
        Stone2 = 166,
        PackedIce = 167,
        DoublePlant = 168,
        StandingBanner = 169,
        WallBanner = 170,
        DaylightDetectorInverted = 171,
        RedSandstone = 172,
        RedSandstoneStairs = 173,
        DoubleStoneSlab2 = 174,
        StoneSlab2 = 175,
        FenceGate2 = 176,
        SpruceFenceGate = 177,
        BirchFenceGate = 178,
        JungleFenceGate = 179,
        DarkOakFenceGate = 180,
        AcaciaFenceGate = 181,
        SpruceDoor = 182,
        BirchDoor = 183,
        JungleDoor = 184,
        AcaciaDoor = 185,
        DarkOakDoor = 186,
        EndRod = 197,
    }

    #endregion

    #region Item Types

    /// <summary>
    /// Item type identifiers
    /// </summary>
    public enum ItemType : byte
    {
        None = 0,
        IronShovel = 1,
        IronPickaxe = 2,
        IronAxe = 3,
        FlintAndSteel = 4,
        Apple = 5,
        Bow = 6,
        Arrow = 7,
        Coal = 8,
        Charcoal = 9,
        Diamond = 10,
        IronIngot = 11,
        GoldIngot = 12,
        IronSword = 13,
        WoodenSword = 14,
        WoodenShovel = 15,
        WoodenPickaxe = 16,
        WoodenAxe = 17,
        StoneSword = 18,
        StoneShovel = 19,
        StonePickaxe = 20,
        StoneAxe = 21,
        DiamondSword = 22,
        DiamondShovel = 23,
        DiamondPickaxe = 24,
        DiamondAxe = 25,
        Stick = 26,
        Bowl = 27,
        MushroomStew = 28,
        GoldenSword = 29,
        GoldenShovel = 30,
        GoldenPickaxe = 31,
        GoldenAxe = 32,
        String = 33,
        Feather = 34,
        Gunpowder = 35,
        WoodenHoe = 36,
        StoneHoe = 37,
        IronHoe = 38,
        DiamondHoe = 39,
        GoldenHoe = 40,
        Seeds = 41,
        Wheat = 42,
        Bread = 43,
        LeatherHelmet = 44,
        LeatherChestplate = 45,
        LeatherLeggings = 46,
        LeatherBoots = 47,
        ChainmailHelmet = 48,
        ChainmailChestplate = 49,
        ChainmailLeggings = 50,
        ChainmailBoots = 51,
        IronHelmet = 52,
        IronChestplate = 53,
        IronLeggings = 54,
        IronBoots = 55,
        DiamondHelmet = 56,
        DiamondChestplate = 57,
        DiamondLeggings = 58,
        DiamondBoots = 59,
        GoldenHelmet = 60,
        GoldenChestplate = 61,
        GoldenLeggings = 62,
        GoldenBoots = 63,
        Flint = 64,
        Porkchop = 65,
        CookedPorkchop = 66,
        Painting = 67,
        GoldenApple = 68,
        Sign = 69,
        WoodenDoor = 70,
        Bucket = 71,
        WaterBucket = 72,
        LavaBucket = 73,
        Minecart = 74,
        Saddle = 75,
        IronDoor = 76,
        Redstone = 77,
        Snowball = 78,
        Boat = 79,
        Leather = 80,
        MilkBucket = 81,
        Brick = 82,
        ClayBall = 83,
        SugarCane = 84,
        Paper = 85,
        Book = 86,
        Slimeball = 87,
        StorageMinecart = 88,
        PoweredMinecart = 89,
        Egg = 90,
        Compass = 91,
        FishingRod = 92,
        Clock = 93,
        GlowstoneDust = 94,
        Fish = 95,
        CookedFish = 96,
        Dye = 97,
        Bone = 98,
        Sugar = 99,
        Cake = 100,
        Bed = 101,
        Repeater = 102,
        Cookie = 103,
        Map = 104,
        Shears = 105,
        Melon = 106,
        PumpkinSeeds = 107,
        MelonSeeds = 108,
        Beef = 109,
        CookedBeef = 110,
        RawChicken = 111,
        CookedChicken = 112,
        RottenFlesh = 113,
        EnderPearl = 114,
        BlazeRod = 115,
        GhastTear = 116,
        GoldNugget = 117,
        NetherWart = 118,
        Potion = 119,
        GlassBottle = 120,
        SpiderEye = 121,
        FermentedSpiderEye = 122,
        BlazePowder = 123,
        MagmaCream = 124,
        BrewingStandItem = 125,
        CauldronItem = 126,
        EyeOfEnder = 127,
        SpeckledMelon = 128,
        MonsterEgg = 129,
        ExpBottle = 130,
        FireCharge = 131,
        BookAndQuill = 132,
        WrittenBook = 133,
        Emerald = 134,
        ItemFrame = 135,
        FlowerPot = 136,
        Carrot = 137,
        Potato = 138,
        BakedPotato = 139,
        PoisonousPotato = 140,
        GoldenCarrot = 141,
        Skull = 142,
        CarrotOnAStick = 143,
        NetherStar = 144,
        PumpkinPie = 145,
        Fireworks = 146,
        FireworkCharge = 147,
        EnchantedBook = 148,
        Comparator = 149,
        NetherBrick = 150,
        NetherQuartz = 151,
        MinecartWithTNT = 152,
        HopperMinecart = 153,
        PrismarineShard = 154,
        PrismarineCrystals = 155,
        Rabbit = 156,
        CookedRabbit = 157,
        RabbitStew = 158,
        RabbitFoot = 159,
        RabbitHide = 160,
        ArmorStand = 161,
        IronHorseArmor = 162,
        GoldenHorseArmor = 163,
        DiamondHorseArmor = 164,
        Lead = 165,
        NameTag = 166,
        CommandBlockMinecart = 167,
        Mutton = 168,
        CookedMutton = 169,
        Banner = 170,
        SpruceDoor = 171,
        BirchDoor = 172,
        JungleDoor = 173,
        AcaciaDoor = 174,
        DarkOakDoor = 175,
        EndCrystal = 176,
        ChorusFruit = 177,
        ChorusFruitPopped = 178,
        Beetroot = 179,
        BeetrootSeeds = 180,
        BeetrootSoup = 181,
        DragonBreath = 182,
        SplashPotion = 183,
        LingeringPotion = 184,
        TippedArrow = 185,
        Shield = 186,
        Elytra = 187,
        SpruceBoat = 188,
        BirchBoat = 189,
        JungleBoat = 190,
        AcaciaBoat = 191,
        DarkOakBoat = 192,
        TotemOfUndying = 193,
        ShulkerShell = 194,
        IronNugget = 195,
        Record13 = 196,
        RecordCat = 197,
        RecordBlocks = 198,
        RecordChirp = 199,
        RecordFar = 200,
        RecordMall = 201,
        RecordMellohi = 202,
        RecordStal = 203,
        RecordStrad = 204,
        RecordWard = 205,
        Record11 = 206,
        RecordWait = 207,
    }

    #endregion

    #region Game Modes

    /// <summary>
    /// Game mode types
    /// </summary>
    public enum GameMode : byte
    {
        Survival = 0,
        Creative = 1,
        Adventure = 2,
        Spectator = 3
    }

    #endregion

    #region Dimensions

    /// <summary>
    /// World dimension types
    /// </summary>
    public enum Dimension : int
    {
        Overworld = 0,
        Nether = -1,
        End = 1
    }

    #endregion

    #region Biome Types

    /// <summary>
    /// Biome type identifiers
    /// </summary>
    public enum BiomeType : byte
    {
        Ocean = 0,
        Plains = 1,
        Desert = 2,
        Mountains = 3,
        Forest = 4,
        Taiga = 5,
        Swamp = 6,
        River = 7,
        Nether = 8,
        TheEnd = 9,
        FrozenOcean = 10,
        FrozenRiver = 11,
        SnowyTundra = 12,
        SnowyMountains = 13,
        MushroomFields = 14,
        MushroomFieldShore = 15,
        Beach = 16,
        DesertHills = 17,
        ForestHills = 18,
        TaigaHills = 19,
        MountainsEdge = 20,
        Jungle = 21,
        JungleHills = 22,
        JungleEdge = 23,
        DeepOcean = 24,
        StoneShore = 25,
        BirchForest = 26,
        BirchForestHills = 27,
        DarkForest = 28,
        SnowyBeach = 29,
        BirchForestMountains = 30,
        SnowyTaiga = 31,
        SnowyTaigaHills = 32,
        GiantTreeTaiga = 33,
        GiantTreeTaigaHills = 34,
        Mountains = 35,
        GravellyMountains = 36,
        FlowerForest = 37,
        IceSpikes = 38,
        SunflowerPlains = 129,
        DesertLakes = 130,
        GravellyMountainsPlus = 131,
        ShatteredSavanna = 132,
        ShatteredSavannaPlateau = 133,
        ErodedBadlands = 134,
        ModifiedBadlandsPlateau = 135,
        ModifiedWoodedBadlandsPlateau = 136,
        BambooJungle = 137,
        BambooJungleHills = 138,
        SoulSandValley = 139,
        CrimsonForest = 140,
        WarpedForest = 141,
        BasaltDeltas = 142,
    }

    #endregion

    #region Sound Types

    /// <summary>
    /// Sound effect types
    /// </summary>
    public enum SoundType : ushort
    {
        None = 0,
        // Block sounds
        BlockStoneBreak = 1,
        BlockStonePlace = 2,
        BlockStoneHit = 3,
        BlockStoneStep = 4,
        BlockWoodBreak = 5,
        BlockWoodPlace = 6,
        BlockWoodHit = 7,
        BlockWoodStep = 8,
        BlockGrassBreak = 9,
        BlockGrassPlace = 10,
        BlockGrassHit = 11,
        BlockGrassStep = 12,
        BlockGravelBreak = 13,
        BlockGravelPlace = 14,
        BlockGravelHit = 15,
        BlockGravelStep = 16,
        BlockSandBreak = 17,
        BlockSandPlace = 18,
        BlockSandHit = 19,
        BlockSandStep = 20,
        BlockGlassBreak = 21,
        BlockGlassPlace = 22,
        BlockGlassHit = 23,
        BlockClothBreak = 24,
        BlockClothPlace = 25,
        BlockClothHit = 26,
        BlockTntBreak = 27,
        BlockTntPlace = 28,
        BlockTntHit = 29,
        BlockWaterSplash = 30,
        BlockWaterAmbient = 31,
        BlockWaterFlow = 32,
        BlockLavaSplash = 33,
        BlockLavaAmbient = 34,
        BlockLavaFlow = 35,
        // Footstep sounds
        FootstepStone = 100,
        FootstepWood = 101,
        FootstepGrass = 102,
        FootstepGravel = 103,
        FootstepSand = 104,
        FootstepSnow = 105,
        FootstepGlass = 106,
        FootstepMetal = 107,
        // Entity sounds
        EntityPlayerHurt = 200,
        EntityPlayerDeath = 201,
        EntityMobHurt = 202,
        EntityMobDeath = 203,
        EntityAmbient = 204,
        EntityStep = 205,
        // Item sounds
        ItemPickup = 300,
        ItemDrop = 301,
        ItemEquip = 302,
        ItemBreak = 303,
        // Weather sounds
        WeatherRain = 400,
        WeatherThunder = 401,
        // Music
        MusicMenu = 500,
        MusicGame = 501,
        MusicCreative = 502,
        MusicNether = 503,
        MusicEnd = 504,
    }

    #endregion

    #region Sound Categories

    /// <summary>
    /// Sound effect categories for volume control
    /// </summary>
    public enum SoundCategory : byte
    {
        Master = 0,
        SndMusic = 1,
        SndRecord = 2,
        SndWeather = 3,
        SndBlock = 4,
        SndHostile = 5,
        SndNeutral = 6,
        SndPlayer = 7,
        SndAmbient = 8,
        SndVoice = 9,
    }

    #endregion

    #region Particle Types

    /// <summary>
    /// Particle effect types
    /// </summary>
    public enum ParticleType : byte
    {
        None = 0,
        Explode = 1,
        LargeExplosion = 2,
        HugeExplosion = 3,
        FireworksSpark = 4,
        Bubble = 5,
        Splash = 6,
        Wake = 7,
        Suspended = 8,
        Depthsuspend = 9,
        Crit = 10,
        MagicCrit = 11,
        Smoke = 12,
        LargeSmoke = 13,
        Spell = 14,
        InstantSpell = 15,
        MobSpell = 16,
        MobSpellAmbient = 17,
        WitchMagic = 18,
        DripWater = 19,
        DripLava = 20,
        AngryVillager = 21,
        HappyVillager = 22,
        TownAura = 23,
        Note = 24,
        Portal = 25,
        EnchantmentTable = 26,
        Flame = 27,
        Lava = 28,
        Footstep = 29,
        Cloud = 30,
        Redstone = 31,
        Snowballpoof = 32,
        SnowShovel = 33,
        Slime = 34,
        Heart = 35,
        Barrier = 36,
        IconCrack = 37,
        BlockCrack = 38,
        BlockDust = 39,
        Droplet = 40,
        Take = 41,
        MobAppearance = 42,
        DragonBreath = 43,
        EndRod = 44,
        DamageIndicator = 45,
        SweepAttack = 46,
        FallingDust = 47,
        TotemOfUndying = 48,
        Spit = 49,
    }

    #endregion

    #region Chat Types

    /// <summary>
    /// Chat message types
    /// </summary>
    public enum ChatType : byte
    {
        ChatGlobal = 0,
        ChatTeam = 1,
        ChatPrivate = 2,
        ChatSystem = 3,
        ChatAnnouncement = 4,
        ChatEmote = 5,
    }

    #endregion

    #region Weather Types

    /// <summary>
    /// Weather types
    /// </summary>
    public enum WeatherType : byte
    {
        Clear = 0,
        Rain = 1,
        Thunder = 2,
        Snow = 3,
    }

    #endregion

    #region Entity Types

    /// <summary>
    /// Entity type identifiers
    /// </summary>
    public enum EntityType : ushort
    {
        // Passive mobs
        Chicken = 1,
        Cow = 2,
        Pig = 3,
        Sheep = 4,
        Wolf = 5,
        Ocelot = 6,
        Horse = 7,
        Rabbit = 8,
        Villager = 9,
        // Hostile mobs
        Zombie = 10,
        Skeleton = 11,
        Spider = 12,
        Creeper = 13,
        Enderman = 14,
        Witch = 15,
        Slime = 16,
        Ghast = 17,
        Blaze = 18,
        PigZombie = 19,
        Silverfish = 20,
        CaveSpider = 21,
        // Bosses
        EnderDragon = 100,
        Wither = 101,
        // Projectiles
        Arrow = 200,
        Snowball = 201,
        Egg = 202,
        Fireball = 203,
        SmallFireball = 204,
        WitherSkull = 205,
        DragonFireball = 206,
        // Items
        Item = 300,
        XpOrb = 301,
        // Vehicles
        Minecart = 400,
        Boat = 401,
        // Hanging entities
        Painting = 500,
        ItemFrame = 501,
        ArmorStand = 502,
    }

    #endregion

    #region Player Actions

    /// <summary>
    /// Player action types
    /// </summary>
    public enum PlayerAction : byte
    {
        None = 0,
        StartDestroyBlock = 1,
        AbortDestroyBlock = 2,
        StopDestroyBlock = 3,
        DropAllItems = 4,
        DropOneItem = 5,
        ReleaseUseItem = 6,
        SwapHandItems = 7,
    }

    #endregion

    #region Block Faces

    /// <summary>
    /// Block face directions
    /// </summary>
    public enum BlockFace : byte
    {
        Bottom = 0,
        Top = 1,
        North = 2,
        South = 3,
        West = 4,
        East = 5,
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// 3D integer vector
    /// </summary>
    public struct Vector3Int
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3Int Zero => new Vector3Int(0, 0, 0);
        public static Vector3Int One => new Vector3Int(1, 1, 1);

        public override string ToString() => $"({X}, {Y}, {Z})";

        public override bool Equals(object obj)
        {
            if (obj is Vector3Int other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(Vector3Int left, Vector3Int right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3Int left, Vector3Int right)
        {
            return !(left == right);
        }

        public static Vector3Int operator +(Vector3Int left, Vector3Int right)
        {
            return new Vector3Int(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Vector3Int operator -(Vector3Int left, Vector3Int right)
        {
            return new Vector3Int(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }
    }

    /// <summary>
    /// 3D floating point vector
    /// </summary>
    public struct Vector3
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 Zero => new Vector3(0, 0, 0);
        public static Vector3 One => new Vector3(1, 1, 1);
        public static Vector3 Up => new Vector3(0, 1, 0);
        public static Vector3 Down => new Vector3(0, -1, 0);
        public static Vector3 Forward => new Vector3(0, 0, 1);
        public static Vector3 Back => new Vector3(0, 0, -1);
        public static Vector3 Left => new Vector3(-1, 0, 0);
        public static Vector3 Right => new Vector3(1, 0, 0);

        public float Length => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public float LengthSquared => X * X + Y * Y + Z * Z;

        public Vector3 Normalized()
        {
            float len = Length;
            if (len > 0.0001f)
            {
                return new Vector3(X / len, Y / len, Z / len);
            }
            return Zero;
        }

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";

        public override bool Equals(object obj)
        {
            if (obj is Vector3 other)
            {
                return Math.Abs(X - other.X) < 0.0001f &&
                       Math.Abs(Y - other.Y) < 0.0001f &&
                       Math.Abs(Z - other.Z) < 0.0001f;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return !(left == right);
        }

        public static Vector3 operator +(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        public static Vector3 operator -(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        public static Vector3 operator *(Vector3 left, float scalar)
        {
            return new Vector3(left.X * scalar, left.Y * scalar, left.Z * scalar);
        }

        public static Vector3 operator *(float scalar, Vector3 right)
        {
            return new Vector3(scalar * right.X, scalar * right.Y, scalar * right.Z);
        }

        public static Vector3 operator /(Vector3 left, float scalar)
        {
            return new Vector3(left.X / scalar, left.Y / scalar, left.Z / scalar);
        }

        public static float Dot(Vector3 left, Vector3 right)
        {
            return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        }

        public static Vector3 Cross(Vector3 left, Vector3 right)
        {
            return new Vector3(
                left.Y * right.Z - left.Z * right.Y,
                left.Z * right.X - left.X * right.Z,
                left.X * right.Y - left.Y * right.X);
        }
    }

    /// <summary>
    /// 2D floating point vector
    /// </summary>
    public struct Vector2
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 Zero => new Vector2(0, 0);
        public static Vector2 One => new Vector2(1, 1);

        public float Length => (float)Math.Sqrt(X * X + Y * Y);
        public float LengthSquared => X * X + Y * Y;

        public Vector2 Normalized()
        {
            float len = Length;
            if (len > 0.0001f)
            {
                return new Vector2(X / len, Y / len);
            }
            return Zero;
        }

        public override string ToString() => $"({X:F2}, {Y:F2})";

        public static Vector2 operator +(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X + right.X, left.Y + right.Y);
        }

        public static Vector2 operator -(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }

        public static Vector2 operator *(Vector2 left, float scalar)
        {
            return new Vector2(left.X * scalar, left.Y * scalar);
        }

        public static float Dot(Vector2 left, Vector2 right)
        {
            return left.X * right.X + left.Y * right.Y;
        }
    }

    /// <summary>
    /// Quaternion for rotation
    /// </summary>
    public struct Quaternion
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }

        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Quaternion Identity => new Quaternion(0, 0, 0, 1);

        public static Quaternion Euler(float x, float y, float z)
        {
            float cx = (float)Math.Cos(x * 0.5f);
            float sx = (float)Math.Sin(x * 0.5f);
            float cy = (float)Math.Cos(y * 0.5f);
            float sy = (float)Math.Sin(y * 0.5f);
            float cz = (float)Math.Cos(z * 0.5f);
            float sz = (float)Math.Sin(z * 0.5f);

            return new Quaternion(
                sx * cy * cz - cx * sy * sz,
                cx * sy * cz + sx * cy * sz,
                cx * cy * sz - sx * sy * cz,
                cx * cy * cz + sx * sy * sz);
        }

        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2}, {W:F2})";
    }

    /// <summary>
    /// Axis-aligned bounding box
    /// </summary>
    public struct BoundingBox
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public Vector3 Center => (Min + Max) * 0.5f;
        public Vector3 Size => Max - Min;

        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        public bool Intersects(BoundingBox other)
        {
            return Min.X <= other.Max.X && Max.X >= other.Min.X &&
                   Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
                   Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
        }
    }

    /// <summary>
    /// Color structure (RGBA)
    /// </summary>
    public struct Color
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
        public float A { get; set; }

        public Color(float r, float g, float b, float a = 1.0f)
        {
            R = Math.Clamp(r, 0.0f, 1.0f);
            G = Math.Clamp(g, 0.0f, 1.0f);
            B = Math.Clamp(b, 0.0f, 1.0f);
            A = Math.Clamp(a, 0.0f, 1.0f);
        }

        public static Color White => new Color(1, 1, 1);
        public static Color Black => new Color(0, 0, 0);
        public static Color Red => new Color(1, 0, 0);
        public static Color Green => new Color(0, 1, 0);
        public static Color Blue => new Color(0, 0, 1);
        public static Color Yellow => new Color(1, 1, 0);
        public static Color Cyan => new Color(0, 1, 1);
        public static Color Magenta => new Color(1, 0, 1);
        public static Color Transparent => new Color(0, 0, 0, 0);

        public uint ToARGB()
        {
            byte a = (byte)(A * 255);
            byte r = (byte)(R * 255);
            byte g = (byte)(G * 255);
            byte b = (byte)(B * 255);
            return (uint)((a << 24) | (r << 16) | (g << 8) | b);
        }

        public override string ToString() => $"RGBA({R:F2}, {G:F2}, {B:F2}, {A:F2})";
    }

    #endregion

    #region Constants

    /// <summary>
    /// Game constants
    /// </summary>
    public static class GameConstants
    {
        // World dimensions
        public const int ChunkSize = 16;
        public const int ChunkHeight = 256;
        public const int ChunkSectionHeight = 16;
        public const int WorldHeight = 256;
        public const int SeaLevel = 64;
        public const int BedrockHeight = 0;

        // Time
        public const int DayLengthTicks = 24000;
        public const int DayTimeTicks = 12000;
        public const int NightTimeTicks = 12000;
        public const int TickRate = 20;

        // Player
        public const float PlayerHeight = 1.8f;
        public const float PlayerWidth = 0.6f;
        public const float PlayerEyeHeight = 1.62f;
        public const float PlayerReachDistance = 4.5f;
        public const float DefaultHealth = 20.0f;
        public const float MaxHealth = 20.0f;
        public const float DefaultHunger = 20.0f;
        public const float MaxHunger = 20.0f;
        public const float DefaultSaturation = 5.0f;
        public const float MaxSaturation = 20.0f;

        // Inventory
        public const int HotbarSlots = 9;
        public const int InventorySlots = 27;
        public const int ArmorSlots = 4;
        public const int OffhandSlot = 1;
        public const int TotalPlayerSlots = HotbarSlots + InventorySlots + ArmorSlots + OffhandSlot;
        public const int MaxStackSize = 64;

        // View distance
        public const int MinimumViewDistance = 2;
        public const int DefaultViewDistance = 8;
        public const int MaximumViewDistance = 32;

        // Network
        public const int DefaultPort = 7777;
        public const int ProtocolVersion = 1;
        public const int KeepAliveIntervalMs = 15000;
        public const int ConnectionTimeoutMs = 30000;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Common helper methods
    /// </summary>
    public static class GameHelpers
    {
        /// <summary>
        /// Clamp a value between min and max
        /// </summary>
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        /// <summary>
        /// Linear interpolation
        /// </summary>
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0.0f, 1.0f);
        }

        /// <summary>
        /// Map a value from one range to another
        /// </summary>
        public static float Map(float value, float inMin, float inMax, float outMin, float outMax)
        {
            return (value - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
        }

        /// <summary>
        /// Convert block position to chunk position
        /// </summary>
        public static Vector3Int BlockToChunk(Vector3Int blockPos)
        {
            return new Vector3Int(
                blockPos.X >> 4,
                blockPos.Y >> 4,
                blockPos.Z >> 4);
        }

        /// <summary>
        /// Convert chunk position to block position
        /// </summary>
        public static Vector3Int ChunkToBlock(Vector3Int chunkPos)
        {
            return new Vector3Int(
                chunkPos.X << 4,
                chunkPos.Y << 4,
                chunkPos.Z << 4);
        }

        /// <summary>
        /// Get block position within a chunk
        /// </summary>
        public static Vector3Int GetLocalBlockPosition(Vector3Int blockPos)
        {
            return new Vector3Int(
                blockPos.X & 0xF,
                blockPos.Y & 0xF,
                blockPos.Z & 0xF);
        }

        /// <summary>
        /// Calculate distance between two positions
        /// </summary>
        public static float Distance(Vector3 a, Vector3 b)
        {
            return (a - b).Length;
        }

        /// <summary>
        /// Calculate squared distance between two positions
        /// </summary>
        public static float DistanceSquared(Vector3 a, Vector3 b)
        {
            return (a - b).LengthSquared;
        }
    }

    #endregion
}

