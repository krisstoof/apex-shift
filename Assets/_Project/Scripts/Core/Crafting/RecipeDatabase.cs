using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Items;

namespace ApexShift.Core.Crafting
{
    public sealed class RecipeDatabase
    {
        private readonly Dictionary<string, RecipeDefinition> recipesById;

        public RecipeDatabase(IEnumerable<RecipeDefinition> recipes)
        {
            if (recipes == null)
            {
                throw new ArgumentNullException(nameof(recipes));
            }

            recipesById = new Dictionary<string, RecipeDefinition>(StringComparer.Ordinal);
            foreach (RecipeDefinition recipe in recipes)
            {
                recipesById.Add(recipe.Id.ToString(), recipe);
            }
        }

        public static RecipeDatabase CreateDefault(ItemDatabase itemDatabase)
        {
            if (itemDatabase == null)
            {
                throw new ArgumentNullException(nameof(itemDatabase));
            }

            RecipeDefinition[] recipes = new[]
            {
                new RecipeDefinition(new RecipeId("campfire"), new ItemId("campfire"), 1, new[]
                {
                    new RecipeIngredient(new ItemId("wood"), 3),
                    new RecipeIngredient(new ItemId("stone"), 2)
                }),
                new RecipeDefinition(new RecipeId("spear"), new ItemId("spear"), 1, new[]
                {
                    new RecipeIngredient(new ItemId("wood"), 2),
                    new RecipeIngredient(new ItemId("stone"), 1),
                    new RecipeIngredient(new ItemId("fiber"), 1)
                }),
                new RecipeDefinition(new RecipeId("torch"), new ItemId("torch"), 1, new[]
                {
                    new RecipeIngredient(new ItemId("wood"), 1),
                    new RecipeIngredient(new ItemId("fiber"), 1)
                }),
                new RecipeDefinition(new RecipeId("bow"), new ItemId("bow"), 1, new[]
                {
                    new RecipeIngredient(new ItemId("wood"), 3),
                    new RecipeIngredient(new ItemId("fiber"), 4),
                    new RecipeIngredient(new ItemId("bone"), 1)
                }),
                new RecipeDefinition(new RecipeId("trap"), new ItemId("trap"), 1, new[]
                {
                    new RecipeIngredient(new ItemId("wood"), 2),
                    new RecipeIngredient(new ItemId("fiber"), 2)
                }),
                new RecipeDefinition(new RecipeId("wall"), new ItemId("wall"), 1, new[]
                {
                    new RecipeIngredient(new ItemId("wood"), 3)
                }),
                new RecipeDefinition(new RecipeId("storage_box"), new ItemId("storage_box"), 1, new[]
                {
                    new RecipeIngredient(new ItemId("wood"), 4)
                }),
                new RecipeDefinition(new RecipeId("tent"), new ItemId("tent"), 1, new[]
                {
                    new RecipeIngredient(new ItemId("wood"), 4),
                    new RecipeIngredient(new ItemId("fiber"), 3)
                })
            };

            foreach (RecipeDefinition recipe in recipes)
            {
                if (!itemDatabase.HasItem(recipe.ResultItemId))
                {
                    throw new InvalidOperationException($"Unknown result item '{recipe.ResultItemId}' for recipe '{recipe.Id}'.");
                }

                foreach (RecipeIngredient ingredient in recipe.Ingredients)
                {
                    if (!itemDatabase.HasItem(ingredient.ItemId))
                    {
                        throw new InvalidOperationException($"Unknown ingredient item '{ingredient.ItemId}' for recipe '{recipe.Id}'.");
                    }
                }
            }

            return new RecipeDatabase(recipes);
        }

        public bool HasRecipe(string recipeId) => HasRecipe(NormalizeRecipeId(recipeId));

        public bool HasRecipe(RecipeId recipeId) => recipeId.IsValid && recipesById.ContainsKey(recipeId.ToString());

        public RecipeDefinition GetRecipe(string recipeId) => GetRecipe(NormalizeRecipeId(recipeId));

        public RecipeDefinition GetRecipe(RecipeId recipeId)
        {
            if (!recipeId.IsValid || !recipesById.TryGetValue(recipeId.ToString(), out RecipeDefinition recipe))
            {
                throw new KeyNotFoundException($"Unknown recipe id '{recipeId}'.");
            }

            return recipe;
        }

        public IReadOnlyCollection<RecipeDefinition> GetAllRecipes() => recipesById.Values.ToArray();

        public RecipeId NormalizeRecipeId(string value)
        {
            string normalized = RecipeId.Normalize(value);
            if (normalized.Length == 0)
            {
                return default;
            }

            return new RecipeId(normalized);
        }
    }
}
