using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApexShift.Core.Ecosystem;
using ApexShift.Core.Save;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Generation;
using UnityEngine;

namespace ApexShift.Runtime.Ecosystem
{
    [DisallowMultipleComponent]
    public sealed class EcosystemDirectorRuntime : MonoBehaviour
    {
        [SerializeField] private float simulationTickSeconds = 12f;
        [SerializeField] private bool processOneBiomePerFrame = true;
        [SerializeField] private bool showDebugOverlay = true;
        [SerializeField] private float debugRefreshIntervalSeconds = 0.50f;

        private static EcosystemDirectorRuntime _instance;
        private readonly Dictionary<string, BiomeEcosystemState> biomeStates = new Dictionary<string, BiomeEcosystemState>(System.StringComparer.Ordinal);
        private readonly List<GeneratedBiomeRegion> regions = new List<GeneratedBiomeRegion>();
        private readonly List<string> pendingTickBiomeIds = new List<string>();
        private float tickTimer;
        private float debugTextRefreshTimer;
        private string cachedDebugText = string.Empty;
        private float cachedDebugHeight = 92f;
        private bool initialized;
        private string ecosystemStateSource = "generated";

        public static EcosystemDirectorRuntime Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Object.FindAnyObjectByType<EcosystemDirectorRuntime>();
                }

                return _instance;
            }
        }

        public static EcosystemDirectorRuntime Active => Instance;
        public IReadOnlyDictionary<string, BiomeEcosystemState> BiomeStates => biomeStates;
        public string EcosystemStateSource => ecosystemStateSource;
        public bool Initialized => initialized;
        public float TickTimer => tickTimer;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }

            if (pendingTickBiomeIds.Count > 0)
            {
                ProcessNextRuntimeBiome();
                return;
            }

            tickTimer += Time.deltaTime;
            if (tickTimer < simulationTickSeconds)
            {
                return;
            }

            tickTimer = 0f;
            BeginRuntimeEcosystemTick();
            if (!processOneBiomePerFrame)
            {
                while (pendingTickBiomeIds.Count > 0)
                {
                    ProcessNextRuntimeBiome();
                }
            }
        }

        public void InitializeFromRegions(IEnumerable<GeneratedBiomeRegion> generatedRegions)
        {
            biomeStates.Clear();
            regions.Clear();
            pendingTickBiomeIds.Clear();
            tickTimer = 0f;
            ecosystemStateSource = "generated";

            if (generatedRegions != null)
            {
                foreach (GeneratedBiomeRegion region in generatedRegions)
                {
                    if (region?.Biome == null || string.IsNullOrWhiteSpace(region.Biome.BiomeId))
                    {
                        continue;
                    }

                    regions.Add(region);
                    string biomeId = NormalizeBiomeId(region.Biome.BiomeId);
                    if (biomeId == "water")
                    {
                        continue;
                    }

                    if (!biomeStates.ContainsKey(biomeId))
                    {
                        biomeStates[biomeId] = BiomeEcosystemState.CreateDefault(biomeId, region.Biome.DisplayName);
                    }
                }
            }

            if (biomeStates.Count == 0)
            {
                biomeStates["default"] = BiomeEcosystemState.CreateDefault("default", "Default");
            }

            initialized = true;
        }

        public string GetBiomeIdForPosition(Vector3 position)
        {
            for (int i = 0; i < regions.Count; i++)
            {
                GeneratedBiomeRegion region = regions[i];
                if (region?.Biome == null)
                {
                    continue;
                }

                if (ContainsHorizontal(region.Bounds, position))
                {
                    return NormalizeBiomeId(region.Biome.BiomeId);
                }
            }

            return biomeStates.Count > 0 ? biomeStates.Keys.OrderBy(id => id).First() : "default";
        }

        public BiomeEcosystemState GetBiomeState(string biomeId)
        {
            string normalized = NormalizeBiomeId(biomeId);
            return biomeStates.TryGetValue(normalized, out BiomeEcosystemState state) ? state : null;
        }

        public void DebugReducePlantBiomass(Vector3 position, float amount)
        {
            string biomeId = GetBiomeIdForPosition(position);
            if (biomeStates.TryGetValue(biomeId, out BiomeEcosystemState state))
            {
                state.ApplyPlantConsumption(amount);
                GameEventBus.PublishBiomassChanged(position, biomeId, Mathf.Max(0f, amount), state.PlantBiomass);
            }
        }

        public void TickDay(int days = 1)
        {
            int safeDays = Mathf.Max(1, days);
            foreach (BiomeEcosystemState state in biomeStates.Values)
            {
                state.TickDays(safeDays);
                GameEventBus.PublishEcosystemTickAdvanced(state.BiomeId, safeDays, "day_tick_advanced");
                GameEventBus.PublishPopulationChanged(Vector3.zero, state.BiomeId, state.PopulationCount);
            }

            AdvanceResourceRegrowth(safeDays);
        }

        public List<BiomeEcosystemSaveData> CaptureSaveData()
        {
            return biomeStates.Values
                .OrderBy(state => state.BiomeId)
                .Select(state => state.ToSaveData())
                .ToList();
        }

        public void LoadSaveData(IReadOnlyList<BiomeEcosystemSaveData> savedStates)
        {
            if (savedStates == null || savedStates.Count == 0)
            {
                return;
            }

            foreach (BiomeEcosystemSaveData savedState in savedStates)
            {
                if (savedState == null)
                {
                    continue;
                }

                BiomeEcosystemState restored = BiomeEcosystemState.FromSaveData(savedState);
                biomeStates[restored.BiomeId] = restored;
            }

            ecosystemStateSource = "save";
            initialized = true;
        }

        public void RestoreRuntimeMetadata(float savedTickTimer, string savedSource)
        {
            tickTimer = Mathf.Max(0f, savedTickTimer);
            ecosystemStateSource = string.IsNullOrWhiteSpace(savedSource) ? "save" : savedSource.Trim();
            initialized = true;
        }

        private void BeginRuntimeEcosystemTick()
        {
            pendingTickBiomeIds.Clear();
            pendingTickBiomeIds.AddRange(biomeStates.Keys.OrderBy(id => id));
        }

        private void ProcessNextRuntimeBiome()
        {
            if (pendingTickBiomeIds.Count == 0)
            {
                return;
            }

            string biomeId = pendingTickBiomeIds[0];
            pendingTickBiomeIds.RemoveAt(0);
            if (biomeStates.TryGetValue(biomeId, out BiomeEcosystemState state))
            {
                state.TickDays(1);
                GameEventBus.PublishEcosystemTickAdvanced(biomeId, 1f, "runtime_biome_tick_advanced");
                GameEventBus.PublishPopulationChanged(Vector3.zero, biomeId, state.PopulationCount);
            }
        }

        private void AdvanceResourceRegrowth(int days)
        {
            foreach (ResourceNodeView resourceNode in Object.FindObjectsByType<ResourceNodeView>())
            {
                if (resourceNode != null)
                {
                    resourceNode.AdvanceGrowthDays(days);
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugOverlay || !initialized || !RuntimeDebugSettings.DebugEnabled || !RuntimeDebugSettings.EcosystemOverlayEnabled)
            {
                return;
            }

            string text = GetCachedDebugText();

            GUI.Box(
                new Rect(12f, Screen.height * 0.55f + 112f, 420f, cachedDebugHeight),
                text);
        }

        private string GetCachedDebugText()
        {
            debugTextRefreshTimer -= Time.unscaledDeltaTime;
            if (!string.IsNullOrEmpty(cachedDebugText) && debugTextRefreshTimer > 0f)
            {
                return cachedDebugText;
            }

            debugTextRefreshTimer = Mathf.Max(0.05f, debugRefreshIntervalSeconds > 0f ? debugRefreshIntervalSeconds : RuntimeDebugSettings.RefreshIntervalSeconds);
            cachedDebugText = BuildEcosystemDebugText();
            int lines = Mathf.Max(1, cachedDebugText.Split('\n').Length);
            cachedDebugHeight = Mathf.Max(92f, 18f + lines * 16f);
            return cachedDebugText;
        }

        private string BuildEcosystemDebugText()
        {
            StringBuilder builder = new StringBuilder(512);
            builder.AppendLine("Ecosystem Director");
            builder.AppendLine($"source:{ecosystemStateSource} biomes:{biomeStates.Count} tick:{tickTimer:0.0}/{simulationTickSeconds:0.0} pending:{pendingTickBiomeIds.Count}");
            builder.AppendLine($"avg plant:{GetAveragePlantBiomassPercent():0.0}%");

            foreach (BiomeEcosystemState state in biomeStates.Values.OrderBy(state => state.BiomeId))
            {
                builder.AppendLine(FormatBiomeDebugLine(state));
            }

            return builder.ToString();
        }

        private static string FormatBiomeDebugLine(BiomeEcosystemState state)
        {
            if (state == null)
            {
                return "biome: missing";
            }

            return $"{Shorten(state.BiomeId, 10)} bio:{state.PlantBiomass:0}/{state.MaxPlantBiomass:0}({state.PlantBiomassPercent:0}%) pop s:{state.SmallPreyPopulation:0.0} g:{state.GrazerPopulation:0.0} v:{state.VarnakPopulation:0.0} stress:{state.FoodStress:0} gen {state.SmallPreyGeneration}/{state.GrazerGeneration}/{state.VarnakGeneration} niche:{Shorten(state.CurrentNiche, 10)} {state.Status}";
        }

        private static string Shorten(string value, int maxLength)
        {
            string safe = string.IsNullOrWhiteSpace(value) ? "none" : value.Trim();
            return safe.Length <= maxLength ? safe : safe.Substring(0, Mathf.Max(1, maxLength));
        }

        private float GetAveragePlantBiomassPercent()
        {
            if (biomeStates.Count == 0)
            {
                return 0f;
            }

            return biomeStates.Values.Average(state => state.PlantBiomassPercent);
        }

        private static bool ContainsHorizontal(Bounds bounds, Vector3 position)
        {
            return position.x >= bounds.min.x && position.x <= bounds.max.x
                   && position.z >= bounds.min.z && position.z <= bounds.max.z;
        }

        private static string NormalizeBiomeId(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "default" : value.Trim().ToLowerInvariant();
        }
    }
}
