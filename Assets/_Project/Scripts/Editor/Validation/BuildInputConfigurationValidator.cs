using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.World.Generation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ApexShift.EditorTools.Validation
{
    public static class BuildInputConfigurationValidator
    {
        public const string ProductionScenePath = "Assets/_Project/Scenes/RuntimeWorld.unity";
        public const string CanonicalInputPath = "Assets/_Project/Input/ApexShiftInputActions.inputactions";
        public const string LegacyInputPath = "Assets/InputSystem_Actions.inputactions";
        public const string InputActionsConfigKey = "com.unity.input.settings.actions";

        private static readonly string[] RequiredGameplayActions =
        {
            "Move", "Look", "Interact", "Attack", "Sprint",
            "OpenInventory", "OpenCrafting", "ToggleMap", "Pause"
        };

        private static readonly string[] RequiredUiActions =
        {
            "navigate", "submit", "cancel", "point", "click",
            "rightClick", "middleClick", "scrollWheel"
        };

        [MenuItem("Apex Shift/Validation/Validate Build & Input Configuration")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
        }

        public static bool Validate(bool logResult = true)
        {
            List<string> problems = CollectProblems();
            if (problems.Count == 0)
            {
                if (logResult)
                {
                    Debug.Log("Apex Shift build and input configuration is valid.");
                }

                return true;
            }

            Debug.LogError("Apex Shift build and input configuration is invalid:\n- " + string.Join("\n- ", problems));
            return false;
        }

        public static void ValidateOrThrow()
        {
            List<string> problems = CollectProblems();
            if (problems.Count == 0)
            {
                Debug.Log("Apex Shift build and input configuration is valid.");
                return;
            }

            throw new InvalidOperationException("Apex Shift build and input configuration is invalid:\n- " + string.Join("\n- ", problems));
        }

        private static List<string> CollectProblems()
        {
            var problems = new List<string>();
            ValidateBuildScenes(problems);
            ValidateProductionScene(problems);

            InputActionAsset canonicalInput = AssetDatabase.LoadAssetAtPath<InputActionAsset>(CanonicalInputPath);
            if (canonicalInput == null)
            {
                problems.Add("Canonical InputActionAsset is missing: " + CanonicalInputPath);
            }
            else
            {
                ValidateActionSchema(canonicalInput, problems);
            }

            InputActionAsset configuredInput;
            EditorBuildSettings.TryGetConfigObject(InputActionsConfigKey, out configuredInput);
            if (canonicalInput != null && configuredInput != canonicalInput)
            {
                string configuredPath = configuredInput == null
                    ? "<null>"
                    : AssetDatabase.GetAssetPath(configuredInput);
                problems.Add($"Global Input System config points to '{configuredPath}', expected '{CanonicalInputPath}'.");
            }

            if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(LegacyInputPath) != null)
            {
                problems.Add("Legacy InputActionAsset still exists: " + LegacyInputPath);
            }

            return problems;
        }

        private static void ValidateBuildScenes(List<string> problems)
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();

            if (enabledScenes.Length != 1)
            {
                problems.Add($"Expected exactly one enabled production scene, found {enabledScenes.Length}.");
            }

            if (enabledScenes.Length != 1 || enabledScenes[0].path != ProductionScenePath)
            {
                string actual = enabledScenes.Length == 0
                    ? "<none>"
                    : string.Join(", ", enabledScenes.Select(scene => scene.path));
                problems.Add($"Enabled production scene must be '{ProductionScenePath}', found '{actual}'.");
            }

            if (enabledScenes.Any(scene => scene.path == "Assets/Scenes/SampleScene.unity"))
            {
                problems.Add("Template SampleScene is enabled in Build Settings.");
            }
        }

        private static void ValidateProductionScene(List<string> problems)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ProductionScenePath) == null)
            {
                problems.Add("Production scene is missing: " + ProductionScenePath);
                return;
            }

            Scene scene = default;
            try
            {
                scene = EditorSceneManager.OpenScene(ProductionScenePath, OpenSceneMode.Additive);
                GameObject[] roots = scene.GetRootGameObjects();
                Transform[] transforms = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
                string[] requiredObjects =
                {
                    "RuntimeWorldGenerator", "Player", "Main Camera",
                    "PlayerFollowCamera", "UI", "TerrainRoot", "ResourceRoot", "CreatureRoot"
                };

                foreach (string objectName in requiredObjects)
                {
                    if (!transforms.Any(transform => transform.name == objectName))
                    {
                        problems.Add($"Production scene is missing gameplay/world object '{objectName}'.");
                    }
                }

                GameObject player = transforms.FirstOrDefault(transform => transform.name == "Player")?.gameObject;
                if (player == null)
                {
                    return;
                }

                if (player.GetComponentInChildren<PlayerInputReader>(true) == null ||
                    player.GetComponentInChildren<PlayerAnimationDriver>(true) == null)
                {
                    problems.Add("Production scene Player is missing the required input or animation driver.");
                }

                RuntimeAnimatorController prototypeController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/_Project/Animations/Player/PlayerPrototype.controller");
                Animator animator = player.GetComponentInChildren<Animator>(true);
                if (prototypeController == null || animator == null || animator.runtimeAnimatorController != prototypeController)
                {
                    problems.Add("Production scene Player must use Assets/_Project/Animations/Player/PlayerPrototype.controller.");
                }

                if (roots.SelectMany(root => root.GetComponentsInChildren<WorldGeneratorRuntime>(true)).Any() == false)
                {
                    problems.Add("Production scene is missing WorldGeneratorRuntime; the world would not boot its generation systems.");
                }

                foreach (string populatedRoot in new[] { "TerrainRoot", "ResourceRoot", "CreatureRoot" })
                {
                    Transform root = transforms.FirstOrDefault(transform => transform.name == populatedRoot);
                    if (root != null && root.childCount == 0)
                    {
                        problems.Add($"Production scene '{populatedRoot}' is empty; the build would not contain the authored world flow.");
                    }
                }

                if (!transforms.SelectMany(transform => transform.GetComponents<MonoBehaviour>())
                    .Any(component => component != null && component.GetType().Name == "ResourceNodeView"))
                {
                    problems.Add("Production scene contains no authored resource nodes.");
                }

                if (!transforms.SelectMany(transform => transform.GetComponents<MonoBehaviour>())
                    .Any(component => component != null && component.GetType().Name == "CreatureAgentView"))
                {
                    problems.Add("Production scene contains no creature gameplay entities.");
                }
            }
            catch (System.Exception exception)
            {
                problems.Add($"Could not inspect production scene '{ProductionScenePath}': {exception.Message}");
            }
            finally
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        private static void ValidateActionSchema(InputActionAsset inputAsset, List<string> problems)
        {
            ValidateMap(inputAsset, "Gameplay", RequiredGameplayActions, problems);
            ValidateMap(inputAsset, "UI", RequiredUiActions, problems);
        }

        private static void ValidateMap(InputActionAsset inputAsset, string mapName, string[] requiredActions, List<string> problems)
        {
            InputActionMap map = inputAsset.FindActionMap(mapName, false);
            if (map == null)
            {
                problems.Add($"Canonical input asset is missing the '{mapName}' action map.");
                return;
            }

            foreach (string actionName in requiredActions)
            {
                if (map.FindAction(actionName, false) == null)
                {
                    problems.Add($"Canonical '{mapName}' action map is missing '{actionName}'.");
                }
            }
        }
    }
}
