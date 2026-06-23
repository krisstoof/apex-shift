using System;

namespace ApexShift.Core.Survival
{
    public sealed class SurvivalSystem
    {
        private readonly SurvivalRules rules;

        public SurvivalSystem()
            : this(SurvivalRules.CreateDefault())
        {
        }

        public SurvivalSystem(SurvivalRules rules)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }

        public SurvivalRules Rules => rules;

        public SurvivalTickResult Tick(SurvivalStats stats, float deltaTime, bool wantsSprint)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (deltaTime <= 0f)
            {
                return SurvivalTickResult.NoChange(stats);
            }

            bool isSprinting = wantsSprint && CanSprint(stats);
            float healthDelta = 0f;
            float hungerDelta = 0f;
            float staminaDelta = 0f;
            float restDelta = 0f;
            bool died = false;
            string deathReason = string.Empty;

            if (!stats.GodMode)
            {
                hungerDelta = stats.ChangeHunger(-rules.HungerDecayPerSecond * deltaTime);

                float restDecay = isSprinting ? rules.SprintRestDecayPerSecond : rules.RestDecayPerSecond;
                restDelta = stats.ChangeRest(-restDecay * deltaTime);

                if (isSprinting)
                {
                    staminaDelta = stats.ChangeStamina(-rules.SprintStaminaCostPerSecond * deltaTime);
                }
                else
                {
                    staminaDelta = stats.ChangeStamina(GetStaminaRegenRate(stats) * deltaTime);
                }

                if (stats.Hunger <= 0f)
                {
                    healthDelta = stats.ApplyDamage(rules.StarvationDamagePerSecond * deltaTime);
                    if (stats.Health <= 0f)
                    {
                        died = true;
                        deathReason = "starvation";
                    }
                }
                else if (stats.Hunger >= rules.LowHungerThreshold && stats.Rest >= rules.ExhaustedRestThreshold)
                {
                    healthDelta = stats.ApplyHeal(GetHealthRegenRate(stats) * deltaTime);
                }
            }
            else
            {
                staminaDelta = stats.ChangeStamina(GetStaminaRegenRate(stats) * deltaTime);
                if (stats.Hunger >= rules.LowHungerThreshold && stats.Rest >= rules.ExhaustedRestThreshold)
                {
                    healthDelta = stats.ApplyHeal(GetHealthRegenRate(stats) * deltaTime);
                }
            }

            return new SurvivalTickResult(
                stats,
                isSprinting,
                healthDelta,
                hungerDelta,
                staminaDelta,
                restDelta,
                died,
                deathReason);
        }

        public SurvivalTickResult ApplyFood(SurvivalStats stats, float nutrition)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            float hungerDelta = stats.ApplyFood(nutrition);
            return new SurvivalTickResult(stats, false, 0f, hungerDelta, 0f, 0f, false, string.Empty);
        }

        public SurvivalTickResult ApplyDamage(SurvivalStats stats, float amount)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            float before = stats.Health;
            float healthDelta = stats.ApplyDamage(amount);
            bool died = stats.Health <= 0f && before > 0f;
            return new SurvivalTickResult(stats, false, healthDelta, 0f, 0f, 0f, died, died ? "damage" : string.Empty);
        }

        public SurvivalTickResult ApplyHeal(SurvivalStats stats, float amount)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            float healthDelta = stats.ApplyHeal(amount);
            return new SurvivalTickResult(stats, false, healthDelta, 0f, 0f, 0f, false, string.Empty);
        }

        public SurvivalTickResult ReduceHungerEnergy(SurvivalStats stats, float amount)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            float beforeHunger = stats.Hunger;
            float beforeStamina = stats.Stamina;
            float beforeRest = stats.Rest;
            stats.ReduceHungerEnergy(amount);
            return new SurvivalTickResult(
                stats,
                false,
                0f,
                stats.Hunger - beforeHunger,
                stats.Stamina - beforeStamina,
                stats.Rest - beforeRest,
                false,
                string.Empty);
        }

        public SurvivalTickResult RestoreHungerEnergy(SurvivalStats stats, float amount)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            float beforeHunger = stats.Hunger;
            float beforeStamina = stats.Stamina;
            float beforeRest = stats.Rest;
            stats.RestoreHungerEnergy(amount);
            return new SurvivalTickResult(
                stats,
                false,
                0f,
                stats.Hunger - beforeHunger,
                stats.Stamina - beforeStamina,
                stats.Rest - beforeRest,
                false,
                string.Empty);
        }

        public bool SpendStamina(SurvivalStats stats, float amount)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            return stats.SpendStamina(amount);
        }

        public bool CanSprint(SurvivalStats stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            return stats.Stamina > rules.MinimumStaminaToSprint
                   && stats.Hunger > rules.MinimumHungerToSprint
                   && stats.Rest > rules.MinimumRestToSprint;
        }

        public float GetStaminaRegenRate(SurvivalStats stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            float regen = rules.BaseStaminaRegenPerSecond;
            if (stats.Hunger < rules.LowHungerThreshold)
            {
                regen *= rules.LowHungerStaminaRegenMultiplier;
            }

            if (stats.Rest < rules.ExhaustedRestThreshold)
            {
                regen *= rules.ExhaustedRestStaminaRegenMultiplier;
            }

            if (stats.CampfireRegenActive)
            {
                regen *= rules.CampfireStaminaRegenMultiplier;
            }

            return regen;
        }

        public float GetHealthRegenRate(SurvivalStats stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            float regen = rules.HealthRegenPerSecond;
            if (stats.CampfireRegenActive)
            {
                regen *= rules.CampfireHealthRegenMultiplier;
            }

            return regen;
        }

        public float GetSpeedMultiplier(SurvivalStats stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            float multiplier = 1f;
            if (stats.Hunger < rules.LowHungerThreshold)
            {
                multiplier *= rules.LowHungerSpeedMultiplier;
            }

            if (stats.Rest < rules.ExhaustedRestThreshold)
            {
                multiplier *= rules.ExhaustedRestSpeedMultiplier;
            }

            return multiplier;
        }

        public string GetConditionText(SurvivalStats stats)
        {
            if (stats == null)
            {
                throw new ArgumentNullException(nameof(stats));
            }

            if (stats.Hunger <= 0f)
            {
                return "starving";
            }

            bool hungry = stats.Hunger < rules.LowHungerThreshold;
            bool exhausted = stats.Rest < rules.ExhaustedRestThreshold;
            if (hungry && exhausted)
            {
                return "hungry, exhausted";
            }

            if (hungry)
            {
                return "hungry";
            }

            return exhausted ? "exhausted" : "steady";
        }
    }
}
