using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Items;
using ApexShift.Core.Save;

namespace ApexShift.Core.Inventory
{
    public sealed class InventoryState
    {
        public const int DefaultSlotCount = 9;

        private readonly ItemDatabase itemDatabase;
        private readonly List<InventorySlot> slots;

        public event Action InventoryChanged;

        public InventoryState(ItemDatabase itemDatabase, int slotCount = DefaultSlotCount)
        {
            this.itemDatabase = itemDatabase ?? throw new ArgumentNullException(nameof(itemDatabase));
            if (slotCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            }

            slots = Enumerable.Range(0, slotCount).Select(_ => new InventorySlot()).ToList();
        }

        public int SlotCount => slots.Count;

        public IReadOnlyList<InventorySlot> Slots => slots;

        public int AddItem(string itemId, int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            string normalizedId = itemDatabase.NormalizeItemId(itemId).ToString();
            if (!itemDatabase.HasItem(normalizedId))
            {
                return amount;
            }

            int maxStack = itemDatabase.GetMaxStack(normalizedId);
            int remaining = amount;
            bool changed = false;

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.ItemId == normalizedId)
                {
                    int before = remaining;
                    remaining = slot.Stack.AddAmount(normalizedId, remaining, maxStack);
                    changed |= remaining != before;
                }
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    int before = remaining;
                    remaining = slot.Stack.AddAmount(normalizedId, remaining, maxStack);
                    changed |= remaining != before;
                }
            }

            if (changed)
            {
                InventoryChanged?.Invoke();
            }

            return remaining;
        }

        public bool CanAddItem(string itemId, int amount)
        {
            string normalizedId = itemDatabase.NormalizeItemId(itemId).ToString();
            if (amount <= 0 || !itemDatabase.HasItem(normalizedId))
            {
                return false;
            }

            int maxStack = itemDatabase.GetMaxStack(normalizedId);
            int remaining = amount;

            foreach (InventorySlot slot in slots)
            {
                if (!slot.IsEmpty && slot.ItemId == normalizedId)
                {
                    remaining -= slot.Stack.GetAvailableSpace(maxStack);
                }
            }

            if (remaining <= 0)
            {
                return true;
            }

            int emptySlots = GetEmptySlotCount();
            int emptyCapacity = emptySlots * maxStack;
            return remaining <= emptyCapacity;
        }

        public bool AddItemFullStack(string itemId, int amount)
        {
            if (!CanAddItem(itemId, amount))
            {
                return false;
            }

            return AddItem(itemId, amount) == 0;
        }

        public bool RemoveItem(string itemId, int amount)
        {
            string normalizedId = itemDatabase.NormalizeItemId(itemId).ToString();
            if (amount <= 0)
            {
                return true;
            }

            if (!itemDatabase.HasItem(normalizedId) || GetAmount(normalizedId) < amount)
            {
                return false;
            }

            int remaining = amount;
            bool changed = false;
            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (!slot.IsEmpty && slot.ItemId == normalizedId)
                {
                    int removed = slot.Stack.RemoveAmount(remaining);
                    remaining -= removed;
                    changed |= removed > 0;
                }
            }

            if (changed)
            {
                InventoryChanged?.Invoke();
            }

            return true;
        }

        public int RemoveItemById(string itemId, int amount)
        {
            string normalizedId = itemDatabase.NormalizeItemId(itemId).ToString();
            if (amount <= 0 || !itemDatabase.HasItem(normalizedId))
            {
                return 0;
            }

            int remaining = amount;
            int removedTotal = 0;

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty || slot.ItemId != normalizedId)
                {
                    continue;
                }

                int removed = slot.Stack.RemoveAmount(remaining);
                if (removed <= 0)
                {
                    continue;
                }

                remaining -= removed;
                removedTotal += removed;
            }

            if (removedTotal > 0)
            {
                InventoryChanged?.Invoke();
            }

            return removedTotal;
        }

        public int RemoveFromSlot(int slotIndex, int amount = -1)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return 0;
            }

            InventorySlot slot = slots[slotIndex];
            if (slot.IsEmpty)
            {
                return 0;
            }

            int requested = amount <= 0 ? slot.Amount : amount;
            int removed = slot.Stack.RemoveAmount(requested);
            if (removed > 0)
            {
                InventoryChanged?.Invoke();
            }

            return removed;
        }

        public bool HasItem(string itemId, int amount) => GetAmount(itemId) >= amount;

        public int GetAmount(string itemId)
        {
            string normalizedId = itemDatabase.NormalizeItemId(itemId).ToString();
            if (!itemDatabase.HasItem(normalizedId))
            {
                return 0;
            }

            return slots.Where(slot => !slot.IsEmpty && slot.ItemId == normalizedId).Sum(slot => slot.Amount);
        }

        public IReadOnlyDictionary<string, int> GetAllItems()
        {
            Dictionary<string, int> totals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (InventorySlot slot in slots)
            {
                if (slot.IsEmpty)
                {
                    continue;
                }

                totals.TryGetValue(slot.ItemId, out int current);
                totals[slot.ItemId] = current + slot.Amount;
            }

            return totals;
        }

        public int GetEmptySlotCount()
        {
            return slots.Count(slot => slot.IsEmpty);
        }

        public bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < slots.Count;
        }

        public InventorySlotSnapshot PeekSlotStack(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return InventorySlotSnapshot.Empty;
            }

            InventorySlot slot = slots[slotIndex];
            return new InventorySlotSnapshot(slot.ItemId, slot.Amount);
        }

        public bool ClearSlotStack(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                return false;
            }

            InventorySlot slot = slots[slotIndex];
            if (slot.IsEmpty)
            {
                return false;
            }

            slot.Stack.Clear();
            InventoryChanged?.Invoke();
            return true;
        }

        public InventorySaveData ToSaveData()
        {
            List<InventorySlotSaveData> saveSlots = new List<InventorySlotSaveData>();
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlotSaveData slotData = slots[i].Stack.ToSaveData(i);
                if (slotData != null)
                {
                    saveSlots.Add(slotData);
                }
            }

            return new InventorySaveData(SlotCount, saveSlots);
        }

        public void LoadFromSaveData(InventorySaveData data)
        {
            ClearAllSlots(false);
            if (data == null)
            {
                InventoryChanged?.Invoke();
                return;
            }

            List<InventorySlotSaveData> compacted = new List<InventorySlotSaveData>();
            foreach (InventorySlotSaveData slotData in data.Slots ?? Array.Empty<InventorySlotSaveData>())
            {
                if (slotData == null || string.IsNullOrWhiteSpace(slotData.ItemId))
                {
                    continue;
                }

                string normalizedId = itemDatabase.NormalizeItemId(slotData.ItemId).ToString();
                if (!itemDatabase.HasItem(normalizedId))
                {
                    continue;
                }

                int maxStack = itemDatabase.GetMaxStack(normalizedId);
                int amount = Math.Max(1, Math.Min(slotData.Amount, maxStack));
                if (slotData.SlotIndex.HasValue)
                {
                    int slotIndex = slotData.SlotIndex.Value;
                    if (!IsValidSlotIndex(slotIndex))
                    {
                        continue;
                    }

                    if (slots[slotIndex].IsEmpty)
                    {
                        slots[slotIndex].Stack.SetStack(normalizedId, amount);
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    compacted.Add(new InventorySlotSaveData(null, normalizedId, amount));
                }
            }

            foreach (InventorySlotSaveData slotData in compacted)
            {
                int emptyIndex = slots.FindIndex(slot => slot.IsEmpty);
                if (emptyIndex < 0)
                {
                    break;
                }

                slots[emptyIndex].Stack.SetStack(slotData.ItemId, slotData.Amount);
            }

            InventoryChanged?.Invoke();
        }

        public void LoadLegacyItemTotals(IReadOnlyDictionary<string, int> itemTotals)
        {
            ClearAllSlots(false);
            if (itemTotals == null)
            {
                InventoryChanged?.Invoke();
                return;
            }

            foreach (KeyValuePair<string, int> pair in itemTotals)
            {
                string normalizedId = itemDatabase.NormalizeItemId(pair.Key).ToString();
                if (!itemDatabase.HasItem(normalizedId))
                {
                    continue;
                }

                AddItem(normalizedId, pair.Value);
            }

            InventoryChanged?.Invoke();
        }

        private void ClearAllSlots(bool raiseEvent)
        {
            foreach (InventorySlot slot in slots)
            {
                slot.Stack.Clear();
            }

            if (raiseEvent)
            {
                InventoryChanged?.Invoke();
            }
        }
    }

    public readonly struct InventorySlotSnapshot
    {
        public static InventorySlotSnapshot Empty => new InventorySlotSnapshot(string.Empty, 0);

        public string ItemId { get; }
        public int Amount { get; }

        public InventorySlotSnapshot(string itemId, int amount)
        {
            ItemId = itemId ?? string.Empty;
            Amount = amount;
        }
    }
}
