using UnityEngine;

public sealed class WorldMapControlProfile
{
    public int ChunkSize { get; }
    public int RenderDistance { get; }
    public int SimulationDistance { get; }
    public int GlobalWaterLevel { get; }
    public int HydrologyGradientStabilityIterations { get; }
    public float HydrologyGradientStabilityBlend { get; }

    public WorldMapControlProfile(WorldConfig config)
    {
        ChunkSize = Mathf.Max(1, config.ChunkSize);
        RenderDistance = Mathf.Max(1, config.RenderDistance);
        SimulationDistance = Mathf.Max(1, config.SimulationDistance);
        GlobalWaterLevel = config.GlobalWaterLevel;
        HydrologyGradientStabilityIterations = Mathf.Max(0, config.HydrologyGradientStabilityIterations);
        HydrologyGradientStabilityBlend = Mathf.Clamp01(config.HydrologyGradientStabilityBlend);
    }
}
