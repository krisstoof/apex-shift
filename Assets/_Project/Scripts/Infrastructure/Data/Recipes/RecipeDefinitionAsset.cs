using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Crafting;
using ApexShift.Core.Items;
using UnityEngine;

namespace ApexShift.Infrastructure.Data.Recipes
{
    [CreateAssetMenu(menuName = "Apex Shift/Data/Recipe Definition", fileName = "RecipeDefinition")]
    public sealed class RecipeDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string recipeId;

        [SerializeField]
        private string resultItemId;

        [SerializeField]
        [Min(1)]
        private int resultAmount = 1;

        [SerializeField]
        private List<RecipeIngredientAsset> ingredients = new List<RecipeIngredientAsset>();

        public RecipeDefinition ToCoreDefinition()
        {
            return new RecipeDefinition(
                new RecipeId(recipeId),
                new ItemId(resultItemId),
                resultAmount,
                ingredients.Select(ingredient => ingredient.ToCoreIngredient()));
        }

        public void ConfigureForTests(string recipeId, string resultItemId, int resultAmount, IEnumerable<RecipeIngredientAsset> ingredients)
        {
            this.recipeId = recipeId;
            this.resultItemId = resultItemId;
            this.resultAmount = resultAmount;
            this.ingredients = ingredients.ToList();
        }
    }
}
