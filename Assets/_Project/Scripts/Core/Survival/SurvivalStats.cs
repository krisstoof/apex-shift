using System;

namespace ApexShift.Core.Survival
{
    public sealed class SurvivalStats
    {
        private readonly SurvivalRules rules;

        public SurvivalStats()
            : this(SurvivalRules.CreateDefault())
        {
        }

        public SurvivalStats(SurvivalRules rules)
        {
            this.rules = rules ?? throw new ArgumentNullException(nameof(rules));
            ResetToMax();
        }

        public SurvivalStats(float health, float hunger, float stamina, float rest, SurvivalRules rules = null)
            : this(rules ?? SurvivalRules.CreateDefault())
        {
            Restore(health, hunger, stamina, rest);
        }

        public float Health { get; private set; }
        public float Hunger { get; private set; }
        public float Stamina { get; private set; }
        public float Rest { get; private set; }
        public bool CampfireRegenActive { get; private set; }
        public float CampfireRegenDistance { get; private set; } = -1f;
        public bool GodMode { get; private set; }

        public bool IsAlive => Health > 0f;
        public bool IsStarving => Hunger <= 0f;

        public void Restore(float health, float hunger, float stamina, float rest)
        {
            Health = Clamp(health, 0f, rules.MaxHealth);
            Hunger = Clamp(hunger, 0f, rules.MaxHunger);
            Stamina = Clamp(stamina, 0f, rules.MaxStamina);
            Rest = Clamp(rest, 0f, rules.MaxRest);
        }

        public void ResetToMax()
        {
            Health = rules.MaxHealth;
            Hunger = rules.MaxHunger;
            Stamina = rules.MaxStamina;
            Rest = rules.MaxRest;
            CampfireRegenActive = false;
            CampfireRegenDistance = -1f;
            GodMode = false;
        }

        public void SetCampfireRegen(bool active, float nearestDistance = -1f)
        {
            CampfireRegenActive = active;
            CampfireRegenDistance = active ? nearestDistance : -1f;
        }

        public void SetGodMode(bool enabled)
        {
            GodMode = enabled;
        }

        public float ChangeHealth(float delta)
        {
            float before = Health;
            Health = Clamp(Health + delta, 0f, rules.MaxHealth);
            return Health - before;
        }

        public float ChangeHunger(float delta)
        {
            float before = Hunger;
            Hunger = Clamp(Hunger + delta, 0f, rules.MaxHunger);
            return Hunger - before;
        }

        public float ChangeStamina(float delta)
        {
            float before = Stamina;
            Stamina = Clamp(Stamina + delta, 0f, rules.MaxStamina);
            return Stamina - before;
        }

        public float ChangeRest(float delta)
        {
            float before = Rest;
            Rest = Clamp(Rest + delta, 0f, rules.MaxRest);
            return Rest - before;
        }

        public bool SpendStamina(float amount)
        {
            if (amount <= 0f || GodMode)
            {
                return true;
            }

            if (Stamina < amount)
            {
                return false;
            }

            ChangeStamina(-amount);
            return true;
        }

        public void RestoreHungerEnergy(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            ChangeHunger(amount);
            ChangeStamina(amount);
            ChangeRest(amount);
        }

        public void ReduceHungerEnergy(float amount)
        {
            if (amount <= 0f || GodMode)
            {
                return;
            }

            ChangeHunger(-amount);
            ChangeStamina(-amount);
            ChangeRest(-amount);
        }

        internal float ApplyDamage(float amount)
        {
            if (amount <= 0f || GodMode)
            {
                return 0f;
            }

            return ChangeHealth(-amount);
        }

        internal float ApplyHeal(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            return ChangeHealth(amount);
        }

        internal float ApplyFood(float nutrition)
        {
            if (nutrition <= 0f)
            {
                return 0f;
            }

            return ChangeHunger(nutrition);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
