namespace ApexShift.Core.Inventory
{
    public sealed class InventorySlot
    {
        private readonly ItemStack stack;

        public InventorySlot()
        {
            stack = new ItemStack();
        }

        internal ItemStack Stack => stack;

        public bool IsEmpty => stack.IsEmpty;

        public string ItemId => stack.ItemId;

        public int Amount => stack.Amount;
    }
}

