using System;

namespace ApexShift.Runtime.World.Environment
{
    /// <summary>Stable IDs for broad procedural world habitats.</summary>
    public static class HabitatIds
    {
        public const string Water = "water";
        public const string Coast = "coast";
        public const string LowlandJungle = "lowland_jungle";
        public const string JungleInterior = "jungle_interior";
        public const string WetJungle = "wet_jungle";
        public const string RockyUpland = "rocky_upland";

        public static string Normalize(string id)
        {
            switch ((id ?? string.Empty).Trim().ToLowerInvariant())
            {
                case Water: return Water;
                case Coast: return Coast;
                case LowlandJungle: return LowlandJungle;
                case JungleInterior: return JungleInterior;
                case WetJungle: return WetJungle;
                case RockyUpland: return RockyUpland;
                default: return JungleInterior;
            }
        }
    }
}
