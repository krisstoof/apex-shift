using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Inventory;
using ApexShift.Core.Save;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Config;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Save;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Query;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class HabitatCreatureLifecycleTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void ApplyLoadedStatePreservesSpeciesRoleAndHabitatWithLegacyCompatibility(bool legacy)
        {
            var root = new GameObject("HabitatCreatureSaveFixture");
            GameObject restored = null;
            try
            {
                var ecosystem = root.AddComponent<EcosystemRuntime>();
                root.AddComponent<WorldQueryRuntime>();
                var service = root.AddComponent<GameSaveService>();
                var data = new CreatureSaveData(legacy ? "varnak" : "island_predator",
                    legacy ? null : "island_predator", 1, 8f, 3f, 9f, 41f, 90f, false,
                    73f, 0.4f, "Scavenge", "westwood", "westwood", "westwood", "saved", "meat_drop", 0.8f, "CARNIVORE", 0.42f);
                if (!legacy)
                {
                    data.currentHabitatId = "wet_jungle";
                    data.homeHabitatId = "lowland_jungle";
                    data.populationHabitatId = "jungle_interior";
                }
                var world = new WorldSaveData(0, 1, 0f, new List<ResourceSaveData>(),
                    new List<BiomeEcosystemSaveData>(), new[]{data}, new List<BuildingSaveData>(), 0f, "save");
                Assert.That(service.ApplyLoadedState(new GameSaveData(InventorySaveData.Empty, SurvivalSaveData.Default, world)), Is.True);
                var agent = ecosystem.Creatures.Single(c=>c != null);
                restored = agent.gameObject;
                Assert.That(agent.SpeciesId, Is.EqualTo("island_predator"));
                Assert.That(agent.CreatureId, Is.EqualTo(agent.SpeciesId));
                Assert.That(agent.Role, Is.EqualTo(CreatureRole.Predator));
                Assert.That(agent.transform.position, Is.EqualTo(new Vector3(8f, 3f, 9f)));
                Assert.That(agent.GetComponent<CreatureHealthRuntime>().CurrentHealth, Is.EqualTo(41f));
                Assert.That(agent.GetComponent<CreatureNeedsRuntime>().State.Hunger, Is.EqualTo(73f));
                Assert.That(agent.GetComponent<CreatureNeedsRuntime>().State.Energy, Is.EqualTo(0.4f).Within(0.001f));
                var brain = agent.GetComponent<CreatureBehaviorBrain>();
                Assert.That(brain.State, Is.EqualTo(CreatureBehaviorState.Scavenge));
                Assert.That(brain.CurrentHabitatId, Is.EqualTo(legacy ? "jungle_interior" : "wet_jungle"));
                Assert.That(brain.HomeHabitatId, Is.EqualTo(legacy ? "jungle_interior" : "lowland_jungle"));
                Assert.That(brain.PopulationHabitatId, Is.EqualTo("jungle_interior"));
                Assert.That(brain.AttackCooldown, Is.EqualTo(0.8f));
                Assert.That(brain.HuntDrive, Is.EqualTo(0.42f));
                var captured = service.CaptureCurrentState().World.CreatureStates.Single();
                Assert.That(captured.SpeciesId, Is.EqualTo("island_predator"));
                Assert.That(captured.CurrentHabitatId, Is.EqualTo(brain.CurrentHabitatId));
                Assert.That(captured.CurrentBiomeId, Is.Null);
                Assert.That(agent.GetComponent<CreatureHitboxRuntime>().CombatCollider.enabled, Is.True);
            }
            finally { if (restored != null) Object.DestroyImmediate(restored); Object.DestroyImmediate(root); }
        }
        [Test] public void CustomPredatorRoleBlocksTentAndSelectsOnlyPreyRoles()
        {
            var root = new GameObject("RoleFixture");
            var predator = new GameObject("CustomPredator");
            var prey = new GameObject("CustomPrey");
            var forager = new GameObject("CustomForager");
            var player = new GameObject("SleepPlayer");
            var definition = SpeciesDefinition.CreateDefault("island_predator");
            try
            {
                definition.Configure("custom_hunter", "Custom Hunter", 90f,100f,18f,32f,58f,80f,140f,200f,38f,22f,36f,58f,0f,1f,0.45f);
                var ecosystem = root.AddComponent<EcosystemRuntime>();
                var tent = root.AddComponent<TentSleepRuntime>();
                player.AddComponent<PlayerSurvivalRuntime>();
                var hunter = predator.AddComponent<CreatureAgentView>(); hunter.Configure("custom_hunter");
                predator.AddComponent<CreatureNeedsRuntime>().Configure("custom_hunter", definition);
                predator.AddComponent<CreatureHealthRuntime>().Configure("custom_hunter", definition);
                predator.transform.position = Vector3.right * 2f;
                ecosystem.RegisterCreature(hunter);
                var small = prey.AddComponent<CreatureAgentView>(); small.Configure("island_small_prey");
                var herbivore = forager.AddComponent<CreatureAgentView>(); herbivore.Configure("island_forager");
                Assert.That(EcosystemRuntime.IsValidPrey(small), Is.True);
                Assert.That(EcosystemRuntime.IsValidPrey(herbivore), Is.True);
                Assert.That(EcosystemRuntime.IsValidPrey(hunter), Is.False);
                Assert.That(tent.TrySleep(player).Success, Is.False);
                Assert.That(tent.TrySleep(player).Message, Does.Contain("threatened"));
                predator.GetComponent<CreatureHealthRuntime>().RestoreHealth(90f, 0f, true);
                Assert.That(ecosystem.TryFindNearestCreatureByRole(Vector3.zero, CreatureRole.Predator, 34f), Is.Null);
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(predator); Object.DestroyImmediate(prey); Object.DestroyImmediate(forager); Object.DestroyImmediate(player); Object.DestroyImmediate(definition); }
        }
        [Test] public void CustomPredatorPrefersMeatWhenHungryWithoutSpeciesIdBranch()
        {
            var root = new GameObject("PredatorDietFixture");
            var hunterObject = new GameObject("CustomHunter");
            var foodObject = new GameObject("meat_drop");
            var definition = SpeciesDefinition.CreateDefault("island_predator");
            try
            {
                root.AddComponent<EcosystemRuntime>();
                root.AddComponent<WorldQueryRuntime>();
                definition.Configure("custom_meat_hunter", "Custom Hunter", 90f,100f,18f,32f,58f,80f,140f,200f,38f,22f,36f,58f,0f,1f,0.45f);
                hunterObject.AddComponent<CreatureAgentView>().Configure(definition.SpeciesId);
                var needs = hunterObject.AddComponent<CreatureNeedsRuntime>();
                needs.Configure(definition.SpeciesId, definition); needs.RestoreNeeds(80f, 0.4f);
                hunterObject.AddComponent<CreatureHealthRuntime>().Configure(definition.SpeciesId, definition);
                var brain = hunterObject.AddComponent<CreatureBehaviorBrain>();
                var food = foodObject.AddComponent<FoodSourceView>();
                food.Configure("meat_drop", "Meat", ApexShift.Core.Ecosystem.FoodKind.Meat, 5f, 10f);
                typeof(CreatureBehaviorBrain).GetMethod("TickBrain", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(brain, null);
                Assert.That(brain.State, Is.EqualTo(CreatureBehaviorState.EatMeat));
                Assert.That(needs.State.Hunger, Is.LessThan(80f));
                Assert.That(brain.LastFoodSource, Is.EqualTo("meat_drop"));
            }
            finally { Object.DestroyImmediate(foodObject); Object.DestroyImmediate(hunterObject); Object.DestroyImmediate(root); Object.DestroyImmediate(definition); }
        }
        [UnityTest] public IEnumerator GeneratedPopulationRespectsCapEnvironmentAndDailyRegistryCount()
        {
            var root = new GameObject("HabitatPopulationGenerator");
            var generator = root.AddComponent<WorldGeneratorRuntime>();
            generator.SetGenerateOnStart(false);
            generator.SetSeed(99123);
            var catalog = ScriptableObject.CreateInstance<ApexShift.Runtime.World.Biomes.BiomeCatalogAsset>();
            generator.SetBiomeCatalog(catalog);
            var config = GameBalanceConfig.CreateFallback();
            foreach(var species in config.SpeciesDefinitions) species.PopulationRules.Configure(3,3,3, attempts: 256, chance: 1f);
            typeof(WorldGeneratorRuntime).GetField("gameBalanceConfig", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(generator,config);
            try
            {
                generator.Generate();
                yield return null;
                var context = generator.CurrentGeneration;
                var ecosystem = context.GenerationRoot.GetComponentInChildren<EcosystemRuntime>();
                Assert.That(ecosystem.Creatures.Count, Is.GreaterThan(0));
                typeof(WorldGeneratorRuntime).GetMethod("HandleDayChanged",System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(generator,new object[]{10});
                foreach(var species in config.SpeciesDefinitions)
                    Assert.That(ecosystem.Creatures.Count(c=>c != null && c.SpeciesId == species.SpeciesId && !c.CachedHealth.IsDead), Is.LessThanOrEqualTo(3));
                foreach(var creature in ecosystem.Creatures)
                {
                    Assert.That(context.IslandTopography.TryGetEnvironmentAt(creature.transform.position,out ApexShift.Runtime.World.Environment.EnvironmentSample sample), Is.True);
                    Assert.That(sample.IsLand && !sample.IsWater, Is.True);
                    Assert.That(creature.Definition.AllowedHabitatIds, Does.Contain(sample.HabitatId));
                    Assert.That(creature.SpeciesId, Does.StartWith("island_"));
                }
            }
            finally
            {
                generator.ClearGeneratedWorld(); Object.Destroy(root);
                foreach(var species in config.SpeciesDefinitions) Object.Destroy(species);
                Object.Destroy(config.SimulationLodConfig); Object.Destroy(config.EcosystemBalanceConfig); Object.Destroy(config.ResourceBalanceConfig); Object.Destroy(config); Object.Destroy(catalog);
            }
            yield return null;
        }
    }
}
