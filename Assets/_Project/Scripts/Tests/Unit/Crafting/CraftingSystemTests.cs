using ApexShift.Core.Crafting;
using ApexShift.Core.Inventory;
using ApexShift.Core.Items;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Crafting
{
    public class CraftingSystemTests
    {
        private ItemDatabase itemDatabase;
        private RecipeDatabase recipeDatabase;
        private CraftingSystem craftingSystem;

        [SetUp]
        public void SetUp()
        {
            itemDatabase = ItemDatabase.CreateDefault();
            recipeDatabase = RecipeDatabase.CreateDefault(itemDatabase);
            craftingSystem = new CraftingSystem(recipeDatabase, itemDatabase);
        }

        [Test]
        public void DefaultRecipeDatabaseContainsAllPrototypeRecipes()
        {
            Assert.IsTrue(recipeDatabase.HasRecipe("campfire"));
            Assert.IsTrue(recipeDatabase.HasRecipe("spear"));
            Assert.IsTrue(recipeDatabase.HasRecipe("torch"));
            Assert.IsTrue(recipeDatabase.HasRecipe("bow"));
            Assert.IsTrue(recipeDatabase.HasRecipe("trap"));
            Assert.IsTrue(recipeDatabase.HasRecipe("wall"));
            Assert.IsTrue(recipeDatabase.HasRecipe("storage_box"));
            Assert.IsTrue(recipeDatabase.HasRecipe("tent"));
        }

        [Test]
        public void CraftSuccessConsumesIngredientsAndAddsResult()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 3);
            inventory.AddItem("stone", 2);

            CraftingResult result = craftingSystem.Craft(inventory, "campfire");

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(CraftingResultStatus.Success, result.Status);
            Assert.AreEqual("campfire", result.ResultItemId);
            Assert.AreEqual(1, result.CraftedAmount);
            Assert.AreEqual(2, result.ConsumedIngredients.Count);
            Assert.AreEqual(1, inventory.GetAmount("campfire"));
            Assert.AreEqual(0, inventory.GetAmount("wood"));
            Assert.AreEqual(0, inventory.GetAmount("stone"));
        }

        [Test]
        public void CraftFailsWhenIngredientsAreMissing()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 3);

            CraftingResult result = craftingSystem.Craft(inventory, "campfire");

            Assert.AreEqual(CraftingResultStatus.MissingIngredients, result.Status);
            Assert.AreEqual(3, inventory.GetAmount("wood"));
            Assert.AreEqual(0, inventory.GetAmount("campfire"));
        }

        [Test]
        public void CraftFailsWhenResultDoesNotFit()
        {
            InventoryState inventory = new InventoryState(itemDatabase, 1);
            inventory.AddItem("wood", 20);

            RecipeDefinition recipe = new RecipeDefinition(new RecipeId("test_output"), new ItemId("stone"), 1, new[]
            {
                new RecipeIngredient(new ItemId("wood"), 1)
            });

            recipeDatabase = new RecipeDatabase(new[] { recipe });
            craftingSystem = new CraftingSystem(recipeDatabase, itemDatabase);

            CraftingResult result = craftingSystem.Craft(inventory, "test_output");

            Assert.AreEqual(CraftingResultStatus.InventoryFull, result.Status);
            Assert.AreEqual(20, inventory.GetAmount("wood"));
            Assert.AreEqual(0, inventory.GetAmount("stone"));
        }

        [Test]
        public void CraftFailsForUnknownRecipe()
        {
            InventoryState inventory = new InventoryState(itemDatabase);

            CraftingResult result = craftingSystem.Craft(inventory, "unknown_recipe");

            Assert.AreEqual(CraftingResultStatus.UnknownRecipe, result.Status);
        }

        [Test]
        public void CanCraftChecksRecipeAndIngredients()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 2);
            inventory.AddItem("stone", 1);
            inventory.AddItem("fiber", 1);

            Assert.IsTrue(craftingSystem.CanCraft(inventory, "spear"));
            Assert.IsFalse(craftingSystem.CanCraft(inventory, "bow"));
            Assert.IsFalse(craftingSystem.CanCraft(inventory, "unknown_recipe"));
        }

        [Test]
        public void CanCraftReturnsFalseWhenResultDoesNotFit()
        {
            InventoryState inventory = new InventoryState(itemDatabase, 1);
            inventory.AddItem("wood", 20);

            RecipeDefinition recipe = new RecipeDefinition(new RecipeId("test_output"), new ItemId("stone"), 1, new[]
            {
                new RecipeIngredient(new ItemId("wood"), 1)
            });

            recipeDatabase = new RecipeDatabase(new[] { recipe });
            craftingSystem = new CraftingSystem(recipeDatabase, itemDatabase);

            Assert.IsFalse(craftingSystem.CanCraft(inventory, "test_output"));
            Assert.AreEqual(20, inventory.GetAmount("wood"));
            Assert.AreEqual(0, inventory.GetAmount("stone"));
        }

        [Test]
        public void MissingIngredientsAreReported()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 1);

            RecipeDefinition torch = recipeDatabase.GetRecipe("torch");
            var missing = craftingSystem.GetMissingIngredients(inventory, torch);

            Assert.AreEqual(1, missing.Count);
            Assert.AreEqual("fiber", missing[0].ItemId.ToString());
            Assert.AreEqual(1, missing[0].Amount);
        }

        [Test]
        public void CampfireRecipeMatchesPrototypeData()
        {
            RecipeDefinition recipe = recipeDatabase.GetRecipe("campfire");

            Assert.AreEqual("campfire", recipe.ResultItemId);
            Assert.AreEqual(1, recipe.ResultAmount);
            Assert.AreEqual(2, recipe.Ingredients.Count);
            AssertIngredient(recipe, 0, "wood", 3);
            AssertIngredient(recipe, 1, "stone", 2);
        }

        [Test]
        public void TorchRecipeMatchesPrototypeData()
        {
            RecipeDefinition torch = recipeDatabase.GetRecipe("torch");

            Assert.AreEqual("torch", torch.ResultItemId);
            Assert.AreEqual(1, torch.ResultAmount);
            Assert.AreEqual(2, torch.Ingredients.Count);
            AssertIngredient(torch, 0, "wood", 1);
            AssertIngredient(torch, 1, "fiber", 1);
        }

        [Test]
        public void SpearRecipeMatchesPrototypeData()
        {
            RecipeDefinition spear = recipeDatabase.GetRecipe("spear");

            Assert.AreEqual("spear", spear.ResultItemId);
            Assert.AreEqual(1, spear.ResultAmount);
            Assert.AreEqual(3, spear.Ingredients.Count);
            AssertIngredient(spear, 0, "wood", 2);
            AssertIngredient(spear, 1, "stone", 1);
            AssertIngredient(spear, 2, "fiber", 1);
        }

        [Test]
        public void BowRecipeMatchesPrototypeData()
        {
            RecipeDefinition recipe = recipeDatabase.GetRecipe("bow");

            Assert.AreEqual("bow", recipe.ResultItemId);
            Assert.AreEqual(1, recipe.ResultAmount);
            Assert.AreEqual(3, recipe.Ingredients.Count);
            AssertIngredient(recipe, 0, "wood", 3);
            AssertIngredient(recipe, 1, "fiber", 4);
            AssertIngredient(recipe, 2, "bone", 1);
        }

        [Test]
        public void TrapRecipeMatchesPrototypeData()
        {
            RecipeDefinition recipe = recipeDatabase.GetRecipe("trap");

            Assert.AreEqual("trap", recipe.ResultItemId);
            Assert.AreEqual(1, recipe.ResultAmount);
            Assert.AreEqual(2, recipe.Ingredients.Count);
            AssertIngredient(recipe, 0, "wood", 2);
            AssertIngredient(recipe, 1, "fiber", 2);
        }

        [Test]
        public void WallRecipeMatchesPrototypeData()
        {
            RecipeDefinition recipe = recipeDatabase.GetRecipe("wall");

            Assert.AreEqual("wall", recipe.ResultItemId);
            Assert.AreEqual(1, recipe.ResultAmount);
            Assert.AreEqual(1, recipe.Ingredients.Count);
            AssertIngredient(recipe, 0, "wood", 3);
        }

        [Test]
        public void StorageBoxRecipeMatchesPrototypeData()
        {
            RecipeDefinition recipe = recipeDatabase.GetRecipe("storage_box");

            Assert.AreEqual("storage_box", recipe.ResultItemId);
            Assert.AreEqual(1, recipe.ResultAmount);
            Assert.AreEqual(1, recipe.Ingredients.Count);
            AssertIngredient(recipe, 0, "wood", 4);
        }

        [Test]
        public void TentRecipeMatchesPrototypeData()
        {
            RecipeDefinition recipe = recipeDatabase.GetRecipe("tent");

            Assert.AreEqual("tent", recipe.ResultItemId);
            Assert.AreEqual(1, recipe.ResultAmount);
            Assert.AreEqual(2, recipe.Ingredients.Count);
            AssertIngredient(recipe, 0, "wood", 4);
            AssertIngredient(recipe, 1, "fiber", 3);
        }

        private static void AssertIngredient(RecipeDefinition recipe, int index, string expectedItemId, int expectedAmount)
        {
            Assert.AreEqual(expectedItemId, recipe.Ingredients[index].ItemId.ToString());
            Assert.AreEqual(expectedAmount, recipe.Ingredients[index].Amount);
        }
    }
}
