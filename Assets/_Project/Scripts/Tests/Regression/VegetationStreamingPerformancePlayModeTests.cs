using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Items;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Vegetation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class VegetationStreamingPerformancePlayModeTests
    {
        private readonly List<GameObject> ownedObjects = new List<GameObject>();
        private readonly List<Object> ownedAssets = new List<Object>();
        private VegetationRuntimeController controller;
        private Transform fakePlayer;
        private VegetationGenerationSettings settings;
        private string treeId;

        [UnityTest]
        public IEnumerator DenseForest_StreamsWithinBudgetsPoolsDecorationsAndPreservesCutTree()
        {
            GameObject forestRoot = Own(new GameObject("VegetationPerf_Root"));
            controller = forestRoot.AddComponent<VegetationRuntimeController>();
            fakePlayer = Own(new GameObject("VegetationPerf_Player")).transform;
            GameObject decorativeVisual = Own(new GameObject("VegetationPerf_DecorativePrefab"));
            GameObject treeVisual = Own(GameObject.CreatePrimitive(PrimitiveType.Cube));
            treeVisual.name = "VegetationPerf_TreePrefab";
            GameObject stumpVisual = Own(new GameObject("VegetationPerf_StumpPrefab"));
            Renderer treeRenderer = treeVisual.GetComponent<MeshRenderer>();
            treeVisual.AddComponent<LODGroup>().SetLODs(new[] { new LOD(0.01f, new[] { treeRenderer }) });
            VegetationSpeciesAsset groundSpecies = CreateSpecies("perf_groundcover", decorativeVisual,
                VegetationCategory.GroundCover, harvestable: false, string.Empty);
            VegetationSpeciesAsset treeSpecies = CreateSpecies("perf_harvestable_tree", treeVisual,
                VegetationCategory.Tree, harvestable: true, "leafy_tree", stumpVisual);

            settings = new VegetationGenerationSettings();
            settings.ConfigurePlacement(24f, 0.88f, true);
            settings.ConfigureStreaming(true, 0.05f, 32f, 24f, 30f, 16f, 4f,
                24, 24, 32, 4, 8, 4, 64);

            var placements = new List<VegetationPlacement>(650);
            for (int chunkZ = 0; chunkZ < 9; chunkZ++)
            for (int chunkX = 0; chunkX < 9; chunkX++)
            for (int i = 0; i < 8; i++)
            {
                Vector3 position = new Vector3(chunkX * 24f + 4f + (i % 4) * 4f, 0f,
                    chunkZ * 24f + 4f + (i / 4) * 8f);
                placements.Add(new VegetationPlacement($"perf_ground_{chunkX}_{chunkZ}_{i}", "westwood",
                    groundSpecies, position, i * 13f, 0.8f + i * 0.02f, chunkX, chunkZ));
            }

            treeId = "perf_cut_tree_stable_id";
            placements.Add(new VegetationPlacement(treeId, "westwood", treeSpecies,
                new Vector3(12f, 0f, 12f), 37f, 1f, 0, 0));
            fakePlayer.position = new Vector3(12f, 0f, 12f);
            controller.Initialize(placements, settings, fakePlayer.position);
            controller.SetTarget(fakePlayer);

            for (int i = 0; i < 6; i++) yield return null;

            HarvestableTreeRuntime tree = null;
            Assert.That(HarvestableTreeRegistry.TryGet(treeId, out tree), Is.True,
                "Harvestable instances must remain registered while streamed out.");
            var actor = Own(new GameObject("VegetationPerf_AxeOwner")).AddComponent<PlayerInventoryRuntime>();
            actor.EnsureInitialized();
            actor.Inventory.AddItem("axe", 1);
            Assert.That(tree.TryAxeHit(new AxeHitContext(actor.gameObject, tree.MaxHealth, Vector3.zero, Vector3.forward)), Is.True);
            Assert.That(tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Falling));
            fakePlayer.position = new Vector3(204f, 0f, 204f);
            controller.SetTarget(fakePlayer);

            // Streaming away while Falling must synchronously settle the tree and preserve its yield state.
            for (int i = 0; i < 16; i++)
            {
                yield return null;
                AssertBudgets();
            }
            VegetationChunkRuntime homeChunk = controller.Chunks.First(chunk => chunk.ChunkX == 0 && chunk.ChunkZ == 0);
            Assert.That(homeChunk.transform.Find("GroundCover").gameObject.activeSelf, Is.False,
                "The distant home chunk's category root should be streamed out.");
            Assert.That(tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted));

            var frameMilliseconds = new List<float>(120);
            float peakStreamingRefreshMs = controller.Stats.StreamingRefreshMs;
            Vector3[] route =
            {
                new Vector3(12f, 0f, 12f),
                new Vector3(108f, 0f, 108f),
                new Vector3(204f, 0f, 204f),
                new Vector3(12f, 0f, 12f)
            };
            for (int i = 0; i < 120; i++)
            {
                if (i % 30 == 0)
                {
                    fakePlayer.position = route[(i / 30) % route.Length];
                    controller.SetTarget(fakePlayer);
                }
                yield return null;
                float frameMs = Time.unscaledDeltaTime * 1000f;
                if (frameMs > 0f) frameMilliseconds.Add(frameMs);
                peakStreamingRefreshMs = Mathf.Max(peakStreamingRefreshMs, controller.Stats.StreamingRefreshMs);
                AssertBudgets();
            }

            fakePlayer.position = new Vector3(12f, 0f, 12f);
            controller.SetTarget(fakePlayer);
            for (int i = 0; i < 20; i++)
            {
                yield return null;
                AssertBudgets();
            }

            Assert.That(tree.LifecycleState, Is.EqualTo(TreeLifecycleState.Depleted),
                "A falling tree must settle safely when its chunk streams out.");
            Assert.That(tree.DropsSpawned, Is.True);
            Assert.That(tree.gameObject.activeInHierarchy, Is.True, "The same tree instance should return with its original state.");
            Assert.That(controller.Stats.TotalPlacements, Is.EqualTo(649));
            Assert.That(controller.Stats.TotalChunks, Is.EqualTo(81));
            Assert.That(controller.Stats.PooledInstances, Is.GreaterThan(0), "Streamed-out decorative vegetation should enter the pool.");
            Assert.That(controller.Stats.PoolHits, Is.GreaterThan(0), "Returning to visited chunks should reuse pooled instances.");

            VegetationInstanceRuntime[] instances = forestRoot.GetComponentsInChildren<VegetationInstanceRuntime>(true);
            VegetationInstanceRuntime restoredMetadata = instances.FirstOrDefault(instance => instance.InstanceId == "perf_ground_0_0_0");
            Assert.That(restoredMetadata, Is.Not.Null, "The stable placement should be represented after reacquisition.");
            Assert.That(restoredMetadata.BiomeId, Is.EqualTo("westwood"));
            Assert.That(restoredMetadata.ChunkX, Is.EqualTo(0));
            Assert.That(restoredMetadata.ChunkZ, Is.EqualTo(0));
            Assert.That(restoredMetadata.transform.position, Is.EqualTo(new Vector3(4f, 0f, 4f)));
            Assert.That(HarvestableTreeRegistry.TryGet(treeId, out HarvestableTreeRuntime sameTree), Is.True);
            Assert.That(sameTree, Is.SameAs(tree));

            frameMilliseconds.Sort();
            float averageFrameMs = frameMilliseconds.Count > 0 ? frameMilliseconds.Average() : 0f;
            float p95FrameMs = frameMilliseconds.Count > 0
                ? frameMilliseconds[Mathf.Clamp(Mathf.CeilToInt(frameMilliseconds.Count * 0.95f) - 1, 0, frameMilliseconds.Count - 1)]
                : 0f;
            float approximateAverageFps = averageFrameMs > 0f ? 1000f / averageFrameMs : 0f;
            Debug.Log($"[VegetationPerfBaseline] placements={controller.Stats.TotalPlacements} chunks={controller.Stats.TotalChunks} avgFrameMs={averageFrameMs:0.###} p95FrameMs={p95FrameMs:0.###} avgFps={approximateAverageFps:0.##} maxStreamingMs={peakStreamingRefreshMs:0.###}");

            // Removing the owning generation must unregister its stateful tree.
            Object.Destroy(forestRoot);
            yield return null;
            Assert.That(HarvestableTreeRegistry.TryGet(treeId, out _), Is.False,
                "Clear/destroy must not leave a stale tree registry entry.");
            controller = null;
            ownedObjects.Remove(forestRoot);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = ItemPickupRegistry.Pickups.Count - 1; i >= 0; i--)
                if (ItemPickupRegistry.Pickups[i] != null) Object.Destroy(ItemPickupRegistry.Pickups[i].gameObject);
            for (int i = ownedObjects.Count - 1; i >= 0; i--)
                if (ownedObjects[i] != null) Object.Destroy(ownedObjects[i]);
            for (int i = ownedAssets.Count - 1; i >= 0; i--)
                if (ownedAssets[i] != null) Object.Destroy(ownedAssets[i]);
            yield return null;
            ItemPickupRegistry.Cleanup();
            ResourceRegistry.ClearForTests();
            HarvestableTreeRegistry.ClearForTests();
        }

        private GameObject Own(GameObject instance)
        {
            ownedObjects.Add(instance);
            return instance;
        }

        private VegetationSpeciesAsset CreateSpecies(string id, GameObject prefab, VegetationCategory category,
            bool harvestable, string resourceKind, GameObject stump = null)
        {
            VegetationSpeciesAsset species = ScriptableObject.CreateInstance<VegetationSpeciesAsset>();
            species.Configure(id, id, prefab, category, 0.8f, 1.2f, 1f, 0f, 90f, 0f, 1f, 0f, 1f,
                new[] { "westwood" }, true, harvestable, resourceKind, stump,
                harvestable ? VegetationCollisionMode.GameplayResource : VegetationCollisionMode.None,
                maxTreeHealth: 25f, regrowthDays: 5, fallDuration: 0.05f);
            ownedAssets.Add(species);
            return species;
        }

        private void AssertBudgets()
        {
            VegetationRuntimeStats stats = controller.Stats;
            Assert.That(stats.VisibleTrees, Is.LessThanOrEqualTo(settings.MaxVisibleTrees));
            Assert.That(stats.VisibleShrubs, Is.LessThanOrEqualTo(settings.MaxVisibleShrubs));
            Assert.That(stats.VisibleGroundCover, Is.LessThanOrEqualTo(settings.MaxVisibleGroundCover));
            Assert.That(stats.ActiveHarvestableTrees, Is.LessThanOrEqualTo(settings.MaxActiveHarvestableTrees));
            Assert.That(stats.ActiveTreeColliders, Is.LessThanOrEqualTo(settings.MaxActiveTreeColliders));
        }
    }
}
