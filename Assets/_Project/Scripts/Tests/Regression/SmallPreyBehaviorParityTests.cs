using System.Collections;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.World.Query;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class SmallPreyBehaviorParityTests
    {
        [UnityTest]
        public IEnumerator HungrySmallPreySeeksPlantFoodThroughWorldQuery()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject preyObject = null;
            GameObject foodObject = null;
            try
            {
                DestroyGlobalPlayerObjects();
                ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();

                navMeshRoot = CreateNavMeshRoot();

                preyObject = CreateCreature("small_prey", Vector3.zero, hungry: true);
                foodObject = CreatePlantFood("grass_patch", new Vector3(8f, 0f, 0f), 10f, 2f);

                for (int i = 0; i < 20; i++)
                {
                    yield return null;
                }

                CreatureBehaviorBrain brain = preyObject.GetComponent<CreatureBehaviorBrain>();
                Assert.AreEqual(CreatureBehaviorState.SeekFood, brain.State);
                Assert.IsTrue(brain.CurrentTargetLabel.Contains("grass_patch"));
                Assert.IsTrue(brain.DecisionReason.Contains("hungry_seek_plant"));
            }
            finally
            {
                if (foodObject != null) Object.DestroyImmediate(foodObject);
                if (preyObject != null) Object.DestroyImmediate(preyObject);
                if (navMeshRoot != null) Object.DestroyImmediate(navMeshRoot);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [UnityTest]
        public IEnumerator SmallPreyFleesNearbyVarnak()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject preyObject = null;
            GameObject varnakObject = null;
            try
            {
                DestroyGlobalPlayerObjects();
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();

                navMeshRoot = CreateNavMeshRoot();

                preyObject = CreateCreature("small_prey", Vector3.zero, hungry: true);
                varnakObject = CreateCreatureAgentOnly("varnak", new Vector3(3f, 0f, 0f));
                ecosystem.RegisterCreature(varnakObject.GetComponent<CreatureAgentView>());

                for (int i = 0; i < 20; i++)
                {
                    yield return null;
                }

                CreatureBehaviorBrain brain = preyObject.GetComponent<CreatureBehaviorBrain>();
                Assert.AreEqual(CreatureBehaviorState.Flee, brain.State);
                Assert.IsTrue(brain.DecisionReason.Contains("flee_varnak"));
            }
            finally
            {
                if (varnakObject != null) Object.DestroyImmediate(varnakObject);
                if (preyObject != null) Object.DestroyImmediate(preyObject);
                if (navMeshRoot != null) Object.DestroyImmediate(navMeshRoot);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [UnityTest]
        public IEnumerator SmallPreyEatingPlantRecordsLastFoodAndConsumesBiomass()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject preyObject = null;
            GameObject foodObject = null;
            try
            {
                DestroyGlobalPlayerObjects();
                ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();

                navMeshRoot = CreateNavMeshRoot();

                preyObject = CreateCreature("small_prey", Vector3.zero, hungry: true);
                foodObject = CreatePlantFood("berry_bush", new Vector3(1f, 0f, 0f), 10f, 4f);
                FoodSourceView food = foodObject.GetComponent<FoodSourceView>();
                float startingBiomass = food.Biomass;

                for (int i = 0; i < 20; i++)
                {
                    yield return null;
                }

                CreatureBehaviorBrain brain = preyObject.GetComponent<CreatureBehaviorBrain>();
                Assert.AreEqual(CreatureBehaviorState.EatPlants, brain.State);
                Assert.AreEqual("berry_bush", brain.LastFoodSource);
                Assert.Less(food.Biomass, startingBiomass);
            }
            finally
            {
                if (foodObject != null) Object.DestroyImmediate(foodObject);
                if (preyObject != null) Object.DestroyImmediate(preyObject);
                if (navMeshRoot != null) Object.DestroyImmediate(navMeshRoot);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        private static GameObject CreateCreature(string creatureId, Vector3 position, bool hungry)
        {
            GameObject creature = CreateCreatureAgentOnly(creatureId, position);
            CreatureNeedsRuntime needs = creature.AddComponent<CreatureNeedsRuntime>();
            needs.Configure(creatureId);
            if (hungry)
            {
                needs.State.SetHunger(80f);
            }

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

        private static GameObject CreatePlantFood(string sourceId, Vector3 position, float maxBiomass, float nutritionPerBiomass)
        {
            GameObject foodObject = new GameObject(sourceId);
            foodObject.transform.position = position;
            FoodSourceView food = foodObject.AddComponent<FoodSourceView>();
            food.Configure(sourceId, sourceId, FoodKind.Plants, maxBiomass, nutritionPerBiomass);
            return foodObject;
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

        private static void DestroyGlobalPlayerObjects()
        {
            GameObject[] players = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (GameObject go in players)
            {
                if (go == null)
                {
                    continue;
                }

                if (go.CompareTag("Player") || go.name == "Player" || go.GetComponent<ApexShift.Runtime.Player.IsometricPlayerController>() != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }
    }
}
