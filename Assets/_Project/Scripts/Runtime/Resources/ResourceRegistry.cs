using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.Resources
{
    /// <summary>
    /// Runtime registry for resource nodes.
    ///
    /// It intentionally keeps disabled/depleted resource nodes registered so save/load can
    /// persist depleted state without doing FindObjectsByType(...Include). Destroyed nodes
    /// are removed on cleanup.
    /// </summary>
    public static class ResourceRegistry
    {
        private static readonly List<ResourceNodeView> resources = new List<ResourceNodeView>();

        public static IReadOnlyList<ResourceNodeView> Resources
        {
            get
            {
                Cleanup();
                return resources;
            }
        }

        public static int ResourceCount
        {
            get
            {
                Cleanup();
                return resources.Count;
            }
        }

        public static int ActiveResourceCount
        {
            get
            {
                Cleanup();
                int count = 0;
                for (int i = 0; i < resources.Count; i++)
                {
                    ResourceNodeView node = resources[i];
                    if (node != null && node.gameObject != null && node.gameObject.activeInHierarchy)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public static void Register(ResourceNodeView node)
        {
            if (node == null || resources.Contains(node))
            {
                return;
            }

            resources.Add(node);
        }

        public static void Unregister(ResourceNodeView node)
        {
            if (node == null)
            {
                return;
            }

            resources.Remove(node);
        }

        public static void Cleanup()
        {
            for (int i = resources.Count - 1; i >= 0; i--)
            {
                if (resources[i] == null)
                {
                    resources.RemoveAt(i);
                }
            }
        }

        public static void ClearForTests()
        {
            resources.Clear();
        }
    }
}
