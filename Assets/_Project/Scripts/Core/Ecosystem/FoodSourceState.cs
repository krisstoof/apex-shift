namespace ApexShift.Core.Ecosystem
{
    public class FoodSourceState
    {
        public float Biomass { get; private set; }
        public float MaxBiomass { get; }
        public float NutritionPerBiomass { get; }

        public FoodSourceState(float maxBiomass, float nutritionPerBiomass)
        {
            MaxBiomass = maxBiomass;
            Biomass = maxBiomass;
            NutritionPerBiomass = nutritionPerBiomass;
        }

        public float Consume(float requestedBiomass)
        {
            float amountToConsume = requestedBiomass > Biomass ? Biomass : requestedBiomass;
            Biomass -= amountToConsume;
            return amountToConsume * NutritionPerBiomass;
        }

        public void Restore(float amount)
        {
            Biomass = (Biomass + amount) > MaxBiomass ? MaxBiomass : (Biomass + amount);
        }
        
        public bool IsEmpty => Biomass <= 0.001f;
    }
}
