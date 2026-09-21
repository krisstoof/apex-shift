using ApexShift.Runtime.Events;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Regression
{
    public sealed class PlayerCombatParityTests
    {
        [SetUp]
        public void SetUp()
        {
            GameEventBus.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GameEventBus.ClearForTests();
        }

        [Test]
        public void MeleeAttackDamagesFrontTargetAndPublishesCombatEvent()
        {
            GameObject player = new GameObject("Player");
            GameObject target = new GameObject("Creature_small_prey");
            try
            {
                player.transform.position = Vector3.zero;
                player.transform.forward = Vector3.forward;
                player.AddComponent<PlayerCombatRuntime>().SetAttackOrigin(player.transform);

                target.transform.position = new Vector3(0f, 0f, 1.1f);
                target.AddComponent<CreatureHealthRuntime>().Configure("small_prey");
                target.AddComponent<CreatureHitboxRuntime>().Configure("small_prey");

                PlayerCombatRuntime combat = player.GetComponent<PlayerCombatRuntime>();
                bool attacked = combat.TriggerPrimaryAttack();

                Assert.IsTrue(attacked);
                Assert.Less(target.GetComponent<CreatureHealthRuntime>().CurrentHealth, target.GetComponent<CreatureHealthRuntime>().MaxHealth);
                Assert.AreEqual(1, GameEventBus.RecentEventCount);
                Assert.AreEqual(GameplayEventKind.PlayerMeleeHit, GameEventBus.RecentEvents[0].kind);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(player);
            }
        }

        [Test]
        public void UnarmedAttackRespectsLegalRange()
        {
            AssertAttackResult(1.1f, false, true);
            AssertAttackResult(1.8f, false, false);
        }

        [Test]
        public void SpearAttackRespectsLegalRange()
        {
            AssertAttackResult(2.0f, true, true);
            AssertAttackResult(2.7f, true, false);
        }

        [Test]
        public void MeleeAttackDoesNotHitTargetBehindPlayer()
        {
            AssertAttackResult(-1.0f, false, false);
        }

        private static void AssertAttackResult(float targetZ, bool spear, bool expectedHit)
        {
            GameObject player = new GameObject("CombatTestPlayer");
            GameObject target = new GameObject("CombatTestCreature");
            try
            {
                player.transform.forward = Vector3.forward;
                PlayerInventoryRuntime inventory = player.AddComponent<PlayerInventoryRuntime>();
                inventory.EnsureInitialized();
                ActionBarRuntime actionBar = player.AddComponent<ActionBarRuntime>();
                if (spear)
                {
                    inventory.Inventory.AddItem("spear", 1);
                    actionBar.AssignItemToSlot(0, "spear");
                }

                PlayerCombatRuntime combat = player.AddComponent<PlayerCombatRuntime>();
                combat.SetAttackOrigin(player.transform);
                combat.SetInventoryRuntime(inventory);
                combat.SetActionBarRuntime(actionBar);

                target.transform.position = new Vector3(0f, 0f, targetZ);
                target.AddComponent<CreatureHealthRuntime>().Configure("small_prey");
                target.AddComponent<CreatureHitboxRuntime>().Configure("small_prey");
                Physics.SyncTransforms();
                CreatureHealthRuntime health = target.GetComponent<CreatureHealthRuntime>();
                float before = health.CurrentHealth;

                bool attacked = combat.TriggerPrimaryAttack();

                Assert.AreEqual(expectedHit, attacked);
                Assert.AreEqual(expectedHit ? before - (spear ? 18f : 5f) : before, health.CurrentHealth);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(player);
            }
        }
    }
}
