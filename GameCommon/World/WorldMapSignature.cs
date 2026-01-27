using System;
using System.Security.Cryptography;
using System.Text;

namespace GameCommon.World
{
    /// <summary>
    /// Deterministic signature builder for world map control. Shared by server and Unity to avoid drift.
    /// </summary>
    public static class WorldMapSignature
    {
        public static string Compute(WorldMapSignatureContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var builder = new StringBuilder()
                .Append(context.PipelineVersion).Append('|')
                .Append(context.WorldName).Append('|')
                .Append(context.Seed).Append('|')
                .Append(context.ProtoBaseline).Append('|')
                .Append(context.ProtoComputed).Append('|')
                .Append(context.ProfileVersion).Append('|')
                .Append(context.ProfileHash).Append('|')
                .Append(context.HydrologySignature).Append('|')
                .Append(context.ChunkSize).Append('|')
                .Append(context.WorldHeight).Append('|')
                .Append(context.RenderDistance).Append('|')
                .Append(context.SimulationDistance).Append('|')
                .Append(context.GlobalWaterLevel).Append('|')
                .Append(context.SeaLevel).Append('|')
                .Append(context.FlowPersistence).Append('|')
                .Append(context.FlowGain).Append('|')
                .Append(context.WatershedStitchWeight).Append('|')
                .Append(context.WatershedStitchRadius).Append('|')
                .Append(context.HydrologyGradientStabilityIterations).Append('|')
                .Append(context.HydrologyGradientStabilityBlend).Append('|')
                .Append(context.HydrologyGradientClamp).Append('|')
                .Append(context.HydrologyCurvatureWeight).Append('|')
                .Append(context.HydrologySlopePenalty).Append('|')
                .Append(context.HydrologyWaterTableClampWeight).Append('|')
                .Append(context.HydrologyWaterTableClampRange).Append('|')
                .Append(context.HydrologyWaterTableSlopeWeight).Append('|')
                .Append(context.LakesMinDepth).Append('|')
                .Append(context.LakesMaxDepth).Append('|')
                .Append(context.LakesShelfDepth).Append('|')
                .Append(context.LakesFlowSeepageWeight).Append('|')
                .Append(context.LakeOutflowSealWeight).Append('|')
                .Append(context.CaveCeilingMoistureWeight).Append('|')
                .Append(context.CaveCeilingMoistureClamp).Append('|')
                .Append(context.CaveMoistureFlowClamp).Append('|')
                .Append(context.FloodedCaveNoiseFrequency).Append('|')
                .Append(context.FloodedCaveThreshold).Append('|')
                .Append(context.FloodedCaveProximityWeight).Append('|')
                .Append(context.CaveWaterThreshold).Append('|')
                .Append(context.CaveLavaThreshold).Append('|')
                .Append(context.HydrologyEdgeBlendRadius).Append('|')
                .Append(context.HydrologyEdgeVarianceClamp).Append('|')
                .Append(context.HydrologyEdgeNormalizationBlend).Append('|')
                .Append(context.HydrologyEdgeNormalizationIterations).Append('|')
                .Append(context.HydrologyFlowMemoryWeight).Append('|')
                .Append(context.HydrologyContinuityWeight).Append('|')
                .Append(context.RiverMeanderJitter).Append('|')
                .Append(context.RiverReliefPenaltyWeight).Append('|')
                .Append(context.RiverAnisotropyDamping).Append('|')
                .Append(context.RiverBankStabilityClamp).Append('|')
                .Append(context.LakeRiverProximitySuppression);

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
