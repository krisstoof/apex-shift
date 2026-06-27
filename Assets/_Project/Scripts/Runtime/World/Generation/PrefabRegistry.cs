using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.World.Biomes;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    [CreateAssetMenu(
        fileName = "PrefabRegistry",
        menuName = "Apex Shift/World/Prefab Registry",
        order = 30)]
    public sealed class PrefabRegistry : ScriptableObject
    {
        [Header("Resources")]
        [SerializeField] private List<ResourcePrefabEntry> resourcePrefabs = new List<ResourcePrefabEntry>();

        [Header("Creatures")]
        [SerializeField] private List<CreaturePrefabEntry> creaturePrefabs = new List<CreaturePrefabEntry>();

        [Header("Buildings")]
        [SerializeField] private List<BuildingPrefabEntry> buildingPrefabs = new List<BuildingPrefabEntry>();

        public IReadOnlyList<ResourcePrefabEntry> ResourcePrefabs => resourcePrefabs;
        public IReadOnlyList<CreaturePrefabEntry> CreaturePrefabs => creaturePrefabs;
        public IReadOnlyList<BuildingPrefabEntry> BuildingPrefabs => buildingPrefabs;

        public bool TryGetResourcePrefab(VegetationSpawnKind kind, out GameObject prefab)
        {
            prefab = null;
            List<GameObject> matches = resourcePrefabs
                .Where(entry => entry != null && entry.Prefab != null && entry.Kind == kind)
                .Select(entry => entry.Prefab)
                .ToList();
            if (matches.Count == 0) return false;
            prefab = matches[UnityEngine.Random.Range(0, matches.Count)];
            return prefab != null;
        }

        public bool TryGetCreaturePrefab(string creatureId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrWhiteSpace(creatureId)) return false;

            string normalized = creatureId.Trim();
            List<GameObject> matches = creaturePrefabs
                .Where(entry => entry != null
                                && entry.Prefab != null
                                && string.Equals(entry.CreatureId.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.Prefab)
                .ToList();
            if (matches.Count == 0) return false;
            prefab = matches[UnityEngine.Random.Range(0, matches.Count)];
            return prefab != null;
        }

        public bool TryGetBuildingPrefab(string buildingId, out GameObject prefab)
        {
            prefab = null;
            if (string.IsNullOrWhiteSpace(buildingId)) return false;

            string normalized = buildingId.Trim();
            List<GameObject> matches = buildingPrefabs
                .Where(entry => entry != null
                                && entry.Prefab != null
                                && string.Equals(entry.BuildingId.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                .Select(entry => entry.Prefab)
                .ToList();
            if (matches.Count == 0) return false;
            prefab = matches[UnityEngine.Random.Range(0, matches.Count)];
            return prefab != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            WarnAboutDuplicateCreatureIds();
            WarnAboutDuplicateBuildingIds();
        }

        private void WarnAboutDuplicateCreatureIds()
        {
            foreach (IGrouping<string, CreaturePrefabEntry> group in creaturePrefabs
                         .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.CreatureId))
                         .GroupBy(entry => entry.CreatureId.Trim().ToLowerInvariant()))
            {
                if (group.Count() > 1)
                {
                    Debug.LogWarning($"PrefabRegistry '{name}' has {group.Count()} creature prefab entries for id '{group.Key}'. This is allowed for variants but should be intentional.", this);
                }
            }
        }

        private void WarnAboutDuplicateBuildingIds()
        {
            foreach (IGrouping<string, BuildingPrefabEntry> group in buildingPrefabs
                         .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.BuildingId))
                         .GroupBy(entry => entry.BuildingId.Trim().ToLowerInvariant()))
            {
                if (group.Count() > 1)
                {
                    Debug.LogWarning($"PrefabRegistry '{name}' has {group.Count()} building prefab entries for id '{group.Key}'. This is allowed for variants but should be intentional.", this);
                }
            }
        }
#endif
    }
}
