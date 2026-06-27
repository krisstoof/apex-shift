using ApexShift.Core.Survival;
using NUnit.Framework;

namespace ApexShift.Tests.EditMode.Core
{
    public sealed class SurvivalStatsTests
    {
        [Test]
        public void Restore_ClampsValuesToRules()
        {
            SurvivalRules rules = SurvivalRules.CreateDefault();
            SurvivalStats stats = new SurvivalStats(rules);

            stats.Restore(999f, -10f, 999f, -5f);

            Assert.AreEqual(rules.MaxHealth, stats.Health);
            Assert.AreEqual(0f, stats.Hunger);
            Assert.AreEqual(rules.MaxStamina, stats.Stamina);
            Assert.AreEqual(0f, stats.Rest);
        }

        [Test]
        public void GodMode_PreventsDamageAndHungerReduction()
        {
            SurvivalStats stats = new SurvivalStats(health: 50f, hunger: 50f, stamina: 50f, rest: 50f);
            SurvivalSystem system = new SurvivalSystem();
            stats.SetGodMode(true);

            system.ApplyDamage(stats, 25f);
            system.ReduceHungerEnergy(stats, 20f);

            Assert.AreEqual(50f, stats.Health);
            Assert.AreEqual(50f, stats.Hunger);
            Assert.AreEqual(50f, stats.Stamina);
            Assert.AreEqual(50f, stats.Rest);
        }

        [Test]
        public void Tick_StarvingPlayerTakesDamage()
        {
            SurvivalStats stats = new SurvivalStats(health: 50f, hunger: 0f, stamina: 50f, rest: 50f);
            SurvivalSystem system = new SurvivalSystem();

            SurvivalTickResult result = system.Tick(stats, deltaTime: 1f, wantsSprint: false);

            Assert.Less(stats.Health, 50f);
            Assert.Less(result.HealthDelta, 0f);
        }

        [Test]
        public void CanSprint_ReturnsFalse_WhenRestIsTooLow()
        {
            SurvivalStats stats = new SurvivalStats(health: 100f, hunger: 100f, stamina: 100f, rest: 0f);
            SurvivalSystem system = new SurvivalSystem();

            Assert.IsFalse(system.CanSprint(stats));
        }
    }
}
