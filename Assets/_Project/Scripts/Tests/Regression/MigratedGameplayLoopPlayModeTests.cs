using System;
using System.Collections;
using System.Linq;
using ApexShift.Core.Crafting;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Items;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Query;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    /// <summary>
    /// PlayMode migration gate for the Unity vertical slice.
    ///
    /// This is intentionally broader than unit tests: it catches regressions where the
    /// migrated runtime no longer behaves like a playable game loop.
    /// </summary>
    public sealed class MigratedGameplayLoopPlayModeTests
    {
        [UnityTest]
        public IEnumerator MigratedGameplayLoop_GenerateCollectCraftCreatureTickSaveLoad_Survives()
        {
            string slotName = $"migration_smoke_{Guid.NewGuid():N}";
            GameObject generatorObject = null;
            GameSaveService saveService = null;

            CleanupRuntimeObjects();
            ResourceRegistry.ClearForTests();
            ItemPickupRegistry.ClearForTests();

            try
            {
                // 1. Runtime migration scene bootstrap: create generator in an empty PlayMode scene.
                generatorObject = new GameObject("MigrationSmoke_WorldGenerator");
                WorldGeneratorRuntime generator = generatorObject.AddComponent<WorldGeneratorRuntime>();
                generator.SetGenerateOnStart(false);
                generator.SetSeed(47047);

                // 2. Generate world.
                generator.Generate();
                yield return null;
                yield return null;

                Assert.NotNull(generator.GetLastResult(), "WorldGeneratorRuntime did not produce a generation result.");
                Assert.GreaterOrEqual(generator.GetLastResult().BiomeCount, 0, "World generation returned an invalid biome count.");

                // 3. Spawn player.
                GameObject player = FindRuntimePlayer();
                Assert.NotNull(player, $"Generated world did not spawn a Player object. Active roots: {DescribeActiveRoots()}");
                Assert.IsTrue(player.activeInHierarchy, "Generated Player exists but is inactive.");

                PlayerInventoryRuntime inventory = player.GetComponent<PlayerInventoryRuntime>();
                PlayerCraftingRuntime crafting = player.GetComponent<PlayerCraftingRuntime>();
                PlayerSurvivalRuntime survival = player.GetComponent<PlayerSurvivalRuntime>();
                Assert.NotNull(inventory, "Player is missing PlayerInventoryRuntime.");
                Assert.NotNull(crafting, "Player is missing PlayerCraftingRuntime.");
                Assert.NotNull(survival, "Player is missing PlayerSurvivalRuntime.");
                inventory.EnsureInitialized();
                Assert.NotNull(inventory.Inventory, "Player inventory did not initialize.");

                // 4. Spawn resource in a deterministic location.
                ResourceNodeView resource = SpawnSmokeResourceNear(player.transform.position);
                yield return null;

                Assert.That(ResourceRegistry.Resources, Does.Contain(resource), "Spawned resource did not register in ResourceRegistry.");

                // 5. Collect resource.
                int woodBefore = inventory.Inventory.GetAmount("wood");
                bool collected = resource.Interact(player);
                Assert.IsTrue(collected, "Resource interaction failed; player could not collect the smoke resource.");
                Assert.Greater(
                    inventory.Inventory.GetAmount("wood"),
                    woodBefore,
                    "Collecting the smoke resource did not add wood to inventory.");

                // 6-7. Craft with the real runtime crafting path.
                // The smoke resource proves resource->inventory flow. Add remaining ingredients
                // directly so this test is not coupled to exact procedural resource placement.
                inventory.Inventory.AddItem("wood", 2);
                inventory.Inventory.AddItem("stone", 2);
                inventory.Inventory.AddItem("fiber", 1);

                CraftingResult craftResult = crafting.CraftRecipe("spear");
                Assert.NotNull(craftResult, "CraftRecipe returned null.");
                Assert.IsTrue(craftResult.Succeeded, $"Crafting spear failed. Status={craftResult.Status}");
                Assert.GreaterOrEqual(inventory.Inventory.GetAmount("spear"), 1, "Crafting succeeded but spear is not present in inventory.");

                // 8. Ensure/spawn creature runtime.
                EcosystemRuntime ecosystem = EcosystemRuntime.Instance;
                Assert.NotNull(ecosystem, "EcosystemRuntime was not created by world generation.");

                CreatureAgentView creature = EnsureCreatureRuntime(ecosystem, player.transform.position + Vector3.right * 6f);
                Assert.NotNull(creature, "Could not create or find a creature runtime.");
                Assert.That(ecosystem.Creatures, Does.Contain(creature), "Creature runtime is not registered in EcosystemRuntime.");

                // 9. Ecosystem tick.
                EcosystemDirectorRuntime director = EcosystemDirectorRuntime.Active;
                Assert.NotNull(director, "EcosystemDirectorRuntime was not created.");
                Assert.IsTrue(director.Initialized, "EcosystemDirectorRuntime exists but was not initialized.");
                int biomeStateCountBeforeTick = director.BiomeStates.Count;
                director.TickDay(1);
                yield return null;
                Assert.Greater(director.BiomeStates.Count, 0, "Ecosystem tick left no biome states.");
                Assert.AreEqual(
                    biomeStateCountBeforeTick,
                    director.BiomeStates.Count,
                    "Ecosystem tick unexpectedly changed biome state count.");

                // 10. Save.
                GameObject saveObject = new GameObject("MigrationSmoke_SaveService");
                saveService = saveObject.AddComponent<GameSaveService>();
                saveService.ResolveReferences();
                saveService.SaveGame(slotName);
                yield return null;

                // Mutate inventory before loading so the load assertion proves restore happened.
                inventory.Inventory.RemoveItem("spear", 1);
                Assert.AreEqual(0, inventory.Inventory.GetAmount("spear"), "Test setup failed: spear should have been removed before load.");

                // 11. Load.
                bool loaded = saveService.LoadGame(slotName);
                yield return null;
                Assert.IsTrue(loaded, "GameSaveService.LoadGame returned false.");

                // WorldGeneratorRuntime.Clear() uses Destroy() in PlayMode, so old Player
                // objects can be destroyed at end-of-frame while a new loaded Player is
                // spawned in the same LoadGame call. Do not rely on GameObject.Find("Player")
                // at one exact frame; wait for the runtime player component graph instead.
                GameObject loadedPlayer = null;
                yield return WaitForCondition(
                    () => (loadedPlayer = FindRuntimePlayer()) != null,
                    1.5f,
                    () => $"After load, Player object is missing. Active roots: {DescribeActiveRoots()}");

                // 12. Verify runtime still exists after load.
                Assert.NotNull(loadedPlayer, "After load, Player object is missing.");
                Transform loadedPlayerTransform = loadedPlayer.transform;
                Assert.IsTrue(loadedPlayerTransform.gameObject.activeInHierarchy, "After load, Player object is inactive.");
                Assert.AreEqual("Player", loadedPlayer.name, "After load, resolved player runtime exists but is not named Player.");

                PlayerInventoryRuntime loadedInventory = loadedPlayerTransform.GetComponent<PlayerInventoryRuntime>();
                Assert.NotNull(loadedInventory, "After load, PlayerInventoryRuntime is missing.");
                loadedInventory.EnsureInitialized();
                Assert.GreaterOrEqual(loadedInventory.Inventory.GetAmount("spear"), 1, "After load, crafted spear was not restored.");

                Assert.NotNull(EcosystemRuntime.Instance, "After load, EcosystemRuntime is missing.");
                Assert.NotNull(EcosystemDirectorRuntime.Active, "After load, EcosystemDirectorRuntime is missing.");
                Assert.IsTrue(EcosystemDirectorRuntime.Active.Initialized, "After load, EcosystemDirectorRuntime is not initialized.");

                Assert.GreaterOrEqual(ResourceRegistry.ResourceCount, 1, "After load, ResourceRegistry has no resources.");
                Assert.GreaterOrEqual(
                    EcosystemRuntime.Instance.CreatureCount,
                    1,
                    "After load, no creature runtime is registered.");
                Assert.NotNull(
                    FindRuntimePlayer(),
                    "After load, player lookup fallback still failed even though the runtime regenerated successfully.");
            }
            finally
            {
                if (saveService != null)
                {
                    saveService.DeleteGame(slotName);
                }

                CleanupRuntimeObjects();
                ResourceRegistry.ClearForTests();
                ItemPickupRegistry.ClearForTests();
            }
        }

        private static ResourceNodeView SpawnSmokeResourceNear(Vector3 playerPosition)
        {
            GameObject resourceObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            resourceObject.name = "MigrationSmoke_Resource_small_tree";
            resourceObject.transform.position = playerPosition + Vector3.forward * 2f + Vector3.up * 0.1f;
            resourceObject.transform.localScale = new Vector3(0.8f, 1.6f, 0.8f);

            ResourceNodeView resource = resourceObject.AddComponent<ResourceNodeView>();
            resource.ConfigureDefault("small_tree");
            return resource;
        }

        private static GameObject FindRuntimePlayer()
        {
            PlayerInventoryRuntime inventory = UnityEngine.Object
                .FindObjectsByType<PlayerInventoryRuntime>(FindObjectsInactive.Include)
                .FirstOrDefault(runtime => runtime != null && runtime.gameObject != null);
            if (inventory != null)
            {
                return inventory.gameObject;
            }

            IsometricPlayerController controller = UnityEngine.Object
                .FindObjectsByType<IsometricPlayerController>(FindObjectsInactive.Include)
                .FirstOrDefault(runtime => runtime != null && runtime.gameObject != null);
            if (controller != null)
            {
                return controller.gameObject;
            }

            PlayerPresenceRuntime presence = UnityEngine.Object
                .FindObjectsByType<PlayerPresenceRuntime>(FindObjectsInactive.Include)
                .FirstOrDefault(runtime => runtime != null && runtime.gameObject != null);
            if (presence != null)
            {
                return presence.gameObject;
            }

            GameObject byName = GameObject.Find("Player");
            if (byName != null)
            {
                return byName;
            }

            GameObject byTag = GameObject.FindGameObjectWithTag("Player");
            if (byTag != null)
            {
                return byTag;
            }

            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            foreach (GameObject go in allObjects)
            {
                if (go != null && go.name == "Player")
                {
                    return go;
                }
            }

            return null;
        }

        private static IEnumerator WaitForCondition(Func<bool> predicate, float timeoutSeconds, Func<string> failureMessage)
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.01f, timeoutSeconds);
            while (Time.realtimeSinceStartup < deadline)
            {
                if (predicate())
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(failureMessage != null ? failureMessage() : "Condition was not met before timeout.");
        }

        private static string DescribeActiveRoots()
        {
            return string.Join(", ", UnityEngine.Object
                .FindObjectsByType<GameObject>(FindObjectsInactive.Exclude)
                .Where(go => go != null && go.transform.parent == null)
                .Select(go => go.name)
                .OrderBy(name => name));
        }

        private static CreatureAgentView EnsureCreatureRuntime(EcosystemRuntime ecosystem, Vector3 position)
        {
            if (ecosystem == null)
            {
                return null;
            }

            CreatureAgentView existing = ecosystem.Creatures.FirstOrDefault(creature => creature != null && creature.isActiveAndEnabled);
            if (existing != null)
            {
                return existing;
            }

            GameObject creatureObject = new GameObject("MigrationSmoke_Creature_small_prey");
            creatureObject.transform.position = position;

            CreatureAgentView view = creatureObject.AddComponent<CreatureAgentView>();
            view.Configure("small_prey");

            CreatureNeedsRuntime needs = creatureObject.AddComponent<CreatureNeedsRuntime>();
            needs.Configure("small_prey");

            creatureObject.AddComponent<CreatureHealthRuntime>().Configure("small_prey");
            creatureObject.AddComponent<CreatureSimulationLodRuntime>();
            creatureObject.AddComponent<CreatureBehaviorBrain>();
            creatureObject.AddComponent<CreatureBehaviorRuntime>();
            creatureObject.AddComponent<WorldQueryRuntime>();

            ecosystem.RegisterCreature(view);
            return view;
        }

        private static void CleanupRuntimeObjects()
        {
            string[] exactNames =
            {
                "MigrationSmoke_WorldGenerator",
                "MigrationSmoke_SaveService",
                "MigrationSmoke_Resource_small_tree",
                "MigrationSmoke_Creature_small_prey",
                "TerrainRoot",
                "BiomeRoot",
                "ResourceRoot",
                "CreatureRoot",
                "BuildingRoot",
                "GameBootstrapper",
                "EcosystemRuntime",
                "DayNightRuntime",
                "DayNightSkyRuntime",
                "WorldMapDebugWindow",
                "GameSnapshotProvider",
                "DebugPanelPresenter",
                "Player",
                "Main Camera",
                "PlayerFollowCamera",
                "Directional Light",
                "WorldBounds",
                "ActionBarUI",
                "CreatureIslandBoundsRuntime",
                "AmbientMusicRuntime",
                "AmbientSoundController",
                "IslandTopographyRuntime",
                "IslandTerrainMesh",
                "WaterSurfaceMesh",
                "SeabedMesh",
                "CliffWallsMesh"
            };

            GameObject[] objects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            for (int i = objects.Length - 1; i >= 0; i--)
            {
                GameObject go = objects[i];
                if (go == null)
                {
                    continue;
                }

                if (exactNames.Contains(go.name) || go.name.StartsWith("MigrationSmoke_", StringComparison.Ordinal))
                {
                    UnityEngine.Object.Destroy(go);
                }
            }
        }
    }
}
