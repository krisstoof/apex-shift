using ApexShift.Core.Ecosystem;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Ecosystem
{
    public sealed class BiomeEcosystemStateTests
    {
        [Test]
        public void DefaultBiomeStartsHealthyWithBiomassAndPopulations()
        {
            BiomeEcosystemState state = BiomeEcosystemState.CreateDefault("south_thicket", "South Thicket");

            Assert.AreEqual("south_thicket", state.BiomeId);
            Assert.AreEqual("South Thicket", state.DisplayName);
            Assert.AreEqual(100f, state.PlantBiomassPercent, 0.001f);
            Assert.AreEqual("healthy", state.Status);
            Assert.Greater(state.PopulationCount, 0f);
        }

        [Test]
        public void PlantConsumptionCreatesFoodStressAndCriticalStatus()
        {
            BiomeEcosystemState state = BiomeEcosystemState.CreateDefault("westwood");

            state.ApplyPlantConsumption(80f);

            Assert.AreEqual(20f, state.PlantBiomassPercent, 0.001f);
            Assert.AreEqual(80f, state.FoodStress, 0.001f);
            Assert.AreEqual("critical", state.Status);
        }

        [Test]
        public void DailyTickRegrowsPlantBiomass()
        {
            BiomeEcosystemState state = BiomeEcosystemState.CreateDefault("westwood");
            state.ApplyPlantConsumption(30f);

            state.TickDays(2);

            Assert.AreEqual(82f, state.PlantBiomass, 0.001f);
            Assert.AreEqual("healthy", state.Status);
        }

        [Test]
        public void SaveRoundTripPreservesBiomeEcosystemValues()
        {
            BiomeEcosystemState state = BiomeEcosystemState.CreateDefault("redfang_wilds", "Redfang Wilds");
            state.ApplyPlantConsumption(50f);

            BiomeEcosystemState restored = BiomeEcosystemState.FromSaveData(state.ToSaveData());

            Assert.AreEqual(state.BiomeId, restored.BiomeId);
            Assert.AreEqual(state.PlantBiomass, restored.PlantBiomass, 0.001f);
            Assert.AreEqual(state.Status, restored.Status);
            Assert.AreEqual(state.FoodStress, restored.FoodStress, 0.001f);
        }
    }
}
