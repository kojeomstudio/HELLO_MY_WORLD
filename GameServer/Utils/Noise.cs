using System;

namespace GameServerApp.Utils
{
    public static class SimplexNoise
    {
        public static (double dx, double dz) DomainWarp(double x, double z, double simplexFrequency, double perlinFrequency, double simplexAmplitude, double perlinAmplitude, int seed)
        {
            double simplexOffsetX = Generate(x, z, simplexFrequency, 3, 1.0, 0.5, seed) * simplexAmplitude;
            double simplexOffsetZ = Generate(x + 37.0, z + 53.0, simplexFrequency, 3, 1.0, 0.5, seed ^ 0x5F5F5F5F) * simplexAmplitude;

            double perlinOffsetX = PerlinNoise.Generate(x, z, perlinFrequency, 2, 1.0, 0.55, seed ^ 0x00FF00FF) * perlinAmplitude;
            double perlinOffsetZ = PerlinNoise.Generate(x + 17.0, z + 23.0, perlinFrequency, 2, 1.0, 0.55, seed ^ 0x7F00EF00) * perlinAmplitude;

            return (simplexOffsetX + perlinOffsetX, simplexOffsetZ + perlinOffsetZ);
        }

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
            
            return total / maxValue;
        }
        
        private static double GenerateOctave(double x, double y, Random random)
        {
            int xi = (int)Math.Floor(x) & 255;
            int yi = (int)Math.Floor(y) & 255;
            
            double xf = x - Math.Floor(x);
            double yf = y - Math.Floor(y);
            
            double u = Fade(xf);
            double v = Fade(yf);
            
            var p = new int[512];
            for (int i = 0; i < 256; i++)
                p[i] = p[i + 256] = random.Next(256);
            
            int aa = p[p[xi] + yi];
            int ab = p[p[xi] + yi + 1];
            int ba = p[p[xi + 1] + yi];
            int bb = p[p[xi + 1] + yi + 1];
            
            double x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1, yf), u);
            double x2 = Lerp(Grad(ab, xf, yf - 1), Grad(bb, xf - 1, yf - 1), u);
            
            return Lerp(x1, x2, v);
        }
        
        private static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static double Lerp(double a, double b, double t) => a + t * (b - a);
        private static double Grad(int hash, double x, double y) => ((hash & 1) == 0 ? x : -x) + ((hash & 2) == 0 ? y : -y);
    }

    public static class PerlinNoise
    {
        public static double Generate(double x, double y, double frequency, int octaves, double amplitude, double persistence, int seed)
        {
            var random = new Random(seed);
            var permutation = BuildPermutation(random);

            double total = 0.0;
            double maxValue = 0.0;
            double currentFrequency = frequency;
            double currentAmplitude = amplitude;

            for (int i = 0; i < octaves; i++)
            {
                total += Perlin(permutation, x * currentFrequency, y * currentFrequency) * currentAmplitude;
                maxValue += currentAmplitude;
                currentFrequency *= 2.0;
                currentAmplitude *= persistence;
            }

            return maxValue == 0 ? 0 : total / maxValue;
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
