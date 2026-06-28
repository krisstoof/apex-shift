using System.Collections.Generic;
using ApexShift.Core.Inventory;
using ApexShift.Core.Save;
using ApexShift.Infrastructure.Save;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Player;
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
            
            // Capture Resources
            List<ResourceSaveData> resources = new List<ResourceSaveData>();
            foreach (var node in FindObjectsByType<ApexShift.Runtime.Resources.ResourceNodeView>())
            {
                Vector3 p = node.transform.position;
                var s = node.State;
                resources.Add(new ResourceSaveData(
                    s.ResourceId,
                    node.gameObject.name,
                    p.x, p.y, p.z,
                    s.Amount, s.MaxAmount, s.IsDepleted,
                    s.GrowthProgress,
                    s.RegrowthDays,
                    s.EdibleByHerbivores,
                    s.FoodValue,
                    s.RenderOnly,
                    s.PondVegetation,
                    s.IsDrop,
                    s.PickupPriority));
            }
            Debug.Log($"[Save] Captured {resources.Count} resource nodes.");

            List<BiomeEcosystemSaveData> biomeStates = CaptureBiomeStates();

            WorldSaveData world = new WorldSaveData(seed, 1, 0f, resources, biomeStates);
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

            GameSaveData saveData = store.Load(slotName);
if (saveData == null)
            {
                Debug.LogError($"[Save] Load failed: Could not deserialize save data for slot '{slotName}'.");
                return false;
            }

            saveData.EnsureDefaults();
            Debug.Log($"[Save] Loaded save data. Seed: {saveData.World.Seed}, Resources in save: {saveData.World.Resources.Count}");

            if (worldGenerator != null)
            {
                Debug.Log($"[Save] Regenerating world with seed: {saveData.World.Seed}");
                worldGenerator.SetSeed(saveData.World.Seed);
                worldGenerator.Generate();
                ResolveReferences();
            }

            // Restore Resources
            RestoreResourceStates(saveData.World.Resources);

            ApplyBiomeStates(saveData.World.BiomeStates);

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
                    CharacterController cc = playerSurvival.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;
                    playerSurvival.transform.position = targetPos;
                    if (cc != null) cc.enabled = true;
                    Debug.Log($"[Save] Restored player position to: {targetPos}");
                }
            }

            Debug.Log("[Save] Load complete.");
            return true;
        }

        private void RestoreResourceStates(IReadOnlyList<ResourceSaveData> savedResources)
        {
            if (savedResources == null || savedResources.Count == 0) return;

            var nodes = FindObjectsByType<ApexShift.Runtime.Resources.ResourceNodeView>();
            Debug.Log($"[Save] Restoring state for {savedResources.Count} saved resources among {nodes.Length} active nodes.");

            // Create a spatial lookup for faster matching
            Dictionary<Vector3Int, ApexShift.Runtime.Resources.ResourceNodeView> lookup = new Dictionary<Vector3Int, ApexShift.Runtime.Resources.ResourceNodeView>();
            foreach (var node in nodes)
            {
                Vector3 p = node.transform.position;
                Vector3Int key = new Vector3Int(Mathf.RoundToInt(p.x * 100), Mathf.RoundToInt(p.y * 100), Mathf.RoundToInt(p.z * 100));
                if (!lookup.ContainsKey(key)) lookup[key] = node;
            }

            int restoredCount = 0;
            foreach (var resData in savedResources)
            {
                Vector3Int key = new Vector3Int(Mathf.RoundToInt(resData.x * 100), Mathf.RoundToInt(resData.y * 100), Mathf.RoundToInt(resData.z * 100));
                if (lookup.TryGetValue(key, out var node))
                {
                    node.LoadState(resData.Amount, resData.Depleted, resData.GrowthProgress);
                    restoredCount++;
                }
            }
            Debug.Log($"[Save] Restored {restoredCount} resource nodes matching saved positions.");
        }

        public void DeleteGame(string slotName)
        {
            GetSaveStore().Delete(slotName);
        }

        private List<BiomeEcosystemSaveData> CaptureBiomeStates()
        {
            if (ecosystemDirector == null)
            {
                return new List<BiomeEcosystemSaveData>();
            }

            return ecosystemDirector.CaptureSaveData();
        }

        private void ApplyBiomeStates(IReadOnlyList<BiomeEcosystemSaveData> biomeStates)
        {
            if (ecosystemDirector == null || biomeStates == null || biomeStates.Count == 0)
            {
                return;
            }

            ecosystemDirector.LoadSaveData(biomeStates);
        }
}
}
