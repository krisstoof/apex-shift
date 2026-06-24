using System;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    [Serializable]
    public sealed class CreaturePrefabEntry
    {
        [SerializeField] private string creatureId;
        [SerializeField] private GameObject prefab;

        public string CreatureId => creatureId;
        public GameObject Prefab => prefab;
    }
}
