using System;
using System.Collections.Generic;
using ApexShift.Core.Inventory;
using ApexShift.Core.Items;

namespace ApexShift.Core.Crafting
{
public sealed class CraftingSystem
{
        private readonly RecipeDatabase recipeDatabase;
        private readonly ItemDatabase itemDatabase;

        public CraftingSystem(RecipeDatabase recipeDatabase, ItemDatabase itemDatabase)
        {
            this.recipeDatabase = recipeDatabase ?? throw new ArgumentNullException(nameof(recipeDatabase));
            this.itemDatabase = itemDatabase ?? throw new ArgumentNullException(nameof(itemDatabase));
        }

        public CraftingResult Craft(InventoryState inventory, string recipeId)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            RecipeId normalizedRecipeId = recipeDatabase.NormalizeRecipeId(recipeId);
            if (!recipeDatabase.HasRecipe(normalizedRecipeId))
            {
                return CraftingResult.Failed(CraftingResultStatus.UnknownRecipe, normalizedRecipeId);
            }

            RecipeDefinition recipe = recipeDatabase.GetRecipe(normalizedRecipeId);
            IReadOnlyList<RecipeIngredient> missingIngredients = GetMissingIngredients(inventory, recipe);
            if (missingIngredients.Count > 0)
            {
                return CraftingResult.Failed(CraftingResultStatus.MissingIngredients, normalizedRecipeId, recipe.ResultItemId, recipe.ResultAmount, missingIngredients);
            }

            InventoryState simulation = CreateSimulationAfterConsumingIngredients(inventory, recipe);
            if (!simulation.CanAddItem(recipe.ResultItemId, recipe.ResultAmount))
            {
                return CraftingResult.Failed(CraftingResultStatus.InventoryFull, normalizedRecipeId, recipe.ResultItemId, recipe.ResultAmount);
            }

            List<RecipeIngredient> consumed = new List<RecipeIngredient>();
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                inventory.RemoveItem(ingredient.ItemId.ToString(), ingredient.Amount);
                consumed.Add(new RecipeIngredient(ingredient.ItemId, ingredient.Amount));
            }

            if (!inventory.AddItemFullStack(recipe.ResultItemId, recipe.ResultAmount))
            {
                throw new InvalidOperationException("Crafting simulation passed but result could not be added.");
            }

            return CraftingResult.Success(recipe.Id, recipe.ResultItemId, recipe.ResultAmount, consumed);
        }

        public bool CanCraft(InventoryState inventory, string recipeId)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            RecipeId normalizedRecipeId = recipeDatabase.NormalizeRecipeId(recipeId);
            if (!recipeDatabase.HasRecipe(normalizedRecipeId))
            {
                return false;
            }

            RecipeDefinition recipe = recipeDatabase.GetRecipe(normalizedRecipeId);
            if (GetMissingIngredients(inventory, recipe).Count > 0)
            {
                return false;
            }

            InventoryState simulation = CreateSimulationAfterConsumingIngredients(inventory, recipe);
            return simulation.CanAddItem(recipe.ResultItemId, recipe.ResultAmount);
        }

        public IReadOnlyList<RecipeIngredient> GetMissingIngredients(InventoryState inventory, RecipeDefinition recipe)
        {
            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            if (recipe == null)
            {
                throw new ArgumentNullException(nameof(recipe));
            }

            List<RecipeIngredient> missing = new List<RecipeIngredient>();
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                int have = inventory.GetAmount(ingredient.ItemId.ToString());
                if (have < ingredient.Amount)
                {
                    missing.Add(new RecipeIngredient(ingredient.ItemId, ingredient.Amount - have));
                }
            }

            return missing;
        }

        private InventoryState CreateSimulationAfterConsumingIngredients(InventoryState inventory, RecipeDefinition recipe)
        {
            InventoryState simulation = new InventoryState(itemDatabase, inventory.SlotCount);
            simulation.LoadFromSaveData(inventory.ToSaveData());

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                simulation.RemoveItem(ingredient.ItemId.ToString(), ingredient.Amount);
            }

            return simulation;
        }
    }
}
