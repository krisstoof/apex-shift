using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    /// <summary>Tracks harvestable trees by their deterministic vegetation instance ID.</summary>
    public static class HarvestableTreeRegistry
    {
        private static readonly Dictionary<string, HarvestableTreeRuntime> trees =
            new Dictionary<string, HarvestableTreeRuntime>(StringComparer.Ordinal);

        public static IReadOnlyDictionary<string, HarvestableTreeRuntime> Trees
        {
            get
            {
                CleanupDestroyed();
                return trees;
            }
        }

        public static int Count
        {
            get
            {
                CleanupDestroyed();
                return trees.Count;
            }
        }

        public static void Register(HarvestableTreeRuntime tree)
        {
            if (tree == null || string.IsNullOrWhiteSpace(tree.TreeId)) return;
            CleanupDestroyed();
            if (trees.TryGetValue(tree.TreeId, out HarvestableTreeRuntime existing))
            {
                if (existing == tree) return;
                Debug.LogError($"Duplicate harvestable tree ID '{tree.TreeId}'. Existing tree remains registered.", tree);
                return;
            }
            trees.Add(tree.TreeId, tree);
        }

        public static void Unregister(HarvestableTreeRuntime tree)
        {
            if (tree == null || string.IsNullOrWhiteSpace(tree.TreeId)) return;
            if (trees.TryGetValue(tree.TreeId, out HarvestableTreeRuntime existing) && existing == tree)
                trees.Remove(tree.TreeId);
        }

        public static bool TryGet(string treeId, out HarvestableTreeRuntime tree)
        {
            CleanupDestroyed();
            return trees.TryGetValue(treeId ?? string.Empty, out tree);
        }

        public static void CleanupDestroyed()
        {
            if (trees.Count == 0) return;
            var removed = ListPool<string>.Get();
            foreach (KeyValuePair<string, HarvestableTreeRuntime> pair in trees)
                if (pair.Value == null) removed.Add(pair.Key);
            for (int i = 0; i < removed.Count; i++) trees.Remove(removed[i]);
            ListPool<string>.Release(removed);
        }

        public static void ClearForTests() => trees.Clear();

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> pool = new Stack<List<T>>();
            public static List<T> Get() => pool.Count > 0 ? pool.Pop() : new List<T>();
            public static void Release(List<T> list) { list.Clear(); pool.Push(list); }
        }
    }
}
