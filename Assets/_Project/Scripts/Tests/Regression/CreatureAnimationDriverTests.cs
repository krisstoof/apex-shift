using System.Collections;
using System.Reflection;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public class CreatureAnimationDriverTests
    {
        [UnityTest]
        public IEnumerator DriverRaisesBlendStateWhenCreatureChases()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            GameObject navMeshRoot = null;
            GameObject creature = null;
            GameObject player = null;
            GameObject animObject = null;
            try
            {
                ecosystemObject.AddComponent<ApexShift.Runtime.Ecosystem.EcosystemRuntime>();

                navMeshRoot = new GameObject("NavMeshRoot");
                GameObject navMeshFloor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                navMeshFloor.name = "NavMeshFloor";
                navMeshFloor.transform.SetParent(navMeshRoot.transform);
                navMeshFloor.transform.position = Vector3.zero;
                navMeshFloor.transform.localScale = new Vector3(2f, 1f, 2f);

                System.Type navMeshSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
                Assert.IsNotNull(navMeshSurfaceType, "Could not resolve NavMeshSurface.");

                Component surface = navMeshRoot.AddComponent(navMeshSurfaceType);
                navMeshSurfaceType.GetProperty("collectObjects")?.SetValue(surface, System.Enum.Parse(navMeshSurfaceType.Assembly.GetType("Unity.AI.Navigation.CollectObjects"), "Children"));
                navMeshSurfaceType.GetMethod("BuildNavMesh")?.Invoke(surface, null);

                creature = new GameObject("Creature_varnak");
                creature.transform.position = Vector3.zero;
                creature.AddComponent<CreatureNavigationAdapter>();
                creature.AddComponent<CreatureAgentView>().Configure("varnak");
                creature.AddComponent<CreatureNeedsRuntime>().Configure("varnak");
                creature.AddComponent<CreatureHealthRuntime>().Configure("varnak");
                CreatureBehaviorBrain behavior = creature.AddComponent<CreatureBehaviorBrain>();
                creature.AddComponent<CreatureBehaviorRuntime>();
                NavMeshAgent agent = creature.GetComponent<NavMeshAgent>();
                Assert.IsNotNull(agent);

                player = new GameObject("Player");
                player.tag = "Player";
                player.transform.position = new Vector3(10f, 0f, 0f);

                animObject = new GameObject("Animator");
                animObject.transform.SetParent(creature.transform);
                animObject.AddComponent<Animator>();

                CreatureAnimationDriver driver = creature.AddComponent<CreatureAnimationDriver>();
                driver.Configure(2f);

                behavior.SetBehaviorStateForTests(CreatureBehaviorState.Chase, "test");

                FieldInfo stateField = typeof(CreatureAnimationDriver).GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(stateField);

                float currentState = 0f;
                float timeout = 1f;
                while (timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;

                    currentState = (float)stateField.GetValue(driver);
                    if (currentState > 0.25f)
                    {
                        break;
                    }
                }

                Assert.Greater(
                    currentState,
                    0.25f,
                    "Chase state blend should rise above the regression threshold after enough simulated PlayMode time.");
            }
            finally
            {
                if (player != null) Object.DestroyImmediate(player);
                if (creature != null) Object.DestroyImmediate(creature);
                if (animObject != null) Object.DestroyImmediate(animObject);
                if (navMeshRoot != null) Object.DestroyImmediate(navMeshRoot);
                Object.DestroyImmediate(ecosystemObject);
            }
        }
    }
}
