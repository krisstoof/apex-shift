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
            Assert.AreEqual(0, restored.World.TreeStates.Count);
            Assert.IsNotNull(restored.World.StoryState);
            Assert.AreEqual("survive_crash", restored.World.StoryState.CurrentStageId);
            Assert.IsEmpty(restored.World.StoryState.CompletedMilestoneIds);
            Assert.IsTrue(restored.Version.IsCompatible);
        }

        [Test]
        public void Deserialize_OldSaveWithoutTreeStatesRemainsCompatible()
        {
            const string oldPayload = "{\"world\":{\"seed\":321,\"day\":3,\"timeOfDay\":0.5,\"resources\":[]}}";
            UnityJsonGameSaveSerializer serializer = new UnityJsonGameSaveSerializer();

            GameSaveData restored = serializer.Deserialize(oldPayload);

            Assert.AreEqual(321, restored.World.Seed);
            Assert.AreEqual(0, restored.World.TreeStates.Count);
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

        [Test]
        public void StoryStateRoundTripsStableStageAndMilestones()
        {
            GameSaveData original = new GameSaveData();
            original.World.storyState = new StorySaveData
            {
                currentStageId = "locate_smuggler_base",
                completedMilestoneIds = new List<string>
                {
                    "crash_survived", "survival_established", "raft_built", "raft_escape_failed",
                    "landmark_discovered:smuggler_cache"
                }
            };
            UnityJsonGameSaveSerializer serializer = new UnityJsonGameSaveSerializer();
            GameSaveData restored = serializer.Deserialize(serializer.Serialize(original));
            Assert.AreEqual(original.World.StoryState.CurrentStageId, restored.World.StoryState.CurrentStageId);
            CollectionAssert.AreEqual(original.World.StoryState.CompletedMilestoneIds, restored.World.StoryState.CompletedMilestoneIds);
        }

        [Test]
        public void LegacySaveWithoutStoryLoadsFreshInitialState()
        {
            // Pre-story schema: intentionally omits story/clue/location/boat/landmark fields.
            const string legacyJson = @"{""version"":{""major"":1,""minor"":0,""patch"":0},
                ""inventory"":{""slotCount"":4,""slots"":[]},
                ""world"":{""seed"":321,""day"":3,""timeOfDay"":0.5,""resources"":[],
                ""biomeStates"":[{""biomeId"":""westwood"",""plantBiomass"":70,""varnakPopulation"":2}]}}";
            GameSaveData restored = new UnityJsonGameSaveSerializer().Deserialize(
                legacyJson);
            Assert.AreEqual(321, restored.World.Seed);
            Assert.AreEqual(3, restored.World.Day);
            Assert.AreEqual(0.5f, restored.World.TimeOfDay);
            Assert.AreEqual("survive_crash", restored.World.StoryState.CurrentStageId);
            Assert.IsEmpty(restored.World.StoryState.CompletedMilestoneIds);
            Assert.AreEqual("island", restored.World.playerLocation.areaId);
            Assert.IsEmpty(restored.World.escapeBoatState.collectedRequirementSourceIds);
            Assert.IsEmpty(restored.World.clueStates);
            Assert.IsEmpty(restored.World.landmarkStates);
            Assert.IsEmpty(restored.World.treeStates);
            Assert.IsEmpty(restored.World.creatureStates);
            Assert.IsEmpty(restored.World.buildingStates);
            Assert.IsEmpty(restored.World.pickups);
            Assert.AreEqual("westwood", restored.World.biomeStates[0].biomeId);
            Assert.AreEqual(2f, restored.World.biomeStates[0].varnakPopulation);
            Assert.IsTrue(restored.Version.IsCompatible);
            Assert.AreNotSame(StorySaveData.Default, StorySaveData.Default);
        }

        [Test]
        public void DeserializeExplicitNullCollectionsNormalizesSerializedFieldsWithoutGetterSideEffects()
        {
            var restored = new UnityJsonGameSaveSerializer().Deserialize(@"{
                ""inventory"":{""slots"":null},""world"":{""day"":-2,""timeOfDay"":-0.25,
                ""resources"":null,""treeStates"":null,""pickups"":null,""biomeStates"":null,
                ""creatureStates"":null,""buildingStates"":null,""landmarkStates"":null,
                ""clueStates"":null,""storyState"":null,""playerLocation"":null,
                ""escapeBoatState"":null,""ecosystemStateSource"":"" ""}}");
            var world = restored.world;
            Assert.NotNull(restored.inventory.slots);
            foreach (var collection in new System.Collections.ICollection[] { world.resources, world.treeStates,
                world.pickups, world.biomeStates, world.creatureStates, world.buildingStates,
                world.landmarkStates, world.clueStates, world.storyState.completedMilestoneIds,
                world.escapeBoatState.collectedRequirementSourceIds })
                Assert.That(collection, Is.Not.Null.And.Empty);
            Assert.AreEqual("survive_crash", world.storyState.currentStageId);
            Assert.AreEqual("island", world.playerLocation.areaId);
            Assert.AreEqual(1, world.day);
            Assert.AreEqual(0.75f, world.timeOfDay);
            Assert.AreEqual("generated", world.ecosystemStateSource);
            world.storyState = new StorySaveData { currentStageId = " ", completedMilestoneIds = null };
            world.escapeBoatState = new EscapeBoatSaveData { collectedRequirementSourceIds = null };
            world.timeOfDay = float.NaN; world.ecosystemTickTimer = float.PositiveInfinity;
            restored.EnsureDefaults();
            Assert.AreEqual(0f, world.timeOfDay); Assert.AreEqual(0f, world.ecosystemTickTimer);
            Assert.AreEqual("survive_crash", world.storyState.currentStageId);
            Assert.IsEmpty(world.storyState.completedMilestoneIds);
            Assert.IsEmpty(world.escapeBoatState.collectedRequirementSourceIds);
            world.timeOfDay = float.NegativeInfinity;
            restored.EnsureDefaults(); Assert.AreEqual(0f, world.timeOfDay);
        }

        [TestCase("escape_island")]
        [TestCase("completed")]
        public void FullStorySaveRoundTripsLocationCluesAndBoatSources(string stage)
        {
            var original = new GameSaveData();
            original.world.storyState = new StorySaveData { currentStageId = stage,
                completedMilestoneIds = new List<string> { "crash_survived", "survival_established",
                    "raft_built", "raft_escape_failed", "human_traces_found",
                    "landmark_discovered:base_entrance", "base_access_gained", "boat_prepared" } };
            if (stage == "completed") original.world.storyState.completedMilestoneIds.Add("island_escaped");
            original.world.clueStates.Add(new StoryClueSaveData("smuggler_cache_manifest", true, 1f, 2f, 3f));
            original.world.playerLocation = new PlayerLocationSaveData { areaId = "smuggler_base",
                hasLocalPosition = true, localX = 1f, localY = 0.1f, localZ = 22f };
            original.world.escapeBoatState.collectedRequirementSourceIds.AddRange(
                new[] { "smuggler_base_fuel", "smuggler_base_battery" });
            var serializer = new UnityJsonGameSaveSerializer();
            var restored = serializer.Deserialize(serializer.Serialize(original));
            Assert.AreEqual(stage, restored.world.storyState.currentStageId);
            CollectionAssert.AreEqual(original.world.storyState.completedMilestoneIds, restored.world.storyState.completedMilestoneIds);
            Assert.AreEqual("smuggler_cache_manifest", restored.world.clueStates[0].clueId);
            Assert.IsTrue(restored.world.clueStates[0].discovered);
            Assert.AreEqual(3f, restored.world.clueStates[0].z);
            Assert.AreEqual("smuggler_base", restored.world.playerLocation.areaId);
            Assert.IsTrue(restored.world.playerLocation.hasLocalPosition);
            Assert.AreEqual(22f, restored.world.playerLocation.localZ);
            CollectionAssert.AreEqual(original.world.escapeBoatState.CollectedRequirementSourceIds,
                restored.world.escapeBoatState.CollectedRequirementSourceIds);
            Assert.IsTrue(restored.Version.IsCompatible);
        }
    }
}
