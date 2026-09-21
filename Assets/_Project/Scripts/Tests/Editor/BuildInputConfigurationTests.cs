using System.Linq;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.World.Generation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ApexShift.Tests.Editor
{
    public sealed class BuildInputConfigurationTests
    {
        private const string ProductionScenePath = "Assets/_Project/Scenes/RuntimeWorld.unity";
        private const string CanonicalInputPath = "Assets/_Project/Input/ApexShiftInputActions.inputactions";
        private const string LegacyInputPath = "Assets/InputSystem_Actions.inputactions";
        private const string InputActionsConfigKey = "com.unity.input.settings.actions";

        [Test]
        public void BuildSettings_ContainsOnlyProductionWorldScene()
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();

            Assert.That(enabledScenes.Select(scene => scene.path), Is.EqualTo(new[] { ProductionScenePath }));
            Assert.That(enabledScenes.Any(scene => scene.path == "Assets/Scenes/SampleScene.unity"), Is.False);
        }

        [Test]
        public void ProductionScene_ContainsAuthoredBiomeWorldAndGameplayFlow()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            string previousPath = previousScene.IsValid() ? previousScene.path : string.Empty;
            Scene productionScene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);

            try
            {
                Transform[] transforms = productionScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .ToArray();
                foreach (string objectName in new[] { "RuntimeWorldGenerator", "Player", "Main Camera", "PlayerFollowCamera", "UI", "TerrainRoot", "ResourceRoot", "CreatureRoot" })
                {
                    Assert.That(transforms.Any(transform => transform.name == objectName), Is.True,
                        $"Production scene is missing gameplay/world object: {objectName}");
                }

                Assert.That(productionScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<WorldGeneratorRuntime>(true)).Any(), Is.True);
                Assert.That(productionScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                    .Any(component => component != null && component.GetType().Name == "ResourceNodeView"), Is.True);
                Assert.That(productionScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                    .Any(component => component != null && component.GetType().Name == "CreatureAgentView"), Is.True);

                foreach (string populatedRoot in new[] { "TerrainRoot", "ResourceRoot", "CreatureRoot" })
                {
                    Transform root = transforms.First(transform => transform.name == populatedRoot);
                    Assert.That(root.childCount, Is.GreaterThan(0), $"Production scene root is empty: {populatedRoot}");
                }
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousPath) && previousPath != ProductionScenePath)
                {
                    EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
                }
            }
        }

        [Test]
        public void ProductionScene_PlayerAnimationHasControllerAndLocomotionTransitions()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            string previousPath = previousScene.IsValid() ? previousScene.path : string.Empty;
            Scene productionScene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);

            try
            {
                GameObject player = productionScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .First(transform => transform.name == "Player")
                    .gameObject;
                Animator animator = player.GetComponentInChildren<Animator>(true);
                PlayerAnimationDriver driver = player.GetComponentInChildren<PlayerAnimationDriver>(true);

                Assert.That(animator, Is.Not.Null);
                Assert.That(driver, Is.Not.Null);
                Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
                RuntimeAnimatorController prototype = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/_Project/Animations/Player/PlayerPrototype.controller");
                Assert.That(animator.runtimeAnimatorController, Is.SameAs(prototype));
                Assert.That(animator.runtimeAnimatorController.animationClips.Any(clip => clip.name == "Idle"), Is.True);
                Assert.That(animator.runtimeAnimatorController.animationClips.Any(clip => clip.name == "Walking"), Is.True);
                Assert.That(animator.runtimeAnimatorController.animationClips.Any(clip => clip.name == "Running"), Is.True);
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousPath) && previousPath != ProductionScenePath)
                {
                    EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
                }
            }
        }

        [Test]
        public void GlobalInputActions_UsesApexShiftInputActions()
        {
            InputActionAsset canonical = AssetDatabase.LoadAssetAtPath<InputActionAsset>(CanonicalInputPath);
            InputActionAsset configured;
            EditorBuildSettings.TryGetConfigObject(InputActionsConfigKey, out configured);

            Assert.That(canonical, Is.Not.Null);
            Assert.That(configured, Is.SameAs(canonical));
        }

        [Test]
        public void CanonicalInputActions_ContainsRequiredGameplayActions()
        {
            InputActionAsset asset = LoadCanonicalAsset();
            InputActionMap gameplay = asset.FindActionMap("Gameplay", false);
            string[] requiredActions = { "Move", "Look", "Interact", "Attack", "Sprint", "OpenInventory", "OpenCrafting", "ToggleMap", "Pause" };

            Assert.That(gameplay, Is.Not.Null);
            foreach (string actionName in requiredActions)
            {
                Assert.That(gameplay.FindAction(actionName, false), Is.Not.Null, $"Missing Gameplay action: {actionName}");
            }
        }

        [Test]
        public void CanonicalInputActions_ContainsRequiredUIActions()
        {
            InputActionAsset asset = LoadCanonicalAsset();
            InputActionMap ui = asset.FindActionMap("UI", false);
            string[] requiredActions = { "navigate", "submit", "cancel", "point", "click", "rightClick", "middleClick", "scrollWheel" };

            Assert.That(ui, Is.Not.Null);
            foreach (string actionName in requiredActions)
            {
                Assert.That(ui.FindAction(actionName, false), Is.Not.Null, $"Missing UI action: {actionName}");
            }
        }

        [Test]
        public void ProductionScene_PlayerInputReaderUsesCanonicalInputActions()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            string previousPath = previousScene.IsValid() ? previousScene.path : string.Empty;
            Scene gameScene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);

            try
            {
                InputActionAsset canonical = LoadCanonicalAsset();
                PlayerInputReader reader = Resources.FindObjectsOfTypeAll<PlayerInputReader>()
                    .FirstOrDefault(candidate => candidate.gameObject.scene == gameScene);

                Assert.That(reader, Is.Not.Null, "RuntimeWorld.unity does not contain a PlayerInputReader.");
                SerializedObject serializedReader = new SerializedObject(reader);
                SerializedProperty inputActions = serializedReader.FindProperty("inputActions");

                Assert.That(inputActions, Is.Not.Null);
                Assert.That(inputActions.objectReferenceValue, Is.SameAs(canonical));
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousPath) && previousPath != ProductionScenePath)
                {
                    EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
                }
            }
        }

        [Test]
        public void LegacyInputActionsAsset_DoesNotExist()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<InputActionAsset>(LegacyInputPath), Is.Null);
        }

        private static InputActionAsset LoadCanonicalAsset()
        {
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(CanonicalInputPath);
            Assert.That(asset, Is.Not.Null, "Canonical InputActionAsset is missing.");
            return asset;
        }
    }
}
