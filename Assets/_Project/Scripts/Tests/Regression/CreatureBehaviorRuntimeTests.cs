using System.Collections;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public class CreatureBehaviorRuntimeTests
    {
        [UnityTest]
        public IEnumerator VarnakTransitionsToChaseWhenPlayerIsNearby()
        {
            GameObject ecosystemObject = new GameObject("Ecosystem");
            ecosystemObject.AddComponent<EcosystemRuntime>();

            GameObject playerObject = new GameObject("Player");
            playerObject.tag = "Player";
            playerObject.transform.position = Vector3.zero;

            GameObject creatureObject = new GameObject("Creature_varnak");
            creatureObject.AddComponent<CreatureNavigationAdapter>();
            creatureObject.AddComponent<CreatureAgentView>().Configure("varnak");
            creatureObject.AddComponent<CreatureNeedsRuntime>().Configure("varnak");
            creatureObject.AddComponent<CreatureHealthRuntime>().Configure("varnak");
            CreatureBehaviorBrain brain = creatureObject.AddComponent<CreatureBehaviorBrain>();
            creatureObject.AddComponent<CreatureBehaviorRuntime>();

            creatureObject.transform.position = new Vector3(8f, 0f, 0f);

            yield return null;
            yield return null;

            Assert.AreEqual(CreatureBehaviorState.Chase, brain.State);
            Assert.AreEqual("player", brain.CurrentTargetLabel);
            Assert.AreEqual(CreatureBehaviorState.Chase, creatureObject.GetComponent<CreatureBehaviorRuntime>().State);
        }
    }
}
