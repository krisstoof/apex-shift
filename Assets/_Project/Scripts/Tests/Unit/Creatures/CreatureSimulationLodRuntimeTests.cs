using ApexShift.Runtime.Creatures;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Creatures
{
    public sealed class CreatureSimulationLodRuntimeTests
    {
        [Test]
        public void ResolveLevelMatchesGodotNearMediumFarThresholds()
        {
            Assert.AreEqual(
                CreatureSimulationLodLevel.Near,
                CreatureSimulationLodRuntime.ResolveLevel(50f, 90f, 180f));
            Assert.AreEqual(
                CreatureSimulationLodLevel.Medium,
                CreatureSimulationLodRuntime.ResolveLevel(120f, 90f, 180f));
            Assert.AreEqual(
                CreatureSimulationLodLevel.Far,
                CreatureSimulationLodRuntime.ResolveLevel(220f, 90f, 180f));
        }

        [Test]
        public void ResolveLevelForcesVarnakNearInsideSpecialDistance()
        {
            CreatureSimulationLodLevel level = CreatureSimulationLodRuntime.ResolveLevel(
                distanceToPlayer: 65f,
                nearDistance: 50f,
                mediumDistance: 180f,
                creatureType: "varnak",
                forceVarnakNearDistance: 70f);

            Assert.AreEqual(CreatureSimulationLodLevel.Near, level);
        }

        [Test]
        public void MediumLevelMultipliesAiInterval()
        {
            GameObject player = new GameObject("Player");
            GameObject creature = new GameObject("Creature_small_prey");
            try
            {
                player.transform.position = Vector3.zero;
                creature.transform.position = new Vector3(120f, 0f, 0f);
                var lod = creature.AddComponent<CreatureSimulationLodRuntime>();
                lod.SetPlayerForTests(player.transform);

                lod.Tick(0.1f, "small_prey");

                Assert.AreEqual(CreatureSimulationLodLevel.Medium, lod.Level);
                Assert.AreEqual(0.75f, lod.GetEffectiveAiInterval(0.25f), 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(creature);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void FarTickIsIntervalBased()
        {
            GameObject player = new GameObject("Player");
            GameObject creature = new GameObject("Creature_small_prey");
            try
            {
                player.transform.position = Vector3.zero;
                creature.transform.position = new Vector3(220f, 0f, 0f);
                var lod = creature.AddComponent<CreatureSimulationLodRuntime>();
                lod.SetPlayerForTests(player.transform);
                lod.Tick(0.1f, "small_prey");

                Assert.AreEqual(CreatureSimulationLodLevel.Far, lod.Level);
                Assert.IsFalse(lod.TryConsumeFarTick(1.0f, out _));
                Assert.IsTrue(lod.TryConsumeFarTick(2.1f, out float elapsed));
                Assert.AreEqual(3.1f, elapsed, 0.001f);
                Assert.AreEqual(1, lod.FarSimulationTickCount);
            }
            finally
            {
                Object.DestroyImmediate(creature);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void BackgroundTickIsSeparateFromFarTick()
        {
            GameObject creature = new GameObject("Creature_small_prey");
            try
            {
                var lod = creature.AddComponent<CreatureSimulationLodRuntime>();
                lod.SetVisibilityCulled(true);
                lod.Tick(0.1f, "small_prey");

                Assert.IsTrue(lod.IsBackgroundSimulationMode);
                Assert.IsFalse(lod.TryConsumeBackgroundTick(1f, out _));
                Assert.IsTrue(lod.TryConsumeBackgroundTick(2f, out _));
                Assert.AreEqual(1, lod.BackgroundSimulationTickCount);
            }
            finally
            {
                Object.DestroyImmediate(creature);
            }
        }
    }
}
