using System;
using System.Collections.Generic;
using System.Linq;

namespace GameCommon.World
{
    /// <summary>
    /// Shared queue pressure utilities so server and client apply the same
    /// load classification / emergency latch semantics.
    /// </summary>
    public enum QueuePressureBand
    {
        Low = 0,
        Elevated = 1,
        High = 2,
        Critical = 3
    }

    /// <summary>
    /// Chunk coordinate value used by shared chunk-priority ordering helpers.
    /// </summary>
    public readonly struct ChunkCoordinate
    {
        public ChunkCoordinate(int x, int z)
        {
            X = x;
            Z = z;
        }

        public int X { get; }

        public int Z { get; }
    }

    public static class WorldMapQueuePolicy
    {
        public static double ClampEmaBlend(double blend, double fallback = 0.18)
        {
            double resolved = blend <= 0.0 ? fallback : blend;
            return Math.Clamp(resolved, 0.05, 0.65);
        }

        public static double ClampEmergencyReleaseRatio(double ratio, double fallback = 0.84)
        {
            double resolved = ratio <= 0.0 ? fallback : ratio;
            return Math.Clamp(resolved, 0.5, 0.99);
        }

        public static double ClampTrendBoostWeight(double weight, double fallback = 0.2)
        {
            double resolved = weight <= 0.0 ? fallback : weight;
            return Math.Clamp(resolved, 0.0, 1.5);
        }

        public static double ClampShockAbsorberWeight(double weight, double fallback = 0.24)
        {
            double resolved = weight <= 0.0 ? fallback : weight;
            return Math.Clamp(resolved, 0.0, 1.5);
        }

        public static double ClampHotspotBias(double bias, double fallback = 0.42)
        {
            double resolved = bias <= 0.0 ? fallback : bias;
            return Math.Clamp(resolved, 0.05, 1.5);
        }

        public static double ClampHotspotEmergencyPenalty(double penalty, double fallback = 1.0)
        {
            double resolved = penalty <= 0.0 ? fallback : penalty;
            return Math.Clamp(resolved, 0.0, 3.0);
        }

        public static int ClampNearChunkKeepCount(
            int nearChunkKeepCount,
            int fallback = 24,
            int min = 8,
            int max = 512)
        {
            min = Math.Max(1, min);
            max = Math.Max(min, max);
            int resolved = nearChunkKeepCount > 0 ? nearChunkKeepCount : fallback;
            return Math.Clamp(resolved, min, max);
        }

        /// <summary>
        /// Computes normalized queue-load volatility from instantaneous/EMA divergence and trend.
        /// Shared by server/client to keep shock handling aligned.
        /// </summary>
        public static double ComputeVolatilityRatio(
            double instantaneousLoad,
            double emaLoad,
            double loadTrend,
            bool emergencyBrake,
            double trendBoostWeight,
            double shockAbsorberWeight)
        {
            double divergence = Math.Abs(instantaneousLoad - emaLoad);
            double clampedTrend = Math.Abs(loadTrend);
            double trendWeight = ClampTrendBoostWeight(trendBoostWeight);
            double shockWeight = ClampShockAbsorberWeight(shockAbsorberWeight);
            double overload = Math.Max(0.0, instantaneousLoad - 1.0);
            double volatility = divergence * 0.55 +
                                clampedTrend * trendWeight * 0.2 +
                                overload * shockWeight * 0.25;
            if (emergencyBrake)
            {
                volatility += 0.12 + shockWeight * 0.08;
            }

            return Math.Clamp(volatility, 0.0, 1.5);
        }

        /// <summary>
        /// Converts volatility into a conservative queue-scaling guard.
        /// Lower values indicate stronger queue throttling pressure.
        /// </summary>
        public static double ComputeVolatilityGuardScale(
            double volatilityRatio,
            bool emergencyBrake,
            double minScale = 0.6,
            double maxScale = 1.0)
        {
            minScale = Math.Clamp(minScale, 0.2, 1.0);
            maxScale = Math.Clamp(maxScale, minScale, 1.5);
            double emergencyPenalty = emergencyBrake ? 0.08 : 0.0;
            double damping = Math.Clamp(volatilityRatio * 0.35 + emergencyPenalty, 0.0, 0.45);
            return Math.Clamp(1.0 - damping, minScale, maxScale);
        }

        public static double ComputeLoadTrend(double instantaneousLoad, double emaLoad)
        {
            return Math.Clamp(instantaneousLoad - emaLoad, -2.0, 2.0);
        }

        public static double ComputeShockAbsorberScale(
            double effectiveLoad,
            double loadTrend,
            bool emergencyBrake,
            double shockAbsorberWeight)
        {
            double weight = ClampShockAbsorberWeight(shockAbsorberWeight);
            if (weight <= 0.0)
            {
                return 1.0;
            }

            double trendPenalty = Math.Max(0.0, loadTrend) * weight * 0.55;
            double overloadPenalty = Math.Max(0.0, effectiveLoad - 0.9) * weight * 0.45;
            double emergencyPenalty = emergencyBrake ? weight * 0.3 : 0.0;
            double scale = 1.0 - trendPenalty - overloadPenalty - emergencyPenalty;
            return Math.Clamp(scale, 0.55, 1.0);
        }

        public static double ComputeAdaptiveEmaBlend(
            double baseBlend,
            double instantaneousLoad,
            double emaLoad,
            bool emergencyBrake)
        {
            double clampedBase = ClampEmaBlend(baseBlend);
            double trend = ComputeLoadTrend(instantaneousLoad, emaLoad);
            double upwardTrendBoost = Math.Max(0.0, trend) * 0.12;
            double emergencyBoost = emergencyBrake ? 0.08 : 0.0;
            return Math.Clamp(clampedBase + upwardTrendBoost + emergencyBoost, 0.05, 0.75);
        }

        public static double UpdateEma(double previousEma, double sample, double blend)
        {
            blend = Math.Clamp(blend, 0.0, 1.0);
            if (previousEma <= 0.0)
            {
                return sample;
            }

            return previousEma * (1.0 - blend) + sample * blend;
        }

        public static bool UpdateEmergencyLatch(
            bool currentlyLatched,
            double effectiveLoad,
            double emergencyThreshold,
            double releaseRatio)
        {
            emergencyThreshold = Math.Max(0.01, emergencyThreshold);
            releaseRatio = Math.Clamp(releaseRatio, 0.05, 0.99);
            double releaseThreshold = Math.Clamp(emergencyThreshold * releaseRatio, 0.05, emergencyThreshold);

            if (effectiveLoad >= emergencyThreshold)
            {
                return true;
            }

            if (currentlyLatched && effectiveLoad <= releaseThreshold)
            {
                return false;
            }

            return currentlyLatched;
        }

        public static double ComputeRecoveryRamp(int remainingTicks, int totalTicks)
        {
            int clampedTotal = Math.Max(1, totalTicks);
            int clampedRemaining = Math.Clamp(remainingTicks, 0, clampedTotal);
            double completedRatio = 1.0 - clampedRemaining / (double)clampedTotal;
            return Math.Clamp(0.15 + completedRatio * 0.85, 0.15, 1.0);
        }

        public static QueuePressureBand ClassifyBand(double effectiveLoad)
        {
            if (effectiveLoad >= 2.0)
            {
                return QueuePressureBand.Critical;
            }

            if (effectiveLoad >= 1.25)
            {
                return QueuePressureBand.High;
            }

            if (effectiveLoad >= 0.75)
            {
                return QueuePressureBand.Elevated;
            }

            return QueuePressureBand.Low;
        }

        public static int GetPressureFactorHint(QueuePressureBand band)
        {
            return band switch
            {
                QueuePressureBand.Critical => 4,
                QueuePressureBand.High => 3,
                QueuePressureBand.Elevated => 2,
                _ => 1
            };
        }

        public static int ComputeAdaptivePressureFactor(
            int basePressureFactor,
            QueuePressureBand band,
            double loadTrend,
            bool emergencyBrake,
            double trendBoostWeight,
            int min = 1,
            int max = 8)
        {
            int clampedBase = Math.Clamp(basePressureFactor, min, max);
            int pressure = Math.Max(clampedBase, GetPressureFactorHint(band));
            double trendBoost = Math.Max(0.0, loadTrend) * ClampTrendBoostWeight(trendBoostWeight) * 4.0;
            pressure += (int)Math.Ceiling(trendBoost);

            if (emergencyBrake)
            {
                pressure = Math.Max(pressure, clampedBase + 1);
            }

            return Math.Clamp(pressure, min, max);
        }

        /// <summary>
        /// Computes the near-chunk keep budget used by queue pruning.
        /// Higher pressure reduces the keep budget while preserving a deterministic floor.
        /// </summary>
        public static int ComputeAdaptiveNearChunkKeepCount(
            int configuredNearChunkKeepCount,
            int fallbackBase,
            QueuePressureBand band,
            double effectiveLoad,
            bool emergencyBrake,
            double hotspotBias,
            double hotspotEmergencyPenalty,
            int min = 8,
            int max = 512)
        {
            int clampedConfigured = ClampNearChunkKeepCount(configuredNearChunkKeepCount, fallbackBase, min, max);
            int clampedFallback = ClampNearChunkKeepCount(fallbackBase, fallbackBase, min, max);
            int baseline = Math.Clamp(Math.Max(clampedConfigured, clampedFallback), min, max);

            double clampedLoad = Math.Clamp(effectiveLoad, 0.0, 4.0);
            double clampedBias = ClampHotspotBias(hotspotBias);
            double clampedEmergencyPenalty = ClampHotspotEmergencyPenalty(hotspotEmergencyPenalty);
            double bandPenalty = band switch
            {
                QueuePressureBand.Critical => 0.28,
                QueuePressureBand.High => 0.18,
                QueuePressureBand.Elevated => 0.08,
                _ => 0.0
            };
            double loadPenalty = Math.Clamp((clampedLoad - 0.95) * 0.22, 0.0, 0.3);
            double retentionBoost = Math.Clamp((1.0 - clampedLoad) * clampedBias * 0.15, 0.0, 0.2);
            double emergencyPenalty = emergencyBrake
                ? Math.Clamp(clampedEmergencyPenalty * 0.08, 0.0, 0.2)
                : 0.0;

            double scale = 1.0 + retentionBoost - bandPenalty - loadPenalty - emergencyPenalty;
            int adaptive = (int)Math.Round(baseline * Math.Clamp(scale, 0.35, 1.2));
            int floor = band switch
            {
                QueuePressureBand.Critical => (int)Math.Ceiling(baseline * 0.45),
                QueuePressureBand.High => (int)Math.Ceiling(baseline * 0.55),
                QueuePressureBand.Elevated => (int)Math.Ceiling(baseline * 0.65),
                _ => (int)Math.Ceiling(baseline * 0.75)
            };

            if (emergencyBrake)
            {
                floor = Math.Max(min, (int)Math.Ceiling(floor * 0.9));
            }

            return Math.Clamp(Math.Max(adaptive, floor), min, max);
        }

        /// <summary>
        /// Computes a shared queue-limit candidate from budget/pressure/slack inputs.
        /// This keeps server/client queue admission behavior aligned.
        /// </summary>
        public static int ComputeQueueLimitFromBudget(
            int cacheBudget,
            int pressureFactor,
            double slackRatio,
            double burstSlackMultiplier,
            double effectiveLoad,
            bool emergencyBrake,
            int min = 64,
            int max = 16384)
        {
            min = Math.Max(1, min);
            max = Math.Max(min, max);
            int budget = Math.Max(min, cacheBudget);
            int pressure = Math.Max(1, pressureFactor);
            double slack = Math.Clamp(slackRatio, 1.1, 6.0);
            double burst = Math.Clamp(burstSlackMultiplier, 1.0, 3.0);
            double appliedBurst = !emergencyBrake && effectiveLoad >= 0.9 ? burst : 1.0;
            int limit = (int)Math.Ceiling(budget * pressure * slack * appliedBurst);
            if (emergencyBrake)
            {
                limit = (int)Math.Ceiling(limit * 0.9);
            }

            return Math.Clamp(limit, min, max);
        }

        /// <summary>
        /// Computes a dynamic stale-pruning budget shared by server/client queue managers.
        /// Budget grows with pressure band, effective load, and emergency latch state.
        /// </summary>
        public static int ComputeStalePruneBudget(
            int queueSize,
            int baseDrain,
            QueuePressureBand band,
            bool emergencyBrake,
            double effectiveLoad,
            int min = 1,
            int max = 128)
        {
            min = Math.Max(1, min);
            max = Math.Max(min, max);
            baseDrain = Math.Clamp(baseDrain, min, max);
            int normalizedQueue = Math.Max(0, queueSize);
            int queueCap = Math.Clamp(Math.Max(baseDrain, normalizedQueue / 3), min, max);
            int bandBoost = band switch
            {
                QueuePressureBand.Critical => 9,
                QueuePressureBand.High => 6,
                QueuePressureBand.Elevated => 3,
                _ => 0
            };
            int loadBoost = (int)Math.Ceiling(Math.Clamp((effectiveLoad - 0.7) * 10.0, 0.0, 16.0));
            int emergencyBoost = emergencyBrake ? Math.Max(2, baseDrain / 2 + 1) : 0;
            int budget = baseDrain + bandBoost + loadBoost + emergencyBoost;
            return Math.Clamp(Math.Min(budget, queueCap), min, max);
        }

        /// <summary>
        /// Computes an emergency-scaled stale prune max budget from a configured base value.
        /// Shared by server/client queue managers to keep emergency expansion behavior aligned.
        /// </summary>
        public static int ComputeEmergencyScaledStalePruneMax(
            int configuredStalePruneMax,
            double emergencyMultiplier,
            bool emergencyMode,
            int min = 8,
            int max = 1024)
        {
            min = Math.Max(1, min);
            max = Math.Max(min, max);
            int baseCap = Math.Clamp(configuredStalePruneMax <= 0 ? min : configuredStalePruneMax, min, max);
            if (!emergencyMode)
            {
                return baseCap;
            }

            double clampedMultiplier = Math.Clamp(emergencyMultiplier <= 0.0 ? 1.0 : emergencyMultiplier, 1.0, 3.0);
            int scaled = (int)Math.Ceiling(baseCap * clampedMultiplier);
            return Math.Clamp(scaled, baseCap, max);
        }

        /// <summary>
        /// Computes a Manhattan distance threshold for queue shedding based on pressure band.
        /// </summary>
        public static int GetDistanceThreshold(int baseRadius, QueuePressureBand band, bool emergencyBrake = false)
        {
            baseRadius = Math.Max(1, baseRadius);
            int offset = band switch
            {
                QueuePressureBand.Critical => -1,
                QueuePressureBand.High => 0,
                QueuePressureBand.Elevated => 1,
                _ => 2
            };

            int emergencyPenalty = emergencyBrake ? 1 : 0;
            return Math.Max(1, baseRadius + offset - emergencyPenalty);
        }

        /// <summary>
        /// Computes a pressure/load aware distance threshold with a hotspot-aware near-chunk bias.
        /// This keeps nearby chunks preferentially admitted while still tightening under overload.
        /// </summary>
        public static int ComputeAdaptiveDistanceThreshold(
            int baseRadius,
            QueuePressureBand band,
            bool emergencyBrake,
            double effectiveLoad,
            double hotspotBias,
            double hotspotEmergencyPenalty)
        {
            int threshold = GetDistanceThreshold(baseRadius, band, emergencyBrake);
            double clampedLoad = Math.Clamp(effectiveLoad, 0.0, 4.0);
            double clampedBias = ClampHotspotBias(hotspotBias);
            double clampedEmergencyPenalty = ClampHotspotEmergencyPenalty(hotspotEmergencyPenalty);

            // Under low load preserve slightly wider near-chunk admission; under high load tighten quickly.
            int nearBoost = (int)Math.Round(Math.Clamp((1.0 - clampedLoad) * clampedBias * 2.0, 0.0, 2.0));
            int loadPenalty = (int)Math.Round(Math.Clamp((clampedLoad - 0.9) * clampedBias * 3.0, 0.0, 3.0));
            int emergencyPenalty = emergencyBrake ? (int)Math.Round(clampedEmergencyPenalty) : 0;

            return Math.Max(1, threshold + nearBoost - loadPenalty - emergencyPenalty);
        }

        /// <summary>
        /// Returns true when the chunk lies outside the pressure-aware distance threshold.
        /// </summary>
        public static bool IsOutsideDistanceThreshold(
            int centerX,
            int centerZ,
            int chunkX,
            int chunkZ,
            int baseRadius,
            QueuePressureBand band,
            bool emergencyBrake = false)
        {
            int threshold = GetDistanceThreshold(baseRadius, band, emergencyBrake);
            int manhattan = Math.Abs(chunkX - centerX) + Math.Abs(chunkZ - centerZ);
            return manhattan > threshold;
        }

        /// <summary>
        /// Returns true when the chunk lies outside the adaptive hotspot-aware distance threshold.
        /// </summary>
        public static bool IsOutsideAdaptiveDistanceThreshold(
            int centerX,
            int centerZ,
            int chunkX,
            int chunkZ,
            int baseRadius,
            QueuePressureBand band,
            bool emergencyBrake,
            double effectiveLoad,
            double hotspotBias,
            double hotspotEmergencyPenalty)
        {
            int threshold = ComputeAdaptiveDistanceThreshold(
                baseRadius,
                band,
                emergencyBrake,
                effectiveLoad,
                hotspotBias,
                hotspotEmergencyPenalty);
            int manhattan = Math.Abs(chunkX - centerX) + Math.Abs(chunkZ - centerZ);
            return manhattan > threshold;
        }

        /// <summary>
        /// Shared distance score used to prioritize nearby chunks consistently between server/client.
        /// Lower score means higher priority.
        /// </summary>
        public static double ComputeDistancePriority(
            int centerX,
            int centerZ,
            ChunkCoordinate coordinate,
            QueuePressureBand band = QueuePressureBand.Low,
            bool emergencyBrake = false)
        {
            int dx = coordinate.X - centerX;
            int dz = coordinate.Z - centerZ;
            int manhattan = Math.Abs(dx) + Math.Abs(dz);
            int dist2 = dx * dx + dz * dz;
            int axisSkew = Math.Abs(Math.Abs(dx) - Math.Abs(dz));

            double pressureWeight = band switch
            {
                QueuePressureBand.Critical => 0.16,
                QueuePressureBand.High => 0.12,
                QueuePressureBand.Elevated => 0.08,
                _ => 0.04
            };

            double emergencyWeight = emergencyBrake ? 0.11 : 0.0;
            return manhattan
                + dist2 * 0.01
                + axisSkew * 0.025
                + manhattan * pressureWeight
                + manhattan * emergencyWeight;
        }

        /// <summary>
        /// Enumerates chunks from nearest to farthest (Manhattan first, then squared distance)
        /// so map controllers can prioritize nearby chunk generation deterministically.
        /// </summary>
        public static IReadOnlyList<ChunkCoordinate> EnumerateByDistance(int centerX, int centerZ, int radius)
        {
            radius = Math.Max(0, radius);
            int side = radius * 2 + 1;
            int capacity = Math.Max(1, side * side);
            var weighted = new List<(ChunkCoordinate Coordinate, int Manhattan, int Dist2)>(capacity);

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int x = centerX + dx;
                    int z = centerZ + dz;
                    int manhattan = Math.Abs(dx) + Math.Abs(dz);
                    int dist2 = dx * dx + dz * dz;
                    weighted.Add((new ChunkCoordinate(x, z), manhattan, dist2));
                }
            }

            return weighted
                .OrderBy(entry => entry.Manhattan)
                .ThenBy(entry => entry.Dist2)
                .ThenBy(entry => entry.Coordinate.X)
                .ThenBy(entry => entry.Coordinate.Z)
                .Select(entry => entry.Coordinate)
                .ToArray();
        }

        /// <summary>
        /// Reorders a coordinate set by distance to the supplied center.
        /// Duplicate coordinates are removed deterministically.
        /// </summary>
        public static IReadOnlyList<ChunkCoordinate> PrioritizeByDistance(
            int centerX,
            int centerZ,
            IEnumerable<ChunkCoordinate> coordinates,
            int maxCount = 0,
            QueuePressureBand band = QueuePressureBand.Low,
            bool emergencyBrake = false)
        {
            if (coordinates == null)
            {
                return Array.Empty<ChunkCoordinate>();
            }

            var deduplicated = coordinates
                .Distinct()
                .Select(coordinate =>
                {
                    int dx = coordinate.X - centerX;
                    int dz = coordinate.Z - centerZ;
                    int manhattan = Math.Abs(dx) + Math.Abs(dz);
                    int dist2 = dx * dx + dz * dz;
                    double priority = ComputeDistancePriority(centerX, centerZ, coordinate, band, emergencyBrake);
                    return (Coordinate: coordinate, Manhattan: manhattan, Dist2: dist2, Priority: priority);
                })
                .OrderBy(entry => entry.Priority)
                .ThenBy(entry => entry.Manhattan)
                .ThenBy(entry => entry.Dist2)
                .ThenBy(entry => entry.Coordinate.X)
                .ThenBy(entry => entry.Coordinate.Z);

            if (maxCount > 0)
            {
                return deduplicated
                    .Take(maxCount)
                    .Select(entry => entry.Coordinate)
                    .ToArray();
            }

            return deduplicated
                .Select(entry => entry.Coordinate)
                .ToArray();
        }
    }
}
