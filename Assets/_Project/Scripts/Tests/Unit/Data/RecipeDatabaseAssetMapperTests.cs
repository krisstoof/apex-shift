using ApexShift.Core.Crafting;
using ApexShift.Core.Items;
using ApexShift.Infrastructure.Data.Items;
using ApexShift.Infrastructure.Data.Mapping;
using ApexShift.Infrastructure.Data.Recipes;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Data
{
    public class RecipeDatabaseAssetMapperTests
    {
        [Test]
        public void RecipeDatabaseAssetMapperCreatesCoreDatabase()
        {
            ItemDefinitionAsset wood = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            wood.ConfigureForTests("wood", "Wood", 20);
            ItemDefinitionAsset fiber = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            fiber.ConfigureForTests("fiber", "Fiber", 20);
            ItemDefinitionAsset torch = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            torch.ConfigureForTests("torch", "Torch", 1);

            ItemDatabase itemDatabase = ItemDatabaseAssetMapper.ToCoreDatabase(new[] { wood, fiber, torch });

            RecipeIngredientAsset woodIngredient = new RecipeIngredientAsset();
            woodIngredient.ConfigureForTests("wood", 1);
            RecipeIngredientAsset fiberIngredient = new RecipeIngredientAsset();
            fiberIngredient.ConfigureForTests("fiber", 1);

            RecipeDefinitionAsset recipeAsset = ScriptableObject.CreateInstance<RecipeDefinitionAsset>();
            recipeAsset.ConfigureForTests("torch", "torch", 1, new[] { woodIngredient, fiberIngredient });

            RecipeDatabase recipeDatabase = RecipeDatabaseAssetMapper.ToCoreDatabase(new[] { recipeAsset }, itemDatabase);

            Assert.IsTrue(recipeDatabase.HasRecipe("torch"));
            RecipeDefinition definition = recipeDatabase.GetRecipe("torch");
            Assert.AreEqual("torch", definition.ResultItemId);
            Assert.AreEqual(1, definition.ResultAmount);
        }
    }
}
