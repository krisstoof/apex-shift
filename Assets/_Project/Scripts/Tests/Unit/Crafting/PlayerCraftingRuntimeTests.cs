using ApexShift.Core.Crafting;
using ApexShift.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Crafting
{
    public sealed class PlayerCraftingRuntimeTests
    {
        [Test]
        public void CraftDefaultRecipeConsumesResourcesAndAddsSpear()
        {
            GameObject player = new GameObject("Player");
            try
            {
                PlayerInventoryRuntime inventory = player.AddComponent<PlayerInventoryRuntime>();
                inventory.EnsureInitialized();
                inventory.Inventory.AddItem("wood", 2);
                inventory.Inventory.AddItem("stone", 1);
                inventory.Inventory.AddItem("fiber", 1);

                PlayerCraftingRuntime crafting = player.AddComponent<PlayerCraftingRuntime>();
                crafting.SetInventoryRuntime(inventory);

                CraftingResult result = crafting.CraftDefaultRecipe();

                Assert.IsTrue(result.Succeeded);
                Assert.AreEqual(CraftingResultStatus.Success, result.Status);
                Assert.AreEqual("spear", result.ResultItemId);
                Assert.AreEqual(1, inventory.Inventory.GetAmount("spear"));
                Assert.AreEqual(0, inventory.Inventory.GetAmount("wood"));
                Assert.AreEqual(0, inventory.Inventory.GetAmount("stone"));
                Assert.AreEqual(0, inventory.Inventory.GetAmount("fiber"));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void CraftDefaultRecipeKeepsInventoryWhenIngredientsAreMissing()
        {
            GameObject player = new GameObject("Player");
            try
            {
                PlayerInventoryRuntime inventory = player.AddComponent<PlayerInventoryRuntime>();
                inventory.EnsureInitialized();
                inventory.Inventory.AddItem("wood", 2);

                PlayerCraftingRuntime crafting = player.AddComponent<PlayerCraftingRuntime>();
                crafting.SetInventoryRuntime(inventory);

                CraftingResult result = crafting.CraftDefaultRecipe();

                Assert.AreEqual(CraftingResultStatus.MissingIngredients, result.Status);
                Assert.AreEqual(0, inventory.Inventory.GetAmount("spear"));
                Assert.AreEqual(2, inventory.Inventory.GetAmount("wood"));
                Assert.AreEqual(0, inventory.Inventory.GetAmount("stone"));
                Assert.AreEqual(0, inventory.Inventory.GetAmount("fiber"));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }
    }
}
