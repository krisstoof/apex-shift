using System.Collections.Generic;

namespace ApexShift.Core.Crafting
{
    public sealed class CraftingResult
    {
        public static CraftingResult Success(RecipeId recipeId, string resultItemId, int resultAmount, IReadOnlyList<RecipeIngredient> consumedIngredients)
        {
            return new CraftingResult(CraftingResultStatus.Success, recipeId, resultItemId, resultAmount, consumedIngredients, new List<RecipeIngredient>());
        }

        public static CraftingResult Failed(CraftingResultStatus status, RecipeId recipeId, string resultItemId = "", int resultAmount = 0, IReadOnlyList<RecipeIngredient> missingIngredients = null)
        {
            return new CraftingResult(status, recipeId, resultItemId, resultAmount, new List<RecipeIngredient>(), missingIngredients ?? new List<RecipeIngredient>());
        }

        public CraftingResultStatus Status { get; }
        public RecipeId RecipeId { get; }
        public string ResultItemId { get; }
        public int CraftedAmount { get; }
        public IReadOnlyList<RecipeIngredient> ConsumedIngredients { get; }
        public IReadOnlyList<RecipeIngredient> MissingIngredients { get; }
        public bool Succeeded => Status == CraftingResultStatus.Success;

        private CraftingResult(CraftingResultStatus status, RecipeId recipeId, string resultItemId, int craftedAmount, IReadOnlyList<RecipeIngredient> consumedIngredients, IReadOnlyList<RecipeIngredient> missingIngredients)
        {
            Status = status;
            RecipeId = recipeId;
            ResultItemId = resultItemId ?? string.Empty;
            CraftedAmount = craftedAmount;
            ConsumedIngredients = consumedIngredients ?? new List<RecipeIngredient>();
            MissingIngredients = missingIngredients ?? new List<RecipeIngredient>();
        }
    }
}
