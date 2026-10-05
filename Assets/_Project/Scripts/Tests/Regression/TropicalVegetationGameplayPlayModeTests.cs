#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class TropicalVegetationGameplayPlayModeTests
    {
        [UnityTest]
        public IEnumerator ProductionBerry_RegistersAndConsumesPlantFood() => CheckFood("shrub_forest_01", "berry_bush", "berries", 8f, 2, false);

        [UnityTest]
        public IEnumerator ProductionGrass_RegistersPlantFoodWithoutBlockingPlayer() => CheckFood("groundcover_forest_01", "grass_patch", "grass", 5f, 1, true);

        private static IEnumerator CheckFood(string speciesId, string resourceKind, string item, float biomass, int regrowth, bool renderOnly)
        {
            GameObject ecosystemOwner = null;
            var ecosystem = EcosystemRuntime.Instance;
            if (ecosystem == null) ecosystem = (ecosystemOwner = new GameObject("TropicalFoodContractEcosystem")).AddComponent<EcosystemRuntime>();
            var root = new GameObject("TropicalFoodContractRoot");
            var species = AssetDatabase.LoadAssetAtPath<VegetationSpeciesAsset>("Assets/_Project/Data/Vegetation/Species/" + speciesId + ".asset");
            int before = ecosystem.PlantFoodSourceCount;
            try
            {
                Assert.That(species, Is.Not.Null);
                var instance = VegetationSpawner.CreateInstance(new VegetationPlacement("food_contract_" + speciesId,
                    "jungle_interior", species, Vector3.zero, 0, 1, 0, 0), root.transform);
                yield return null;
                var node = instance.GetComponent<ResourceNodeView>();
                var food = instance.GetComponent<FoodSourceView>();
                Assert.That(node, Is.Not.Null);
                Assert.That(food, Is.Not.Null);
                Assert.That(node.State.ResourceId, Is.EqualTo(resourceKind));
                Assert.That(node.State.ItemId, Is.EqualTo(item));
                Assert.That(node.State.PlayerHarvestable, Is.False, "Preserve actual ES contract rather than inventing berry pickup semantics.");
                Assert.That(node.State.RenderOnly, Is.EqualTo(renderOnly));
                Assert.That(node.State.RegrowthDays, Is.EqualTo(regrowth));
                Assert.That(food.Kind, Is.EqualTo(FoodKind.Plants));
                Assert.That(food.SourceId, Is.EqualTo(resourceKind));
                Assert.That(food.Biomass, Is.EqualTo(biomass));
                var trigger = instance.GetComponent<SphereCollider>();
                Assert.That(trigger, Is.Not.Null);
                float worldRadius = trigger.radius * Mathf.Max(instance.transform.lossyScale.x,
                    instance.transform.lossyScale.y, instance.transform.lossyScale.z);
                Assert.That(worldRadius, Is.EqualTo(renderOnly ? .3375f : .45f).Within(.001f),
                    "Identity wrappers must preserve the actual ES interaction trigger radius after Awake.");
                Assert.That(ecosystem.FoodSources, Does.Contain(food));
                Assert.That(ecosystem.PlantFoodSourceCount, Is.EqualTo(before + 1));
                if (renderOnly) Assert.That(instance.GetComponentsInChildren<Collider>(true).All(c => c.isTrigger || !c.enabled), Is.True);
                Assert.That(instance.GetComponent<HarvestableTreeRuntime>(), Is.Null);
                Assert.That(food.Consume(.5f), Is.EqualTo(.5f));
                Assert.That(food.Biomass, Is.EqualTo(biomass - .5f));
                food.Consume(biomass);
                Assert.That(food.IsEmpty, Is.True);
                Assert.That(ecosystem.FoodSources.Contains(food), Is.False);
                Assert.That(ecosystem.PlantFoodSourceCount, Is.EqualTo(before));
            }
            finally
            {
                Object.DestroyImmediate(root);
                if (ecosystemOwner != null) Object.DestroyImmediate(ecosystemOwner);
            }
        }

        [UnityTest]
        public IEnumerator ProductionHarvestableTrees_PromoteWithStableIdsAndRetainNativeLods()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<VegetationCatalogAsset>("Assets/_Project/Data/Vegetation/VegetationCatalog.asset");
            var root = new GameObject("TropicalTreePromotionRoot");
            try
            {
                foreach (var species in catalog.Species.Where(s => s.Harvestable && (s.Category == VegetationCategory.Tree || s.Category == VegetationCategory.DeadTree)))
                {
                    string id = "production_promotion_" + species.SpeciesId;
                    var instance = VegetationSpawner.CreateInstance(new VegetationPlacement(id, "jungle_interior", species,
                        Vector3.zero, 0, 1, 0, 0), root.transform);
                    var tree = instance.GetComponent<HarvestableTreeRuntime>();
                    Assert.That(tree, Is.Not.Null, species.SpeciesId);
                    Assert.That(tree.TreeId, Is.EqualTo(id));
                    Assert.That(HarvestableTreeRegistry.TryGet(id, out var registered), Is.True);
                    Assert.That(registered, Is.SameAs(tree));
                    Assert.That(instance.GetComponent<ResourceNodeView>().State.ResourceId, Is.EqualTo(species.ResourceKind));
                    var originalLods = species.VisualPrefab.GetComponentsInChildren<LODGroup>(true);
                    var spawnedLods = instance.GetComponentsInChildren<LODGroup>(true);
                    Assert.That(spawnedLods.Length, Is.EqualTo(originalLods.Length));
                    for (int i = 0; i < originalLods.Length; i++)
                    {
                        Assert.That(spawnedLods[i].lodCount, Is.EqualTo(originalLods[i].lodCount));
                        Assert.That(spawnedLods[i].GetLODs().SelectMany(l => l.renderers).All(r => r != null), Is.True);
                    }
                    if (AssetDatabase.GetAssetPath(species.VisualPrefab).Contains("/SpeedTree/"))
                        Assert.That(spawnedLods.Length, Is.GreaterThan(0), species.SpeciesId);
                    Object.DestroyImmediate(instance);
                    Assert.That(HarvestableTreeRegistry.TryGet(id, out _), Is.False);
                }
                yield return null;
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
#endif
