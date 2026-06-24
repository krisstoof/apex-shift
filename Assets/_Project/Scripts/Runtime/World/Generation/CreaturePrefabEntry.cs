using System;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    [Serializable]
    public sealed class CreaturePrefabEntry
    {
        [SerializeField] private string creatureId = string.Empty;
        [SerializeField] private GameObject prefab;

        public string CreatureId => creatureId ?? string.Empty;
        public GameObject Prefab => prefab;
    }
}
