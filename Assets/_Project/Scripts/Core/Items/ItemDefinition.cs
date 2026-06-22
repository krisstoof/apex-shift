using System;

namespace ApexShift.Core.Items
{
    public sealed class ItemDefinition
    {
        public ItemId Id { get; }
        public string DisplayName { get; }
        public int MaxStackSize { get; }

        public ItemDefinition(ItemId id, string displayName, int maxStackSize)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Item id must be valid.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name cannot be empty.", nameof(displayName));
            }

            if (maxStackSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxStackSize), "Max stack size must be at least 1.");
            }

            Id = id;
            DisplayName = displayName.Trim();
            MaxStackSize = maxStackSize;
        }
    }
}

