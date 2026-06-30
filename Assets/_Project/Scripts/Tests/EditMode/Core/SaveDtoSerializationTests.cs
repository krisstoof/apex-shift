using System.Collections.Generic;
using ApexShift.Core.Save;
using ApexShift.Infrastructure.Save;
using NUnit.Framework;

namespace ApexShift.Tests.EditMode.Core
{
    public sealed class SaveDtoSerializationTests
    {
        [Test]
        public void UnityJsonSerializer_RoundTripsGameSaveData()
        {
            InventorySaveData inventory = new InventorySaveData(
                slotCount: 4,
                slots: new List<InventorySlotSaveData>
                {
                    new InventorySlotSaveData(0, "wood", 3),
                    new InventorySlotSaveData(2, "stone", 2)
                });

            SurvivalSaveData survival = new SurvivalSaveData(
                health: 75f,
                hunger: 50f,
                stamina: 25f,
                rest: 90f,
                campfireRegenActive: true,
                campfireRegenDistance: 4.5f,
                godMode: true);
            survival.SetPosition(1f, 2f, 3f);

            WorldSaveData world = new WorldSaveData(
                seed: 12345,
                day: 2,
                timeOfDay: 0.25f,
                resources: new List<ResourceSaveData>
                {
                    new ResourceSaveData("tree_1", "conifer_tree", 1f, 0f, 2f, 0, 4, depleted: true)
                },
                biomeStates: new List<BiomeEcosystemSaveData>(),
                creatureStates: new List<CreatureSaveData>(),
                buildingStates: new List<BuildingSaveData>(),
                ecosystemTickTimer: 0f,
                ecosystemStateSource: "generated");

            GameSaveData original = new GameSaveData(inventory, survival, world);
            UnityJsonGameSaveSerializer serializer = new UnityJsonGameSaveSerializer();

            string payload = serializer.Serialize(original);
            GameSaveData restored = serializer.Deserialize(payload);

            Assert.IsNotNull(payload);
            Assert.IsTrue(payload.Contains("wood"));
            Assert.AreEqual(4, restored.Inventory.SlotCount);
            Assert.AreEqual(2, restored.Inventory.Slots.Count);
            Assert.AreEqual(75f, restored.Survival.Health);
            Assert.IsTrue(restored.Survival.GodMode);
            Assert.IsTrue(restored.Survival.hasPosition);
            Assert.AreEqual(12345, restored.World.Seed);
            Assert.AreEqual(2, restored.World.Day);
            Assert.AreEqual(0.25f, restored.World.TimeOfDay, 0.001f);
            Assert.AreEqual(1, restored.World.Resources.Count);
            Assert.IsTrue(restored.Version.IsCompatible);
        }

        [Test]
        public void Deserialize_EmptyPayload_ReturnsDefaultSave()
        {
            UnityJsonGameSaveSerializer serializer = new UnityJsonGameSaveSerializer();

            GameSaveData save = serializer.Deserialize(string.Empty);

            Assert.IsNotNull(save);
            Assert.IsNotNull(save.Inventory);
            Assert.IsNotNull(save.Survival);
            Assert.IsNotNull(save.World);
            Assert.IsTrue(save.Version.IsCompatible);
        }
    }
}
