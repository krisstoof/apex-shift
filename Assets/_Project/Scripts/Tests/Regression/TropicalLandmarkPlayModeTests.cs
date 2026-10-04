#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Presentation.HUD;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class TropicalLandmarkPlayModeTests
    {
        [Test]
        public void MapAndMinimapHideUndiscoveredLandmarksAndDiscoveryIsIdempotent()
        {
            var owner = new GameObject("UndiscoveredEntrance");
            try
            {
                var landmark = owner.AddComponent<LandmarkRuntime>();
                landmark.Configure("base_entrance", LandmarkType.BaseEntrance, null);
                Assert.AreEqual("Hidden Entrance", landmark.DisplayName);
                var discovery = owner.AddComponent<LandmarkDiscoveryRuntime>();
                Assert.False(MapScreenUI.ShouldShowLandmark(landmark));
                Assert.False(MiniMapUI.ShouldShowLandmark(landmark));
                Assert.False(discovery.TryDiscoverAt(new Vector3(50f, 0f, 0f)));
                int events = 0;
                landmark.Discovered += _ => events++;
                Assert.True(discovery.TryDiscoverAt(Vector3.zero));
                Assert.False(landmark.Discover());
                Assert.AreEqual(1, events);
                Assert.True(MapScreenUI.ShouldShowLandmark(landmark));
                Assert.True(MiniMapUI.ShouldShowLandmark(landmark));
                owner.SetActive(false);
                Assert.False(MapScreenUI.ShouldShowLandmark(landmark));
                Assert.False(MiniMapUI.ShouldShowLandmark(landmark));
            }
            finally { Object.DestroyImmediate(owner); LandmarkRegistry.ClearForTests(); }
        }

        [UnityTest]
        public IEnumerator RegisteredPlayerProximityDiscoversWithoutSceneScan()
        {
            var owner = new GameObject("ProximityLandmark");
            var player = new GameObject("RegisteredTestPlayer");
            try
            {
                var landmark = owner.AddComponent<LandmarkRuntime>();
                landmark.Configure("smuggler_cache", LandmarkType.SmugglerCache, null);
                owner.AddComponent<LandmarkDiscoveryRuntime>();
                player.transform.position = new Vector3(50f, 0f, 0f);
                player.AddComponent<PlayerPresenceRuntime>();
                yield return null;
                Assert.False(landmark.IsDiscovered);
                player.transform.position = new Vector3(5f, 0f, 0f);
                yield return null;
                Assert.True(landmark.IsDiscovered);
            }
            finally
            {
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(owner);
                LandmarkRegistry.ClearForTests();
            }
        }

        [UnityTest]
        public IEnumerator ProductionGeneration_ThreeSeedsStartAtCrashAndPreserveDiscoveryThroughSaveLoad()
        {
            var owner = new GameObject("TropicalLandmarkGenerator");
            GameObject saveOwner = null;
            var generator = owner.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            var catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>("Assets/_Project/Data/Biomes/BiomeCatalog.asset");
            Assert.NotNull(catalog);
            generator.SetBiomeCatalog(catalog);
            generator.SetHabitatVegetationCatalog(AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>(
                "Assets/_Project/Data/Vegetation/HabitatVegetationCatalog.asset"));
            try
            {
                foreach (int seed in new[] { 12345, 81281, 91284 })
                {
                    generator.SetSeed(seed);
                    generator.Generate();
                    // Check initial discovery before proximity Update.
                    string[] ids = LandmarkPlacementProfile.Production().Select(p => p.LandmarkId).ToArray();
                    CollectionAssert.AreEquivalent(ids, LandmarkRegistry.Landmarks.Select(l => l.LandmarkId).ToArray());
                    CollectionAssert.AreEqual(new[] { "plane_crash", "base_entrance" },
                        generator.LandmarkPlacements.Take(2).Select(p => p.LandmarkId).ToArray(),
                        "Reserve the constrained base site before freshwater/cache/camp consume its legal candidates.");
                    foreach (LandmarkRuntime landmark in LandmarkRegistry.Landmarks)
                        Assert.AreEqual(landmark.Type == LandmarkType.PlaneCrash, landmark.IsDiscovered, landmark.LandmarkId);

                    WorldGenerationContext context = generator.CurrentGeneration;
                    Assert.NotNull(context.VegetationRuntime);
                    LandmarkRuntime crash = LandmarkRegistry.FindById("plane_crash");
                    Vector3 playerPosition = context.Player.transform.position;
                    float spawnDistance = LandmarkPlacementPlanner.Distance(playerPosition, crash.transform.position);
                    Assert.That(spawnDistance, Is.InRange(2f, 8f));
                    Assert.AreEqual(generator.ResolvePlayerSpawnPoint(), playerPosition);
                    Assert.True(context.IslandTopography.TryGetEnvironmentAt(playerPosition, out EnvironmentSample environment));
                    Assert.True(environment.IsLand);
                    Assert.False(environment.IsWater);
                    Assert.False(environment.IsShoreline);
                    Assert.LessOrEqual(environment.SlopeDegrees, 14f);
                    Assert.AreNotEqual(ApexShift.Runtime.World.Topography.TerrainType.Ridge, environment.TerrainType);
                    Assert.True(context.WorldBounds.Contains(playerPosition));
                    EnvironmentSample[] environmentBefore = SnapshotEnvironment(context);
                    Assert.GreaterOrEqual(DistanceFromCrash("smuggler_cache"), 45f);
                    Assert.GreaterOrEqual(DistanceFromCrash("smuggler_camp"), 60f);
                    Assert.GreaterOrEqual(DistanceFromCrash("base_entrance"), 60f);

                    var points = new List<VegetationDebugPoint>();
                    context.VegetationRuntime.CopyDebugPoints(points);
                    Assert.That(points.Count, Is.GreaterThan(0), "Production species must generate real placements.");
                    foreach (VegetationDebugPoint point in points)
                    {
                        Assert.True(context.IslandTopography.TryGetEnvironmentAt(point.Position, out EnvironmentSample vegetationSample));
                        Assert.True(vegetationSample.IsLand); Assert.False(vegetationSample.IsWater);
                        Assert.False(vegetationSample.IsShoreline);
                        if (point.Category != VegetationCategory.Tree && point.Category != VegetationCategory.DeadTree) continue;
                        Assert.GreaterOrEqual(LandmarkPlacementPlanner.Distance(point.Position, playerPosition),
                            generator.StartClearingRadius - 0.05f);
                        foreach (LandmarkRuntime landmark in LandmarkRegistry.Landmarks)
                            Assert.GreaterOrEqual(LandmarkPlacementPlanner.Distance(point.Position, landmark.transform.position),
                                generator.GenerationSettings.Vegetation.LandmarkClearances.GetRadius(landmark.Type) - 0.05f);
                    }
                    ValidateInstantiatedVegetation(context);
                    foreach (LandmarkPlacementResult result in generator.LandmarkPlacements)
                    {
                        Assert.True(context.IslandTopography.TryGetEnvironmentAt(result.Position, out EnvironmentSample sample));
                        Assert.True(sample.IsLand); Assert.False(sample.IsWater); Assert.False(sample.IsShoreline);
                        Assert.AreEqual(sample.HabitatId, result.HabitatId);
                        LandmarkPlacementProfile profile = LandmarkPlacementProfile.Production()
                            .Single(p => p.LandmarkId == result.LandmarkId);
                        CollectionAssert.Contains(profile.AllowedHabitats, sample.HabitatId,
                            $"{result.LandmarkId} escaped its hard habitat constraint for seed {seed}.");
                        Assert.That(sample.DistanceToCoast, Is.InRange(profile.MinCoastDistance, profile.MaxCoastDistance),
                            $"{result.LandmarkId} escaped its hard coast band for seed {seed}.");
                        Assert.That(sample.SlopeDegrees, Is.InRange(profile.MinSlope, profile.MaxSlope));
                        Assert.That(sample.NormalizedElevation, Is.InRange(profile.MinElevation, profile.MaxElevation));
                        Assert.That(sample.Moisture01, Is.InRange(profile.MinMoisture, profile.MaxMoisture));
                        if (profile.AllowedTerrain.Length > 0) CollectionAssert.Contains(profile.AllowedTerrain, sample.TerrainType);
                        if (result.LandmarkId != "plane_crash")
                            Assert.GreaterOrEqual(DistanceFromCrash(result.LandmarkId), profile.MinStartDistance);
                        if (result.LandmarkId == "smuggler_camp")
                            Assert.That(LandmarkPlacementPlanner.Distance(result.Position, LandmarkRegistry.FindById("smuggler_cache").transform.position),
                                Is.InRange(profile.MinAnchorDistance, profile.MaxAnchorDistance));
                        Debug.Log($"[LandmarkPlacement] seed={seed} id={result.LandmarkId} position={result.Position:F3} habitat={result.HabitatId} terrain={result.TerrainType} slope={result.Slope:F2} elevation={result.Elevation:F3} coast={result.DistanceToCoast:F2} fallback={result.UsedFallback}");
                        foreach (LandmarkPlacementResult other in generator.LandmarkPlacements)
                            if (other != result) Assert.GreaterOrEqual(LandmarkPlacementPlanner.Distance(result.Position, other.Position),
                                Mathf.Max(result.MinimumSeparation, other.MinimumSeparation) - 0.01f);
                    }
                    Debug.Log($"[LandmarkDistances] seed={seed} cache={DistanceFromCrash("smuggler_cache"):F3} camp={DistanceFromCrash("smuggler_camp"):F3} base={DistanceFromCrash("base_entrance"):F3}");
                    Vector3[] positions = generator.LandmarkPlacements.Select(p => p.Position).ToArray();
                    LandmarkRegistry.FindById("smuggler_cache").Discover();
                    saveOwner = new GameObject("LandmarkSaveService");
                    var service = saveOwner.AddComponent<GameSaveService>();
                    // Exercise the real DTO serialization boundary, then regeneration/ApplyLoadedState.
                    GameSaveData saved = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(service.CaptureCurrentState()));
                    Assert.True(service.ApplyLoadedState(saved, "issue100"));
                    CollectionAssert.AreEqual(environmentBefore, SnapshotEnvironment(generator.CurrentGeneration),
                        "The same seed must reproduce habitat, terrain, height, slope, moisture, temperature and coast distance.");
                    Assert.AreEqual(playerPosition, generator.ResolvePlayerSpawnPoint(), "Logical crash spawn changed after regeneration.");
                    CollectionAssert.AreEqual(positions, generator.LandmarkPlacements.Select(p => p.Position).ToArray());
                    Assert.True(LandmarkRegistry.FindById("smuggler_cache").IsDiscovered);
                    Assert.False(LandmarkRegistry.FindById("base_entrance").IsDiscovered);
                    Assert.AreEqual(1, LandmarkRegistry.Landmarks.Count(l => l.LandmarkId == "smuggler_cache"));
                    Assert.AreEqual(1, LandmarkRegistry.Landmarks.Count(l => l.LandmarkId == "base_entrance"));
                    // A legacy save entry is restored under the owned generation, not a scene root.
                    saved.World.landmarkStates.Add(new LandmarkSaveData("ruins", "Ruins", "Legacy Ruins", "",
                        0f, 0f, 0f, true));
                    Assert.True(service.ApplyLoadedState(saved, "issue100_legacy"));
                    Assert.AreEqual(LandmarkType.Ruins, LandmarkRegistry.FindById("ruins").Type);
                    Assert.True(LandmarkRegistry.FindById("ruins").transform.IsChildOf(generator.CurrentGeneration.LandmarkRoot));
                    Object.DestroyImmediate(saveOwner);
                    generator.ClearGeneratedWorld();
                    yield return null;
                }
            }
            finally
            {
                if (saveOwner != null) Object.DestroyImmediate(saveOwner);
                generator.ClearGeneratedWorld();
                Object.DestroyImmediate(owner);
                LandmarkRegistry.ClearForTests();
            }
        }

        private static float DistanceFromCrash(string id) => LandmarkPlacementPlanner.Distance(
            LandmarkRegistry.FindById("plane_crash").transform.position, LandmarkRegistry.FindById(id).transform.position);

        private static EnvironmentSample[] SnapshotEnvironment(WorldGenerationContext context)
        {
            var samples = new List<EnvironmentSample>();
            var bounds = context.IslandTopography.WorldBounds;
            string[] canonical = { "water", "coast", "lowland_jungle", "jungle_interior", "wet_jungle", "rocky_upland" };
            for (float x = bounds.min.x + 2f; x < bounds.max.x; x += 4f)
            for (float z = bounds.min.z + 2f; z < bounds.max.z; z += 4f)
            {
                Assert.True(context.IslandTopography.TryGetEnvironmentAt(new Vector3(x, 0f, z), out EnvironmentSample sample));
                CollectionAssert.Contains(canonical, sample.HabitatId, "Production environment must use habitat IDs, never legacy profile IDs.");
                samples.Add(sample);
            }
            return samples.ToArray();
        }

        private static void ValidateInstantiatedVegetation(WorldGenerationContext context)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<HabitatVegetationCatalogAsset>(
                "Assets/_Project/Data/Vegetation/HabitatVegetationCatalog.asset");
            var instances = context.VegetationRoot.GetComponentsInChildren<VegetationInstanceRuntime>(true);
            Assert.That(instances.Length, Is.GreaterThan(0), "Check actual production instances, not only planner data.");
            foreach (var instance in instances)
            {
                Vector3 position = instance.transform.position;
                Assert.True(context.IslandTopography.TryGetEnvironmentAt(position, out EnvironmentSample sample));
                Assert.True(sample.IsLand); Assert.False(sample.IsWater); Assert.False(sample.IsShoreline);
                Assert.True(context.WorldBounds.Contains(position));
                Assert.AreEqual(sample.HabitatId, instance.HabitatId);
                var profile = catalog.GetProfile(sample.HabitatId);
                Assert.NotNull(profile, "Production instance must come from the tropical habitat catalog.");
                var species = profile.Species.Select(entry => entry.Species).Single(s => s.SpeciesId == instance.SpeciesId);
                Assert.True(species.AllowsHabitat(sample.HabitatId));
                Assert.True(species.AllowsTerrain(sample.TerrainType));
                Assert.That(sample.SlopeDegrees, Is.InRange(species.MinSlopeDegrees, species.MaxSlopeDegrees));
                Assert.That(sample.NormalizedElevation, Is.InRange(species.MinElevation01, species.MaxElevation01));
                Assert.That(sample.Moisture01, Is.InRange(species.MinMoisture01, species.MaxMoisture01));
                Assert.That(sample.DistanceToCoast, Is.InRange(species.MinDistanceToCoast, species.MaxDistanceToCoast));
                Assert.True(instance.transform.IsChildOf(context.GenerationRoot));
            }
        }
    }
}
#endif
