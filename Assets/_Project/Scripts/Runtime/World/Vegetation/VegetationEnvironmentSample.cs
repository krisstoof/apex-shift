namespace ApexShift.Runtime.World.Vegetation
{
    public readonly struct VegetationEnvironmentSample
    {
        public readonly bool IsLand;
        public readonly bool IsWater;
        public readonly bool IsShoreline;
        public readonly string BiomeId;
        public readonly float NormalizedElevation;
        public readonly float SlopeDegrees;
        public readonly float Moisture01;

        public VegetationEnvironmentSample(bool isLand, bool isWater, bool isShoreline, string biomeId,
            float normalizedElevation, float slopeDegrees, float moisture01)
        {
            IsLand = isLand;
            IsWater = isWater;
            IsShoreline = isShoreline;
            BiomeId = biomeId ?? string.Empty;
            NormalizedElevation = normalizedElevation;
            SlopeDegrees = slopeDegrees;
            Moisture01 = moisture01;
        }
    }
}
