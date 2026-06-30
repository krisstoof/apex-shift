using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    [CreateAssetMenu(fileName = "PlaceableDefinition", menuName = "Apex Shift/Buildings/Placeable Definition", order = 70)]
    public sealed class PlaceableDefinition : ScriptableObject
    {
        [Serializable]
        public sealed class PlaceableBuildCost
        {
            [SerializeField] private string itemId = string.Empty;
            [SerializeField] private int amount = 1;

            public string ItemId => string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
            public int Amount => Mathf.Max(1, amount);

            public static PlaceableBuildCost Create(string id, int count)
            {
                PlaceableBuildCost cost = new PlaceableBuildCost
                {
                    itemId = id,
                    amount = count
                };

                return cost;
            }
        }

        [SerializeField] private string buildingId = "storage_box";
        [SerializeField] private string itemId = "storage_box";
        [SerializeField] private string displayName = "Storage Box";
        [SerializeField] private GameObject prefab;
        [SerializeField] private Vector3 footprintSize = new Vector3(2f, 1.5f, 2f);
        [SerializeField] private float minDistanceFromPlayer = 1.35f;
        [SerializeField] private List<PlaceableBuildCost> materialCosts = new List<PlaceableBuildCost>();
        [SerializeField] private bool blocksPlacement = true;
        [SerializeField] private bool rejectWater = true;
        [SerializeField] private bool requireWorldBounds = true;
        [SerializeField] private bool requireNavMeshSample;

        public string BuildingId => Normalize(buildingId, itemId);
        public string ItemId => BuildingId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? BuildingId : displayName.Trim();
        public GameObject Prefab => prefab;
        public Vector3 FootprintSize => new Vector3(Mathf.Max(0.25f, footprintSize.x), Mathf.Max(0.25f, footprintSize.y), Mathf.Max(0.25f, footprintSize.z));
        public float MinDistanceFromPlayer => Mathf.Max(0f, minDistanceFromPlayer);
        public int ConsumeAmount => 0;
        public IReadOnlyList<PlaceableBuildCost> MaterialCosts => materialCosts;
        public string BuildCostText => materialCosts == null || materialCosts.Count == 0
            ? "free"
            : string.Join(", ", materialCosts.Where(cost => cost != null && !string.IsNullOrWhiteSpace(cost.ItemId)).Select(cost => $"{cost.ItemId} x{cost.Amount}"));
        public bool BlocksPlacement => blocksPlacement;
        public bool RejectWater => rejectWater;
        public bool RequireWorldBounds => requireWorldBounds;
        public bool RequireNavMeshSample => requireNavMeshSample;

        public bool MatchesBuilding(string candidateBuildingId)
        {
            return !string.IsNullOrWhiteSpace(candidateBuildingId) && string.Equals(BuildingId, candidateBuildingId.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public bool MatchesItem(string candidateItemId) => MatchesBuilding(candidateItemId);

        public static PlaceableDefinition CreateRuntime(string buildingId, string displayName, Vector3 footprintSize, IEnumerable<PlaceableBuildCost> buildCosts)
        {
            PlaceableDefinition definition = CreateInstance<PlaceableDefinition>();
            definition.buildingId = Normalize(buildingId, buildingId);
            definition.itemId = definition.buildingId;
            definition.displayName = string.IsNullOrWhiteSpace(displayName) ? definition.buildingId : displayName.Trim();
            definition.footprintSize = footprintSize;
            definition.materialCosts = buildCosts != null ? buildCosts.Where(cost => cost != null).ToList() : new List<PlaceableBuildCost>();
            definition.name = $"RuntimePlaceable_{definition.buildingId}";
            return definition;
        }

        private static string Normalize(string value, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            return string.IsNullOrWhiteSpace(fallback) ? "unknown" : fallback.Trim();
        }
    }
}
