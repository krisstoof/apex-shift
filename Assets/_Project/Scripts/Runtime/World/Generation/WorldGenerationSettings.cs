using System;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    [Serializable]
    public sealed class WorldGenerationSettings
    {
        [SerializeField] private float regionSize = 40f;
        [SerializeField] private float padding = 5f;

        public float RegionSize => regionSize;
        public float Padding => padding;
    }
}
