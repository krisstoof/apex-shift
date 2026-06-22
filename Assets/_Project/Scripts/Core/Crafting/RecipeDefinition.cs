using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Items;

namespace ApexShift.Core.Crafting
{
    public sealed class RecipeDefinition
    {
        private readonly IReadOnlyList<RecipeIngredient> ingredients;

        public RecipeId Id { get; }
        public string ResultItemId { get; }
        public int ResultAmount { get; }
        public IReadOnlyList<RecipeIngredient> Ingredients => ingredients;

        public RecipeDefinition(RecipeId id, ItemId resultItemId, int resultAmount, IEnumerable<RecipeIngredient> ingredients)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Recipe id must be valid.", nameof(id));
            }

            if (!resultItemId.IsValid)
            {
                throw new ArgumentException("Result item id must be valid.", nameof(resultItemId));
            }

            if (resultAmount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(resultAmount));
            }

            if (ingredients == null)
            {
                throw new ArgumentNullException(nameof(ingredients));
            }

            IReadOnlyList<RecipeIngredient> list = ingredients.ToArray();
            if (list.Count == 0)
            {
                throw new ArgumentException("Recipe must contain at least one ingredient.", nameof(ingredients));
            }

            Id = id;
            ResultItemId = resultItemId.ToString();
            ResultAmount = resultAmount;
            this.ingredients = list;
        }
    }
}
