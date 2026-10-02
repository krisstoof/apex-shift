using ApexShift.Runtime.World.Topography;

namespace ApexShift.Runtime.World.Vegetation
{
    public readonly struct VegetationEnvironmentSample
    {
        public readonly bool IsLand;
        public readonly bool IsWater;
        public readonly bool IsShoreline;
        public readonly string HabitatId;
        public readonly float NormalizedElevation;
        public readonly float SlopeDegrees;
        public readonly float Moisture01;
        public readonly TerrainType TerrainType;
        public readonly float DistanceToCoast;

        public VegetationEnvironmentSample(bool isLand, bool isWater, bool isShoreline, string habitatId,
            float normalizedElevation, float slopeDegrees, float moisture01,
            TerrainType terrainType = TerrainType.Plain, float distanceToCoast = float.MaxValue)
        {
            IsLand = isLand;
            IsWater = isWater;
            IsShoreline = isShoreline;
            HabitatId = habitatId ?? string.Empty;
            NormalizedElevation = normalizedElevation;
            SlopeDegrees = slopeDegrees;
            Moisture01 = moisture01;
            TerrainType = terrainType;
            DistanceToCoast = distanceToCoast;
        }
    }
}
