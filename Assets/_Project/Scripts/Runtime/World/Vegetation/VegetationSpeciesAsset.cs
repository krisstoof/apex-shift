using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ApexShift.Runtime.World.Topography;

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
        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private VegetationCategory category;
        [SerializeField] private VegetationForm form;
        [SerializeField, Min(0f)] private float minDistanceToCoast;
        [SerializeField, Min(0f)] private float maxDistanceToCoast = float.MaxValue;
        [SerializeField] private List<TerrainType> allowedTerrainTypes = new List<TerrainType>();
        [SerializeField, Min(0.01f)] private float minScale = 0.8f;
        [SerializeField, Min(0.01f)] private float maxScale = 1.2f;
        [SerializeField, Min(0f)] private float minimumSpacing = 1f;
        [Header("Tree trunk collision")]
        [SerializeField, Min(0.01f)] private float trunkRadius = 0.18f;
        [SerializeField, Min(0.02f)] private float trunkHeight = 1.8f;
        [SerializeField, Min(0f)] private float trunkCenterY = 0.9f;
        [Header("Tree lifecycle")]
        [SerializeField, Min(1f)] private float treeMaxHealth = 100f;
        [SerializeField, Min(0)] private int treeRegrowthDays = 5;
        [SerializeField, Min(0f)] private float treeFallDuration = 1.2f;
        [SerializeField, Range(0f, 90f)] private float minSlopeDegrees;
        [SerializeField, Range(0f, 90f)] private float maxSlopeDegrees = 90f;
        [SerializeField, Range(0f, 1f)] private float minElevation01;
        [SerializeField, Range(0f, 1f)] private float maxElevation01 = 1f;
        [SerializeField, Range(0f, 1f)] private float minMoisture01;
        [SerializeField, Range(0f, 1f)] private float maxMoisture01 = 1f;
        [FormerlySerializedAs("allowedBiomeIds")]
        [SerializeField] private List<string> allowedHabitatIds = new List<string>();
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
        public VegetationForm Form => form;
        public float MinDistanceToCoast => minDistanceToCoast;
        public float MaxDistanceToCoast => maxDistanceToCoast;
        public IReadOnlyList<TerrainType> AllowedTerrainTypes => allowedTerrainTypes;
        public float MinScale => minScale;
        public float MaxScale => maxScale;
        public float MinimumSpacing => minimumSpacing;
        public float TrunkRadius => trunkRadius;
        public float TrunkHeight => trunkHeight;
        public float TrunkCenterY => trunkCenterY;
        public float TreeMaxHealth => treeMaxHealth;
        public int TreeRegrowthDays => treeRegrowthDays;
        public float TreeFallDuration => treeFallDuration;
        public float MinSlopeDegrees => minSlopeDegrees;
        public float MaxSlopeDegrees => maxSlopeDegrees;
        public float MinElevation01 => minElevation01;
        public float MaxElevation01 => maxElevation01;
        public float MinMoisture01 => minMoisture01;
        public float MaxMoisture01 => maxMoisture01;
        public IReadOnlyList<string> AllowedHabitatIds => allowedHabitatIds;
        public bool RandomYaw => randomYaw;
        public bool Harvestable => harvestable;
        /// <summary>The Apex Shift ResourceDefinition kind, not the resulting drop/item ID.</summary>
        public string ResourceKind => resourceKind ?? string.Empty;
        public GameObject DepletedVisualPrefab => depletedVisualPrefab;
        public VegetationCollisionMode CollisionMode => collisionMode;

        public void Configure(string id, string label, GameObject prefab, VegetationCategory vegetationCategory,
            float minimumScale, float maximumScale, float spacing,
            float minimumSlope, float maximumSlope, float minimumElevation, float maximumElevation,
            float minimumMoisture, float maximumMoisture, IEnumerable<string> habitatIds,
            bool yawRandomized, bool canHarvest, string harvestResourceKind,
            GameObject depletedPrefab, VegetationCollisionMode collision,
            float treeTrunkRadius = 0.18f, float treeTrunkHeight = 1.8f, float treeTrunkCenterY = 0.9f,
            float maxTreeHealth = 100f, int regrowthDays = 5, float fallDuration = 1.2f)
        {
            speciesId = NormalizeSpeciesId(id);
            displayName = label ?? string.Empty;
            visualPrefab = prefab;
            category = vegetationCategory;
            minScale = minimumScale;
            maxScale = maximumScale;
            minimumSpacing = spacing;
            trunkRadius = Mathf.Max(0.01f, treeTrunkRadius);
            trunkHeight = Mathf.Max(trunkRadius * 2f, treeTrunkHeight);
            trunkCenterY = Mathf.Max(0f, treeTrunkCenterY);
            treeMaxHealth = Mathf.Max(1f, maxTreeHealth);
            treeRegrowthDays = Mathf.Max(0, regrowthDays);
            treeFallDuration = Mathf.Max(0f, fallDuration);
            minSlopeDegrees = minimumSlope;
            maxSlopeDegrees = maximumSlope;
            minElevation01 = minimumElevation;
            maxElevation01 = maximumElevation;
            minMoisture01 = minimumMoisture;
            maxMoisture01 = maximumMoisture;
            allowedHabitatIds = new List<string>();
            if (habitatIds != null)
                foreach (string habitatId in habitatIds)
                    allowedHabitatIds.Add(NormalizeSpeciesId(habitatId));
            randomYaw = yawRandomized;
            harvestable = canHarvest;
            resourceKind = harvestResourceKind ?? string.Empty;
            depletedVisualPrefab = depletedPrefab;
            collisionMode = collision;
        }

        public List<string> Validate()
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

            var seenHabitats = new HashSet<string>(StringComparer.Ordinal);
            foreach (string habitatId in allowedHabitatIds ?? new List<string>())
            {
                string normalizedHabitat = NormalizeSpeciesId(habitatId);
                if (string.IsNullOrWhiteSpace(habitatId) || !string.Equals(habitatId, normalizedHabitat, StringComparison.Ordinal))
                    errors.Add($"Species '{speciesId}' has invalid habitat ID '{habitatId}'.");
                if (!seenHabitats.Add(normalizedHabitat))
                    errors.Add($"Species '{speciesId}' repeats allowed habitat ID '{normalizedHabitat}'.");
                if (!HabitatVegetationProfileAsset.IsValidHabitatId(normalizedHabitat))
                    errors.Add($"Species '{speciesId}' references unknown habitat ID '{habitatId}'.");
            }
            if (!IsFinite(minDistanceToCoast) || !IsFinite(maxDistanceToCoast)
                || minDistanceToCoast < 0f || maxDistanceToCoast < minDistanceToCoast)
                errors.Add($"Species '{speciesId}' has invalid distance-to-coast range.");
            if (!Enum.IsDefined(typeof(VegetationForm), form))
                errors.Add($"Species '{speciesId}' has invalid vegetation form.");
            var terrainSet = new HashSet<TerrainType>();
            foreach (TerrainType terrain in allowedTerrainTypes ?? new List<TerrainType>())
                if (!Enum.IsDefined(typeof(TerrainType), terrain) || terrain == TerrainType.Water || !terrainSet.Add(terrain))
                    errors.Add($"Species '{speciesId}' has invalid or duplicate land TerrainType '{terrain}'.");
            return errors;
        }

        public void ConfigureEnvironment(VegetationForm vegetationForm, float minimumCoastDistance = 0f,
            float maximumCoastDistance = float.MaxValue, IEnumerable<TerrainType> terrainTypes = null)
        {
            form = vegetationForm;
            minDistanceToCoast = minimumCoastDistance;
            maxDistanceToCoast = maximumCoastDistance;
            allowedTerrainTypes = terrainTypes == null ? new List<TerrainType>() : new List<TerrainType>(terrainTypes);
        }

        public bool AllowsTerrain(TerrainType terrain)
            => terrain != TerrainType.Water && (allowedTerrainTypes == null || allowedTerrainTypes.Count == 0 || allowedTerrainTypes.Contains(terrain));

        public bool AllowsHabitat(string habitatId)
        {
            string normalized = NormalizeSpeciesId(habitatId);
            if (!HabitatVegetationProfileAsset.IsValidHabitatId(normalized)) return false;
            if (allowedHabitatIds == null || allowedHabitatIds.Count == 0) return true;
            for (int i = 0; i < allowedHabitatIds.Count; i++)
                if (string.Equals(allowedHabitatIds[i], normalized, StringComparison.Ordinal)) return true;
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


        private void ValidateUnitRange(string label, float min, float max, List<string> errors)
        {
            if (!IsFinite(min) || !IsFinite(max) || min < 0f || max > 1f || max < min)
                errors.Add($"Species '{speciesId}' has invalid {label} range [{min}, {max}]; expected 0..1 with min <= max.");
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
