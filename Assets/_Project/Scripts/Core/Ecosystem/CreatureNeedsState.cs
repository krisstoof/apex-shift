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

        public void Eat(float nutrition)
        {
            Hunger -= nutrition;
            if (Hunger < 0) Hunger = 0;
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
