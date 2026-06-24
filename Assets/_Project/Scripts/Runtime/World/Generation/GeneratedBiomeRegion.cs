using ApexShift.Runtime.World.Biomes;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    public sealed class GeneratedBiomeRegion
    {
        public BiomeDefinitionAsset Biome { get; }
        public Bounds Bounds { get; }

        public GeneratedBiomeRegion(BiomeDefinitionAsset biome, Bounds bounds)
        {
            Biome = biome;
            Bounds = bounds;
        }
    }
}
