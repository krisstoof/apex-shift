using System.Collections.Generic;
using ApexShift.Core.Save;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Resources;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Regression
{
    public sealed class GameplayEventBusParityTests
    {
        [SetUp]
        public void SetUp()
        {
            GameEventBus.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GameEventBus.ClearForTests();
        }

        [Test]
        public void PublishingWithoutListenersDoesNotThrowAndStoresRecentEvent()
        {
            Assert.DoesNotThrow(() => GameEventBus.PublishCreatureEvent(
                GameplayEventKind.SmallPreyConsumedPlants,
                Vector3.one,
                "forest",
                "small_prey",
                "plants",
                amount: 0.4f,
                nutrition: 2f,
                biomassImpact: 0.4f,
                message: "small_prey_consumed_plants"));

            Assert.AreEqual(1, GameEventBus.RecentEventCount);
            Assert.AreEqual(GameplayEventKind.SmallPreyConsumedPlants, GameEventBus.RecentEvents[0].kind);
            Assert.AreEqual("forest", GameEventBus.RecentEvents[0].biomeId);
        }

        [Test]
        public void SubscriberReceivesResourceHarvestedPayload()
        {
            List<GameplayEvent> received = new List<GameplayEvent>();
            using (GameEventBus.Subscribe(received.Add))
            {
                GameEventBus.PublishResourceHarvested(new Vector3(2f, 0f, 3f), "meadow", "berry_bush", "berries", 3f, "harvested_berries");
            }

            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(GameplayEventKind.ResourceHarvested, received[0].kind);
            Assert.AreEqual("berry_bush", received[0].resourceId);
            Assert.AreEqual("berries", received[0].itemId);
            Assert.AreEqual(3f, received[0].amount, 0.001f);
            Assert.IsTrue(GameEventBus.GetRecentEventLines(4)[0].Contains("ResourceHarvested"));
        }

        [Test]
        public void EcosystemDirectorEmitsBiomassChangedEvent()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            List<GameplayEvent> received = new List<GameplayEvent>();
            try
            {
                EcosystemDirectorRuntime director = ecosystemObject.AddComponent<EcosystemDirectorRuntime>();
                director.LoadSaveData(new[]
                {
                    new BiomeEcosystemSaveData("default", "Default", 100f, 100f, 6f, 0f, 0f, 0f, 4f, 3f, 1f, 1, 1, 1, "HERBIVORE", "healthy")
                });

                using (GameEventBus.Subscribe(received.Add))
                {
                    director.DebugReducePlantBiomass(Vector3.zero, 2.5f);
                }

                Assert.AreEqual(1, received.Count);
                Assert.AreEqual(GameplayEventKind.BiomassChanged, received[0].kind);
                Assert.AreEqual("default", received[0].biomeId);
                Assert.AreEqual(2.5f, received[0].biomassImpact, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [Test]
        public void ResourceNodeInteractEmitsHarvestEvent()
        {
            GameObject resourceObject = new GameObject("ConiferTree");
            GameObject actorObject = new GameObject("Player");
            List<GameplayEvent> received = new List<GameplayEvent>();
            try
            {
                ResourceNodeView node = resourceObject.AddComponent<ResourceNodeView>();
                node.ConfigureDefault("conifer_tree");

                PlayerInventoryRuntime inventory = actorObject.AddComponent<PlayerInventoryRuntime>();
                inventory.EnsureInitialized();

                using (GameEventBus.Subscribe(received.Add))
                {
                    Assert.IsTrue(node.Interact(actorObject));
                }

                Assert.AreEqual(1, received.Count);
                Assert.AreEqual(GameplayEventKind.ResourceHarvested, received[0].kind);
                Assert.AreEqual("conifer_tree", received[0].resourceId);
                Assert.Greater(received[0].amount, 0f);
            }
            finally
            {
                Object.DestroyImmediate(actorObject);
                Object.DestroyImmediate(resourceObject);
            }
        }

        [Test]
        public void RecentEventLogKeepsConfiguredCapacity()
        {
            GameEventBus.ConfigureLogCapacity(2);
            GameEventBus.PublishEcosystemTickAdvanced("a");
            GameEventBus.PublishEcosystemTickAdvanced("b");
            GameEventBus.PublishEcosystemTickAdvanced("c");

            Assert.AreEqual(2, GameEventBus.RecentEventCount);
            Assert.AreEqual("b", GameEventBus.RecentEvents[0].biomeId);
            Assert.AreEqual("c", GameEventBus.RecentEvents[1].biomeId);
        }
    }
}
