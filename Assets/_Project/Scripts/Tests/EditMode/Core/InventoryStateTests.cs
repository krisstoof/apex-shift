using ApexShift.Core.Inventory;
using ApexShift.Core.Items;
using NUnit.Framework;

namespace ApexShift.Tests.EditMode.Core
{
    public sealed class InventoryStateTests
    {
        [Test]
        public void AddItem_StacksItemsAndReportsRemainder()
        {
            ItemDatabase itemDatabase = ItemDatabase.CreateDefault();
            InventoryState inventory = new InventoryState(itemDatabase, slotCount: 2);

            int firstRemainder = inventory.AddItem("wood", 15);
            int secondRemainder = inventory.AddItem("wood", 10);

            Assert.AreEqual(0, firstRemainder);
            Assert.AreEqual(0, secondRemainder);
            Assert.AreEqual(25, inventory.GetAmount("wood"));
            Assert.AreEqual(0, inventory.GetEmptySlotCount());
        }

        [Test]
        public void AddItem_ReturnsOriginalAmount_WhenItemIsUnknown()
        {
            ItemDatabase itemDatabase = ItemDatabase.CreateDefault();
            InventoryState inventory = new InventoryState(itemDatabase, slotCount: 2);

            int remainder = inventory.AddItem("unknown_item", 5);

            Assert.AreEqual(5, remainder);
            Assert.AreEqual(2, inventory.GetEmptySlotCount());
        }

        [Test]
        public void SaveLoad_RestoresKnownItemsAndSlots()
        {
            ItemDatabase itemDatabase = ItemDatabase.CreateDefault();
            InventoryState source = new InventoryState(itemDatabase, slotCount: 3);
            source.AddItem("wood", 3);
            source.AddItem("stone", 2);

            InventoryState restored = new InventoryState(itemDatabase, slotCount: 3);
            restored.LoadFromSaveData(source.ToSaveData());

            Assert.AreEqual(3, restored.GetAmount("wood"));
            Assert.AreEqual(2, restored.GetAmount("stone"));
            Assert.AreEqual(1, restored.GetEmptySlotCount());
        }

        [Test]
        public void RemoveItem_ConsumesAcrossStacks()
        {
            ItemDatabase itemDatabase = ItemDatabase.CreateDefault();
            InventoryState inventory = new InventoryState(itemDatabase, slotCount: 2);
            inventory.AddItem("wood", 25);

            bool removed = inventory.RemoveItem("wood", 22);

            Assert.IsTrue(removed);
            Assert.AreEqual(3, inventory.GetAmount("wood"));
            Assert.AreEqual(1, inventory.GetEmptySlotCount());
        }
    }
}
