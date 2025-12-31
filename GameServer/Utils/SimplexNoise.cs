using System;

namespace GameServerApp.Utils
{
    /// <summary>
    /// Simplex noise generation utility for terrain generation
    /// </summary>
    public static class SimplexNoise
    {
        private static readonly int[] Grad3 = { 1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1, 0, 1, 0, 1, -1, 0, 1, 1, 0, -1, -1, 0, -1 };
        private static readonly int[] Perm = { 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180 };
        private static readonly int[] PermMod12 = { 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180 };

        /// <summary>
        /// Generate 2D simplex noise
        /// </summary>
        public static double Generate(double x, double y, double frequency, int octaves, double persistence, double lacunarity, int seed)
        {
            double total = 0;
            double amplitude = 1;
            double maxValue = 0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency, seed) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }

        /// <summary>
        /// Generate 3D simplex noise
        /// </summary>
        public static double Generate(double x, double y, double z, double frequency, int octaves, double persistence, double lacunarity, int seed)
        {
            double total = 0;
            double amplitude = 1;
            double maxValue = 0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency, z * frequency, seed) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }

        /// <summary>
        /// Apply domain warping to coordinates
        /// </summary>
        public static (double dx, double dz) DomainWarp(double x, double z, double frequency, double amplitude, double warpFrequency, double warpAmplitude, int seed)
        {
            double offsetX = Noise(x * warpFrequency, z * warpFrequency, seed) * warpAmplitude;
            double offsetZ = Noise(x * warpFrequency + 100, z * warpFrequency + 100, seed) * warpAmplitude;
            
            return (offsetX, offsetZ);
        }

        /// <summary>
        /// 2D simplex noise function
        /// </summary>
        private static double Noise(double xin, double yin, int seed)
        {
            double n0, n1, n2;
            double F2 = 0.5 * (Math.Sqrt(3.0) - 1.0);
            double s = (xin + yin) * F2;
            int i = (int)Math.Floor(xin + s);
            int j = (int)Math.Floor(yin + s);
            double G2 = (3.0 - Math.Sqrt(3.0)) / 6.0;
            double t = (i + j) * G2;
            double X0 = i - t;
            double Y0 = j - t;
            double x0 = xin - X0;
            double y0 = yin - Y0;
            
            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; }
            else { i1 = 0; j1 = 1; }
            
            double x1 = x0 - i1 + G2;
            double y1 = y0 - j1 + G2;
            double x2 = x0 - 1.0 + 2.0 * G2;
            double y2 = y0 - 1.0 + 2.0 * G2;
            
            int ii = i & 255;
            int jj = j & 255;
            int gi0 = PermMod12[ii + Perm[jj]] + seed;
            int gi1 = PermMod12[ii + i1 + Perm[jj + j1]] + seed;
            int gi2 = PermMod12[ii + 1 + Perm[jj + 1]] + seed;
            
            double t0 = 0.5 - x0 * x0 - y0 * y0;
            if (t0 < 0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(Grad3, gi0, x0, y0);
            }
            
            double t1 = 0.5 - x1 * x1 - y1 * y1;
            if (t1 < 0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(Grad3, gi1, x1, y1);
            }
            
            double t2 = 0.5 - x2 * x2 - y2 * y2;
            if (t2 < 0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(Grad3, gi2, x2, y2);
            }
            
            return 70.0 * (n0 + n1 + n2);
        }

        /// <summary>
        /// 3D simplex noise function
        /// </summary>
        private static double Noise(double xin, double yin, double zin, int seed)
        {
            double n0, n1, n2, n3;
            double F3 = 1.0 / 3.0;
            double s = (xin + yin + zin) * F3;
            int i = (int)Math.Floor(xin + s);
            int j = (int)Math.Floor(yin + s);
            int k = (int)Math.Floor(zin + s);
            double G3 = 1.0 / 6.0;
            double t = (i + j + k) * G3;
            double X0 = i - t;
            double Y0 = j - t;
            double Z0 = k - t;
            double x0 = xin - X0;
            double y0 = yin - Y0;
            double z0 = zin - Z0;
            
            int i1, j1, k1;
            int i2, j2, k2;
            
            if (x0 >= y0)
            {
                if (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; }
                else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; }
            }
            else
            {
                if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; }
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; }
                else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
            }
            
            double x1 = x0 - i1 + G3;
            double y1 = y0 - j1 + G3;
            double z1 = z0 - k1 + G3;
            double x2 = x0 - i2 + 2.0 * G3;
            double y2 = y0 - j2 + 2.0 * G3;
            double z2 = z0 - k2 + 2.0 * G3;
            double x3 = x0 - 1.0 + 3.0 * G3;
            double y3 = y0 - 1.0 + 3.0 * G3;
            double z3 = z0 - 1.0 + 3.0 * G3;
            
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;
            
            int gi0 = PermMod12[ii + Perm[jj + Perm[kk]]] + seed;
            int gi1 = PermMod12[ii + i1 + Perm[jj + j1 + Perm[kk + k1]]] + seed;
            int gi2 = PermMod12[ii + i2 + Perm[jj + j2 + Perm[kk + k2]]] + seed;
            int gi3 = PermMod12[ii + 1 + Perm[jj + 1 + Perm[kk + 1]]] + seed;
            
            double t0 = 0.6 - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 < 0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot3(gi0, x0, y0, z0);
            }
            
            double t1 = 0.6 - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 < 0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot3(gi1, x1, y1, z1);
            }
            
            double t2 = 0.6 - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 < 0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot3(gi2, x2, y2, z2);
            }
            
            double t3 = 0.6 - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 < 0) n3 = 0.0;
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * Dot3(gi3, x3, y3, z3);
            }
            
            return 32.0 * (n0 + n1 + n2 + n3);
        }

        /// <summary>
        /// Dot product for 2D gradient
        /// </summary>
        private static double Dot(int[] g, int gi, double x, double y)
        {
            return g[gi] * x + g[gi + 1] * y;
        }

        /// <summary>
        /// Dot product for 3D gradient
        /// </summary>
        private static double Dot3(int gi, double x, double y, double z)
        {
            return Grad3[gi] * x + Grad3[gi + 1] * y + Grad3[gi + 2] * z;
        }
    }
}

namespace GameServerApp.Utils
{
    /// <summary>
    /// Simplex noise generation utility for terrain generation
    /// </summary>
    public static class SimplexNoise
    {
        private static readonly int[] Grad3 = { 1, 1, 0, -1, 1, 0, 1, -1, 0, -1, -1, 0, 1, 0, 1, -1, 0, 1, 1, 0, -1, -1, 0, -1 };
        private static readonly int[] Perm = { 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180 };
        private static readonly int[] PermMod12 = { 151, 160, 137, 91, 90, 15, 131, 13, 201, 95, 96, 53, 194, 233, 7, 225, 140, 36, 103, 30, 69, 142, 8, 99, 37, 240, 21, 10, 23, 190, 6, 148, 247, 120, 234, 75, 0, 26, 197, 62, 94, 252, 219, 203, 117, 35, 11, 32, 57, 177, 33, 88, 237, 149, 56, 87, 174, 20, 125, 136, 171, 168, 68, 175, 74, 165, 71, 134, 139, 48, 27, 166, 77, 146, 158, 231, 83, 111, 229, 122, 60, 211, 133, 230, 220, 105, 92, 41, 55, 46, 245, 40, 244, 102, 143, 54, 65, 25, 63, 161, 1, 216, 80, 73, 209, 76, 132, 187, 208, 89, 18, 169, 200, 196, 135, 130, 116, 188, 159, 86, 164, 100, 109, 198, 173, 186, 3, 64, 52, 217, 226, 250, 124, 123, 5, 202, 38, 147, 118, 126, 255, 82, 85, 212, 207, 206, 59, 227, 47, 16, 58, 17, 182, 189, 28, 42, 223, 183, 170, 213, 119, 248, 152, 2, 44, 154, 163, 70, 221, 153, 101, 155, 167, 43, 172, 9, 129, 22, 39, 253, 19, 98, 108, 110, 79, 113, 224, 232, 178, 185, 112, 104, 218, 246, 97, 228, 251, 34, 242, 193, 238, 210, 144, 12, 191, 179, 162, 241, 81, 51, 145, 235, 249, 14, 239, 107, 49, 192, 214, 31, 181, 199, 106, 157, 184, 84, 204, 176, 115, 121, 50, 45, 127, 4, 150, 254, 138, 236, 205, 93, 222, 114, 67, 29, 24, 72, 243, 141, 128, 195, 78, 66, 215, 61, 156, 180 };

        /// <summary>
        /// Generate 2D simplex noise
        /// </summary>
        public static double Generate(double x, double y, double frequency, int octaves, double persistence, double lacunarity, int seed)
        {
            double total = 0;
            double amplitude = 1;
            double maxValue = 0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency, seed) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }

        /// <summary>
        /// Generate 3D simplex noise
        /// </summary>
        public static double Generate(double x, double y, double z, double frequency, int octaves, double persistence, double lacunarity, int seed)
        {
            double total = 0;
            double amplitude = 1;
            double maxValue = 0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += Noise(x * frequency, y * frequency, z * frequency, seed) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }

        /// <summary>
        /// Apply domain warping to coordinates
        /// </summary>
        public static (double dx, double dz) DomainWarp(double x, double z, double frequency, double amplitude, double warpFrequency, double warpAmplitude, int seed)
        {
            double offsetX = Noise(x * warpFrequency, z * warpFrequency, seed) * warpAmplitude;
            double offsetZ = Noise(x * warpFrequency + 100, z * warpFrequency + 100, seed) * warpAmplitude;
            
            return (offsetX, offsetZ);
        }

        /// <summary>
        /// 2D simplex noise function
        /// </summary>
        private static double Noise(double xin, double yin, int seed)
        {
            double n0, n1, n2;
            double F2 = 0.5 * (Math.Sqrt(3.0) - 1.0);
            double s = (xin + yin) * F2;
            int i = (int)Math.Floor(xin + s);
            int j = (int)Math.Floor(yin + s);
            double G2 = (3.0 - Math.Sqrt(3.0)) / 6.0;
            double t = (i + j) * G2;
            double X0 = i - t;
            double Y0 = j - t;
            double x0 = xin - X0;
            double y0 = yin - Y0;
            
            int i1, j1;
            if (x0 > y0) { i1 = 1; j1 = 0; }
            else { i1 = 0; j1 = 1; }
            
            double x1 = x0 - i1 + G2;
            double y1 = y0 - j1 + G2;
            double x2 = x0 - 1.0 + 2.0 * G2;
            double y2 = y0 - 1.0 + 2.0 * G2;
            
            int ii = i & 255;
            int jj = j & 255;
            int gi0 = PermMod12[ii + Perm[jj]] + seed;
            int gi1 = PermMod12[ii + i1 + Perm[jj + j1]] + seed;
            int gi2 = PermMod12[ii + 1 + Perm[jj + 1]] + seed;
            
            double t0 = 0.5 - x0 * x0 - y0 * y0;
            if (t0 < 0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(Grad3, gi0, x0, y0);
            }
            
            double t1 = 0.5 - x1 * x1 - y1 * y1;
            if (t1 < 0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot(Grad3, gi1, x1, y1);
            }
            
            double t2 = 0.5 - x2 * x2 - y2 * y2;
            if (t2 < 0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(Grad3, gi2, x2, y2);
            }
            
            return 70.0 * (n0 + n1 + n2);
        }

        /// <summary>
        /// 3D simplex noise function
        /// </summary>
        private static double Noise(double xin, double yin, double zin, int seed)
        {
            double n0, n1, n2, n3;
            double F3 = 1.0 / 3.0;
            double s = (xin + yin + zin) * F3;
            int i = (int)Math.Floor(xin + s);
            int j = (int)Math.Floor(yin + s);
            int k = (int)Math.Floor(zin + s);
            double G3 = 1.0 / 6.0;
            double t = (i + j + k) * G3;
            double X0 = i - t;
            double Y0 = j - t;
            double Z0 = k - t;
            double x0 = xin - X0;
            double y0 = yin - Y0;
            double z0 = zin - Z0;
            
            int i1, j1, k1;
            int i2, j2, k2;
            
            if (x0 >= y0)
            {
                if (y0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
                else if (x0 >= z0) { i1 = 1; j1 = 0; k1 = 0; i2 = 1; j2 = 0; k2 = 1; }
                else { i1 = 0; j1 = 0; k1 = 1; i2 = 1; j2 = 0; k2 = 1; }
            }
            else
            {
                if (y0 < z0) { i1 = 0; j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; }
                else if (x0 < z0) { i1 = 0; j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; }
                else { i1 = 0; j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; }
            }
            
            double x1 = x0 - i1 + G3;
            double y1 = y0 - j1 + G3;
            double z1 = z0 - k1 + G3;
            double x2 = x0 - i2 + 2.0 * G3;
            double y2 = y0 - j2 + 2.0 * G3;
            double z2 = z0 - k2 + 2.0 * G3;
            double x3 = x0 - 1.0 + 3.0 * G3;
            double y3 = y0 - 1.0 + 3.0 * G3;
            double z3 = z0 - 1.0 + 3.0 * G3;
            
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;
            
            int gi0 = PermMod12[ii + Perm[jj + Perm[kk]]] + seed;
            int gi1 = PermMod12[ii + i1 + Perm[jj + j1 + Perm[kk + k1]]] + seed;
            int gi2 = PermMod12[ii + i2 + Perm[jj + j2 + Perm[kk + k2]]] + seed;
            int gi3 = PermMod12[ii + 1 + Perm[jj + 1 + Perm[kk + 1]]] + seed;
            
            double t0 = 0.6 - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 < 0) n0 = 0.0;
            else
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot3(gi0, x0, y0, z0);
            }
            
            double t1 = 0.6 - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 < 0) n1 = 0.0;
            else
            {
                t1 *= t1;
                n1 = t1 * t1 * Dot3(gi1, x1, y1, z1);
            }
            
            double t2 = 0.6 - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 < 0) n2 = 0.0;
            else
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot3(gi2, x2, y2, z2);
            }
            
            double t3 = 0.6 - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 < 0) n3 = 0.0;
            else
            {
                t3 *= t3;
                n3 = t3 * t3 * Dot3(gi3, x3, y3, z3);
            }
            
            return 32.0 * (n0 + n1 + n2 + n3);
        }

        /// <summary>
        /// Dot product for 2D gradient
        /// </summary>
        private static double Dot(int[] g, int gi, double x, double y)
        {
            return g[gi] * x + g[gi + 1] * y;
        }

        /// <summary>
        /// Dot product for 3D gradient
        /// </summary>
        private static double Dot3(int gi, double x, double y, double z)
        {
            return Grad3[gi] * x + Grad3[gi + 1] * y + Grad3[gi + 2] * z;
        }
    }
}
}
            int ix = (int)Math.Floor(x) & 0xFFFF;
            int iz = (int)Math.Floor(z) & 0xFFFF;
            
            ix = (ix * 374761393 + seed) ^ (seed * 668265263);
            iz = (iz * 374761393 + seed) ^ (seed * 668265263);
            
            return (ix * 374761393 + iz * 668265263) & 0x7fffffff;
        }

        /// <summary>
        /// Simple hash function for 3D noise generation
        /// </summary>
        private static int Hash(double x, double y, double z, int seed)
        {
            int ix = (int)Math.Floor(x) & 0xFFFF;
            int iy = (int)Math.Floor(y) & 0xFFFF;
            int iz = (int)Math.Floor(z) & 0xFFFF;
            
            ix = (ix * 374761393 + seed) ^ (seed * 668265263);
            iy = (iy * 374761393 + seed) ^ (seed * 668265263);
            iz = (iz * 374761393 + seed) ^ (seed * 668265263);
            
            return ((ix * 374761393 + iy * 668265263) ^ iz * 123456789) & 0x7fffffff;
        }
    }
}
}
namespace GameServerApp.Utils
{
    /// <summary>
    /// Simplex noise generation utility
    /// </summary>
    public static class SimplexNoise
    {
        /// <summary>
        /// Generate 2D simplex noise
        /// </summary>
        public static double Generate(double x, double z, double frequency, int octaves, double persistence, double lacunarity, int seed)
        {
            double total = 0.0;
            double amplitude = persistence;
            double maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += GenerateSingle(x * frequency, z * frequency, seed + i) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }

        /// <summary>
        /// Generate 3D simplex noise
        /// </summary>
        public static double Generate(double x, double y, double z, double frequency, int octaves, double persistence, double lacunarity, int seed)
        {
            double total = 0.0;
            double amplitude = persistence;
            double maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += GenerateSingle(x * frequency, y * frequency, z * frequency, seed + i) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }

        /// <summary>
        /// Generate domain warped noise
        /// </summary>
        public static (double dx, double dz) DomainWarp(double x, double z, double simplexFrequency, double perlinFrequency, double simplexAmplitude, double perlinAmplitude, int seed)
        {
            double warpX = GenerateSingle(x * simplexFrequency, z * simplexFrequency, seed) * simplexAmplitude;
            double warpZ = GenerateSingle(x * simplexFrequency + 1000, z * simplexFrequency + 1000, seed) * simplexAmplitude;
            
            double perlinX = GeneratePerlin(x * perlinFrequency, z * perlinFrequency, seed + 1) * perlinAmplitude;
            double perlinZ = GeneratePerlin(x * perlinFrequency + 1000, z * perlinFrequency + 1000, seed + 1) * perlinAmplitude;
            
            return (warpX + perlinX, warpZ + perlinZ);
        }

        /// <summary>
        /// Generate single 2D simplex noise value
        /// </summary>
        private static double GenerateSingle(double x, double z, int seed)
        {
            // Simple implementation of simplex noise
            // In a real implementation, this would use the actual simplex noise algorithm
            Random random = new Random(seed);
            double randomValue = random.NextDouble();
            
            // Use a simple hash function to generate consistent noise
            int hash = Hash(x, z, seed);
            return (hash % 1000) / 500.0 - 1.0; // Range: [-1, 1]
        }

        /// <summary>
        /// Generate single 3D simplex noise value
        /// </summary>
        private static double GenerateSingle(double x, double y, double z, int seed)
        {
            // Simple implementation of 3D simplex noise
            int hash = Hash(x, y, z, seed);
            return (hash % 1000) / 500.0 - 1.0; // Range: [-1, 1]
        }

        /// <summary>
        /// Generate Perlin noise
        /// </summary>
        private static double GeneratePerlin(double x, double z, int seed)
        {
            // Simple implementation of Perlin noise
            int hash = Hash(x, z, seed);
            return (hash % 1000) / 500.0 - 1.0; // Range: [-1, 1]
        }

        /// <summary>
        /// Simple hash function for noise generation
        /// </summary>
        private static int Hash(double x, double z, int seed)
        {
            int ix = (int)Math.Floor(x) & 0xFFFF;
            int iz = (int)Math.Floor(z) & 0xFFFF;
            
            ix = (ix * 374761393 + seed) ^ (seed * 668265263);
            iz = (iz * 374761393 + seed) ^ (seed * 668265263);
            
            return (ix * 374761393 + iz * 668265263) & 0x7fffffff;
        }

        /// <summary>
        /// Simple hash function for 3D noise generation
        /// </summary>
        private static int Hash(double x, double y, double z, int seed)
        {
            int ix = (int)Math.Floor(x) & 0xFFFF;
            int iy = (int)Math.Floor(y) & 0xFFFF;
            int iz = (int)Math.Floor(z) & 0xFFFF;
            
            ix = (ix * 374761393 + seed) ^ (seed * 668265263);
            iy = (iy * 374761393 + seed) ^ (seed * 668265263);
            iz = (iz * 374761393 + seed) ^ (seed * 668265263);
            
            return ((ix * 374761393 + iy * 668265263) ^ iz * 123456789) & 0x7fffffff;
        }
    }
}
}
{
    /// <summary>
    /// Simplex noise generation utility
    /// </summary>
    public static class SimplexNoise
    {
        /// <summary>
        /// Generate 2D simplex noise
        /// </summary>
        public static double Generate(double x, double z, double frequency, int octaves, double persistence, double lacunarity, int seed)
        {
            double total = 0.0;
            double amplitude = persistence;
            double maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += GenerateSingle(x * frequency, z * frequency, seed + i) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }

        /// <summary>
        /// Generate 3D simplex noise
        /// </summary>
        public static double Generate(double x, double y, double z, double frequency, int octaves, double persistence, double lacunarity, int seed)
        {
            double total = 0.0;
            double amplitude = persistence;
            double maxValue = 0.0;
            
            for (int i = 0; i < octaves; i++)
            {
                total += GenerateSingle(x * frequency, y * frequency, z * frequency, seed + i) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return total / maxValue;
        }

        /// <summary>
        /// Generate domain warped noise
        /// </summary>
        public static (double dx, double dz) DomainWarp(double x, double z, double simplexFrequency, double perlinFrequency, double simplexAmplitude, double perlinAmplitude, int seed)
        {
            double warpX = GenerateSingle(x * simplexFrequency, z * simplexFrequency, seed) * simplexAmplitude;
            double warpZ = GenerateSingle(x * simplexFrequency + 1000, z * simplexFrequency + 1000, seed) * simplexAmplitude;
            
            double perlinX = GeneratePerlin(x * perlinFrequency, z * perlinFrequency, seed + 1) * perlinAmplitude;
            double perlinZ = GeneratePerlin(x * perlinFrequency + 1000, z * perlinFrequency + 1000, seed + 1) * perlinAmplitude;
            
            return (warpX + perlinX, warpZ + perlinZ);
        }

        /// <summary>
        /// Generate single 2D simplex noise value
        /// </summary>
        private static double GenerateSingle(double x, double z, int seed)
        {
            // Simple implementation of simplex noise
            // In a real implementation, this would use the actual simplex noise algorithm
            Random random = new Random(seed);
            double randomValue = random.NextDouble();
            
            // Use a simple hash function to generate consistent noise
            int hash = Hash(x, z, seed);
            return (hash % 1000) / 500.0 - 1.0; // Range: [-1, 1]
        }

        /// <summary>
        /// Generate single 3D simplex noise value
        /// </summary>
        private static double GenerateSingle(double x, double y, double z, int seed)
        {
            // Simple implementation of 3D simplex noise
            int hash = Hash(x, y, z, seed);
            return (hash % 1000) / 500.0 - 1.0; // Range: [-1, 1]
        }

        /// <summary>
        /// Generate Perlin noise
        /// </summary>
        private static double GeneratePerlin(double x, double z, int seed)
        {
            // Simple implementation of Perlin noise
            int hash = Hash(x, z, seed);
            return (hash % 1000) / 500.0 - 1.0; // Range: [-1, 1]
        }

        /// <summary>
        /// Simple hash function for noise generation
        /// </summary>
        private static int Hash(double x, double z, int seed)
        {
            int ix = (int)Math.Floor(x) & 0xFFFF;
            int iz = (int)Math.Floor(z) & 0xFFFF;
            
            ix = (ix * 374761393 + seed) ^ (seed * 668265263);
            iz = (iz * 374761393 + seed) ^ (seed * 668265263);
            
            return (ix * 374761393 + iz * 668265263) & 0x7fffffff;
        }

        /// <summary>
        /// Simple hash function for 3D noise generation
        /// </summary>
        private static int Hash(double x, double y, double z, int seed)
        {
            int ix = (int)Math.Floor(x) & 0xFFFF;
            int iy = (int)Math.Floor(y) & 0xFFFF;
            int iz = (int)Math.Floor(z) & 0xFFFF;
            
            ix = (ix * 374761393 + seed) ^ (seed * 668265263);
            iy = (iy * 374761393 + seed) ^ (seed * 668265263);
            iz = (iz * 374761393 + seed) ^ (seed * 668265263);
            
            return ((ix * 374761393 + iy * 668265263) ^ iz * 123456789) & 0x7fffffff;
        }
    }
}
