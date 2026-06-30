using ApexShift.Core.Save;
using ApexShift.Core.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Buildings
{
    public sealed class BuildingSaveDataTests
    {
        [Test]
        public void BuildingSaveData_NormalizesIdsAndYaw()
        {
            BuildingSaveData data = new BuildingSaveData(" id ", " CampFire ", 1f, 2f, 3f, -90f);

            Assert.AreEqual("id", data.InstanceId);
            Assert.AreEqual("CampFire", data.BuildingId);
            Assert.AreEqual(270f, data.RotationY, 0.001f);
            Assert.IsTrue(data.Active);
        }

        [Test]
        public void BuildingPlacementRuntime_CanSelectByBuildingId()
        {
            GameObject go = new GameObject("Placement");
            try
            {
                var runtime = go.AddComponent<ApexShift.Runtime.Buildings.BuildingPlacementRuntime>();
                bool selected = runtime.SelectBuilding("campfire");

                Assert.IsTrue(selected);
                Assert.AreEqual("campfire", runtime.SelectedBuildingId);
                Assert.IsNotEmpty(runtime.GetSelectedHint());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BuildingSaveData_PreservesStorageInventory()
        {
            InventorySaveData storage = new InventorySaveData(6, new[]
            {
                new InventorySlotSaveData(0, "wood", 4),
                new InventorySlotSaveData(1, "stone", 2)
            });

            BuildingSaveData data = new BuildingSaveData("box_1", "storage_box", 3f, 1f, 5f, 90f, true, storage);

            Assert.AreEqual(6, data.StorageInventory.SlotCount);
            Assert.AreEqual(2, data.StorageInventory.Slots.Count);
            Assert.AreEqual("wood", data.StorageInventory.Slots[0].ItemId);
            Assert.AreEqual(4, data.StorageInventory.Slots[0].Amount);
            Assert.AreEqual("stone", data.StorageInventory.Slots[1].ItemId);
            Assert.AreEqual(2, data.StorageInventory.Slots[1].Amount);
        }
    }
}
