using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.GodotParity
{
    public sealed class CreatureSpeciesGodotParityTests
    {
        [Test]
        public void SmallPreyIsPlantOnlyAndDeathCreatesOneMeatDrop()
        {
            GameObject creature = new GameObject("Creature_small_prey");
            try
            {
                creature.AddComponent<CreatureAgentView>().Configure("small_prey");
                CreatureNeedsRuntime needs = creature.AddComponent<CreatureNeedsRuntime>();
                needs.Configure("small_prey");
                CreatureHealthRuntime health = creature.AddComponent<CreatureHealthRuntime>();
                health.Configure("small_prey");

                Assert.Greater(needs.Diet.PlantPreference, 0f);
                Assert.AreEqual(0f, needs.Diet.MeatPreference, 0.001f);

                int before = CountMeatDrops();
                health.TakeDamage(999f);
                int after = CountMeatDrops();

                Assert.AreEqual(before + 1, after);
            }
            finally
            {
                DestroyAllMeatDrops();
                Object.DestroyImmediate(creature);
            }
        }

        [Test]
        public void GrazerPrefersPlantsButCanUseMeatAndScavenge()
        {
            CreatureDietProfile diet = CreatureDietProfile.GetDefault("grazer");

            Assert.Greater(diet.PlantPreference, diet.MeatPreference);
            Assert.Greater(diet.PlantPreference, diet.ScavengerPreference);
            Assert.Greater(diet.MeatPreference, 0f);
            Assert.Greater(diet.ScavengerPreference, 0f);
        }

        [Test]
        public void VarnakUsesMeatAndScavengerDietButNotPlants()
        {
            CreatureDietProfile diet = CreatureDietProfile.GetDefault("varnak");

            Assert.AreEqual(0f, diet.PlantPreference, 0.001f);
            Assert.Greater(diet.MeatPreference, 0f);
            Assert.Greater(diet.ScavengerPreference, 0f);
            Assert.Greater(diet.MeatPreference, diet.ScavengerPreference);
        }

        [Test]
        public void VarnakHasHigherHealthThanPreySpecies()
        {
            GameObject smallPrey = new GameObject("Creature_small_prey");
            GameObject grazer = new GameObject("Creature_grazer");
            GameObject varnak = new GameObject("Creature_varnak");
            try
            {
                CreatureHealthRuntime smallPreyHealth = smallPrey.AddComponent<CreatureHealthRuntime>();
                CreatureHealthRuntime grazerHealth = grazer.AddComponent<CreatureHealthRuntime>();
                CreatureHealthRuntime varnakHealth = varnak.AddComponent<CreatureHealthRuntime>();

                smallPreyHealth.Configure("small_prey");
                grazerHealth.Configure("grazer");
                varnakHealth.Configure("varnak");

                Assert.Greater(varnakHealth.MaxHealth, grazerHealth.MaxHealth);
                Assert.Greater(grazerHealth.MaxHealth, smallPreyHealth.MaxHealth);
            }
            finally
            {
                Object.DestroyImmediate(smallPrey);
                Object.DestroyImmediate(grazer);
                Object.DestroyImmediate(varnak);
            }
        }

        private static int CountMeatDrops()
        {
            int count = 0;
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go != null && go.name.StartsWith("MeatDrop_"))
                {
                    count++;
                }
            }

            return count;
        }

        private static void DestroyAllMeatDrops()
        {
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                if (go != null && go.name.StartsWith("MeatDrop_"))
                {
                    Object.DestroyImmediate(go);
                }
            }
        }
    }
}
