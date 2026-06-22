using System;
using ApexShift.Core.Items;
using ApexShift.Core.Save;

namespace ApexShift.Core.Inventory
{
    public sealed class ItemStack
    {
        private ItemId itemId;
        private int amount;

        public bool IsEmpty => !itemId.IsValid || amount <= 0;

        public string ItemId => itemId.ToString();

        public int Amount => IsEmpty ? 0 : amount;

        public bool CanStackWith(string stackItemId)
        {
            if (IsEmpty)
            {
                return false;
            }

            return string.Equals(ItemId, ApexShift.Core.Items.ItemId.Normalize(stackItemId), StringComparison.Ordinal);
        }

        public int GetAvailableSpace(int maxStack)
        {
            if (maxStack <= 0)
            {
                return 0;
            }

            if (IsEmpty)
            {
                return maxStack;
            }

            return Math.Max(0, maxStack - amount);
        }

        public int AddAmount(string stackItemId, int value, int maxStack)
        {
            if (value <= 0 || maxStack <= 0)
            {
                return value;
            }

            if (IsEmpty)
            {
                int added = Math.Min(value, maxStack);
                itemId = new ItemId(stackItemId);
                amount = added;
                return value - added;
            }

            if (!CanStackWith(stackItemId))
            {
                return value;
            }

            int available = GetAvailableSpace(maxStack);
            int addedAmount = Math.Min(value, available);
            amount += addedAmount;
            return value - addedAmount;
        }

        public int RemoveAmount(int value)
        {
            if (value <= 0 || IsEmpty)
            {
                return 0;
            }

            int removed = Math.Min(value, amount);
            amount -= removed;
            if (amount <= 0)
            {
                Clear();
            }

            return removed;
        }

        public void SetStack(string stackItemId, int stackAmount)
        {
            if (!ApexShift.Core.Items.ItemId.TryCreate(stackItemId, out itemId) || stackAmount <= 0)
            {
                Clear();
                return;
            }

            amount = stackAmount;
        }

        public void Clear()
        {
            itemId = default;
            amount = 0;
        }

        public InventorySlotSaveData ToSaveData(int slotIndex)
        {
            if (IsEmpty)
            {
                return null;
            }

            return new InventorySlotSaveData(slotIndex, ItemId, amount);
        }
    }
}
