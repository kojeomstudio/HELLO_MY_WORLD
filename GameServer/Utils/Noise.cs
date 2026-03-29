using System;

namespace GameServerApp.Utils
{
    public static class PerlinNoise
    {
        public static double Generate(double x, double y, double frequency, int octaves, double amplitude, double persistence, int seed)
        {
            var random = new Random(seed);
            double total = 0;
            double maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += GenerateOctave(x * frequency, y * frequency, random) * amplitude;
                maxValue += amplitude;

                frequency *= 2;
                amplitude *= persistence;
            }

            return maxValue > 0 ? total / maxValue : 0.0;
        }

        private static double GenerateOctave(double x, double y, Random random)
        {
            int[] permutation = BuildPermutation(random);
            return Perlin(permutation, x, y);
        }

        private static double Perlin(int[] permutation, double x, double y)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;

            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);

            double u = Fade(xf);
            double v = Fade(yf);

            int aa = permutation[permutation[xi] + yi];
            int ab = permutation[permutation[xi] + yi + 1];
            int ba = permutation[permutation[xi + 1] + yi];
            int bb = permutation[permutation[xi + 1] + yi + 1];

            double x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            double x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);

            return Lerp(x1, x2, v);
        }

        private static int[] BuildPermutation(Random random)
        {
            var baseArray = new int[256];
            for (int i = 0; i < 256; i++)
            {
                baseArray[i] = i;
            }

            for (int i = 255; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                var temp = baseArray[i];
                baseArray[i] = baseArray[swapIndex];
                baseArray[swapIndex] = temp;
            }

            var permutation = new int[512];
            for (int i = 0; i < 512; i++)
            {
                permutation[i] = baseArray[i & 255];
            }

            return permutation;
        }

        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);

        private static double Lerp(double a, double b, double t) => a + t * (b - a);

        private static double Grad(int hash, double x, double y)
        {
            int h = hash & 7;
            double u = h < 4 ? x : y;
            double v = h < 4 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}
