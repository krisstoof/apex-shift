using System;

namespace ApexShift.Core.Survival
{
    public readonly struct SurvivalTickResult
    {
        public SurvivalTickResult(
            SurvivalStats stats,
            bool isSprinting,
            float healthDelta,
            float hungerDelta,
            float staminaDelta,
            float restDelta,
            bool died,
            string deathReason)
        {
            Stats = stats;
            IsSprinting = isSprinting;
            HealthDelta = healthDelta;
            HungerDelta = hungerDelta;
            StaminaDelta = staminaDelta;
            RestDelta = restDelta;
            Died = died;
            DeathReason = deathReason ?? string.Empty;
        }

        public SurvivalStats Stats { get; }
        public bool IsSprinting { get; }
        public float HealthDelta { get; }
        public float HungerDelta { get; }
        public float StaminaDelta { get; }
        public float RestDelta { get; }
        public bool Died { get; }
        public string DeathReason { get; }
        public bool IsStarving => Stats != null && Stats.IsStarving;
        public bool Changed => !NearlyZero(HealthDelta) || !NearlyZero(HungerDelta) || !NearlyZero(StaminaDelta) || !NearlyZero(RestDelta);

        public static SurvivalTickResult NoChange(SurvivalStats stats)
        {
            return new SurvivalTickResult(stats, false, 0f, 0f, 0f, 0f, false, string.Empty);
        }

        private static bool NearlyZero(float value)
        {
            return Math.Abs(value) <= 0.00001f;
        }
    }
}
