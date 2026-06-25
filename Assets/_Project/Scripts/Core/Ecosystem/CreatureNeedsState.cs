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
            float maxEnergy = 100f)
        {
            MaxHunger = maxHunger;
            HungerGrowthRate = hungerGrowthRate;
            HungryThreshold = hungryThreshold;
            StarvingThreshold = starvingThreshold;
            DesperateThreshold = desperateThreshold;
            MaxEnergy = maxEnergy;
            Energy = maxEnergy;
            Hunger = 0f;
        }

        public void Tick(float deltaTime)
        {
            Hunger += HungerGrowthRate * deltaTime;
            if (Hunger > MaxHunger) Hunger = MaxHunger;
        }

        public void Tick(float deltaTime, float movementIntensity)
        {
            float movement = movementIntensity < 0f ? 0f : movementIntensity > 1f ? 1f : movementIntensity;
            Hunger += HungerGrowthRate * deltaTime * (1f + movement * 0.35f);
            if (Hunger > MaxHunger) Hunger = MaxHunger;

            float energyDelta = deltaTime * (0.02f + movement * 0.08f);
            Energy -= energyDelta;
            if (Energy < 0f) Energy = 0f;
        }

        public void Eat(float nutrition)
        {
            Hunger -= nutrition;
            if (Hunger < 0) Hunger = 0;

            Energy += nutrition * 0.35f;
            if (Energy > MaxEnergy) Energy = MaxEnergy;
        }

        public void SetHunger(float hunger)
        {
            if (hunger < 0f)
            {
                Hunger = 0f;
                return;
            }

            Hunger = hunger > MaxHunger ? MaxHunger : hunger;
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

        public float RiskDrive => Hunger / MaxHunger;
        
        public bool IsHungry => Stage != HungerStage.Satisfied;
    }
}
