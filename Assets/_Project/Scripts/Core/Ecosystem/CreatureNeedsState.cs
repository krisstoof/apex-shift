namespace ApexShift.Core.Ecosystem
{
    public enum HungerStage
    {
        Satisfied,
        Hungry,
        Starving,
        Desperate
    }

    public class CreatureNeedsState
    {
        // Ported from apex-shift-2d/scripts/core/survival/hunger_diet.gd.
        public const float BaseHungerTimeScale = 0.05f;
        public const float MovementHungerTimeScale = 0.06f;
        public const float BaseEnergyDrainRate = 0.015f;
        public const float MovementEnergyDrainRate = 0.035f;
        public const float HungerRecoveryEnergyRate = 0.02f;

        public float Hunger { get; private set; }
        public float MaxHunger { get; }
        public float HungerGrowthRate { get; }
        
        public float Energy { get; private set; }
        public float MaxEnergy { get; }

        public float HungryThreshold { get; }
        public float StarvingThreshold { get; }
        public float DesperateThreshold { get; }

        public CreatureNeedsState(
            float maxHunger, 
            float hungerGrowthRate, 
            float hungryThreshold, 
            float starvingThreshold, 
            float desperateThreshold,
            float maxEnergy = 1f)
        {
            MaxHunger = maxHunger <= 0.01f ? 0.01f : maxHunger;
            HungerGrowthRate = hungerGrowthRate < 0f ? 0f : hungerGrowthRate;
            HungryThreshold = Clamp(hungryThreshold, 0.01f, MaxHunger * 0.98f);
            StarvingThreshold = Clamp(
                starvingThreshold < HungryThreshold + 0.01f ? HungryThreshold + 0.01f : starvingThreshold,
                0.02f,
                MaxHunger * 0.99f);
            DesperateThreshold = Clamp(
                desperateThreshold < StarvingThreshold + 0.01f ? StarvingThreshold + 0.01f : desperateThreshold,
                0.03f,
                MaxHunger);
            MaxEnergy = maxEnergy <= 0.01f ? 0.01f : maxEnergy;
            Energy = MaxEnergy;
            Hunger = 0f;
        }

        public float HungerRatio => Clamp01(Hunger / MaxHunger);
        public float EnergyRatio => Clamp01(Energy / MaxEnergy);

        public void Tick(float deltaTime)
        {
            Tick(deltaTime, 0f);
        }

        public void Tick(float deltaTime, float movementIntensity)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            float movement = Clamp01(movementIntensity);
            float baseHungerDelta = HungerGrowthRate * BaseHungerTimeScale * deltaTime;
            float movementHungerDelta = HungerGrowthRate * movement * MovementHungerTimeScale * deltaTime;
            SetHunger(Hunger + baseHungerDelta + movementHungerDelta);

            float energyDrain = deltaTime * (BaseEnergyDrainRate + movement * MovementEnergyDrainRate);
            float energyRecovery = deltaTime * HungerRecoveryEnergyRate * (1f - HungerRatio);
            SetEnergy(Energy - energyDrain + energyRecovery);
        }

        public void Eat(float nutrition)
        {
            if (nutrition <= 0f)
            {
                return;
            }

            SetHunger(Hunger - nutrition);
            SetEnergy(Energy + nutrition * 0.35f);
        }

        public void SetHunger(float hunger)
        {
            Hunger = Clamp(hunger, 0f, MaxHunger);
        }

        public void SetEnergy(float energy)
        {
            Energy = Clamp(energy, 0f, MaxEnergy);
        }

        public HungerStage Stage
        {
            get
            {
                if (Hunger >= DesperateThreshold) return HungerStage.Desperate;
                if (Hunger >= StarvingThreshold) return HungerStage.Starving;
                if (Hunger >= HungryThreshold) return HungerStage.Hungry;
                return HungerStage.Satisfied;
            }
        }

        public float RiskDrive => Clamp01(HungerRatio * 0.75f + (1f - EnergyRatio) * 0.25f);
        
        public bool IsHungry => Stage != HungerStage.Satisfied;

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (max < min)
            {
                max = min;
            }

            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
