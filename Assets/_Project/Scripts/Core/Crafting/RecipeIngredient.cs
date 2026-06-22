using System;
using ApexShift.Core.Items;

namespace ApexShift.Core.Crafting
{
    public sealed class RecipeIngredient
    {
        public ItemId ItemId { get; }
        public int Amount { get; }

        public RecipeIngredient(ItemId itemId, int amount)
        {
            if (!itemId.IsValid)
            {
                throw new ArgumentException("Item id must be valid.", nameof(itemId));
            }

            if (amount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Ingredient amount must be at least 1.");
            }

            ItemId = itemId;
            Amount = amount;
        }
    }
}
