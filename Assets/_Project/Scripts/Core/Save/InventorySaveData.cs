using System;
using System.Collections.Generic;
using System.Linq;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class InventorySaveData
    {
        public int slotCount;
        public List<InventorySlotSaveData> slots = new List<InventorySlotSaveData>();

        public int SlotCount => slotCount;
        public IReadOnlyList<InventorySlotSaveData> Slots => slots;

        public static InventorySaveData Empty => new InventorySaveData(0, Array.Empty<InventorySlotSaveData>());

        public InventorySaveData()
        {
        }

        public InventorySaveData(int slotCount, IReadOnlyList<InventorySlotSaveData> slots)
        {
            this.slotCount = Math.Max(0, slotCount);
            this.slots = slots != null
                ? slots.Where(slot => slot != null).ToList()
                : new List<InventorySlotSaveData>();
        }
    }
}
