using ApexShift.Runtime.World.Topography;

namespace ApexShift.Runtime.World.Environment
{
    /// <summary>Classifies habitat only from cached environmental fields, never from position regions.</summary>
    public sealed class HabitatClassifier
    {
        private readonly HabitatClassificationSettings settings;

        public HabitatClassifier(HabitatClassificationSettings settings = null)
        {
            this.settings = settings ?? new HabitatClassificationSettings();
        }

        public string Classify(bool isLand, bool isShoreline, float distanceToCoast,
            TerrainType terrainType, float normalizedElevation, float slopeDegrees, float moisture01)
        {
            if (!isLand || terrainType == TerrainType.Water) return HabitatIds.Water;
            if (isShoreline || distanceToCoast <= settings.CoastBandDistance) return HabitatIds.Coast;
            if (terrainType == TerrainType.Ridge || normalizedElevation >= settings.RockyElevationThreshold
                || slopeDegrees >= settings.RockySlopeThreshold) return HabitatIds.RockyUpland;
            if (moisture01 >= settings.WetMoistureThreshold) return HabitatIds.WetJungle;
            if (normalizedElevation <= settings.LowlandElevationMax) return HabitatIds.LowlandJungle;
            return HabitatIds.JungleInterior;
        }
    }
}
