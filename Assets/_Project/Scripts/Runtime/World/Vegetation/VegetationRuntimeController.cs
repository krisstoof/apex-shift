using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    [DisallowMultipleComponent]
    public sealed class VegetationRuntimeController : MonoBehaviour
    {
        internal sealed class InstanceEntry
        {
            public readonly VegetationPlacement Placement;
            public GameObject Instance;
            public HarvestableTreeRuntime Tree;
            public bool DesiredVisible;
            public bool DesiredGameplay;
            public InstanceEntry(VegetationPlacement placement) { Placement = placement; }
        }

        private static readonly ProfilerMarker RefreshMarker = new ProfilerMarker("Vegetation.StreamingRefresh");
        private static readonly ProfilerMarker ActivateMarker = new ProfilerMarker("Vegetation.ChunkActivate");
        private static readonly ProfilerMarker DeactivateMarker = new ProfilerMarker("Vegetation.ChunkDeactivate");
        private readonly List<VegetationChunkRuntime> chunks = new List<VegetationChunkRuntime>();
        private readonly List<VegetationChunkRuntime> pendingTransitions = new List<VegetationChunkRuntime>();
        private VegetationGenerationSettings settings;
        private VegetationInstancePool pool;
        private Transform target;
        private Vector3 lastTargetPosition;
        private Vector3 lastRefreshPosition;
        private float updateTimer;
        private int pendingCursor;
        private int totalPlacements, lodGroupTreeCount, missingLodTreeCount;
        private int activeTreeChunks, activeShrubChunks, activeGroundCoverChunks;
        private int visibleTrees, visibleShrubs, visibleGroundCover, activeHarvestableTrees, activeTreeColliders;
        private int chunkTransitionsLastRefresh;
        private float streamingRefreshMs;
        private VegetationRuntimeStats stats;

        public VegetationRuntimeStats Stats => stats;
        public IReadOnlyList<VegetationChunkRuntime> Chunks => chunks;
        public VegetationInstancePool Pool => pool;
        public int MaxActiveHarvestableTrees => settings != null ? settings.MaxActiveHarvestableTrees : 0;
        public int MaxActiveTreeColliders => settings != null ? settings.MaxActiveTreeColliders : 0;

        public void Initialize(IReadOnlyList<VegetationPlacement> placements, VegetationGenerationSettings generationSettings,
            Vector3 initialTargetPosition)
        {
            ClearRuntime();
            settings = generationSettings ?? new VegetationGenerationSettings();
            var poolObject = new GameObject("VegetationPoolRoot");
            poolObject.transform.SetParent(transform, false);
            pool = new VegetationInstancePool(poolObject.transform, settings.DecorativePoolCapacityPerSpecies);

            var chunkLookup = new Dictionary<long, VegetationChunkRuntime>();
            var missingLodSpecies = new HashSet<string>(StringComparer.Ordinal);
            if (placements != null)
            {
                for (int i = 0; i < placements.Count; i++)
                {
                    VegetationPlacement placement = placements[i];
                    if (placement.SpeciesAsset == null || placement.SpeciesAsset.VisualPrefab == null) continue;
                    long key = ChunkKey(placement.ChunkX, placement.ChunkZ);
                    if (!chunkLookup.TryGetValue(key, out VegetationChunkRuntime chunk))
                    {
                        var chunkObject = new GameObject($"Chunk_{placement.ChunkX}_{placement.ChunkZ}");
                        chunkObject.transform.SetParent(transform, false);
                        chunk = chunkObject.AddComponent<VegetationChunkRuntime>();
                        chunk.Initialize(placement.ChunkX, placement.ChunkZ, settings.ChunkSize);
                        chunkLookup.Add(key, chunk);
                        chunks.Add(chunk);
                    }

                    var entry = new InstanceEntry(placement);
                    chunk.Add(entry);
                    totalPlacements++;
                    if (IsTreeCategory(placement.Category))
                    {
                        if (placement.SpeciesAsset.VisualPrefab.GetComponentInChildren<LODGroup>(true) != null) lodGroupTreeCount++;
                        else if (missingLodSpecies.Add(placement.SpeciesId))
                        {
                            missingLodTreeCount++;
                            Debug.LogWarning($"Vegetation tree species '{placement.SpeciesId}' has no LODGroup on its assigned visual prefab.", placement.SpeciesAsset);
                        }
                    }

                    // Stateful trees stay allocated while streamed out so save/load and day regrowth
                    // can resolve every deterministic TreeId without object scans.
                    if (placement.SpeciesAsset.Harvestable && IsTreeCategory(placement.Category))
                        CreateInstance(chunk, entry, visible: false);
                }
            }

            chunks.Sort((a, b) => a.ChunkX != b.ChunkX ? a.ChunkX.CompareTo(b.ChunkX) : a.ChunkZ.CompareTo(b.ChunkZ));
            lastTargetPosition = initialTargetPosition;
            lastRefreshPosition = initialTargetPosition;
            RecalculateDesired(initialTargetPosition);
            ProcessPendingTransitions();
            RefreshStats(0f);
        }

        public void SetTarget(Transform player)
        {
            target = player;
            if (target == null) return;
            lastTargetPosition = target.position;
            RecalculateDesired(lastTargetPosition);
            ProcessPendingTransitions();
        }

        public void RefreshNow(Vector3 targetPosition)
        {
            lastTargetPosition = targetPosition;
            RecalculateDesired(targetPosition);
            ProcessPendingTransitions();
        }

        public void ClearRuntime()
        {
            pool?.Clear();
            chunks.Clear(); pendingTransitions.Clear(); pendingCursor = 0;
            totalPlacements = lodGroupTreeCount = missingLodTreeCount = 0;
            activeTreeChunks = activeShrubChunks = activeGroundCoverChunks = 0;
            visibleTrees = visibleShrubs = visibleGroundCover = activeHarvestableTrees = activeTreeColliders = 0;
            if (Application.isPlaying)
            {
                for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            }
            else
            {
                for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);
            }
            pool = null;
            stats = default;
        }

        private void Update()
        {
            updateTimer += Time.unscaledDeltaTime;
            if (updateTimer < (settings != null ? settings.StreamingUpdateInterval : 0.25f)) return;
            updateTimer = 0f;
            if (target != null) lastTargetPosition = target.position;
            Vector3 delta = lastTargetPosition - lastRefreshPosition;
            bool changedChunk = settings != null && settings.ChunkSize > 0f
                && (Mathf.FloorToInt(lastTargetPosition.x / settings.ChunkSize) != Mathf.FloorToInt(lastRefreshPosition.x / settings.ChunkSize)
                    || Mathf.FloorToInt(lastTargetPosition.z / settings.ChunkSize) != Mathf.FloorToInt(lastRefreshPosition.z / settings.ChunkSize));
            bool significantMove = delta.x * delta.x + delta.z * delta.z >= 4f;
            if (changedChunk || significantMove)
            {
                lastRefreshPosition = lastTargetPosition;
                RecalculateDesired(lastTargetPosition);
            }
            ProcessPendingTransitions();
        }

        private void RecalculateDesired(Vector3 position)
        {
            using (RefreshMarker.Auto())
            {
                float start = Time.realtimeSinceStartup;
                pendingTransitions.Clear(); pendingCursor = 0;
                for (int c = 0; c < chunks.Count; c++)
                    for (int e = 0; e < chunks[c].Entries.Count; e++)
                    { chunks[c].Entries[e].DesiredVisible = false; chunks[c].Entries[e].DesiredGameplay = false; }

                float treeRadius = settings.StreamingEnabled ? settings.TreeVisualRadius : float.MaxValue;
                float shrubRadius = settings.StreamingEnabled ? settings.ShrubVisualRadius : float.MaxValue;
                float coverRadius = settings.StreamingEnabled ? settings.GroundCoverVisualRadius : float.MaxValue;
                SelectVisible(position, VegetationCategory.Tree, treeRadius + settings.HysteresisDistance, settings.MaxVisibleTrees);
                SelectVisible(position, VegetationCategory.Shrub, shrubRadius + settings.HysteresisDistance, settings.MaxVisibleShrubs);
                SelectVisible(position, VegetationCategory.GroundCover, coverRadius + settings.HysteresisDistance, settings.MaxVisibleGroundCover);
                SelectGameplayTrees(position);

                for (int c = 0; c < chunks.Count; c++)
                    if (ChunkNeedsTransition(chunks[c])) pendingTransitions.Add(chunks[c]);
                chunkTransitionsLastRefresh = 0;
                streamingRefreshMs = Mathf.Max(0f, (Time.realtimeSinceStartup - start) * 1000f);
                RefreshStats(streamingRefreshMs);
            }
        }

        private void SelectVisible(Vector3 position, VegetationCategory category, float radius, int budget)
        {
            var candidates = new List<InstanceEntry>();
            for (int c = 0; c < chunks.Count; c++)
            {
                VegetationChunkRuntime chunk = chunks[c];
                float effectiveRadius = radius - settings.HysteresisDistance;
                if (chunk.CategoryRoot(CategoryIndex(category)) != null && chunk.CategoryRoot(CategoryIndex(category)).gameObject.activeSelf)
                    effectiveRadius += settings.HysteresisDistance;
                if (HorizontalDistanceToBounds(position, chunk.Bounds) > effectiveRadius) continue;
                for (int e = 0; e < chunk.Entries.Count; e++)
                {
                    InstanceEntry entry = chunk.Entries[e];
                    if (MatchesCategory(entry.Placement.Category, category)) candidates.Add(entry);
                }
            }
            candidates.Sort((a, b) => HorizontalDistanceSquared(position, a.Placement.Position).CompareTo(HorizontalDistanceSquared(position, b.Placement.Position)));
            int count = Mathf.Min(Mathf.Max(0, budget), candidates.Count);
            for (int i = 0; i < count; i++) candidates[i].DesiredVisible = true;
        }

        private void SelectGameplayTrees(Vector3 position)
        {
            var candidates = new List<InstanceEntry>();
            float radius = settings.TreeGameplayRadius;
            for (int c = 0; c < chunks.Count; c++)
            {
                VegetationChunkRuntime chunk = chunks[c];
                for (int e = 0; e < chunk.Entries.Count; e++)
                {
                    InstanceEntry entry = chunk.Entries[e];
                    if (entry.Tree == null) continue;
                    TreeLifecycleState state = entry.Tree.LifecycleState;
                    if (state != TreeLifecycleState.Standing && state != TreeLifecycleState.Falling) continue;
                    float effectiveRadius = entry.Tree.StreamingGameplayActive ? radius + settings.HysteresisDistance : radius;
                    if (HorizontalDistanceSquared(position, entry.Placement.Position) <= effectiveRadius * effectiveRadius)
                        candidates.Add(entry);
                }
            }
            candidates.Sort((a, b) =>
            {
                bool af = a.Tree.LifecycleState == TreeLifecycleState.Falling;
                bool bf = b.Tree.LifecycleState == TreeLifecycleState.Falling;
                if (af != bf) return af ? -1 : 1;
                return HorizontalDistanceSquared(position, a.Placement.Position).CompareTo(HorizontalDistanceSquared(position, b.Placement.Position));
            });

            int treeBudget = settings.MaxActiveHarvestableTrees;
            int colliderBudget = settings.MaxActiveTreeColliders;
            int active = 0, colliders = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                InstanceEntry entry = candidates[i];
                int cost = Mathf.Max(1, entry.Tree.StreamingColliderCount);
                bool fits = active < treeBudget && colliders + cost <= colliderBudget;
                if (fits) { entry.DesiredGameplay = true; active++; colliders += cost; }
                else if (entry.Tree.LifecycleState == TreeLifecycleState.Falling)
                    entry.Tree.PrepareForStreamingDeactivation();
            }
        }

        private bool ChunkNeedsTransition(VegetationChunkRuntime chunk)
        {
            bool[] rootsWanted = new bool[3];
            for (int i = 0; i < chunk.Entries.Count; i++)
            {
                InstanceEntry entry = chunk.Entries[i];
                if (entry.DesiredVisible) rootsWanted[CategoryIndex(entry.Placement.Category)] = true;
                if (entry.Instance != null && entry.Instance.activeSelf != entry.DesiredVisible) return true;
                if (entry.Tree != null && entry.Tree.StreamingGameplayActive != entry.DesiredGameplay) return true;
            }
            for (int i = 0; i < 3; i++)
            {
                Transform root = chunk.CategoryRoot(i);
                if (root != null && root.gameObject.activeSelf != rootsWanted[i]) return true;
            }
            return false;
        }

        private void ProcessPendingTransitions()
        {
            int processed = 0;
            int limit = settings != null ? settings.MaxChunkTransitionsPerUpdate : 4;
            while (pendingCursor < pendingTransitions.Count && processed < limit)
            {
                VegetationChunkRuntime chunk = pendingTransitions[pendingCursor++];
                if (chunk == null) continue;
                bool activating = HasDesiredVisible(chunk);
                using ((activating ? ActivateMarker : DeactivateMarker).Auto()) ApplyChunk(chunk);
                processed++;
                chunkTransitionsLastRefresh++;
            }
            if (pendingCursor >= pendingTransitions.Count) { pendingTransitions.Clear(); pendingCursor = 0; }
            RefreshStats(streamingRefreshMs);
        }

        private void ApplyChunk(VegetationChunkRuntime chunk)
        {
            bool[] rootsWanted = new bool[3];
            for (int i = 0; i < chunk.Entries.Count; i++)
            {
                InstanceEntry entry = chunk.Entries[i];
                if (entry.Tree != null) entry.Tree.SetStreamingGameplayActive(entry.DesiredGameplay);
                if (entry.DesiredVisible)
                {
                    rootsWanted[CategoryIndex(entry.Placement.Category)] = true;
                    if (entry.Instance == null) CreateInstance(chunk, entry, visible: true);
                    else
                    {
                        entry.Instance.transform.SetParent(chunk.GetRoot(entry.Placement.Category), false);
                        entry.Instance.transform.SetPositionAndRotation(entry.Placement.Position, Quaternion.Euler(0f, entry.Placement.Yaw, 0f));
                        entry.Instance.SetActive(true);
                    }
                }
                else if (entry.Instance != null)
                {
                    if (entry.Tree != null && entry.Tree.LifecycleState == TreeLifecycleState.Falling)
                        entry.Tree.PrepareForStreamingDeactivation();
                    if (pool != null && VegetationInstancePool.CanPoolPlacement(entry.Placement)
                        && pool.Release(entry.Placement, entry.Instance)) entry.Instance = null;
                    else entry.Instance.SetActive(false);
                }
            }
            for (int i = 0; i < 3; i++) chunk.SetCategoryRootActive(i, rootsWanted[i]);
        }

        private void CreateInstance(VegetationChunkRuntime chunk, InstanceEntry entry, bool visible)
        {
            VegetationPlacement placement = entry.Placement;
            Transform parent = chunk.GetRoot(placement.Category);
            if (!(pool != null && pool.TryAcquire(placement, parent, out entry.Instance)))
                entry.Instance = VegetationSpawner.CreateInstance(placement, parent);
            if (entry.Instance == null) return;
            if (entry.Tree == null) entry.Tree = entry.Instance.GetComponent<HarvestableTreeRuntime>();
            if (entry.Tree != null)
            {
                chunk.CacheTree(entry.Tree);
                entry.Tree.SetStreamingGameplayActive(entry.DesiredGameplay);
            }
            entry.Instance.SetActive(visible);
        }

        private void RefreshStats(float refreshMs)
        {
            activeTreeChunks = activeShrubChunks = activeGroundCoverChunks = 0;
            visibleTrees = visibleShrubs = visibleGroundCover = activeHarvestableTrees = activeTreeColliders = 0;
            for (int c = 0; c < chunks.Count; c++)
            {
                VegetationChunkRuntime chunk = chunks[c];
                if (chunk.CategoryRoot(0).gameObject.activeSelf) activeTreeChunks++;
                if (chunk.CategoryRoot(1).gameObject.activeSelf) activeShrubChunks++;
                if (chunk.CategoryRoot(2).gameObject.activeSelf) activeGroundCoverChunks++;
                for (int e = 0; e < chunk.Entries.Count; e++)
                {
                    InstanceEntry entry = chunk.Entries[e];
                    if (entry.Instance != null && entry.Instance.activeInHierarchy)
                    {
                        switch (entry.Placement.Category)
                        {
                            case VegetationCategory.Tree:
                            case VegetationCategory.DeadTree: visibleTrees++; break;
                            case VegetationCategory.Shrub: visibleShrubs++; break;
                            default: visibleGroundCover++; break;
                        }
                    }
                    if (entry.Tree != null && entry.Tree.StreamingGameplayActive)
                    {
                        activeHarvestableTrees++;
                        activeTreeColliders += entry.Tree.ActiveStreamingColliderCount;
                    }
                }
            }
            stats = new VegetationRuntimeStats(totalPlacements, chunks.Count, activeTreeChunks, activeShrubChunks,
                activeGroundCoverChunks, visibleTrees, visibleShrubs, visibleGroundCover,
                activeHarvestableTrees, activeTreeColliders, pool != null ? pool.PooledCount : 0,
                pool != null ? pool.PoolHits : 0, pool != null ? pool.PoolMisses : 0,
                lodGroupTreeCount, missingLodTreeCount, refreshMs, chunkTransitionsLastRefresh);
        }

        private static bool HasDesiredVisible(VegetationChunkRuntime chunk)
        { for (int i = 0; i < chunk.Entries.Count; i++) if (chunk.Entries[i].DesiredVisible) return true; return false; }
        private static long ChunkKey(int x, int z) => ((long)x << 32) ^ (uint)z;
        private static bool IsTreeCategory(VegetationCategory category) => category == VegetationCategory.Tree || category == VegetationCategory.DeadTree;
        private static bool MatchesCategory(VegetationCategory value, VegetationCategory requested) => requested == VegetationCategory.Tree
            ? IsTreeCategory(value) : requested == value;
        private static int CategoryIndex(VegetationCategory category) => IsTreeCategory(category) ? 0 : category == VegetationCategory.Shrub ? 1 : 2;
        private static float HorizontalDistanceSquared(Vector3 a, Vector3 b) { float x = a.x - b.x, z = a.z - b.z; return x * x + z * z; }
        private static float HorizontalDistanceToBounds(Vector3 point, Bounds bounds)
        {
            float dx = Mathf.Max(bounds.min.x - point.x, 0f, point.x - bounds.max.x);
            float dz = Mathf.Max(bounds.min.z - point.z, 0f, point.z - bounds.max.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
