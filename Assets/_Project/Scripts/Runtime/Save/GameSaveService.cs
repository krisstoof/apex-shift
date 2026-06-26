using ApexShift.Core.Inventory;
using ApexShift.Core.Save;
using ApexShift.Infrastructure.Save;
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

        private readonly IGameSaveStore saveStore = new JsonFileGameSaveStore();

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
        }

        public GameSaveData CaptureCurrentState()
        {
            ResolveReferences();

            InventorySaveData inventory = playerInventory != null ? playerInventory.ToSaveData() : InventorySaveData.Empty;
            SurvivalSaveData survival = playerSurvival != null ? playerSurvival.ToSaveData() : SurvivalSaveData.Default;
            int seed = worldGenerator != null ? worldGenerator.Seed : 0;
            WorldSaveData world = new WorldSaveData(seed, 1, 0f, System.Array.Empty<ResourceSaveData>());

            return new GameSaveData(inventory, survival, world);
        }

        public void SaveGame(string slotName)
        {
            saveStore.Save(slotName, CaptureCurrentState());
        }

        public bool LoadGame(string slotName)
        {
            ResolveReferences();

            GameSaveData saveData = saveStore.Load(slotName);
            if (saveData == null)
            {
                return false;
            }

            saveData.EnsureDefaults();

            if (worldGenerator != null)
            {
                worldGenerator.SetSeed(saveData.World != null ? saveData.World.Seed : 0);
                worldGenerator.Generate();
                ResolveReferences();
            }

            if (playerInventory != null)
            {
                playerInventory.LoadFromSaveData(saveData.Inventory);
            }

            if (playerSurvival != null)
            {
                playerSurvival.LoadFromSaveData(saveData.Survival);
            }

            return true;
        }

        public void DeleteGame(string slotName)
        {
            saveStore.Delete(slotName);
        }
    }
}
