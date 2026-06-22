using System.Collections.Generic;

namespace ApexShift.Core.Save
{
    public sealed class InventorySaveData
    {
        public int SlotCount { get; }
        public IReadOnlyList<InventorySlotSaveData> Slots { get; }

        public InventorySaveData(int slotCount, IReadOnlyList<InventorySlotSaveData> slots)
        {
            SlotCount = slotCount;
            Slots = slots;
        }
    }
}

