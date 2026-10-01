using System;

namespace ApexShift.Runtime.World.Environment
{
    /// <summary>Temporary habitat-to-profile adapter for consumers migrating in issues #98/#99.</summary>
    public static class LegacyBiomeCompatibility
    {
        public static string ToLegacyBiomeId(string habitatId)
        {
            switch (HabitatIds.Normalize(habitatId))
            {
                case HabitatIds.Water: return "water";
                case HabitatIds.Coast: return "hearth_meadow";
                case HabitatIds.LowlandJungle:
                case HabitatIds.WetJungle: return "south_thicket";
                case HabitatIds.RockyUpland: return "stoneback_ridge";
                default: return "westwood";
            }
        }

        /// <summary>Compatibility conversion for legacy topography constructors and tests only.</summary>
        public static string FromLegacyBiomeId(string biomeId)
        {
            switch ((biomeId ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "water": return HabitatIds.Water;
                case "hearth_meadow": return HabitatIds.Coast;
                case "south_thicket": return HabitatIds.WetJungle;
                case "stoneback_ridge": return HabitatIds.RockyUpland;
                case "westwood": return HabitatIds.JungleInterior;
                default: return HabitatIds.JungleInterior;
            }
        }
    }
}
