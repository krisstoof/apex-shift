using ApexShift.Runtime.World.Topography;

namespace ApexShift.Runtime.World.Environment
{
    /// <summary>Immutable environment data cached by the generated topography.</summary>
    public readonly struct EnvironmentSample
    {
        public readonly bool IsLand;
        public readonly bool IsWater;
        public readonly bool IsShoreline;
        public readonly string HabitatId;
        public readonly TerrainType TerrainType;
        public readonly float Height;
        public readonly float NormalizedElevation;
        public readonly float SlopeDegrees;
        public readonly float Moisture01;
        public readonly float Temperature01;
        public readonly float DistanceToCoast;
        public float Dryness01 => 1f - Moisture01;

        public EnvironmentSample(bool isLand, bool isWater, bool isShoreline, string habitatId,
            TerrainType terrainType, float height, float normalizedElevation, float slopeDegrees,
            float moisture01, float temperature01, float distanceToCoast)
        {
            IsLand = isLand;
            IsWater = isWater;
            IsShoreline = isShoreline;
            HabitatId = HabitatIds.Normalize(habitatId);
            TerrainType = terrainType;
            Height = height;
            NormalizedElevation = normalizedElevation;
            SlopeDegrees = slopeDegrees;
            Moisture01 = UnityEngine.Mathf.Clamp01(moisture01);
            Temperature01 = UnityEngine.Mathf.Clamp01(temperature01);
            DistanceToCoast = UnityEngine.Mathf.Max(0f, distanceToCoast);
        }
    }
}
