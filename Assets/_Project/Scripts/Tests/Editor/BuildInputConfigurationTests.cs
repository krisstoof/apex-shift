using System.Linq;
using ApexShift.Runtime.PlayerInput;
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
        private const string CanonicalScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string CanonicalInputPath = "Assets/_Project/Input/ApexShiftInputActions.inputactions";
        private const string LegacyInputPath = "Assets/InputSystem_Actions.inputactions";
        private const string InputActionsConfigKey = "com.unity.input.settings.actions";

        [Test]
        public void BuildSettings_ContainsOnlyCanonicalGameScene()
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();

            Assert.That(enabledScenes.Select(scene => scene.path), Is.EqualTo(new[] { CanonicalScenePath }));
            Assert.That(enabledScenes.Any(scene => scene.path == "Assets/Scenes/SampleScene.unity"), Is.False);
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
        public void GameScene_PlayerInputReaderUsesCanonicalInputActions()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            string previousPath = previousScene.IsValid() ? previousScene.path : string.Empty;
            Scene gameScene = EditorSceneManager.OpenScene(CanonicalScenePath, OpenSceneMode.Single);

            try
            {
                InputActionAsset canonical = LoadCanonicalAsset();
                PlayerInputReader reader = Resources.FindObjectsOfTypeAll<PlayerInputReader>()
                    .FirstOrDefault(candidate => candidate.gameObject.scene == gameScene);

                Assert.That(reader, Is.Not.Null, "Game.unity does not contain a PlayerInputReader.");
                SerializedObject serializedReader = new SerializedObject(reader);
                SerializedProperty inputActions = serializedReader.FindProperty("inputActions");

                Assert.That(inputActions, Is.Not.Null);
                Assert.That(inputActions.objectReferenceValue, Is.SameAs(canonical));
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousPath) && previousPath != CanonicalScenePath)
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
