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
        [SerializeField, Min(0f)] private float treeBaseDensity = 0.016f;
        [SerializeField, Min(0f)] private float deadTreeBaseDensity = 0.0025f;
        [SerializeField, Min(0f)] private float shrubBaseDensity = 0.030f;
        [SerializeField, Min(0f)] private float groundCoverBaseDensity = 0.075f;
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
        [Header("Streaming")]
        [SerializeField] private bool streamingEnabled = true;
        [SerializeField, Min(0.05f)] private float streamingUpdateInterval = 0.25f;
        [SerializeField, Min(0f)] private float treeVisualRadius = 100f;
        [SerializeField, Min(0f)] private float shrubVisualRadius = 65f;
        [SerializeField, Min(0f)] private float groundCoverVisualRadius = 42f;
        [SerializeField, Min(0f)] private float treeGameplayRadius = 32f;
        [SerializeField, Min(0f)] private float hysteresisDistance = 12f;
        [SerializeField, Min(0)] private int maxVisibleTrees = 1200;
        [SerializeField, Min(0)] private int maxVisibleShrubs = 1000;
        [SerializeField, Min(0)] private int maxVisibleGroundCover = 1800;
        [SerializeField, Min(0)] private int maxActiveHarvestableTrees = 160;
        [SerializeField, Min(0)] private int maxActiveTreeColliders = 200;
        [SerializeField, Min(1)] private int maxChunkTransitionsPerUpdate = 4;
        [SerializeField, Min(0)] private int decorativePoolCapacityPerSpecies = 512;

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
        public bool StreamingEnabled => streamingEnabled;
        public float StreamingUpdateInterval => Mathf.Max(0.05f, streamingUpdateInterval);
        public float TreeVisualRadius => Mathf.Max(0f, treeVisualRadius);
        public float ShrubVisualRadius => Mathf.Max(0f, shrubVisualRadius);
        public float GroundCoverVisualRadius => Mathf.Max(0f, groundCoverVisualRadius);
        public float TreeGameplayRadius => Mathf.Max(0f, treeGameplayRadius);
        public float HysteresisDistance => Mathf.Max(0f, hysteresisDistance);
        public int MaxVisibleTrees => Mathf.Max(0, maxVisibleTrees);
        public int MaxVisibleShrubs => Mathf.Max(0, maxVisibleShrubs);
        public int MaxVisibleGroundCover => Mathf.Max(0, maxVisibleGroundCover);
        public int MaxActiveHarvestableTrees => Mathf.Max(0, maxActiveHarvestableTrees);
        public int MaxActiveTreeColliders => Mathf.Max(0, maxActiveTreeColliders);
        public int MaxChunkTransitionsPerUpdate => Mathf.Max(1, maxChunkTransitionsPerUpdate);
        public int DecorativePoolCapacityPerSpecies => Mathf.Max(0, decorativePoolCapacityPerSpecies);

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

        public void ConfigureStreaming(bool enabled, float updateInterval, float treesRadius, float shrubsRadius,
            float groundCoverRadius, float gameplayRadius, float hysteresis, int visibleTrees, int visibleShrubs,
            int visibleGroundCover, int activeTrees, int activeTreeColliders, int chunkTransitions, int poolCapacity)
        {
            streamingEnabled = enabled;
            streamingUpdateInterval = Mathf.Max(0.05f, updateInterval);
            treeVisualRadius = Mathf.Max(0f, treesRadius);
            shrubVisualRadius = Mathf.Max(0f, shrubsRadius);
            groundCoverVisualRadius = Mathf.Max(0f, groundCoverRadius);
            treeGameplayRadius = Mathf.Max(0f, gameplayRadius);
            hysteresisDistance = Mathf.Max(0f, hysteresis);
            maxVisibleTrees = Mathf.Max(0, visibleTrees);
            maxVisibleShrubs = Mathf.Max(0, visibleShrubs);
            maxVisibleGroundCover = Mathf.Max(0, visibleGroundCover);
            maxActiveHarvestableTrees = Mathf.Max(0, activeTrees);
            maxActiveTreeColliders = Mathf.Max(0, activeTreeColliders);
            maxChunkTransitionsPerUpdate = Mathf.Max(1, chunkTransitions);
            decorativePoolCapacityPerSpecies = Mathf.Max(0, poolCapacity);
        }
    }
}
