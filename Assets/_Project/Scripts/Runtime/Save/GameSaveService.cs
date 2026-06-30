using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Ecosystem;
using ApexShift.Core.Inventory;
using ApexShift.Core.Save;
using ApexShift.Infrastructure.Save;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Items;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.Camera;
using ApexShift.Runtime.World.Generation;
using UnityEngine;

namespace ApexShift.Runtime.Save
{
    [DisallowMultipleComponent]
    public sealed class GameSaveService : MonoBehaviour
    {
        [SerializeField] private WorldGeneratorRuntime worldGenerator;
        [SerializeField] private PlayerInventoryRuntime playerInventory;
        [SerializeField] private PlayerSurvivalRuntime playerSurvival;
        [SerializeField] private EcosystemDirectorRuntime ecosystemDirector;
        [SerializeField] private DayNightRuntime dayNightRuntime;
        [SerializeField] private BuildingRegistry buildingRegistry;

        private IGameSaveStore saveStore;

        private IGameSaveStore GetSaveStore()
        {
            if (saveStore == null)
            {
                saveStore = new JsonFileGameSaveStore();
            }

            return saveStore;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        public void ResolveReferences()
        {
            if (worldGenerator == null)
            {
                worldGenerator = FindAnyObjectByType<WorldGeneratorRuntime>();
            }

            if (playerInventory == null || playerSurvival == null)
            {
                IsometricPlayerController controller = FindAnyObjectByType<IsometricPlayerController>();
                if (controller != null)
                {
                    if (playerInventory == null)
                    {
                        playerInventory = controller.GetComponent<PlayerInventoryRuntime>();
                    }

                    if (playerSurvival == null)
                    {
                        playerSurvival = controller.GetComponent<PlayerSurvivalRuntime>();
                    }
                }
            }

            if (playerInventory == null)
            {
                playerInventory = FindAnyObjectByType<PlayerInventoryRuntime>();
            }

            if (playerSurvival == null)
            {
                playerSurvival = FindAnyObjectByType<PlayerSurvivalRuntime>();
            }

            if (ecosystemDirector == null)
            {
                ecosystemDirector = FindAnyObjectByType<EcosystemDirectorRuntime>();
            }

            if (dayNightRuntime == null)
            {
                dayNightRuntime = FindAnyObjectByType<DayNightRuntime>();
            }

            if (buildingRegistry == null)
            {
                buildingRegistry = FindAnyObjectByType<BuildingRegistry>();
            }
        }

        public GameSaveData CaptureCurrentState()
        {
            ResolveReferences();

            InventorySaveData inventory = playerInventory != null ? playerInventory.ToSaveData() : InventorySaveData.Empty;
            SurvivalSaveData survival = playerSurvival != null ? playerSurvival.ToSaveData() : SurvivalSaveData.Default;

            if (playerSurvival != null)
            {
                Vector3 pos = playerSurvival.transform.position;
                survival.SetPosition(pos.x, pos.y, pos.z);
            }

            int seed = worldGenerator != null ? worldGenerator.Seed : 0;

            List<ResourceSaveData> resources = new List<ResourceSaveData>();
            foreach (ResourceNodeView node in FindObjectsByType<ResourceNodeView>(FindObjectsInactive.Include))
            {
                if (node == null)
                {
                    continue;
                }

                Vector3 p = node.transform.position;
                var s = node.State;
                resources.Add(new ResourceSaveData(
                    s.ResourceId,
                    node.gameObject.name,
                    p.x,
                    p.y,
                    p.z,
                    s.Amount,
                    s.MaxAmount,
                    s.IsDepleted,
                    s.GrowthProgress,
                    s.RegrowthDays,
                    s.EdibleByHerbivores,
                    s.FoodValue,
                    s.RenderOnly,
                    s.PondVegetation,
                    s.IsDrop,
                    s.PickupPriority));
            }

            CaptureDynamicMeatDrops(resources);
            List<PickupSaveData> pickups = CapturePickupStates();
            List<BiomeEcosystemSaveData> biomeStates = CaptureBiomeStates();
            List<CreatureSaveData> creatureStates = CaptureCreatureStates();
            List<BuildingSaveData> buildingStates = CaptureBuildingStates();
            int day = dayNightRuntime != null ? dayNightRuntime.Day : 1;
            float timeOfDay = dayNightRuntime != null ? dayNightRuntime.TimeOfDay01 : 0f;

            WorldSaveData world = new WorldSaveData(
                seed,
                day,
                timeOfDay,
                resources,
                pickups,
                biomeStates,
                creatureStates,
                buildingStates,
                ecosystemDirector != null ? ecosystemDirector.TickTimer : 0f,
                ecosystemDirector != null ? ecosystemDirector.EcosystemStateSource : "generated");

            return new GameSaveData(inventory, survival, world);
        }

        public void SaveGame(string slotName)
        {
            Debug.Log($"[Save] SaveGame requested for slot: {slotName}");
            GameSaveData state = CaptureCurrentState();
            IGameSaveStore store = GetSaveStore();
            store.Save(slotName, state);
            Debug.Log($"[Save] Game saved successfully. Path: {(store is JsonFileGameSaveStore jss ? jss.GetPath(slotName) : slotName)}");
        }

        public bool LoadGame(string slotName)
        {
            Debug.Log($"[Save] LoadGame requested for slot: {slotName}");
            ResolveReferences();

            IGameSaveStore store = GetSaveStore();
            if (!store.Exists(slotName))
            {
                Debug.LogWarning($"[Save] Load failed: Slot '{slotName}' does not exist.");
                return false;
            }

            GameSaveData saveData;
            try
            {
                saveData = store.Load(slotName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Save] Load failed: Exception while reading slot '{slotName}': {ex.Message}");
                return false;
            }

            return ApplyLoadedState(saveData, slotName);
        }

        public bool ApplyLoadedState(GameSaveData saveData, string slotName = "runtime")
        {
            if (saveData == null)
            {
                Debug.LogError($"[Save] Load failed: Could not deserialize save data for slot '{slotName}'.");
                return false;
            }

            saveData.EnsureDefaults();

            if (worldGenerator != null)
            {
                worldGenerator.SetSeed(saveData.World.Seed);
                worldGenerator.Generate();
                ResolveReferences();
            }

            RestoreResourceStates(saveData.World.Resources);
            RestorePickupStates(saveData.World.Pickups);
            ApplyBiomeStates(saveData.World.BiomeStates);
            ApplyEcosystemMetadata(saveData.World);
            RestoreCreatureStates(saveData.World.CreatureStates);
            RestoreBuildingStates(saveData.World.BuildingStates);
            if (dayNightRuntime != null)
            {
                dayNightRuntime.LoadFromWorldSaveData(saveData.World.Day, saveData.World.TimeOfDay);
            }

            if (playerInventory != null)
            {
                playerInventory.LoadFromSaveData(saveData.Inventory);
            }

            if (playerSurvival != null)
            {
                playerSurvival.LoadFromSaveData(saveData.Survival);
                if (saveData.Survival.hasPosition)
                {
                    Vector3 targetPos = new Vector3(saveData.Survival.posX, saveData.Survival.posY, saveData.Survival.posZ);
                    ApplyPlayerPosition(targetPos);
                }
            }

            Debug.Log("[Save] Load complete.");
            return true;
        }

        private void ApplyPlayerPosition(Vector3 targetPos)
        {
            if (playerSurvival == null)
            {
                return;
            }

            CharacterController cc = playerSurvival.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            playerSurvival.transform.SetPositionAndRotation(targetPos, playerSurvival.transform.rotation);
            Physics.SyncTransforms();

            if (cc != null)
            {
                cc.enabled = true;
            }

            IsometricCameraFollow cameraFollow = FindAnyObjectByType<IsometricCameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(playerSurvival.transform);
                cameraFollow.SnapToTarget();
            }
        }

        public void DeleteGame(string slotName)
        {
            GetSaveStore().Delete(slotName);
        }

        private List<BiomeEcosystemSaveData> CaptureBiomeStates()
        {
            return ecosystemDirector != null ? ecosystemDirector.CaptureSaveData() : new List<BiomeEcosystemSaveData>();
        }

        private List<CreatureSaveData> CaptureCreatureStates()
        {
            List<CreatureSaveData> creatures = new List<CreatureSaveData>();
            foreach (CreatureAgentView agent in FindObjectsByType<CreatureAgentView>(FindObjectsInactive.Include))
            {
                if (agent == null)
                {
                    continue;
                }

                CreatureHealthRuntime health = agent.GetComponent<CreatureHealthRuntime>();
                CreatureNeedsRuntime needs = agent.GetComponent<CreatureNeedsRuntime>();
                CreatureBehaviorBrain brain = agent.GetComponent<CreatureBehaviorBrain>();
                Vector3 p = agent.transform.position;
                string creatureId = CreatureSaveData.NormalizeCreatureId(agent.CreatureId);
                CreatureBehaviorState state = brain != null ? brain.State : CreatureBehaviorState.Wander;

                creatures.Add(new CreatureSaveData(
                    creatureId,
                    creatureId,
                    1,
                    p.x,
                    p.y,
                    p.z,
                    health != null ? health.CurrentHealth : 1f,
                    health != null ? health.MaxHealth : 1f,
                    (health != null && health.IsDead) || state == CreatureBehaviorState.Dead || !agent.gameObject.activeInHierarchy,
                    needs != null ? needs.State.Hunger : 0f,
                    needs != null ? needs.State.Energy : 1f,
                    state.ToString(),
                    brain != null ? brain.CurrentBiomeId : "default",
                    brain != null ? brain.HomeBiomeId : "default",
                    brain != null ? brain.PopulationBiomeId : "default",
                    brain != null ? brain.DecisionReason : "save_capture",
                    brain != null ? brain.LastFoodSource : "none",
                    brain != null ? brain.AttackCooldown : 0f,
                    brain != null ? brain.CurrentNiche : "HERBIVORE",
                    brain != null ? brain.HuntDrive : 0f));
            }

            return creatures;
        }

        private List<BuildingSaveData> CaptureBuildingStates()
        {
            return buildingRegistry != null ? buildingRegistry.CaptureSaveData() : new List<BuildingSaveData>();
        }

        private void RestoreBuildingStates(IReadOnlyList<BuildingSaveData> buildingStates)
        {
            if (buildingRegistry == null)
            {
                return;
            }

            buildingRegistry.RestoreFromSaveData(buildingStates, worldGenerator != null ? worldGenerator.transform : null);
        }

        private void RestoreCreatureStates(IReadOnlyList<CreatureSaveData> savedCreatures)
        {
            if (savedCreatures == null || savedCreatures.Count == 0)
            {
                return;
            }

            CreatureAgentView[] liveCreatures = FindObjectsByType<CreatureAgentView>(FindObjectsInactive.Include);
            HashSet<CreatureAgentView> used = new HashSet<CreatureAgentView>();

            foreach (CreatureSaveData saved in savedCreatures.Where(item => item != null))
            {
                CreatureAgentView agent = FindBestCreatureMatch(saved, liveCreatures, used);
                if (agent == null)
                {
                    continue;
                }

                used.Add(agent);
                RestoreCreatureState(agent, saved);
            }
        }

        private static CreatureAgentView FindBestCreatureMatch(CreatureSaveData saved, IReadOnlyList<CreatureAgentView> candidates, ISet<CreatureAgentView> used)
        {
            string expectedId = CreatureSaveData.NormalizeCreatureId(saved.CreatureId);
            CreatureAgentView best = null;
            float bestDistance = float.PositiveInfinity;
            Vector3 savedPosition = new Vector3(saved.x, saved.y, saved.z);

            foreach (CreatureAgentView candidate in candidates)
            {
                if (candidate == null || used.Contains(candidate))
                {
                    continue;
                }

                if (CreatureSaveData.NormalizeCreatureId(candidate.CreatureId) != expectedId)
                {
                    continue;
                }

                float distance = (candidate.transform.position - savedPosition).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private static void RestoreCreatureState(CreatureAgentView agent, CreatureSaveData saved)
        {
            CharacterController controller = agent.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            agent.transform.position = new Vector3(saved.x, saved.y, saved.z);

            if (controller != null)
            {
                controller.enabled = true;
            }

            CreatureNeedsRuntime needs = agent.GetComponent<CreatureNeedsRuntime>();
            if (needs != null)
            {
                needs.RestoreNeeds(saved.Hunger, saved.Energy);
            }

            CreatureBehaviorBrain brain = agent.GetComponent<CreatureBehaviorBrain>();
            if (brain != null)
            {
                brain.RestoreSaveState(
                    saved.BehaviorState,
                    saved.DecisionReason,
                    saved.LastFoodSource,
                    saved.CurrentBiomeId,
                    saved.HomeBiomeId,
                    saved.PopulationBiomeId,
                    saved.AttackCooldown,
                    saved.CurrentNiche,
                    saved.HuntDrive);
            }

            CreatureHealthRuntime health = agent.GetComponent<CreatureHealthRuntime>();
            if (health != null)
            {
                health.RestoreHealth(saved.MaxHealth, saved.Health, saved.Dead);
            }

            agent.gameObject.SetActive(!saved.Dead);
        }

        private static void CaptureDynamicMeatDrops(List<ResourceSaveData> resources)
        {
            foreach (FoodSourceView food in FindObjectsByType<FoodSourceView>(FindObjectsInactive.Include))
            {
                if (food == null || !LooksLikeDynamicMeatDrop(food.gameObject))
                {
                    continue;
                }

                Vector3 p = food.transform.position;
                int amount = Mathf.Max(0, Mathf.RoundToInt(food.Biomass));
                resources.Add(new ResourceSaveData(
                    string.IsNullOrWhiteSpace(food.SourceId) ? "meat_drop" : food.SourceId,
                    food.gameObject.name,
                    p.x,
                    p.y,
                    p.z,
                    amount,
                    Mathf.Max(1, amount),
                    food.IsEmpty,
                    0f,
                    0,
                    false,
                    10f,
                    false,
                    false,
                    true,
                    100));
            }
        }

        private static bool LooksLikeDynamicMeatDrop(GameObject go)
        {
            return go != null && go.name.StartsWith("MeatDrop_", StringComparison.OrdinalIgnoreCase);
        }

        private static void ClearExistingDynamicMeatDrops()
        {
            foreach (GameObject go in FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (LooksLikeDynamicMeatDrop(go))
                {
                    DestroyImmediate(go);
                }
            }
        }

        private static bool IsDynamicMeatDrop(ResourceSaveData data)
        {
            if (data == null)
            {
                return false;
            }

            return data.IsDrop
                   || (!string.IsNullOrWhiteSpace(data.ResourceType) && data.ResourceType.StartsWith("MeatDrop_", StringComparison.OrdinalIgnoreCase))
                   || (!string.IsNullOrWhiteSpace(data.ResourceId) && data.ResourceId.StartsWith("meat_", StringComparison.OrdinalIgnoreCase));
        }

        private static void RestoreDynamicMeatDrop(ResourceSaveData data)
        {
            if (data == null || data.Depleted || data.Amount <= 0)
            {
                return;
            }

            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drop.name = string.IsNullOrWhiteSpace(data.ResourceType) ? "MeatDrop_restored" : data.ResourceType;
            drop.transform.position = new Vector3(data.x, data.y, data.z);
            drop.transform.localScale = new Vector3(0.45f, 0.2f, 0.45f);
            FoodSourceView food = drop.GetComponent<FoodSourceView>() ?? drop.AddComponent<FoodSourceView>();
            food.Configure(string.IsNullOrWhiteSpace(data.ResourceId) ? "meat_drop" : data.ResourceId, "Meat", FoodKind.Meat, Mathf.Max(0.01f, data.Amount), Mathf.Max(0.01f, data.FoodValue));
        }

        private List<PickupSaveData> CapturePickupStates()
        {
            List<PickupSaveData> pickups = new List<PickupSaveData>();
            foreach (ItemPickupView pickup in FindObjectsByType<ItemPickupView>(FindObjectsInactive.Include))
            {
                if (pickup == null || string.IsNullOrWhiteSpace(pickup.ItemId) || pickup.Amount <= 0)
                {
                    continue;
                }

                Transform t = pickup.transform;
                Quaternion rot = t != null ? t.rotation : Quaternion.identity;
                Vector3 pos = t != null ? t.position : Vector3.zero;
                pickups.Add(new PickupSaveData(pickup.ItemId, pickup.Amount, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w));
            }

            return pickups;
        }

        private void RestorePickupStates(IReadOnlyList<PickupSaveData> savedPickups)
        {
            if (savedPickups == null || savedPickups.Count == 0)
            {
                return;
            }

            foreach (PickupSaveData pickup in savedPickups)
            {
                if (pickup == null || string.IsNullOrWhiteSpace(pickup.ItemId) || pickup.Amount <= 0)
                {
                    continue;
                }

                Vector3 pos = new Vector3(pickup.X, pickup.Y, pickup.Z);
                Quaternion rot = new Quaternion(pickup.RotX, pickup.RotY, pickup.RotZ, pickup.RotW);
                ItemPickupSpawner.Spawn(pickup.ItemId, pickup.Amount, pos, rot);
            }
        }

        private void RestoreResourceStates(IReadOnlyList<ResourceSaveData> savedResources)
        {
            if (savedResources == null || savedResources.Count == 0)
            {
                return;
            }

            ClearExistingDynamicMeatDrops();
            ResourceNodeView[] nodes = FindObjectsByType<ResourceNodeView>(FindObjectsInactive.Include);
            Dictionary<Vector3Int, ResourceNodeView> lookup = new Dictionary<Vector3Int, ResourceNodeView>();
            foreach (ResourceNodeView node in nodes)
            {
                Vector3 p = node.transform.position;
                Vector3Int key = new Vector3Int(Mathf.RoundToInt(p.x * 100), Mathf.RoundToInt(p.y * 100), Mathf.RoundToInt(p.z * 100));
                if (!lookup.ContainsKey(key))
                {
                    lookup[key] = node;
                }
            }

            int restoredCount = 0;
            foreach (ResourceSaveData resData in savedResources)
            {
                if (IsDynamicMeatDrop(resData))
                {
                    RestoreDynamicMeatDrop(resData);
                    continue;
                }

                Vector3Int key = new Vector3Int(Mathf.RoundToInt(resData.x * 100), Mathf.RoundToInt(resData.y * 100), Mathf.RoundToInt(resData.z * 100));
                if (lookup.TryGetValue(key, out ResourceNodeView node))
                {
                    node.LoadState(resData.Amount, resData.Depleted, resData.GrowthProgress);
                    restoredCount++;
                }
            }
        }

        private void ApplyBiomeStates(IReadOnlyList<BiomeEcosystemSaveData> biomeStates)
        {
            if (ecosystemDirector == null || biomeStates == null || biomeStates.Count == 0)
            {
                return;
            }

            ecosystemDirector.LoadSaveData(biomeStates);
        }

        private void ApplyEcosystemMetadata(WorldSaveData world)
        {
            if (ecosystemDirector == null || world == null)
            {
                return;
            }

            ecosystemDirector.RestoreRuntimeMetadata(world.EcosystemTickTimer, world.EcosystemStateSource);
        }
    }
}
