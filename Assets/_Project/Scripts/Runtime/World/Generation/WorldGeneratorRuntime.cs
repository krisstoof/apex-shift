using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexShift.Runtime.Bootstrap;
using ApexShift.Runtime.Camera;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Config;
using ApexShift.Runtime.Audio;
using ApexShift.Runtime.World.Query;
using ApexShift.Runtime.DayNight;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.AI.Navigation;
using CameraComponent = UnityEngine.Camera;

namespace ApexShift.Runtime.World.Generation
{
    public sealed class WorldGeneratorRuntime : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private BiomeCatalogAsset biomeCatalog;
        [SerializeField] private WorldGenerationSettings settings;
        [SerializeField] private PrefabRegistry prefabRegistry;

        [Header("Legacy Prefab Lists - prefer PrefabRegistry")]
        [SerializeField] private List<ResourcePrefabEntry> resourcePrefabs = new List<ResourcePrefabEntry>();
        [SerializeField] private List<CreaturePrefabEntry> creaturePrefabs = new List<CreaturePrefabEntry>();

        [Header("Assets")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private RuntimeAnimatorController playerAnimatorController;
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameBalanceConfig gameBalanceConfig;
        [SerializeField] private CreatureAudioProfile creatureAudioProfile;
        [SerializeField] private CombatAudioProfile combatAudioProfile;
        [SerializeField] private TrapAudioProfile trapAudioProfile;

        [Header("Ambient Music")]
        [SerializeField] private bool enableAmbientMusic = true;
        [SerializeField, Range(0f, 1f)] private float ambientMusicVolume = 0.22f;

        [Header("Creature Population Balance")]
        [SerializeField] private float creatureSpawnDensityMultiplier = 0.85f;
        [SerializeField] private bool scaleVarnaksByDay = true;
        [SerializeField] private int varnakDayOneMaxCount = 0;
        [SerializeField] private int varnakAddEveryDays = 2;
        [SerializeField] private int varnakAbsoluteMaxCount = 5;
        [SerializeField] private float varnakDayOneSpawnMultiplier = 0.05f;
        [SerializeField] private float varnakSpawnMultiplierPerDay = 0.10f;
        [SerializeField] private float varnakMaxSpawnMultiplier = 0.65f;
        [SerializeField] private float minCreatureDistanceFromPlayer = 14f;
        [SerializeField] private float minVarnakDistanceFromPlayer = 36f;
        [SerializeField] private int creatureSpawnPositionAttempts = 16;
        [SerializeField] private int nonVarnakMinimumPerBiomeEntry = 1;

        [Header("Resource Size / Tool Gating")]
        [SerializeField] private float bigTreeScaleThreshold = 0.92f;
        [SerializeField] private float bigRockScaleThreshold = 0.88f;
        [SerializeField] private float bigTreeVisualScaleMultiplier = 1.18f;
        [SerializeField] private float bigRockVisualScaleMultiplier = 1.16f;
        [SerializeField] private float smallResourceVisualScaleMultiplier = 0.82f;

        [Header("Settings")]
        [SerializeField] private bool generateOnStart = false;
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool useCinemachine = true;
        [SerializeField] private float clearingRadius = 8f;

        private WorldGenerationResult _lastResult;
        private Transform _terrainRoot;
        private Transform _biomeRoot;
        private Transform _resourceRoot;
        private Transform _creatureRoot;
        private Transform _buildingRoot;
        private List<Vector3> _allTileCenters = new List<Vector3>();
        private List<Vector3> _landTileCenters = new List<Vector3>();
        private Transform _playerTransform;
        private int _spawnedVarnakCount;
        private int _currentSpawnDay = 1;

        private const string DefaultInputActionsPath = "Assets/InputSystem_Actions.inputactions";

        public event System.Action<GameObject> OnGenerationComplete;
        public int Seed => seed;
        public InputActionAsset InputActions => inputActions;

        private void Start()
        {
            if (generateOnStart)
            {
                Generate();
            }
        }

        public void SetGenerateOnStart(bool value)
        {
            generateOnStart = value;
        }

        public void ClearGeneratedWorld()
        {
            Clear();
        }

        [ContextMenu("Generate World")]
        public void Generate()
        {
            Clear();
            _allTileCenters.Clear();
            _landTileCenters.Clear();

            Random.State oldState = Random.state;
            Random.InitState(seed);

            _lastResult = new WorldGenerationResult { Seed = seed };

            CreateBootstrapper();
            EnsureEcosystemRuntime();
            EnsureDayNightRuntime();
            EnsureGameSnapshotProvider();
            EnsureDebugPanelPresenter();
            EnsureWorldMapDebugWindow();
            EnsureRoots();
            EnsureBuildingRegistry();
            GenerateIslandLayout();

            GameObject player = CreatePlayer();
            _playerTransform = player != null ? player.transform : null;
            _currentSpawnDay = ResolveCurrentDay();
            _spawnedVarnakCount = 0;
            GameObject cameraGo = CreateCamera(player.transform);
            EnsureAmbientMusicRuntime();
            CreateWorldBounds();

            ConfigurePlayerRuntime(player, cameraGo);
            InitializeEcosystemDirector();

            // Add and build NavMesh
            NavMeshSurface surface = _terrainRoot.GetComponent<NavMeshSurface>();
            if (surface == null) surface = _terrainRoot.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();
            EnsureCreatureIslandBoundsRuntime();

            SpawnAllRegionCreatures();

            Random.state = oldState;

            Debug.Log($"World Generation Complete. Biomes: {_lastResult.BiomeCount}, Resources: {_lastResult.ResourceCount}, Seed: {seed}");
            
            OnGenerationComplete?.Invoke(player);
        }

        public void SetBiomeCatalog(BiomeCatalogAsset catalog)
        {
            biomeCatalog = catalog;
        }

        public void SetSeed(int value)
        {
            seed = value;
        }

        public WorldGenerationResult GetLastResult()
        {
            return _lastResult;
        }

        private void Clear()
        {
            if (_terrainRoot != null) DestroyObject(_terrainRoot.gameObject);
            if (_biomeRoot != null) DestroyObject(_biomeRoot.gameObject);
            if (_resourceRoot != null) DestroyObject(_resourceRoot.gameObject);
            if (_creatureRoot != null) DestroyObject(_creatureRoot.gameObject);
            if (_buildingRoot != null) DestroyObject(_buildingRoot.gameObject);

            DestroyAllByName("TerrainRoot");
            DestroyAllByName("BiomeRoot");
            DestroyAllByName("ResourceRoot");
            DestroyAllByName("CreatureRoot");
            DestroyAllByName("BuildingRoot");
            DestroyAllByName("GameBootstrapper");
            DestroyAllByName("EcosystemRuntime");
            DestroyAllByName("DayNightRuntime");
            DestroyAllByName("WorldMapDebugWindow");
            DestroyAllByName("Player");
            DestroyAllByName("Main Camera");
            DestroyAllByName("PlayerFollowCamera");
            DestroyAllByName("Directional Light");
            DestroyAllByName("WorldBounds");
            DestroyAllByName("ActionBarUI");
            DestroyAllByName("CreatureIslandBoundsRuntime");
            DestroyAllByName("AmbientMusicRuntime");

            // Do not destroy generic menu objects here. Main menu / start screen
            // often uses roots named "UI" and a shared EventSystem.
        }

        private void DestroyAllByName(string name)
        {
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in objects)
            {
                if (go != null && go.name == name)
                {
                    // Only destroy if it's a root or child of the generator/parent
                    if (go.transform.parent == null || go.transform.parent == transform || (transform.parent != null && go.transform.parent == transform.parent))
                    {
                        DestroyObject(go);
                    }
                }
            }
        }

        private void DestroyObject(GameObject obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }

        private void EnsureRoots()
        {
            _terrainRoot = CreateRoot("TerrainRoot");
            _biomeRoot = CreateRoot("BiomeRoot");
            _resourceRoot = CreateRoot("ResourceRoot");
            _creatureRoot = CreateRoot("CreatureRoot");
            _buildingRoot = CreateRoot("BuildingRoot");
        }

        private void EnsureBuildingRegistry()
        {
            if (_buildingRoot == null)
            {
                return;
            }

            BuildingRegistry registry = _buildingRoot.GetComponent<BuildingRegistry>();
            if (registry == null)
            {
                registry = _buildingRoot.gameObject.AddComponent<BuildingRegistry>();
            }

            registry.SetPrefabRegistry(prefabRegistry);
        }

        private void EnsureCreatureIslandBoundsRuntime()
        {
            CreatureIslandBoundsRuntime bounds = Object.FindAnyObjectByType<CreatureIslandBoundsRuntime>();
            if (bounds == null)
            {
                GameObject go = new GameObject("CreatureIslandBoundsRuntime");
                go.transform.SetParent(transform);
                bounds = go.AddComponent<CreatureIslandBoundsRuntime>();
            }

            // Tile size is currently 8m. A 5.85m radius keeps targets inside the playable
            // land tile footprint while allowing movement across tile seams.
            bounds.Configure(_allTileCenters, 5.85f);
        }

        private void EnsureAmbientMusicRuntime()
        {
            if (!enableAmbientMusic)
            {
                return;
            }

            AmbientMusicRuntime ambient = Object.FindAnyObjectByType<AmbientMusicRuntime>();
            if (ambient == null)
            {
                GameObject go = new GameObject("AmbientMusicRuntime");
                go.transform.SetParent(transform);
                ambient = go.AddComponent<AmbientMusicRuntime>();
            }

            ambient.SetVolume(ambientMusicVolume);
            ambient.Play();
        }

        private Transform CreateRoot(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            return go.transform;
        }

        private void CreateBootstrapper()
        {
            GameObject go = new GameObject("GameBootstrapper");
            go.transform.SetParent(transform);
            go.AddComponent<GameBootstrapper>();
        }

        private void EnsureEcosystemRuntime()
        {
            EcosystemRuntime existing = Object.FindAnyObjectByType<EcosystemRuntime>();
            if (existing != null)
            {
                EnsureEcosystemComponents(existing);
                return;
            }

            GameObject go = new GameObject("EcosystemRuntime");
            go.transform.SetParent(transform);
            EcosystemRuntime runtime = go.AddComponent<EcosystemRuntime>();
            EnsureEcosystemComponents(runtime);
        }

        private void EnsureEcosystemComponents(EcosystemRuntime runtime)
        {
            if (runtime == null) return;
            if (runtime.GetComponent<EcosystemDirectorRuntime>() == null) runtime.gameObject.AddComponent<EcosystemDirectorRuntime>();
            if (runtime.GetComponent<WorldQueryRuntime>() == null) runtime.gameObject.AddComponent<WorldQueryRuntime>();
        }

        private void InitializeEcosystemDirector()
        {
            EcosystemDirectorRuntime director = Object.FindAnyObjectByType<EcosystemDirectorRuntime>();
            if (director != null && _lastResult != null)
            {
                director.InitializeFromRegions(_lastResult.Regions);
            }
        }

        private void EnsureWorldMapDebugWindow()
        {
            if (Object.FindAnyObjectByType<WorldMapDebugWindow>() != null)
            {
                return;
            }

            GameObject go = new GameObject("WorldMapDebugWindow");
            go.transform.SetParent(transform);
            go.AddComponent<WorldMapDebugWindow>();
        }

        private void EnsureGameSnapshotProvider()
        {
            if (Object.FindAnyObjectByType<ApexShift.Runtime.UI.Snapshots.GameSnapshotProvider>() != null)
            {
                return;
            }

            GameObject go = new GameObject("GameSnapshotProvider");
            go.transform.SetParent(transform);
            go.AddComponent<ApexShift.Runtime.UI.Snapshots.GameSnapshotProvider>();
        }

        private void EnsureDayNightRuntime()
        {
            if (Object.FindAnyObjectByType<DayNightRuntime>() != null)
            {
                return;
            }

            GameObject go = new GameObject("DayNightRuntime");
            go.transform.SetParent(transform);
            go.AddComponent<DayNightRuntime>();
        }

        private void EnsureDebugPanelPresenter()
        {
            if (Object.FindAnyObjectByType<ApexShift.Runtime.UI.Debugging.DebugPanelPresenter>() != null)
            {
                return;
            }

            GameObject go = new GameObject("DebugPanelPresenter");
            go.transform.SetParent(transform);
            go.AddComponent<ApexShift.Runtime.UI.Debugging.DebugPanelPresenter>();
        }

        private bool IsInsideIsland(float x, float z)
        {
            const float islandRadiusX = 108f;
            const float islandRadiusZ = 82f;

            float normalizedX = x / islandRadiusX;
            float normalizedZ = z / islandRadiusZ;
            float distance = normalizedX * normalizedX + normalizedZ * normalizedZ;

            float edgeNoiseA = Mathf.PerlinNoise((x + 100f) * 0.035f, (z + 100f) * 0.035f);
            float edgeNoiseB = Mathf.PerlinNoise((x + 250f) * 0.085f, (z + 250f) * 0.070f);
            float edgeNoiseC = Mathf.PerlinNoise((x + 510f) * 0.145f, (z + 510f) * 0.120f);

            float radiusModifier = Mathf.Lerp(0.78f, 1.18f, edgeNoiseA);
            radiusModifier += (edgeNoiseB - 0.5f) * 0.16f;
            radiusModifier += (edgeNoiseC - 0.5f) * 0.07f;

            // Create larger peninsulas and coves, but keep the world readable.
            float westPeninsula = Mathf.Exp(-Mathf.Pow((x + 92f) / 34f, 2f) - Mathf.Pow((z + 12f) / 40f, 2f)) * 0.18f;
            float northBay = Mathf.Exp(-Mathf.Pow(x / 42f, 2f) - Mathf.Pow((z - 74f) / 24f, 2f)) * 0.13f;
            float southBite = Mathf.Exp(-Mathf.Pow((x - 18f) / 40f, 2f) - Mathf.Pow((z + 72f) / 22f, 2f)) * 0.16f;

            radiusModifier += westPeninsula;
            radiusModifier -= northBay;
            radiusModifier -= southBite;

            return distance <= radiusModifier;
        }

        private string DetermineBiome(Vector3 position)
        {
            float borderNoise = Mathf.PerlinNoise((position.x + 200f) * 0.030f, (position.z + 200f) * 0.030f) - 0.5f;
            float x = position.x + borderNoise * 22f;
            float z = position.z + borderNoise * 18f;

            float centerDistance = Mathf.Sqrt(x * x + z * z);
            if (centerDistance < 18f)
            {
                return "hearth_meadow";
            }

            float moisture = Mathf.PerlinNoise((x + 650f) * 0.026f, (z + 870f) * 0.026f);
            float heat = Mathf.PerlinNoise((x + 125f) * 0.022f, (z + 430f) * 0.022f);
            float ridge = Mathf.PerlinNoise((x + 910f) * 0.045f, (z + 220f) * 0.032f);

            if (z > 38f || (ridge > 0.66f && z > 8f))
            {
                return "stoneback_ridge";
            }

            if (x < -34f && moisture > 0.35f)
            {
                return "westwood";
            }

            if (x > 42f || (heat > 0.62f && z < -12f))
            {
                return "redfang_wilds";
            }

            if (moisture > 0.58f || (z < -10f && x < 28f))
            {
                return "south_thicket";
            }

            if (x < -22f)
            {
                return "westwood";
            }

            if (heat > 0.55f)
            {
                return "redfang_wilds";
            }

            return "south_thicket";
        }

        private void GenerateIslandLayout()
        {
            if (biomeCatalog == null)
            {
                Debug.LogWarning("No BiomeCatalogAsset assigned to WorldGeneratorRuntime.");
                return;
            }

            int gridSize = 38;
            float tileSize = 8f;
            
            bool[,] landGrid = new bool[gridSize, gridSize];
            Vector3 centerOffset = new Vector3(gridSize * tileSize * 0.5f, 0, gridSize * tileSize * 0.5f);

            // First pass: Determine Land/Water and Create Tiles.
            // This is intentionally runtime-specific: large island layout, not handcrafted rectangles.
            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize) - centerOffset + new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f);
                    
                    bool isLand = IsInsideIsland(pos.x, pos.z);
                    landGrid[x, z] = isLand;

                    string biomeId;
                    if (isLand)
                    {
                        biomeId = DetermineBiome(pos);
                        _landTileCenters.Add(pos);
                    }
                    else
                    {
                        biomeId = "water";
                    }

                    AddTileRegion(biomeId, pos, tileSize);
                }
            }

            // Second pass: Add Hard Boundaries (Invisible Walls)
            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    if (!landGrid[x, z]) continue;
                    Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize) - centerOffset + new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f);
                    
                    CheckAndAddWall(x + 1, z, pos, Vector3.right, landGrid, gridSize, tileSize);
                    CheckAndAddWall(x - 1, z, pos, Vector3.left, landGrid, gridSize, tileSize);
                    CheckAndAddWall(x, z + 1, pos, Vector3.forward, landGrid, gridSize, tileSize);
                    CheckAndAddWall(x, z - 1, pos, Vector3.back, landGrid, gridSize, tileSize);
                }
            }
        }

        private void CheckAndAddWall(int nx, int nz, Vector3 pos, Vector3 direction, bool[,] landGrid, int gridSize, float tileSize)
        {
            bool isWater = false;
            if (nx < 0 || nx >= gridSize || nz < 0 || nz >= gridSize) isWater = true;
            else if (!landGrid[nx, nz]) isWater = true;

            if (isWater)
            {
                GameObject wall = new GameObject("IslandWall");
                wall.transform.SetParent(_terrainRoot);
                wall.transform.position = pos + direction * (tileSize * 0.5f) + Vector3.up * 5f;
                BoxCollider col = wall.AddComponent<BoxCollider>();
                col.size = (direction.x != 0) ? new Vector3(0.1f, 10f, tileSize) : new Vector3(tileSize, 10f, 0.1f);
            }
        }

        private void AddTileRegion(string biomeId, Vector3 center, float size)
        {
            BiomeDefinitionAsset biome = biomeCatalog.GetBiome(biomeId);
            if (biome == null)
            {
                Debug.LogWarning($"Biome '{biomeId}' not found in catalog.");
                return;
            }

            float terrainHeight = biomeId == "water" ? -0.35f : GetTerrainHeight(center, biomeId);
            Vector3 regionCenter = new Vector3(center.x, terrainHeight, center.z);
            Bounds bounds = new Bounds(regionCenter, new Vector3(size, 2f, size));
            GeneratedBiomeRegion region = new GeneratedBiomeRegion(biome, bounds);
            _lastResult.Regions.Add(region);
            _lastResult.BiomeCount++;

            CreateTerrainTile(region);
            
            if (biomeId != "water")
            {
                // Store the actual terrain surface height, not the original flat y=0 center.
                // Player/creature/resource spawning relies on these centers.
                _allTileCenters.Add(regionCenter);

                SpawnRegionResources(region);
            }
        }

        private float GetTerrainHeight(Vector3 position, string biomeId)
        {
            float broad = Mathf.PerlinNoise((position.x + 300f) * 0.014f, (position.z + 300f) * 0.014f);
            float detail = Mathf.PerlinNoise((position.x + 900f) * 0.045f, (position.z + 900f) * 0.045f);

            float height = (broad - 0.5f) * 0.45f + (detail - 0.5f) * 0.16f;

            switch (biomeId)
            {
                case "stoneback_ridge":
                    height += 0.45f + Mathf.PerlinNoise((position.x + 30f) * 0.035f, (position.z + 80f) * 0.035f) * 0.35f;
                    break;
                case "westwood":
                    height += 0.18f;
                    break;
                case "redfang_wilds":
                    height += 0.10f;
                    break;
                case "south_thicket":
                    height += 0.06f;
                    break;
                case "hearth_meadow":
                    height = Mathf.Lerp(height, 0.03f, 0.75f);
                    break;
            }

            height = Mathf.Clamp(height, -0.20f, biomeId == "stoneback_ridge" ? 0.95f : 0.45f);
            return Mathf.Round(height * 10f) / 10f;
        }

        private void CreateTerrainTile(GeneratedBiomeRegion region)
        {
            const float overlap = 0.08f;
            const float thickness = 0.42f;

            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"Terrain_{region.Biome.BiomeId}";
            tile.transform.SetParent(_terrainRoot);

            // Keep the top surface at region.Bounds.center.y while making the tile thick enough
            // to hide vertical seams between tiles with slightly different heights.
            tile.transform.position = region.Bounds.center + Vector3.down * (thickness * 0.5f);
            tile.transform.localScale = new Vector3(region.Bounds.size.x + overlap, thickness, region.Bounds.size.z + overlap);

            if (region.Biome.GroundMaterial != null)
            {
                tile.GetComponent<Renderer>().sharedMaterial = region.Biome.GroundMaterial;
            }
            else
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat.shader = Shader.Find("Standard");
                
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", region.Biome.GroundColor);
                else
                    mat.color = region.Biome.GroundColor;
                
                tile.GetComponent<Renderer>().sharedMaterial = mat;
            }

            if (region.Biome.BiomeId == "water")
            {
                ConfigureWaterTile(tile, region.Bounds.center);
            }
        }

        private void ConfigureWaterTile(GameObject tile, Vector3 center)
        {
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material source = biomeCatalog != null ? biomeCatalog.GetBiome("water")?.GroundMaterial : null;
                Material waterMaterial = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                Shader waterShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (waterShader != null)
                {
                    waterMaterial.shader = waterShader;
                }
                else if (waterMaterial.shader == null)
                {
                    waterMaterial.shader = Shader.Find("Standard");
                }

                float distanceToLand = GetDistanceToNearestLand(center);
                bool shoreline = distanceToLand < 16f;
                bool lakeLike = distanceToLand < 10f;
                Color color = lakeLike
                    ? new Color(0.14f, 0.50f, 0.66f, 0.76f)
                    : shoreline
                        ? new Color(0.10f, 0.42f, 0.62f, 0.70f)
                        : new Color(0.03f, 0.22f, 0.36f, 0.82f);

                if (waterMaterial.HasProperty("_BaseColor"))
                {
                    waterMaterial.SetColor("_BaseColor", color);
                }

                if (waterMaterial.HasProperty("_Color"))
                {
                    waterMaterial.SetColor("_Color", color);
                }

                if (waterMaterial.HasProperty("_Smoothness"))
                {
                    waterMaterial.SetFloat("_Smoothness", lakeLike ? 0.62f : shoreline ? 0.72f : 0.91f);
                }

                if (waterMaterial.HasProperty("_Metallic"))
                {
                    waterMaterial.SetFloat("_Metallic", 0.0f);
                }

                if (waterMaterial.HasProperty("_SpecColor"))
                {
                    waterMaterial.SetColor("_SpecColor", lakeLike ? new Color(0.34f, 0.44f, 0.48f, 1f) : new Color(0.26f, 0.34f, 0.42f, 1f));
                }

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = waterMaterial;
            }

            WaterSurfaceAnimator animator = tile.GetComponent<WaterSurfaceAnimator>();
            if (animator == null)
            {
                animator = tile.AddComponent<WaterSurfaceAnimator>();
            }

            animator.Configure(GetDistanceToNearestLand(center) < 16f);
        }

        private float GetDistanceToNearestLand(Vector3 center)
        {
            if (_landTileCenters.Count == 0)
            {
                return float.PositiveInfinity;
            }

            float best = float.PositiveInfinity;
            Vector2 waterPoint = new Vector2(center.x, center.z);
            foreach (Vector3 landCenter in _landTileCenters)
            {
                float distance = Vector2.Distance(waterPoint, new Vector2(landCenter.x, landCenter.z));
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best;
        }

        private void SpawnRegionResources(GeneratedBiomeRegion region)
        {
            float padding = settings?.Padding ?? 5f;
            Bounds spawnBounds = region.Bounds;
            float actualPadding = Mathf.Min(padding, region.Bounds.size.x * 0.2f);
            spawnBounds.Expand(new Vector3(-actualPadding * 2, 0, -actualPadding * 2));

            float originalRegionSize = 40f;
            float spawnProbability = (region.Bounds.size.x * region.Bounds.size.z) / (originalRegionSize * originalRegionSize);

            foreach (var entry in region.Biome.Vegetation)
            {
                if (entry == null) continue;

                for (int i = 0; i < entry.Count; i++)
                {
                    if (Random.value > spawnProbability) continue;

                    Vector3 pos = GetRandomPointInBounds(spawnBounds);
                    if (pos.magnitude < clearingRadius) continue;

                    _lastResult.SpawnAttempts++;
                    SpawnResource(entry, pos);
                }
            }
        }

        private void SpawnRegionCreatures(GeneratedBiomeRegion region)
        {
            float padding = settings?.Padding ?? 5f;
            Bounds spawnBounds = region.Bounds;
            float actualPadding = Mathf.Min(padding, region.Bounds.size.x * 0.2f);
            spawnBounds.Expand(new Vector3(-actualPadding * 2, 0, -actualPadding * 2));

            float originalRegionSize = 40f;
            float spawnProbability = (region.Bounds.size.x * region.Bounds.size.z) / (originalRegionSize * originalRegionSize);

            foreach (var entry in region.Biome.Creatures)
            {
                if (entry == null) continue;
                string creatureId = NormalizeCreatureId(entry.CreatureId);
                if (creatureId == "varnak" && !CanSpawnMoreVarnaks())
                {
                    continue;
                }

                float creatureSpawnProbability = spawnProbability;
                if (creatureId == "varnak")
                {
                    creatureSpawnProbability *= GetVarnakSpawnMultiplierForDay(_currentSpawnDay);
                    if (creatureSpawnProbability <= 0f)
                    {
                        continue;
                    }
                }

                int count = Mathf.CeilToInt(Random.Range(entry.MinCount, entry.MaxCount + 1) * Mathf.Clamp01(creatureSpawnDensityMultiplier));
                count = Mathf.Max(0, count);
                if (creatureId != "varnak" && entry.MaxCount > 0)
                {
                    count = Mathf.Max(Mathf.Clamp(nonVarnakMinimumPerBiomeEntry, 0, Mathf.Max(1, entry.MaxCount)), count);
                }
                if (creatureId == "varnak")
                {
                    count = Mathf.Min(count, GetRemainingVarnakSpawnCapacity());
                }

                int countToSpawn = 0;
                for (int i = 0; i < count; i++)
                {
                    if (Random.value < creatureSpawnProbability)
                    {
                        countToSpawn++;
                    }
                }

                if (creatureId == "varnak")
                {
                    countToSpawn = Mathf.Min(countToSpawn, GetRemainingVarnakSpawnCapacity());
                }

                for (int i = 0; i < countToSpawn; i++)
                {
                    if (!TryGetSafeCreatureSpawnPoint(spawnBounds, creatureId, out Vector3 pos))
                    {
                        continue;
                    }

                    SpawnCreature(entry, pos);
                }
            }
        }

        private void SpawnAllRegionCreatures()
        {
            foreach (GeneratedBiomeRegion region in _lastResult.Regions)
            {
                if (region?.Biome == null || region.Biome.BiomeId == "water")
                {
                    continue;
                }

                SpawnRegionCreatures(region);
            }
        }

        private bool TryGetSafeCreatureSpawnPoint(Bounds spawnBounds, string creatureId, out Vector3 pos)
        {
            int attempts = Mathf.Max(1, creatureSpawnPositionAttempts);
            float minDistance = creatureId == "varnak"
                ? Mathf.Max(minCreatureDistanceFromPlayer, minVarnakDistanceFromPlayer)
                : Mathf.Max(clearingRadius, minCreatureDistanceFromPlayer);

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                Vector3 candidate = GetRandomPointInBounds(spawnBounds);
                CreatureIslandBoundsRuntime bounds = CreatureIslandBoundsRuntime.Active;
                if (bounds != null && bounds.HasLand)
                {
                    bounds.TryClampToLand(candidate, out candidate);
                }

                if (!IsCreatureSpawnPointSafe(candidate, minDistance))
                {
                    continue;
                }

                pos = candidate;
                return true;
            }

            pos = default;
            return false;
        }

        private bool IsCreatureSpawnPointSafe(Vector3 pos, float minDistanceFromPlayer)
        {
            if (pos.magnitude < clearingRadius)
            {
                return false;
            }

            if (_playerTransform != null)
            {
                // Player can be snapped to terrain after creation, so always use current transform.
                Vector3 delta = pos - _playerTransform.position;
                delta.y = 0f;
                if (delta.sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
                {
                    return false;
                }
            }

            // Extra safety against center/start-area spawns even if player reference is missing.
            float startSafeRadius = Mathf.Max(clearingRadius, minCreatureDistanceFromPlayer);
            if (pos.sqrMagnitude < startSafeRadius * startSafeRadius)
            {
                return false;
            }

            return true;
        }

        private int ResolveCurrentDay()
        {
            ApexShift.Runtime.DayNight.DayNightRuntime dayNight = ApexShift.Runtime.DayNight.DayNightRuntime.Active;
            return dayNight != null ? Mathf.Max(1, dayNight.Day) : 1;
        }

        private int GetVarnakMaxCountForDay(int day)
        {
            if (!scaleVarnaksByDay)
            {
                return Mathf.Max(0, varnakAbsoluteMaxCount);
            }

            int safeDay = Mathf.Max(1, day);
            int addEvery = Mathf.Max(1, varnakAddEveryDays);
            int additional = Mathf.FloorToInt((safeDay - 1) / (float)addEvery);
            int maxForDay = Mathf.Max(0, varnakDayOneMaxCount) + additional;
            return Mathf.Clamp(maxForDay, 0, Mathf.Max(0, varnakAbsoluteMaxCount));
        }

        private float GetVarnakSpawnMultiplierForDay(int day)
        {
            if (!scaleVarnaksByDay)
            {
                return 1f;
            }

            int safeDay = Mathf.Max(1, day);
            float multiplier = varnakDayOneSpawnMultiplier + Mathf.Max(0, safeDay - 1) * Mathf.Max(0f, varnakSpawnMultiplierPerDay);
            return Mathf.Clamp(multiplier, 0f, Mathf.Max(0f, varnakMaxSpawnMultiplier));
        }

        private int GetRemainingVarnakSpawnCapacity()
        {
            return Mathf.Max(0, GetVarnakMaxCountForDay(_currentSpawnDay) - _spawnedVarnakCount);
        }

        private bool CanSpawnMoreVarnaks()
        {
            return GetRemainingVarnakSpawnCapacity() > 0;
        }

        private static string NormalizeCreatureId(string creatureId)
        {
            return string.IsNullOrWhiteSpace(creatureId) ? string.Empty : creatureId.Trim().ToLowerInvariant();
        }

        private Vector3 GetRandomPointInBounds(Bounds bounds)
        {
            return new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y + 0.02f,
                Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        private void SpawnResource(VegetationSpawnEntryAsset entry, Vector3 position)
        {
            float scale = Random.Range(entry.MinScale, entry.MaxScale);
            string resolvedKind = ResolveResourceKind(entry, scale);
            
            GameObject prefab = GetPrefabForResolvedKind(resolvedKind, entry.Kind);
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

            instance.name = $"{resolvedKind}_{_lastResult.ResourceCount}";

            float visualScaleMultiplier = ResolveResourceVisualScaleMultiplier(resolvedKind);
            instance.transform.localScale *= scale * visualScaleMultiplier;

            string configuredKind = resolvedKind;
            if (string.IsNullOrWhiteSpace(configuredKind))
            {
                if (entry.Kind == VegetationSpawnKind.GreenBush)
                    configuredKind = "bush";
                else if (entry.Kind == VegetationSpawnKind.GrassOrFlower)
                    configuredKind = "";
                else
                    configuredKind = entry.RoleId;
            }

            if (!string.IsNullOrWhiteSpace(configuredKind))
            {
                ResourceNodeView nodeView = instance.GetComponent<ResourceNodeView>();
                if (nodeView == null)
                {
                    nodeView = instance.AddComponent<ResourceNodeView>();
                }
                nodeView.ConfigureDefault(configuredKind);
                nodeView.ConfigureToolRequirement(ResolveRequiredToolForResource(configuredKind));
            }

            AddFoodSourceToResource(entry.Kind, instance);

            _lastResult.ResourceCount++;
        }

        private string ResolveResourceKind(VegetationSpawnEntryAsset entry, float scale)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            string explicitKind = string.IsNullOrWhiteSpace(entry.ResourceKind)
                ? string.Empty
                : entry.ResourceKind.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(explicitKind))
            {
                if (IsTreeResourceKind(explicitKind))
                {
                    return scale >= Mathf.Max(0.01f, bigTreeScaleThreshold) ? "big_tree" : "small_tree";
                }

                if (IsRockResourceKind(explicitKind))
                {
                    return scale >= Mathf.Max(0.01f, bigRockScaleThreshold) ? "big_rock" : "small_rock";
                }

                return explicitKind;
            }

            string role = string.IsNullOrWhiteSpace(entry.RoleId) ? string.Empty : entry.RoleId.Trim().ToLowerInvariant();
            if (entry.Kind == VegetationSpawnKind.ConiferTree ||
                entry.Kind == VegetationSpawnKind.LeafyTree ||
                entry.Kind == VegetationSpawnKind.DryTree ||
                IsTreeResourceKind(role))
            {
                return scale >= Mathf.Max(0.01f, bigTreeScaleThreshold) ? "big_tree" : "small_tree";
            }

            if (entry.Kind == VegetationSpawnKind.Rock || IsRockResourceKind(role))
            {
                return scale >= Mathf.Max(0.01f, bigRockScaleThreshold) ? "big_rock" : "small_rock";
            }

            return role;
        }

        private float ResolveResourceVisualScaleMultiplier(string resourceKind)
        {
            string normalized = string.IsNullOrWhiteSpace(resourceKind) ? string.Empty : resourceKind.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "big_tree":
                    return Mathf.Max(1f, bigTreeVisualScaleMultiplier);
                case "big_rock":
                    return Mathf.Max(1f, bigRockVisualScaleMultiplier);
                case "small_tree":
                case "small_rock":
                    return Mathf.Clamp(smallResourceVisualScaleMultiplier, 0.25f, 1f);
                default:
                    return 1f;
            }
        }

        private static string ResolveRequiredToolForResource(string resourceKind)
        {
            string normalized = string.IsNullOrWhiteSpace(resourceKind) ? string.Empty : resourceKind.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "big_tree":
                    return "axe";
                case "big_rock":
                    return "pickaxe";
                default:
                    return string.Empty;
            }
        }

        private static bool IsTreeResourceKind(string resourceKind)
        {
            string normalized = string.IsNullOrWhiteSpace(resourceKind) ? string.Empty : resourceKind.Trim().ToLowerInvariant();
            return normalized == "tree" || normalized == "conifer_tree" || normalized == "leafy_tree" || normalized == "dry_tree" || normalized.EndsWith("_tree");
        }

        private static bool IsRockResourceKind(string resourceKind)
        {
            string normalized = string.IsNullOrWhiteSpace(resourceKind) ? string.Empty : resourceKind.Trim().ToLowerInvariant();
            return normalized == "rock" || normalized.EndsWith("_rock");
        }

        private void AddFoodSourceToResource(VegetationSpawnKind kind, GameObject instance)
        {
            FoodSourceView fv = null;
            switch (kind)
            {
                case VegetationSpawnKind.GrassOrFlower:
                    fv = instance.AddComponent<FoodSourceView>();
                    fv.Configure(ApexShift.Core.Ecosystem.FoodKind.Plants, 5f, 2f);
                    break;
                case VegetationSpawnKind.BerryBush:
                    fv = instance.AddComponent<FoodSourceView>();
                    fv.Configure(ApexShift.Core.Ecosystem.FoodKind.Plants, 15f, 8f);
                    break;
                case VegetationSpawnKind.GreenBush:
                case VegetationSpawnKind.DryBush:
                    fv = instance.AddComponent<FoodSourceView>();
                    fv.Configure(ApexShift.Core.Ecosystem.FoodKind.Plants, 10f, 4f);
                    break;
            }
        }

        private void SpawnCreature(CreatureSpawnEntryAsset entry, Vector3 position)
        {
            CreatureIslandBoundsRuntime bounds = CreatureIslandBoundsRuntime.Active;
            if (bounds != null && bounds.HasLand)
            {
                bounds.TryClampToLand(position, out position);
            }

            GameObject prefab = GetPrefabForCreature(entry.CreatureId);
            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0, 360), 0), _creatureRoot);
            }
            else
            {
                instance = CreateCreatureFallback(entry.CreatureId, position);
                instance.transform.SetParent(_creatureRoot);
            }

            instance.name = $"Creature_{entry.CreatureId}";
            if (NormalizeCreatureId(entry.CreatureId) == "varnak")
            {
                _spawnedVarnakCount++;
            }

            // Remove existing movement components from asset pack prefabs to prevent player input interference
            var moveInput = instance.GetComponent("MovePlayerInput");
            if (moveInput != null)
            {
                if (Application.isPlaying) Destroy(moveInput);
                else DestroyImmediate(moveInput);
            }

            var creatureMover = instance.GetComponent("CreatureMover");
            if (creatureMover != null)
            {
                if (Application.isPlaying) Destroy(creatureMover);
                else DestroyImmediate(creatureMover);
            }

            // Remove CharacterController if present, as we use NavMeshAgent for movement
            var cc = instance.GetComponent<CharacterController>();
            if (cc != null)
            {
                if (Application.isPlaying) Destroy(cc);
                else DestroyImmediate(cc);
            }

            // Add and configure components
            var navAgent = instance.GetComponent<UnityEngine.AI.NavMeshAgent>();
if (navAgent == null) navAgent = instance.AddComponent<UnityEngine.AI.NavMeshAgent>();
            
            var adapter = instance.GetComponent<CreatureNavigationAdapter>();
            if (adapter == null) adapter = instance.AddComponent<CreatureNavigationAdapter>();

            var view = instance.GetComponent<CreatureAgentView>();
            if (view == null) view = instance.AddComponent<CreatureAgentView>();
            view.Configure(entry.CreatureId);

            var wander = instance.GetComponent<CreatureWanderBehavior>();
            if (wander == null) wander = instance.AddComponent<CreatureWanderBehavior>();

            var needs = instance.GetComponent<CreatureNeedsRuntime>();
            if (needs == null) needs = instance.AddComponent<CreatureNeedsRuntime>();
            needs.SetGameBalanceConfigForTests(gameBalanceConfig);
            needs.Configure(entry.CreatureId);

            var health = instance.GetComponent<CreatureHealthRuntime>();
            if (health == null) health = instance.AddComponent<CreatureHealthRuntime>();
            health.SetGameBalanceConfigForTests(gameBalanceConfig);
            health.SetCreatureAudioProfileForTests(creatureAudioProfile);
            health.Configure(entry.CreatureId);

            var hitbox = instance.GetComponent<CreatureHitboxRuntime>();
            if (hitbox == null) hitbox = instance.AddComponent<CreatureHitboxRuntime>();
            hitbox.Configure(entry.CreatureId);

            var creatureAudio = instance.GetComponent<CreatureAudioRuntime>();
            if (creatureAudio == null) creatureAudio = instance.AddComponent<CreatureAudioRuntime>();
            creatureAudio.SetCreatureAudioProfile(creatureAudioProfile);
            creatureAudio.Configure(entry.CreatureId);

            var oldFoodSeeking = instance.GetComponent<CreatureFoodSeekingBehavior>();
            if (oldFoodSeeking != null) oldFoodSeeking.enabled = false;

            var playerAwareness = instance.GetComponent<CreaturePlayerAwarenessBehavior>();
            if (playerAwareness == null) playerAwareness = instance.AddComponent<CreaturePlayerAwarenessBehavior>();
            playerAwareness.enabled = true;
            playerAwareness.Configure(entry.CreatureId);

            var behavior = instance.GetComponent<CreatureBehaviorRuntime>();
            if (behavior == null) behavior = instance.AddComponent<CreatureBehaviorRuntime>();

            var debugOverlay = instance.GetComponent<CreatureDebugOverlay>();
            if (debugOverlay == null) debugOverlay = instance.AddComponent<CreatureDebugOverlay>();

            var animDriver = instance.GetComponent<CreatureAnimationDriver>();
            if (animDriver == null) animDriver = instance.AddComponent<CreatureAnimationDriver>();
            float runThreshold = 2.0f;
            if (entry.CreatureId == "grazer") runThreshold = 1.2f;
            else if (entry.CreatureId == "small_prey") runThreshold = 2.5f;
            else if (entry.CreatureId == "varnak") runThreshold = 3.5f;
            animDriver.Configure(runThreshold);

            ConfigureCreatureMovement(entry.CreatureId, adapter, wander);

            var ecosystem = EcosystemRuntime.Instance;
            ecosystem?.RegisterCreature(view);
        }

        private GameObject CreateCreatureFallback(string creatureId, Vector3 position)
        {
            GameObject root = new GameObject($"Creature_{creatureId}_Fallback");
            root.transform.position = position;

            GameObject visual;
            Color color = Color.white;
            Vector3 scale = Vector3.one;

            switch (creatureId)
            {
                case "small_prey":
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    color = new Color(0.9f, 0.9f, 0.9f); // Brighter small sphere
                    scale = Vector3.one * 0.5f;
                    break;
                case "grazer":
                    visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    color = new Color(0.6f, 0.4f, 0.2f); // Lighter brown
                    scale = new Vector3(0.8f, 0.8f, 0.8f);
                    break;
                case "varnak":
                    visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    color = Color.red;
                    scale = new Vector3(1.0f, 1.2f, 1.0f);
                    break;
                default:
                    visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    color = Color.magenta;
                    break;
            }

            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.up * (scale.y * 0.5f);
            visual.transform.localScale = scale;

            Collider coll = visual.GetComponent<Collider>();
            if (coll != null)
            {
                if (Application.isPlaying) Destroy(coll);
                else DestroyImmediate(coll);
            }
            
            Renderer renderer = visual.GetComponent<Renderer>();
if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat.shader = Shader.Find("Standard");
                
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else
                    mat.color = color;
                
                renderer.sharedMaterial = mat;
            }

            return root;
        }

        private void ConfigureCreatureMovement(string creatureId, CreatureNavigationAdapter adapter, CreatureWanderBehavior wander)
        {
            switch (creatureId)
            {
                case "small_prey":
                    adapter.ConfigureMovement(speed: 3.5f, acceleration: 8f, stoppingDistance: 0.5f);
                    wander.Configure(radius: 8f, minWait: 2f, maxWait: 4f);
                    break;
                case "grazer":
                    adapter.ConfigureMovement(speed: 2f, acceleration: 4f, stoppingDistance: 0.75f);
                    wander.Configure(radius: 12f, minWait: 4f, maxWait: 8f);
                    break;
                case "varnak":
                    adapter.ConfigureMovement(speed: 5f, acceleration: 12f, stoppingDistance: 1f);
                    wander.Configure(radius: 15f, minWait: 1f, maxWait: 3f);
                    break;
                default:
                    adapter.ConfigureMovement(speed: 3f, acceleration: 8f, stoppingDistance: 0.5f);
                    wander.Configure(radius: 10f, minWait: 2f, maxWait: 5f);
                    break;
            }
        }

        private GameObject GetPrefabForKind(VegetationSpawnKind kind)
        {
            if (prefabRegistry != null && prefabRegistry.TryGetResourcePrefab(kind, out GameObject registryPrefab))
            {
                return registryPrefab;
            }

            var matches = resourcePrefabs.Where(p => p != null && p.Prefab != null && p.Kind == kind).ToList();
            if (matches.Count == 0) return null;
            return matches[Random.Range(0, matches.Count)].Prefab;
        }

        private GameObject GetPrefabForResolvedKind(string resolvedKind, VegetationSpawnKind fallbackKind)
        {
            // First, try to find a prefab based on the resolved kind (small_tree, big_tree, small_rock, big_rock, etc.)
            // This allows for dedicated asset variants for different sizes.
            // Users can name their prefabs to include the resolved kind (e.g., "tree_small", "tree_big", "rock_small", "rock_big")
            if (!string.IsNullOrWhiteSpace(resolvedKind))
            {
                string normalizedResolved = resolvedKind.Trim().ToLowerInvariant();
                
                // Check resourcePrefabs list - prefab names should include the resolved kind
                // For example: name a small tree prefab "tree_small" or "small_tree_variant"
                var sizedMatches = resourcePrefabs
                    .Where(p => p != null && p.Prefab != null && 
                           p.Prefab.name.ToLowerInvariant().Contains(normalizedResolved))
                    .ToList();

                if (sizedMatches.Count > 0)
                {
                    return sizedMatches[Random.Range(0, sizedMatches.Count)].Prefab;
                }
            }

            // Fall back to the original VegetationSpawnKind lookup
            // This ensures we always get a generic tree/rock prefab, which will be scaled appropriately
            return GetPrefabForKind(fallbackKind);
        }

        private GameObject GetPrefabForCreature(string creatureId)
        {
            if (prefabRegistry != null && prefabRegistry.TryGetCreaturePrefab(creatureId, out GameObject registryPrefab))
            {
                return registryPrefab;
            }

            var matches = creaturePrefabs
                .Where(p => p != null
                            && p.Prefab != null
                            && string.Equals(p.CreatureId.Trim(), (creatureId ?? string.Empty).Trim(), System.StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0) return null;
            return matches[Random.Range(0, matches.Count)].Prefab;
        }

        private GameObject CreateFallbackPrimitive(VegetationSpawnKind kind, Vector3 position)
        {
            GameObject go;
            Color color = Color.green;

            switch (kind)
            {
                case VegetationSpawnKind.ConiferTree:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.position = position + Vector3.up * 1f;
                    go.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                    color = new Color(0.06f, 0.24f, 0.10f);
                    break;
                case VegetationSpawnKind.LeafyTree:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.position = position + Vector3.up * 1f;
                    go.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                    color = new Color(0.16f, 0.46f, 0.16f);
                    break;
                case VegetationSpawnKind.DryTree:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.transform.position = position + Vector3.up * 1f;
                    go.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                    color = new Color(0.52f, 0.34f, 0.16f);
                    break;
                case VegetationSpawnKind.Rock:
                    go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = position + Vector3.up * 0.5f;
                    color = new Color(0.45f, 0.45f, 0.42f);
                    break;
                case VegetationSpawnKind.BerryBush:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.position = position + Vector3.up * 0.25f;
                    go.transform.localScale = Vector3.one * 0.6f;
                    color = new Color(0.20f, 0.50f, 0.20f);
                    break;
                case VegetationSpawnKind.DryBush:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.position = position + Vector3.up * 0.25f;
                    go.transform.localScale = Vector3.one * 0.5f;
                    color = new Color(0.48f, 0.33f, 0.14f);
                    break;
                default:
                    go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.position = position + Vector3.up * 0.25f;
                    go.transform.localScale = Vector3.one * 0.5f;
                    color = new Color(0.35f, 0.65f, 0.22f);
                    break;
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat.shader = Shader.Find("Standard");
                
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", color);
                else
                    mat.color = color;
                
                renderer.sharedMaterial = mat;
            }

            return go;
        }

        private GameObject CreatePlayer()
        {
            Vector3 spawnPos = Vector3.up * 0.10f;
            if (_allTileCenters.Count > 0)
            {
                // Find actual terrain-surface tile center closest to zero.
                Vector3 nearest = _allTileCenters[0];
                float minDist = new Vector2(nearest.x, nearest.z).sqrMagnitude;
                foreach (var center in _allTileCenters)
                {
                    float d = new Vector2(center.x, center.z).sqrMagnitude;
                    if (d < minDist)
                    {
                        minDist = d;
                        nearest = center;
                    }
                }

                spawnPos = nearest + Vector3.up * 0.10f;
            }

            GameObject player;
            if (playerPrefab != null)
            {
                player = Instantiate(playerPrefab, spawnPos, Quaternion.Euler(0, 45, 0));
                player.name = "Player";
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.transform.position = spawnPos;
            }

            player.tag = "Player";
            player.SetActive(true);
            EnsurePlayerVisible(player);
            SnapObjectToTerrainSurface(player, 0.10f);
            Debug.Log($"[WorldGen] Player spawned: name={player.name}, pos={player.transform.position}, active={player.activeInHierarchy}, renderers={player.GetComponentsInChildren<Renderer>(true).Length}");
            return player;
        }

        private void EnsurePlayerVisible(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
            bool hasVisibleRenderer = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                    hasVisibleRenderer = true;
                }
            }

            if (hasVisibleRenderer)
            {
                return;
            }

            Debug.LogWarning("[WorldGen] Player prefab has no renderers; attaching a visible fallback capsule so the player is not invisible.");
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "PlayerVisualFallback";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = Vector3.up * 0.9f;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(0.85f, 1.1f, 0.85f);
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private void SnapObjectToTerrainSurface(GameObject target, float surfaceOffset)
        {
            if (target == null)
            {
                return;
            }

            Vector3 origin = target.transform.position + Vector3.up * 20f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 60f))
            {
                target.transform.position = hit.point + Vector3.up * Mathf.Max(0.02f, surfaceOffset);
            }
        }

        private void ConfigurePlayerRuntime(GameObject player, GameObject cameraGo)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = player.AddComponent<CharacterController>();
                cc.height = 2f;
                cc.radius = 0.5f;
                cc.center = Vector3.up * (cc.height * 0.5f);
            }
            else
            {
                cc.center = Vector3.up * (cc.height * 0.5f);
            }

            PlayerInputReader inputReader = player.GetComponent<PlayerInputReader>();
            if (inputReader == null) inputReader = player.AddComponent<PlayerInputReader>();

            PlayerPresenceRuntime presence = player.GetComponent<PlayerPresenceRuntime>();
            if (presence == null) presence = player.AddComponent<PlayerPresenceRuntime>();
            presence.MarkActive();
            
            if (inputActions != null)
            {
                inputReader.SetInputActions(inputActions);
            }
            else
            {
#if UNITY_EDITOR
                InputActionAsset loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(DefaultInputActionsPath);
                if (loaded != null) inputReader.SetInputActions(loaded);
#endif
            }

            PlayerSurvivalRuntime survival = player.GetComponent<PlayerSurvivalRuntime>();
            if (survival == null) survival = player.AddComponent<PlayerSurvivalRuntime>();
            survival.SetInputReader(inputReader);

            PlayerInventoryRuntime inventory = player.GetComponent<PlayerInventoryRuntime>();
            if (inventory == null) inventory = player.AddComponent<PlayerInventoryRuntime>();

            PlayerCraftingRuntime crafting = player.GetComponent<PlayerCraftingRuntime>();
            if (crafting == null) crafting = player.AddComponent<PlayerCraftingRuntime>();
            crafting.SetInputReader(inputReader);
            crafting.SetInventoryRuntime(inventory);

            ApexShift.Runtime.Player.PlayerInventoryPanelRuntime inventoryPanel = player.GetComponent<ApexShift.Runtime.Player.PlayerInventoryPanelRuntime>();
            if (inventoryPanel == null) inventoryPanel = player.AddComponent<ApexShift.Runtime.Player.PlayerInventoryPanelRuntime>();
            inventoryPanel.SetInputReader(inputReader);
            inventoryPanel.SetInventoryRuntime(inventory);

            ActionBarRuntime actionBar = player.GetComponent<ActionBarRuntime>();
            if (actionBar == null) actionBar = player.AddComponent<ActionBarRuntime>();
            actionBar.SetInventoryRuntime(inventory);
            actionBar.SetInputReader(inputReader);

            CraftingPanelUI craftingPanel = player.GetComponent<CraftingPanelUI>();
            if (craftingPanel == null) craftingPanel = player.AddComponent<CraftingPanelUI>();
            craftingPanel.SetInputReader(inputReader);
            craftingPanel.SetCraftingRuntime(crafting);
            craftingPanel.SetInventoryRuntime(inventory);

            PlayerCombatRuntime combat = player.GetComponent<PlayerCombatRuntime>();
            if (combat == null)
            {
                combat = player.AddComponent<PlayerCombatRuntime>();
            }
            combat.SetInputReader(inputReader);
            combat.SetInventoryRuntime(inventory);
            combat.SetSurvivalRuntime(survival);
            combat.SetAttackOrigin(player.transform);

            PlayerCombatExperienceRuntime combatExperience = player.GetComponent<PlayerCombatExperienceRuntime>();
            if (combatExperience == null)
            {
                combatExperience = player.AddComponent<PlayerCombatExperienceRuntime>();
            }
            combatExperience.SetInputReader(inputReader);
            combatExperience.SetVisualRoot(player.transform.childCount > 0 ? player.transform.GetChild(0) : player.transform);

            IsometricPlayerController controller = player.GetComponent<IsometricPlayerController>();
            if (controller == null) controller = player.AddComponent<IsometricPlayerController>();
            controller.SetInputReader(inputReader);
            controller.SetSurvivalRuntime(survival);

            PlayerInteractionController interaction = player.GetComponent<PlayerInteractionController>();
            if (interaction == null) interaction = player.AddComponent<PlayerInteractionController>();
            interaction.SetInputReader(inputReader);
            interaction.SetInteractionOrigin(player.transform);

            PlayerAnimationDriver animDriver = player.GetComponent<PlayerAnimationDriver>();
            if (animDriver == null) animDriver = player.AddComponent<PlayerAnimationDriver>();
            animDriver.SetInputReader(inputReader);
            
            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                if (playerAnimatorController != null) anim.runtimeAnimatorController = playerAnimatorController;
                animDriver.SetAnimator(anim);
            }

            // Disable demo scripts from asset packs
            var viewer = player.GetComponentInChildren<MonoBehaviour>(true);
            if (viewer != null && viewer.GetType().Name == "UniversalAnimationViewer")
            {
                viewer.enabled = false;
            }

            PlayerActionFeedback feedback = player.GetComponent<PlayerActionFeedback>();
            if (feedback == null) feedback = player.AddComponent<PlayerActionFeedback>();
            feedback.SetInputReader(inputReader);
            feedback.SetVisualRenderer(player.GetComponentInChildren<Renderer>());

            PlayerMotionVisualFeedback motionFeedback = player.GetComponent<PlayerMotionVisualFeedback>();
            if (motionFeedback == null) motionFeedback = player.AddComponent<PlayerMotionVisualFeedback>();
            motionFeedback.SetInputReader(inputReader);
            motionFeedback.SetVisualRoot(player.transform.childCount > 0 ? player.transform.GetChild(0) : player.transform);

            PlayerActionDebugLog debugLog = player.GetComponent<PlayerActionDebugLog>();
            if (debugLog == null) debugLog = player.AddComponent<PlayerActionDebugLog>();
            debugLog.SetInputReader(inputReader);
            debugLog.SetWatchedTarget(player.transform);
            debugLog.SetSecondaryTarget(cameraGo != null ? cameraGo.transform : null);
            debugLog.SetMovementController(controller);
            debugLog.SetMotionFeedback(motionFeedback);
            debugLog.SetCameraFollow(cameraGo != null ? cameraGo.GetComponent<IsometricCameraFollow>() : null);

            BuildingPlacementRuntime buildingPlacement = player.GetComponent<BuildingPlacementRuntime>();
            if (buildingPlacement == null)
            {
                buildingPlacement = player.AddComponent<BuildingPlacementRuntime>();
            }

            buildingPlacement.SetInventoryRuntime(inventory);
            buildingPlacement.SetPrefabRegistry(prefabRegistry);
            buildingPlacement.SetBuildingRegistry(_buildingRoot != null ? _buildingRoot.GetComponent<BuildingRegistry>() : BuildingRegistry.Active);
            buildingPlacement.SetPlacementOrigin(player.transform);
            buildingPlacement.SetBuildingParent(_buildingRoot);
            inputReader.SetBuildingPlacementRuntime(buildingPlacement);
            combatExperience.SetBuildingPlacementRuntime(buildingPlacement);

            BuildingSelectionPanelUI selectionPanel = player.GetComponent<BuildingSelectionPanelUI>();
            if (selectionPanel == null)
            {
                selectionPanel = player.AddComponent<BuildingSelectionPanelUI>();
            }

            selectionPanel.SetPlacementRuntime(buildingPlacement);
            selectionPanel.SetInventoryRuntime(inventory);
            buildingPlacement.SetSelectionPanel(selectionPanel);
        }

        private void EnsurePlayerWorldVisuals(GameObject player)
        {
            if (player == null)
            {
                return;
            }

            Canvas accidentalCanvas = player.GetComponent<Canvas>();
            if (accidentalCanvas != null)
            {
                Destroy(accidentalCanvas);
            }

            GraphicRaycaster accidentalRaycaster = player.GetComponent<GraphicRaycaster>();
            if (accidentalRaycaster != null)
            {
                Destroy(accidentalRaycaster);
            }

            CanvasScaler accidentalScaler = player.GetComponent<CanvasScaler>();
            if (accidentalScaler != null)
            {
                Destroy(accidentalScaler);
            }

            if (player.GetComponentInChildren<Renderer>() != null)
            {
                return;
            }

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "PlayerFallbackVisual";
            visual.transform.SetParent(player.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            visual.transform.localScale = new Vector3(0.55f, 0.9f, 0.55f);

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat.shader = Shader.Find("Standard");
                Color color = new Color(0.25f, 0.55f, 0.95f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                else mat.color = color;
                renderer.sharedMaterial = mat;
            }
        }

        private GameObject CreateCamera(Transform target)
        {
            if (useCinemachine)
            {
                Debug.Log($"[WorldGen] Creating Cinemachine camera for target={(target != null ? target.name : "<null>")} at {(target != null ? target.position.ToString() : "<null>")}");
                return CreateCinemachineRig(target);
            }

            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            CameraComponent cam = go.AddComponent<CameraComponent>();
            cam.orthographic = true;
            cam.orthographicSize = 14f;
            EnsureSingleAudioListener(go);
            
            System.Type cameraDataType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (cameraDataType != null) {
                var data = go.AddComponent(cameraDataType);
                var prop = cameraDataType.GetProperty("renderType");
                if (prop != null) prop.SetValue(data, 0);
            }

            IsometricCameraFollow follow = go.AddComponent<IsometricCameraFollow>();
            follow.SetTarget(target);
            follow.SetInitialRotation(Quaternion.Euler(35.264f, 45f, 0f));
            follow.SnapToTarget();
            Debug.Log($"[WorldGen] Main Camera positioned at {go.transform.position}, target={(target != null ? target.name : "<null>")} targetPos={(target != null ? target.position.ToString() : "<null>")}");

            GameObject lightGo = new GameObject("Directional Light");
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            return go;
        }

        private GameObject CreateCinemachineRig(Transform target)
        {
            float pitch = 35.264f;
            float yaw = 45f;
            float orthographicSize = 14f;
            float followDistance = 20f;
            Vector3 focusOffset = new Vector3(0f, 1.25f, 0f);
            Quaternion rigRotation = Quaternion.Euler(pitch, yaw, 0f);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.rotation = rigRotation;

            CameraComponent camera = cameraObject.AddComponent<CameraComponent>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            EnsureSingleAudioListener(cameraObject);
            
            // Add URP data safely via reflection
            System.Type cameraDataType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (cameraDataType != null) {
                var data = cameraObject.AddComponent(cameraDataType);
                var prop = cameraDataType.GetProperty("renderType");
                if (prop != null) prop.SetValue(data, 0);
            }

            cameraObject.AddComponent<CinemachineBrain>();

            GameObject followCamera = new GameObject("PlayerFollowCamera");
            followCamera.transform.rotation = rigRotation;

            Vector3 cameraOffset = -(rigRotation * Vector3.forward) * followDistance + focusOffset;
            followCamera.transform.position = target != null ? target.position + cameraOffset : cameraOffset;

            CinemachineCamera cinemachineCamera = followCamera.AddComponent<CinemachineCamera>();
            cinemachineCamera.Target.TrackingTarget = target;
            cinemachineCamera.Target.LookAtTarget = target;
            
            LensSettings lens = LensSettings.FromCamera(camera);
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = orthographicSize;
            cinemachineCamera.Lens = lens;
            cinemachineCamera.Priority.Value = 20;

            CinemachineFollow follow = followCamera.AddComponent<CinemachineFollow>();
            follow.FollowOffset = cameraOffset;
            followCamera.AddComponent<CinemachineOrthographicZoom>();
            Debug.Log($"[WorldGen] Cinemachine rig created. MainCamera={cameraObject.transform.position}, FollowCamera={followCamera.transform.position}, target={(target != null ? target.name : "<null>")} targetPos={(target != null ? target.position.ToString() : "<null>")}");

            GameObject lightGo = new GameObject("Directional Light");
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            return cameraObject;
        }

        private static void EnsureSingleAudioListener(GameObject cameraObject)
        {
            if (cameraObject == null)
            {
                return;
            }

            AudioListener listener = cameraObject.GetComponent<AudioListener>();
            if (listener == null)
            {
                listener = cameraObject.AddComponent<AudioListener>();
            }

            AudioListener[] allListeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            bool keptFirst = false;
            foreach (AudioListener item in allListeners)
            {
                if (item == null)
                {
                    continue;
                }

                if (!keptFirst)
                {
                    keptFirst = true;
                    item.enabled = true;
                    continue;
                }

                if (item.gameObject != cameraObject)
                {
                    item.enabled = false;
                }
            }
        }

        private void CreateWorldBounds()
        {
            GameObject go = new GameObject("WorldBounds");
            go.transform.SetParent(transform);
            WorldBounds bounds = go.AddComponent<WorldBounds>();
            bounds.Configure(8f, _allTileCenters);
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
            // Move lower to avoid overlapping with resources panel
            GUILayout.BeginArea(new Rect(Screen.width - 310, 450, 300, 200));
            GUILayout.Label($"Seed: {_lastResult.Seed}");
            GUILayout.Label($"Biomes: {_lastResult.BiomeCount}");
            GUILayout.Label($"Resources: {_lastResult.ResourceCount}");
            GUILayout.Label($"Spawn Attempts: {_lastResult.SpawnAttempts}");
            GUILayout.EndArea();
        }
}
}
