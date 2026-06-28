using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.Events;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.GodotParity
{
    public sealed class DebugPerformanceGodotParityTests
    {
        [TearDown]
        public void TearDown()
        {
            RuntimeDebugSettings.RestoreDefaults();
            GameEventBus.ClearForTests();
        }

        [Test]
        public void MediumLodIncreasesAiDecisionInterval()
        {
            GameObject creature = new GameObject("Creature_grazer");
            GameObject player = new GameObject("Player");
            try
            {
                CreatureSimulationLodRuntime lod = creature.AddComponent<CreatureSimulationLodRuntime>();
                lod.ForceDistancesForTests(10f, 30f, 0f);
                player.transform.position = new Vector3(20f, 0f, 0f);
                lod.SetPlayerForTests(player.transform);

                lod.Tick(0.1f, "grazer");

                Assert.AreEqual(CreatureSimulationLodLevel.Medium, lod.Level);
                Assert.Greater(lod.GetEffectiveAiInterval(0.25f), 0.25f);
            }
            finally
            {
                Object.DestroyImmediate(creature);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void FarLodDoesNotRunFullAi()
        {
            GameObject creature = new GameObject("Creature_small_prey");
            GameObject player = new GameObject("Player");
            try
            {
                CreatureSimulationLodRuntime lod = creature.AddComponent<CreatureSimulationLodRuntime>();
                lod.ForceDistancesForTests(10f, 30f, 0f);
                player.transform.position = new Vector3(80f, 0f, 0f);
                lod.SetPlayerForTests(player.transform);

                lod.Tick(0.1f, "small_prey");

                Assert.AreEqual(CreatureSimulationLodLevel.Far, lod.Level);
                Assert.IsFalse(lod.ShouldRunFullAi);
            }
            finally
            {
                Object.DestroyImmediate(creature);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void DebugRefreshCanBeThrottledGlobally()
        {
            RuntimeDebugSettings.SetRefreshInterval(0.75f);
            RuntimeDebugSettings.SetDebugEnabled(false);

            Assert.AreEqual(0.75f, RuntimeDebugSettings.RefreshIntervalSeconds, 0.001f);
            Assert.IsFalse(RuntimeDebugSettings.DebugEnabled);
        }

        [Test]
        public void RecentEventLogKeepsBoundedSnapshotData()
        {
            GameEventBus.ConfigureLogCapacity(3);
            GameEventBus.PublishEcosystemTickAdvanced("a");
            GameEventBus.PublishEcosystemTickAdvanced("b");
            GameEventBus.PublishEcosystemTickAdvanced("c");
            GameEventBus.PublishEcosystemTickAdvanced("d");

            Assert.AreEqual(3, GameEventBus.RecentEventCount);
            Assert.AreEqual("b", GameEventBus.RecentEvents[0].biomeId);
            Assert.AreEqual("d", GameEventBus.RecentEvents[2].biomeId);
        }
    }
}
