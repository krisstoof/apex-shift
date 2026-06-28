using System.Collections;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Query;
using NUnit.Framework;
using UnityEngine.AI;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Unit.World
{
    public sealed class WorldQueryRuntimeTests
    {
        [Test]
        public void TryFindNearestPlantFoodUsesRegisteredFoodSources()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject farPlant = new GameObject("FarPlant");
            GameObject nearPlant = new GameObject("NearPlant");

            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                WorldQueryRuntime query = WorldQueryRuntime.GetOrCreate(ecosystem);

                farPlant.transform.position = new Vector3(20f, 0f, 0f);
                FoodSourceView far = farPlant.AddComponent<FoodSourceView>();
                far.Configure("far_plant", "Far Plant", FoodKind.Plants, 10f, 1f);

                nearPlant.transform.position = new Vector3(4f, 0f, 0f);
                FoodSourceView near = nearPlant.AddComponent<FoodSourceView>();
                near.Configure("near_plant", "Near Plant", FoodKind.Plants, 10f, 1f);

                Assert.IsTrue(query.TryFindNearestPlantFood(Vector3.zero, 50f, out FoodSourceView result));
                Assert.AreEqual(near, result);
            }
            finally
            {
                Object.DestroyImmediate(nearPlant);
                Object.DestroyImmediate(farPlant);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [Test]
        public void TryFindNearestMeatFoodUsesRegisteredMeatAndScavengerSources()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject meatObject = new GameObject("MeatDrop");

            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                WorldQueryRuntime query = WorldQueryRuntime.GetOrCreate(ecosystem);

                meatObject.transform.position = new Vector3(3f, 0f, 0f);
                FoodSourceView meat = meatObject.AddComponent<FoodSourceView>();
                meat.Configure("meat_drop", "Meat", FoodKind.Meat, 10f, 1f);

                Assert.IsTrue(query.TryFindNearestMeatFood(Vector3.zero, 50f, out FoodSourceView result));
                Assert.AreEqual(meat, result);
            }
            finally
            {
                Object.DestroyImmediate(meatObject);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [UnityTest]
        public IEnumerator TryFindNearestCreatureByIdUsesRegisteredCreatures()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject farCreature = new GameObject("FarSmallPrey");
            GameObject nearCreature = new GameObject("NearSmallPrey");
            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                WorldQueryRuntime query = WorldQueryRuntime.GetOrCreate(ecosystem);

                navMeshRoot = GameObject.CreatePrimitive(PrimitiveType.Plane);
                navMeshRoot.name = "NavMeshRoot";
                navMeshRoot.transform.position = Vector3.zero;
                System.Type navMeshSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
                Assert.IsNotNull(navMeshSurfaceType, "Could not resolve NavMeshSurface.");
                Component surface = navMeshRoot.AddComponent(navMeshSurfaceType);
                System.Type collectObjectsType = navMeshSurfaceType.Assembly.GetType("Unity.AI.Navigation.CollectObjects");
                Assert.IsNotNull(collectObjectsType, "Could not resolve CollectObjects.");
                navMeshSurfaceType.GetProperty("collectObjects")?.SetValue(surface, System.Enum.Parse(collectObjectsType, "Children"));
                navMeshSurfaceType.GetMethod("BuildNavMesh")?.Invoke(surface, null);
                yield return null;

                farCreature.transform.position = new Vector3(20f, 0f, 0f);
                CreatureAgentView far = farCreature.AddComponent<CreatureAgentView>();
                far.Configure("small_prey");
                farCreature.AddComponent<CreatureHealthRuntime>().Configure("small_prey");
                ecosystem.RegisterCreature(far);

                nearCreature.transform.position = new Vector3(2f, 0f, 0f);
                CreatureAgentView near = nearCreature.AddComponent<CreatureAgentView>();
                near.Configure("small_prey");
                nearCreature.AddComponent<CreatureHealthRuntime>().Configure("small_prey");
                ecosystem.RegisterCreature(near);

                Assert.IsTrue(query.TryFindNearestCreatureById(Vector3.zero, "small_prey", 50f, out CreatureAgentView result));
                Assert.AreEqual(near, result);
            }
            finally
            {
                Object.DestroyImmediate(navMeshRoot);
                Object.DestroyImmediate(nearCreature);
                Object.DestroyImmediate(farCreature);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [UnityTest]
        public IEnumerator TryFindNearestPreyIgnoresPredatorsAndDeadCreatures()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject predatorObject = new GameObject("Varnak");
            GameObject preyObject = new GameObject("SmallPrey");
            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                WorldQueryRuntime query = WorldQueryRuntime.GetOrCreate(ecosystem);

                navMeshRoot = GameObject.CreatePrimitive(PrimitiveType.Plane);
                navMeshRoot.name = "NavMeshRoot";
                navMeshRoot.transform.position = Vector3.zero;
                System.Type navMeshSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
                Assert.IsNotNull(navMeshSurfaceType, "Could not resolve NavMeshSurface.");
                Component surface = navMeshRoot.AddComponent(navMeshSurfaceType);
                System.Type collectObjectsType = navMeshSurfaceType.Assembly.GetType("Unity.AI.Navigation.CollectObjects");
                Assert.IsNotNull(collectObjectsType, "Could not resolve CollectObjects.");
                navMeshSurfaceType.GetProperty("collectObjects")?.SetValue(surface, System.Enum.Parse(collectObjectsType, "Children"));
                navMeshSurfaceType.GetMethod("BuildNavMesh")?.Invoke(surface, null);
                yield return null;

                predatorObject.transform.position = new Vector3(1f, 0f, 0f);
                CreatureAgentView predator = predatorObject.AddComponent<CreatureAgentView>();
                predator.Configure("varnak");
                predatorObject.AddComponent<CreatureHealthRuntime>().Configure("varnak");
                ecosystem.RegisterCreature(predator);

                preyObject.transform.position = new Vector3(3f, 0f, 0f);
                CreatureAgentView prey = preyObject.AddComponent<CreatureAgentView>();
                prey.Configure("small_prey");
                preyObject.AddComponent<CreatureHealthRuntime>().Configure("small_prey");
                ecosystem.RegisterCreature(prey);

                Assert.IsTrue(query.TryFindNearestPrey(Vector3.zero, "varnak", 50f, out CreatureAgentView result));
                Assert.AreEqual(prey, result);
            }
            finally
            {
                Object.DestroyImmediate(navMeshRoot);
                Object.DestroyImmediate(preyObject);
                Object.DestroyImmediate(predatorObject);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [Test]
        public void GetBiomeIdForPositionReturnsStableDefaultUntilBiomeIndexExists()
        {
            GameObject queryObject = new GameObject("WorldQuery");
            try
            {
                WorldQueryRuntime query = queryObject.AddComponent<WorldQueryRuntime>();

                Assert.AreEqual("default", query.GetBiomeIdForPosition(Vector3.zero));
            }
            finally
            {
                Object.DestroyImmediate(queryObject);
            }
        }

        [Test]
        public void GetBiomeIdForPositionUsesDirectorOnSameEcosystemObject()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                EcosystemDirectorRuntime director = ecosystemObject.AddComponent<EcosystemDirectorRuntime>();
                WorldQueryRuntime query = ecosystemObject.AddComponent<WorldQueryRuntime>();

                BiomeDefinitionAsset biome = ScriptableObject.CreateInstance<BiomeDefinitionAsset>();
                biome.Configure("test_biome", "Test Biome", Color.green, false, null, null);
                director.InitializeFromRegions(new[]
                {
                    new GeneratedBiomeRegion(biome, new Bounds(Vector3.zero, new Vector3(20f, 2f, 20f)))
                });

                Assert.AreEqual("test_biome", query.GetBiomeIdForPosition(Vector3.zero));
            }
            finally
            {
                Object.DestroyImmediate(ecosystemObject);
            }
        }

    }
}
