using System.IO;
using System;
using ApexShift.Runtime.Bootstrap;
using ApexShift.Runtime.Camera;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace ApexShift.EditorTools
{
    public static class ApexShiftSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string MaterialPath = "Assets/_Project/Materials/Ground_Test_Material.mat";
        private const string PlayerPrefabPath = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Prefab/Player.prefab";
        private const string InputActionsPath = "Assets/_Project/Input/ApexShiftInputActions.inputactions";
        private const string PlayerControllerPath = "Assets/_Project/Animations/Player/PlayerPrototype.controller";
        private static readonly Quaternion PlayerFacingRotation = Quaternion.Euler(0f, 45f, 0f);

        [MenuItem("Tools/Apex Shift/Create Base Playable Scene")]
        public static void CreateBasePlayableScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Create Base Playable Scene can only run in Edit mode. Exit Play mode first.");
                return;
            }

            EnsureFolders();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject gameRoot = new GameObject("Game");
            SceneManager.MoveGameObjectToScene(gameRoot, scene);

            CreateBootstrapper(gameRoot.transform);
            GameObject worldRoot = CreateChild(gameRoot.transform, "WorldRoot");
            GameObject terrainRoot = CreateChild(worldRoot.transform, "TerrainRoot");
            CreateChild(worldRoot.transform, "ResourceRoot");
            CreateChild(worldRoot.transform, "CreatureRoot");
            CreateChild(worldRoot.transform, "BuildingRoot");

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(terrainRoot.transform, false);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ApplyGroundMaterial(ground);

            GameObject player = LoadOrCreatePlayer();
            player.transform.SetParent(gameRoot.transform, false);
            player.transform.localPosition = new Vector3(0f, 0f, 0f);
            player.transform.localScale = Vector3.one * 0.85f;
            player.transform.localRotation = PlayerFacingRotation;
            RemoveDemoViewerComponents(player);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(gameRoot.transform, false);
            cameraObject.transform.position = new Vector3(0f, 8f, -8f);
            cameraObject.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            IsometricCameraFollow follow = cameraObject.AddComponent<IsometricCameraFollow>();
            follow.SetTarget(player.transform);

            ConfigurePlayerRuntime(player, cameraObject);

            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(gameRoot.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;

            CreateChild(gameRoot.transform, "UI");
            CreateChild(gameRoot.transform, "DebugRoot");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath);

            Debug.Log("Apex Shift base playable scene created at " + ScenePath);
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Scenes");
            EnsureFolder("Assets/_Project/Materials");
            EnsureFolder("Assets/_Project/Scripts");
            EnsureFolder("Assets/_Project/Scripts/Editor");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void CreateBootstrapper(Transform parent)
        {
            GameObject bootstrapper = new GameObject("GameBootstrapper");
            bootstrapper.transform.SetParent(parent, false);
            bootstrapper.AddComponent<GameBootstrapper>();
        }

        private static void ApplyGroundMaterial(GameObject ground)
        {
            Material material = LoadOrCreateGroundMaterial();
            if (material == null)
            {
                return;
            }

            MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static GameObject LoadOrCreatePlayer()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.name = "Player";
                return fallback;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "Player";
            return instance;
        }

        private static void RemoveDemoViewerComponents(GameObject player)
        {
            MonoBehaviour[] components = player.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                if (component != null && component.GetType().Name == "UniversalAnimationViewer")
                {
                    UnityEngine.Object.DestroyImmediate(component);
                }
            }
        }

        private static void ConfigurePlayerRuntime(GameObject player, GameObject cameraObject)
        {
            PlayerInputReader inputReader = player.GetComponent<PlayerInputReader>();
            if (inputReader == null)
            {
                inputReader = player.AddComponent<PlayerInputReader>();
            }

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions != null)
            {
                inputReader.SetInputActions(inputActions);
            }
            else
            {
                Debug.LogWarning("Missing input actions asset at " + InputActionsPath);
            }

            IsometricPlayerController playerController = player.GetComponent<IsometricPlayerController>();
            if (playerController == null)
            {
                playerController = player.AddComponent<IsometricPlayerController>();
            }
            playerController.SetInputReader(inputReader);

            PlayerAnimationDriver animationDriver = player.GetComponent<PlayerAnimationDriver>();
            if (animationDriver == null)
            {
                animationDriver = player.AddComponent<PlayerAnimationDriver>();
            }
            animationDriver.SetInputReader(inputReader);
            animationDriver.SetAnimator(player.GetComponentInChildren<Animator>());

            PlayerActionFeedback feedback = player.GetComponent<PlayerActionFeedback>();
            if (feedback == null)
            {
                feedback = player.AddComponent<PlayerActionFeedback>();
            }
            feedback.SetInputReader(inputReader);

            PlayerMotionVisualFeedback motionFeedback = player.GetComponent<PlayerMotionVisualFeedback>();
            if (motionFeedback == null)
            {
                motionFeedback = player.AddComponent<PlayerMotionVisualFeedback>();
            }
            motionFeedback.SetInputReader(inputReader);
            motionFeedback.SetVisualRoot(ResolvePlayerVisualRoot(player));

            PlayerActionDebugLog debugLog = player.GetComponent<PlayerActionDebugLog>();
            if (debugLog == null)
            {
                debugLog = player.AddComponent<PlayerActionDebugLog>();
            }
            debugLog.SetInputReader(inputReader);
            debugLog.SetWatchedTarget(player.transform);
            debugLog.SetSecondaryTarget(cameraObject != null ? cameraObject.transform : null);
            debugLog.SetMovementController(playerController);
            debugLog.SetMotionFeedback(motionFeedback);
            debugLog.SetCameraFollow(cameraObject != null ? cameraObject.GetComponent<IsometricCameraFollow>() : null);

            Animator animator = player.GetComponentInChildren<Animator>();
            RuntimeAnimatorController runtimeController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerControllerPath);
            if (animator != null && runtimeController != null)
            {
                animator.runtimeAnimatorController = runtimeController;
                animationDriver.SetAnimator(animator);
            }
        }

        private static Material LoadOrCreateGroundMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = "Ground_Test_Material"
            };
            material.color = new Color(0.45f, 0.52f, 0.42f, 1f);

            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static Transform ResolvePlayerVisualRoot(GameObject player)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = player.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshRenderer != null)
            {
                return skinnedMeshRenderer.transform;
            }

            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.transform != player.transform)
            {
                return animator.transform;
            }

            if (player.transform.childCount > 0)
            {
                return player.transform.GetChild(0);
            }

            return player.transform;
        }
    }
}
