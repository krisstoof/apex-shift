using System;
using System.Collections.Generic;
using ApexShift.Runtime.World.Generation;
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

        private const float MaximumLocalDensityMultiplier = 1.35f;
        private const float LocalDensityMinimum = 0.55f;
        private const float LocalDensityMaximum = 1.35f;
        private const float LocalDensityCellSize = 48f;

        public List<VegetationPlacement> Plan(int seed, HabitatVegetationCatalogAsset habitatCatalog,
            VegetationGenerationSettings settings, Bounds worldBounds,
            Func<Vector3, VegetationEnvironmentSample> sampleEnvironment,
            Func<Vector3, float> sampleHeight, Vector3 playerSpawn, float startClearingRadius,
            IReadOnlyList<VegetationLandmarkClearance> landmarks,
            VegetationRejectionCounts rejectionCounts = null)
        {
            var placements = new List<VegetationPlacement>();
            if (habitatCatalog == null || settings == null || sampleEnvironment == null || sampleHeight == null) return placements;

            spacing.Clear();
            emittedMissingPrefabWarnings.Clear();
            spacingCellSize = FindMaximumSpacing(habitatCatalog);

            foreach (HabitatVegetationProfileAsset profile in habitatCatalog.Profiles)
            {
                if (profile == null || profile.Species == null || profile.Species.Count == 0) continue;
                float[] categoryWeights = new float[4];
                categoryWeights[(int)VegetationCategory.Tree] = CalculateCategoryWeight(profile.Species, VegetationCategory.Tree);
                categoryWeights[(int)VegetationCategory.DeadTree] = CalculateCategoryWeight(profile.Species, VegetationCategory.DeadTree);
                categoryWeights[(int)VegetationCategory.Shrub] = CalculateCategoryWeight(profile.Species, VegetationCategory.Shrub);
                categoryWeights[(int)VegetationCategory.GroundCover] = CalculateCategoryWeight(profile.Species, VegetationCategory.GroundCover);

                for (int i = 0; i < profile.Species.Count; i++)
                {
                    HabitatVegetationSpeciesEntry entry = profile.Species[i];
                    VegetationSpeciesAsset speciesAsset = entry != null ? entry.Species : null;
                    if (speciesAsset == null || entry.Weight <= 0f || entry.DensityMultiplier <= 0f) continue;
                    float categoryWeight = categoryWeights[(int)speciesAsset.Category];
                    if (categoryWeight <= 0f) continue;
                    string habitatId = profile.HabitatId;
                    if (!speciesAsset.AllowsHabitat(habitatId)) continue;
                    if (speciesAsset.VisualPrefab == null)
                    {
                        if (emittedMissingPrefabWarnings.Add(speciesAsset.SpeciesId))
                            Debug.LogWarning($"Vegetation configuration error: species '{speciesAsset.SpeciesId}' has no VisualPrefab; it will be skipped.");
                        continue;
                    }

                    float density = settings.GetBaseDensity(speciesAsset.Category)
                        * profile.OverallDensity
                        * entry.DensityMultiplier
                        * (entry.Weight / categoryWeight);
                    if (!IsFinite(density) || density <= 0f) continue;
                    PlanSpecies(seed, habitatId, speciesAsset, density, settings, worldBounds,
                        sampleEnvironment, sampleHeight, playerSpawn, startClearingRadius,
                        landmarks, placements, rejectionCounts);
                }
            }
            return placements;
        }

        private static float CalculateCategoryWeight(IReadOnlyList<HabitatVegetationSpeciesEntry> entries, VegetationCategory category)
        {
            if (entries == null) return 0f;
            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                HabitatVegetationSpeciesEntry entry = entries[i];
                if (entry?.Species != null && entry.Species.Category == category)
                    total += Mathf.Max(0f, entry.Weight);
            }
            return total;
        }

        private void PlanSpecies(int seed, string habitatId, VegetationSpeciesAsset species,
            float density, VegetationGenerationSettings settings, Bounds worldBounds,
            Func<Vector3, VegetationEnvironmentSample> sampleEnvironment, Func<Vector3, float> sampleHeight,
            Vector3 playerSpawn, float startClearingRadius,
            IReadOnlyList<VegetationLandmarkClearance> landmarks,
            List<VegetationPlacement> output, VegetationRejectionCounts rejections)
        {
            // Candidate density uses the modulation ceiling; the local field then
            // deterministically thins candidates to form broad patches and clearings.
            float candidateDensity = density * MaximumLocalDensityMultiplier;
            float cellSize = Mathf.Sqrt(1f / candidateDensity);
            if (!IsFinite(cellSize) || cellSize <= 0f) return;
            int minX = Mathf.FloorToInt(worldBounds.min.x / cellSize);
            int maxX = Mathf.CeilToInt(worldBounds.max.x / cellSize);
            int minZ = Mathf.FloorToInt(worldBounds.min.z / cellSize);
            int maxZ = Mathf.CeilToInt(worldBounds.max.z / cellSize);
            float coastClearance = settings.GetCoastalClearance(species.Category);
            int spacingGroup = GetSpacingGroup(species.Category);

            for (int gridZ = minZ; gridZ < maxZ; gridZ++)
            for (int gridX = minX; gridX < maxX; gridX++)
            {
                float baseX = (gridX + 0.5f) * cellSize;
                float baseZ = (gridZ + 0.5f) * cellSize;
                var basePosition = new Vector3(baseX, 0f, baseZ);
                VegetationEnvironmentSample baseEnvironment = sampleEnvironment(basePosition);
                if (!baseEnvironment.IsLand || baseEnvironment.IsWater) { if (rejections != null) rejections.Water++; continue; }
                if (!string.Equals(baseEnvironment.HabitatId, habitatId, StringComparison.Ordinal)) { if (rejections != null) rejections.HabitatMismatch++; continue; }

                int baseChunkX = Mathf.FloorToInt(baseX / settings.ChunkSize);
                int baseChunkZ = Mathf.FloorToInt(baseZ / settings.ChunkSize);
                var random = new LocalDeterministicRandom(StableHash(seed, baseChunkX, baseChunkZ, habitatId, species.SpeciesId, gridX, gridZ));
                float x = baseX + (random.Next01() - 0.5f) * cellSize * settings.JitterFraction;
                float z = baseZ + (random.Next01() - 0.5f) * cellSize * settings.JitterFraction;
                var candidate = new Vector3(x, 0f, z);
                VegetationEnvironmentSample environment = sampleEnvironment(candidate);
                if (!environment.IsLand || environment.IsWater) { if (rejections != null) rejections.Water++; continue; }
                if (!string.Equals(environment.HabitatId, habitatId, StringComparison.Ordinal) || !species.AllowsHabitat(environment.HabitatId)) { if (rejections != null) rejections.HabitatMismatch++; continue; }
                if (environment.SlopeDegrees < species.MinSlopeDegrees || environment.SlopeDegrees > species.MaxSlopeDegrees) { if (rejections != null) rejections.ExcessiveSlope++; continue; }
                if (environment.NormalizedElevation < species.MinElevation01 || environment.NormalizedElevation > species.MaxElevation01) { if (rejections != null) rejections.Elevation++; continue; }
                if (environment.Moisture01 < species.MinMoisture01 || environment.Moisture01 > species.MaxMoisture01) { if (rejections != null) rejections.Moisture++; continue; }
                if (!species.AllowsTerrain(environment.TerrainType))
                { if (rejections != null) rejections.Terrain++; continue; }
                if (environment.DistanceToCoast < species.MinDistanceToCoast || environment.DistanceToCoast > species.MaxDistanceToCoast)
                { if (rejections != null) rejections.CoastDistance++; continue; }
                if (environment.IsShoreline) { if (rejections != null) rejections.ShorelineOrClearing++; continue; }
                if (!CanPlaceInStartClearing(species.Category, settings, candidate, playerSpawn, startClearingRadius)
                    || IsNearLandmark(candidate, landmarks) || environment.DistanceToCoast < coastClearance)
                { if (rejections != null) rejections.ShorelineOrClearing++; continue; }

                float localDensityMultiplier = SampleLocalDensityMultiplier(seed, candidate.x, candidate.z);
                if (random.Next01() > localDensityMultiplier / MaximumLocalDensityMultiplier) continue;

                float height = sampleHeight(candidate);
                candidate.y = height;
                if (ViolatesSpacing(candidate, species.MinimumSpacing, spacingGroup)) { if (rejections != null) rejections.SpacingOrCollision++; continue; }

                float yaw = species.RandomYaw ? random.Next01() * 360f : 0f;
                float scale = Mathf.Lerp(species.MinScale, species.MaxScale, random.Next01());
                int chunkX = Mathf.FloorToInt(candidate.x / settings.ChunkSize);
                int chunkZ = Mathf.FloorToInt(candidate.z / settings.ChunkSize);
                string instanceId = BuildInstanceId(seed, species.SpeciesId, candidate);
                var placement = new VegetationPlacement(instanceId, habitatId, species, candidate, yaw, scale, chunkX, chunkZ);
                AddSpacingPoint(candidate, species.MinimumSpacing, spacingGroup);
                output.Add(placement);
            }
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

        private static float FindMaximumSpacing(HabitatVegetationCatalogAsset catalog)
        {
            float max = 0.1f;
            foreach (HabitatVegetationProfileAsset profile in catalog.Profiles)
            {
                IReadOnlyList<HabitatVegetationSpeciesEntry> entries = profile != null ? profile.Species : null;
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

        private static string BuildInstanceId(int seed, string speciesId, Vector3 position)
        {
            ulong hash = FnvOffset;
            AddInt(ref hash, seed);
            AddString(ref hash, speciesId);
            AddInt(ref hash, Mathf.RoundToInt(position.x * 1000f));
            AddInt(ref hash, Mathf.RoundToInt(position.z * 1000f));
            return "veg_" + hash.ToString("x16", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static float SampleLocalDensityMultiplier(int seed, float x, float z)
        {
            float gx = x / LocalDensityCellSize;
            float gz = z / LocalDensityCellSize;
            int x0 = Mathf.FloorToInt(gx);
            int z0 = Mathf.FloorToInt(gz);
            float tx = Smooth01(gx - x0);
            float tz = Smooth01(gz - z0);
            float a = Mathf.Lerp(HashDensity(seed, x0, z0), HashDensity(seed, x0 + 1, z0), tx);
            float b = Mathf.Lerp(HashDensity(seed, x0, z0 + 1), HashDensity(seed, x0 + 1, z0 + 1), tx);
            return Mathf.Lerp(LocalDensityMinimum, LocalDensityMaximum, Mathf.Lerp(a, b, tz));
        }

        private static float HashDensity(int seed, int x, int z)
        {
            ulong hash = FnvOffset;
            AddInt(ref hash, seed);
            AddInt(ref hash, x);
            AddInt(ref hash, z);
            uint value = (uint)(hash ^ (hash >> 32));
            return (value & 0x00ffffffu) / 16777215f;
        }

        private static float Smooth01(float value) => value * value * (3f - 2f * value);

        private static ulong StableHash(int seed, int chunkX, int chunkZ, string habitatId, string speciesId, int gridX, int gridZ)
        {
            ulong hash = FnvOffset;
            AddInt(ref hash, seed);
            AddInt(ref hash, chunkX);
            AddInt(ref hash, chunkZ);
            AddString(ref hash, habitatId);
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
