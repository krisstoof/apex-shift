using ApexShift.Core.Ecosystem;
using ApexShift.Core.Save;
using NUnit.Framework;

namespace ApexShift.Tests.GodotParity
{
    public sealed class EcosystemGodotParityTests
    {
        [Test]
        public void BiomassDropsAfterPlantConsumption()
        {
            BiomeEcosystemState state = BiomeEcosystemState.CreateDefault("forest", "Forest");
            float before = state.PlantBiomass;

            state.ApplyPlantConsumption(12f);

            Assert.Less(state.PlantBiomass, before);
            Assert.AreEqual(12f, state.PlantConsumptionPressure, 0.001f);
        }

        [Test]
        public void BiomassRegrowsOverDays()
        {
            BiomeEcosystemState state = new BiomeEcosystemState("forest", "Forest", 40f, 100f, 6f, 6f, 3f);
            state.TickDays(2);

            Assert.AreEqual(52f, state.PlantBiomass, 0.001f);
            Assert.AreEqual(0f, state.PlantConsumptionPressure, 0.001f);
        }

        [Test]
        public void PopulationsAreTrackedPerBiome()
        {
            BiomeEcosystemState meadow = BiomeEcosystemState.CreateDefault("meadow", "Meadow");
            BiomeEcosystemState forest = BiomeEcosystemState.CreateDefault("forest", "Forest");

            meadow.SetPopulations(2f, 1f, 0f);
            forest.SetPopulations(8f, 4f, 1f);

            Assert.AreEqual(3f, meadow.PopulationCount, 0.001f);
            Assert.AreEqual(13f, forest.PopulationCount, 0.001f);
            Assert.AreNotEqual(meadow.PopulationCount, forest.PopulationCount);
        }

        [Test]
        public void TraitsAreReadFromBiomeSaveState()
        {
            BiomeEcosystemSaveData save = new BiomeEcosystemSaveData(
                "meadow",
                "Meadow",
                25f,
                100f,
                6f,
                12f,
                4f,
                75f,
                2f,
                1f,
                0f,
                3,
                2,
                1,
                "OMNIVORE",
                "stressed");

            BiomeEcosystemState state = BiomeEcosystemState.FromSaveData(save);

            Assert.AreEqual("OMNIVORE", state.CurrentNiche);
            Assert.AreEqual("stressed", state.Status);
            Assert.AreEqual(75f, state.FoodStress, 0.001f);
        }

        [Test]
        public void EcosystemStateSurvivesSaveLoadRoundTrip()
        {
            BiomeEcosystemState original = new BiomeEcosystemState("forest", "Forest", 44f, 100f, 7f, 5f, 2f, 1f, 4, 3, 2);
            original.ApplyPlantConsumption(5f);
            BiomeEcosystemSaveData save = original.ToSaveData();
            BiomeEcosystemState restored = BiomeEcosystemState.FromSaveData(save);

            Assert.AreEqual(original.BiomeId, restored.BiomeId);
            Assert.AreEqual(original.PlantBiomass, restored.PlantBiomass, 0.001f);
            Assert.AreEqual(original.SmallPreyPopulation, restored.SmallPreyPopulation, 0.001f);
            Assert.AreEqual(original.GrazerPopulation, restored.GrazerPopulation, 0.001f);
            Assert.AreEqual(original.VarnakPopulation, restored.VarnakPopulation, 0.001f);
            Assert.AreEqual(original.SmallPreyGeneration, restored.SmallPreyGeneration);
            Assert.AreEqual(original.GrazerGeneration, restored.GrazerGeneration);
            Assert.AreEqual(original.VarnakGeneration, restored.VarnakGeneration);
        }
    }
}
