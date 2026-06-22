namespace ApexShift.Core.Save
{
    public sealed class InventorySlotSaveData
    {
        public int? SlotIndex { get; }
        public string ItemId { get; }
        public int Amount { get; }

        public InventorySlotSaveData(int? slotIndex, string itemId, int amount)
        {
            SlotIndex = slotIndex;
            ItemId = itemId;
            Amount = amount;
        }
    }
}

