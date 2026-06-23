using System;

namespace ApexShift.Core.Survival
{
    public sealed class SurvivalRules
    {
        public SurvivalRules(
            float maxHealth,
            float maxHunger,
            float maxStamina,
            float maxRest,
            float lowHungerThreshold,
            float exhaustedRestThreshold,
            float hungerDecayPerSecond,
            float restDecayPerSecond,
            float sprintRestDecayPerSecond,
            float sprintStaminaCostPerSecond,
            float starvationDamagePerSecond,
            float healthRegenPerSecond,
            float campfireHealthRegenMultiplier,
            float baseStaminaRegenPerSecond,
            float lowHungerStaminaRegenMultiplier,
            float exhaustedRestStaminaRegenMultiplier,
            float campfireStaminaRegenMultiplier,
            float lowHungerSpeedMultiplier,
            float exhaustedRestSpeedMultiplier,
            float minimumStaminaToSprint,
            float minimumHungerToSprint,
            float minimumRestToSprint,
            float meatNutrition)
        {
            MaxHealth = RequirePositive(maxHealth, nameof(maxHealth));
            MaxHunger = RequirePositive(maxHunger, nameof(maxHunger));
            MaxStamina = RequirePositive(maxStamina, nameof(maxStamina));
            MaxRest = RequirePositive(maxRest, nameof(maxRest));
            LowHungerThreshold = RequireNonNegative(lowHungerThreshold, nameof(lowHungerThreshold));
            ExhaustedRestThreshold = RequireNonNegative(exhaustedRestThreshold, nameof(exhaustedRestThreshold));
            HungerDecayPerSecond = RequireNonNegative(hungerDecayPerSecond, nameof(hungerDecayPerSecond));
            RestDecayPerSecond = RequireNonNegative(restDecayPerSecond, nameof(restDecayPerSecond));
            SprintRestDecayPerSecond = RequireNonNegative(sprintRestDecayPerSecond, nameof(sprintRestDecayPerSecond));
            SprintStaminaCostPerSecond = RequireNonNegative(sprintStaminaCostPerSecond, nameof(sprintStaminaCostPerSecond));
            StarvationDamagePerSecond = RequireNonNegative(starvationDamagePerSecond, nameof(starvationDamagePerSecond));
            HealthRegenPerSecond = RequireNonNegative(healthRegenPerSecond, nameof(healthRegenPerSecond));
            CampfireHealthRegenMultiplier = RequireNonNegative(campfireHealthRegenMultiplier, nameof(campfireHealthRegenMultiplier));
            BaseStaminaRegenPerSecond = RequireNonNegative(baseStaminaRegenPerSecond, nameof(baseStaminaRegenPerSecond));
            LowHungerStaminaRegenMultiplier = RequireNonNegative(lowHungerStaminaRegenMultiplier, nameof(lowHungerStaminaRegenMultiplier));
            ExhaustedRestStaminaRegenMultiplier = RequireNonNegative(exhaustedRestStaminaRegenMultiplier, nameof(exhaustedRestStaminaRegenMultiplier));
            CampfireStaminaRegenMultiplier = RequireNonNegative(campfireStaminaRegenMultiplier, nameof(campfireStaminaRegenMultiplier));
            LowHungerSpeedMultiplier = RequireNonNegative(lowHungerSpeedMultiplier, nameof(lowHungerSpeedMultiplier));
            ExhaustedRestSpeedMultiplier = RequireNonNegative(exhaustedRestSpeedMultiplier, nameof(exhaustedRestSpeedMultiplier));
            MinimumStaminaToSprint = RequireNonNegative(minimumStaminaToSprint, nameof(minimumStaminaToSprint));
            MinimumHungerToSprint = RequireNonNegative(minimumHungerToSprint, nameof(minimumHungerToSprint));
            MinimumRestToSprint = RequireNonNegative(minimumRestToSprint, nameof(minimumRestToSprint));
            MeatNutrition = RequireNonNegative(meatNutrition, nameof(meatNutrition));
        }

        public float MaxHealth { get; }
        public float MaxHunger { get; }
        public float MaxStamina { get; }
        public float MaxRest { get; }
        public float LowHungerThreshold { get; }
        public float ExhaustedRestThreshold { get; }
        public float HungerDecayPerSecond { get; }
        public float RestDecayPerSecond { get; }
        public float SprintRestDecayPerSecond { get; }
        public float SprintStaminaCostPerSecond { get; }
        public float StarvationDamagePerSecond { get; }
        public float HealthRegenPerSecond { get; }
        public float CampfireHealthRegenMultiplier { get; }
        public float BaseStaminaRegenPerSecond { get; }
        public float LowHungerStaminaRegenMultiplier { get; }
        public float ExhaustedRestStaminaRegenMultiplier { get; }
        public float CampfireStaminaRegenMultiplier { get; }
        public float LowHungerSpeedMultiplier { get; }
        public float ExhaustedRestSpeedMultiplier { get; }
        public float MinimumStaminaToSprint { get; }
        public float MinimumHungerToSprint { get; }
        public float MinimumRestToSprint { get; }
        public float MeatNutrition { get; }

        public static SurvivalRules CreateDefault()
        {
            return new SurvivalRules(
                maxHealth: 100f,
                maxHunger: 100f,
                maxStamina: 100f,
                maxRest: 100f,
                lowHungerThreshold: 25f,
                exhaustedRestThreshold: 20f,
                hungerDecayPerSecond: 0.75f,
                restDecayPerSecond: 0.35f,
                sprintRestDecayPerSecond: 0.9f,
                sprintStaminaCostPerSecond: 14f,
                starvationDamagePerSecond: 1f,
                healthRegenPerSecond: 0.45f,
                campfireHealthRegenMultiplier: 2.2f,
                baseStaminaRegenPerSecond: 16f,
                lowHungerStaminaRegenMultiplier: 0.45f,
                exhaustedRestStaminaRegenMultiplier: 0.55f,
                campfireStaminaRegenMultiplier: 1.75f,
                lowHungerSpeedMultiplier: 0.82f,
                exhaustedRestSpeedMultiplier: 0.85f,
                minimumStaminaToSprint: 1f,
                minimumHungerToSprint: 5f,
                minimumRestToSprint: 5f,
                meatNutrition: 32f);
        }

        private static float RequirePositive(float value, string parameterName)
        {
            if (value <= 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than zero.");
            }

            return value;
        }

        private static float RequireNonNegative(float value, string parameterName)
        {
            if (value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value cannot be negative.");
            }

            return value;
        }
    }
}
