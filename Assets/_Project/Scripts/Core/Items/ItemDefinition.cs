using System;

namespace ApexShift.Core.Items
{
    public sealed class ItemDefinition
    {
        public ItemId Id { get; }
        public string DisplayName { get; }
        public int MaxStackSize { get; }
        public bool IsEdible { get; }
        public float HungerRestore { get; }
        public float HealthRestore { get; }
        public float StaminaRestore { get; }
        public bool IsUnsafeRawFood { get; }

        public ItemDefinition(
            ItemId id,
            string displayName,
            int maxStackSize,
            bool isEdible = false,
            float hungerRestore = 0f,
            float healthRestore = 0f,
            float staminaRestore = 0f,
            bool isUnsafeRawFood = false)
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

            if (hungerRestore < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(hungerRestore), "Food values cannot be negative.");
            }

            if (healthRestore < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(healthRestore), "Food values cannot be negative.");
            }

            if (staminaRestore < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaRestore), "Food values cannot be negative.");
            }

            Id = id;
            DisplayName = displayName.Trim();
            MaxStackSize = maxStackSize;
            IsEdible = isEdible;
            HungerRestore = hungerRestore;
            HealthRestore = healthRestore;
            StaminaRestore = staminaRestore;
            IsUnsafeRawFood = isUnsafeRawFood;
        }
    }
}
