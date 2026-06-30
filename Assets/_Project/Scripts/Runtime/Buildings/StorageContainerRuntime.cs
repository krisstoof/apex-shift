using System;
using ApexShift.Core.Inventory;
using ApexShift.Core.Items;
using ApexShift.Core.Save;
using ApexShift.Runtime.Player;
using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    [DisallowMultipleComponent]
    public sealed class StorageContainerRuntime : MonoBehaviour
    {
        [SerializeField] private string containerId;
        [SerializeField] private int slotCount = 12;

        private InventoryState inventory;

        public string ContainerId => string.IsNullOrWhiteSpace(containerId) ? $"{gameObject.name}_{GetHashCode():X}" : containerId;
        public InventoryState Inventory
        {
            get
            {
                EnsureInitialized();
                return inventory;
            }
        }

        public event Action StorageChanged;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Configure(string id)
        {
            containerId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id.Trim();
            EnsureInitialized();
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
            StorageChanged?.Invoke();
        }

        public bool Open(GameObject actor)
        {
            if (actor == null)
            {
                return false;
            }

            PlayerInventoryRuntime playerInventory = actor.GetComponent<PlayerInventoryRuntime>() ?? actor.GetComponentInParent<PlayerInventoryRuntime>();
            if (playerInventory == null)
            {
                Debug.LogWarning("[Storage] Cannot open storage box because actor has no PlayerInventoryRuntime.", this);
                return false;
            }

            Debug.Log($"[Storage] Opening container '{ContainerId}' for actor '{actor.name}'.", this);
            StorageTransferPanelUI.Open(this, playerInventory);
            return true;
        }

        public bool TryTransferFromPlayer(PlayerInventoryRuntime playerInventory, string itemId, int amount)
        {
            EnsureInitialized();
            if (!CanUse(playerInventory, itemId, amount))
            {
                return false;
            }

            int transferAmount = Mathf.Min(amount, playerInventory.Inventory.GetAmount(itemId));
            if (transferAmount <= 0 || !inventory.CanAddItem(itemId, transferAmount))
            {
                return false;
            }

            if (!playerInventory.Inventory.RemoveItem(itemId, transferAmount))
            {
                return false;
            }

            int remainder = inventory.AddItem(itemId, transferAmount);
            if (remainder > 0)
            {
                playerInventory.Inventory.AddItem(itemId, remainder);
            }

            StorageChanged?.Invoke();
            return true;
        }

        public bool TryTransferToPlayer(PlayerInventoryRuntime playerInventory, string itemId, int amount)
        {
            EnsureInitialized();
            if (!CanUse(playerInventory, itemId, amount))
            {
                return false;
            }

            int transferAmount = Mathf.Min(amount, inventory.GetAmount(itemId));
            if (transferAmount <= 0 || !playerInventory.Inventory.CanAddItem(itemId, transferAmount))
            {
                return false;
            }

            if (!inventory.RemoveItem(itemId, transferAmount))
            {
                return false;
            }

            int remainder = playerInventory.Inventory.AddItem(itemId, transferAmount);
            if (remainder > 0)
            {
                inventory.AddItem(itemId, remainder);
            }

            StorageChanged?.Invoke();
            return true;
        }

        private bool CanUse(PlayerInventoryRuntime playerInventory, string itemId, int amount)
        {
            return playerInventory != null
                   && playerInventory.Inventory != null
                   && !string.IsNullOrWhiteSpace(itemId)
                   && amount > 0;
        }

        private void EnsureInitialized()
        {
            if (inventory != null)
            {
                return;
            }

            inventory = new InventoryState(ItemDatabase.CreateDefault(), Mathf.Max(1, slotCount));
        }
    }
}
