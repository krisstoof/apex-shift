using System.Collections.Generic;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Biomes;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    public sealed class WorldGeneratorRuntime : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private BiomeCatalogAsset biomeCatalog;
        [SerializeField] private WorldGenerationSettings settings;
        [SerializeField] private List<ResourcePrefabEntry> resourcePrefabs = new List<ResourcePrefabEntry>();

        [Header("Settings")]
        [SerializeField] private bool generateOnStart = true;
        [SerializeField] private int seed = 12345;

        private WorldGenerationResult _lastResult;
        private Transform _terrainRoot;
        private Transform _biomeRoot;
        private Transform _resourceRoot;
        private Transform _creatureRoot;
        private Transform _buildingRoot;

        private void Start()
        {
            if (generateOnStart)
            {
                Generate();
            }
        }

        [ContextMenu("Generate World")]
        public void Generate()
        {
            Clear();
            EnsureRoots();

            Random.State oldState = Random.state;
            Random.InitState(seed);

            _lastResult = new WorldGenerationResult { Seed = seed };

            GenerateFixedLayout();

            Random.state = oldState;
            Debug.Log($"World Generation Complete. Biomes: {_lastResult.BiomeCount}, Resources: {_lastResult.ResourceCount}, Seed: {seed}");
        }

        public void SetBiomeCatalog(BiomeCatalogAsset catalog)
        {
            biomeCatalog = catalog;
        }

        private void Clear()
        {
            if (_terrainRoot != null) DestroyImmediate(_terrainRoot.gameObject);
            if (_biomeRoot != null) DestroyImmediate(_biomeRoot.gameObject);
            if (_resourceRoot != null) DestroyImmediate(_resourceRoot.gameObject);
            if (_creatureRoot != null) DestroyImmediate(_creatureRoot.gameObject);
            if (_buildingRoot != null) DestroyImmediate(_buildingRoot.gameObject);

            // Also find by name if references were lost
            DestroyByName("TerrainRoot");
            DestroyByName("BiomeRoot");
            DestroyByName("ResourceRoot");
            DestroyByName("CreatureRoot");
            DestroyByName("BuildingRoot");
        }

        private void DestroyByName(string name)
        {
            Transform t = transform.Find(name);
            if (t != null) DestroyImmediate(t.gameObject);
        }

        private void EnsureRoots()
        {
            _terrainRoot = CreateRoot("TerrainRoot");
            _biomeRoot = CreateRoot("BiomeRoot");
            _resourceRoot = CreateRoot("ResourceRoot");
            _creatureRoot = CreateRoot("CreatureRoot");
            _buildingRoot = CreateRoot("BuildingRoot");
        }

        private Transform CreateRoot(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            return go.transform;
        }

        private void GenerateFixedLayout()
        {
            if (biomeCatalog == null)
            {
                Debug.LogWarning("No BiomeCatalogAsset assigned to WorldGeneratorRuntime.");
                return;
            }

            float size = settings?.RegionSize ?? 40f;

            // Define locations
            AddRegion("hearth_meadow", Vector3.zero, size);
            AddRegion("westwood", new Vector3(-size, 0, 0), size);
            AddRegion("stoneback_ridge", new Vector3(size, 0, 0), size);
            AddRegion("south_thicket", new Vector3(0, 0, -size), size);
            AddRegion("redfang_wilds", new Vector3(0, 0, size), size);
        }

        private void AddRegion(string biomeId, Vector3 center, float size)
        {
            BiomeDefinitionAsset biome = biomeCatalog.GetBiome(biomeId);
            if (biome == null)
            {
                Debug.LogWarning($"Biome '{biomeId}' not found in catalog.");
                return;
            }

            Bounds bounds = new Bounds(center, new Vector3(size, 2f, size));
            GeneratedBiomeRegion region = new GeneratedBiomeRegion(biome, bounds);
            _lastResult.Regions.Add(region);
            _lastResult.BiomeCount++;

            CreateTerrainPlane(region);
            SpawnRegionResources(region);
        }

        private void CreateTerrainPlane(GeneratedBiomeRegion region)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = $"Terrain_{region.Biome.BiomeId}";
            plane.transform.SetParent(_terrainRoot);
            plane.transform.position = region.Bounds.center;
            // Primitive plane is 10x10 units
            float scale = region.Bounds.size.x / 10f;
            plane.transform.localScale = new Vector3(scale, 1f, scale);

            if (region.Biome.GroundMaterial != null)
            {
                plane.GetComponent<Renderer>().sharedMaterial = region.Biome.GroundMaterial;
            }
            else
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = region.Biome.GroundColor;
                plane.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        private void SpawnRegionResources(GeneratedBiomeRegion region)
        {
            float padding = settings?.Padding ?? 5f;
            Bounds spawnBounds = region.Bounds;
            spawnBounds.Expand(new Vector3(-padding * 2, 0, -padding * 2));

            foreach (var entry in region.Biome.Vegetation)
            {
                if (entry == null) continue;

                for (int i = 0; i < entry.Count; i++)
                {
                    _lastResult.SpawnAttempts++;
                    Vector3 pos = GetRandomPointInBounds(spawnBounds);
                    SpawnResource(entry, pos);
                }
            }
        }

        private Vector3 GetRandomPointInBounds(Bounds bounds)
        {
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                0f,
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        private void SpawnResource(VegetationSpawnEntryAsset entry, Vector3 position)
        {
            GameObject prefab = GetPrefabForKind(entry.Kind);
            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0, 360), 0), _resourceRoot);
            }
            else
            {
                instance = CreateFallbackPrimitive(entry.Kind, position);
                instance.transform.SetParent(_resourceRoot);
                instance.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            }

            instance.name = $"{entry.Kind}_{_lastResult.ResourceCount}";

            float scale = Random.Range(entry.MinScale, entry.MaxScale);
            instance.transform.localScale *= scale;

            ResourceNodeView nodeView = instance.GetComponent<ResourceNodeView>();
            if (nodeView == null)
            {
                nodeView = instance.AddComponent<ResourceNodeView>();
            }

            string rKind = string.IsNullOrWhiteSpace(entry.ResourceKind) ? entry.RoleId : entry.ResourceKind;
            nodeView.ConfigureDefault(rKind);

            // Interaction radius based on role if not set? 
            // The requirement says "use SphereCollider trigger for interaction radius"
            // ResourceNodeView already ensures a SphereCollider trigger in Awake/Reset.
            
            _lastResult.ResourceCount++;
        }

        private GameObject GetPrefabForKind(VegetationSpawnKind kind)
        {
            return resourcePrefabs.Find(p => p.Kind == kind)?.Prefab;
        }

        private GameObject CreateFallbackPrimitive(VegetationSpawnKind kind, Vector3 position)
        {
            GameObject go;
            switch (kind)
            {
                case VegetationSpawnKind.ConiferTree:
                case VegetationSpawnKind.LeafyTree:
                case VegetationSpawnKind.DryTree:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.position = position + Vector3.up * 1f;
                    go.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                    break;
                case VegetationSpawnKind.Rock:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = position + Vector3.up * 0.5f;
                    break;
                default:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.position = position + Vector3.up * 0.25f;
                    go.transform.localScale = Vector3.one * 0.5f;
                    break;
            }
            return go;
        }

        private void OnDrawGizmos()
        {
            if (_lastResult == null) return;

            foreach (var region in _lastResult.Regions)
            {
                Gizmos.color = region.Biome.GroundColor;
                Gizmos.DrawWireCube(region.Bounds.center, region.Bounds.size);
            }
        }

        private void OnGUI()
        {
            if (_lastResult == null) return;

            GUI.color = Color.black;
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Seed: {_lastResult.Seed}");
            GUILayout.Label($"Biomes: {_lastResult.BiomeCount}");
            GUILayout.Label($"Resources: {_lastResult.ResourceCount}");
            GUILayout.Label($"Spawn Attempts: {_lastResult.SpawnAttempts}");
            GUILayout.EndArea();
        }
    }
}
