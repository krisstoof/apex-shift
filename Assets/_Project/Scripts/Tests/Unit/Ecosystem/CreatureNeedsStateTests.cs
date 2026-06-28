using ApexShift.Core.Ecosystem;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Ecosystem
{
    public class CreatureNeedsStateTests
    {
        [Test]
        public void HungerIncreasesOverTime()
        {
            var state = new CreatureNeedsState(1f, 0.2f, 0.35f, 0.60f, 0.82f);
            state.Tick(10f);
            Assert.AreEqual(0.1f, state.Hunger, 0.001f);
        }

        [Test]
        public void EatingPlantsReducesHunger()
        {
            var state = new CreatureNeedsState(1f, 0.2f, 0.35f, 0.60f, 0.82f);
            state.SetHunger(0.50f);
            state.Eat(0.20f);
            Assert.AreEqual(0.30f, state.Hunger, 0.001f);
        }

        [Test]
        public void HungerStagesAreCorrect()
        {
            var state = new CreatureNeedsState(1f, 0.2f, 0.35f, 0.60f, 0.82f);
            
            Assert.AreEqual(HungerStage.Satisfied, state.Stage);
            
            state.SetHunger(0.40f);
            Assert.AreEqual(HungerStage.Hungry, state.Stage);
            
            state.SetHunger(0.60f);
            Assert.AreEqual(HungerStage.Starving, state.Stage);
            
            state.SetHunger(0.82f);
            Assert.AreEqual(HungerStage.Desperate, state.Stage);
        }

        [Test]
        public void DefaultDietProfilesMatchExpectations()
        {
            var smallPrey = CreatureDietProfile.GetDefault("small_prey");
            Assert.IsTrue(smallPrey.PlantDiet);
            Assert.IsFalse(smallPrey.MeatDiet);

            var grazer = CreatureDietProfile.GetDefault("grazer");
            Assert.IsTrue(grazer.PlantDiet);

            var varnak = CreatureDietProfile.GetDefault("varnak");
            Assert.IsFalse(varnak.PlantDiet);
            Assert.IsTrue(varnak.MeatDiet);
        }

        [Test]
        public void SetHungerCanSeedCreatureAsHungry()
        {
            var state = new CreatureNeedsState(1f, 0.2f, 0.35f, 0.60f, 0.82f);

            state.SetHunger(0.40f);

            Assert.AreEqual(HungerStage.Hungry, state.Stage);
            Assert.IsTrue(state.IsHungry);
        }

        [Test]
        public void SetHungerClampsToMaximum()
        {
            var state = new CreatureNeedsState(1f, 0.2f, 0.35f, 0.60f, 0.82f);

            state.SetHunger(999f);

            Assert.AreEqual(1f, state.Hunger);
            Assert.AreEqual(HungerStage.Desperate, state.Stage);
        }

        [Test]
        public void TickWithMovementConsumesEnergy()
        {
            var state = new CreatureNeedsState(1f, 0.2f, 0.35f, 0.60f, 0.82f);
            float initialEnergy = state.Energy;

            state.Tick(10f, 1f);

            Assert.Less(state.Energy, initialEnergy);
            Assert.Greater(state.Hunger, 0f);
        }

        [Test]
        public void SetHungerCanSeedAboveHungryThreshold()
        {
            var state = new CreatureNeedsState(1f, 0.2f, 0.35f, 0.60f, 0.82f);

            state.SetHunger(0.36f);

            Assert.IsTrue(state.IsHungry);
            Assert.AreEqual(HungerStage.Hungry, state.Stage);
        }
    }
}
