using UnityEngine;

namespace ApexShift.Runtime.World.Biomes
{
    public sealed class BiomeFieldGenerator
    {
        private readonly int seed;
        private readonly BiomeFieldSettings settings;
        private readonly Vector2 moistureOffset;
        private readonly Vector2 temperatureOffset;

        public BiomeFieldGenerator(int seed, BiomeFieldSettings settings)
        {
            this.seed = seed;
            this.settings = settings ?? new BiomeFieldSettings();
            moistureOffset = Offset(seed, 173);
            temperatureOffset = Offset(seed, 911);
        }

        public float SampleMoisture(float x, float z)
        {
            float warpX = Mathf.PerlinNoise((x + moistureOffset.x) * settings.MoistureWarpFrequency, (z + moistureOffset.y) * settings.MoistureWarpFrequency) * 2f - 1f;
            float warpZ = Mathf.PerlinNoise((x + moistureOffset.y) * settings.MoistureWarpFrequency, (z + moistureOffset.x) * settings.MoistureWarpFrequency) * 2f - 1f;
            float value = Mathf.PerlinNoise(
                (x + moistureOffset.x + warpX * settings.MoistureWarpStrength) * settings.MoistureFrequency,
                (z + moistureOffset.y + warpZ * settings.MoistureWarpStrength) * settings.MoistureFrequency);
            return Mathf.Clamp01(value);
        }

        public float SampleTemperature(float x, float z)
        {
            float value = Mathf.PerlinNoise((x + temperatureOffset.x) * settings.TemperatureFrequency, (z + temperatureOffset.y) * settings.TemperatureFrequency);
            return Mathf.Clamp01(value);
        }

        public float SampleTemperature(float x, float z, float elevation01)
        {
            return Mathf.Clamp01(Mathf.Lerp(0.5f, SampleTemperature(x, z), settings.TemperatureNoiseInfluence)
                                 - Mathf.Clamp01(elevation01) * settings.ElevationCooling);
        }

        private static Vector2 Offset(int seed, int salt)
        {
            unchecked
            {
                int value = seed * 73856093 ^ salt * 19349663;
                float x = 1000f + (value & 0x7fff);
                float z = 2000f + ((value >> 8) & 0x7fff);
                return new Vector2(x, z);
            }
        }
    }
}
