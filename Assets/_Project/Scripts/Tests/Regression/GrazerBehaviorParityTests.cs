using System.Collections;
using System.Linq;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.World.Query;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class GrazerBehaviorParityTests
    {
        [UnityTest]
        public IEnumerator HungryGrazerPrefersPlantsOverNearbyMeat()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject grazerObject = null;
            GameObject plantObject = null;
            GameObject meatObject = null;
            try
            {
                DestroyGlobalPlayerObjects();
                ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();
                navMeshRoot = CreateNavMeshRoot();

                grazerObject = CreateCreature("grazer", Vector3.zero, 45f);
                plantObject = CreateFood("berry_bush", FoodKind.Plants, new Vector3(8f, 0f, 0f), 10f, 3f);
                meatObject = CreateFood("meat_drop", FoodKind.Meat, new Vector3(2f, 0f, 0f), 10f, 10f);

                yield return WaitForAi();

                CreatureBehaviorBrain brain = grazerObject.GetComponent<CreatureBehaviorBrain>();
                Assert.AreEqual(CreatureBehaviorState.SeekFood, brain.State);
                Assert.IsTrue(brain.CurrentTargetLabel.Contains("berry_bush"));
                Assert.IsTrue(brain.DecisionReason.Contains("hungry_prefer_plants"));
            }
            finally
            {
                DestroyIfNotNull(meatObject);
                DestroyIfNotNull(plantObject);
                DestroyIfNotNull(grazerObject);
                DestroyIfNotNull(navMeshRoot);
                DestroyIfNotNull(ecosystemObject);
            }
        }

        [UnityTest]
        public IEnumerator StarvingGrazerStillPrefersPlantWhenAvailable()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject grazerObject = null;
            GameObject plantObject = null;
            GameObject meatObject = null;
            try
            {
                DestroyGlobalPlayerObjects();
                ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();
                navMeshRoot = CreateNavMeshRoot();

                grazerObject = CreateCreature("grazer", Vector3.zero, 70f);
                plantObject = CreateFood("grass_patch", FoodKind.Plants, new Vector3(8f, 0f, 0f), 10f, 3f);
                meatObject = CreateFood("meat_drop", FoodKind.Meat, new Vector3(2f, 0f, 0f), 10f, 10f);

                yield return WaitForAi();

                CreatureBehaviorBrain brain = grazerObject.GetComponent<CreatureBehaviorBrain>();
                Assert.AreEqual(CreatureBehaviorState.SeekFood, brain.State);
                Assert.IsTrue(brain.CurrentTargetLabel.Contains("grass_patch"));
                Assert.IsTrue(brain.DecisionReason.Contains("starving_still_prefers_plant"));
            }
            finally
            {
                DestroyIfNotNull(meatObject);
                DestroyIfNotNull(plantObject);
                DestroyIfNotNull(grazerObject);
                DestroyIfNotNull(navMeshRoot);
                DestroyIfNotNull(ecosystemObject);
            }
        }

        [UnityTest]
        public IEnumerator StarvingGrazerScavengesMeatOnlyWhenPlantsAreUnavailable()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject grazerObject = null;
            GameObject meatObject = null;
            try
            {
                DestroyGlobalPlayerObjects();
                ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();
                navMeshRoot = CreateNavMeshRoot();

                grazerObject = CreateCreature("grazer", Vector3.zero, 70f);
                meatObject = CreateFood("meat_drop", FoodKind.Meat, new Vector3(8f, 0f, 0f), 10f, 10f);

                yield return WaitForAi();

                CreatureBehaviorBrain brain = grazerObject.GetComponent<CreatureBehaviorBrain>();
                Assert.AreEqual(CreatureBehaviorState.Scavenge, brain.State);
                Assert.IsTrue(brain.CurrentTargetLabel.Contains("meat_drop"));
                Assert.IsTrue(brain.DecisionReason.Contains("starving_scavenge"));
            }
            finally
            {
                DestroyIfNotNull(meatObject);
                DestroyIfNotNull(grazerObject);
                DestroyIfNotNull(navMeshRoot);
                DestroyIfNotNull(ecosystemObject);
            }
        }

        [UnityTest]
        public IEnumerator DesperateGrazerHuntsSmallPreyWhenPlantsAreUnavailable()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject grazerObject = null;
            GameObject preyObject = null;
            try
            {
                DestroyGlobalPlayerObjects();
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();
                navMeshRoot = CreateNavMeshRoot();

                grazerObject = CreateCreature("grazer", Vector3.zero, 90f);
                preyObject = CreateCreatureAgentOnly("small_prey", new Vector3(6f, 0f, 0f));
                preyObject.AddComponent<CreatureHealthRuntime>().Configure("small_prey");
                ecosystem.RegisterCreature(preyObject.GetComponent<CreatureAgentView>());

                yield return WaitForAi();

                CreatureBehaviorBrain brain = grazerObject.GetComponent<CreatureBehaviorBrain>();
                Assert.AreEqual(CreatureBehaviorState.HuntSmallPrey, brain.State);
                Assert.IsTrue(brain.CurrentTargetLabel.Contains("small_prey"));
                Assert.IsTrue(brain.DecisionReason.Contains("grazer_predation"));
            }
            finally
            {
                DestroyIfNotNull(preyObject);
                DestroyIfNotNull(grazerObject);
                DestroyIfNotNull(navMeshRoot);
                DestroyIfNotNull(ecosystemObject);
            }
        }

        [UnityTest]
        public IEnumerator GrazerFleesNearbyVarnak()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject grazerObject = null;
            GameObject varnakObject = null;
            try
            {
                DestroyGlobalPlayerObjects();
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();
                navMeshRoot = CreateNavMeshRoot();

                grazerObject = CreateCreature("grazer", Vector3.zero, 45f);
                varnakObject = CreateCreatureAgentOnly("varnak", new Vector3(3f, 0f, 0f));
                ecosystem.RegisterCreature(varnakObject.GetComponent<CreatureAgentView>());

                yield return WaitForAi();

                CreatureBehaviorBrain brain = grazerObject.GetComponent<CreatureBehaviorBrain>();
                Assert.AreEqual(CreatureBehaviorState.Flee, brain.State);
                Assert.IsTrue(brain.DecisionReason.Contains("flee_varnak"));
            }
            finally
            {
                DestroyIfNotNull(varnakObject);
                DestroyIfNotNull(grazerObject);
                DestroyIfNotNull(navMeshRoot);
                DestroyIfNotNull(ecosystemObject);
            }
        }

        [Test]
        public void GrazerDeathCreatesMeatDrop()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject grazerObject = null;
            try
            {
                ecosystemObject.AddComponent<EcosystemRuntime>();
                grazerObject = CreateCreature("grazer", Vector3.zero, 45f);

                grazerObject.GetComponent<CreatureHealthRuntime>().TakeDamage(999f);

                int meatDrops = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
                    .Count(go => go != null && go.name.StartsWith("MeatDrop_grazer"));

                Assert.AreEqual(1, meatDrops);
                Assert.AreEqual(CreatureBehaviorState.Dead, grazerObject.GetComponent<CreatureBehaviorBrain>().State);
            }
            finally
            {
                DestroyAllMeatDrops();
                DestroyIfNotNull(grazerObject);
                DestroyIfNotNull(ecosystemObject);
            }
        }

        private static GameObject CreateCreature(string creatureId, Vector3 position, float hunger)
        {
            GameObject creature = CreateCreatureAgentOnly(creatureId, position);
            CreatureNeedsRuntime needs = creature.AddComponent<CreatureNeedsRuntime>();
            needs.Configure(creatureId);
            needs.State.SetHunger(hunger);
            creature.AddComponent<CreatureHealthRuntime>().Configure(creatureId);
            creature.AddComponent<CreatureSimulationLodRuntime>();
            creature.AddComponent<CreatureDebugOverlay>();
            creature.AddComponent<CreatureBehaviorBrain>();
            creature.AddComponent<CreatureBehaviorRuntime>();
            return creature;
        }

        private static GameObject CreateCreatureAgentOnly(string creatureId, Vector3 position)
        {
            GameObject creature = new GameObject($"Creature_{creatureId}");
            creature.transform.position = position;
            creature.AddComponent<CreatureNavigationAdapter>();
            creature.AddComponent<CreatureAgentView>().Configure(creatureId);
            return creature;
        }

        private static GameObject CreateFood(string sourceId, FoodKind kind, Vector3 position, float maxBiomass, float nutritionPerBiomass)
        {
            GameObject foodObject = new GameObject(sourceId);
            foodObject.transform.position = position;
            FoodSourceView food = foodObject.AddComponent<FoodSourceView>();
            food.Configure(sourceId, sourceId, kind, maxBiomass, nutritionPerBiomass);
            return foodObject;
        }

        private static IEnumerator WaitForAi()
        {
            for (int i = 0; i < 20; i++)
            {
                yield return null;
            }
        }

        private static GameObject CreateNavMeshRoot()
        {
            GameObject navMeshRoot = new GameObject("NavMeshRoot");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "NavMeshFloor";
            floor.transform.SetParent(navMeshRoot.transform);
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            System.Type navMeshSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            Assert.IsNotNull(navMeshSurfaceType, "Could not resolve NavMeshSurface.");

            Component surface = navMeshRoot.AddComponent(navMeshSurfaceType);
            System.Type collectObjectsType = navMeshSurfaceType.Assembly.GetType("Unity.AI.Navigation.CollectObjects");
            Assert.IsNotNull(collectObjectsType, "Could not resolve CollectObjects.");
            navMeshSurfaceType.GetProperty("collectObjects")?.SetValue(surface, System.Enum.Parse(collectObjectsType, "Children"));
            navMeshSurfaceType.GetMethod("BuildNavMesh")?.Invoke(surface, null);
            return navMeshRoot;
        }

        private static void DestroyAllMeatDrops()
        {
            GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (GameObject go in objects)
            {
                if (go != null && go.name.StartsWith("MeatDrop_"))
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static void DestroyIfNotNull(GameObject go)
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void DestroyGlobalPlayerObjects()
        {
            GameObject[] players = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (GameObject go in players)
            {
                if (go == null)
                {
                    continue;
                }

                if (go.name == "Player" || go.GetComponent<ApexShift.Runtime.Player.IsometricPlayerController>() != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }
    }
}
