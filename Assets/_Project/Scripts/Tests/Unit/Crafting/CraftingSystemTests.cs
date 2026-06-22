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
        public void TorchRecipeMatchesPrototypeData()
        {
            RecipeDefinition torch = recipeDatabase.GetRecipe("torch");

            Assert.AreEqual("torch", torch.ResultItemId);
            Assert.AreEqual(1, torch.ResultAmount);
            Assert.AreEqual(2, torch.Ingredients.Count);
            Assert.AreEqual("wood", torch.Ingredients[0].ItemId.ToString());
            Assert.AreEqual(1, torch.Ingredients[0].Amount);
            Assert.AreEqual("fiber", torch.Ingredients[1].ItemId.ToString());
            Assert.AreEqual(1, torch.Ingredients[1].Amount);
        }

        [Test]
        public void SpearRecipeMatchesPrototypeData()
        {
            RecipeDefinition spear = recipeDatabase.GetRecipe("spear");

            Assert.AreEqual("spear", spear.ResultItemId);
            Assert.AreEqual(1, spear.ResultAmount);
            Assert.AreEqual(3, spear.Ingredients.Count);
            Assert.AreEqual("wood", spear.Ingredients[0].ItemId.ToString());
            Assert.AreEqual(2, spear.Ingredients[0].Amount);
            Assert.AreEqual("stone", spear.Ingredients[1].ItemId.ToString());
            Assert.AreEqual(1, spear.Ingredients[1].Amount);
            Assert.AreEqual("fiber", spear.Ingredients[2].ItemId.ToString());
            Assert.AreEqual(1, spear.Ingredients[2].Amount);
        }
    }
}
