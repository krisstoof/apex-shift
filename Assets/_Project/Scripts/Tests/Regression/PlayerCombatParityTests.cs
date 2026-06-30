using ApexShift.Runtime.Events;
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
                target.AddComponent<CapsuleCollider>();
                target.AddComponent<ApexShift.Runtime.Creatures.CreatureHealthRuntime>().Configure("small_prey");

                PlayerCombatRuntime combat = player.GetComponent<PlayerCombatRuntime>();
                bool attacked = combat.TriggerPrimaryAttack();

                Assert.IsTrue(attacked);
                Assert.Less(target.GetComponent<ApexShift.Runtime.Creatures.CreatureHealthRuntime>().CurrentHealth, target.GetComponent<ApexShift.Runtime.Creatures.CreatureHealthRuntime>().MaxHealth);
                Assert.AreEqual(1, GameEventBus.RecentEventCount);
                Assert.AreEqual(GameplayEventKind.PlayerMeleeHit, GameEventBus.RecentEvents[0].kind);
            }
            finally
            {
                Object.DestroyImmediate(target);
                Object.DestroyImmediate(player);
            }
        }
    }
}
