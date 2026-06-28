using System.Collections;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public class CreatureBehaviorRuntimeTests
    {
        [UnityTest]
        public IEnumerator VarnakTransitionsToChaseWhenPlayerIsNearby()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject playerObject = null;
            GameObject creatureObject = null;
            try
            {
                ecosystemObject.AddComponent<EcosystemRuntime>();

                navMeshRoot = new GameObject("NavMeshRoot");
                GameObject navMeshFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                navMeshFloor.name = "NavMeshFloor";
                navMeshFloor.transform.SetParent(navMeshRoot.transform);
                navMeshFloor.transform.position = Vector3.zero;
                navMeshFloor.transform.localScale = new Vector3(2f, 1f, 2f);

                System.Type navMeshSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
                Assert.IsNotNull(navMeshSurfaceType, "Could not resolve NavMeshSurface.");
                Component surface = navMeshRoot.AddComponent(navMeshSurfaceType);
                System.Type collectObjectsType = navMeshSurfaceType.Assembly.GetType("Unity.AI.Navigation.CollectObjects");
                Assert.IsNotNull(collectObjectsType, "Could not resolve CollectObjects.");
                navMeshSurfaceType.GetProperty("collectObjects")?.SetValue(surface, System.Enum.Parse(collectObjectsType, "Children"));
                navMeshSurfaceType.GetMethod("BuildNavMesh")?.Invoke(surface, null);

                playerObject = new GameObject("Player");
                playerObject.tag = "Player";
                playerObject.transform.position = Vector3.zero;

                creatureObject = new GameObject("Creature_varnak");
                creatureObject.AddComponent<CreatureNavigationAdapter>();
                creatureObject.AddComponent<CreatureAgentView>().Configure("varnak");
                creatureObject.AddComponent<CreatureNeedsRuntime>().Configure("varnak");
                creatureObject.AddComponent<CreatureHealthRuntime>().Configure("varnak");
                CreatureBehaviorBrain brain = creatureObject.AddComponent<CreatureBehaviorBrain>();
                creatureObject.AddComponent<CreatureBehaviorRuntime>();

                creatureObject.transform.position = new Vector3(8f, 0f, 0f);

                for (int i = 0; i < 2; i++)
                {
                    yield return null;
                }

                Assert.AreEqual(CreatureBehaviorState.Chase, brain.State);
                Assert.AreEqual("player", brain.CurrentTargetLabel);
                Assert.AreEqual(CreatureBehaviorState.Chase, creatureObject.GetComponent<CreatureBehaviorRuntime>().State);
            }
            finally
            {
                if (creatureObject != null) Object.DestroyImmediate(creatureObject);
                if (playerObject != null) Object.DestroyImmediate(playerObject);
                if (navMeshRoot != null) Object.DestroyImmediate(navMeshRoot);
                Object.DestroyImmediate(ecosystemObject);
            }
        }
    }
}
