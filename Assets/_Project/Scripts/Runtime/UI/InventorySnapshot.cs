using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Inventory;

namespace ApexShift.Runtime.UI.Snapshots
{
    [Serializable]
    public sealed class InventoryItemSnapshot
    {
        public string itemId;
        public int amount;

        public InventoryItemSnapshot(string itemId, int amount)
        {
            this.itemId = itemId ?? string.Empty;
            this.amount = Math.Max(0, amount);
        }
    }

    [Serializable]
    public sealed class InventorySnapshot
    {
        public int slotCount;
        public int emptySlotCount;
        public int occupiedSlotCount;
        public List<InventoryItemSnapshot> items = new List<InventoryItemSnapshot>();

        public static InventorySnapshot Empty => new InventorySnapshot(0, 0, Array.Empty<InventoryItemSnapshot>());

        public InventorySnapshot(int slotCount, int emptySlotCount, IReadOnlyList<InventoryItemSnapshot> items)
        {
            this.slotCount = Math.Max(0, slotCount);
            this.emptySlotCount = Math.Max(0, emptySlotCount);
            occupiedSlotCount = Math.Max(0, this.slotCount - this.emptySlotCount);
            this.items = items != null ? items.Where(item => item != null && item.amount > 0).ToList() : new List<InventoryItemSnapshot>();
        }

        public static InventorySnapshot FromInventory(InventoryState inventory)
        {
            if (inventory == null) return Empty;

            List<InventoryItemSnapshot> itemSnapshots = inventory
                .GetAllItems()
                .Select(pair => new InventoryItemSnapshot(pair.Key, pair.Value))
                .OrderBy(item => item.itemId, StringComparer.Ordinal)
                .ToList();

            return new InventorySnapshot(inventory.SlotCount, inventory.GetEmptySlotCount(), itemSnapshots);
        }
    }
}
