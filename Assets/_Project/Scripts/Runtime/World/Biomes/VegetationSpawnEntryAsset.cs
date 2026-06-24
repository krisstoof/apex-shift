using System;
using ApexShift.Core.World.Biomes;
using UnityEngine;

namespace ApexShift.Runtime.World.Biomes
{
    [Serializable]
    public sealed class VegetationSpawnEntryAsset
    {
        [SerializeField] private VegetationSpawnKind kind;
        [SerializeField] private int count;
        [SerializeField] private float weight = 1f;
        [SerializeField] private float minScale = 0.5f;
        [SerializeField] private float maxScale = 1f;
        [SerializeField] private string resourceKind = string.Empty;
        [SerializeField] private bool harvestable;

        public VegetationSpawnEntryAsset()
        {
        }

        public VegetationSpawnEntryAsset(
            VegetationSpawnKind kind,
            int count,
            float weight,
            float minScale,
            float maxScale,
            string resourceKind,
            bool harvestable)
        {
            this.kind = kind;
            this.count = count;
            this.weight = weight;
            this.minScale = minScale;
            this.maxScale = maxScale;
            this.resourceKind = resourceKind;
            this.harvestable = harvestable;
        }

        public VegetationSpawnKind Kind => kind;
        public string RoleId => ToRoleId(kind);
        public int Count => Mathf.Max(0, count);
        public float Weight => Mathf.Max(0f, weight);
        public float MinScale => Mathf.Max(0.01f, minScale);
        public float MaxScale => Mathf.Max(MinScale, maxScale);
        public string ResourceKind => resourceKind ?? string.Empty;
        public bool Harvestable => harvestable;

        public VegetationSpawnEntry ToCore()
        {
            return new VegetationSpawnEntry(RoleId, Count, Weight, MinScale, MaxScale, ResourceKind, Harvestable);
        }

        public static string ToRoleId(VegetationSpawnKind kind)
        {
            switch (kind)
            {
                case VegetationSpawnKind.ConiferTree:
                    return "conifer_tree";
                case VegetationSpawnKind.LeafyTree:
                    return "leafy_tree";
                case VegetationSpawnKind.DryTree:
                    return "dry_tree";
                case VegetationSpawnKind.Rock:
                    return "rock";
                case VegetationSpawnKind.GreenBush:
                    return "green_bush";
                case VegetationSpawnKind.DryBush:
                    return "dry_bush";
                case VegetationSpawnKind.GrassOrFlower:
                    return "grass_or_flower";
                case VegetationSpawnKind.BerryBush:
                    return "berry_bush";
                default:
                    return kind.ToString().ToLowerInvariant();
            }
        }
    }
}
