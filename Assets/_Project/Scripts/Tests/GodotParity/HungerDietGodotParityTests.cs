using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Ecosystem;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.GodotParity
{
    /// <summary>
    /// Godot parity tests for apex-shift-2d/scripts/core/survival/hunger_diet.gd.
    /// These tests assert migration behavior, not Unity-only implementation details.
    /// </summary>
    public sealed class HungerDietGodotParityTests
    {
        [Test]
        public void MovementIncreasesHungerFasterThanIdle()
        {
            CreatureNeedsState idle = new CreatureNeedsState(100f, 20f, 35f, 60f, 82f);
            CreatureNeedsState moving = new CreatureNeedsState(100f, 20f, 35f, 60f, 82f);

            idle.Tick(10f, 0f);
            moving.Tick(10f, 1f);

            Assert.Greater(moving.Hunger, idle.Hunger);
        }

        [Test]
        public void EatingPreferredFoodReducesHungerMoreThanNonPreferredFood()
        {
            GameObject plantGrazer = new GameObject("GrazerPlants");
            GameObject meatGrazer = new GameObject("GrazerMeat");
            try
            {
                CreatureNeedsRuntime plantNeeds = plantGrazer.AddComponent<CreatureNeedsRuntime>();
                CreatureNeedsRuntime meatNeeds = meatGrazer.AddComponent<CreatureNeedsRuntime>();
                plantNeeds.Configure("grazer");
                meatNeeds.Configure("grazer");
                plantNeeds.RestoreNeeds(80f, 0.5f);
                meatNeeds.RestoreNeeds(80f, 0.5f);

                float plantReduction = plantNeeds.Eat(FoodKind.Plants, 20f);
                float meatReduction = meatNeeds.Eat(FoodKind.Meat, 20f);

                Assert.Greater(plantReduction, meatReduction);
                Assert.Less(plantNeeds.State.Hunger, meatNeeds.State.Hunger);
            }
            finally
            {
                Object.DestroyImmediate(plantGrazer);
                Object.DestroyImmediate(meatGrazer);
            }
        }

        [Test]
        public void HungerStagesMatchGodotThresholdOrder()
        {
            CreatureNeedsState state = new CreatureNeedsState(100f, 20f, 35f, 60f, 82f);

            state.SetHunger(34f);
            Assert.AreEqual(HungerStage.Satisfied, state.Stage);

            state.SetHunger(35f);
            Assert.AreEqual(HungerStage.Hungry, state.Stage);

            state.SetHunger(60f);
            Assert.AreEqual(HungerStage.Starving, state.Stage);

            state.SetHunger(82f);
            Assert.AreEqual(HungerStage.Desperate, state.Stage);
        }

        [Test]
        public void RiskDriveIncreasesWithHungerAndLowEnergy()
        {
            CreatureNeedsState safe = new CreatureNeedsState(100f, 20f, 35f, 60f, 82f);
            CreatureNeedsState risky = new CreatureNeedsState(100f, 20f, 35f, 60f, 82f);

            safe.SetHunger(10f);
            safe.SetEnergy(1f);
            risky.SetHunger(90f);
            risky.SetEnergy(0.2f);

            Assert.Greater(risky.RiskDrive, safe.RiskDrive);
            Assert.GreaterOrEqual(risky.RiskDrive, 0.85f);
        }
    }
}
