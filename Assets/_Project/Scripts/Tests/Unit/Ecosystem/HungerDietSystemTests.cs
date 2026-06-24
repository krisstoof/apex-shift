using System.Collections.Generic;
using ApexShift.Core.Ecosystem;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Ecosystem
{
    public sealed class HungerDietSystemTests
    {
        [Test]
        public void ChoosePreferredFoodTypeUsesAvailabilityAndDietPreference()
        {
            HungerDietSystem system = new HungerDietSystem();
            CreatureDietProfile grazer = CreatureDietProfile.GetDefault("grazer");

            FoodKind selected = system.ChoosePreferredFoodType(grazer, new Dictionary<FoodKind, float>
            {
                { FoodKind.Plants, 0.5f },
                { FoodKind.Meat, 10f },
                { FoodKind.Scavenger, 1f }
            });

            Assert.AreEqual(FoodKind.Plants, selected);
        }

        [Test]
        public void VarnakPrefersMeatWhenAvailable()
        {
            HungerDietSystem system = new HungerDietSystem();
            CreatureDietProfile varnak = CreatureDietProfile.GetDefault("varnak");

            FoodKind selected = system.ChoosePreferredFoodType(varnak, new Dictionary<FoodKind, float>
            {
                { FoodKind.Plants, 100f },
                { FoodKind.Meat, 1f },
                { FoodKind.Scavenger, 1f }
            });

            Assert.AreEqual(FoodKind.Meat, selected);
        }

        [Test]
        public void EatWeightsNutritionByDietPreference()
        {
            HungerDietSystem system = new HungerDietSystem();
            CreatureNeedsState needs = new CreatureNeedsState(100f, 1f, 30f, 60f, 90f);
            needs.Tick(50f);

            float appliedNutrition = system.Eat(needs, CreatureDietProfile.GetDefault("grazer"), FoodKind.Plants, 10f);

            Assert.AreEqual(8.5f, appliedNutrition, 0.001f);
            Assert.AreEqual(41.5f, needs.Hunger, 0.001f);
        }

        [Test]
        public void DesperateStageIncreasesSearchRadiusAndRiskTolerance()
        {
            HungerDietSystem system = new HungerDietSystem();
            CreatureNeedsState needs = new CreatureNeedsState(100f, 1f, 30f, 60f, 90f);
            needs.Tick(95f);

            HungerBehaviorParameters behavior = system.GetBehaviorParameters(needs, 50f, 80f);

            Assert.IsTrue(behavior.ShouldSeekFood);
            Assert.IsTrue(behavior.CanUseDesperateFood);
            Assert.AreEqual(80f, behavior.FoodSearchRadius, 0.001f);
            Assert.AreEqual(1f, behavior.RiskTolerance, 0.001f);
        }

        [Test]
        public void HungerStateMirrorsCreatureNeedsState()
        {
            CreatureNeedsState needs = new CreatureNeedsState(100f, 1f, 30f, 60f, 90f);
            needs.Tick(40f);

            HungerState state = HungerState.From(needs);

            Assert.AreEqual(HungerStage.Hungry, state.Stage);
            Assert.IsTrue(state.IsHungry);
            Assert.AreEqual(0.4f, state.HungerRatio, 0.001f);
        }
    }
}
