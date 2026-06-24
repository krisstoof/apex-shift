using System;
using System.Collections.Generic;
using ApexShift.Core.World.Biomes;
using UnityEngine;

namespace ApexShift.Runtime.World.Biomes
{
    [CreateAssetMenu(menuName = "Apex Shift/World/Biome Catalog", fileName = "BiomeCatalog")]
    public sealed class BiomeCatalogAsset : ScriptableObject
    {
        [SerializeField] private List<BiomeDefinitionAsset> biomes = new List<BiomeDefinitionAsset>();

        private Dictionary<string, BiomeDefinitionAsset> _lookup;

        public IReadOnlyList<BiomeDefinitionAsset> Biomes => biomes;

        public BiomeDefinitionAsset GetBiome(string id)
        {
            EnsureLookup();
            string normalized = global::ApexShift.Core.World.Biomes.BiomeId.NormalizeId(id);
            return _lookup.TryGetValue(normalized, out var biome) ? biome : null;
        }

        public BiomeDefinitionAsset GetStarterBiome()
        {
            foreach (var biome in biomes)
            {
                if (biome != null && biome.StarterBiome)
                {
                    return biome;
                }
            }

            return biomes.Count > 0 ? biomes[0] : null;
        }

        private void EnsureLookup()
        {
            if (_lookup != null && _lookup.Count == biomes.Count) return;

            _lookup = new Dictionary<string, BiomeDefinitionAsset>(StringComparer.OrdinalIgnoreCase);
            foreach (var biome in biomes)
            {
                if (biome == null) continue;
                string id = global::ApexShift.Core.World.Biomes.BiomeId.NormalizeId(biome.BiomeId);
                _lookup[id] = biome;
            }
        }

        public void SetBiomes(IEnumerable<BiomeDefinitionAsset> biomeAssets)
        {
            biomes = new List<BiomeDefinitionAsset>(biomeAssets);
            _lookup = null;
        }
    }
}
