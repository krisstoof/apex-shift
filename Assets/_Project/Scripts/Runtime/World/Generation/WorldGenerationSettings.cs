using System;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Vegetation;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    [Serializable]
    public sealed class WorldGenerationSettings
    {
        [SerializeField] private float regionSize = 40f;
        [SerializeField] private float padding = 5f;
        [SerializeField] private TerrainHeightfieldSettings terrain = new TerrainHeightfieldSettings();
        [SerializeField] private BiomeFieldSettings biome = new BiomeFieldSettings();
        [SerializeField] private VegetationGenerationSettings vegetation = new VegetationGenerationSettings();

        public float RegionSize => regionSize;
        public float Padding => padding;
        public TerrainHeightfieldSettings Terrain => terrain ?? (terrain = new TerrainHeightfieldSettings());
        public BiomeFieldSettings Biome => biome ?? (biome = new BiomeFieldSettings());
        public VegetationGenerationSettings Vegetation => vegetation ?? (vegetation = new VegetationGenerationSettings());
    }
}
