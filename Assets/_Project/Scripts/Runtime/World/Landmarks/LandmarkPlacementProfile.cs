using System;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Topography;

namespace ApexShift.Runtime.World.Landmarks
{
    /// <summary>Hard safety limits are never relaxed; preferred bands may relax in the second pass.</summary>
    [Serializable]
    public sealed class LandmarkPlacementProfile
    {
        public string LandmarkId;
        public LandmarkType Type;
        public string[] AllowedHabitats = Array.Empty<string>();
        public string[] PreferredHabitats = Array.Empty<string>();
        public TerrainType[] AllowedTerrain = Array.Empty<TerrainType>();
        public TerrainType[] PreferredTerrain = Array.Empty<TerrainType>();
        public float MinSlope, MaxSlope = 24f;
        public float MinElevation, MaxElevation = 1f;
        public float MinMoisture, MaxMoisture = 1f;
        public float MinCoastDistance = 4f, MaxCoastDistance = float.PositiveInfinity;
        public float MinStartDistance, MinAnchorDistance;
        public float MaxAnchorDistance = float.PositiveInfinity;
        public float MinimumSeparation = 18f;
        public float PreferredMinCoast = 16f, PreferredMaxCoast = 150f;
        public float PreferredStartDistance;
        public float PreferredAnchorDistance = 60f;
        public float PreferredElevation = 0.3f, PreferredSlope = 5f, PreferredMoisture = 0.55f;
        public float HabitatWeight = 3f, TerrainWeight = 1f, ElevationWeight = 1f;
        public float SlopeWeight = 1f, MoistureWeight = 1f, CoastWeight = 2f, AnchorWeight = 1f;

        public static LandmarkPlacementProfile[] Production()
        {
            var crash = New("plane_crash", LandmarkType.PlaneCrash,
                HabitatIds.Coast, HabitatIds.LowlandJungle);
            crash.AllowedTerrain = new[] { TerrainType.Plain, TerrainType.Forest, TerrainType.Hills };
            crash.MaxSlope = 14f; crash.MaxElevation = 0.65f; crash.MaxMoisture = 0.9f;
            crash.MinCoastDistance = 8f; crash.PreferredMinCoast = 16f; crash.PreferredMaxCoast = 40f;
            crash.PreferredElevation = 0.15f; crash.PreferredSlope = 2f;
            crash.MinimumSeparation = 25f;

            var freshwater = New("freshwater_source", LandmarkType.FreshwaterSource,
                HabitatIds.WetJungle, HabitatIds.LowlandJungle, HabitatIds.JungleInterior);
            freshwater.MinStartDistance = 30f; freshwater.MaxSlope = 14f;
            freshwater.PreferredMoisture = 0.85f; freshwater.MoistureWeight = 5f;
            freshwater.PreferredMinCoast = 25f;

            var cache = New("smuggler_cache", LandmarkType.SmugglerCache,
                HabitatIds.LowlandJungle, HabitatIds.JungleInterior);
            cache.AllowedTerrain = crash.AllowedTerrain;
            cache.MinStartDistance = 45f; cache.MaxSlope = 18f;
            cache.PreferredStartDistance = 60f; cache.PreferredMinCoast = 25f;

            var camp = New("smuggler_camp", LandmarkType.SmugglerCamp,
                HabitatIds.JungleInterior, HabitatIds.WetJungle, HabitatIds.LowlandJungle);
            camp.MinStartDistance = 60f; camp.MinAnchorDistance = 25f;
            camp.PreferredStartDistance = 100f; camp.PreferredMinCoast = 40f;
            camp.PreferredAnchorDistance = 60f; camp.MinimumSeparation = 25f;

            var entrance = New("base_entrance", LandmarkType.BaseEntrance,
                HabitatIds.RockyUpland, HabitatIds.JungleInterior);
            entrance.PreferredTerrain = new[] { TerrainType.Ridge, TerrainType.Hills };
            entrance.MinStartDistance = 60f; entrance.PreferredStartDistance = 90f;
            entrance.PreferredElevation = 0.7f; entrance.PreferredSlope = 13f;
            entrance.ElevationWeight = 3f; entrance.TerrainWeight = 3f;
            entrance.MinCoastDistance = 20f; entrance.PreferredMinCoast = 35f; entrance.MinimumSeparation = 30f;

            var tree = New("old_tree", LandmarkType.OldTree, HabitatIds.JungleInterior, HabitatIds.WetJungle);
            tree.MinStartDistance = 25f; tree.MaxSlope = 18f;
            tree.PreferredMinCoast = 30f;

            return new[] { crash, freshwater, cache, camp, entrance, tree };
        }

        private static LandmarkPlacementProfile New(string id, LandmarkType type, params string[] preferred)
            => new LandmarkPlacementProfile { LandmarkId = id, Type = type, PreferredHabitats = preferred };
    }
}
