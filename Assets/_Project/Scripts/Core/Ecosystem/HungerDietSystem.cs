using System;
using System.Collections.Generic;

namespace ApexShift.Core.Ecosystem
{
    public sealed class HungerDietSystem
    {
        public void Tick(CreatureNeedsState needs, float deltaSeconds)
        {
            Tick(needs, deltaSeconds, 0f);
        }

        public void Tick(CreatureNeedsState needs, float deltaSeconds, float movementIntensity)
        {
            if (needs == null)
            {
                throw new ArgumentNullException(nameof(needs));
            }

            needs.Tick(deltaSeconds, movementIntensity);
        }

        public HungerState GetState(CreatureNeedsState needs)
        {
            if (needs == null)
            {
                throw new ArgumentNullException(nameof(needs));
            }

            return HungerState.From(needs);
        }

        public FoodKind ChoosePreferredFoodType(CreatureDietProfile diet, IReadOnlyDictionary<FoodKind, float> availableFood)
        {
            if (diet == null)
            {
                throw new ArgumentNullException(nameof(diet));
            }

            FoodKind bestKind = FoodKind.Plants;
            float bestScore = 0f;
            bool found = false;

            foreach (KeyValuePair<FoodKind, float> entry in availableFood)
            {
                FoodPreference preference = new FoodPreference(entry.Key, entry.Value, diet.GetPreference(entry.Key));
                if (!preference.IsViable)
                {
                    continue;
                }

                if (!found || preference.Score > bestScore)
                {
                    found = true;
                    bestKind = preference.Kind;
                    bestScore = preference.Score;
                }
            }

            return found ? bestKind : FoodKind.Plants;
        }

        public float Eat(CreatureNeedsState needs, CreatureDietProfile diet, FoodKind foodKind, float nutrition)
        {
            if (needs == null)
            {
                throw new ArgumentNullException(nameof(needs));
            }

            if (diet == null)
            {
                throw new ArgumentNullException(nameof(diet));
            }

            if (nutrition <= 0f)
            {
                return 0f;
            }

            float preference = Math.Max(diet.GetPreference(foodKind), 0.05f);
            float weightedNutrition = nutrition * preference;
            needs.Eat(weightedNutrition);
            return weightedNutrition;
        }

        public HungerBehaviorParameters GetBehaviorParameters(
            CreatureNeedsState needs,
            float normalFoodSearchRadius = 50f,
            float desperateFoodSearchRadius = 80f)
        {
            if (needs == null)
            {
                throw new ArgumentNullException(nameof(needs));
            }

            HungerState state = HungerState.From(needs);
            float radius = state.IsDesperate
                ? Math.Max(normalFoodSearchRadius, desperateFoodSearchRadius)
                : normalFoodSearchRadius;

            return new HungerBehaviorParameters(
                radius,
                state.RiskDrive,
                state.IsHungry,
                state.IsDesperate);
        }
    }
}
