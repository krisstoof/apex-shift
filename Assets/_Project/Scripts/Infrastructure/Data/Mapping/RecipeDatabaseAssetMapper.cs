using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Crafting;
using ApexShift.Core.Items;
using ApexShift.Infrastructure.Data.Items;
using ApexShift.Infrastructure.Data.Recipes;

namespace ApexShift.Infrastructure.Data.Mapping
{
    public static class RecipeDatabaseAssetMapper
    {
        public static RecipeDatabase ToCoreDatabase(IEnumerable<RecipeDefinitionAsset> assets, ItemDatabase itemDatabase)
        {
            if (assets == null)
            {
                throw new ArgumentNullException(nameof(assets));
            }

            if (itemDatabase == null)
            {
                throw new ArgumentNullException(nameof(itemDatabase));
            }

            List<RecipeDefinition> recipes = new List<RecipeDefinition>();
            foreach (RecipeDefinitionAsset asset in assets)
            {
                RecipeDefinition definition = asset.ToCoreDefinition();
                if (!itemDatabase.HasItem(definition.ResultItemId))
                {
                    throw new InvalidOperationException($"Unknown result item '{definition.ResultItemId}' for recipe '{definition.Id}'.");
                }

                foreach (RecipeIngredient ingredient in definition.Ingredients)
                {
                    if (!itemDatabase.HasItem(ingredient.ItemId))
                    {
                        throw new InvalidOperationException($"Unknown ingredient item '{ingredient.ItemId}' for recipe '{definition.Id}'.");
                    }
                }

                recipes.Add(definition);
            }

            return new RecipeDatabase(recipes);
        }
    }
}
