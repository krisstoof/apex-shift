using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Runtime.World.Generation;
using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    [DisallowMultipleComponent]
    public sealed class BuildingRegistry : MonoBehaviour
    {
        private static BuildingRegistry active;
        private readonly List<PlaceableStructureRuntime> structures = new List<PlaceableStructureRuntime>();
        [SerializeField] private PrefabRegistry prefabRegistry;

        public static BuildingRegistry Active
        {
            get
            {
                if (active == null)
                {
                    active = FindAnyObjectByType<BuildingRegistry>();
                }

                return active;
            }
        }

        public IReadOnlyList<PlaceableStructureRuntime> Structures => structures;

        private void Awake()
        {
            if (active != null && active != this)
            {
                Destroy(this);
                return;
            }

            active = this;
        }

        private void OnDestroy()
        {
            if (active == this)
            {
                active = null;
            }
        }

        public void SetPrefabRegistry(PrefabRegistry registry)
        {
            prefabRegistry = registry;
        }

        public void Register(PlaceableStructureRuntime structure)
        {
            if (structure != null && !structures.Contains(structure))
            {
                structures.Add(structure);
            }
        }

        public void Unregister(PlaceableStructureRuntime structure)
        {
            structures.Remove(structure);
        }

        public List<BuildingSaveData> CaptureSaveData()
        {
            return structures
                .Where(structure => structure != null && structure.gameObject != null)
                .OrderBy(structure => structure.InstanceId)
                .Select(structure => structure.ToSaveData())
                .Where(data => data != null)
                .ToList();
        }

        public void RestoreFromSaveData(IReadOnlyList<BuildingSaveData> savedBuildings, Transform parent = null)
        {
            ClearRuntimeStructures();
            if (savedBuildings == null || savedBuildings.Count == 0)
            {
                return;
            }

            Transform targetParent = parent != null ? parent : transform;
            foreach (BuildingSaveData data in savedBuildings.Where(data => data != null && data.Active))
            {
                Vector3 position = new Vector3(data.X, data.Y, data.Z);
                Quaternion rotation = Quaternion.Euler(0f, data.RotationY, 0f);
                GameObject prefab = ResolvePrefab(data.BuildingId);
                GameObject instance = prefab != null
                    ? Instantiate(prefab, position, rotation, targetParent)
                    : PlaceableFallbackFactory.CreateFallback(data.BuildingId, position, rotation, targetParent);

                if (instance == null)
                {
                    continue;
                }

                instance.name = $"Building_{data.BuildingId}_{data.InstanceId}";
                PlaceableStructureRuntime structure = instance.GetComponent<PlaceableStructureRuntime>();
                if (structure == null)
                {
                    structure = instance.AddComponent<PlaceableStructureRuntime>();
                }

                structure.Configure(data.BuildingId, data.InstanceId, structure.FootprintSize);
                Register(structure);
            }
        }

        private GameObject ResolvePrefab(string buildingId)
        {
            if (prefabRegistry != null && prefabRegistry.TryGetBuildingPrefab(buildingId, out GameObject prefab))
            {
                return prefab;
            }

            return null;
        }

        private void ClearRuntimeStructures()
        {
            PlaceableStructureRuntime[] existing = structures.Where(item => item != null).ToArray();
            structures.Clear();
            foreach (PlaceableStructureRuntime structure in existing)
            {
                if (structure == null || structure.gameObject == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(structure.gameObject);
                }
                else
                {
                    DestroyImmediate(structure.gameObject);
                }
            }
        }
    }
}
