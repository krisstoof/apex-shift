namespace ApexShift.Core.Ecosystem
{
    public readonly struct HungerState
    {
        public HungerState(float hunger, float maxHunger, float energy, HungerStage stage, float riskDrive)
        {
            Hunger = hunger;
            MaxHunger = maxHunger;
            Energy = energy;
            Stage = stage;
            RiskDrive = riskDrive;
        }

        public float Hunger { get; }
        public float MaxHunger { get; }
        public float Energy { get; }
        public HungerStage Stage { get; }
        public float RiskDrive { get; }
        public float HungerRatio => MaxHunger <= 0f ? 0f : Hunger / MaxHunger;
        public bool IsHungry => Stage != HungerStage.Satisfied;
        public bool IsStarving => Stage == HungerStage.Starving || Stage == HungerStage.Desperate;
        public bool IsDesperate => Stage == HungerStage.Desperate;

        public static HungerState From(CreatureNeedsState needs)
        {
            return new HungerState(
                needs.Hunger,
                needs.MaxHunger,
                needs.Energy,
                needs.Stage,
                needs.RiskDrive);
        }
    }
}
