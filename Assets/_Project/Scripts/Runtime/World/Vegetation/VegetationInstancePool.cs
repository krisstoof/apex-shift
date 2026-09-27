using System.Collections.Generic;
using ApexShift.Runtime.Resources;
using Unity.Profiling;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    public sealed class VegetationInstancePool
    {
        private static readonly ProfilerMarker AcquireMarker = new ProfilerMarker("Vegetation.PoolAcquire");
        private static readonly ProfilerMarker ReleaseMarker = new ProfilerMarker("Vegetation.PoolRelease");
        private readonly Dictionary<string, Stack<GameObject>> pools = new Dictionary<string, Stack<GameObject>>(System.StringComparer.Ordinal);
        private readonly Transform poolRoot;
        private readonly int capacityPerSpecies;
        private int poolHits, poolMisses, pooledCount;

        public int PoolHits => poolHits;
        public int PoolMisses => poolMisses;
        public int PooledCount => pooledCount;

        public VegetationInstancePool(Transform root, int capacity)
        {
            poolRoot = root;
            capacityPerSpecies = Mathf.Max(0, capacity);
        }

        public bool TryAcquire(VegetationPlacement placement, Transform parent, out GameObject instance)
        {
            using (AcquireMarker.Auto())
            {
                instance = null;
                if (placement.SpeciesAsset == null || !CanPoolPlacement(placement)) return false;

                if (pools.TryGetValue(placement.SpeciesId, out Stack<GameObject> stack))
                {
                    while (stack.Count > 0 && instance == null) { instance = stack.Pop(); pooledCount--; }
                }
                if (instance == null)
                {
                    poolMisses++;
                    return false;
                }

                poolHits++;
                instance.transform.SetParent(parent, false);
                instance.transform.SetPositionAndRotation(placement.Position, Quaternion.Euler(0f, placement.Yaw, 0f));
                instance.transform.localScale = Vector3.Scale(placement.SpeciesAsset.VisualPrefab.transform.localScale, Vector3.one * placement.Scale);
                VegetationSpawner.ConfigureExistingInstance(instance, placement);
                instance.SetActive(true);
                return true;
            }
        }

        public bool Release(VegetationPlacement placement, GameObject instance)
        {
            using (ReleaseMarker.Auto())
            {
                if (instance == null || placement.SpeciesAsset == null || !CanPoolInstance(instance, placement)) return false;
                if (capacityPerSpecies <= 0) { DestroyInstance(instance); return true; }
                if (!pools.TryGetValue(placement.SpeciesId, out Stack<GameObject> stack))
                    pools.Add(placement.SpeciesId, stack = new Stack<GameObject>());
                if (stack.Count >= capacityPerSpecies) { DestroyInstance(instance); return true; }
                instance.SetActive(false);
                instance.transform.SetParent(poolRoot, false);
                stack.Push(instance);
                pooledCount++;
                return true;
            }
        }

        public void Clear()
        {
            foreach (Stack<GameObject> stack in pools.Values)
                while (stack.Count > 0) DestroyInstance(stack.Pop());
            pools.Clear();
            pooledCount = 0;
        }

        public static bool CanPoolPlacement(VegetationPlacement placement)
        {
            if (placement.Category == VegetationCategory.GroundCover) return true;
            if (placement.Category == VegetationCategory.Shrub) return true;
            return !placement.SpeciesAsset.Harvestable;
        }

        private static bool CanPoolInstance(GameObject instance, VegetationPlacement placement)
        {
            if (!CanPoolPlacement(placement)) return false;
            if (instance.GetComponentInChildren<HarvestableTreeRuntime>(true) != null
                || instance.GetComponentInChildren<ResourceNodeView>(true) != null)
            {
                Debug.LogWarning($"Decorative vegetation species '{placement.SpeciesId}' unexpectedly owns gameplay components and will not enter the pool.", instance);
                return false;
            }
            return true;
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (instance == null) return;
            if (Application.isPlaying) Object.Destroy(instance);
            else Object.DestroyImmediate(instance);
        }
    }
}
