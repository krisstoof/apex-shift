using System.Collections.Generic;
using ApexShift.Core.Inventory;
using ApexShift.Core.Items;
using ApexShift.Core.Save;
using NUnit.Framework;

namespace ApexShift.Tests.EditMode.Inventory
{
    public class InventoryStateTests
    {
        private ItemDatabase itemDatabase;

        [SetUp]
        public void SetUp()
        {
            itemDatabase = ItemDatabase.CreateDefault();
        }

        [Test]
        public void DefaultInventoryHasNineSlots()
        {
            InventoryState inventory = new InventoryState(itemDatabase);

            Assert.AreEqual(9, inventory.SlotCount);
            Assert.AreEqual(9, inventory.GetEmptySlotCount());
            foreach (InventorySlot slot in inventory.Slots)
            {
                Assert.IsTrue(slot.IsEmpty);
            }
        }

        [Test]
        public void AddItemFillsEmptySlot()
        {
            InventoryState inventory = new InventoryState(itemDatabase);

            int remainder = inventory.AddItem("wood", 5);

            Assert.AreEqual(0, remainder);
            Assert.AreEqual(5, inventory.GetAmount("wood"));
            Assert.AreEqual(8, inventory.GetEmptySlotCount());
        }

        [Test]
        public void AddItemStacksIntoExistingStackFirst()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 5);

            inventory.AddItem("wood", 3);

            Assert.AreEqual(8, inventory.GetAmount("wood"));
            Assert.AreEqual(1, CountOccupiedSlots(inventory));
        }

        [Test]
        public void AddItemSplitsAcrossStacksBasedOnMaxStack()
        {
            InventoryState inventory = new InventoryState(itemDatabase);

            int remainder = inventory.AddItem("wood", 25);

            Assert.AreEqual(0, remainder);
            Assert.AreEqual(20, inventory.PeekSlotStack(0).Amount);
            Assert.AreEqual(5, inventory.PeekSlotStack(1).Amount);
        }

        [Test]
        public void AddItemReturnsRemainderWhenFull()
        {
            InventoryState inventory = new InventoryState(itemDatabase, 2);

            int remainder = inventory.AddItem("wood", 50);

            Assert.AreEqual(10, remainder);
            Assert.AreEqual(40, inventory.GetAmount("wood"));
        }

        [Test]
        public void UnknownItemIsRejectedByReturningFullAmount()
        {
            InventoryState inventory = new InventoryState(itemDatabase);

            int remainder = inventory.AddItem("unknown_item", 5);

            Assert.AreEqual(5, remainder);
            Assert.AreEqual(9, inventory.GetEmptySlotCount());
        }

        [Test]
        public void CanAddItemWorksLikeGodot()
        {
            InventoryState inventory = new InventoryState(itemDatabase, 1);

            Assert.IsTrue(inventory.CanAddItem("wood", 20));
            Assert.IsFalse(inventory.CanAddItem("wood", 21));
            Assert.IsFalse(inventory.CanAddItem("unknown_item", 1));
            Assert.IsFalse(inventory.CanAddItem("wood", 0));
        }

        [Test]
        public void AddItemFullStackIsAtomic()
        {
            InventoryState inventory = new InventoryState(itemDatabase, 1);
            inventory.AddItem("wood", 15);

            bool result = inventory.AddItemFullStack("wood", 10);

            Assert.IsFalse(result);
            Assert.AreEqual(15, inventory.GetAmount("wood"));
        }

        [Test]
        public void RemoveItemRequiresEnoughAmount()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 5);

            bool result = inventory.RemoveItem("wood", 8);

            Assert.IsFalse(result);
            Assert.AreEqual(5, inventory.GetAmount("wood"));
        }

        [Test]
        public void RemoveItemRemovesAcrossStacksAfterAvailabilityCheck()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 25);

            bool result = inventory.RemoveItem("wood", 22);

            Assert.IsTrue(result);
            Assert.AreEqual(3, inventory.GetAmount("wood"));
        }

        [Test]
        public void RemoveFromSlotRemovesPartialAmount()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 10);

            int removed = inventory.RemoveFromSlot(0, 4);

            Assert.AreEqual(4, removed);
            Assert.AreEqual(6, inventory.PeekSlotStack(0).Amount);
        }

        [Test]
        public void RemoveFromSlotWithAmountLessThanOrEqualZeroClearsEntireSlot()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 10);

            int removed = inventory.RemoveFromSlot(0, -1);

            Assert.AreEqual(10, removed);
            Assert.IsTrue(inventory.PeekSlotStack(0).Amount == 0);
        }

        [Test]
        public void GetAllItemsGroupsTotals()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 5);
            inventory.AddItem("stone", 3);
            inventory.AddItem("wood", 2);

            IReadOnlyDictionary<string, int> totals = inventory.GetAllItems();

            Assert.AreEqual(7, totals["wood"]);
            Assert.AreEqual(3, totals["stone"]);
        }

        [Test]
        public void SaveDataStoresNonEmptySlotsOnly()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            inventory.AddItem("wood", 5);
            inventory.AddItem("stone", 3);

            InventorySaveData saveData = inventory.ToSaveData();

            Assert.AreEqual(2, saveData.Slots.Count);
        }

        [Test]
        public void LoadFromSaveDataRestoresInventory()
        {
            InventoryState original = new InventoryState(itemDatabase);
            original.AddItem("wood", 5);
            original.AddItem("stone", 3);

            InventorySaveData saveData = original.ToSaveData();

            InventoryState loaded = new InventoryState(itemDatabase);
            loaded.LoadFromSaveData(saveData);

            Assert.AreEqual(5, loaded.GetAmount("wood"));
            Assert.AreEqual(3, loaded.GetAmount("stone"));
        }

        [Test]
        public void LoadFromSaveDataIgnoresUnknownItems()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            InventorySaveData saveData = new InventorySaveData(9, new[]
            {
                new InventorySlotSaveData(0, "unknown_item", 5)
            });

            inventory.LoadFromSaveData(saveData);

            Assert.AreEqual(0, inventory.GetAllItems().Count);
        }

        [Test]
        public void LoadFromSaveDataClampsAmountToMaxStack()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            InventorySaveData saveData = new InventorySaveData(9, new[]
            {
                new InventorySlotSaveData(0, "wood", 999)
            });

            inventory.LoadFromSaveData(saveData);

            Assert.AreEqual(20, inventory.PeekSlotStack(0).Amount);
        }

        [Test]
        public void InventoryChangedEventFiresOnlyWhenChanged()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            int fired = 0;
            inventory.InventoryChanged += () => fired++;

            inventory.AddItem("wood", 1);
            Assert.AreEqual(1, fired);

            inventory.AddItem("unknown_item", 1);
            Assert.AreEqual(1, fired);

            inventory.RemoveItem("wood", 1);
            Assert.AreEqual(2, fired);

            inventory.RemoveItem("wood", 1);
            Assert.AreEqual(2, fired);
        }

        [Test]
        public void LoadLegacyItemTotalsMigratesKnownItems()
        {
            InventoryState inventory = new InventoryState(itemDatabase);
            IReadOnlyDictionary<string, int> totals = new Dictionary<string, int>
            {
                { "wood", 5 },
                { "stone", 3 },
                { "unknown_item", 7 }
            };

            inventory.LoadLegacyItemTotals(totals);

            Assert.AreEqual(5, inventory.GetAmount("wood"));
            Assert.AreEqual(3, inventory.GetAmount("stone"));
            Assert.AreEqual(0, inventory.GetAmount("unknown_item"));
        }

        private static int CountOccupiedSlots(InventoryState inventory)
        {
            int count = 0;
            foreach (InventorySlot slot in inventory.Slots)
            {
                if (!slot.IsEmpty)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
