using UnityEngine;

public sealed class WorldMapControlProfile
{
    public int ChunkSize { get; }
    public int RenderDistance { get; }
    public int SimulationDistance { get; }
    public int GlobalWaterLevel { get; }
    public int HydrologyGradientStabilityIterations { get; }
    public float HydrologyGradientStabilityBlend { get; }
    public float HydrologyCurvatureWeight { get; }
    public int HydrologyEdgeBlendRadius { get; }
    public int HydrologySeamRelaxIterations { get; }
    public float HydrologyVarianceBlend { get; }
    public float HydrologyVarianceClamp { get; }
    public int LakeBasinSmoothIterations { get; }
    public bool EnableRivers { get; }
    public bool EnableLakes { get; }
    public bool EnableCaves { get; }
    public bool UseImprovedRivers { get; }
    public bool UseImprovedLakes { get; }
    public bool UseImprovedCaves { get; }

    public WorldMapControlProfile(WorldConfig config)
    {
        ChunkSize = Mathf.Max(1, config.ChunkSize);
        RenderDistance = Mathf.Max(1, config.RenderDistance);
        SimulationDistance = Mathf.Max(1, config.SimulationDistance);
        GlobalWaterLevel = config.GlobalWaterLevel;
        HydrologyGradientStabilityIterations = Mathf.Max(0, config.HydrologyGradientStabilityIterations);
        HydrologyGradientStabilityBlend = Mathf.Clamp01(config.HydrologyGradientStabilityBlend);
        HydrologyCurvatureWeight = Mathf.Clamp(config.HydrologyCurvatureWeight, 0f, 1.5f);
        HydrologyEdgeBlendRadius = Mathf.Max(1, config.HydrologyEdgeBlendRadius);
        HydrologySeamRelaxIterations = Mathf.Max(0, config.HydrologySeamRelaxIterations);
        HydrologyVarianceBlend = Mathf.Clamp01(config.HydrologyVarianceBlend);
        HydrologyVarianceClamp = Mathf.Clamp(config.HydrologyVarianceClamp, 0f, 1.25f);
        LakeBasinSmoothIterations = Mathf.Max(0, config.LakeBasinSmoothIterations);
        EnableRivers = config.EnableRivers;
        EnableLakes = config.EnableLakes;
        EnableCaves = config.EnableCaves;
        UseImprovedRivers = config.UseImprovedRivers;
        UseImprovedLakes = config.UseImprovedLakes;
        UseImprovedCaves = config.UseImprovedCaves;
    }
}
