using System;

namespace GameServerApp.World
{
    /// <summary>
    /// Data-driven snapshot for world map control so server and client hydrology/cave previews stay aligned.
    /// </summary>
    public sealed class WorldMapControlProfile
    {
        public int ChunkSize { get; init; }
        public int RenderDistance { get; init; }
        public int SimulationDistance { get; init; }
        public int GlobalWaterLevel { get; init; }
        public int HydrologyGradientStabilityIterations { get; init; }
        public double HydrologyGradientStabilityBlend { get; init; }
        public double HydrologyCurvatureWeight { get; init; }
        public int HydrologyEdgeBlendRadius { get; init; }
        public double HydrologyVarianceBlend { get; init; }
        public double HydrologyVarianceClamp { get; init; }
        public int HydrologySeamRelaxIterations { get; init; }
        public bool EnableRivers { get; init; }
        public bool EnableLakes { get; init; }
        public bool EnableCaves { get; init; }
        public bool UseImprovedCaves { get; init; }
        public bool UseImprovedRivers { get; init; }
        public bool UseImprovedLakes { get; init; }
        public int LakeBasinSmoothIterations { get; init; }
    }
}
