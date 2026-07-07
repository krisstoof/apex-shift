using ApexShift.Core.Inventory;
using ApexShift.Core.Items;
using ApexShift.Core.Save;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public sealed class PlayerInventoryRuntime : MonoBehaviour
    {
        [SerializeField]
        private int slotCount = InventoryState.DefaultSlotCount;

        [SerializeField]
        private PlayerSurvivalRuntime survivalRuntime;

        private ItemDatabase itemDatabase;
        private InventoryState inventory;

        public InventoryState Inventory => inventory;
        public ItemDatabase ItemDatabase => itemDatabase;

        private void Awake()
        {
            if (survivalRuntime == null)
            {
                survivalRuntime = GetComponent<PlayerSurvivalRuntime>();
            }

            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (inventory != null) return;
            itemDatabase = ApexShift.Core.Items.ItemDatabase.CreateDefault();
            inventory = new InventoryState(itemDatabase, slotCount);
        }

        public bool CanEat(string itemId)
        {
            EnsureInitialized();
            return !string.IsNullOrWhiteSpace(itemId)
                   && itemDatabase.HasItem(itemId)
                   && itemDatabase.IsEdible(itemId)
                   && inventory.GetAmount(itemId) > 0
                   && (survivalRuntime == null || !survivalRuntime.IsDead);
        }

        public FoodConsumptionResult TryEatItem(string itemId, int amount = 1)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return FoodConsumptionResult.Failed("Cannot eat empty item.");
            }

            if (survivalRuntime != null && survivalRuntime.IsDead)
            {
                return FoodConsumptionResult.Failed("Cannot eat after death.");
            }

            string normalizedId = itemDatabase.NormalizeItemId(itemId).ToString();
            if (!itemDatabase.HasItem(normalizedId))
            {
                return FoodConsumptionResult.Failed($"Unknown item '{itemId}'.");
            }

            ItemDefinition definition = itemDatabase.GetDefinition(normalizedId);
            if (!definition.IsEdible)
            {
                return FoodConsumptionResult.Failed($"Cannot eat {definition.DisplayName}.");
            }

            if (inventory.GetAmount(normalizedId) <= 0)
            {
                return FoodConsumptionResult.Failed($"No {definition.DisplayName} in inventory.");
            }

            int consumeAmount = Mathf.Max(1, amount);
            if (!inventory.RemoveItem(normalizedId, consumeAmount))
            {
                return FoodConsumptionResult.Failed($"Could not consume {definition.DisplayName}.");
            }

            float hungerDelta = 0f;
            float healthDelta = 0f;
            float staminaDelta = 0f;
            if (survivalRuntime != null)
            {
                hungerDelta = survivalRuntime.ApplyFood(definition.HungerRestore * consumeAmount).HungerDelta;
                if (definition.HealthRestore > 0f)
                {
                    healthDelta = survivalRuntime.Heal(definition.HealthRestore * consumeAmount).HealthDelta;
                }

                if (definition.StaminaRestore > 0f)
                {
                    staminaDelta = survivalRuntime.RestoreStamina(definition.StaminaRestore * consumeAmount);
                }
            }

            string message = definition.IsUnsafeRawFood
                ? $"Ate raw {definition.DisplayName}. It helped, but may be unsafe later."
                : $"Ate {definition.DisplayName}.";
            Debug.Log($"[Inventory] {message} hungerDelta={hungerDelta:0.0} healthDelta={healthDelta:0.0} staminaDelta={staminaDelta:0.0}", this);
            return new FoodConsumptionResult(true, normalizedId, definition.DisplayName, message, hungerDelta, healthDelta, staminaDelta, definition.IsUnsafeRawFood);
        }

        public FoodConsumptionResult TryEatSlot(int slotIndex)
        {
            EnsureInitialized();
            InventorySlotSnapshot slot = inventory.PeekSlotStack(slotIndex);
            if (string.IsNullOrWhiteSpace(slot.ItemId) || slot.Amount <= 0)
            {
                return FoodConsumptionResult.Failed("Slot is empty.");
            }

            return TryEatItem(slot.ItemId, 1);
        }

        public InventorySaveData ToSaveData()
        {
            EnsureInitialized();
            return inventory.ToSaveData();
        }

        public void LoadFromSaveData(InventorySaveData data)
        {
            EnsureInitialized();
            inventory.LoadFromSaveData(data);
        }
    }
}
