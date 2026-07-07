using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Survival
{
    public sealed class PlayerSurvivalSleepTests
    {
        [Test]
        public void ApplySleepRecovery_RestoresRestAndStaminaAndConsumesHunger()
        {
            GameObject go = new GameObject("PlayerSurvivalSleepTest");
            try
            {
                PlayerSurvivalRuntime survival = go.AddComponent<PlayerSurvivalRuntime>();
                survival.Restore(80f, 60f, 10f, 15f);

                TentSleepResult result = survival.ApplySleepRecovery(80f, 70f, 12f, 2f, 6f);

                Assert.IsTrue(result.Success);
                Assert.GreaterOrEqual(survival.Stats.Rest, 80f);
                Assert.GreaterOrEqual(survival.Stats.Stamina, 70f);
                Assert.AreEqual(48f, survival.Stats.Hunger, 0.01f);
                Assert.AreEqual(6f, result.HoursAdvanced, 0.01f);
                Assert.Greater(result.RestDelta, 0f);
                Assert.Greater(result.StaminaDelta, 0f);
                Assert.Less(result.HungerDelta, 0f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ApplySleepRecovery_FailsWhenDead()
        {
            GameObject go = new GameObject("PlayerSurvivalSleepTest");
            try
            {
                PlayerSurvivalRuntime survival = go.AddComponent<PlayerSurvivalRuntime>();
                survival.Restore(0f, 60f, 10f, 15f);

                TentSleepResult result = survival.ApplySleepRecovery(80f, 70f, 12f, 2f, 6f);

                Assert.IsFalse(result.Success);
                Assert.AreEqual(0f, survival.Stats.Health, 0.01f);
                Assert.AreEqual(60f, survival.Stats.Hunger, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
