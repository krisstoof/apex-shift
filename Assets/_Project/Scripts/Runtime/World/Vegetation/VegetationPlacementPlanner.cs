using System;
using System.Collections.Generic;
using ApexShift.Runtime.World.Biomes;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    public readonly struct VegetationLandmarkClearance
    {
        public readonly Vector3 Position;
        public readonly float Radius;
        public VegetationLandmarkClearance(Vector3 position, float radius) { Position = position; Radius = Mathf.Max(0f, radius); }
    }

    /// <summary>Deterministic, allocation-bounded placement planning; this class never creates GameObjects.</summary>
    public sealed class VegetationPlacementPlanner
    {
        private readonly Dictionary<SpacingCell, List<AcceptedPoint>> spacing = new Dictionary<SpacingCell, List<AcceptedPoint>>();
        private readonly HashSet<string> emittedMissingPrefabWarnings = new HashSet<string>(StringComparer.Ordinal);
        private float spacingCellSize;

        public List<VegetationPlacement> Plan(int seed, BiomeCatalogAsset biomeCatalog,
            VegetationGenerationSettings settings, Bounds worldBounds,
            Func<Vector3, VegetationEnvironmentSample> sampleEnvironment,
            Func<Vector3, float> sampleHeight, Vector3 playerSpawn, float startClearingRadius,
            IReadOnlyList<VegetationLandmarkClearance> landmarks, IReadOnlyList<Vector3> shorelinePoints)
        {
            var placements = new List<VegetationPlacement>();
            if (biomeCatalog == null || settings == null || sampleEnvironment == null || sampleHeight == null) return placements;

            spacing.Clear();
            emittedMissingPrefabWarnings.Clear();
            spacingCellSize = FindMaximumSpacing(biomeCatalog);

            foreach (BiomeDefinitionAsset biome in biomeCatalog.Biomes)
            {
                BiomeVegetationProfileAsset profile = biome != null ? biome.VegetationProfile : null;
                if (profile == null || profile.Species == null || profile.Species.Count == 0) continue;
                float totalWeight = 0f;
                for (int i = 0; i < profile.Species.Count; i++)
                    if (profile.Species[i] != null) totalWeight += Mathf.Max(0f, profile.Species[i].Weight);
                if (totalWeight <= 0f) continue;

                for (int i = 0; i < profile.Species.Count; i++)
                {
                    BiomeVegetationSpeciesEntry entry = profile.Species[i];
                    VegetationSpeciesAsset speciesAsset = entry != null ? entry.Species : null;
                    if (speciesAsset == null || entry.Weight <= 0f || entry.DensityMultiplier <= 0f) continue;
                    string biomeId = biome.BiomeId;
                    if (!speciesAsset.AllowsBiome(biomeId)) continue;
                    if (speciesAsset.VisualPrefab == null)
                    {
                        if (emittedMissingPrefabWarnings.Add(speciesAsset.SpeciesId))
                            Debug.LogWarning($"Vegetation configuration error: species '{speciesAsset.SpeciesId}' has no VisualPrefab; it will be skipped.");
                        continue;
                    }

                    float density = settings.GetBaseDensity(speciesAsset.Category)
                        * profile.OverallDensity
                        * entry.DensityMultiplier
                        * (entry.Weight / totalWeight);
                    if (!IsFinite(density) || density <= 0f) continue;
                    PlanSpecies(seed, biomeId, speciesAsset, density, settings, worldBounds,
                        sampleEnvironment, sampleHeight, playerSpawn, startClearingRadius,
                        landmarks, shorelinePoints, placements);
                }
            }
            return placements;
        }

        private void PlanSpecies(int seed, string biomeId, VegetationSpeciesAsset species,
            float density, VegetationGenerationSettings settings, Bounds worldBounds,
            Func<Vector3, VegetationEnvironmentSample> sampleEnvironment, Func<Vector3, float> sampleHeight,
            Vector3 playerSpawn, float startClearingRadius,
            IReadOnlyList<VegetationLandmarkClearance> landmarks, IReadOnlyList<Vector3> shorelinePoints,
            List<VegetationPlacement> output)
        {
            float cellSize = Mathf.Sqrt(1f / density);
            if (!IsFinite(cellSize) || cellSize <= 0f) return;
            int minX = Mathf.FloorToInt(worldBounds.min.x / cellSize);
            int maxX = Mathf.CeilToInt(worldBounds.max.x / cellSize);
            int minZ = Mathf.FloorToInt(worldBounds.min.z / cellSize);
            int maxZ = Mathf.CeilToInt(worldBounds.max.z / cellSize);
            float jitter = settings.JitterFraction * 0.5f;
            float coastClearance = settings.GetCoastalClearance(species.Category);
            int spacingGroup = GetSpacingGroup(species.Category);

            for (int gridZ = minZ; gridZ < maxZ; gridZ++)
            for (int gridX = minX; gridX < maxX; gridX++)
            {
                float baseX = (gridX + 0.5f) * cellSize;
                float baseZ = (gridZ + 0.5f) * cellSize;
                var basePosition = new Vector3(baseX, 0f, baseZ);
                VegetationEnvironmentSample baseEnvironment = sampleEnvironment(basePosition);
                if (!baseEnvironment.IsLand || baseEnvironment.IsWater
                    || !string.Equals(baseEnvironment.BiomeId, biomeId, StringComparison.Ordinal)) continue;

                int baseChunkX = Mathf.FloorToInt(baseX / settings.ChunkSize);
                int baseChunkZ = Mathf.FloorToInt(baseZ / settings.ChunkSize);
                var random = new LocalDeterministicRandom(StableHash(seed, baseChunkX, baseChunkZ, biomeId, species.SpeciesId, gridX, gridZ));
                float x = baseX + (random.Next01() - 0.5f) * cellSize * settings.JitterFraction;
                float z = baseZ + (random.Next01() - 0.5f) * cellSize * settings.JitterFraction;
                var candidate = new Vector3(x, 0f, z);
                VegetationEnvironmentSample environment = sampleEnvironment(candidate);
                if (!IsEnvironmentValid(species, biomeId, environment)) continue;
                if (environment.IsShoreline) continue;
                if (!CanPlaceInStartClearing(species.Category, settings, candidate, playerSpawn, startClearingRadius)) continue;
                if (IsNearLandmark(candidate, landmarks)) continue;
                if (IsNearShoreline(candidate, shorelinePoints, coastClearance)) continue;

                float height = sampleHeight(candidate);
                candidate.y = height;
                if (ViolatesSpacing(candidate, species.MinimumSpacing, spacingGroup)) continue;

                float yaw = species.RandomYaw ? random.Next01() * 360f : 0f;
                float scale = Mathf.Lerp(species.MinScale, species.MaxScale, random.Next01());
                int chunkX = Mathf.FloorToInt(candidate.x / settings.ChunkSize);
                int chunkZ = Mathf.FloorToInt(candidate.z / settings.ChunkSize);
                string instanceId = BuildInstanceId(seed, biomeId, species.SpeciesId, candidate);
                var placement = new VegetationPlacement(instanceId, biomeId, species, candidate, yaw, scale, chunkX, chunkZ);
                AddSpacingPoint(candidate, species.MinimumSpacing, spacingGroup);
                output.Add(placement);
            }
        }

        private static bool IsEnvironmentValid(VegetationSpeciesAsset species, string expectedBiome, VegetationEnvironmentSample environment)
        {
            return environment.IsLand && !environment.IsWater && !environment.IsShoreline
                && string.Equals(environment.BiomeId, expectedBiome, StringComparison.Ordinal)
                && species.AllowsBiome(environment.BiomeId)
                && environment.NormalizedElevation >= species.MinElevation01
                && environment.NormalizedElevation <= species.MaxElevation01
                && environment.SlopeDegrees >= species.MinSlopeDegrees
                && environment.SlopeDegrees <= species.MaxSlopeDegrees
                && environment.Moisture01 >= species.MinMoisture01
                && environment.Moisture01 <= species.MaxMoisture01;
        }

        private static bool CanPlaceInStartClearing(VegetationCategory category, VegetationGenerationSettings settings,
            Vector3 candidate, Vector3 playerSpawn, float radius)
        {
            if (category == VegetationCategory.GroundCover)
                return settings.AllowGroundCoverInStartClearing || HorizontalSqrDistance(candidate, playerSpawn) >= radius * radius;
            if (category != VegetationCategory.Tree && category != VegetationCategory.DeadTree && category != VegetationCategory.Shrub) return true;
            return HorizontalSqrDistance(candidate, playerSpawn) >= radius * radius;
        }

        private static bool IsNearLandmark(Vector3 candidate, IReadOnlyList<VegetationLandmarkClearance> landmarks)
        {
            if (landmarks == null) return false;
            for (int i = 0; i < landmarks.Count; i++)
            {
                VegetationLandmarkClearance landmark = landmarks[i];
                if (HorizontalSqrDistance(candidate, landmark.Position) < landmark.Radius * landmark.Radius) return true;
            }
            return false;
        }

        private static bool IsNearShoreline(Vector3 candidate, IReadOnlyList<Vector3> shorelinePoints, float clearance)
        {
            if (shorelinePoints == null || clearance <= 0f) return false;
            float sqr = clearance * clearance;
            for (int i = 0; i < shorelinePoints.Count; i++)
                if (HorizontalSqrDistance(candidate, shorelinePoints[i]) < sqr) return true;
            return false;
        }

        private bool ViolatesSpacing(Vector3 candidate, float spacingValue, int group)
        {
            float required = Mathf.Max(0f, spacingValue);
            if (required <= 0f) return false;
            int cellX = Mathf.FloorToInt(candidate.x / spacingCellSize);
            int cellZ = Mathf.FloorToInt(candidate.z / spacingCellSize);
            float requiredSqr = required * required;
            for (int dz = -1; dz <= 1; dz++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (!spacing.TryGetValue(new SpacingCell(cellX + dx, cellZ + dz, group), out List<AcceptedPoint> points)) continue;
                for (int i = 0; i < points.Count; i++)
                {
                    float pairRequired = Mathf.Max(required, points[i].MinimumSpacing);
                    if (HorizontalSqrDistance(candidate, points[i].Position) < pairRequired * pairRequired) return true;
                }
            }
            return false;
        }

        private void AddSpacingPoint(Vector3 position, float minimumSpacing, int group)
        {
            int x = Mathf.FloorToInt(position.x / spacingCellSize);
            int z = Mathf.FloorToInt(position.z / spacingCellSize);
            var key = new SpacingCell(x, z, group);
            if (!spacing.TryGetValue(key, out List<AcceptedPoint> points))
                spacing.Add(key, points = new List<AcceptedPoint>());
            points.Add(new AcceptedPoint(position, Mathf.Max(0f, minimumSpacing)));
        }

        private static float FindMaximumSpacing(BiomeCatalogAsset catalog)
        {
            float max = 0.1f;
            foreach (BiomeDefinitionAsset biome in catalog.Biomes)
            {
                IReadOnlyList<BiomeVegetationSpeciesEntry> entries = biome != null ? biome.VegetationProfile?.Species : null;
                if (entries == null) continue;
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i]?.Species != null) max = Mathf.Max(max, entries[i].Species.MinimumSpacing);
            }
            return max;
        }

        private static int GetSpacingGroup(VegetationCategory category)
        {
            switch (category)
            {
                case VegetationCategory.Tree:
                case VegetationCategory.DeadTree: return 0;
                case VegetationCategory.Shrub: return 1;
                default: return 2;
            }
        }

        private static float HorizontalSqrDistance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return x * x + z * z;
        }

        private static string BuildInstanceId(int seed, string biomeId, string speciesId, Vector3 position)
        {
            ulong hash = FnvOffset;
            AddInt(ref hash, seed);
            AddString(ref hash, biomeId);
            AddString(ref hash, speciesId);
            AddInt(ref hash, Mathf.RoundToInt(position.x * 1000f));
            AddInt(ref hash, Mathf.RoundToInt(position.z * 1000f));
            return "veg_" + hash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static ulong StableHash(int seed, int chunkX, int chunkZ, string biomeId, string speciesId, int gridX, int gridZ)
        {
            ulong hash = FnvOffset;
            AddInt(ref hash, seed);
            AddInt(ref hash, chunkX);
            AddInt(ref hash, chunkZ);
            AddString(ref hash, biomeId);
            AddString(ref hash, speciesId);
            AddInt(ref hash, gridX);
            AddInt(ref hash, gridZ);
            return hash;
        }

        private const ulong FnvOffset = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        private static void AddString(ref ulong hash, string value)
        {
            if (value == null) { AddByte(ref hash, 0xff); return; }
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                AddByte(ref hash, (byte)c);
                AddByte(ref hash, (byte)(c >> 8));
            }
            AddByte(ref hash, 0);
        }

        private static void AddInt(ref ulong hash, int value)
        {
            unchecked
            {
                AddByte(ref hash, (byte)value);
                AddByte(ref hash, (byte)(value >> 8));
                AddByte(ref hash, (byte)(value >> 16));
                AddByte(ref hash, (byte)(value >> 24));
            }
        }

        private static void AddByte(ref ulong hash, byte value) { hash ^= value; hash *= FnvPrime; }
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private struct AcceptedPoint
        {
            public readonly Vector3 Position;
            public readonly float MinimumSpacing;
            public AcceptedPoint(Vector3 position, float minimumSpacing) { Position = position; MinimumSpacing = minimumSpacing; }
        }

        private readonly struct SpacingCell : IEquatable<SpacingCell>
        {
            private readonly int x, z, group;
            public SpacingCell(int x, int z, int group) { this.x = x; this.z = z; this.group = group; }
            public bool Equals(SpacingCell other) => x == other.x && z == other.z && group == other.group;
            public override bool Equals(object obj) => obj is SpacingCell other && Equals(other);
            public override int GetHashCode() => unchecked(((x * 397) ^ z) * 397 ^ group);
        }

        private struct LocalDeterministicRandom
        {
            private uint state;
            public LocalDeterministicRandom(ulong seed)
            {
                state = (uint)(seed ^ (seed >> 32));
                if (state == 0u) state = 0x6d2b79f5u;
            }

            public float Next01()
            {
                uint x = state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                state = x;
                return (x >> 8) * (1f / 16777216f);
            }
        }
    }
}
