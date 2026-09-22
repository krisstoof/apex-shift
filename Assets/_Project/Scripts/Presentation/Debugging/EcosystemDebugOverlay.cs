using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Ecosystem;
using UnityEngine;

namespace ApexShift.Presentation.Debugging
{
    /// <summary>Optional presentation for the active ecosystem registry.</summary>
    public sealed class EcosystemDebugOverlay : MonoBehaviour
    {
        private EcosystemRuntime ecosystem;

        public void Configure(EcosystemRuntime runtime)
        {
            ecosystem = runtime;
        }

        private void OnGUI()
        {
            if (ecosystem == null) return;

            GUI.Box(
                new Rect(12f, Screen.height * 0.55f, 270f, 104f),
                $"Ecosystem Debug\n" +
                $"food sources: {ecosystem.FoodSourceCount}\n" +
                $"creatures: {ecosystem.CreatureCount}\n" +
                $"plants: {ecosystem.PlantFoodSourceCount} avg:{ecosystem.GetAverageBiomassRatio(FoodKind.Plants):0.00}\n" +
                $"meat: {ecosystem.MeatFoodSourceCount} avg:{ecosystem.GetAverageBiomassRatio(FoodKind.Meat):0.00}");
        }
    }
}
