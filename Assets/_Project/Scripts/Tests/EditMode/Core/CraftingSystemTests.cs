using ApexShift.Core.Crafting;
using ApexShift.Core.Inventory;
using ApexShift.Core.Items;
using NUnit.Framework;

namespace ApexShift.Tests.EditMode.Core
{
    public sealed class CraftingSystemTests
    {
        [Test]
        public void Craft_CampfireConsumesIngredientsAndAddsResult()
        {
            ItemDatabase itemDatabase = ItemDatabase.CreateDefault();
            RecipeDatabase recipeDatabase = RecipeDatabase.CreateDefault(itemDatabase);
            CraftingSystem crafting = new CraftingSystem(recipeDatabase, itemDatabase);
            InventoryState inventory = new InventoryState(itemDatabase, slotCount: 6);
            inventory.AddItem("wood", 3);
            inventory.AddItem("stone", 2);

            CraftingResult result = crafting.Craft(inventory, "campfire");

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual("campfire", result.ResultItemId);
            Assert.AreEqual(1, inventory.GetAmount("campfire"));
            Assert.AreEqual(0, inventory.GetAmount("wood"));
            Assert.AreEqual(0, inventory.GetAmount("stone"));
        }

        [Test]
        public void Craft_ReturnsMissingIngredients_WhenInventoryLacksInputs()
        {
            ItemDatabase itemDatabase = ItemDatabase.CreateDefault();
            RecipeDatabase recipeDatabase = RecipeDatabase.CreateDefault(itemDatabase);
            CraftingSystem crafting = new CraftingSystem(recipeDatabase, itemDatabase);
            InventoryState inventory = new InventoryState(itemDatabase, slotCount: 6);
            inventory.AddItem("wood", 1);

            CraftingResult result = crafting.Craft(inventory, "campfire");

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual(CraftingResultStatus.MissingIngredients, result.Status);
            Assert.AreEqual(0, inventory.GetAmount("campfire"));
            Assert.AreEqual(1, inventory.GetAmount("wood"));
        }

        [Test]
        public void CanCraft_ReturnsFalse_ForUnknownRecipe()
        {
            ItemDatabase itemDatabase = ItemDatabase.CreateDefault();
            RecipeDatabase recipeDatabase = RecipeDatabase.CreateDefault(itemDatabase);
            CraftingSystem crafting = new CraftingSystem(recipeDatabase, itemDatabase);
            InventoryState inventory = new InventoryState(itemDatabase);

            Assert.IsFalse(crafting.CanCraft(inventory, "not_a_recipe"));
        }
    }
}
