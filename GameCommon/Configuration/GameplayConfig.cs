namespace GameCommon.Configuration
{
    /// <summary>
    /// 게임플레이 규칙 설정
    /// config/gameplay.json에서 로드
    /// </summary>
    public class GameplayConfig
    {
        public DifficultySettings Difficulty { get; set; } = new();
        public PlayerSettings Player { get; set; } = new();
        public MobSettings Mobs { get; set; } = new();
        public PhysicsSettings Physics { get; set; } = new();
        public CraftingSettings Crafting { get; set; } = new();
        public TimeSettings Time { get; set; } = new();
    }

    public class DifficultySettings
    {
        public string Difficulty { get; set; } = "normal"; // peaceful, easy, normal, hard
        public bool EnablePvP { get; set; } = false;
        public bool EnableFriendlyFire { get; set; } = false;
        public bool EnableHunger { get; set; } = true;
        public bool EnableNaturalRegeneration { get; set; } = true;
        public double DamageMultiplier { get; set; } = 1.0;
    }

    public class PlayerSettings
    {
        public int MaxHealth { get; set; } = 20;
        public int MaxHunger { get; set; } = 20;
        public double WalkSpeed { get; set; } = 4.317;
        public double SprintSpeed { get; set; } = 5.612;
        public double JumpHeight { get; set; } = 1.25;
        public double Reach { get; set; } = 4.5;
        public int MaxInventorySlots { get; set; } = 36;
        public bool EnableFlying { get; set; } = false;
        public double FlySpeed { get; set; } = 10.0;
    }

    public class MobSettings
    {
        public bool EnableMobSpawning { get; set; } = true;
        public bool EnableMobAI { get; set; } = true;
        public bool EnableHostileMobs { get; set; } = true;
        public bool EnablePassiveMobs { get; set; } = true;
        public int MobSpawnRange { get; set; } = 128;
        public int MaxMobsPerChunk { get; set; } = 10;
        public double MobDespawnDistance { get; set; } = 128.0;
        public double MobHealthMultiplier { get; set; } = 1.0;
        public double MobDamageMultiplier { get; set; } = 1.0;
    }

    public class PhysicsSettings
    {
        public double Gravity { get; set; } = 32.0; // blocks/s²
        public bool EnableBlockGravity { get; set; } = true; // Sand, Gravel
        public bool EnableWaterFlow { get; set; } = true;
        public bool EnableLavaFlow { get; set; } = true;
        public int WaterFlowSpeed { get; set; } = 5; // ticks
        public int LavaFlowSpeed { get; set; } = 30; // ticks
        public int MaxWaterFlowDistance { get; set; } = 7;
        public int MaxLavaFlowDistance { get; set; } = 3;
        public bool EnableFireSpread { get; set; } = true;
        public int FireSpreadSpeed { get; set; } = 30; // ticks
    }

    public class CraftingSettings
    {
        public bool Enable3x3Crafting { get; set; } = true;
        public bool EnableFurnaceSmelting { get; set; } = true;
        public bool EnableBrewingStand { get; set; } = false;
        public bool EnableEnchantingTable { get; set; } = false;
        public bool EnableAnvil { get; set; } = false;
        public int FurnaceSmeltTime { get; set; } = 200; // ticks (10 seconds)
    }

    public class TimeSettings
    {
        public bool EnableDayNightCycle { get; set; } = true;
        public int DayLength { get; set; } = 20; // minutes
        public int NightLength { get; set; } = 10; // minutes
        public bool EnableWeatherCycle { get; set; } = true;
        public double RainChance { get; set; } = 0.1;
        public double ThunderChance { get; set; } = 0.05;
    }
}
