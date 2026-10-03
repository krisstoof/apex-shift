using System.Linq;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Topography;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class LandmarkPlacementPlannerTests
    {
        private static LandmarkPlacementCandidate Candidate(float x, string habitat = HabitatIds.LowlandJungle,
            TerrainType terrain = TerrainType.Plain, bool land = true, bool shoreline = false,
            float slope = 3f, float coast = 25f, float elevation = 0.2f)
            => new LandmarkPlacementCandidate(new Vector3(x, 0f, 0f),
                new EnvironmentSample(land, !land, shoreline, habitat, terrain, 0f, elevation, slope, 0.55f, 0.8f, coast));

        [Test]
        public void CrashRejectsWaterShoreRidgeAndExtremeSlope()
        {
            LandmarkPlacementCandidate safe = Candidate(100f);
            var candidates = new[] { Candidate(0f, land: false), Candidate(20f, shoreline: true),
                Candidate(40f, slope: 40f), Candidate(60f, terrain: TerrainType.Ridge), safe };
            var result = new LandmarkPlacementPlanner().Plan(100, candidates,
                LandmarkPlacementProfile.Production()[0], null);
            Assert.NotNull(result);
            Assert.AreEqual(safe.Position, result.Position);
            Assert.False(result.UsedFallback);
            Assert.That(result.DistanceToCoast, Is.InRange(16f, 40f));
        }

        [Test]
        public void BasePrefersRockyRidgeAndNeverFallsBackNearCrash()
        {
            var profile = LandmarkPlacementProfile.Production()[4];
            var candidates = new[] { Candidate(10f, HabitatIds.RockyUpland, TerrainType.Ridge, elevation: 0.7f),
                Candidate(100f, coast: 50f),
                Candidate(110f, HabitatIds.RockyUpland, TerrainType.Ridge, slope: 13f, coast: 50f, elevation: 0.7f) };
            var planner = new LandmarkPlacementPlanner();
            var result = planner.Plan(123, candidates, profile, null, Vector3.zero);
            Assert.NotNull(result);
            Assert.AreEqual(candidates[2].Position, result.Position);
            Assert.IsNull(planner.Plan(123, new[] { candidates[0] }, profile, null, Vector3.zero));
            result = planner.Plan(123, new[] { Candidate(70f, HabitatIds.JungleInterior, TerrainType.Hills, coast: 50f) }, profile, null, Vector3.zero);
            Assert.NotNull(result);
            Assert.True(result.UsedFallback);
            Assert.GreaterOrEqual(result.Position.x, profile.MinStartDistance);
        }

        [Test]
        public void BaseRelaxesPreferredDistanceBeforeDiscardingRockyInteriorPreference()
        {
            var profile = LandmarkPlacementProfile.Production()[4];
            var candidates = new[] { Candidate(100f, coast: 50f),
                Candidate(75f, HabitatIds.RockyUpland, TerrainType.Ridge, slope: 13f, coast: 50f, elevation: 0.7f) };
            var result = new LandmarkPlacementPlanner().Plan(123, candidates, profile, null, Vector3.zero);
            Assert.NotNull(result);
            Assert.AreEqual(candidates[1].Position, result.Position);
            Assert.True(result.UsedFallback);
        }

        [Test]
        public void ProductionProfilesHaveExplicitHardHabitatSets()
        {
            var expected = new[]
            {
                new[] { HabitatIds.Coast, HabitatIds.LowlandJungle },
                new[] { HabitatIds.WetJungle, HabitatIds.LowlandJungle, HabitatIds.JungleInterior },
                new[] { HabitatIds.LowlandJungle, HabitatIds.JungleInterior },
                new[] { HabitatIds.JungleInterior, HabitatIds.WetJungle, HabitatIds.LowlandJungle },
                new[] { HabitatIds.RockyUpland, HabitatIds.JungleInterior },
                new[] { HabitatIds.JungleInterior, HabitatIds.WetJungle }
            };
            var profiles = LandmarkPlacementProfile.Production();
            Assert.AreEqual(expected.Length, profiles.Length);
            for (int i = 0; i < profiles.Length; i++)
            {
                Assert.That(profiles[i].AllowedHabitats, Is.Not.Empty, profiles[i].LandmarkId);
                CollectionAssert.AreEquivalent(expected[i], profiles[i].AllowedHabitats, profiles[i].LandmarkId);
            }
        }

        [Test]
        public void CrashFallbackCannotEscapeAllowedHabitatOrHardCoastBand()
        {
            var profile = LandmarkPlacementProfile.Production()[0];
            var planner = new LandmarkPlacementPlanner();
            var interior = Candidate(0f, HabitatIds.JungleInterior, coast: 100f);
            var tooFar = Candidate(20f, coast: profile.MaxCoastDistance + 1f);
            // Outside preferred band forces pass two, while the hard band still admits it.
            var permitted = Candidate(40f, coast: 50f, slope: 10f);
            var result = planner.Plan(9, new[] { interior, tooFar, permitted }, profile, null);
            Assert.NotNull(result);
            Assert.AreEqual(permitted.Position, result.Position);
            Assert.True(result.UsedFallback);
            Assert.IsNull(planner.Plan(9, new[] { interior }, profile, null));
            Assert.IsNull(planner.Plan(9, new[] { tooFar }, profile, null));
            // Habitat rejection is independent of the coast-band rejection.
            Assert.IsNull(planner.Plan(9, new[] { Candidate(0f, HabitatIds.JungleInterior, coast: 25f) }, profile, null));
        }

        [Test]
        public void BaseFallbackRejectsLowlandPlainButAcceptsInteriorHills()
        {
            var profile = LandmarkPlacementProfile.Production()[4];
            var planner = new LandmarkPlacementPlanner();
            var lowland = Candidate(100f, coast: 50f);
            var interior = Candidate(75f, HabitatIds.JungleInterior, TerrainType.Hills, coast: 50f);
            var rocky = Candidate(110f, HabitatIds.RockyUpland, TerrainType.Ridge,
                slope: 13f, coast: 50f, elevation: 0.7f);
            Assert.IsNull(planner.Plan(9, new[] { lowland }, profile, null, Vector3.zero));
            var fallback = planner.Plan(9, new[] { lowland, interior }, profile, null, Vector3.zero);
            Assert.NotNull(fallback);
            Assert.True(fallback.UsedFallback);
            Assert.AreEqual(interior.Position, fallback.Position);
            Assert.AreEqual(rocky.Position,
                planner.Plan(9, new[] { lowland, interior, rocky }, profile, null, Vector3.zero).Position);
        }

        [Test]
        public void SeparationAndDeterminismAreIndependentOfCandidateOrderAndGlobalRandom()
        {
            var planner = new LandmarkPlacementPlanner();
            var profile = LandmarkPlacementProfile.Production()[0];
            var candidates = new[] { Candidate(0f), Candidate(5f), Candidate(40f), Candidate(80f) };
            Random.State original = Random.state;
            var first = planner.Plan(9, candidates, profile, null);
            Assert.AreEqual(original, Random.state);
            var repeat = planner.Plan(9, candidates.Reverse().ToArray(), profile, null);
            Assert.AreEqual(first.Position, repeat.Position);
            var nextProfile = new LandmarkPlacementProfile { LandmarkId = "second", MinimumSeparation = 30f };
            var second = planner.Plan(9, candidates, nextProfile, new[] { first });
            Assert.NotNull(second);
            Assert.GreaterOrEqual(LandmarkPlacementPlanner.Distance(first.Position, second.Position), 30f);
            Assert.IsNull(planner.Plan(9, new[] { new LandmarkPlacementCandidate(first.Position, candidates[0].Environment) },
                nextProfile, new[] { first }));
        }

        [TestCase("plane_crash", LandmarkType.PlaneCrash)]
        [TestCase("planecrash", LandmarkType.PlaneCrash)]
        [TestCase("freshwater_source", LandmarkType.FreshwaterSource)]
        [TestCase("freshwatersource", LandmarkType.FreshwaterSource)]
        [TestCase("smuggler_cache", LandmarkType.SmugglerCache)]
        [TestCase("smugglercache", LandmarkType.SmugglerCache)]
        [TestCase("smuggler_camp", LandmarkType.SmugglerCamp)]
        [TestCase("smugglercamp", LandmarkType.SmugglerCamp)]
        [TestCase("base_entrance", LandmarkType.BaseEntrance)]
        [TestCase("baseentrance", LandmarkType.BaseEntrance)]
        [TestCase("old_tree", LandmarkType.OldTree)]
        [TestCase("ruins", LandmarkType.Ruins)]
        [TestCase("pond", LandmarkType.Pond)]
        [TestCase("camp", LandmarkType.Camp)]
        [TestCase("cave_placeholder", LandmarkType.CavePlaceholder)]
        public void ParsesProductionAndLegacyTypes(string name, LandmarkType expected)
            => Assert.AreEqual(expected, LandmarkRuntime.ParseType(name));

        [Test]
        public void LegacySerializedEnumValuesRemainStable()
        {
            Assert.AreEqual(0, (int)LandmarkType.Unknown);
            Assert.AreEqual(1, (int)LandmarkType.OldTree);
            Assert.AreEqual(2, (int)LandmarkType.Ruins);
            Assert.AreEqual(3, (int)LandmarkType.Pond);
            Assert.AreEqual(4, (int)LandmarkType.Camp);
            Assert.AreEqual(5, (int)LandmarkType.CavePlaceholder);
        }
    }
}
