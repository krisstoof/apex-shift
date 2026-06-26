using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class InventorySlotSaveData
    {
        public bool hasSlotIndex;
        public int slotIndex;
        public string itemId;
        public int amount;

        public int? SlotIndex => hasSlotIndex ? slotIndex : (int?)null;
        public string ItemId => itemId;
        public int Amount => amount;

        public InventorySlotSaveData()
        {
        }

        public InventorySlotSaveData(int? slotIndex, string itemId, int amount)
        {
            hasSlotIndex = slotIndex.HasValue;
            this.slotIndex = slotIndex.GetValueOrDefault();
            this.itemId = itemId ?? string.Empty;
            this.amount = Math.Max(0, amount);
        }
    }
}
