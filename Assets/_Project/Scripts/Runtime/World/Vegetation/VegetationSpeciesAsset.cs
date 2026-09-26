using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    public enum VegetationCategory
    {
        Tree,
        Shrub,
        GroundCover,
        DeadTree
    }

    public enum VegetationCollisionMode
    {
        None,
        VisualPrefab,
        TrunkOnly,
        GameplayResource
    }

    [CreateAssetMenu(menuName = "Apex Shift/World/Vegetation Species", fileName = "VegetationSpecies")]
    public sealed class VegetationSpeciesAsset : ScriptableObject
    {
        public static readonly string[] CanonicalBiomeIds =
        {
            "hearth_meadow", "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds"
        };

        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private VegetationCategory category;
        [SerializeField, Min(0.01f)] private float minScale = 0.8f;
        [SerializeField, Min(0.01f)] private float maxScale = 1.2f;
        [SerializeField, Min(0f)] private float minimumSpacing = 1f;
        [SerializeField, Range(0f, 90f)] private float minSlopeDegrees;
        [SerializeField, Range(0f, 90f)] private float maxSlopeDegrees = 90f;
        [SerializeField, Range(0f, 1f)] private float minElevation01;
        [SerializeField, Range(0f, 1f)] private float maxElevation01 = 1f;
        [SerializeField, Range(0f, 1f)] private float minMoisture01;
        [SerializeField, Range(0f, 1f)] private float maxMoisture01 = 1f;
        [SerializeField] private List<string> allowedBiomeIds = new List<string>();
        [SerializeField] private bool randomYaw = true;
        [SerializeField] private bool harvestable;
        [Tooltip("Apex Shift ResourceDefinition kind (for example leafy_tree), not the resulting drop/item ID.")]
        [SerializeField] private string resourceKind = string.Empty;
        [SerializeField] private GameObject depletedVisualPrefab;
        [SerializeField] private VegetationCollisionMode collisionMode;

        public string SpeciesId => speciesId;
        public string DisplayName => displayName;
        public GameObject VisualPrefab => visualPrefab;
        public VegetationCategory Category => category;
        public float MinScale => minScale;
        public float MaxScale => maxScale;
        public float MinimumSpacing => minimumSpacing;
        public float MinSlopeDegrees => minSlopeDegrees;
        public float MaxSlopeDegrees => maxSlopeDegrees;
        public float MinElevation01 => minElevation01;
        public float MaxElevation01 => maxElevation01;
        public float MinMoisture01 => minMoisture01;
        public float MaxMoisture01 => maxMoisture01;
        public IReadOnlyList<string> AllowedBiomeIds => allowedBiomeIds;
        public bool RandomYaw => randomYaw;
        public bool Harvestable => harvestable;
        /// <summary>The Apex Shift ResourceDefinition kind, not the resulting drop/item ID.</summary>
        public string ResourceKind => resourceKind ?? string.Empty;
        public GameObject DepletedVisualPrefab => depletedVisualPrefab;
        public VegetationCollisionMode CollisionMode => collisionMode;

        public void Configure(string id, string label, GameObject prefab, VegetationCategory vegetationCategory,
            float minimumScale, float maximumScale, float spacing,
            float minimumSlope, float maximumSlope, float minimumElevation, float maximumElevation,
            float minimumMoisture, float maximumMoisture, IEnumerable<string> biomeIds,
            bool yawRandomized, bool canHarvest, string harvestResourceKind,
            GameObject depletedPrefab, VegetationCollisionMode collision)
        {
            speciesId = NormalizeSpeciesId(id);
            displayName = label ?? string.Empty;
            visualPrefab = prefab;
            category = vegetationCategory;
            minScale = minimumScale;
            maxScale = maximumScale;
            minimumSpacing = spacing;
            minSlopeDegrees = minimumSlope;
            maxSlopeDegrees = maximumSlope;
            minElevation01 = minimumElevation;
            maxElevation01 = maximumElevation;
            minMoisture01 = minimumMoisture;
            maxMoisture01 = maximumMoisture;
            allowedBiomeIds = new List<string>();
            if (biomeIds != null)
                foreach (string biomeId in biomeIds)
                    allowedBiomeIds.Add(NormalizeSpeciesId(biomeId));
            randomYaw = yawRandomized;
            harvestable = canHarvest;
            resourceKind = harvestResourceKind ?? string.Empty;
            depletedVisualPrefab = depletedPrefab;
            collisionMode = collision;
        }

        public List<string> Validate(ICollection<string> validBiomeIds = null)
        {
            var errors = new List<string>();
            string normalizedId = NormalizeSpeciesId(speciesId);
            if (string.IsNullOrWhiteSpace(speciesId))
                errors.Add("species ID is empty");
            else if (!string.Equals(speciesId, normalizedId, StringComparison.Ordinal))
                errors.Add($"species ID '{speciesId}' is not canonical snake_case (expected '{normalizedId}')");

            if (visualPrefab == null) errors.Add($"Species '{speciesId}' has no visual prefab assigned.");
            if (!IsFinite(minScale) || !IsFinite(maxScale) || minScale <= 0f || maxScale < minScale)
                errors.Add($"Species '{speciesId}' has invalid scale range [{minScale}, {maxScale}].");
            if (!IsFinite(minimumSpacing) || minimumSpacing < 0f)
                errors.Add($"Species '{speciesId}' has invalid minimum spacing {minimumSpacing}.");
            if (!IsFinite(minSlopeDegrees) || !IsFinite(maxSlopeDegrees)
                || minSlopeDegrees < 0f || maxSlopeDegrees > 90f || maxSlopeDegrees < minSlopeDegrees)
                errors.Add($"Species '{speciesId}' has invalid slope range [{minSlopeDegrees}, {maxSlopeDegrees}].");
            ValidateUnitRange("elevation", minElevation01, maxElevation01, errors);
            ValidateUnitRange("moisture", minMoisture01, maxMoisture01, errors);
            if (!Enum.IsDefined(typeof(VegetationCategory), category))
                errors.Add($"Species '{speciesId}' has invalid vegetation category '{category}'.");
            if (!Enum.IsDefined(typeof(VegetationCollisionMode), collisionMode))
                errors.Add($"Species '{speciesId}' has invalid collision mode '{collisionMode}'.");
            if (harvestable && string.IsNullOrWhiteSpace(resourceKind))
                errors.Add($"Harvestable species '{speciesId}' has no resourceKind.");

            var seenBiomes = new HashSet<string>(StringComparer.Ordinal);
            foreach (string biomeId in allowedBiomeIds ?? new List<string>())
            {
                string normalizedBiome = NormalizeSpeciesId(biomeId);
                if (string.IsNullOrWhiteSpace(biomeId) || !string.Equals(biomeId, normalizedBiome, StringComparison.Ordinal))
                    errors.Add($"Species '{speciesId}' has invalid biome ID '{biomeId}'.");
                if (!seenBiomes.Add(normalizedBiome))
                    errors.Add($"Species '{speciesId}' repeats allowed biome ID '{normalizedBiome}'.");
                ICollection<string> knownBiomes = validBiomeIds;
                if (knownBiomes == null) knownBiomes = CanonicalBiomeSet;
                if (!knownBiomes.Contains(normalizedBiome))
                    errors.Add($"Species '{speciesId}' references unknown biome ID '{biomeId}'.");
            }
            return errors;
        }

        public bool AllowsBiome(string biomeId)
        {
            if (allowedBiomeIds == null || allowedBiomeIds.Count == 0) return true;
            string normalized = NormalizeSpeciesId(biomeId);
            for (int i = 0; i < allowedBiomeIds.Count; i++)
                if (string.Equals(allowedBiomeIds[i], normalized, StringComparison.Ordinal)) return true;
            return false;
        }

        public static string NormalizeSpeciesId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var result = new System.Text.StringBuilder(value.Length);
            bool pendingSeparator = false;
            foreach (char raw in value.Trim())
            {
                char c = char.ToLowerInvariant(raw);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    if (pendingSeparator && result.Length > 0) result.Append('_');
                    result.Append(c);
                    pendingSeparator = false;
                }
                else pendingSeparator = true;
            }
            return result.ToString();
        }

        private static readonly HashSet<string> CanonicalBiomeSet = new HashSet<string>(CanonicalBiomeIds, StringComparer.Ordinal);

        private void ValidateUnitRange(string label, float min, float max, List<string> errors)
        {
            if (!IsFinite(min) || !IsFinite(max) || min < 0f || max > 1f || max < min)
                errors.Add($"Species '{speciesId}' has invalid {label} range [{min}, {max}]; expected 0..1 with min <= max.");
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
