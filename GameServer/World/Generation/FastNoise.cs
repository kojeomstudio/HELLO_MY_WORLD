using System;

namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Simple noise generator for terrain generation
    /// </summary>
    public class FastNoise
    {
        private int _seed;
        private float _frequency = 0.01f;
        private int _octaves = 1;
        private float _lacunarity = 2.0f;
        private float _gain = 0.5f;
        private Random _random;
        
        public FastNoise()
        {
            _seed = Environment.TickCount;
            _random = new Random(_seed);
        }
        
        public FastNoise(int seed)
        {
            _seed = seed;
            _random = new Random(seed);
        }
        
        /// <summary>
        /// Sets the frequency of the noise
        /// </summary>
        public void SetFrequency(float frequency)
        {
            _frequency = frequency;
        }
        
        /// <summary>
        /// Sets the number of octaves for fractal noise
        /// </summary>
        public void SetFractalOctaves(int octaves)
        {
            _octaves = Math.Max(1, octaves);
        }
        
        /// <summary>
        /// Sets the lacunarity for fractal noise
        /// </summary>
        public void SetFractalLacunarity(float lacunarity)
        {
            _lacunarity = lacunarity;
        }
        
        /// <summary>
        /// Sets the gain for fractal noise
        /// </summary>
        public void SetFractalGain(float gain)
        {
            _gain = gain;
        }
        
        /// <summary>
        /// Gets noise value at the specified coordinates
        /// </summary>
        public float GetNoise(float x, float y)
        {
            return GetFractalNoise(x, y);
        }
        
        /// <summary>
        /// Gets noise value at the specified coordinates
        /// </summary>
        public float GetNoise(int x, int z)
        {
            return GetNoise((float)x, (float)z);
        }
        
        /// <summary>
        /// Generates fractal noise using multiple octaves
        /// </summary>
        private float GetFractalNoise(float x, float y)
        {
            float total = 0.0f;
            float frequency = _frequency;
            float amplitude = 1.0f;
            float maxValue = 0.0f;
            
            for (int i = 0; i < _octaves; i++)
            {
                total += GetSimpleNoise(x * frequency, y * frequency) * amplitude;
                
                maxValue += amplitude;
                amplitude *= _gain;
                frequency *= _lacunarity;
            }
            
            return total / maxValue;
        }
        
        /// <summary>
        /// Generates simple noise using a pseudo-random function
        /// </summary>
        private float GetSimpleNoise(float x, float y)
        {
            // Simple pseudo-random noise function
            int n = (int)x + (int)y * 57;
            n = (n << 13) ^ n;
            int nn = (n * (n * n * 60493 + 19990303) + 1376312589) & 0x7fffffff;
            
            // Normalize to -1 to 1 range
            return (float)(1.0 - ((double)nn / 1073741824.0));
        }
    }
}
namespace GameServerApp.World.Generation
{
    /// <summary>
    /// Simple noise generator for terrain generation
    /// </summary>
    public class FastNoise
    {
        private int _seed;
        private float _frequency = 0.01f;
        private int _octaves = 1;
        private float _lacunarity = 2.0f;
        private float _gain = 0.5f;
        private Random _random;
        
        public FastNoise()
        {
            _seed = Environment.TickCount;
            _random = new Random(_seed);
        }
        
        public FastNoise(int seed)
        {
            _seed = seed;
            _random = new Random(seed);
        }
        
        /// <summary>
        /// Sets the frequency of the noise
        /// </summary>
        public void SetFrequency(float frequency)
        {
            _frequency = frequency;
        }
        
        /// <summary>
        /// Sets the number of octaves for fractal noise
        /// </summary>
        public void SetFractalOctaves(int octaves)
        {
            _octaves = Math.Max(1, octaves);
        }
        
        /// <summary>
        /// Sets the lacunarity for fractal noise
        /// </summary>
        public void SetFractalLacunarity(float lacunarity)
        {
            _lacunarity = lacunarity;
        }
        
        /// <summary>
        /// Sets the gain for fractal noise
        /// </summary>
        public void SetFractalGain(float gain)
        {
            _gain = gain;
        }
        
        /// <summary>
        /// Gets noise value at the specified coordinates
        /// </summary>
        public float GetNoise(float x, float y)
        {
            return GetFractalNoise(x, y);
        }
        
        /// <summary>
        /// Gets noise value at the specified coordinates
        /// </summary>
        public float GetNoise(int x, int z)
        {
            return GetNoise((float)x, (float)z);
        }
        
        /// <summary>
        /// Generates fractal noise using multiple octaves
        /// </summary>
        private float GetFractalNoise(float x, float y)
        {
            float total = 0.0f;
            float frequency = _frequency;
            float amplitude = 1.0f;
            float maxValue = 0.0f;
            
            for (int i = 0; i < _octaves; i++)
            {
                total += GetSimpleNoise(x * frequency, y * frequency) * amplitude;
                
                maxValue += amplitude;
                amplitude *= _gain;
                frequency *= _lacunarity;
            }
            
            return total / maxValue;
        }
        
        /// <summary>
        /// Generates simple noise using a pseudo-random function
        /// </summary>
        private float GetSimpleNoise(float x, float y)
        {
            // Simple pseudo-random noise function
            int n = (int)x + (int)y * 57;
            n = (n << 13) ^ n;
            int nn = (n * (n * n * 60493 + 19990303) + 1376312589) & 0x7fffffff;
            
            // Normalize to -1 to 1 range
            return (float)(1.0 - ((double)nn / 1073741824.0));
        }
    }
}
