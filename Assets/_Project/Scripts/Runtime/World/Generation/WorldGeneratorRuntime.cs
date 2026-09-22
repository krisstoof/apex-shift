using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexShift.Runtime.Bootstrap;
using ApexShift.Runtime.Camera;
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
using ApexShift.Runtime.Fire;
using ApexShift.Runtime.World.Query;
using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.World.Sky;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Landmarks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.World.Generation
{
    public sealed class WorldGeneratorRuntime : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private BiomeCatalogAsset biomeCatalog;
        [SerializeField] private WorldGenerationSettings settings;
        [SerializeField] private PrefabRegistry prefabRegistry;

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
        [SerializeField] private int varnakAddEveryDays = 1;
        [SerializeField] private int varnakAbsoluteMaxCount = 5;
        [SerializeField] private float varnakDayOneSpawnMultiplier = 0.05f;
        [SerializeField] private float varnakSpawnMultiplierPerDay = 0.10f;
        [SerializeField] private float varnakMaxSpawnMultiplier = 0.65f;
        [SerializeField] private float minCreatureDistanceFromPlayer = 30f;
        [SerializeField] private float minVarnakDistanceFromPlayer = 48f;
        [SerializeField] private int creatureSpawnPositionAttempts = 36;
        [SerializeField] private int nonVarnakMinimumPerBiomeEntry = 1;

        [Header("Daily Varnak Spawning")]
        [SerializeField] private bool spawnVarnaksOnDayChange = true;
        [SerializeField] private int firstVarnakSpawnDay = 2;
        [SerializeField] private int varnakDailySpawnAttempts = 8;
        [SerializeField] private float varnakDailySpawnChanceMultiplier = 1.35f;

        [Header("Resource Size / Tool Gating")]
        [SerializeField] private float bigTreeScaleThreshold = 0.92f;
        [SerializeField] private float bigRockScaleThreshold = 0.88f;
        [SerializeField] private float bigTreeVisualScaleMultiplier = 1.18f;
        [SerializeField] private float bigRockVisualScaleMultiplier = 1.16f;
        [SerializeField] private float smallResourceVisualScaleMultiplier = 0.82f;

        [Header("Terrain")]
        [SerializeField] private Material seabedMaterial;
        [SerializeField] private Material waterSurfaceMaterial;
        [SerializeField] private Material cliffMaterial;

        [Header("Settings")]
        [SerializeField] private bool generateOnStart = false;
        [SerializeField] private int seed = 12345;
        [SerializeField] private bool useCinemachine = true;
        [SerializeField]
        [Tooltip("Destroy generated runtime objects immediately before rebuilding the world. This prevents delayed Destroy() from deleting or shadowing freshly regenerated objects during save/load and PlayMode smoke tests.")]
        private bool destroyGeneratedObjectsImmediately = true;
        [SerializeField] private float clearingRadius = 8f;

        private WorldGenerationResult _lastResult;
        private Transform _terrainRoot;
        private Transform _biomeRoot;
        private Transform _resourceRoot;
        private Transform _creatureRoot;
        private Transform _buildingRoot;
        private Transform _landmarkRoot;
        private List<Vector3> _allTileCenters = new List<Vector3>();
        private List<Vector3> _landTileCenters = new List<Vector3>();
        private Transform _playerTransform;
        private int _spawnedVarnakCount;
        private int _currentSpawnDay = 1;
        private IslandTopographyRuntime _islandTopography;
        private DayNightRuntime _dayNightRuntime;
        private WorldRuntimeOwner _runtimeOwner;
        private WorldGenerationContext _generationContext;
        private WorldGenerationCoordinator _generationCoordinator;
        private RuntimeCompositionRoot _runtimeComposition;
        private RuntimeAudioSetup _runtimeAudio;
        private RuntimeCameraSetup _runtimeCamera;
        private WorldSpawnService _worldSpawnService;
        private TerrainHeightfieldGenerator _terrainHeightfield;
        private BiomeFieldGenerator _biomeField;
        private BiomeClassifier _biomeClassifier;

        private const string DefaultInputActionsPath = "Assets/_Project/Input/ApexShiftInputActions.inputactions";

        public event System.Action<GameObject> OnGenerationComplete;
        public int Seed => seed;
        public InputActionAsset InputActions => inputActions;
        public WorldGenerationContext CurrentGeneration => _generationContext;
        public IReadOnlyList<string> LastGenerationStageOrder => _generationCoordinator != null
            ? _generationCoordinator.LastStageOrder
            : System.Array.Empty<string>();

        private Transform CurrentGenerationParent => _runtimeOwner != null && _runtimeOwner.GenerationRoot != null
            ? _runtimeOwner.GenerationRoot
            : transform;

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
            EnsureRuntimeOwner();
            _generationContext = _runtimeOwner.BeginGeneration(seed);
            _generationCoordinator = new WorldGenerationCoordinator();
            _worldSpawnService = new WorldSpawnService();

            _lastResult = new WorldGenerationResult { Seed = seed };
            _terrainHeightfield = new TerrainHeightfieldGenerator(seed, settings != null ? settings.Terrain : null);
            _biomeField = new BiomeFieldGenerator(seed, settings != null ? settings.Biome : null);
            _biomeClassifier = new BiomeClassifier(seed, settings != null ? settings.Biome : null);
            _generationContext.Result = _lastResult;
            _generationCoordinator.Generate(_generationContext,
                new WorldGenerationStage("PrepareGeneration", context =>
                {
                    _runtimeComposition = new RuntimeCompositionRoot();
                    _runtimeComposition.Compose(CurrentGenerationParent, this);
                    UnsubscribeFromDayNightRuntime();
                    _dayNightRuntime = CurrentGenerationParent.GetComponentInChildren<DayNightRuntime>(true);
                    if (_dayNightRuntime != null) _dayNightRuntime.DayChanged += HandleDayChanged;
                    context.DayNight = _dayNightRuntime;
                }),
                new WorldGenerationStage("CreateWorldRoots", context =>
                {
                    EnsureRoots();
                    EnsureBuildingRegistry();
                }),
                new WorldGenerationStage("GenerateTerrainAndBiomes", context => GenerateIslandLayout()),
                // Resources historically spawned while regions were being added. They
                // now have a real stage, while remaining before landmarks to preserve
                // the previous seeded random sequence.
                new WorldGenerationStage("SpawnResources", context => SpawnAllRegionResources()),
                new WorldGenerationStage("GenerateLandmarks", context => GenerateLandmarks()),
                new WorldGenerationStage("SpawnPlayer", context =>
                {
                    context.Player = CreatePlayer();
                    _playerTransform = context.Player != null ? context.Player.transform : null;
                    _currentSpawnDay = ResolveCurrentDay();
                    _spawnedVarnakCount = 0;
                }),
                new WorldGenerationStage("ConfigureCamera", context =>
                {
                    _runtimeAudio = new RuntimeAudioSetup();
                    _runtimeAudio.Compose(CurrentGenerationParent, biomeCatalog, enableAmbientMusic, ambientMusicVolume);
                    _runtimeCamera = new RuntimeCameraSetup();
                    context.MainCamera = _runtimeCamera.Create(CurrentGenerationParent,
                        context.Player != null ? context.Player.transform : null, useCinemachine);
                    CreateWorldBounds();
                    context.WorldBounds = GetComponentInChildren<WorldBounds>(true);
                    ConfigurePlayerRuntime(context.Player, context.MainCamera);
                    InitializeEcosystemDirector();
                }),
                new WorldGenerationStage("BuildNavMesh", context => WorldNavMeshBuildStage.Execute(context)),
                new WorldGenerationStage("SpawnCreatures", context =>
                {
                    EnsureCreatureIslandBoundsRuntime();
                    SpawnAllRegionCreatures();
                }),
                new WorldGenerationStage("FinalizeGeneration", context =>
                {
                    context.Result = _lastResult;
                    Debug.Log($"World Generation Complete. Biomes: {_lastResult.BiomeCount}, Resources: {_lastResult.ResourceCount}, Seed: {seed}");
                    OnGenerationComplete?.Invoke(context.Player);
                }));
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
            if (_runtimeOwner == null)
            {
                _runtimeOwner = GetComponent<WorldRuntimeOwner>();
            }

            if (_dayNightRuntime != null)
            {
                _dayNightRuntime.DayChanged -= HandleDayChanged;
                _dayNightRuntime = null;
            }
            if (_runtimeOwner != null) _runtimeOwner.Clear();
            _generationContext = null;
            _terrainRoot = null;
            _biomeRoot = null;
            _resourceRoot = null;
            _creatureRoot = null;
            _buildingRoot = null;
            _landmarkRoot = null;
            _playerTransform = null;
            _islandTopography = null;
        }

        private void EnsureRuntimeOwner()
        {
            if (_runtimeOwner == null)
            {
                _runtimeOwner = GetComponent<WorldRuntimeOwner>();
                if (_runtimeOwner == null) _runtimeOwner = gameObject.AddComponent<WorldRuntimeOwner>();
            }
            _runtimeOwner.Configure(destroyGeneratedObjectsImmediately);
        }

        private void EnsureRoots()
        {
            _terrainRoot = CreateRoot("TerrainRoot");
            _biomeRoot = CreateRoot("BiomeRoot");
            _resourceRoot = CreateRoot("ResourceRoot");
            _creatureRoot = CreateRoot("CreatureRoot");
            _buildingRoot = CreateRoot("BuildingRoot");
            _landmarkRoot = CreateRoot("LandmarkRoot");
            if (_generationContext != null)
            {
                _generationContext.TerrainRoot = _terrainRoot;
                _generationContext.BiomeRoot = _biomeRoot;
                _generationContext.ResourceRoot = _resourceRoot;
                _generationContext.CreatureRoot = _creatureRoot;
                _generationContext.BuildingRoot = _buildingRoot;
                _generationContext.LandmarkRoot = _landmarkRoot;
            }
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

        private void EnsureIslandTopographyRuntime()
        {
            if (_islandTopography != null) return;
            _islandTopography = _runtimeOwner != null && _runtimeOwner.GenerationRoot != null
                ? _runtimeOwner.GenerationRoot.GetComponentInChildren<IslandTopographyRuntime>(true)
                : null;
            if (_islandTopography == null)
            {
                GameObject go = new GameObject("IslandTopographyRuntime");
                go.transform.SetParent(CurrentGenerationParent, false);
                _islandTopography = go.AddComponent<IslandTopographyRuntime>();
            }
            if (_generationContext != null) _generationContext.IslandTopography = _islandTopography;
        }

        private void EnsureCreatureIslandBoundsRuntime()
        {
            CreatureIslandBoundsRuntime bounds = _runtimeOwner != null && _runtimeOwner.GenerationRoot != null
                ? _runtimeOwner.GenerationRoot.GetComponentInChildren<CreatureIslandBoundsRuntime>(true)
                : null;
            if (bounds == null)
            {
                GameObject go = new GameObject("CreatureIslandBoundsRuntime");
                go.transform.SetParent(CurrentGenerationParent, false);
                bounds = go.AddComponent<CreatureIslandBoundsRuntime>();
            }

            // Use topography safe creature centres if available, otherwise fall back to all land centres.
            List<Vector3> creatureCenters = _islandTopography != null
                ? _islandTopography.GetSafeCreatureLandCenters()
                : _allTileCenters;

            bounds.Configure(creatureCenters, 5.85f);
        }

        private Transform CreateRoot(string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(_runtimeOwner != null && _runtimeOwner.GenerationRoot != null
                ? _runtimeOwner.GenerationRoot
                : transform, false);
            return go.transform;
        }

        private void InitializeEcosystemDirector()
        {
            EcosystemDirectorRuntime director = _runtimeOwner != null && _runtimeOwner.GenerationRoot != null
                ? _runtimeOwner.GenerationRoot.GetComponentInChildren<EcosystemDirectorRuntime>(true)
                : null;
            if (director != null && _lastResult != null)
            {
                director.InitializeFromRegions(_lastResult.Regions);
            }
        }

        private float SampleIslandField(float x, float z)
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

            return radiusModifier - distance;
        }

        private bool IsInsideIsland(float x, float z) => SampleIslandField(x, z) >= 0f;

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

            // First pass: determine only land/water. Biomes are assigned by topography below.
            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    Vector3 pos = new Vector3(x * tileSize, 0, z * tileSize) - centerOffset + new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f);
                    
                    bool isLand = IsInsideIsland(pos.x, pos.z);
                    landGrid[x, z] = isLand;

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

            // Third pass: Build the unified natural terrain mesh (replaces per-tile land cubes)
            // Build topography runtime before terrain mesh so other systems can query it immediately
            EnsureIslandTopographyRuntime();
            _islandTopography.Build(gridSize, tileSize, IsInsideIsland, SampleTerrainHeight, null,
                settings != null ? settings.Terrain : null, _biomeField, _biomeClassifier);

            // Create regions from the same classified cells used by every later query.
            for (int z = 0; z < gridSize; z++)
                for (int x = 0; x < gridSize; x++)
                {
                    TopographyCell cell = _islandTopography.GetCell(x, z);
                    if (cell == null) continue;
                    Vector3 pos = cell.WorldCenter;
                    AddTileRegion(cell.IsLand ? cell.BiomeId : "water", pos, tileSize);
                    if (cell.IsLand) _landTileCenters.Add(pos);
                }

            NaturalTerrainBuilder.BuildIslandTerrain(
                _terrainRoot,
                gridSize,
                tileSize,
                biomeCatalog,
                SampleIslandField,
                SampleTerrainHeight,
                _islandTopography.GetBiomeIdAt);

            // Fourth pass: Unified water surface mesh (replaces per-tile water cubes)
            NaturalTerrainBuilder.BuildUnifiedWaterSurface(
                _terrainRoot,
                gridSize,
                tileSize,
                waterSurfaceMaterial,
                waterSurfaceMaterial,
                SampleIslandField);

            // Fifth pass: Build the seabed mesh visible below the water surface
            NaturalTerrainBuilder.BuildSeabed(
                _terrainRoot,
                gridSize,
                tileSize,
                seabedMaterial,
                IsInsideIsland);

            // Sixth pass: Cliff walls at elevated coastlines
            NaturalTerrainBuilder.BuildCliffWalls(
                _terrainRoot,
                gridSize,
                tileSize,
                cliffMaterial,
                biomeCatalog,
                SampleIslandField,
                SampleTerrainHeight,
                _islandTopography.GetBiomeIdAt);
        }

        private void CheckAndAddWall(int nx, int nz, Vector3 pos, Vector3 direction, bool[,] landGrid, int gridSize, float tileSize)
        {
            bool outsideGrid = nx < 0 || nx >= gridSize || nz < 0 || nz >= gridSize;
            if (!outsideGrid)
            {
                // Do not place invisible walls between land and water.
                // The player is allowed to enter shallow water; WorldBounds limits only deep/out-of-world water.
                return;
            }

            if (outsideGrid)
            {
                GameObject wall = new GameObject("IslandWall");
                wall.transform.SetParent(_terrainRoot);
                wall.transform.position = pos + direction * (tileSize * 0.5f) + Vector3.up * 5f;
                BoxCollider col = wall.AddComponent<BoxCollider>();
                col.size = (direction.x != 0) ? new Vector3(0.1f, 10f, tileSize) : new Vector3(tileSize, 10f, 0.1f);
            }
        }

        private void GenerateLandmarks()
        {
            if (_landmarkRoot != null && _islandTopography != null)
            {
                LandmarkWorldGenerator.Generate(_landmarkRoot, _islandTopography, seed);
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

            float terrainHeight = biomeId == "water" ? -0.35f : SampleTerrainHeight(center);
            Vector3 regionCenter = new Vector3(center.x, terrainHeight, center.z);
            Bounds bounds = new Bounds(regionCenter, new Vector3(size, 2f, size));
            GeneratedBiomeRegion region = new GeneratedBiomeRegion(biome, bounds);
            _lastResult.Regions.Add(region);
            _lastResult.BiomeCount++;

            // Land and water tiles: unified meshes handle all rendering and collision.
            // Neither individual cubes nor water surface tiles are created here anymore.
            // Water tiles still need to store region bounds for biome queries.

            if (biomeId != "water")
            {
                // Store the actual terrain surface height, not the original flat y=0 center.
                // Player/creature/resource spawning relies on these centers.
                _allTileCenters.Add(regionCenter);

            }
        }

        private void SpawnAllRegionResources()
        {
            foreach (GeneratedBiomeRegion region in _lastResult.Regions)
            {
                if (region?.Biome == null || region.Biome.BiomeId == "water") continue;
                SpawnRegionResources(region);
            }
        }

        private float SampleTerrainHeight(Vector3 position)
        {
            if (_terrainHeightfield == null)
                _terrainHeightfield = new TerrainHeightfieldGenerator(seed, settings != null ? settings.Terrain : null);
            return _terrainHeightfield.SampleSurfaceHeight(position.x, position.z, IsInsideIsland);
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

                // Enable URP alpha transparency so the seabed is visible through the water surface
                if (waterMaterial.HasProperty("_Surface"))
                    waterMaterial.SetFloat("_Surface", 1f);        // 1 = Transparent
                if (waterMaterial.HasProperty("_Blend"))
                    waterMaterial.SetFloat("_Blend", 0f);          // 0 = Alpha blend
                if (waterMaterial.HasProperty("_AlphaClip"))
                    waterMaterial.SetFloat("_AlphaClip", 0f);
                waterMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                waterMaterial.renderQueue = 3000;

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

            // Add a trigger volume slightly above the water surface so PlayerWaterDetector
            // can switch the player into swim mode when they step onto water.
            float tileHalfSize = center.magnitude > 0.001f
                ? Mathf.Round(8f * 0.5f)   // default tileSize/2
                : 4f;

            GameObject waterTriggerGo = new GameObject("WaterTrigger");
            waterTriggerGo.transform.SetParent(tile.transform);
            waterTriggerGo.transform.localPosition = Vector3.zero;
            waterTriggerGo.layer = LayerMask.NameToLayer("Default");
            // Tag used by PlayerWaterDetector
            if (UnityEngine.Application.isPlaying)
                waterTriggerGo.tag = "Water";

            BoxCollider trigger = waterTriggerGo.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            // Height: from slightly below water surface to just above – shallow swim zone
            trigger.center = Vector3.zero;
            trigger.size   = new Vector3(8f, 1.4f, 8f);
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

                    // Fine-resolution check: spawn position must be inside the island boundary
                    // (topography uses 8-unit grid cells; coastline is defined at ~1.3-unit resolution)
                    if (!IsInsideIsland(pos.x, pos.z)) continue;

                    // Topography guard: skip water/shoreline resource positions
                    if (_islandTopography != null && !_islandTopography.IsSafeForResourceAt(pos.x, pos.z))
                        continue;

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

        private void UnsubscribeFromDayNightRuntime()
        {
            if (_dayNightRuntime != null)
            {
                _dayNightRuntime.DayChanged -= HandleDayChanged;
                _dayNightRuntime = null;
            }
        }

        private void HandleDayChanged(int day)
        {
            _currentSpawnDay = Mathf.Max(1, day);
            TrySpawnVarnaksForDay(_currentSpawnDay);
        }

        private void TrySpawnVarnaksForDay(int day)
        {
            if (!spawnVarnaksOnDayChange || _lastResult == null || _creatureRoot == null)
            {
                return;
            }

            int safeDay = Mathf.Max(1, day);
            if (safeDay < Mathf.Max(2, firstVarnakSpawnDay))
            {
                return;
            }

            int remainingCapacity = GetRemainingVarnakSpawnCapacity();
            if (remainingCapacity <= 0)
            {
                Debug.Log($"[VarnakSpawn] Day {safeDay}: capacity reached ({_spawnedVarnakCount}/{GetVarnakMaxCountForDay(safeDay)}).", this);
                return;
            }

            int spawned = 0;
            int attempts = Mathf.Max(1, varnakDailySpawnAttempts);
            float chance = Mathf.Clamp01(GetVarnakSpawnMultiplierForDay(safeDay) * Mathf.Max(0f, varnakDailySpawnChanceMultiplier));
            CreatureSpawnEntryAsset varnakEntry = new CreatureSpawnEntryAsset("varnak", 1, 1, 1f);

            for (int attempt = 0; attempt < attempts && spawned < remainingCapacity; attempt++)
            {
                if (Random.value > chance)
                {
                    continue;
                }

                if (!TryGetVarnakSpawnPoint(out Vector3 pos))
                {
                    continue;
                }

                SpawnCreature(varnakEntry, pos);
                spawned++;
            }

            if (spawned > 0)
            {
                Debug.Log($"[VarnakSpawn] Day {safeDay}: spawned {spawned}. Total={_spawnedVarnakCount}/{GetVarnakMaxCountForDay(safeDay)}.", this);
            }
        }

        private bool TryGetVarnakSpawnPoint(out Vector3 pos)
        {
            pos = default;
            if (_lastResult == null || _lastResult.Regions == null || _lastResult.Regions.Count == 0)
            {
                return false;
            }

            List<GeneratedBiomeRegion> preferred = _lastResult.Regions
                .Where(region => region?.Biome != null
                                 && region.Biome.BiomeId != "water"
                                 && region.Biome.Creatures != null
                                 && region.Biome.Creatures.Any(entry => entry != null && NormalizeCreatureId(entry.CreatureId) == "varnak"))
                .ToList();

            List<GeneratedBiomeRegion> candidates = preferred.Count > 0
                ? preferred
                : _lastResult.Regions.Where(region => region?.Biome != null && region.Biome.BiomeId != "water").ToList();

            if (candidates.Count == 0)
            {
                return false;
            }

            int attempts = Mathf.Max(8, creatureSpawnPositionAttempts);
            for (int i = 0; i < attempts; i++)
            {
                GeneratedBiomeRegion region = candidates[Random.Range(0, candidates.Count)];
                Bounds spawnBounds = region.Bounds;
                float padding = settings?.Padding ?? 5f;
                float actualPadding = Mathf.Min(padding, region.Bounds.size.x * 0.2f);
                spawnBounds.Expand(new Vector3(-actualPadding * 2f, 0f, -actualPadding * 2f));

                if (TryGetSafeCreatureSpawnPoint(spawnBounds, "varnak", out pos))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsCreatureSpawnPointSafe(Vector3 pos, float minDistanceFromPlayer)
        {
            if (pos.magnitude < clearingRadius) return false;

            // Topography: reject water, shoreline, and out-of-island positions
            if (_islandTopography != null && !_islandTopography.IsSafeForCreatureAt(pos.x, pos.z))
                return false;

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

        /// <summary>Returns a random land point validated by topography (not water, not shoreline).
        /// Falls back to GetRandomPointInBounds if topography is unavailable.</summary>
        private bool TryGetSafeResourcePoint(Bounds bounds, out Vector3 point, int maxAttempts = 8)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                point = GetRandomPointInBounds(bounds);
                if (_islandTopography != null && !_islandTopography.IsSafeForResourceAt(point.x, point.z))
                    continue;
                return true;
            }
            point = bounds.center;
            return false;
        }

        private void SpawnResource(VegetationSpawnEntryAsset entry, Vector3 position)
        {
            float scale = Random.Range(entry.MinScale, entry.MaxScale);
            string resolvedKind = ResolveResourceKind(entry, scale);
            
            GameObject prefab = GetPrefabForResolvedKind(resolvedKind, entry.Kind);
            GameObject instance;

            float yaw = Random.Range(0f, 360f);
            instance = _worldSpawnService.SpawnResource(prefab, position, yaw, _resourceRoot, entry.Kind);

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

            float yaw = Random.Range(0f, 360f);
            instance = _worldSpawnService.SpawnCreature(prefab, entry.CreatureId, position, yaw, _creatureRoot);

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
            return null;
        }

        private GameObject GetPrefabForResolvedKind(string resolvedKind, VegetationSpawnKind fallbackKind)
        {
            // First, try to find a prefab based on the resolved kind (small_tree, big_tree, small_rock, big_rock, etc.)
            // This allows for dedicated asset variants for different sizes.
            // Users can name their prefabs to include the resolved kind (e.g., "tree_small", "tree_big", "rock_small", "rock_big")
            if (!string.IsNullOrWhiteSpace(resolvedKind))
            {
                string normalizedResolved = resolvedKind.Trim().ToLowerInvariant();
                
                // Search both the legacy inspector list and the shared registry. New
                // generated vegetation is stored in PrefabRegistry, so looking only at
                // the legacy list silently makes those variants invisible to generation.
                IEnumerable<ResourcePrefabEntry> candidates = prefabRegistry != null && prefabRegistry.ResourcePrefabs != null
                    ? prefabRegistry.ResourcePrefabs
                    : Enumerable.Empty<ResourcePrefabEntry>();

                var sizedMatches = candidates
                    .Where(p => p != null && p.Prefab != null &&
                           p.Prefab.name.ToLowerInvariant().Contains(normalizedResolved))
                    .GroupBy(p => p.Prefab)
                    .Select(group => group.First())
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

            return null;
        }

        private GameObject CreatePlayer()
        {
            // Use topography to find a safe player spawn point (not shoreline, not ridge).
            Vector3 spawnPos;
            if (_islandTopography != null && _islandTopography.IsBuilt)
            {
                // GetSafePlayerSpawnPoint already returns correct terrain height
                spawnPos = _islandTopography.GetSafePlayerSpawnPoint();
            }
            else if (_allTileCenters.Count > 0)
            {
                Vector3 nearest = _allTileCenters[0];
                float minDist = new Vector2(nearest.x, nearest.z).sqrMagnitude;
                foreach (var center in _allTileCenters)
                {
                    float d = new Vector2(center.x, center.z).sqrMagnitude;
                    if (d < minDist) { minDist = d; nearest = center; }
                }
                spawnPos = nearest;
            }
            else
            {
                spawnPos = Vector3.up * 0.10f;
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

            player.transform.SetParent(CurrentGenerationParent, true);

            player.tag = "Player";
            player.SetActive(true);
            EnsurePlayerVisible(player);
            Debug.Log($"[WorldGen] Player spawned at: {player.transform.position}, topography built: {(_islandTopography?.IsBuilt ?? false)}");
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

        private void ConfigurePlayerRuntime(GameObject player, GameObject cameraGo)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = player.AddComponent<CharacterController>();
                cc.height = 2f;
                cc.radius = 0.4f;
                cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
                cc.slopeLimit = 45f;
                cc.stepOffset = 0.3f;
            }
            else
            {
                cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            }
            Debug.Log($"[WorldGen] CharacterController configured: height={cc.height}, radius={cc.radius}, center={cc.center}");

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

            PlayerHeldItemRuntime heldItem = player.GetComponent<PlayerHeldItemRuntime>();
            if (heldItem == null) heldItem = player.AddComponent<PlayerHeldItemRuntime>();
            heldItem.SetActionBarRuntime(actionBar);
            heldItem.SetInventoryRuntime(inventory);

            PlayerCombatRuntime combat = player.GetComponent<PlayerCombatRuntime>();
            if (combat == null)
            {
                combat = player.AddComponent<PlayerCombatRuntime>();
            }
            combat.SetInputReader(inputReader);
            combat.SetInventoryRuntime(inventory);
            combat.SetSurvivalRuntime(survival);
            combat.SetActionBarRuntime(actionBar);
            combat.SetAttackOrigin(player.transform);
            combat.SetAimCamera(cameraGo != null ? cameraGo.GetComponent<UnityEngine.Camera>() : null);

            PlayerCombatExperienceRuntime combatExperience = player.GetComponent<PlayerCombatExperienceRuntime>();
            if (combatExperience == null)
            {
                combatExperience = player.AddComponent<PlayerCombatExperienceRuntime>();
            }
            combatExperience.SetInputReader(inputReader);
            combatExperience.SetVisualRoot(player.transform.childCount > 0 ? player.transform.GetChild(0) : player.transform);

            TorchRuntime torchRuntime = player.GetComponent<TorchRuntime>();
            if (torchRuntime == null) torchRuntime = player.AddComponent<TorchRuntime>();

            IsometricPlayerController controller = player.GetComponent<IsometricPlayerController>();
            if (controller == null) controller = player.AddComponent<IsometricPlayerController>();
            controller.SetInputReader(inputReader);
            controller.SetSurvivalRuntime(survival);

            // Water detection (PlayerWaterDetector requires IsometricPlayerController + PlayerAnimationDriver)
            if (player.GetComponent<PlayerWaterDetector>() == null)
                player.AddComponent<PlayerWaterDetector>();

            PlayerInteractionController interaction = player.GetComponent<PlayerInteractionController>();
            if (interaction == null) interaction = player.AddComponent<PlayerInteractionController>();
            interaction.SetInputReader(inputReader);
            interaction.SetInteractionOrigin(player.transform);

            PlayerAnimationDriver animDriver = player.GetComponent<PlayerAnimationDriver>();
            if (animDriver == null) animDriver = player.AddComponent<PlayerAnimationDriver>();
            animDriver.SetInputReader(inputReader);

            Animator anim = player.GetComponentInChildren<Animator>();
            if (anim == null)
            {
                // If no animator found in hierarchy, try to get or add it to the root player
                anim = player.GetComponent<Animator>();
                if (anim == null)
                {
                    anim = player.AddComponent<Animator>();
                    Debug.Log("[WorldGen] Added missing Animator component to player");
                }
            }

            if (anim != null)
            {
                if (playerAnimatorController != null)
                {
                    anim.runtimeAnimatorController = playerAnimatorController;
                    Debug.Log($"[WorldGen] Animator controller assigned: {playerAnimatorController.name}");
                }
                else
                {
                    Debug.LogWarning("[WorldGen] No playerAnimatorController assigned in inspector!");
                }
            }
            else
            {
                Debug.LogWarning("[WorldGen] Could not ensure Animator component on player!");
            }

            KevinIglesiasPlayerAnimationBinder animationBinder = player.GetComponent<KevinIglesiasPlayerAnimationBinder>();
            if (animationBinder == null) animationBinder = player.AddComponent<KevinIglesiasPlayerAnimationBinder>();
            animationBinder.Configure(inputReader, anim, playerAnimatorController);
            anim = animationBinder.BoundAnimator;
            if (anim != null)
            {
                animDriver.SetAnimator(anim);
                heldItem.RebindToRigHand();
                Debug.Log("[WorldGen] Animation binder configured successfully");
            }
            else if (playerAnimatorController != null)
            {
                anim = player.GetComponent<Animator>() ?? player.AddComponent<Animator>();
                anim.runtimeAnimatorController = playerAnimatorController;
                animDriver.SetAnimator(anim);
                Debug.Log("[WorldGen] Fallback animator setup completed");
            }
            else
            {
                Debug.LogWarning("[WorldGen] Could not setup animations - no controller available");
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

            if (_runtimeComposition != null)
            {
                _runtimeComposition.SnapshotProvider?.Configure(
                    this,
                    inventory,
                    survival,
                    player.transform,
                    _dayNightRuntime,
                    _runtimeComposition.Ecosystem,
                    _buildingRoot != null ? _buildingRoot.GetComponent<BuildingRegistry>() : null);
            }

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

        private void CreateWorldBounds()
        {
            GameObject go = new GameObject("WorldBounds");
            go.transform.SetParent(CurrentGenerationParent, false);
            WorldBounds bounds = go.AddComponent<WorldBounds>();

            // Include land + shallow water (3 tiles from shore) so the player
            // can enter the water without hitting an invisible wall.
            List<Vector3> navigable = _islandTopography != null
                ? _islandTopography.GetNavigableCenters(3)
                : _allTileCenters;

            bounds.Configure(8f, navigable);
            if (_generationContext != null) _generationContext.WorldBounds = bounds;
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
}
}
