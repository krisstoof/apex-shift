using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class BiomeEcosystemSaveData
    {
        public string biomeId;
        public string displayName;
        public float plantBiomass;
        public float maxPlantBiomass;
        public float plantRegrowthRate;
        public float plantConsumptionPressure;
        public float overgrazingPressure;
        public float foodStress;
        public float smallPreyPopulation;
        public float grazerPopulation;
        public float varnakPopulation;
        public int smallPreyGeneration;
        public int grazerGeneration;
        public int varnakGeneration;
        public string currentNiche;
        public string status;

        public string BiomeId => biomeId;
        public string DisplayName => displayName;
        public float PlantBiomass => plantBiomass;
        public float MaxPlantBiomass => maxPlantBiomass;
        public float PlantRegrowthRate => plantRegrowthRate;
        public float PlantConsumptionPressure => plantConsumptionPressure;
        public float OvergrazingPressure => overgrazingPressure;
        public float FoodStress => foodStress;
        public float SmallPreyPopulation => smallPreyPopulation;
        public float GrazerPopulation => grazerPopulation;
        public float VarnakPopulation => varnakPopulation;
        public int SmallPreyGeneration => smallPreyGeneration;
        public int GrazerGeneration => grazerGeneration;
        public int VarnakGeneration => varnakGeneration;
        public string CurrentNiche => currentNiche;
        public string Status => status;

        public BiomeEcosystemSaveData()
        {
        }

        public BiomeEcosystemSaveData(
            string biomeId,
            string displayName,
            float plantBiomass,
            float maxPlantBiomass,
            float plantRegrowthRate,
            float plantConsumptionPressure,
            float overgrazingPressure,
            float foodStress,
            float smallPreyPopulation,
            float grazerPopulation,
            float varnakPopulation,
            int smallPreyGeneration,
            int grazerGeneration,
            int varnakGeneration,
            string currentNiche,
            string status)
        {
            this.biomeId = string.IsNullOrWhiteSpace(biomeId) ? "default" : biomeId.Trim().ToLowerInvariant();
            this.displayName = string.IsNullOrWhiteSpace(displayName) ? this.biomeId : displayName.Trim();
            this.maxPlantBiomass = Math.Max(0.01f, maxPlantBiomass);
            this.plantBiomass = Math.Max(0f, Math.Min(this.maxPlantBiomass, plantBiomass));
            this.plantRegrowthRate = Math.Max(0f, plantRegrowthRate);
            this.plantConsumptionPressure = Math.Max(0f, plantConsumptionPressure);
            this.overgrazingPressure = Math.Max(0f, overgrazingPressure);
            this.foodStress = Math.Max(0f, Math.Min(100f, foodStress));
            this.smallPreyPopulation = Math.Max(0f, smallPreyPopulation);
            this.grazerPopulation = Math.Max(0f, grazerPopulation);
            this.varnakPopulation = Math.Max(0f, varnakPopulation);
            this.smallPreyGeneration = Math.Max(1, smallPreyGeneration);
            this.grazerGeneration = Math.Max(1, grazerGeneration);
            this.varnakGeneration = Math.Max(1, varnakGeneration);
            this.currentNiche = string.IsNullOrWhiteSpace(currentNiche) ? "HERBIVORE" : currentNiche;
            this.status = string.IsNullOrWhiteSpace(status) ? "healthy" : status;
        }
    }
}
