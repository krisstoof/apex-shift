using System.Linq;
using ApexShift.Core.Crafting;
using ApexShift.Core.Inventory;
using ApexShift.Core.Items;
using ApexShift.Infrastructure.Data.Items;
using ApexShift.Infrastructure.Data.Recipes;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Escape;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Topography;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class RaftDefinitionAndDangerTests
    {
        [Test]
        public void CachedCoastDistanceMeasuresWaterAsWellAsLand()
        {
            var owner = new GameObject("CoastDistanceContract");
            try
            {
                var topography = owner.AddComponent<IslandTopographyRuntime>();
                topography.Build(32, 4f, (x, z) => x < 0f, p => 1f, p => "westwood", biomeResolutionPerTile: 2);
                Assert.IsTrue(topography.TryGetEnvironmentAt(new Vector3(4f, 0f, 0f), out EnvironmentSample near));
                Assert.IsTrue(topography.TryGetEnvironmentAt(new Vector3(30f, 0f, 0f), out EnvironmentSample far));
                Assert.IsTrue(topography.TryGetEnvironmentAt(new Vector3(-30f, 0f, 0f), out EnvironmentSample inland));
                Assert.IsTrue(near.IsWater);
                Assert.IsTrue(far.IsWater);
                Assert.Greater(near.DistanceToCoast, 0f);
                Assert.Greater(far.DistanceToCoast, near.DistanceToCoast);
                Assert.Greater(far.DistanceToCoast, 16f);
                Assert.IsTrue(inland.IsLand);
                Assert.Greater(inland.DistanceToCoast, 16f);
                var random = Random.state;
                Assert.IsTrue(topography.TryGetEnvironmentAt(new Vector3(30f, 0f, 0f), out EnvironmentSample repeated));
                Assert.AreEqual(far.DistanceToCoast, repeated.DistanceToCoast);
                Assert.AreEqual(random, Random.state);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void CoreAndProductionAssetsAgreeOnRaftAndRecipe()
        {
            ItemDatabase items = ItemDatabase.CreateDefault();
            RecipeDatabase recipes = RecipeDatabase.CreateDefault(items);
            Assert.IsTrue(items.HasItem("raft"));
            Assert.AreEqual(1, items.GetMaxStack("raft"));
            Assert.IsTrue(recipes.HasRecipe("raft"));
            var recipe = recipes.GetRecipe("raft");
            Assert.AreEqual("raft", recipe.ResultItemId.ToString());
            Assert.AreEqual(1, recipe.ResultAmount);
            Assert.AreEqual(10, recipe.Ingredients.Single(i => i.ItemId.ToString() == "wood").Amount);
            Assert.AreEqual(6, recipe.Ingredients.Single(i => i.ItemId.ToString() == "fiber").Amount);
            var itemAsset = AssetDatabase.LoadAssetAtPath<ItemDefinitionAsset>("Assets/_Project/Data/Items/raft.asset");
            var recipeAsset = AssetDatabase.LoadAssetAtPath<RecipeDefinitionAsset>("Assets/_Project/Data/Recipes/raft.asset");
            Assert.NotNull(itemAsset);
            Assert.NotNull(recipeAsset);
            Assert.AreEqual(1, itemAsset.ToCoreDefinition().MaxStackSize);
            CollectionAssert.AreEquivalent(recipe.Ingredients.Select(i => (i.ItemId.ToString(), i.Amount)),
                recipeAsset.ToCoreDefinition().Ingredients.Select(i => (i.ItemId.ToString(), i.Amount)));
        }

        [Test]
        public void CraftingConsumesResourcesAndMissingMaterialsCannotCraft()
        {
            ItemDatabase items = ItemDatabase.CreateDefault();
            var crafting = new CraftingSystem(RecipeDatabase.CreateDefault(items), items);
            var inventory = new InventoryState(items);
            inventory.AddItem("wood", 10);
            inventory.AddItem("fiber", 5);
            Assert.IsFalse(crafting.Craft(inventory, "raft").Succeeded);
            Assert.AreEqual(10, inventory.GetAmount("wood"));
            inventory.AddItem("fiber", 1);
            Assert.IsTrue(crafting.Craft(inventory, "raft").Succeeded);
            Assert.AreEqual(0, inventory.GetAmount("wood"));
            Assert.AreEqual(0, inventory.GetAmount("fiber"));
            Assert.AreEqual(1, inventory.GetAmount("raft"));
        }

        [Test]
        public void WaterPlacementRequiresBothEnvironmentAndPlayableBounds()
        {
            var owner = new GameObject("LaunchValidation");
            try
            {
                var topography = owner.AddComponent<IslandTopographyRuntime>();
                topography.Build(32, 4f, (x, z) => x < 0f, p => 1f, p => "westwood", biomeResolutionPerTile: 2);
                var bounds = owner.AddComponent<ApexShift.Runtime.World.WorldBounds>();
                Vector3 shallow = new Vector3(4f, 0f, 0f);
                Vector3 deep = new Vector3(30f, 0f, 0f);
                bounds.Configure(4f, new[] { shallow, deep });
                Assert.IsTrue(PlaceableSurfaceValidation.ValidateWater(shallow, topography, bounds, 0f, 16f).isValid);
                Assert.IsFalse(PlaceableSurfaceValidation.ValidateWater(deep, topography, bounds, 0f, 16f).isValid,
                    "Deep water must be rejected even when playable bounds include it.");
                Assert.IsFalse(PlaceableSurfaceValidation.ValidateWater(shallow, topography, null, 0f, 16f).isValid);
                Assert.IsFalse(PlaceableSurfaceValidation.ValidateWater(shallow, null, bounds, 0f, 16f).isValid);
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [Test]
        public void RaftDefinitionUsesCraftedItemAndSurfaceModeWhileOtherBuildingsStayLand()
        {
            GameObject owner = new GameObject("RaftDefinitions");
            try
            {
                var placement = owner.AddComponent<BuildingPlacementRuntime>();
                var raft = placement.AvailableDefinitions.Single(d => d.BuildingId == "raft");
                Assert.AreEqual(PlaceableSurfaceMode.ShorelineWater, raft.SurfaceMode);
                Assert.AreEqual("raft", raft.MaterialCosts.Single().ItemId);
                Assert.AreEqual(1, raft.MaterialCosts.Single().Amount);
                Assert.AreEqual(16f, raft.MaxDistanceToCoast);
                Assert.AreEqual(new Vector3(4.8f, 0.6f, 2.7f), raft.FootprintSize);
                Assert.IsTrue(placement.AvailableDefinitions.Where(d => d.BuildingId != "raft")
                    .All(d => d.SurfaceMode == PlaceableSurfaceMode.Land));
            }
            finally { Object.DestroyImmediate(owner); }
        }

        [TestCase(true, false, 5f, false)]
        [TestCase(false, true, 8f, true)]
        [TestCase(false, true, 16f, true)]
        [TestCase(false, true, 17f, false)]
        public void LaunchSurfaceUsesPhysicalWaterAndCoastBand(bool land, bool water, float coast, bool expected)
        {
            var sample = new EnvironmentSample(land, water, false, HabitatIds.Coast, TerrainType.Plain,
                0f, 0f, 0f, 0.5f, 0.5f, coast);
            Assert.AreEqual(expected, PlaceableSurfaceValidation.IsInLaunchBand(sample, 0f, 16f));
        }

        [Test]
        public void DangerIsMonotonicAndReturningReducesDanger()
        {
            var profile = new OceanDangerProfile();
            var calm = OceanDangerEvaluator.Evaluate(profile, 10f, 1f, 1f);
            var medium = OceanDangerEvaluator.Evaluate(profile, 20f, 1f, 1f);
            var severe = OceanDangerEvaluator.Evaluate(profile, 30f, 1f, 1f);
            var failure = OceanDangerEvaluator.Evaluate(profile, 40f, 1f, 1f);
            Assert.AreEqual(0f, calm.Danger01);
            Assert.AreEqual(1f, calm.Durability01);
            Assert.Greater(medium.Danger01, calm.Danger01);
            Assert.Greater(severe.Danger01, medium.Danger01);
            Assert.IsTrue(failure.ShouldFail);
            Assert.Less(OceanDangerEvaluator.Evaluate(profile, 15f, severe.Durability01, 1f).Danger01, severe.Danger01);
            Assert.IsTrue(OceanDangerEvaluator.Evaluate(profile, 35f, 0.01f, 100f).ShouldFail);
        }

        [Test]
        public void DangerIsDeterministicAndNeverUsesRandomState()
        {
            var random = Random.state;
            var a = OceanDangerEvaluator.Evaluate(new OceanDangerProfile(), 27f, 0.9f, 0.1f);
            var b = OceanDangerEvaluator.Evaluate(new OceanDangerProfile(), 27f, 0.9f, 0.1f);
            Assert.AreEqual(a.Danger01, b.Danger01);
            Assert.AreEqual(a.Durability01, b.Durability01);
            Assert.AreEqual(a.ShouldFail, b.ShouldFail);
            Assert.AreEqual(random, Random.state);
        }
    }
}
