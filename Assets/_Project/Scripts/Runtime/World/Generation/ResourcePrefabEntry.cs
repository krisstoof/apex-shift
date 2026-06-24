using System;
using ApexShift.Runtime.World.Biomes;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    [Serializable]
    public sealed class ResourcePrefabEntry
    {
        [SerializeField] private VegetationSpawnKind kind;
        [SerializeField] private GameObject prefab;

        public VegetationSpawnKind Kind => kind;
        public GameObject Prefab => prefab;
    }
}
