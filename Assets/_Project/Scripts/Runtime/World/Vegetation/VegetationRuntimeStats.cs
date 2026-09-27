namespace ApexShift.Runtime.World.Vegetation
{
    public readonly struct VegetationDebugPoint
    {
        public readonly UnityEngine.Vector3 Position;
        public readonly VegetationCategory Category;
        public readonly bool Harvestable;
        public readonly bool StreamingGameplayActive;
        public readonly TreeLifecycleState TreeState;
        public readonly float RegrowthProgress;
        public VegetationDebugPoint(UnityEngine.Vector3 position, VegetationCategory category, bool harvestable,
            bool streamingGameplayActive, TreeLifecycleState treeState, float regrowthProgress)
        { Position = position; Category = category; Harvestable = harvestable; StreamingGameplayActive = streamingGameplayActive; TreeState = treeState; RegrowthProgress = regrowthProgress; }
    }

    public readonly struct VegetationRuntimeStats
    {
        public readonly int TotalPlacements, TotalChunks;
        public readonly int ActiveTreeChunks, ActiveShrubChunks, ActiveGroundCoverChunks;
        public readonly int VisibleTrees, VisibleShrubs, VisibleGroundCover;
        public readonly int ActiveHarvestableTrees, ActiveTreeColliders;
        public readonly int PooledInstances, PoolHits, PoolMisses;
        public readonly int LodGroupTreeCount, MissingLodTreeCount;
        public readonly float StreamingRefreshMs;
        public readonly int ChunkTransitionsLastRefresh;

        public VegetationRuntimeStats(int totalPlacements, int totalChunks, int activeTreeChunks, int activeShrubChunks,
            int activeGroundCoverChunks, int visibleTrees, int visibleShrubs, int visibleGroundCover,
            int activeHarvestableTrees, int activeTreeColliders, int pooledInstances, int poolHits, int poolMisses,
            int lodGroupTreeCount, int missingLodTreeCount, float streamingRefreshMs, int chunkTransitionsLastRefresh)
        {
            TotalPlacements = totalPlacements; TotalChunks = totalChunks;
            ActiveTreeChunks = activeTreeChunks; ActiveShrubChunks = activeShrubChunks; ActiveGroundCoverChunks = activeGroundCoverChunks;
            VisibleTrees = visibleTrees; VisibleShrubs = visibleShrubs; VisibleGroundCover = visibleGroundCover;
            ActiveHarvestableTrees = activeHarvestableTrees; ActiveTreeColliders = activeTreeColliders;
            PooledInstances = pooledInstances; PoolHits = poolHits; PoolMisses = poolMisses;
            LodGroupTreeCount = lodGroupTreeCount; MissingLodTreeCount = missingLodTreeCount;
            StreamingRefreshMs = streamingRefreshMs; ChunkTransitionsLastRefresh = chunkTransitionsLastRefresh;
        }
    }
}
