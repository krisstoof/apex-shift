using System;
using UnityEngine;
using ApexShift.Runtime.World.Landmarks;

namespace ApexShift.Runtime.World.Vegetation
{
    [Serializable]
    public sealed class VegetationLandmarkClearanceSettings
    {
        [SerializeField, Min(0f)] private float oldTree = 18f;
        [SerializeField, Min(0f)] private float ruins = 10f;
        [SerializeField, Min(0f)] private float pond = 8f;
        [SerializeField, Min(0f)] private float camp = 12f;
        [SerializeField, Min(0f)] private float cavePlaceholder = 10f;

        public float GetRadius(LandmarkType type)
        {
            switch (type)
            {
                case LandmarkType.OldTree: return oldTree;
                case LandmarkType.Ruins: return ruins;
                case LandmarkType.Pond: return pond;
                case LandmarkType.Camp: return camp;
                case LandmarkType.CavePlaceholder: return cavePlaceholder;
                default: return 0f;
            }
        }

        public void Configure(float oldTreeRadius, float ruinsRadius, float pondRadius, float campRadius, float caveRadius)
        {
            oldTree = Mathf.Max(0f, oldTreeRadius);
            ruins = Mathf.Max(0f, ruinsRadius);
            pond = Mathf.Max(0f, pondRadius);
            camp = Mathf.Max(0f, campRadius);
            cavePlaceholder = Mathf.Max(0f, caveRadius);
        }
    }

    /// <summary>All authored density, chunk, sampling, and clearance controls for procedural vegetation.</summary>
    [Serializable]
    public sealed class VegetationGenerationSettings
    {
        [Header("Base density (expected instances per square world unit)")]
        [SerializeField, Min(0f)] private float treeBaseDensity = 0.009f;
        [SerializeField, Min(0f)] private float deadTreeBaseDensity = 0.0012f;
        [SerializeField, Min(0f)] private float shrubBaseDensity = 0.018f;
        [SerializeField, Min(0f)] private float groundCoverBaseDensity = 0.045f;
        [Header("Placement")]
        [SerializeField, Min(1f)] private float chunkSize = 24f;
        [SerializeField, Range(0f, 1f)] private float jitterFraction = 0.88f;
        [SerializeField] private bool allowGroundCoverInStartClearing;
        [Header("Coast clearance")]
        [SerializeField, Min(0f)] private float treeCoastalClearance = 4f;
        [SerializeField, Min(0f)] private float shrubCoastalClearance = 3f;
        [SerializeField, Min(0f)] private float groundCoverCoastalClearance = 0.75f;
        [Header("Landmark clearance")]
        [SerializeField] private VegetationLandmarkClearanceSettings landmarkClearances = new VegetationLandmarkClearanceSettings();

        public float TreeBaseDensity => treeBaseDensity;
        public float DeadTreeBaseDensity => deadTreeBaseDensity;
        public float ShrubBaseDensity => shrubBaseDensity;
        public float GroundCoverBaseDensity => groundCoverBaseDensity;
        public float ChunkSize => Mathf.Max(1f, chunkSize);
        public float JitterFraction => Mathf.Clamp01(jitterFraction);
        public bool AllowGroundCoverInStartClearing => allowGroundCoverInStartClearing;
        public float TreeCoastalClearance => treeCoastalClearance;
        public float ShrubCoastalClearance => shrubCoastalClearance;
        public float GroundCoverCoastalClearance => groundCoverCoastalClearance;
        public VegetationLandmarkClearanceSettings LandmarkClearances => landmarkClearances ?? (landmarkClearances = new VegetationLandmarkClearanceSettings());

        public float GetBaseDensity(VegetationCategory category)
        {
            switch (category)
            {
                case VegetationCategory.Tree: return treeBaseDensity;
                case VegetationCategory.DeadTree: return deadTreeBaseDensity;
                case VegetationCategory.Shrub: return shrubBaseDensity;
                case VegetationCategory.GroundCover: return groundCoverBaseDensity;
                default: return 0f;
            }
        }

        public float GetCoastalClearance(VegetationCategory category)
        {
            switch (category)
            {
                case VegetationCategory.Tree:
                case VegetationCategory.DeadTree: return treeCoastalClearance;
                case VegetationCategory.Shrub: return shrubCoastalClearance;
                case VegetationCategory.GroundCover: return groundCoverCoastalClearance;
                default: return 0f;
            }
        }

        public void ConfigureDensities(float trees, float deadTrees, float shrubs, float groundCover)
        {
            treeBaseDensity = Mathf.Max(0f, trees);
            deadTreeBaseDensity = Mathf.Max(0f, deadTrees);
            shrubBaseDensity = Mathf.Max(0f, shrubs);
            groundCoverBaseDensity = Mathf.Max(0f, groundCover);
        }

        public void ConfigurePlacement(float newChunkSize, float newJitterFraction, bool groundCoverInStartClearing)
        {
            chunkSize = Mathf.Max(1f, newChunkSize);
            jitterFraction = Mathf.Clamp01(newJitterFraction);
            allowGroundCoverInStartClearing = groundCoverInStartClearing;
        }

        public void ConfigureClearances(float treeCoast, float shrubCoast, float groundCoverCoast,
            float oldTree, float ruins, float pond, float camp, float cave)
        {
            treeCoastalClearance = Mathf.Max(0f, treeCoast);
            shrubCoastalClearance = Mathf.Max(0f, shrubCoast);
            groundCoverCoastalClearance = Mathf.Max(0f, groundCoverCoast);
            LandmarkClearances.Configure(oldTree, ruins, pond, camp, cave);
        }
    }
}
