using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.Ecosystem;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Regression
{
    public sealed class DebugDataParityTests
    {
        [Test]
        public void CreatureDebugDataHandlesMissingTargetsAndOptionalComponents()
        {
            GameObject creature = new GameObject("Creature_small_prey");
            try
            {
                creature.AddComponent<CreatureAgentView>().Configure("small_prey");

                CreatureDebugData data = CreatureDebugData.Capture(creature);

                Assert.AreEqual("small_prey", data.speciesId);
                Assert.AreEqual("none", data.currentTarget);
                Assert.AreEqual("none", data.targetDetails);
                Assert.AreEqual("missing", data.navStatus);
                Assert.DoesNotThrow(() => data.ToOverlayText());
            }
            finally
            {
                Object.DestroyImmediate(creature);
            }
        }

        [Test]
        public void CreatureDebugDataIncludesNeedsHealthAndSimulationCounters()
        {
            GameObject creature = new GameObject("Creature_grazer");
            try
            {
                creature.AddComponent<CreatureNavigationAdapter>();
                creature.AddComponent<CreatureAgentView>().Configure("grazer");
                CreatureNeedsRuntime needs = creature.AddComponent<CreatureNeedsRuntime>();
                needs.Configure("grazer");
                needs.State.SetHunger(70f);
                creature.AddComponent<CreatureHealthRuntime>().Configure("grazer");
                creature.AddComponent<CreatureSimulationLodRuntime>();
                creature.AddComponent<CreatureBehaviorBrain>();

                CreatureDebugData data = CreatureDebugData.Capture(creature);

                Assert.AreEqual("grazer", data.speciesId);
                Assert.AreEqual(70f, data.hunger, 0.001f);
                Assert.Greater(data.maxHealth, 0f);
                Assert.AreEqual("near", data.simulationLevel);
                StringAssert.Contains("hun:", data.ToOverlayText());
            }
            finally
            {
                Object.DestroyImmediate(creature);
            }
        }

        [Test]
        public void CreatureDebugDataReportsTargetBiomassSafely()
        {
            GameObject creature = new GameObject("Creature_varnak");
            GameObject meat = new GameObject("MeatDrop_test");
            try
            {
                creature.AddComponent<CreatureNavigationAdapter>();
                creature.AddComponent<CreatureAgentView>().Configure("varnak");
                CreatureNeedsRuntime needs = creature.AddComponent<CreatureNeedsRuntime>();
                needs.Configure("varnak");
                CreatureBehaviorBrain brain = creature.AddComponent<CreatureBehaviorBrain>();
                creature.AddComponent<CreatureHealthRuntime>().Configure("varnak");
                creature.AddComponent<CreatureSimulationLodRuntime>();

                meat.transform.position = new Vector3(2f, 0f, 0f);
                meat.AddComponent<FoodSourceView>().Configure("meat_test", "Meat", ApexShift.Core.Ecosystem.FoodKind.Meat, 12f, 8f);

                CreatureDebugData data = CreatureDebugData.Capture(creature);

                Assert.NotNull(brain);
                Assert.DoesNotThrow(() => data.ToOverlayText());
            }
            finally
            {
                Object.DestroyImmediate(meat);
                Object.DestroyImmediate(creature);
            }
        }

        [Test]
        public void RuntimeDebugSettingsCanDisableDebugWithoutRemovingComponents()
        {
            RuntimeDebugSettings.RestoreDefaults();
            RuntimeDebugSettings.SetDebugEnabled(false);
            RuntimeDebugSettings.SetCreatureFramesEnabled(false);
            RuntimeDebugSettings.SetEcosystemOverlayEnabled(false);

            Assert.IsFalse(RuntimeDebugSettings.DebugEnabled);
            Assert.IsFalse(RuntimeDebugSettings.CreatureFramesEnabled);
            Assert.IsFalse(RuntimeDebugSettings.EcosystemOverlayEnabled);

            RuntimeDebugSettings.RestoreDefaults();
        }
    }
}
