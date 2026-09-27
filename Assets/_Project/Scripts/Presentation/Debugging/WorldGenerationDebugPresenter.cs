using System.Collections.Generic;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Vegetation;
using UnityEngine;

namespace ApexShift.Presentation.Debugging
{
    public enum WorldDebugViewMode { None, Elevation, Slope, Moisture, Biome, VegetationDensity, HarvestableTrees, VegetationChunks }

    /// <summary>Development-only, cached world map and generation report presentation.</summary>
    public sealed class WorldGenerationDebugPresenter : MonoBehaviour
    {
        private const int TextureResolution = 96;
        private static readonly Color WaterColor = new Color(0.08f, 0.28f, 0.42f);
        private static readonly Color[] BiomeColors = { new Color(.74f,.68f,.38f), new Color(.16f,.38f,.18f), new Color(.20f,.54f,.25f), new Color(.45f,.42f,.40f), new Color(.52f,.22f,.19f) };
        private static readonly string[] BiomeIds = { "hearth_meadow", "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds" };
        private readonly List<VegetationDebugPoint> vegetationPoints = new List<VegetationDebugPoint>(2048);
        private readonly List<VegetationChunkRuntime> chunks = new List<VegetationChunkRuntime>(128);
        private WorldGenerationResult result;
        private IslandTopographyRuntime topography;
        private VegetationRuntimeController vegetation;
        private Texture2D overlay;
        private Color[] pixels;
        private WorldDebugViewMode mode;
        private string cachedSummary = string.Empty;
        private float refreshTimer;

        public WorldDebugViewMode CurrentMode => mode;
        public Texture2D DebugTexture => overlay;

        public void Configure(WorldGenerationResult generationResult, IslandTopographyRuntime generationTopography,
            VegetationRuntimeController vegetationController = null)
        {
            result = generationResult;
            topography = generationTopography;
            vegetation = vegetationController;
            mode = WorldDebugViewMode.None;
            cachedSummary = BuildSummary();
            if (overlay == null)
            {
                overlay = new Texture2D(TextureResolution, TextureResolution, TextureFormat.RGBA32, false) { name = "WorldGenerationDebugOverlay", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                pixels = new Color[TextureResolution * TextureResolution];
            }
            RenderOverlay();
        }

        private void Update()
        {
            if (!RuntimeDebugSettings.DeveloperDiagnosticsEnabled) return;
            if (Keyboard.current != null && Keyboard.current.f6Key.wasPressedThisFrame)
            {
                mode = (WorldDebugViewMode)(((int)mode + 1) % System.Enum.GetValues(typeof(WorldDebugViewMode)).Length);
                cachedSummary = BuildSummary();
                RenderOverlay();
            }
            if (mode == WorldDebugViewMode.HarvestableTrees || mode == WorldDebugViewMode.VegetationChunks)
            {
                refreshTimer -= Time.unscaledDeltaTime;
                if (refreshTimer <= 0f) { refreshTimer = 1f; cachedSummary = BuildSummary(); RenderOverlay(); }
            }
        }

        private void OnGUI()
        {
            if (!RuntimeDebugSettings.DeveloperDiagnosticsEnabled || result == null) return;
            GUILayout.BeginArea(new Rect(Screen.width - 330f, 32f, 320f, 455f), GUI.skin.box);
            GUILayout.Label("WORLD QA  |  F6: " + mode);
            GUILayout.Label(cachedSummary);
            if (mode != WorldDebugViewMode.None && overlay != null) GUILayout.Label(overlay, GUILayout.Width(288f), GUILayout.Height(288f));
            GUILayout.EndArea();
        }

        private string BuildSummary()
        {
            WorldGenerationReport report = result != null ? result.Report : null;
            if (report == null) return result != null ? $"Seed: {result.Seed}\nBiome regions: {result.BiomeCount}\nVegetation: {result.VegetationInstanceCount}\nReport pending" : string.Empty;
            VegetationRuntimeStats stats = vegetation != null ? vegetation.Stats : default;
            return $"Seed: {report.Seed}; regions/resources/spawn attempts: {result.BiomeCount}/{result.ResourceCount}/{result.SpawnAttempts}\nElevation min/max/avg: {report.MinElevation:0.00}/{report.MaxElevation:0.00}/{report.AverageElevation:0.00}\n" +
                $"Land/water/shore/ridge: {report.LandCells}/{report.WaterCells}/{report.ShoreCells}/{report.RidgeCells}\n" +
                $"Vegetation: {report.VegetationPlacements} (harvestable {report.Harvestable}, decorative {report.Decorative})\n" +
                $"Trees/shrubs/ground: {report.Trees}/{report.Shrubs}/{report.GroundCover}\n" +
                $"Biome cells: {FormatBiomeCounts(report)}\nRejected water/slope/biome/elev/moist/clear/spacing: " +
                $"{report.Rejections.Water}/{report.Rejections.ExcessiveSlope}/{report.Rejections.BiomeMismatch}/{report.Rejections.Elevation}/{report.Rejections.Moisture}/{report.Rejections.ShorelineOrClearing}/{report.Rejections.SpacingOrCollision}\n" +
                $"Chunks active/total: {stats.ActiveTreeChunks + stats.ActiveShrubChunks + stats.ActiveGroundCoverChunks}/{stats.TotalChunks}; gameplay trees: {stats.ActiveHarvestableTrees}";
        }

        private static string FormatBiomeCounts(WorldGenerationReport report)
        {
            var text = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, int> item in report.BiomeCells)
            {
                if (text.Length > 0) text.Append("  ");
                report.BiomeLandPercentages.TryGetValue(item.Key, out float percent);
                text.Append(item.Key).Append(':').Append(item.Value).Append('(').Append(percent.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append("%)");
            }
            return text.ToString();
        }

        private void RenderOverlay()
        {
            if (overlay == null || topography == null || !topography.IsBuilt) return;
            if (mode == WorldDebugViewMode.VegetationDensity || mode == WorldDebugViewMode.HarvestableTrees || mode == WorldDebugViewMode.VegetationChunks)
                vegetation?.CopyDebugPoints(vegetationPoints);
            chunks.Clear();
            if (vegetation != null) chunks.AddRange(vegetation.Chunks);
            Bounds bounds = topography.WorldBounds;
            float minX = bounds.min.x, minZ = bounds.min.z, width = bounds.size.x, depth = bounds.size.z;
            int[] density = mode == WorldDebugViewMode.VegetationDensity ? new int[pixels.Length] : null;
            if (density != null)
                for (int i = 0; i < vegetationPoints.Count; i++)
                {
                    Vector3 p = vegetationPoints[i].Position;
                    int px = Mathf.Clamp((int)((p.x - minX) / width * TextureResolution), 0, TextureResolution - 1);
                    int py = Mathf.Clamp((int)((p.z - minZ) / depth * TextureResolution), 0, TextureResolution - 1);
                    density[py * TextureResolution + px]++;
                }
            for (int y = 0; y < TextureResolution; y++)
            for (int x = 0; x < TextureResolution; x++)
            {
                Vector3 pos = new Vector3(minX + (x + .5f) / TextureResolution * width, 0f, minZ + (y + .5f) / TextureResolution * depth);
                TopographyCell cell = topography.GetCellAt(pos);
                Color color = cell == null || cell.IsWater ? WaterColor : Color.white;
                if (cell != null && cell.IsLand)
                {
                    switch (mode)
                    {
                        case WorldDebugViewMode.Elevation:
                            { float elevation = reportRange(cell.Height); color = Color.Lerp(new Color(.18f,.22f,.55f), new Color(.82f,.9f,.42f), elevation); break; }
                        case WorldDebugViewMode.Slope: color = Color.Lerp(new Color(.12f,.48f,.18f), new Color(.86f,.12f,.08f), Mathf.Clamp01(cell.SlopeDegrees / 45f)); break;
                        case WorldDebugViewMode.Moisture: color = Color.Lerp(new Color(.70f,.43f,.20f), new Color(.05f,.35f,.83f), cell.Moisture01); break;
                        case WorldDebugViewMode.Biome: color = BiomeColor(topography.GetBiomeIdAt(pos)); break;
                        case WorldDebugViewMode.VegetationDensity:
                            int count = density[y * TextureResolution + x]; color = Color.Lerp(new Color(.8f,.8f,.72f), new Color(.02f,.55f,.12f), Mathf.Clamp01(count / 5f)); break;
                        case WorldDebugViewMode.HarvestableTrees: color = Color.white; break;
                        case WorldDebugViewMode.VegetationChunks: color = new Color(.78f,.76f,.65f); break;
                        default: color = new Color(.35f,.55f,.25f); break;
                    }
                }
                pixels[y * TextureResolution + x] = color;
            }
            if (mode == WorldDebugViewMode.HarvestableTrees)
                for (int i = 0; i < vegetationPoints.Count; i++)
                {
                    VegetationDebugPoint point = vegetationPoints[i];
                    if (point.Category != VegetationCategory.Tree && point.Category != VegetationCategory.DeadTree) continue;
                    Color color = !point.Harvestable ? new Color(.3f,.3f,.3f) : point.StreamingGameplayActive ? Color.cyan
                        : point.TreeState == TreeLifecycleState.Falling ? new Color(1f,.45f,.05f)
                : point.TreeState == TreeLifecycleState.Depleted
                    ? Color.Lerp(new Color(.38f,.19f,.08f), new Color(.75f,.72f,.24f), point.RegrowthProgress)
                    : new Color(.08f,.55f,.18f);
                    Stamp(point.Position, color, minX, minZ, width, depth);
                }
            else if (mode == WorldDebugViewMode.VegetationChunks)
                for (int i = 0; i < chunks.Count; i++)
                {
                    VegetationChunkRuntime chunk = chunks[i];
                    Color color = !HasVisibleCategory(chunk) ? new Color(.45f,.45f,.45f)
                        : chunk.HasActiveGameplayTree ? new Color(.95f,.48f,.05f) : new Color(.1f,.65f,.75f);
                    int x0 = Mathf.Clamp(Mathf.RoundToInt((chunk.Bounds.min.x - minX) / width * TextureResolution), 0, TextureResolution - 1);
                    int x1 = Mathf.Clamp(Mathf.RoundToInt((chunk.Bounds.max.x - minX) / width * TextureResolution), 0, TextureResolution - 1);
                    int y0 = Mathf.Clamp(Mathf.RoundToInt((chunk.Bounds.min.z - minZ) / depth * TextureResolution), 0, TextureResolution - 1);
                    int y1 = Mathf.Clamp(Mathf.RoundToInt((chunk.Bounds.max.z - minZ) / depth * TextureResolution), 0, TextureResolution - 1);
                    for (int x = x0; x <= x1; x++) { pixels[y0 * TextureResolution + x] = Color.black; pixels[y1 * TextureResolution + x] = Color.black; }
                    for (int y = y0; y <= y1; y++) { pixels[y * TextureResolution + x0] = Color.black; pixels[y * TextureResolution + x1] = Color.black; }
                    if (x1 - x0 > 2 && y1 - y0 > 2)
                        for (int y = y0 + 1; y < y1; y++) for (int x = x0 + 1; x < x1; x++) pixels[y * TextureResolution + x] = color;
                }
            overlay.SetPixels(pixels);
            overlay.Apply(false, false);
        }

        private float reportRange(float height)
        {
            float min = result?.Report?.MinElevation ?? 0f, max = result?.Report?.MaxElevation ?? 1f;
            return Mathf.InverseLerp(min, max, height);
        }

        private void Stamp(Vector3 position, Color color, float minX, float minZ, float width, float depth)
        {
            int x = Mathf.Clamp((int)((position.x - minX) / width * TextureResolution), 0, TextureResolution - 1);
            int y = Mathf.Clamp((int)((position.z - minZ) / depth * TextureResolution), 0, TextureResolution - 1);
            pixels[y * TextureResolution + x] = color;
        }

        private static bool HasVisibleCategory(VegetationChunkRuntime chunk)
        {
            return chunk != null && chunk.transform.childCount >= 3
                && (chunk.transform.GetChild(0).gameObject.activeSelf || chunk.transform.GetChild(1).gameObject.activeSelf || chunk.transform.GetChild(2).gameObject.activeSelf);
        }

        private static Color BiomeColor(string id)
        {
            for (int i = 0; i < BiomeIds.Length; i++) if (BiomeIds[i] == id) return BiomeColors[i];
            return Color.magenta;
        }

        private void OnDestroy()
        {
            if (overlay == null) return;
            if (Application.isPlaying) Destroy(overlay); else DestroyImmediate(overlay);
            overlay = null;
        }
    }
}
