using System;
using ApexShift.Core.Crafting;
using ApexShift.Core.Items;
using UnityEngine;

namespace ApexShift.Infrastructure.Data.Recipes
{
    [Serializable]
    public sealed class RecipeIngredientAsset
    {
        [SerializeField]
        private string itemId;

        [SerializeField]
        [Min(1)]
        private int amount = 1;

        public RecipeIngredient ToCoreIngredient()
        {
            return new RecipeIngredient(new ItemId(itemId), amount);
        }

        public void ConfigureForTests(string itemId, int amount)
        {
            this.itemId = itemId;
            this.amount = amount;
        }
    }
}
