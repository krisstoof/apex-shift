using ApexShift.Runtime.Config;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Regression
{
    public sealed class BalanceConfigParityTests
    {
        [Test]
        public void SpeciesDefinitionChangesCreatureNeedsRuntime()
        {
            GameObject creature = new GameObject("ConfigGrazer");
            SpeciesDefinition definition = ScriptableObject.CreateInstance<SpeciesDefinition>();
            try
            {
                definition.Configure("grazer", "Config Grazer", 77f, 200f, 5f, 40f, 80f, 140f, 33f, 66f, 22f, 11f, 10f, 20f, 0.20f, 0.90f, 0.30f);
                CreatureNeedsRuntime needs = creature.AddComponent<CreatureNeedsRuntime>();
                needs.Configure("grazer", definition);

                Assert.AreEqual(200f, needs.State.MaxHunger, 0.001f);
                Assert.AreEqual(5f, needs.State.HungerGrowthRate, 0.001f);
                Assert.AreEqual(0.90f, needs.Diet.MeatPreference, 0.001f);
                Assert.AreEqual(22f, needs.PreySeekHungerThreshold, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(creature);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void SpeciesDefinitionChangesCreatureHealthRuntime()
        {
            GameObject creature = new GameObject("ConfigVarnak");
            SpeciesDefinition definition = ScriptableObject.CreateInstance<SpeciesDefinition>();
            try
            {
                definition.Configure("varnak", "Config Varnak", 123f, 100f, 18f, 32f, 58f, 80f, 140f, 200f, 38f, 22f, 36f, 58f, 0f, 1f, 0.45f);
                CreatureHealthRuntime health = creature.AddComponent<CreatureHealthRuntime>();
                health.Configure("varnak", definition);

                Assert.AreEqual(123f, health.MaxHealth, 0.001f);
                Assert.AreEqual(123f, health.CurrentHealth, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(creature);
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void MissingConfigFallsBackWithoutCrashing()
        {
            GameObject creature = new GameObject("FallbackSmallPrey");
            try
            {
                CreatureNeedsRuntime needs = creature.AddComponent<CreatureNeedsRuntime>();
                Assert.DoesNotThrow(() => needs.Configure("small_prey"));
                Assert.NotNull(needs.State);
                Assert.Greater(needs.State.MaxHunger, 0f);
                Assert.Greater(needs.Diet.PlantPreference, 0f);
            }
            finally
            {
                Object.DestroyImmediate(creature);
            }
        }

        [Test]
        public void LodConfigChangesRuntimeThresholds()
        {
            GameObject creature = new GameObject("Creature_grazer");
            GameObject player = new GameObject("Player");
            CreatureSimulationLodConfig config = ScriptableObject.CreateInstance<CreatureSimulationLodConfig>();
            try
            {
                config.Configure(5f, 10f, 2f, 4f, 3f, 7f, 8f, false);
                CreatureSimulationLodRuntime lod = creature.AddComponent<CreatureSimulationLodRuntime>();
                lod.ApplyConfig(config);
                lod.SetPlayerForTests(player.transform);
                player.transform.position = new Vector3(7f, 0f, 0f);
                lod.Tick(0.1f, "grazer");

                Assert.AreEqual(CreatureSimulationLodLevel.Medium, lod.Level);
                Assert.AreEqual(1f, lod.GetEffectiveAiInterval(0.25f), 0.001f);
                Assert.AreEqual(7f, lod.FarUpdateIntervalSeconds, 0.001f);
                Assert.IsFalse(lod.DebugEnabled);
            }
            finally
            {
                Object.DestroyImmediate(creature);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void GameBalanceConfigValidatorReportsMissingAssets()
        {
            GameBalanceConfig config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                Assert.IsNotEmpty(config.ValidateConfig());
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
