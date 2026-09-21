using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexShift.EditorTools.Validation
{
    public static class BuildInputConfigurationValidator
    {
        public const string CanonicalScenePath = "Assets/_Project/Scenes/Game.unity";
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

            if (enabledScenes.Length != 1 || enabledScenes[0].path != CanonicalScenePath)
            {
                string actual = enabledScenes.Length == 0
                    ? "<none>"
                    : string.Join(", ", enabledScenes.Select(scene => scene.path));
                problems.Add($"Enabled production scene must be '{CanonicalScenePath}', found '{actual}'.");
            }

            if (enabledScenes.Any(scene => scene.path == "Assets/Scenes/SampleScene.unity"))
            {
                problems.Add("Template SampleScene is enabled in Build Settings.");
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
