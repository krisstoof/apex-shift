using System.Collections.Generic;
using ApexShift.Core.World.Biomes;
using UnityEngine;

namespace ApexShift.Runtime.World.Biomes
{
    [CreateAssetMenu(menuName = "Apex Shift/World/Biome Definition", fileName = "BiomeDefinition")]
    public sealed class BiomeDefinitionAsset : ScriptableObject
    {
        [SerializeField] private string biomeId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private Color groundColor = Color.green;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private bool starterBiome;
        [SerializeField] private List<VegetationSpawnEntryAsset> vegetation = new List<VegetationSpawnEntryAsset>();
        [SerializeField] private List<CreatureSpawnEntryAsset> creatures = new List<CreatureSpawnEntryAsset>();

        public string BiomeId => biomeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? biomeId : displayName;
        public Color GroundColor => groundColor;
        public Material GroundMaterial => groundMaterial;
        public bool StarterBiome => starterBiome;
        public IReadOnlyList<VegetationSpawnEntryAsset> Vegetation => vegetation;
        public IReadOnlyList<CreatureSpawnEntryAsset> Creatures => creatures;

        public void Configure(
            string id,
            string displayNameValue,
            Color color,
            bool isStarterBiome,
            IEnumerable<VegetationSpawnEntryAsset> vegetationEntries,
            IEnumerable<CreatureSpawnEntryAsset> creatureEntries = null,
            Material material = null)
        {
            biomeId = global::ApexShift.Core.World.Biomes.BiomeId.NormalizeId(id);
            displayName = displayNameValue;
            groundColor = color;
            starterBiome = isStarterBiome;
            groundMaterial = material;
            vegetation = new List<VegetationSpawnEntryAsset>(vegetationEntries ?? new List<VegetationSpawnEntryAsset>());
            creatures = new List<CreatureSpawnEntryAsset>(creatureEntries ?? new List<CreatureSpawnEntryAsset>());
        }

        public BiomeDefinition ToCoreDefinition()
        {
            List<VegetationSpawnEntry> vegetationEntries = new List<VegetationSpawnEntry>();
            foreach (VegetationSpawnEntryAsset entry in vegetation)
            {
                if (entry != null)
                {
                    vegetationEntries.Add(entry.ToCore());
                }
            }

            List<CreatureSpawnEntry> creatureEntries = new List<CreatureSpawnEntry>();
            foreach (CreatureSpawnEntryAsset entry in creatures)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.CreatureId))
                {
                    creatureEntries.Add(entry.ToCore());
                }
            }

            return new BiomeDefinition(
                new global::ApexShift.Core.World.Biomes.BiomeId(biomeId),
                DisplayName,
                new BiomeSpawnProfile(vegetationEntries, creatureEntries),
                starterBiome);
        }

        public int GetVegetationCount(string roleId)
        {
            string normalized = global::ApexShift.Core.World.Biomes.BiomeId.NormalizeId(roleId);
            int total = 0;

            foreach (VegetationSpawnEntryAsset entry in vegetation)
            {
                if (entry != null && global::ApexShift.Core.World.Biomes.BiomeId.NormalizeId(entry.RoleId) == normalized)
                {
                    total += entry.Count;
                }
            }

            return total;
        }

        public float GetMinScale(string roleId, float fallback)
        {
            string normalized = global::ApexShift.Core.World.Biomes.BiomeId.NormalizeId(roleId);
            foreach (VegetationSpawnEntryAsset entry in vegetation)
            {
                if (entry != null && global::ApexShift.Core.World.Biomes.BiomeId.NormalizeId(entry.RoleId) == normalized)
                {
                    return entry.MinScale;
                }
            }

            return fallback;
        }

        public float GetMaxScale(string roleId, float fallback)
        {
            string normalized = global::ApexShift.Core.World.Biomes.BiomeId.NormalizeId(roleId);
            foreach (VegetationSpawnEntryAsset entry in vegetation)
            {
                if (entry != null && global::ApexShift.Core.World.Biomes.BiomeId.NormalizeId(entry.RoleId) == normalized)
                {
                    return entry.MaxScale;
                }
            }

            return fallback;
        }
    }
}
