namespace ApexShift.Core.Ecosystem
{
    public readonly struct HungerBehaviorParameters
    {
        public HungerBehaviorParameters(float foodSearchRadius, float riskTolerance, bool shouldSeekFood, bool canUseDesperateFood)
        {
            FoodSearchRadius = foodSearchRadius;
            RiskTolerance = riskTolerance;
            ShouldSeekFood = shouldSeekFood;
            CanUseDesperateFood = canUseDesperateFood;
        }

        public float FoodSearchRadius { get; }
        public float RiskTolerance { get; }
        public bool ShouldSeekFood { get; }
        public bool CanUseDesperateFood { get; }
    }
}
