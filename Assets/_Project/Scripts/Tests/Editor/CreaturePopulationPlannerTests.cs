using System.IO;
using System.Linq;
using ApexShift.Runtime.Config;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Topography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class CreaturePopulationPlannerTests
    {
        private SpeciesDefinition species;
        private readonly Bounds bounds = new Bounds(Vector3.zero, new Vector3(200f, 20f, 200f));
        private readonly Vector3 player = new Vector3(400f, 0f, 400f);
        [SetUp] public void SetUp() { species = SpeciesDefinition.CreateDefault("island_small_prey"); species.PopulationRules.Configure(3, 3, 3); }
        [TearDown] public void TearDown() => Object.DestroyImmediate(species);
        private static EnvironmentSample Sample(bool land = true, bool water = false, bool shoreline = false,
            string habitat = "lowland_jungle", float slope = 2f, float elevation = 0.2f, float moisture = 0.5f, float coast = 10f)
            => new EnvironmentSample(land, water, shoreline, habitat, TerrainType.Plain, 3f, elevation, slope, moisture, 0.8f, coast);
        private static bool Land(Vector3 p, out EnvironmentSample sample) { sample = Sample(); return true; }
        [Test] public void PopulationCapAndGrowthAreGeneric()
        {
            Assert.That(species.PopulationRules.GetPopulationCapForDay(1), Is.EqualTo(3));
            Assert.That(species.PopulationRules.GetPopulationCapForDay(10), Is.EqualTo(3));
            species.PopulationRules.Configure(0, 0, 6, 3, 2, 1);
            Assert.That(species.PopulationRules.GetPopulationCapForDay(2), Is.Zero);
            Assert.That(species.PopulationRules.GetPopulationCapForDay(3), Is.EqualTo(1));
            Assert.That(species.PopulationRules.GetPopulationCapForDay(5), Is.EqualTo(2));
            Assert.That(species.PopulationRules.GetPopulationCapForDay(int.MaxValue), Is.EqualTo(6));
        }
        [Test] public void PlanningNeverExceedsRemainingCapAndUsesAuthoritativeHeight()
        {
            var plan = CreaturePopulationPlanner.Plan(99, species, 1, bounds, Land, player, 2, true);
            Assert.That(plan.Count, Is.EqualTo(1));
            Assert.That(plan[0].Position.y, Is.EqualTo(3f));
            Assert.That(plan[0].SpeciesId, Is.EqualTo("island_small_prey"));
            Assert.That(plan[0].HabitatId, Is.EqualTo("lowland_jungle"));
            Assert.That(CreaturePopulationPlanner.Plan(99, species, 1, bounds, Land, player, 3, true), Is.Empty);
        }
        [TestCase(false, false, false, "lowland_jungle", 2f)]
        [TestCase(true, true, false, "lowland_jungle", 2f)]
        [TestCase(true, false, true, "lowland_jungle", 2f)]
        [TestCase(true, false, false, "rocky_upland", 2f)]
        [TestCase(true, false, false, "lowland_jungle", 60f)]
        public void InvalidEnvironmentProducesNoSpawns(bool land, bool water, bool shoreline, string habitat, float slope)
        {
            bool Sampler(Vector3 p, out EnvironmentSample value) { value = Sample(land, water, shoreline, habitat, slope); return true; }
            Assert.That(CreaturePopulationPlanner.Plan(99, species, 1, bounds, Sampler, player, 0, true), Is.Empty);
        }
        [Test] public void RejectsOutsideBoundsPlayerDistanceAndInvalidRanges()
        {
            Assert.That(CreaturePopulationPlanner.IsCandidateValid(species, bounds, player, new Vector3(101f, 0f, 0f), Sample()), Is.False);
            Assert.That(CreaturePopulationPlanner.IsCandidateValid(species, bounds, Vector3.zero, Vector3.one, Sample()), Is.False);
            Assert.That(CreaturePopulationPlanner.IsCandidateValid(species, bounds, player, Vector3.zero, Sample(coast: 0f)), Is.False);
            species.ConfigureEnvironment(new[]{"lowland_jungle"}, new[]{TerrainType.Plain}, maxElevation: 0.4f, minMoisture: 0.3f, maxMoisture: 0.7f);
            Assert.That(CreaturePopulationPlanner.IsCandidateValid(species, bounds, player, Vector3.zero, Sample(elevation: 0.8f)), Is.False);
            Assert.That(CreaturePopulationPlanner.IsCandidateValid(species, bounds, player, Vector3.zero, Sample(moisture: 0.9f)), Is.False);
            Assert.That(CreaturePopulationPlanner.IsCandidateValid(species, bounds, player, Vector3.zero, Sample()), Is.True);
        }
        [Test] public void InitialAndDailyPlanningAreDeterministicWithoutChangingGlobalRandom()
        {
            var original = Random.state;
            foreach (bool initial in new[]{true, false})
            {
                var first = CreaturePopulationPlanner.Plan(99, species, 4, bounds, Land, player, 0, initial);
                var same = CreaturePopulationPlanner.Plan(99, species, 4, bounds, Land, player, 0, initial);
                var other = CreaturePopulationPlanner.Plan(100, species, 4, bounds, Land, player, 0, initial);
                Assert.That(first.Select(p=>p.Position).ToArray(), Is.EqualTo(same.Select(p=>p.Position).ToArray()));
                Assert.That(first.Select(p=>p.Position).ToArray(), Is.Not.EqualTo(other.Select(p=>p.Position).ToArray()));
            }
            Assert.That(JsonUtility.ToJson(Random.state), Is.EqualTo(JsonUtility.ToJson(original)));
        }
        [Test] public void ProductionDataHasNeutralRolesValidRulesAndPrefabBindings()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameBalanceConfig>("Assets/_Project/Config/GameBalanceConfig.asset");
            var registry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>("Assets/_Project/Data/World/PrefabRegistry.asset");
            Assert.That(config.ValidateConfig(), Is.Empty);
            Assert.That(config.SpeciesDefinitions.Select(s=>s.SpeciesId), Is.EquivalentTo(new[]{"island_small_prey","island_forager","island_predator"}));
            foreach(var definition in config.SpeciesDefinitions) Assert.That(registry.TryGetCreaturePrefab(definition.SpeciesId, out var prefab) && prefab != null, Is.True);
        }
        [Test] public void ProductionGeneratorCannotRegressToLegacyPopulationOrBiomeSpawning()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/World/Generation/WorldGeneratorRuntime.cs");
            foreach(string forbidden in new[]{"scaleVarnaksByDay","varnakDayOneMaxCount","varnakAbsoluteMaxCount","spawnVarnaksOnDayChange","TrySpawnVarnaksForDay","GetVarnakMaxCountForDay","_spawnedVarnakCount","SpawnRegionCreatures","region.Biome.Creatures"})
                Assert.That(source, Does.Not.Contain(forbidden));
            Assert.That(source, Does.Not.Contain("new BiomeClassifier"), "Production terrain must use HabitatClassifier.");
        }
        [TestCase("small_prey", "island_small_prey")]
        [TestCase("grazer", "island_forager")]
        [TestCase("varnak", "island_predator")]
        public void LegacyIdsMigrateCentrally(string legacy, string canonical)
            => Assert.That(CreatureSpeciesCompatibility.Canonicalize(legacy), Is.EqualTo(canonical));
    }
}
