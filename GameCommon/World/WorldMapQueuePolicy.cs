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
    }
}

