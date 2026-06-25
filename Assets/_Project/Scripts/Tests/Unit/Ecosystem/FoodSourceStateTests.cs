using ApexShift.Core.Ecosystem;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Ecosystem
{
    public class FoodSourceStateTests
    {
        [Test]
        public void ConsumingFoodSourceReducesBiomass()
        {
            var state = new FoodSourceState(100f, 5f);
            float nutrition = state.Consume(10f);
            
            Assert.AreEqual(90f, state.Biomass);
            Assert.AreEqual(50f, nutrition);
        }

        [Test]
        public void FoodSourceCannotGoBelowZero()
        {
            var state = new FoodSourceState(100f, 5f);
            float nutrition = state.Consume(150f);
            
            Assert.AreEqual(0f, state.Biomass);
            Assert.AreEqual(500f, nutrition);
            Assert.IsTrue(state.IsEmpty);
        }

        [Test]
        public void RestorationWorks()
        {
            var state = new FoodSourceState(100f, 5f);
            state.Consume(50f);
            state.Restore(20f);
            Assert.AreEqual(70f, state.Biomass);
            
            state.Restore(100f);
            Assert.AreEqual(100f, state.Biomass);
        }

        [Test]
        public void MeatFoodSourceCanProvideNutrition()
        {
            var state = new FoodSourceState(20f, 10f);

            float nutrition = state.Consume(2f);

            Assert.AreEqual(18f, state.Biomass);
            Assert.AreEqual(20f, nutrition);
            Assert.IsFalse(state.IsEmpty);
        }
    }
}
