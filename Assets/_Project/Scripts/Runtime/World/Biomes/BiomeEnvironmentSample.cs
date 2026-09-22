using UnityEngine;

namespace ApexShift.Runtime.World.Biomes
{
    public readonly struct BiomeEnvironmentSample
    {
        public readonly float Elevation01;
        public readonly float SlopeDegrees;
        public readonly float Moisture01;
        public readonly float Temperature01;
        public float Dryness01 => 1f - Moisture01;

        public BiomeEnvironmentSample(float elevation01, float slopeDegrees, float moisture01, float temperature01)
        {
            Elevation01 = Mathf.Clamp01(elevation01);
            SlopeDegrees = Mathf.Max(0f, slopeDegrees);
            Moisture01 = Mathf.Clamp01(moisture01);
            Temperature01 = Mathf.Clamp01(temperature01);
        }
    }
}
