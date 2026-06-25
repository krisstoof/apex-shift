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
            GameObject creature = new GameObject("Creature_varnak");
            creature.AddComponent<CreatureNavigationAdapter>();
            creature.AddComponent<CreatureAgentView>().Configure("varnak");
            creature.AddComponent<CreatureNeedsRuntime>().Configure("varnak");
            creature.AddComponent<CreatureHealthRuntime>().Configure("varnak");
            CreatureBehaviorBrain behavior = creature.AddComponent<CreatureBehaviorBrain>();
            creature.AddComponent<CreatureBehaviorRuntime>();
            creature.AddComponent<NavMeshAgent>();

            GameObject animObject = new GameObject("Animator");
            animObject.transform.SetParent(creature.transform);
            animObject.AddComponent<Animator>();

            CreatureAnimationDriver driver = creature.AddComponent<CreatureAnimationDriver>();
            driver.Configure(2f);

            behavior.SetBehaviorStateForTests(CreatureBehaviorState.Chase, "test");

            yield return null;

            FieldInfo stateField = typeof(CreatureAnimationDriver).GetField("_currentState", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(stateField);
            float currentState = (float)stateField.GetValue(driver);
            Assert.Greater(currentState, 0.25f);
        }
    }
}
