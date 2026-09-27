using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Infrastructure.Save;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Items;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEngine;

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
            ItemPickupRegistry.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = owned.Count - 1; i >= 0; i--)
                if (owned[i] != null) Object.DestroyImmediate(owned[i]);
            owned.Clear();
            for (int i = ItemPickupRegistry.Pickups.Count - 1; i >= 0; i--)
                if (ItemPickupRegistry.Pickups[i] != null) Object.DestroyImmediate(ItemPickupRegistry.Pickups[i].gameObject);
            ItemPickupRegistry.ClearForTests();
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
            Assert.That(node.DirectInteractionEnabled, Is.False);
            Assert.That(node.Prompt, Is.Empty);
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
            MeshRenderer standingRenderer = visual.AddComponent<MeshRenderer>();
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
            Assert.That(tree.CanAxeHit(actor), Is.False);
            Assert.That(tree.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward)), Is.False);
            Assert.That(tree.CurrentHealth, Is.EqualTo(tree.MaxHealth));
            Assert.That(node.State.IsDepleted, Is.False);
            Assert.That(node.Interact(actor), Is.False, "Direct interaction may not harvest a tree outside its HP lifecycle.");
            Assert.That(inventory.Inventory.GetAmount("wood"), Is.EqualTo(0));

            inventory.Inventory.AddItem("axe", 1);
            Assert.That(node.CanInteract(actor), Is.False, "Interaction must not bypass tree HP even while an axe is owned.");
            Assert.That(tree.CanAxeHit(actor), Is.True);
            for (int i = 0; i < 4; i++)
            {
                Assert.That(tree.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward)), Is.True);
                Assert.That(tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Standing));
            }
            Assert.That(tree.CurrentHealth, Is.EqualTo(20f));
            Assert.That(tree.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward)), Is.True);
            Assert.That(node.State.IsDepleted, Is.True);
            Assert.That(tree.CurrentHealth, Is.EqualTo(0f));
            Assert.That(tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted));
            Assert.That(inventory.Inventory.GetAmount("wood"), Is.EqualTo(0), "Tree harvest drops are pickups, not direct inventory changes.");
            Assert.That(instance.activeSelf, Is.True);
            Assert.That(tree.DropsSpawned, Is.True);
            Assert.That(ItemPickupRegistry.Pickups.Count, Is.EqualTo(1));
            Assert.That(ItemPickupRegistry.Pickups[0].ItemId, Is.EqualTo("wood"));
            Assert.That(ItemPickupRegistry.Pickups[0].Amount, Is.EqualTo(4));
            Assert.That(instance.transform.Find("TreeStump").gameObject.activeSelf, Is.True);
            Assert.That(instance.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That(tree.TrunkColliders[0].enabled, Is.False);
            Assert.That(node.GetComponent<SphereCollider>().enabled, Is.False, "The interaction trigger is unusable after depletion.");
            Assert.That(HarvestableTreeRegistry.Count, Is.EqualTo(1), "Depleted inactive trees remain registered for stable identity/save integration.");
            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(1));
            Assert.That(tree.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward)), Is.False);
            Assert.That(ItemPickupRegistry.Pickups.Count, Is.EqualTo(1));
            Assert.That(standingRenderer.enabled, Is.True, "Falling/depleting one instance must not change its shared visual prefab.");
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
            for (int i = 0; i < 4; i++)
                Assert.That(combat.TryHitHarvestableTree(Vector3.forward), Is.True,
                    "The child capsule trunk should resolve its parent HarvestableTreeRuntime.");
            HarvestableTreeRuntime harvestedTree = root.GetChild(1).GetChild(0).GetComponent<HarvestableTreeRuntime>();
            Assert.That(harvestedTree.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted));
            Assert.That(inventory.Inventory.GetAmount("wood"), Is.EqualTo(0));
            Assert.That(ItemPickupRegistry.Pickups.Count, Is.EqualTo(1));
            Assert.That(ItemPickupRegistry.Pickups[0].Amount, Is.EqualTo(4));
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
        public void RegrowthRestoresHealthVisualsColliderAndResourceState()
        {
            var setup = SpawnTree("tree_dead_01", VegetationCategory.DeadTree, "dry_tree", 0f, 4);
            GameObject actor = CreateActorWithAxe("tree_cutter");
            for (int i = 0; i < 3; i++) setup.Tree.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward));
            Assert.That(setup.Tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted));
            Assert.That(setup.Node.State.IsDepleted, Is.True);
            Assert.That(setup.Tree.AdvanceGrowthDays(3), Is.True);
            Assert.That(setup.Tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted));
            Assert.That(setup.Tree.RegrowthProgress, Is.EqualTo(0.75f).Within(0.001f));

            Assert.That(setup.Tree.AdvanceGrowthDays(1), Is.True);
            Assert.That(setup.Tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Standing));
            Assert.That(setup.Tree.CurrentHealth, Is.EqualTo(70f));
            Assert.That(setup.Tree.DropsSpawned, Is.False);
            Assert.That(setup.Node.State.IsDepleted, Is.False);
            Assert.That(setup.Tree.TrunkColliders[0].enabled, Is.True);
            Assert.That(setup.Instance.GetComponentInChildren<Renderer>().enabled, Is.True);
            Assert.That(setup.Instance.transform.Find("TreeStump"), Is.Null);
        }

        [Test]
        public void EcosystemDirectorTickDayAdvancesRegisteredTreeRegrowth()
        {
            var setup = SpawnTree("tree_dead_01", VegetationCategory.DeadTree, "dry_tree", 0f, 4);
            GameObject actor = CreateActorWithAxe("tree_cutter");
            for (int i = 0; i < 3; i++)
                setup.Tree.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward));
            Assert.That(setup.Tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted));

            GameObject directorObject = Track(new GameObject("ecosystem_director"));
            EcosystemDirectorRuntime director = directorObject.AddComponent<EcosystemDirectorRuntime>();
            director.InitializeFromRegions(null);
            director.TickDay(3);
            Assert.That(setup.Tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted));

            director.TickDay(1);
            Assert.That(setup.Tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Standing));
            Assert.That(setup.Tree.CurrentHealth, Is.EqualTo(setup.Tree.MaxHealth));
        }

        [Test]
        public void PartialDamageSaveRestoresByTreeIdAndFurtherHitsContinue()
        {
            GameObject visual = Track(new GameObject("tree_visual"));
            VegetationSpeciesAsset species = CreateSpecies("tree_leafy_01", visual, VegetationCategory.Tree, true, "leafy_tree");
            var placement = new VegetationPlacement("tree_partial_save_id", "westwood", species, Vector3.zero, 0f, 1f, 0, 0);
            Transform firstRoot = Track(new GameObject("first_generation")).transform;
            new VegetationSpawner().Spawn(new[] { placement }, firstRoot, null);
            HarvestableTreeRuntime firstTree = firstRoot.GetComponentInChildren<HarvestableTreeRuntime>();
            GameObject actor = CreateActorWithAxe("tree_cutter");
            firstTree.TryAxeHit(new AxeHitContext(actor, 55f, Vector3.zero, Vector3.forward));
            TreeSaveData saved = firstTree.CaptureSaveData();
            Assert.That(saved.CurrentHealth, Is.EqualTo(45f));

            Object.DestroyImmediate(firstRoot.gameObject);
            Transform secondRoot = Track(new GameObject("loaded_generation")).transform;
            new VegetationSpawner().Spawn(new[] { placement }, secondRoot, null);
            HarvestableTreeRuntime restored = secondRoot.GetComponentInChildren<HarvestableTreeRuntime>();
            restored.RestoreSaveData(saved);
            Assert.That(restored.TreeId, Is.EqualTo("tree_partial_save_id"));
            Assert.That(restored.CurrentHealth, Is.EqualTo(45f));
            Assert.That(restored.LifecycleState, Is.EqualTo(TreeLifecycleState.Standing));
            Assert.That(restored.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward)), Is.True);
            Assert.That(restored.CurrentHealth, Is.EqualTo(20f));
            Assert.That(restored.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward)), Is.True);
            Assert.That(restored.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted));
            Assert.That(ItemPickupRegistry.Pickups.Count, Is.EqualTo(1));
        }

        [Test]
        public void DepletedAndFallingSaveRestoreDoNotDuplicateDrops()
        {
            var depletedSetup = SpawnTree("tree_leafy_01", VegetationCategory.Tree, "leafy_tree", 0f, 5);
            GameObject actor = CreateActorWithAxe("tree_cutter");
            for (int i = 0; i < 4; i++) depletedSetup.Tree.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward));
            TreeSaveData depletedSave = depletedSetup.Tree.CaptureSaveData();
            Assert.That(depletedSave.DropsSpawned, Is.True);
            Assert.That(ItemPickupRegistry.Pickups.Count, Is.EqualTo(1));
            depletedSetup.Tree.RestoreSaveData(depletedSave);
            Assert.That(ItemPickupRegistry.Pickups.Count, Is.EqualTo(1));

            var fallingSetup = SpawnTree("tree_conifer_01", VegetationCategory.Tree, "conifer_tree", 1.35f, 6);
            fallingSetup.Tree.TryAxeHit(new AxeHitContext(actor, 120f, Vector3.zero, Vector3.forward));
            TreeSaveData fallingSave = fallingSetup.Tree.CaptureSaveData();
            Assert.That(fallingSave.LifecycleState, Is.EqualTo("Falling"));
            Assert.That(fallingSetup.Tree.CurrentHealth, Is.EqualTo(0f));
            Assert.That(fallingSetup.Tree.TrunkColliders[0].enabled, Is.False);
            Assert.That(fallingSave.DropsSpawned, Is.False);
            Assert.That(fallingSetup.Tree.TryAxeHit(new AxeHitContext(actor, 25f, Vector3.zero, Vector3.forward)), Is.False);
            fallingSetup.Tree.RestoreSaveData(fallingSave);
            Assert.That(fallingSetup.Tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted));
            Assert.That(ItemPickupRegistry.Pickups.Count, Is.EqualTo(2), "Settling a saved Falling tree creates its pending yield once.");
            TreeSaveData settledSave = fallingSetup.Tree.CaptureSaveData();
            fallingSetup.Tree.RestoreSaveData(settledSave);
            Assert.That(ItemPickupRegistry.Pickups.Count, Is.EqualTo(2));
        }

        [Test]
        public void SaveCapturesTreeOnceAndKeepsOrdinaryResourcesAndPickups()
        {
            var tree = SpawnTree("tree_leafy_01", VegetationCategory.Tree, "leafy_tree", 0f, 5);
            tree.Tree.TryAxeHit(new AxeHitContext(CreateActorWithAxe("tree_cutter"), 25f, Vector3.zero, Vector3.forward));
            GameObject rockObject = Track(new GameObject("ordinary_rock"));
            rockObject.transform.position = new Vector3(40f, 0f, 0f);
            ResourceNodeView rock = rockObject.AddComponent<ResourceNodeView>();
            rock.ConfigureDefault("rock");
            GameObject pickup = ItemPickupSpawner.Spawn("wood", 3, new Vector3(10f, 0f, 0f), Quaternion.identity);

            GameSaveService saveService = Track(new GameObject("save_service")).AddComponent<GameSaveService>();
            GameSaveData save = saveService.CaptureCurrentState();
            Assert.That(save.World.TreeStates.Count, Is.EqualTo(1));
            Assert.That(save.World.TreeStates[0].TreeId, Is.EqualTo(tree.Tree.TreeId));
            Assert.That(save.World.TreeStates[0].CurrentHealth, Is.EqualTo(75f));
            Assert.That(save.World.Resources.Any(resource =>
                Mathf.Abs(resource.x - tree.Instance.transform.position.x) < 0.001f
                && Mathf.Abs(resource.z - tree.Instance.transform.position.z) < 0.001f), Is.False,
                "A tree must not also be serialized as a generic ResourceSaveData.");
            Assert.That(save.World.Resources.Any(resource => resource.ResourceId == "rock"), Is.True);
            Assert.That(save.World.Pickups.Any(savedPickup => savedPickup.ItemId == "wood" && savedPickup.Amount == 3), Is.True);

            tree.Tree.TryAxeHit(new AxeHitContext(CreateActorWithAxe("second_cutter"), 25f, Vector3.zero, Vector3.forward));
            Assert.That(tree.Tree.CurrentHealth, Is.EqualTo(50f));
            var restoreTrees = typeof(GameSaveService).GetMethod("RestoreTreeStates", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(restoreTrees);
            restoreTrees.Invoke(null, new object[] { save.World.TreeStates });
            Assert.That(tree.Tree.CurrentHealth, Is.EqualTo(75f), "Save restoration resolves state by stable TreeId.");

            Object.DestroyImmediate(pickup);
            var restoreMethod = typeof(GameSaveService).GetMethod("RestorePickupStates", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(restoreMethod);
            restoreMethod.Invoke(saveService, new object[] { save.World.Pickups });
            Assert.That(ItemPickupRegistry.Pickups.Count(p => p != null && p.ItemId == "wood" && p.Amount == 3), Is.EqualTo(1));
        }

        [Test]
        public void LegacySaveWithoutTreeStatesRemainsDeserializable()
        {
            const string payload = "{\"world\":{\"seed\":321,\"day\":3,\"timeOfDay\":0.5,\"resources\":[]}}";
            GameSaveData restored = new UnityJsonGameSaveSerializer().Deserialize(payload);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.World.Seed, Is.EqualTo(321));
            Assert.That(restored.World.TreeStates, Is.Empty);
        }

        [Test]
        public void OrdinaryResourceMapVisibilityDefaultsToTrueAndCanBeDisabledForTrees()
        {
            GameObject rockObject = Track(new GameObject("rock"));
            ResourceNodeView rock = rockObject.AddComponent<ResourceNodeView>();
            rock.ConfigureDefault("rock");
            Assert.That(rock.ShowOnResourceMap, Is.True);
            Assert.That(rock.DirectInteractionEnabled, Is.True);

            GameObject treeObject = Track(new GameObject("tree"));
            ResourceNodeView tree = treeObject.AddComponent<ResourceNodeView>();
            tree.ConfigureDefault("dry_tree");
            tree.SetShowOnResourceMap(false);
            Assert.That(tree.ShowOnResourceMap, Is.False);
        }

        private VegetationSpeciesAsset CreateSpecies(string id, GameObject visual, VegetationCategory category, bool harvestable,
            string resourceKind, float fallDuration = 0f, int regrowthDays = 5)
        {
            var species = Track(ScriptableObject.CreateInstance<VegetationSpeciesAsset>());
            GameObject stumpVisual = Track(new GameObject(id + "_stump_visual"));
            float maxHealth = id == "tree_conifer_01" ? 120f : id == "tree_dead_01" ? 70f : 100f;
            species.Configure(id, id, visual, category, 1f, 1f, 3.8f, 0f, 90f, 0f, 1f, 0f, 1f,
                VegetationSpeciesAsset.CanonicalBiomeIds, true, harvestable, resourceKind, stumpVisual,
                VegetationCollisionMode.GameplayResource, 0.18f, 1.8f, 0.9f, maxHealth, regrowthDays, fallDuration);
            return species;
        }

        private (HarvestableTreeRuntime Tree, ResourceNodeView Node, GameObject Instance) SpawnTree(
            string speciesId, VegetationCategory category, string resourceKind, float fallDuration, int regrowthDays)
        {
            GameObject visual = Track(new GameObject(speciesId + "_visual"));
            visual.AddComponent<MeshRenderer>();
            VegetationSpeciesAsset species = CreateSpecies(speciesId, visual, category, true, resourceKind,
                fallDuration, regrowthDays);
            Transform root = Track(new GameObject("tree_test_generation")).transform;
            SpawnOne(species, root, Vector3.zero);
            GameObject instance = root.GetChild(0).GetChild(0).gameObject;
            return (instance.GetComponent<HarvestableTreeRuntime>(), instance.GetComponent<ResourceNodeView>(), instance);
        }

        private GameObject CreateActorWithAxe(string name)
        {
            GameObject actor = Track(new GameObject(name));
            PlayerInventoryRuntime inventory = actor.AddComponent<PlayerInventoryRuntime>();
            inventory.EnsureInitialized();
            inventory.Inventory.AddItem("axe", 1);
            return actor;
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
