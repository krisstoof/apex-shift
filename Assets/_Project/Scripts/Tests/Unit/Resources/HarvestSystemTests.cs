using ApexShift.Core.Inventory;
using ApexShift.Core.Items;
using ApexShift.Core.Resources;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Resources
{
    public class HarvestSystemTests
    {
        private ItemDatabase itemDatabase;
        private HarvestSystem harvestSystem;

        [SetUp]
        public void SetUp()
        {
            itemDatabase = ItemDatabase.CreateDefault();
            harvestSystem = new HarvestSystem();
        }

        [Test]
        public void DefaultTreeMatchesGodotDevelopDropTable()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("conifer_tree");

            Assert.AreEqual("conifer_tree", definition.Id.ToString());
            Assert.AreEqual("wood", definition.ItemId);
            Assert.AreEqual(4, definition.HarvestAmount);
            Assert.IsTrue(definition.PlayerHarvestable);
        }

        [Test]
        public void DefaultRockMatchesGodotDevelopDropTable()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("rock");

            Assert.AreEqual("stone", definition.ItemId);
            Assert.AreEqual(2, definition.HarvestAmount);
        }

        [Test]
        public void DefaultBushMatchesGodotDevelopDropTable()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("bush");

            Assert.AreEqual("fiber", definition.ItemId);
            Assert.AreEqual(2, definition.HarvestAmount);
        }

        [Test]
        public void HarvestAddsItemsToInventoryAndDepletesResource()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            ResourceState tree = ResourceDefinition.CreateDefault("conifer_tree").CreateState();

            HarvestResult result = harvestSystem.Harvest(tree, inventory);

            Assert.IsTrue(result.Success);
            Assert.AreEqual("wood", result.ItemId);
            Assert.AreEqual(4, result.AddedAmount);
            Assert.AreEqual(4, inventory.GetAmount("wood"));
            Assert.IsTrue(tree.IsDepleted);
            Assert.IsTrue(result.ShouldRemoveNode);
        }

        [Test]
        public void HarvestFailsWhenInventoryIsFull()
        {
            InventoryState inventory = new InventoryState(itemDatabase, 1);
            inventory.AddItem("stone", 20);
            ResourceState tree = ResourceDefinition.CreateDefault("conifer_tree").CreateState();

            HarvestResult result = harvestSystem.Harvest(tree, inventory);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Inventory full", result.Message);
            Assert.AreEqual(0, inventory.GetAmount("wood"));
            Assert.IsFalse(tree.IsDepleted);
        }

        [Test]
        public void NonHarvestableResourceCannotBeHarvested()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            ResourceState berries = ResourceDefinition.CreateDefault("berry_bush").CreateState();

            HarvestResult result = harvestSystem.Harvest(berries, inventory);

            Assert.IsFalse(result.Success);
            Assert.AreEqual("Cannot be gathered", result.Message);
            Assert.AreEqual(0, inventory.GetAmount("berries"));
        }

        [Test]
        public void EmptyResourceCannotBeHarvestedTwice()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            ResourceState rock = ResourceDefinition.CreateDefault("rock").CreateState();

            harvestSystem.Harvest(rock, inventory);
            HarvestResult secondResult = harvestSystem.Harvest(rock, inventory);

            Assert.IsFalse(secondResult.Success);
            Assert.AreEqual("Regrowing", secondResult.Message);
            Assert.AreEqual(2, inventory.GetAmount("stone"));
        }

        [Test]
        public void PromptUsesDropItemAndAmount()
        {
            ResourceState bush = ResourceDefinition.CreateDefault("bush").CreateState();

            string prompt = harvestSystem.GetPrompt(bush);

            Assert.AreEqual("E: gather fiber x2", prompt);
        }
    }
}
