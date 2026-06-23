using ApexShift.Core.Crafting;
using ApexShift.Infrastructure.Data.Recipes;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Data
{
    public class RecipeDefinitionAssetTests
    {
        [Test]
        public void RecipeDefinitionAssetMapsToCore()
        {
            RecipeIngredientAsset wood = new RecipeIngredientAsset();
            wood.ConfigureForTests("wood", 1);
            RecipeIngredientAsset fiber = new RecipeIngredientAsset();
            fiber.ConfigureForTests("fiber", 1);

            RecipeDefinitionAsset asset = ScriptableObject.CreateInstance<RecipeDefinitionAsset>();
            asset.ConfigureForTests("torch", "torch", 1, new[] { wood, fiber });

            RecipeDefinition definition = asset.ToCoreDefinition();

            Assert.AreEqual("torch", definition.Id.ToString());
            Assert.AreEqual("torch", definition.ResultItemId);
            Assert.AreEqual(1, definition.ResultAmount);
            Assert.AreEqual(2, definition.Ingredients.Count);
            Assert.AreEqual("wood", definition.Ingredients[0].ItemId.ToString());
            Assert.AreEqual(1, definition.Ingredients[0].Amount);
            Assert.AreEqual("fiber", definition.Ingredients[1].ItemId.ToString());
            Assert.AreEqual(1, definition.Ingredients[1].Amount);
        }
    }
}
