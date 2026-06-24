using ApexShift.Core.Ecosystem;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Ecosystem
{
    public class CreatureNeedsStateTests
    {
        [Test]
        public void HungerIncreasesOverTime()
        {
            var state = new CreatureNeedsState(100f, 1f, 30f, 60f, 90f);
            state.Tick(10f);
            Assert.AreEqual(10f, state.Hunger);
        }

        [Test]
        public void EatingPlantsReducesHunger()
        {
            var state = new CreatureNeedsState(100f, 1f, 30f, 60f, 90f);
            state.Tick(50f);
            state.Eat(20f);
            Assert.AreEqual(30f, state.Hunger);
        }

        [Test]
        public void HungerStagesAreCorrect()
        {
            var state = new CreatureNeedsState(100f, 1f, 30f, 60f, 90f);
            
            Assert.AreEqual(HungerStage.Satisfied, state.Stage);
            
            state.Tick(40f);
            Assert.AreEqual(HungerStage.Hungry, state.Stage);
            
            state.Tick(30f);
            Assert.AreEqual(HungerStage.Starving, state.Stage);
            
            state.Tick(25f);
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
    }
}
