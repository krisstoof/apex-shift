using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    public readonly struct VegetationPlacement
    {
        public readonly string InstanceId;
        public readonly string HabitatId;
        public readonly string SpeciesId;
        public readonly Vector3 Position;
        public readonly float Yaw;
        public readonly float Scale;
        public readonly int ChunkX;
        public readonly int ChunkZ;
        public readonly float MinimumSpacing;
        public readonly VegetationCategory Category;
        public readonly VegetationCollisionMode CollisionMode;
        public readonly VegetationSpeciesAsset SpeciesAsset;

        public VegetationPlacement(string instanceId, string habitatId, VegetationSpeciesAsset species,
            Vector3 position, float yaw, float scale, int chunkX, int chunkZ)
        {
            InstanceId = instanceId;
            HabitatId = habitatId;
            SpeciesAsset = species;
            SpeciesId = species != null ? species.SpeciesId : string.Empty;
            Position = position;
            Yaw = yaw;
            Scale = scale;
            ChunkX = chunkX;
            ChunkZ = chunkZ;
            MinimumSpacing = species != null ? species.MinimumSpacing : 0f;
            Category = species != null ? species.Category : VegetationCategory.GroundCover;
            CollisionMode = species != null ? species.CollisionMode : VegetationCollisionMode.None;
        }
    }
}
