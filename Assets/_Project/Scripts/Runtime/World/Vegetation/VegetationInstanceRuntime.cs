using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    [DisallowMultipleComponent]
    public sealed class VegetationInstanceRuntime : MonoBehaviour
    {
        [SerializeField] private string instanceId = string.Empty;
        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private string biomeId = string.Empty;
        [SerializeField] private int chunkX;
        [SerializeField] private int chunkZ;
        [SerializeField] private bool harvestableMetadata;
        [SerializeField] private string resourceKind = string.Empty;

        public string InstanceId => instanceId;
        public string SpeciesId => speciesId;
        public string BiomeId => biomeId;
        public int ChunkX => chunkX;
        public int ChunkZ => chunkZ;
        public bool HarvestableMetadata => harvestableMetadata;
        public string ResourceKind => resourceKind;

        public void Configure(in VegetationPlacement placement)
        {
            instanceId = placement.InstanceId;
            speciesId = placement.SpeciesId;
            biomeId = placement.BiomeId;
            chunkX = placement.ChunkX;
            chunkZ = placement.ChunkZ;
            harvestableMetadata = placement.SpeciesAsset != null && placement.SpeciesAsset.Harvestable;
            resourceKind = placement.SpeciesAsset != null ? placement.SpeciesAsset.ResourceKind : string.Empty;
        }
    }
}
