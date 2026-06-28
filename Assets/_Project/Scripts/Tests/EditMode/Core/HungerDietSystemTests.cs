using System.Collections.Generic;
using ApexShift.Core.Ecosystem;
using NUnit.Framework;

namespace ApexShift.Tests.EditMode.Core
{
    public sealed class HungerDietSystemTests
    {
        [Test]
        public void Tick_UsesGodotBaseHungerTimeScale()
        {
            CreatureNeedsState needs = new CreatureNeedsState(
                maxHunger: 1f,
                hungerGrowthRate: 0.2f,
                hungryThreshold: 0.35f,
                starvingThreshold: 0.60f,
                desperateThreshold: 0.82f);
            HungerDietSystem system = new HungerDietSystem();

            system.Tick(needs, deltaSeconds: 1f);

            Assert.AreEqual(0.01f, needs.Hunger, 0.0001f);
            Assert.AreEqual(HungerStage.Satisfied, needs.Stage);
        }

        [Test]
        public void Tick_WithMovementIncreasesHungerFasterAndDrainsEnergy()
        {
            CreatureNeedsState idle = new CreatureNeedsState(1f, 0.2f, 0.35f, 0.60f, 0.82f);
            CreatureNeedsState moving = new CreatureNeedsState(1f, 0.2f, 0.35f, 0.60f, 0.82f);
            HungerDietSystem system = new HungerDietSystem();

            system.Tick(idle, deltaSeconds: 1f, movementIntensity: 0f);
            system.Tick(moving, deltaSeconds: 1f, movementIntensity: 1f);

            Assert.AreEqual(0.01f, idle.Hunger, 0.0001f);
            Assert.AreEqual(0.022f, moving.Hunger, 0.0001f);
            Assert.Less(moving.Energy, idle.Energy);
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
                maxHunger: 1f,
                hungerGrowthRate: 0f,
                hungryThreshold: 0.35f,
                starvingThreshold: 0.60f,
                desperateThreshold: 0.82f);
            needs.SetHunger(0.50f);
            HungerDietSystem system = new HungerDietSystem();
            CreatureDietProfile diet = new CreatureDietProfile(plant: 0.5f, meat: 1f, scavenger: 0f);

            float applied = system.Eat(needs, diet, FoodKind.Plants, nutrition: 0.20f);

            Assert.AreEqual(0.10f, applied, 0.0001f);
            Assert.AreEqual(0.40f, needs.Hunger, 0.0001f);
        }

        [Test]
        public void GetBehaviorParameters_UsesContinuousGodotRiskDrive()
        {
            CreatureNeedsState needs = new CreatureNeedsState(
                maxHunger: 1f,
                hungerGrowthRate: 0f,
                hungryThreshold: 0.35f,
                starvingThreshold: 0.60f,
                desperateThreshold: 0.82f);
            needs.SetHunger(0.80f);
            needs.SetEnergy(0.25f);
            HungerDietSystem system = new HungerDietSystem();

            HungerBehaviorParameters parameters = system.GetBehaviorParameters(needs);

            Assert.IsTrue(parameters.ShouldSeekFood);
            Assert.AreEqual(0.7875f, parameters.RiskTolerance, 0.0001f);
        }
    }
}
