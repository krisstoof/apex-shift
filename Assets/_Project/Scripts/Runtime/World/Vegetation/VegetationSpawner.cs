using System.Collections.Generic;
using ApexShift.Runtime.World.Generation;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    /// <summary>Instantiates planned vegetation under spatial chunk roots owned by one generation.</summary>
    public sealed class VegetationSpawner
    {
        private readonly Dictionary<long, Transform> chunkRoots = new Dictionary<long, Transform>();

        public int Spawn(IReadOnlyList<VegetationPlacement> placements, Transform vegetationRoot, WorldGenerationResult result)
        {
            if (placements == null || vegetationRoot == null) return 0;
            chunkRoots.Clear();
            int spawned = 0;
            for (int i = 0; i < placements.Count; i++)
            {
                VegetationPlacement placement = placements[i];
                VegetationSpeciesAsset species = placement.SpeciesAsset;
                if (species == null || species.VisualPrefab == null) continue;
                Transform parent = GetChunkRoot(vegetationRoot, placement.ChunkX, placement.ChunkZ);
                GameObject instance = CreateInstance(placement, parent);
                result?.RecordVegetation(placement.BiomeId, placement.SpeciesId, placement.Category);
                if (instance != null) spawned++;
            }
            return spawned;
        }

        public static GameObject CreateInstance(VegetationPlacement placement, Transform parent)
        {
            if (placement.SpeciesAsset == null || placement.SpeciesAsset.VisualPrefab == null || parent == null) return null;
            GameObject instance = Object.Instantiate(placement.SpeciesAsset.VisualPrefab, placement.Position,
                Quaternion.Euler(0f, placement.Yaw, 0f), parent);
            ConfigureExistingInstance(instance, placement);
            return instance;
        }

        public static void ConfigureExistingInstance(GameObject instance, VegetationPlacement placement)
        {
            if (instance == null || placement.SpeciesAsset == null) return;
            instance.name = placement.SpeciesId + "_" + placement.InstanceId;
            instance.transform.localScale = Vector3.Scale(placement.SpeciesAsset.VisualPrefab.transform.localScale, Vector3.one * placement.Scale);
            VegetationInstanceRuntime marker = instance.GetComponent<VegetationInstanceRuntime>();
            if (marker == null) marker = instance.AddComponent<VegetationInstanceRuntime>();
            marker.Configure(placement);
            ApplyCollisionPolicy(instance, placement.CollisionMode);
            VegetationGameplayPromoter.Promote(instance, marker, placement.SpeciesAsset);
        }

        private Transform GetChunkRoot(Transform root, int chunkX, int chunkZ)
        {
            long key = ((long)chunkX << 32) ^ (uint)chunkZ;
            if (chunkRoots.TryGetValue(key, out Transform chunk) && chunk != null) return chunk;
            var chunkObject = new GameObject($"Chunk_{chunkX}_{chunkZ}");
            chunkObject.transform.SetParent(root, false);
            chunk = chunkObject.transform;
            chunkRoots[key] = chunk;
            return chunk;
        }

        private static void ApplyCollisionPolicy(GameObject instance, VegetationCollisionMode policy)
        {
            if (policy == VegetationCollisionMode.VisualPrefab || policy == VegetationCollisionMode.GameplayResource) return;
            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null) continue;
                colliders[i].enabled = policy == VegetationCollisionMode.TrunkOnly
                    && colliders[i].GetComponent<VegetationTrunkCollider>() != null;
            }
        }
    }
}
