using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    [DisallowMultipleComponent]
    public sealed class VegetationChunkRuntime : MonoBehaviour
    {
        private readonly List<VegetationRuntimeController.InstanceEntry> entries = new List<VegetationRuntimeController.InstanceEntry>();
        private readonly List<HarvestableTreeRuntime> harvestableTrees = new List<HarvestableTreeRuntime>();
        private Transform treesRoot, shrubsRoot, groundCoverRoot;

        public int ChunkX { get; private set; }
        public int ChunkZ { get; private set; }
        public Bounds Bounds { get; private set; }
        public int TreeCount { get; private set; }
        public int ShrubCount { get; private set; }
        public int GroundCoverCount { get; private set; }
        public IReadOnlyList<HarvestableTreeRuntime> HarvestableTrees => harvestableTrees;
        public bool HasActiveGameplayTree
        {
            get { for (int i = 0; i < harvestableTrees.Count; i++) if (harvestableTrees[i] != null && harvestableTrees[i].StreamingGameplayActive) return true; return false; }
        }
        internal List<VegetationRuntimeController.InstanceEntry> Entries => entries;

        internal void Initialize(int x, int z, float size)
        {
            ChunkX = x; ChunkZ = z;
            float minX = x * size, minZ = z * size;
            Bounds = new Bounds(new Vector3(minX + size * 0.5f, 0f, minZ + size * 0.5f), new Vector3(size, 10000f, size));
            treesRoot = CreateCategoryRoot("Trees");
            shrubsRoot = CreateCategoryRoot("Shrubs");
            groundCoverRoot = CreateCategoryRoot("GroundCover");
            treesRoot.gameObject.SetActive(false);
            shrubsRoot.gameObject.SetActive(false);
            groundCoverRoot.gameObject.SetActive(false);
        }

        internal Transform GetRoot(VegetationCategory category)
        {
            switch (category)
            {
                case VegetationCategory.Tree:
                case VegetationCategory.DeadTree: return treesRoot;
                case VegetationCategory.Shrub: return shrubsRoot;
                default: return groundCoverRoot;
            }
        }

        internal void Add(VegetationRuntimeController.InstanceEntry entry)
        {
            entries.Add(entry);
            switch (entry.Placement.Category)
            {
                case VegetationCategory.Tree:
                case VegetationCategory.DeadTree: TreeCount++; break;
                case VegetationCategory.Shrub: ShrubCount++; break;
                default: GroundCoverCount++; break;
            }
        }

        internal void CacheTree(HarvestableTreeRuntime tree)
        {
            if (tree != null && !harvestableTrees.Contains(tree)) harvestableTrees.Add(tree);
        }

        internal Transform CategoryRoot(int index) => index == 0 ? treesRoot : index == 1 ? shrubsRoot : groundCoverRoot;
        internal void SetCategoryRootActive(int index, bool active)
        {
            Transform root = CategoryRoot(index);
            if (root != null && root.gameObject.activeSelf != active) root.gameObject.SetActive(active);
        }

        private Transform CreateCategoryRoot(string rootName)
        {
            var root = new GameObject(rootName);
            root.transform.SetParent(transform, false);
            return root.transform;
        }
    }
}
