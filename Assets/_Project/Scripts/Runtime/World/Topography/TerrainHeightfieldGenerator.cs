using UnityEngine;

namespace ApexShift.Runtime.World.Topography
{
    /// <summary>
    /// Stateless, deterministic terrain height sampler. It never touches Unity's global RNG.
    /// </summary>
    public sealed class TerrainHeightfieldGenerator
    {
        private readonly int seed;
        private readonly TerrainHeightfieldSettings settings;
        private readonly float offsetX;
        private readonly float offsetZ;
        private readonly float warpOffsetX;
        private readonly float warpOffsetZ;

        public TerrainHeightfieldGenerator(int seed, TerrainHeightfieldSettings settings)
        {
            this.seed = seed;
            this.settings = settings ?? new TerrainHeightfieldSettings();

            // Stable integer-derived offsets avoid consuming the global Random stream.
            offsetX = StableOffset(seed, 0x45d9f3b);
            offsetZ = StableOffset(seed, unchecked((int)0x9e3779b9));
            warpOffsetX = StableOffset(seed, 0x7f4a7c15);
            warpOffsetZ = StableOffset(seed, unchecked((int)0x94d049bb));
        }

        public float SampleHeight(float worldX, float worldZ)
        {
            float warpedX = worldX + (Mathf.PerlinNoise(worldX * settings.WarpFrequency + warpOffsetX, worldZ * settings.WarpFrequency + warpOffsetZ) - 0.5f) * settings.WarpStrength;
            float warpedZ = worldZ + (Mathf.PerlinNoise(worldX * settings.WarpFrequency + warpOffsetZ, worldZ * settings.WarpFrequency + warpOffsetX) - 0.5f) * settings.WarpStrength;

            float macro = CenteredNoise(warpedX * settings.MacroFrequency + offsetX, warpedZ * settings.MacroFrequency + offsetZ) * settings.MacroAmplitude;
            float hills = CenteredNoise(warpedX * settings.HillsFrequency + offsetZ, warpedZ * settings.HillsFrequency + offsetX) * settings.HillsAmplitude;

            float ridgeNoise = Mathf.PerlinNoise(warpedX * settings.RidgeFrequency + offsetX, warpedZ * settings.RidgeFrequency + offsetZ);
            float ridge = (1f - Mathf.Abs(2f * ridgeNoise - 1f)) * settings.RidgeAmplitude;
            float ridgeMask = Mathf.Clamp01((macro + 0.18f) / 0.55f);

            float valleyNoise = CenteredNoise(warpedX * settings.ValleyFrequency + warpOffsetX, warpedZ * settings.ValleyFrequency + warpOffsetZ);
            float valleys = valleyNoise * settings.ValleyStrength;
            float detail = CenteredNoise(worldX * settings.DetailFrequency + warpOffsetZ, worldZ * settings.DetailFrequency + warpOffsetX) * settings.DetailAmplitude;

            float height = settings.SpawnTargetElevation + macro + hills + ridge * ridgeMask - valleys + detail;
            float distance = Mathf.Sqrt(worldX * worldX + worldZ * worldZ);
            float flattenRange = Mathf.Max(0.001f, settings.SpawnFlattenOuterRadius - settings.SpawnFlattenInnerRadius);
            float flatten = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((distance - settings.SpawnFlattenInnerRadius) / flattenRange));
            height = Mathf.Lerp(height, settings.SpawnTargetElevation, flatten);

            return Mathf.Clamp(height, settings.MinimumHeight, settings.MaximumHeight);
        }

        private static float CenteredNoise(float x, float z)
        {
            return Mathf.PerlinNoise(x, z) - 0.5f;
        }

        private static float StableOffset(int value, int salt)
        {
            unchecked
            {
                uint hash = (uint)(value * 397) ^ (uint)salt;
                hash ^= hash >> 16;
                hash *= 0x7feb352du;
                hash ^= hash >> 15;
                return (hash & 0x00ffffffu) / 1000f;
            }
        }
    }
}
