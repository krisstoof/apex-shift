using System;
using ApexShift.Runtime.World.Vegetation;

namespace ApexShift.Runtime.UI.Snapshots
{
    [Serializable]
    public sealed class VegetationDebugSnapshot
    {
        public int totalPlacements, totalChunks;
        public int activeTreeChunks, activeShrubChunks, activeGroundCoverChunks;
        public int visibleTrees, visibleShrubs, visibleGroundCover;
        public int activeHarvestableTrees, activeTreeColliders;
        public int maxActiveHarvestableTrees, maxActiveTreeColliders;
        public int pooledInstances, poolHits, poolMisses;
        public int lodGroupTreeCount, missingLodTreeCount;
        public float streamingRefreshMs;
        public int chunkTransitionsLastRefresh;

        public static VegetationDebugSnapshot Empty => new VegetationDebugSnapshot();

        public static VegetationDebugSnapshot FromStats(VegetationRuntimeStats stats, int treeBudget = 0, int colliderBudget = 0)
        {
            return new VegetationDebugSnapshot
            {
                totalPlacements = stats.TotalPlacements,
                totalChunks = stats.TotalChunks,
                activeTreeChunks = stats.ActiveTreeChunks,
                activeShrubChunks = stats.ActiveShrubChunks,
                activeGroundCoverChunks = stats.ActiveGroundCoverChunks,
                visibleTrees = stats.VisibleTrees,
                visibleShrubs = stats.VisibleShrubs,
                visibleGroundCover = stats.VisibleGroundCover,
                activeHarvestableTrees = stats.ActiveHarvestableTrees,
                activeTreeColliders = stats.ActiveTreeColliders,
                maxActiveHarvestableTrees = treeBudget,
                maxActiveTreeColliders = colliderBudget,
                pooledInstances = stats.PooledInstances,
                poolHits = stats.PoolHits,
                poolMisses = stats.PoolMisses,
                lodGroupTreeCount = stats.LodGroupTreeCount,
                missingLodTreeCount = stats.MissingLodTreeCount,
                streamingRefreshMs = stats.StreamingRefreshMs,
                chunkTransitionsLastRefresh = stats.ChunkTransitionsLastRefresh
            };
        }
    }
}
