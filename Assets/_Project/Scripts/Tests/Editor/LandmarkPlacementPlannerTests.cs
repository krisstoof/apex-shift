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
            result = planner.Plan(123, new[] { Candidate(70f, coast: 50f) }, profile, null, Vector3.zero);
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
