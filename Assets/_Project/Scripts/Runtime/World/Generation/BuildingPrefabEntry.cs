using System;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    [Serializable]
    public sealed class BuildingPrefabEntry
    {
        [SerializeField] private string buildingId = string.Empty;
        [SerializeField] private GameObject prefab;

        public string BuildingId => buildingId ?? string.Empty;
        public GameObject Prefab => prefab;

        public bool Matches(string id)
        {
            return !string.IsNullOrWhiteSpace(id)
                   && string.Equals(BuildingId.Trim(), id.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
