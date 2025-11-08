namespace GameCommon.Configuration
{
    /// <summary>
    /// 월드 생성 설정
    /// config/world.json에서 로드
    /// </summary>
    public class WorldConfig
    {
        public string WorldName { get; set; } = "HELLO_MY_WORLD";
        public int Seed { get; set; } = 0;
        public string GameMode { get; set; } = "survival"; // survival, creative, adventure
        public int WorldHeight { get; set; } = 256;
        public int ChunkSize { get; set; } = 16;
        public int RenderDistance { get; set; } = 10;
        public int SimulationDistance { get; set; } = 8;

        public TerrainGenerationConfig TerrainGeneration { get; set; } = new();
        public WaterConfig Water { get; set; } = new();
        public CaveConfig Caves { get; set; } = new();
        public OreConfig Ores { get; set; } = new();
        public StructureConfig Structures { get; set; } = new();
    }

    public class TerrainGenerationConfig
    {
        public int SeaLevel { get; set; } = 62;
        public int BedrockLevel { get; set; } = 5;
        public double NoiseScale { get; set; } = 100.0;
        public double NoiseAmplitude { get; set; } = 50.0;
        public int Octaves { get; set; } = 4;
        public double Persistence { get; set; } = 0.5;
        public double Lacunarity { get; set; } = 2.0;

        // 바이옴 설정
        public double BiomeScale { get; set; } = 0.005;
        public double TemperatureScale { get; set; } = 0.003;
        public double HumidityScale { get; set; } = 0.004;

        // 산맥 설정
        public double MountainThreshold { get; set; } = 0.6;
        public int MountainMaxHeight { get; set; } = 200;
        public int PlainBaseHeight { get; set; } = 64;
    }

    public class WaterConfig
    {
        public int GlobalWaterLevel { get; set; } = 62;
        public double RiverCenterThreshold { get; set; } = 0.0125;
        public double RiverBankThreshold { get; set; } = 0.028;
        public double RiverNoiseScale { get; set; } = 0.015;
        public int RiverDepth { get; set; } = 5;
        public bool EnableOceans { get; set; } = true;
        public bool EnableRivers { get; set; } = true;
    }

    public class CaveConfig
    {
        public bool EnableCaves { get; set; } = true;
        public double CaveDensity { get; set; } = 0.3;
        public double CaveNoiseScale { get; set; } = 0.05;
        public double CaveThreshold { get; set; } = 0.6;
        public int MinCaveHeight { get; set; } = 5;
        public int MaxCaveHeight { get; set; } = 128;
    }

    public class OreConfig
    {
        public bool EnableOreGeneration { get; set; } = true;
        public OreVeinSettings Coal { get; set; } = new() { MinHeight = 5, MaxHeight = 128, VeinSize = 17, VeinsPerChunk = 20 };
        public OreVeinSettings Iron { get; set; } = new() { MinHeight = 5, MaxHeight = 64, VeinSize = 9, VeinsPerChunk = 20 };
        public OreVeinSettings Gold { get; set; } = new() { MinHeight = 5, MaxHeight = 32, VeinSize = 9, VeinsPerChunk = 2 };
        public OreVeinSettings Diamond { get; set; } = new() { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 1 };
        public OreVeinSettings Redstone { get; set; } = new() { MinHeight = 5, MaxHeight = 16, VeinSize = 8, VeinsPerChunk = 8 };
        public OreVeinSettings Lapis { get; set; } = new() { MinHeight = 5, MaxHeight = 32, VeinSize = 7, VeinsPerChunk = 1 };
    }

    public class OreVeinSettings
    {
        public int MinHeight { get; set; }
        public int MaxHeight { get; set; }
        public int VeinSize { get; set; }
        public int VeinsPerChunk { get; set; }
    }

    public class StructureConfig
    {
        public bool EnableTrees { get; set; } = true;
        public double TreeDensity { get; set; } = 0.05;
        public bool EnableVillages { get; set; } = false;
        public bool EnableMineshafts { get; set; } = false;
        public bool EnableDungeons { get; set; } = true;
        public double DungeonChance { get; set; } = 0.01;
    }
}
