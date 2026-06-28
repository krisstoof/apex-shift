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
            GameObject creature = null;
            GameObject player = null;
            GameObject animObject = null;
            try
            {
                ecosystemObject.AddComponent<ApexShift.Runtime.Ecosystem.EcosystemRuntime>();

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

                for (int i = 0; i < 30; i++)
                {
                    yield return null;
                }

                FieldInfo stateField = typeof(CreatureAnimationDriver).GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(stateField);
                float currentState = (float)stateField.GetValue(driver);
                Assert.Greater(currentState, 0.25f);
            }
            finally
            {
                if (player != null) Object.DestroyImmediate(player);
                if (creature != null) Object.DestroyImmediate(creature);
                if (animObject != null) Object.DestroyImmediate(animObject);
                Object.DestroyImmediate(ecosystemObject);
            }
        }
    }
}
