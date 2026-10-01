using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Vegetation;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>Stable, frame-independent summary of one completed world generation.</summary>
    public sealed class WorldGenerationReport
    {
        public int Seed { get; private set; }
        public int LandCells { get; private set; }
        public int WaterCells { get; private set; }
        public int ShoreCells { get; private set; }
        public int RidgeCells { get; private set; }
        public int VegetationPlacements { get; private set; }
        public float MinElevation { get; private set; }
        public float MaxElevation { get; private set; }
        public float AverageElevation { get; private set; }
        public IReadOnlyList<int> SlopeBuckets => slopeBuckets;
        public IReadOnlyDictionary<string, int> HabitatCells => habitatCells;
        public IReadOnlyDictionary<string, float> HabitatLandPercentages => habitatLandPercentages;
        public IReadOnlyDictionary<string, int> VegetationByBiomeSpecies => vegetationByBiomeSpecies;
        public int Harvestable { get; private set; }
        public int Decorative { get; private set; }
        public int Trees { get; private set; }
        public int Shrubs { get; private set; }
        public int GroundCover { get; private set; }
        public VegetationRejectionCounts Rejections { get; private set; }

        private readonly int[] slopeBuckets = new int[5];
        private readonly SortedDictionary<string, int> habitatCells = new SortedDictionary<string, int>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, float> habitatLandPercentages = new SortedDictionary<string, float>(StringComparer.Ordinal);
        private readonly SortedDictionary<string, int> vegetationByBiomeSpecies = new SortedDictionary<string, int>(StringComparer.Ordinal);

        public static WorldGenerationReport Create(WorldGenerationResult result, IslandTopographyRuntime topography,
            VegetationRejectionCounts rejections)
        {
            var report = new WorldGenerationReport { Seed = result != null ? result.Seed : 0, Rejections = Copy(rejections) };
            if (result != null)
            {
                report.VegetationPlacements = result.VegetationInstanceCount;
                foreach (KeyValuePair<string, int> item in result.VegetationCounts)
                    report.vegetationByBiomeSpecies.Add(item.Key, item.Value);
                foreach (KeyValuePair<string, int> item in result.VegetationHarvestableCounts) report.Harvestable += item.Value;
                foreach (KeyValuePair<string, int> item in result.VegetationDecorativeCounts) report.Decorative += item.Value;
                foreach (KeyValuePair<string, int> item in result.VegetationCategoryCounts)
                {
                    string category = item.Key.Substring(item.Key.LastIndexOf('/') + 1);
                    switch (category)
                    {
                        case nameof(VegetationCategory.Tree):
                        case nameof(VegetationCategory.DeadTree): report.Trees += item.Value; break;
                        case nameof(VegetationCategory.Shrub): report.Shrubs += item.Value; break;
                        case nameof(VegetationCategory.GroundCover): report.GroundCover += item.Value; break;
                    }
                }
            }

            if (topography == null || !topography.IsBuilt) return report;
            report.LandCells = topography.LandCellCount;
            report.WaterCells = topography.WaterCellCount;
            report.ShoreCells = topography.ShoreCellCount;
            report.RidgeCells = topography.RidgeCellCount;
            float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity, sum = 0f;
            int count = 0;
            TopographyCell[,] grid = topography.GetGridReadOnly();
            for (int z = 0; z < grid.GetLength(1); z++)
            for (int x = 0; x < grid.GetLength(0); x++)
            {
                TopographyCell cell = grid[x, z];
                if (cell == null) continue;
                if (cell.IsLand)
                {
                    minimum = Math.Min(minimum, cell.Height); maximum = Math.Max(maximum, cell.Height); sum += cell.Height; count++;
                    int bucket = cell.SlopeDegrees < 10f ? 0 : cell.SlopeDegrees < 20f ? 1 : cell.SlopeDegrees < 30f ? 2 : cell.SlopeDegrees < 40f ? 3 : 4;
                    report.slopeBuckets[bucket]++;
                    report.habitatCells.TryGetValue(cell.HabitatId ?? string.Empty, out int countForHabitat);
                    report.habitatCells[cell.HabitatId ?? string.Empty] = countForHabitat + 1;
                }
            }
            report.MinElevation = count == 0 ? 0f : minimum;
            report.MaxElevation = count == 0 ? 0f : maximum;
            report.AverageElevation = count == 0 ? 0f : sum / count;
            foreach (KeyValuePair<string, int> habitat in report.habitatCells)
                report.habitatLandPercentages.Add(habitat.Key, report.LandCells == 0 ? 0f : (float)habitat.Value / report.LandCells * 100f);
            return report;
        }

        public string ToDeterministicString()
        {
            var text = new StringBuilder(512);
            text.Append("seed=").Append(I(Seed))
                .Append(";cells=").Append(I(LandCells)).Append(',').Append(I(WaterCells)).Append(',').Append(I(ShoreCells)).Append(',').Append(I(RidgeCells))
                .Append(";elevation=").Append(F(MinElevation)).Append(',').Append(F(MaxElevation)).Append(',').Append(F(AverageElevation))
                .Append(";slope=");
            for (int i = 0; i < slopeBuckets.Length; i++) { if (i > 0) text.Append(','); text.Append(slopeBuckets[i].ToString(CultureInfo.InvariantCulture)); }
            text
                .Append(";vegetation=").Append(I(VegetationPlacements)).Append(',').Append(I(Harvestable)).Append(',').Append(I(Decorative)).Append(',').Append(I(Trees)).Append(',').Append(I(Shrubs)).Append(',').Append(I(GroundCover));
            AppendMap(text, ";habitats=", habitatCells);
            text.Append(";habitatPercent=");
            bool firstPercent = true;
            foreach (KeyValuePair<string, float> item in habitatLandPercentages)
            { if (!firstPercent) text.Append(','); firstPercent = false; text.Append(item.Key).Append(':').Append(F(item.Value)); }
            AppendMap(text, ";species=", vegetationByBiomeSpecies);
            text.Append(";rejected=").Append(I(Rejections.Water)).Append(',').Append(I(Rejections.ExcessiveSlope)).Append(',')
                .Append(I(Rejections.BiomeMismatch)).Append(',').Append(I(Rejections.Elevation)).Append(',').Append(I(Rejections.Moisture)).Append(',')
                .Append(I(Rejections.ShorelineOrClearing)).Append(',').Append(I(Rejections.SpacingOrCollision));
            return text.ToString();
        }

        private static VegetationRejectionCounts Copy(VegetationRejectionCounts source)
        {
            return new VegetationRejectionCounts
            {
                Water = source?.Water ?? 0, ExcessiveSlope = source?.ExcessiveSlope ?? 0,
                BiomeMismatch = source?.BiomeMismatch ?? 0, Elevation = source?.Elevation ?? 0,
                Moisture = source?.Moisture ?? 0, ShorelineOrClearing = source?.ShorelineOrClearing ?? 0,
                SpacingOrCollision = source?.SpacingOrCollision ?? 0
            };
        }

        private static void AppendMap(StringBuilder text, string prefix, SortedDictionary<string, int> map)
        {
            text.Append(prefix);
            bool first = true;
            foreach (KeyValuePair<string, int> item in map)
            {
                if (!first) text.Append(',');
                first = false;
                text.Append(item.Key).Append(':').Append(I(item.Value));
            }
        }

        private static string F(float value) => value.ToString("R", CultureInfo.InvariantCulture);
        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
