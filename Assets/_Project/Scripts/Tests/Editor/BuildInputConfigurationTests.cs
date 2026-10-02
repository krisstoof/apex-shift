using System.Linq;
using ApexShift.Runtime.Flow;
using ApexShift.EditorTools.Validation;
using ApexShift.Runtime.World.Generation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
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
        public void ProductionScene_ContainsStartupShellWithoutSerializedGameplay()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            string previousPath = previousScene.IsValid() ? previousScene.path : string.Empty;
            Scene productionScene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);

            try
            {
                GameObject[] roots = productionScene.GetRootGameObjects();
                WorldGeneratorRuntime generator = roots.SelectMany(root => root.GetComponentsInChildren<WorldGeneratorRuntime>(true)).Single();
                Assert.That(generator.GetComponents<MonoBehaviour>().Any(component => component != null &&
                    component.GetType().FullName == "ApexShift.Presentation.HUD.RuntimeHUDProvisioner"), Is.True);
                Assert.That(roots.SelectMany(root => root.GetComponentsInChildren<GameStartupController>(true)), Is.Not.Empty);
                var configured = new SerializedObject(generator);
                Assert.That(configured.FindProperty("generateOnStart").boolValue, Is.False);
                foreach (string field in new[] { "biomeCatalog", "prefabRegistry", "playerPrefab", "playerAnimatorController" })
                    Assert.That(configured.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
                WorldRuntimeOwner owner = generator.GetComponent<WorldRuntimeOwner>();
                if (owner != null && owner.GenerationRoot != null)
                {
                    Transform preview = owner.GenerationRoot;
                    Assert.That(preview.Find("TerrainRoot"), Is.Not.Null);
                    foreach (string name in new[] { "ResourceRoot", "VegetationRoot", "BuildingRoot", "LandmarkRoot",
                        "CreatureRoot", "Player", "Main Camera", "WorldBounds" })
                        Assert.That(preview.Find(name), Is.Null, "Stale generated content: " + name);
                    Assert.That(preview.GetComponentsInChildren<ApexShift.Runtime.Resources.ResourceNodeView>(true), Is.Empty);
                    Assert.That(preview.GetComponentsInChildren<ApexShift.Runtime.Creatures.CreatureAgentView>(true), Is.Empty);
                }
                Assert.That(BuildInputConfigurationValidator.Validate(false), Is.True);
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
        public void ProductionScene_PlayerAnimationAssetsHaveControllerAndLocomotionTransitions()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            string previousPath = previousScene.IsValid() ? previousScene.path : string.Empty;
            Scene productionScene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);

            try
            {
                WorldGeneratorRuntime generator = productionScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<WorldGeneratorRuntime>(true)).Single();
                var configuration = new SerializedObject(generator);
                GameObject prefab = configuration.FindProperty("playerPrefab").objectReferenceValue as GameObject;
                Assert.That(prefab, Is.Not.Null);
                Assert.That(prefab.GetComponentInChildren<Animator>(true), Is.Not.Null);
                Assert.That(configuration.FindProperty("playerAnimatorController").objectReferenceValue, Is.Not.Null);
                RuntimeAnimatorController generated = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/_Project/Generated/Animation/GeneratedKevinIglesiasPlayerItemUse.controller");
                Assert.That(generated, Is.Not.Null);
                Assert.That(generated.animationClips.Any(clip => clip.name == "Idle"), Is.True);
                Assert.That(generated.animationClips.Any(clip => clip.name == "Walking"), Is.True);
                Assert.That(generated.animationClips.Any(clip => clip.name == "Running"), Is.True);
                AnimatorController controller = generated as AnimatorController;
                Assert.That(controller, Is.Not.Null, "Generated player controller must expose its authored state/parameter contract.");
                string[] requiredParameters =
                {
                    "Speed", "IsMoving", "IsSprinting", "IsSwimming", "Attack", "Interact", "Gather",
                    "SpearAttack", "BowAttack", "AxeUse", "PickaxeUse", "TorchUse", "Hurt", "Death"
                };
                string[] parameterNames = controller.parameters.Select(parameter => parameter.name).ToArray();
                foreach (string parameterName in requiredParameters)
                    Assert.That(parameterNames, Does.Contain(parameterName), $"Generated player controller is missing parameter {parameterName}.");
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
        public void ProductionScene_GeneratorUsesCanonicalInputActionsForSpawnedPlayer()
        {
            Scene previousScene = SceneManager.GetActiveScene();
            string previousPath = previousScene.IsValid() ? previousScene.path : string.Empty;
            Scene gameScene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Single);

            try
            {
                InputActionAsset canonical = LoadCanonicalAsset();
                WorldGeneratorRuntime generator = gameScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<WorldGeneratorRuntime>(true)).Single();
                Assert.That(generator.InputActions, Is.SameAs(canonical));
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
