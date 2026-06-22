using System;
using ApexShift.Core.Crafting;
using ApexShift.Core.Items;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Crafting
{
    public class RecipeModelTests
    {
        [Test]
        public void RecipeIdNormalizesWhitespaceAndCase()
        {
            RecipeId id = new RecipeId(" Torch ");

            Assert.AreEqual("torch", id.ToString());
        }

        [Test]
        public void RecipeDefinitionUsesNormalizedResultItemId()
        {
            RecipeDefinition definition = new RecipeDefinition(
                new RecipeId("torch"),
                new ItemId("torch"),
                1,
                new[]
                {
                    new RecipeIngredient(new ItemId("wood"), 1),
                    new RecipeIngredient(new ItemId("fiber"), 1)
                });

            Assert.AreEqual("torch", definition.ResultItemId);
            Assert.AreEqual(1, definition.ResultAmount);
        }

        [Test]
        public void RecipeIngredientRejectsInvalidAmount()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new RecipeIngredient(new ItemId("wood"), 0);
            });
        }

        [Test]
        public void RecipeDatabaseCreateDefaultRequiresItemDatabase()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                RecipeDatabase.CreateDefault(null);
            });
        }

        [Test]
        public void RecipeDatabaseContainsPrototypeRecipes()
        {
            ItemDatabase itemDatabase = ItemDatabase.CreateDefault();
            RecipeDatabase database = RecipeDatabase.CreateDefault(itemDatabase);

            Assert.IsTrue(database.HasRecipe("campfire"));
            Assert.IsTrue(database.HasRecipe("spear"));
            Assert.IsTrue(database.HasRecipe("torch"));
            Assert.IsTrue(database.HasRecipe("bow"));
            Assert.IsTrue(database.HasRecipe("trap"));
            Assert.IsTrue(database.HasRecipe("wall"));
            Assert.IsTrue(database.HasRecipe("storage_box"));
            Assert.IsTrue(database.HasRecipe("tent"));
        }
    }
}
