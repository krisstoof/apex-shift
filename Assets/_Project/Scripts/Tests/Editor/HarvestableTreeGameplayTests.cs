using System.Collections.Generic;
using System.Text.RegularExpressions;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Editor
{
    public sealed class HarvestableTreeGameplayTests
    {
        private readonly List<Object> owned = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            ResourceRegistry.ClearForTests();
            HarvestableTreeRegistry.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
            ResourceRegistry.ClearForTests();
            HarvestableTreeRegistry.ClearForTests();
        }

        [Test]
        public void HarvestableTreeIsPromotedAndDecorativeTreeWithSameVisualIsNot()
        {
            GameObject visual = Track(new GameObject("shared_tree_visual"));
            BoxCollider visualCollider = visual.AddComponent<BoxCollider>();
            visualCollider.center = new Vector3(0f, 0.9f, 0f);
            FoodSourceView authoredFoodSource = visual.AddComponent<FoodSourceView>();

            VegetationSpeciesAsset harvestable = CreateSpecies("tree_leafy_01", visual, VegetationCategory.Tree, true, "leafy_tree");
            VegetationSpeciesAsset decorative = CreateSpecies("tree_decorative", visual, VegetationCategory.Tree, false, string.Empty);
            VegetationSpeciesAsset harvestableShrub = CreateSpecies("shrub_decorative_test", visual, VegetationCategory.Shrub, true, "berry_bush");
            Transform root = Track(new GameObject("vegetation_root")).transform;

            SpawnOne(harvestable, root, new Vector3(-4f, 0f, 0f));
            GameObject harvestableInstance = root.GetChild(0).GetChild(0).gameObject;
            HarvestableTreeRuntime tree = harvestableInstance.GetComponent<HarvestableTreeRuntime>();
            ResourceNodeView node = harvestableInstance.GetComponent<ResourceNodeView>();
            Assert.NotNull(tree);
            Assert.NotNull(node);
            Assert.That(tree.TreeId, Is.EqualTo(harvestableInstance.GetComponent<VegetationInstanceRuntime>().InstanceId));
            Assert.That(tree.SpeciesId, Is.EqualTo("tree_leafy_01"));
            Assert.That(tree.BiomeId, Is.EqualTo("westwood"));
            Assert.That(tree.ResourceKind, Is.EqualTo("leafy_tree"));
            Assert.That(node.State.ResourceId, Is.EqualTo("leafy_tree"));
            Assert.That(node.ShowOnResourceMap, Is.False);
            Assert.That(harvestableInstance.GetComponent<FoodSourceView>(), Is.Not.Null);
            Assert.That(harvestableInstance.GetComponentInChildren<FoodSourceView>(true).enabled, Is.False);
            Assert.That(ResourceRegistry.Resources, Does.Contain(node));
            Assert.That(HarvestableTreeRegistry.TryGet(tree.TreeId, out HarvestableTreeRuntime registered), Is.True);
            Assert.That(registered, Is.SameAs(tree));
            CapsuleCollider trunk = harvestableInstance.GetComponentInChildren<CapsuleCollider>();
            Assert.NotNull(trunk);
            Assert.That(trunk.enabled, Is.True);
            Assert.That(trunk.isTrigger, Is.False);
            Assert.That(trunk.direction, Is.EqualTo(1));
            Assert.That(harvestableInstance.GetComponent<BoxCollider>().enabled, Is.False,
                "The unmarked instantiated prefab collider must not become a crown-sized tree trunk.");
            Assert.That(visualCollider.enabled, Is.True, "Promoting an instance must not mutate its shared source visual prefab.");

            SpawnOne(decorative, root, new Vector3(4f, 0f, 0f));
            GameObject decorativeInstance = root.GetChild(1).GetChild(0).gameObject;
            Assert.That(decorativeInstance.GetComponent<VegetationInstanceRuntime>(), Is.Not.Null);
            Assert.That(decorativeInstance.GetComponent<HarvestableTreeRuntime>(), Is.Null);
            Assert.That(decorativeInstance.GetComponent<ResourceNodeView>(), Is.Null);
            Assert.That(decorativeInstance.GetComponent<Collider>(), Is.Not.Null);
            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(1));
            Assert.That(HarvestableTreeRegistry.Count, Is.EqualTo(1));

            SpawnOne(harvestableShrub, root, new Vector3(40f, 0f, 0f));
            GameObject shrubInstance = root.GetChild(2).GetChild(0).gameObject;
            Assert.That(shrubInstance.GetComponent<HarvestableTreeRuntime>(), Is.Null,
                "Harvestable metadata does not promote shrubs in the tree-only slice.");
            Assert.That(shrubInstance.GetComponent<ResourceNodeView>(), Is.Null);
            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(1));
            Assert.That(HarvestableTreeRegistry.Count, Is.EqualTo(1));
            Assert.That(authoredFoodSource.enabled, Is.True, "Promoting an instance must not mutate the shared visual prefab.");
        }

        [Test]
        public void AxeRequirementBlocksHarvestWithoutAxeAndYieldsConfiguredWoodWithAxe()
        {
            GameObject visual = Track(new GameObject("tree_visual"));
            VegetationSpeciesAsset species = CreateSpecies("tree_conifer_01", visual, VegetationCategory.Tree, true, "conifer_tree");
            Transform root = Track(new GameObject("vegetation_root")).transform;
            SpawnOne(species, root, new Vector3(0f, 0f, 2f));
            GameObject instance = root.GetChild(0).GetChild(0).gameObject;
            HarvestableTreeRuntime tree = instance.GetComponent<HarvestableTreeRuntime>();
            ResourceNodeView node = tree.ResourceNode;

            GameObject actor = Track(new GameObject("player_without_axe"));
            PlayerInventoryRuntime inventory = actor.AddComponent<PlayerInventoryRuntime>();
            inventory.EnsureInitialized();
            Assert.That(node.CanInteract(actor), Is.False);
            Assert.That(tree.TryAxeHit(actor), Is.False);
            Assert.That(node.State.IsDepleted, Is.False);
            Assert.That(inventory.Inventory.GetAmount("wood"), Is.EqualTo(0));

            inventory.Inventory.AddItem("axe", 1);
            Assert.That(node.CanInteract(actor), Is.True);
            // Audio fallback uses Destroy on its temporary clip source; Unity warns about this in EditMode only.
            LogAssert.Expect(LogType.Error, new Regex("WorldAudio_proc_pickup_fallback: Destroy may not be called from edit mode.*"));
            Assert.That(tree.TryAxeHit(actor), Is.True);
            Assert.That(node.State.IsDepleted, Is.True);
            Assert.That(inventory.Inventory.GetAmount("wood"), Is.EqualTo(4));
            Assert.That(instance.activeSelf, Is.False);
            Assert.That(HarvestableTreeRegistry.Count, Is.EqualTo(1), "Depleted inactive trees remain registered for stable identity/save integration.");
            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(1));
        }

        [Test]
        public void AxeSphereCastHitsChildTrunkButRejectsDecorativeCollider()
        {
            GameObject visual = Track(new GameObject("shared_visual"));
            BoxCollider visualCollider = visual.AddComponent<BoxCollider>();
            visualCollider.center = new Vector3(0f, 0.9f, 0f);
            VegetationSpeciesAsset decorative = CreateSpecies("tree_decorative", visual, VegetationCategory.Tree, false, string.Empty);
            VegetationSpeciesAsset harvestable = CreateSpecies("tree_leafy_01", visual, VegetationCategory.Tree, true, "leafy_tree");
            Transform root = Track(new GameObject("vegetation_root")).transform;
            SpawnOne(decorative, root, new Vector3(0f, 0f, 1f));

            GameObject player = Track(new GameObject("axe_player"));
            player.transform.forward = Vector3.forward;
            PlayerInventoryRuntime inventory = player.AddComponent<PlayerInventoryRuntime>();
            inventory.EnsureInitialized();
            inventory.Inventory.AddItem("axe", 1);
            PlayerCombatRuntime combat = player.AddComponent<PlayerCombatRuntime>();
            combat.SetAttackOrigin(player.transform);
            combat.SetInventoryRuntime(inventory);
            Physics.SyncTransforms();
            Assert.That(combat.TryHitHarvestableTree(Vector3.forward), Is.False,
                "A collider on decorative VegetationInstanceRuntime is not an axe target.");
            Assert.That(inventory.Inventory.GetAmount("wood"), Is.EqualTo(0));

            SpawnOne(harvestable, root, new Vector3(0f, 0f, 2.4f));
            Physics.SyncTransforms();
            // Keep the EditMode-only audio cleanup warning from masking the combat assertions.
            LogAssert.Expect(LogType.Error, new Regex("WorldAudio_proc_pickup_fallback: Destroy may not be called from edit mode.*"));
            Assert.That(combat.TryHitHarvestableTree(Vector3.forward), Is.True,
                "The child capsule trunk should resolve its parent HarvestableTreeRuntime.");
            Assert.That(inventory.Inventory.GetAmount("wood"), Is.EqualTo(4));
        }

        [Test]
        public void TreeRegistryRetainsInactiveTreesAndUnregistersDestroyedTrees()
        {
            GameObject visual = Track(new GameObject("tree_visual"));
            VegetationSpeciesAsset species = CreateSpecies("tree_dead_01", visual, VegetationCategory.DeadTree, true, "dry_tree");
            Transform root = Track(new GameObject("vegetation_root")).transform;
            SpawnOne(species, root, Vector3.zero);
            HarvestableTreeRuntime tree = root.GetComponentInChildren<HarvestableTreeRuntime>();
            string treeId = tree.TreeId;
            tree.gameObject.SetActive(false);
            Assert.That(HarvestableTreeRegistry.TryGet(treeId, out HarvestableTreeRuntime inactive), Is.True);
            Assert.That(inactive, Is.SameAs(tree));
            Object.DestroyImmediate(tree.gameObject);
            Assert.That(HarvestableTreeRegistry.TryGet(treeId, out _), Is.False);
            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(0));
        }

        [Test]
        public void SamePlacementRegeneratedAfterClearKeepsTreeIdAndClearsBothRegistries()
        {
            GameObject visual = Track(new GameObject("tree_visual"));
            VegetationSpeciesAsset species = CreateSpecies("tree_leafy_01", visual, VegetationCategory.Tree, true, "leafy_tree");
            var placement = new VegetationPlacement("stable_tree_91_104_207", "westwood", species,
                new Vector3(10.4f, 0f, 20.7f), 17f, 1.1f, 0, 0);

            Transform firstRoot = Track(new GameObject("first_generation_vegetation")).transform;
            new VegetationSpawner().Spawn(new[] { placement }, firstRoot, null);
            HarvestableTreeRuntime firstTree = firstRoot.GetComponentInChildren<HarvestableTreeRuntime>();
            Assert.NotNull(firstTree);
            string firstTreeId = firstTree.TreeId;
            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(1));
            Assert.That(HarvestableTreeRegistry.Count, Is.EqualTo(1));

            Object.DestroyImmediate(firstRoot.gameObject);
            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(0));
            Assert.That(HarvestableTreeRegistry.Count, Is.EqualTo(0));

            Transform secondRoot = Track(new GameObject("second_generation_vegetation")).transform;
            new VegetationSpawner().Spawn(new[] { placement }, secondRoot, null);
            HarvestableTreeRuntime secondTree = secondRoot.GetComponentInChildren<HarvestableTreeRuntime>();
            Assert.NotNull(secondTree);
            Assert.That(secondTree.TreeId, Is.EqualTo(firstTreeId));
            Assert.That(HarvestableTreeRegistry.TryGet(firstTreeId, out HarvestableTreeRuntime registered), Is.True);
            Assert.That(registered, Is.SameAs(secondTree));
            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(1));
            Assert.That(HarvestableTreeRegistry.Count, Is.EqualTo(1));
        }

        [Test]
        public void ExplicitlyMarkedPrefabTrunkColliderIsReusedAndEnabled()
        {
            GameObject visual = Track(new GameObject("tree_visual_with_trunk"));
            var crownCollider = visual.AddComponent<BoxCollider>();
            GameObject trunkObject = new GameObject("Trunk");
            trunkObject.transform.SetParent(visual.transform, false);
            trunkObject.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            trunkObject.AddComponent<VegetationTrunkCollider>();
            CapsuleCollider authoredTrunk = trunkObject.AddComponent<CapsuleCollider>();
            authoredTrunk.enabled = false;
            authoredTrunk.isTrigger = true;

            VegetationSpeciesAsset species = CreateSpecies("tree_conifer_01", visual, VegetationCategory.Tree, true, "conifer_tree");
            Transform root = Track(new GameObject("vegetation_root")).transform;
            SpawnOne(species, root, Vector3.zero);
            GameObject instance = root.GetChild(0).GetChild(0).gameObject;
            CapsuleCollider reusedTrunk = instance.GetComponentInChildren<CapsuleCollider>();

            Assert.NotNull(reusedTrunk);
            Assert.That(reusedTrunk.enabled, Is.True);
            Assert.That(reusedTrunk.isTrigger, Is.False);
            Assert.That(reusedTrunk.GetComponent<VegetationTrunkCollider>(), Is.Not.Null);
            Assert.That(instance.transform.Find("ApexShiftCombatTrunk"), Is.Null);
            Assert.That(instance.GetComponent<BoxCollider>().enabled, Is.False);
            Assert.That(crownCollider.enabled, Is.True, "Promoter must not mutate the source prefab collider.");
            Assert.That(authoredTrunk.enabled, Is.False, "Promoter must not mutate the source prefab trunk collider.");
        }

        [Test]
        public void OrdinaryResourceMapVisibilityDefaultsToTrueAndCanBeDisabledForTrees()
        {
            GameObject rockObject = Track(new GameObject("rock"));
            ResourceNodeView rock = rockObject.AddComponent<ResourceNodeView>();
            rock.ConfigureDefault("rock");
            Assert.That(rock.ShowOnResourceMap, Is.True);

            GameObject treeObject = Track(new GameObject("tree"));
            ResourceNodeView tree = treeObject.AddComponent<ResourceNodeView>();
            tree.ConfigureDefault("dry_tree");
            tree.SetShowOnResourceMap(false);
            Assert.That(tree.ShowOnResourceMap, Is.False);
        }

        private VegetationSpeciesAsset CreateSpecies(string id, GameObject visual, VegetationCategory category, bool harvestable, string resourceKind)
        {
            var species = Track(ScriptableObject.CreateInstance<VegetationSpeciesAsset>());
            species.Configure(id, id, visual, category, 1f, 1f, 3.8f, 0f, 90f, 0f, 1f, 0f, 1f,
                VegetationSpeciesAsset.CanonicalBiomeIds, true, harvestable, resourceKind, null,
                VegetationCollisionMode.GameplayResource);
            return species;
        }

        private static void SpawnOne(VegetationSpeciesAsset species, Transform root, Vector3 position)
        {
            string stableId = $"stable_test_{species.SpeciesId}_{Mathf.RoundToInt(position.x * 100f)}_{Mathf.RoundToInt(position.z * 100f)}";
            var placement = new VegetationPlacement(stableId, "westwood", species, position,
                0f, 1f, Mathf.FloorToInt(position.x / 24f), Mathf.FloorToInt(position.z / 24f));
            new VegetationSpawner().Spawn(new[] { placement }, root, null);
        }

        private T Track<T>(T value) where T : Object { owned.Add(value); return value; }
    }
}
