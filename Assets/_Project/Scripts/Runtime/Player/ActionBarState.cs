using System;

namespace ApexShift.Runtime.Player
{
    public sealed class ActionBarState
    {
        private readonly string[] items;
        private readonly bool rejectNonActionItems;
        private int activeSlotIndex = -1;
        public ActionBarState(int slotCount = 9, bool rejectNonActionItems = true) { SlotCount = Math.Max(1, Math.Min(9, slotCount)); this.rejectNonActionItems = rejectNonActionItems; items = new string[SlotCount]; }
        public int SlotCount { get; }
        public int ActiveSlotIndex => activeSlotIndex;
        public string ActiveItemId => GetItem(activeSlotIndex);
        public event Action Changed;
        public event Action<int, string> ActiveSlotChanged;
        public string GetItem(int slotIndex) => slotIndex >= 0 && slotIndex < items.Length ? items[slotIndex] ?? string.Empty : string.Empty;
        public bool AssignItem(int slotIndex, string itemId)
        {
            if (slotIndex < 0 || slotIndex >= items.Length) return false;
            string normalized = Normalize(itemId);
            if (string.IsNullOrEmpty(normalized) || (rejectNonActionItems && !ActionBarItemPolicy.IsActionBarItem(normalized))) return false;
            items[slotIndex] = normalized; Changed?.Invoke(); return SetActiveSlot(slotIndex);
        }
        public bool SetActiveSlot(int slotIndex)
        {
            if (slotIndex < -1 || slotIndex >= items.Length) return false;
            activeSlotIndex = slotIndex; ActiveSlotChanged?.Invoke(activeSlotIndex, ActiveItemId); Changed?.Invoke(); return true;
        }
        public void ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= items.Length) return;
            items[slotIndex] = string.Empty;
            if (activeSlotIndex == slotIndex)
            {
                activeSlotIndex = -1;
                ActiveSlotChanged?.Invoke(-1, string.Empty);
            }
            Changed?.Invoke();
        }
        public void ClearActiveSlot() => SetActiveSlot(-1);
        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
