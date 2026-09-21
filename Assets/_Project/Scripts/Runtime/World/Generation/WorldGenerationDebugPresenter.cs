using UnityEngine;
using ApexShift.Runtime.World.Topography;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>Presentation-only generation diagnostics; generation code never draws UI.</summary>
    public sealed class WorldGenerationDebugPresenter : MonoBehaviour
    {
        private WorldGenerationResult result;
        private IslandTopographyRuntime topography;

        public void Configure(WorldGenerationResult generationResult, IslandTopographyRuntime generationTopography)
        {
            result = generationResult;
            topography = generationTopography;
        }

        private void OnGUI()
        {
            if (result == null) return;
            GUI.color = Color.black;
            GUILayout.BeginArea(new Rect(Screen.width - 310, 450, 300, 260));
            GUILayout.Label($"Seed: {result.Seed}");
            GUILayout.Label($"Biomes: {result.BiomeCount}");
            GUILayout.Label($"Resources: {result.ResourceCount}");
            GUILayout.Label($"Spawn Attempts: {result.SpawnAttempts}");
            if (topography != null && topography.IsBuilt)
            {
                GUILayout.Space(4);
                GUILayout.Label("-- Topography --");
                GUILayout.Label($"Land: {topography.LandCellCount}  Water: {topography.WaterCellCount}");
                GUILayout.Label($"Shore: {topography.ShoreCellCount}  Ridge: {topography.RidgeCellCount}");
                GUILayout.Label($"Safe Player: {topography.SafePlayerCells}");
                GUILayout.Label($"Safe Creature: {topography.SafeCreatureCells}");
            }
            GUILayout.EndArea();
        }
    }
}
