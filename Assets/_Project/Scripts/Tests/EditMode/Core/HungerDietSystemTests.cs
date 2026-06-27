using System.Collections.Generic;
using ApexShift.Core.Ecosystem;
using NUnit.Framework;

namespace ApexShift.Tests.EditMode.Core
{
    public sealed class HungerDietSystemTests
    {
        [Test]
        public void Tick_IncreasesHungerAndUpdatesStage()
        {
            CreatureNeedsState needs = new CreatureNeedsState(
                maxHunger: 100f,
                hungerGrowthRate: 10f,
                hungryThreshold: 25f,
                starvingThreshold: 60f,
                desperateThreshold: 90f);
            HungerDietSystem system = new HungerDietSystem();

            system.Tick(needs, deltaSeconds: 3f);

            Assert.AreEqual(30f, needs.Hunger);
            Assert.AreEqual(HungerStage.Hungry, needs.Stage);
        }

        [Test]
        public void ChoosePreferredFoodType_SelectsHighestViableWeightedFood()
        {
            HungerDietSystem system = new HungerDietSystem();
            CreatureDietProfile diet = CreatureDietProfile.Varnak();
            Dictionary<FoodKind, float> available = new Dictionary<FoodKind, float>
            {
                { FoodKind.Plants, 100f },
                { FoodKind.Meat, 10f },
                { FoodKind.Scavenger, 30f }
            };

            FoodKind chosen = system.ChoosePreferredFoodType(diet, available);

            Assert.AreEqual(FoodKind.Scavenger, chosen);
        }

        [Test]
        public void Eat_ReducesHungerByWeightedNutrition()
        {
            CreatureNeedsState needs = new CreatureNeedsState(
                maxHunger: 100f,
                hungerGrowthRate: 0f,
                hungryThreshold: 25f,
                starvingThreshold: 60f,
                desperateThreshold: 90f);
            needs.SetHunger(50f);
            HungerDietSystem system = new HungerDietSystem();
            CreatureDietProfile diet = new CreatureDietProfile(plant: 0.5f, meat: 1f, scavenger: 0f);

            float applied = system.Eat(needs, diet, FoodKind.Plants, nutrition: 20f);

            Assert.AreEqual(10f, applied);
            Assert.AreEqual(40f, needs.Hunger);
        }

        [Test]
        public void GetBehaviorParameters_IncreasesRiskWhenDesperate()
        {
            CreatureNeedsState needs = new CreatureNeedsState(
                maxHunger: 100f,
                hungerGrowthRate: 0f,
                hungryThreshold: 25f,
                starvingThreshold: 60f,
                desperateThreshold: 90f);
            needs.SetHunger(95f);
            HungerDietSystem system = new HungerDietSystem();

            HungerBehaviorParameters parameters = system.GetBehaviorParameters(needs);

            Assert.IsTrue(parameters.ShouldSeekFood);
            Assert.AreEqual(1f, parameters.RiskTolerance);
        }
    }
}
