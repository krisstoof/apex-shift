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
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
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
        [SerializeField] private List<ResourcePrefabEntry> resourcePrefabs = new List<ResourcePrefabEntry>();
        [SerializeField] private List<CreaturePrefabEntry> creaturePrefabs = new List<CreaturePrefabEntry>();

        [Header("Assets")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private RuntimeAnimatorController playerAnimatorController;
        [SerializeField] private GameObject playerPrefab;

        [Header("Settings")]
        [SerializeField] private bool generateOnStart = true;
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

        private const string DefaultInputActionsPath = "Assets/_Project/Input/ApexShiftInputActions.inputactions";

        public event System.Action<GameObject> OnGenerationComplete;

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
            _allTileCenters.Clear();

            Random.State oldState = Random.state;
            Random.InitState(seed);

            _lastResult = new WorldGenerationResult { Seed = seed };

            CreateBootstrapper();
            EnsureEcosystemRuntime();
            EnsureWorldMapDebugWindow();
            EnsureRoots();
            GenerateIslandLayout();

            GameObject player = CreatePlayer();
            GameObject cameraGo = CreateCamera(player.transform);
            CreateWorldBounds();

            ConfigurePlayerRuntime(player, cameraGo);

            // Add and build NavMesh
            NavMeshSurface surface = _terrainRoot.GetComponent<NavMeshSurface>();
            if (surface == null) surface = _terrainRoot.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();

            Random.state = oldState;

            Debug.Log($"World Generation Complete. Biomes: {_lastResult.BiomeCount}, Resources: {_lastResult.ResourceCount}, Seed: {seed}");
            
            OnGenerationComplete?.Invoke(player);
        }

        public void SetBiomeCatalog(BiomeCatalogAsset catalog)
        {
            biomeCatalog = catalog;
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
            DestroyAllByName("WorldMapDebugWindow");
            DestroyAllByName("Player");
            DestroyAllByName("Main Camera");
            DestroyAllByName("PlayerFollowCamera");
            DestroyAllByName("Directional Light");
            DestroyAllByName("UI");
            DestroyAllByName("WorldBounds");
            DestroyAllByName("EventSystem");
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
            if (Object.FindAnyObjectByType<EcosystemRuntime>() != null)
            {
                return;
            }

            GameObject go = new GameObject("EcosystemRuntime");
            go.transform.SetParent(transform);
            go.AddComponent<EcosystemRuntime>();
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
                SpawnRegionCreatures(region);
                TrySpawnInitialMeatFoodSource(region, region.Bounds);
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

                int count = Random.Range(entry.MinCount, entry.MaxCount + 1);
                int countToSpawn = 0;
                for (int i = 0; i < count; i++)
                {
                    if (Random.value < spawnProbability)
                    {
                        countToSpawn++;
                    }
                }

                for (int i = 0; i < countToSpawn; i++)
                {
                    Vector3 pos = GetRandomPointInBounds(spawnBounds);
                    if (pos.magnitude < clearingRadius) continue;
                    SpawnCreature(entry, pos);
                }
            }
        }

        private void TrySpawnInitialMeatFoodSource(GeneratedBiomeRegion region, Bounds spawnBounds)
        {
            // Foundation-only meat source for #21.
            // Full "creature death creates meat drop" belongs to the later death/hunting issue.
            if (region == null || region.Biome == null || region.Biome.BiomeId != "redfang_wilds")
            {
                return;
            }

            if (Random.value > 0.08f)
            {
                return;
            }

            Vector3 pos = GetRandomPointInBounds(spawnBounds);
            if (pos.magnitude < clearingRadius)
            {
                return;
            }

            CreateMeatFoodSource(pos);
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

            string rKind = entry.ResourceKind;
            if (string.IsNullOrWhiteSpace(rKind))
            {
                if (entry.Kind == VegetationSpawnKind.GreenBush)
                    rKind = "bush";
                else if (entry.Kind == VegetationSpawnKind.GrassOrFlower)
                    rKind = "";
                else
                    rKind = entry.RoleId;
            }

            if (!string.IsNullOrWhiteSpace(rKind))
            {
                ResourceNodeView nodeView = instance.GetComponent<ResourceNodeView>();
                if (nodeView == null)
                {
                    nodeView = instance.AddComponent<ResourceNodeView>();
                }
                nodeView.ConfigureDefault(rKind);
            }

            AddFoodSourceToResource(entry.Kind, instance);

            _lastResult.ResourceCount++;
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
            var matches = creaturePrefabs.Where(p => p.CreatureId == entry.CreatureId).ToList();
            GameObject prefab = matches.Count > 0 ? matches[Random.Range(0, matches.Count)].Prefab : null;
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
            needs.Configure(entry.CreatureId);

            var health = instance.GetComponent<CreatureHealthRuntime>();
            if (health == null) health = instance.AddComponent<CreatureHealthRuntime>();
            health.Configure(entry.CreatureId);

            var oldFoodSeeking = instance.GetComponent<CreatureFoodSeekingBehavior>();
            if (oldFoodSeeking != null) oldFoodSeeking.enabled = false;

            var oldAwareness = instance.GetComponent<CreaturePlayerAwarenessBehavior>();
            if (oldAwareness != null) oldAwareness.enabled = false;

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

        private void CreateMeatFoodSource(Vector3 position)
        {
            GameObject meat = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            meat.name = $"MeatDrop_{_lastResult.ResourceCount}";
            meat.transform.SetParent(_resourceRoot);
            meat.transform.position = position + Vector3.up * 0.10f;
            meat.transform.localScale = new Vector3(0.45f, 0.20f, 0.45f);

            Renderer renderer = meat.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat.shader = Shader.Find("Standard");

                Color meatColor = new Color(0.55f, 0.08f, 0.06f);
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", meatColor);
                else
                    mat.color = meatColor;

                renderer.sharedMaterial = mat;
            }

            ResourceNodeView nodeView = meat.GetComponent<ResourceNodeView>();
            if (nodeView == null)
            {
                nodeView = meat.AddComponent<ResourceNodeView>();
            }
            nodeView.ConfigureDefault("meat_drop");

            FoodSourceView food = meat.GetComponent<FoodSourceView>();
            if (food == null)
            {
                food = meat.AddComponent<FoodSourceView>();
            }
            food.Configure(ApexShift.Core.Ecosystem.FoodKind.Meat, 20f, 10f);

            _lastResult.ResourceCount++;
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
            var matches = resourcePrefabs.Where(p => p.Kind == kind).ToList();
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

            SnapObjectToTerrainSurface(player, 0.10f);
            return player;
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
        }

        private GameObject CreateCamera(Transform target)
        {
            if (useCinemachine)
            {
                return CreateCinemachineRig(target);
            }

            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            CameraComponent cam = go.AddComponent<CameraComponent>();
            cam.orthographic = true;
            cam.orthographicSize = 14f;

            IsometricCameraFollow follow = go.AddComponent<IsometricCameraFollow>();
            follow.SetTarget(target);
            follow.SetInitialRotation(Quaternion.Euler(35.264f, 45f, 0f));
            follow.SnapToTarget();

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

            GameObject lightGo = new GameObject("Directional Light");
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            return cameraObject;
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
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Seed: {_lastResult.Seed}");
            GUILayout.Label($"Biomes: {_lastResult.BiomeCount}");
            GUILayout.Label($"Resources: {_lastResult.ResourceCount}");
            GUILayout.Label($"Spawn Attempts: {_lastResult.SpawnAttempts}");
            GUILayout.EndArea();
        }
    }
}
