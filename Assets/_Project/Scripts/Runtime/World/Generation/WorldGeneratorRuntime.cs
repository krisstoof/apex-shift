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
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
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
            EnsureRoots();
            GenerateFixedLayout();
            
            GameObject player = CreatePlayer();
            GameObject cameraGo = CreateCamera(player.transform);
            CreateWorldBounds();

            ConfigurePlayerRuntime(player, cameraGo);

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
            var objects = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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

        private void GenerateFixedLayout()
        {
            if (biomeCatalog == null)
            {
                Debug.LogWarning("No BiomeCatalogAsset assigned to WorldGeneratorRuntime.");
                return;
            }

            float size = settings?.RegionSize ?? 40f;

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
            SpawnRegionCreatures(region);

            _allTileCenters.Add(center);
            float halfSize = size * 0.5f;
            _allTileCenters.Add(center + new Vector3(halfSize, 0, 0));
            _allTileCenters.Add(center + new Vector3(-halfSize, 0, 0));
            _allTileCenters.Add(center + new Vector3(0, 0, halfSize));
            _allTileCenters.Add(center + new Vector3(0, 0, -halfSize));
        }

        private void CreateTerrainPlane(GeneratedBiomeRegion region)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = $"Terrain_{region.Biome.BiomeId}";
            plane.transform.SetParent(_terrainRoot);
            plane.transform.position = region.Bounds.center;
            float scale = region.Bounds.size.x / 10f;
            plane.transform.localScale = new Vector3(scale, 1f, scale);

            if (region.Biome.GroundMaterial != null)
            {
                plane.GetComponent<Renderer>().sharedMaterial = region.Biome.GroundMaterial;
            }
            else
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null) mat.shader = Shader.Find("Standard");
                
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", region.Biome.GroundColor);
                else
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
            spawnBounds.Expand(new Vector3(-padding * 2, 0, -padding * 2));

            foreach (var entry in region.Biome.Creatures)
            {
                if (entry == null) continue;

                int count = Random.Range(entry.MinCount, entry.MaxCount + 1);
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos = GetRandomPointInBounds(spawnBounds);
                    if (pos.magnitude < clearingRadius) continue;
                    SpawnCreature(entry, pos);
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

            _lastResult.ResourceCount++;
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
                instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                instance.name = $"Creature_{entry.CreatureId}_Fallback";
                instance.transform.position = position + Vector3.up * 0.5f;
                instance.transform.SetParent(_creatureRoot);
                
                Renderer renderer = instance.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    if (mat.shader == null) mat.shader = Shader.Find("Standard");
                    mat.color = Color.red;
                    renderer.sharedMaterial = mat;
                }
            }

            instance.name = $"Creature_{entry.CreatureId}";
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
            GameObject player;
            if (playerPrefab != null)
            {
                player = Instantiate(playerPrefab, Vector3.up * 0.05f, Quaternion.Euler(0, 45, 0));
                player.name = "Player";
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.transform.position = new Vector3(0, 1, 0);
            }

            return player;
        }

        private void ConfigurePlayerRuntime(GameObject player, GameObject cameraGo)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = player.AddComponent<CharacterController>();
                cc.center = new Vector3(0, 0, 0);
                cc.height = 2f;
                cc.radius = 0.5f;
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
            bounds.Configure(settings?.RegionSize ?? 40f, _allTileCenters);
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
