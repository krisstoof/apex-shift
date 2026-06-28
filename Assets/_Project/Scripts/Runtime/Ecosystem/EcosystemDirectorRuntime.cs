using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Ecosystem;
using ApexShift.Core.Save;
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

        private static EcosystemDirectorRuntime _instance;
        private readonly Dictionary<string, BiomeEcosystemState> biomeStates = new Dictionary<string, BiomeEcosystemState>(System.StringComparer.Ordinal);
        private readonly List<GeneratedBiomeRegion> regions = new List<GeneratedBiomeRegion>();
        private readonly List<string> pendingTickBiomeIds = new List<string>();
        private float tickTimer;
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
            }
        }

        public void TickDay(int days = 1)
        {
            int safeDays = Mathf.Max(1, days);
            foreach (BiomeEcosystemState state in biomeStates.Values)
            {
                state.TickDays(safeDays);
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
            if (!showDebugOverlay || !initialized)
            {
                return;
            }

            GUI.Box(
                new Rect(12f, Screen.height * 0.55f + 112f, 310f, 92f),
                $"Ecosystem Director\n" +
                $"source: {ecosystemStateSource} biomes:{biomeStates.Count}\n" +
                $"tick: {tickTimer:0.0}/{simulationTickSeconds:0.0} pending:{pendingTickBiomeIds.Count}\n" +
                $"avg plant: {GetAveragePlantBiomassPercent():0.0}%");
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
