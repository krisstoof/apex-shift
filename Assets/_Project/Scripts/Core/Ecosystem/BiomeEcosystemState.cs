using System;
using ApexShift.Core.Save;

namespace ApexShift.Core.Ecosystem
{
    public sealed class BiomeEcosystemState
    {
        public const float DefaultPlantBiomass = 100f;
        public const float DefaultMaxPlantBiomass = 100f;
        public const float DefaultPlantRegrowthRate = 6f;
        public const float DefaultSmallPreyPopulation = 6f;
        public const float DefaultGrazerPopulation = 3f;

        public BiomeEcosystemState(
            string biomeId,
            string displayName,
            float plantBiomass,
            float maxPlantBiomass,
            float plantRegrowthRate,
            float smallPreyPopulation,
            float grazerPopulation,
            float varnakPopulation = 0f,
            int smallPreyGeneration = 1,
            int grazerGeneration = 1,
            int varnakGeneration = 1)
        {
            BiomeId = NormalizeBiomeId(biomeId);
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? BiomeId : displayName.Trim();
            MaxPlantBiomass = Math.Max(0.01f, maxPlantBiomass);
            PlantBiomass = Clamp(plantBiomass, 0f, MaxPlantBiomass);
            PlantRegrowthRate = Math.Max(0f, plantRegrowthRate);
            SmallPreyPopulation = Math.Max(0f, smallPreyPopulation);
            GrazerPopulation = Math.Max(0f, grazerPopulation);
            VarnakPopulation = Math.Max(0f, varnakPopulation);
            SmallPreyGeneration = Math.Max(1, smallPreyGeneration);
            GrazerGeneration = Math.Max(1, grazerGeneration);
            VarnakGeneration = Math.Max(1, varnakGeneration);
            CurrentNiche = "HERBIVORE";
            Status = "healthy";
            RefreshDerivedState();
        }

        public string BiomeId { get; }
        public string DisplayName { get; }
        public float PlantBiomass { get; private set; }
        public float MaxPlantBiomass { get; }
        public float PlantBiomassPercent { get; private set; }
        public float PlantRegrowthRate { get; }
        public float PlantConsumptionPressure { get; private set; }
        public float OvergrazingPressure { get; private set; }
        public float FoodStress { get; private set; }
        public float SmallPreyPopulation { get; private set; }
        public float GrazerPopulation { get; private set; }
        public float VarnakPopulation { get; private set; }
        public int SmallPreyGeneration { get; private set; }
        public int GrazerGeneration { get; private set; }
        public int VarnakGeneration { get; private set; }
        public string CurrentNiche { get; private set; }
        public string Status { get; private set; }
        public float PopulationCount => SmallPreyPopulation + GrazerPopulation + VarnakPopulation;

        public static BiomeEcosystemState CreateDefault(string biomeId, string displayName = null)
        {
            return new BiomeEcosystemState(
                biomeId,
                displayName,
                DefaultPlantBiomass,
                DefaultMaxPlantBiomass,
                DefaultPlantRegrowthRate,
                DefaultSmallPreyPopulation,
                DefaultGrazerPopulation);
        }

        public static BiomeEcosystemState FromSaveData(BiomeEcosystemSaveData saveData)
        {
            if (saveData == null)
            {
                return CreateDefault("default");
            }

            BiomeEcosystemState state = new BiomeEcosystemState(
                saveData.BiomeId,
                saveData.DisplayName,
                saveData.PlantBiomass,
                saveData.MaxPlantBiomass,
                saveData.PlantRegrowthRate,
                saveData.SmallPreyPopulation,
                saveData.GrazerPopulation,
                saveData.VarnakPopulation,
                saveData.SmallPreyGeneration,
                saveData.GrazerGeneration,
                saveData.VarnakGeneration);

            state.PlantConsumptionPressure = Math.Max(0f, saveData.PlantConsumptionPressure);
            state.OvergrazingPressure = Math.Max(0f, saveData.OvergrazingPressure);
            state.FoodStress = Math.Max(0f, Math.Min(100f, saveData.FoodStress));
            state.CurrentNiche = string.IsNullOrWhiteSpace(saveData.CurrentNiche) ? "HERBIVORE" : saveData.CurrentNiche;
            state.Status = string.IsNullOrWhiteSpace(saveData.Status) ? state.Status : saveData.Status;
            return state;
        }

        public BiomeEcosystemSaveData ToSaveData()
        {
            return new BiomeEcosystemSaveData(
                BiomeId,
                DisplayName,
                PlantBiomass,
                MaxPlantBiomass,
                PlantRegrowthRate,
                PlantConsumptionPressure,
                OvergrazingPressure,
                FoodStress,
                SmallPreyPopulation,
                GrazerPopulation,
                VarnakPopulation,
                SmallPreyGeneration,
                GrazerGeneration,
                VarnakGeneration,
                CurrentNiche,
                Status);
        }

        public void ApplyPlantConsumption(float amount)
        {
            float consumed = Math.Max(0f, amount);
            PlantConsumptionPressure = consumed;
            PlantBiomass = Clamp(PlantBiomass - consumed, 0f, MaxPlantBiomass);
            RefreshDerivedState();
        }

        public void SetPopulations(float smallPrey, float grazers, float varnaks)
        {
            SmallPreyPopulation = Math.Max(0f, smallPrey);
            GrazerPopulation = Math.Max(0f, grazers);
            VarnakPopulation = Math.Max(0f, varnaks);
            RefreshDerivedState();
        }

        public void TickDays(int days)
        {
            if (days <= 0)
            {
                return;
            }

            PlantBiomass = Clamp(PlantBiomass + PlantRegrowthRate * days, 0f, MaxPlantBiomass);
            PlantConsumptionPressure = 0f;
            RefreshDerivedState();
        }

        private void RefreshDerivedState()
        {
            PlantBiomassPercent = MaxPlantBiomass <= 0f ? 0f : Clamp(PlantBiomass / MaxPlantBiomass * 100f, 0f, 100f);
            FoodStress = Clamp(100f - PlantBiomassPercent, 0f, 100f);
            OvergrazingPressure = GrazerPopulation <= 0f ? 0f : Clamp(PlantConsumptionPressure / Math.Max(1f, GrazerPopulation), 0f, 100f);
            Status = PlantBiomassPercent < 25f ? "critical" : PlantBiomassPercent < 55f ? "stressed" : "healthy";
        }

        private static string NormalizeBiomeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "default" : value.Trim().ToLowerInvariant();
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            return value > max ? max : value;
        }
    }
}
