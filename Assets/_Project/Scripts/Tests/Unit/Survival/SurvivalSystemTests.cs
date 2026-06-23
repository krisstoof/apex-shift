using ApexShift.Core.Survival;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Survival
{
    public class SurvivalSystemTests
    {
        private SurvivalRules rules;
        private SurvivalSystem system;

        [SetUp]
        public void SetUp()
        {
            rules = SurvivalRules.CreateDefault();
            system = new SurvivalSystem(rules);
        }

        [Test]
        public void DefaultStatsStartAtMaximumValues()
        {
            SurvivalStats stats = new SurvivalStats(rules);

            Assert.AreEqual(rules.MaxHealth, stats.Health);
            Assert.AreEqual(rules.MaxHunger, stats.Hunger);
            Assert.AreEqual(rules.MaxStamina, stats.Stamina);
            Assert.AreEqual(rules.MaxRest, stats.Rest);
            Assert.IsFalse(stats.CampfireRegenActive);
            Assert.IsFalse(stats.GodMode);
        }

        [Test]
        public void TickDecreasesHungerOverTime()
        {
            SurvivalStats stats = new SurvivalStats(rules);

            SurvivalTickResult result = system.Tick(stats, 10f, false);

            Assert.IsTrue(result.Changed);
            Assert.AreEqual(92.5f, stats.Hunger, 0.001f);
            Assert.AreEqual(-7.5f, result.HungerDelta, 0.001f);
        }

        [Test]
        public void SprintConsumesStaminaAndRestFaster()
        {
            SurvivalStats stats = new SurvivalStats(rules);

            SurvivalTickResult result = system.Tick(stats, 1f, true);

            Assert.IsTrue(result.IsSprinting);
            Assert.AreEqual(86f, stats.Stamina, 0.001f);
            Assert.AreEqual(99.1f, stats.Rest, 0.001f);
        }

        [Test]
        public void SprintDoesNotStartWhenStaminaIsTooLow()
        {
            SurvivalStats stats = new SurvivalStats(rules);
            stats.ChangeStamina(-100f);

            SurvivalTickResult result = system.Tick(stats, 1f, true);

            Assert.IsFalse(result.IsSprinting);
            Assert.Greater(stats.Stamina, 0f);
        }

        [Test]
        public void StaminaRegeneratesWhenNotSprinting()
        {
            SurvivalStats stats = new SurvivalStats(rules);
            stats.ChangeStamina(-50f);

            system.Tick(stats, 1f, false);

            Assert.AreEqual(66f, stats.Stamina, 0.001f);
        }

        [Test]
        public void HungerAndExhaustionSlowStaminaRegeneration()
        {
            SurvivalStats stats = new SurvivalStats(rules);
            stats.Restore(100f, 20f, 50f, 10f);

            system.Tick(stats, 1f, false);

            float expected = 50f + rules.BaseStaminaRegenPerSecond
                * rules.LowHungerStaminaRegenMultiplier
                * rules.ExhaustedRestStaminaRegenMultiplier;
            Assert.AreEqual(expected, stats.Stamina, 0.001f);
        }

        [Test]
        public void CampfireMultipliesStaminaAndHealthRegeneration()
        {
            SurvivalStats stats = new SurvivalStats(rules);
            stats.Restore(50f, 100f, 50f, 100f);
            stats.SetCampfireRegen(true, 3f);

            system.Tick(stats, 1f, false);

            Assert.AreEqual(50f + rules.BaseStaminaRegenPerSecond * rules.CampfireStaminaRegenMultiplier, stats.Stamina, 0.001f);
            Assert.AreEqual(50f + rules.HealthRegenPerSecond * rules.CampfireHealthRegenMultiplier, stats.Health, 0.001f);
        }

        [Test]
        public void StarvationDamagesHealthAndReportsDeathReason()
        {
            SurvivalStats stats = new SurvivalStats(rules);
            stats.Restore(1f, 0.1f, 100f, 100f);

            SurvivalTickResult result = system.Tick(stats, 1f, false);

            Assert.AreEqual(0f, stats.Hunger, 0.001f);
            Assert.AreEqual(0f, stats.Health, 0.001f);
            Assert.IsTrue(result.Died);
            Assert.AreEqual("starvation", result.DeathReason);
        }

        [Test]
        public void ApplyingFoodRestoresHungerAndCapsAtMaximum()
        {
            SurvivalStats stats = new SurvivalStats(rules);
            stats.ChangeHunger(-20f);

            SurvivalTickResult result = system.ApplyFood(stats, rules.MeatNutrition);

            Assert.IsTrue(result.Changed);
            Assert.AreEqual(100f, stats.Hunger, 0.001f);
        }

        [Test]
        public void SpeedMultiplierMatchesHungryAndExhaustedState()
        {
            SurvivalStats stats = new SurvivalStats(rules);
            stats.Restore(100f, 10f, 100f, 10f);

            float multiplier = system.GetSpeedMultiplier(stats);

            Assert.AreEqual(rules.LowHungerSpeedMultiplier * rules.ExhaustedRestSpeedMultiplier, multiplier, 0.001f);
            Assert.AreEqual("hungry, exhausted", system.GetConditionText(stats));
        }

        [Test]
        public void GodModePreventsDecayDamageAndStaminaSpend()
        {
            SurvivalStats stats = new SurvivalStats(rules);
            stats.Restore(10f, 0f, 10f, 10f);
            stats.SetGodMode(true);

            SurvivalTickResult result = system.Tick(stats, 1f, true);

            Assert.IsFalse(result.IsSprinting);
            Assert.AreEqual(10f, stats.Health, 0.001f);
            Assert.AreEqual(0f, stats.Hunger, 0.001f);
            Assert.AreEqual(10f, stats.Rest, 0.001f);
            Assert.Greater(stats.Stamina, 10f);
            Assert.IsTrue(system.SpendStamina(stats, 999f));
        }
    }
}
