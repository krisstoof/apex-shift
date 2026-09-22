using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.World.Query;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public sealed class Issue82RegistryRuntimeTests
    {
        [Test]
        public void CreatureBrainRegistersUnregistersAndReRegistersCreature()
        {
            GameObject ecosystemObject = new GameObject("Issue82_Ecosystem");
            GameObject creatureObject = null;
            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                WorldQueryRuntime query = ecosystemObject.AddComponent<WorldQueryRuntime>();
                creatureObject = CreateCreature("small_prey", true);
                CreatureAgentView view = creatureObject.GetComponent<CreatureAgentView>();

                Assert.That(ecosystem.Creatures, Does.Contain(view));
                Assert.IsTrue(query.TryFindNearestCreatureById(Vector3.zero, "small_prey", 10f, out CreatureAgentView found));
                Assert.AreSame(view, found);

                creatureObject.SetActive(false);
                Assert.IsFalse(ecosystem.Creatures.Contains(view));
                Assert.IsFalse(query.TryFindNearestCreatureById(Vector3.zero, "small_prey", 10f, out _));

                creatureObject.SetActive(true);
                Assert.That(ecosystem.Creatures, Does.Contain(view));

                Object.DestroyImmediate(creatureObject);
                Assert.IsFalse(query.TryFindNearestCreatureById(Vector3.zero, "small_prey", 10f, out _));
            }
            finally
            {
                Object.DestroyImmediate(creatureObject);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [Test]
        public void CombatRegistryMembershipDoesNotReplaceMissingHitbox()
        {
            GameObject ecosystemObject = new GameObject("Issue82_CombatEcosystem");
            GameObject playerObject = new GameObject("Issue82_Player");
            GameObject registeredObject = null;
            GameObject unregisteredObject = null;
            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                ecosystemObject.AddComponent<WorldQueryRuntime>();
                registeredObject = CreateCreature("small_prey", false);
                registeredObject.transform.position = new Vector3(0f, 0f, 1.1f);
                CreatureAgentView registered = registeredObject.GetComponent<CreatureAgentView>();
                ecosystem.RegisterCreature(registered);

                unregisteredObject = new GameObject("Issue82_UnregisteredCreature");
                unregisteredObject.transform.position = new Vector3(0f, 0f, 0.75f);
                unregisteredObject.AddComponent<CreatureAgentView>().Configure("small_prey");
                unregisteredObject.AddComponent<CreatureHealthRuntime>().Configure("small_prey");

                playerObject.transform.forward = Vector3.forward;
                PlayerCombatRuntime combat = playerObject.AddComponent<PlayerCombatRuntime>();
                combat.SetAttackOrigin(playerObject.transform);
                CreatureHealthRuntime registeredHealth = registeredObject.GetComponent<CreatureHealthRuntime>();
                float before = registeredHealth.CurrentHealth;
                Assert.IsFalse(combat.TriggerPrimaryAttack(), "A registered creature without an authoritative hitbox must not be selected.");
                Assert.AreEqual(before, registeredHealth.CurrentHealth);
                Assert.AreEqual(registeredHealth.MaxHealth, unregisteredObject.GetComponent<CreatureHealthRuntime>().CurrentHealth,
                    "An unregistered creature must not be selected by the registry fallback.");
            }
            finally
            {
                Object.DestroyImmediate(unregisteredObject);
                Object.DestroyImmediate(registeredObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [UnityTest]
        public IEnumerator AwarenessNotifiesOnlyRegisteredCreaturesInRadius()
        {
            GameObject ecosystemObject = new GameObject("Issue82_AwarenessEcosystem");
            GameObject playerObject = new GameObject("Issue82_AwarenessPlayer");
            GameObject nearObject = null;
            GameObject farObject = null;
            GameObject unregisteredObject = null;
            try
            {
                EcosystemRuntime ecosystem = ecosystemObject.AddComponent<EcosystemRuntime>();
                WorldQueryRuntime query = ecosystemObject.AddComponent<WorldQueryRuntime>();
                nearObject = CreateCreature("small_prey", true);
                nearObject.transform.position = new Vector3(2f, 0f, 0f);
                CreaturePlayerAwarenessBehavior nearAwareness = nearObject.AddComponent<CreaturePlayerAwarenessBehavior>();
                nearAwareness.Configure("small_prey");
                ecosystem.RegisterCreature(nearObject.GetComponent<CreatureAgentView>());

                farObject = CreateCreature("small_prey", true);
                farObject.transform.position = new Vector3(20f, 0f, 0f);
                CreaturePlayerAwarenessBehavior farAwareness = farObject.AddComponent<CreaturePlayerAwarenessBehavior>();
                farAwareness.Configure("small_prey");
                ecosystem.RegisterCreature(farObject.GetComponent<CreatureAgentView>());

                unregisteredObject = new GameObject("Issue82_UnregisteredAwareness");
                unregisteredObject.transform.position = new Vector3(1f, 0f, 0f);
                CreatureAgentView unregisteredView = unregisteredObject.AddComponent<CreatureAgentView>();
                unregisteredView.Configure("small_prey");
                CreaturePlayerAwarenessBehavior unregisteredAwareness = unregisteredObject.AddComponent<CreaturePlayerAwarenessBehavior>();
                unregisteredAwareness.Configure("small_prey");

                CreatureBehaviorBrain nearBrain = nearObject.GetComponent<CreatureBehaviorBrain>();
                CreatureBehaviorBrain farBrain = farObject.GetComponent<CreatureBehaviorBrain>();
                playerObject.transform.position = Vector3.zero;

                CreaturePlayerAwarenessBehavior.NotifyNearby(Vector3.zero, playerObject.transform, 5f, 1f, "issue82_test");
                yield return null;

                Assert.AreEqual(CreatureBehaviorState.Flee, nearBrain.State, "The nearby registered creature did not receive the threat.");
                Assert.AreNotEqual(CreatureBehaviorState.Flee, farBrain.State, "A far registered creature received an out-of-range threat.");
                List<CreatureAgentView> initialResults = new List<CreatureAgentView>();
                Assert.AreEqual(2, query.GetCreaturesInRadius(Vector3.zero, 30f, initialResults));
                Assert.IsFalse(initialResults.Contains(unregisteredView), "An unregistered creature entered the query result.");

                Object.DestroyImmediate(nearObject);
                List<CreatureAgentView> results = new List<CreatureAgentView>();
                Assert.AreEqual(1, query.GetCreaturesInRadius(Vector3.zero, 30f, results));
                Assert.That(results, Does.Contain(farObject.GetComponent<CreatureAgentView>()));
            }
            finally
            {
                Object.DestroyImmediate(unregisteredObject);
                Object.DestroyImmediate(farObject);
                Object.DestroyImmediate(nearObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(ecosystemObject);
            }
        }

        [Test]
        public void ActionBarRuntimeOwnsStateWithoutCreatingUi()
        {
            GameObject player = new GameObject("Issue82_ActionBarPlayer");
            try
            {
                ActionBarRuntime actionBar = player.AddComponent<ActionBarRuntime>();
                Assert.AreEqual(0, player.GetComponentsInChildren<Canvas>(true).Length);
                Assert.IsTrue(actionBar.AssignItemToSlot(0, "spear"));
                Assert.AreEqual("spear", actionBar.ActiveItemId);
                Assert.IsTrue(actionBar.SetActiveSlot(0));
            }
            finally
            {
                Object.DestroyImmediate(player);
            }
        }

        private static GameObject CreateCreature(string creatureId, bool addBrain)
        {
            GameObject creature = new GameObject($"Issue82_Creature_{creatureId}");
            creature.AddComponent<CreatureAgentView>().Configure(creatureId);
            creature.AddComponent<CreatureNeedsRuntime>().Configure(creatureId);
            creature.AddComponent<CreatureHealthRuntime>().Configure(creatureId);
            creature.AddComponent<CreatureSimulationLodRuntime>();
            if (addBrain)
            {
                creature.AddComponent<CreatureBehaviorBrain>();
            }

            return creature;
        }
    }
}
