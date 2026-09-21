using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using ApexShift.Runtime.World.Generation;

namespace ApexShift.EditorTools.Validation
{
    public static class AssetRepositoryLayoutValidator
    {
        public const string CanonicalAudioPath = "Assets/_Project/Audio";
        public const string CanonicalPlaceablesPath = "Assets/_Project/Art/Placeables/Models";
        public const string CanonicalIconsPath = "Assets/_Project/Resources/ApexShift2D/Art/Icons";
        public const string CanonicalSettingsPath = "Assets/Settings";
        public const string SampleSceneProfileGuid = "10fc4df2da32a41aaa32d77bc913491c";

        private static readonly string[] RemovedTemplatePaths =
        {
            "Assets/Readme.asset", "Assets/TutorialInfo", "Assets/Scenes/SampleScene.unity", "Assets/Editor.meta"
        };

        private static readonly string[] RemovedWrapperPaths =
        {
            "Assets/ApexShiftIconPack", "Assets/apex_shift_audio_assets_v3_realistic",
            "Assets/apex_shift_placeables_3d_v2_unity_obj", "Assets/apex_shift_audio_assets_v3_oga_ref"
        };

        [MenuItem("Apex Shift/Validation/Validate Asset Repository Layout")]
        public static void ValidateFromMenu() { ValidateOrThrow(); }

        public static bool Validate(bool logResult = true)
        {
            List<string> problems = CollectProblems();
            if (problems.Count == 0)
            {
                if (logResult) Debug.Log("Apex Shift asset repository layout is valid.");
                return true;
            }
            Debug.LogError("Apex Shift asset repository layout is invalid:\n- " + string.Join("\n- ", problems));
            return false;
        }

        public static void ValidateOrThrow()
        {
            List<string> problems = CollectProblems();
            if (problems.Count == 0)
            {
                Debug.Log("Apex Shift asset repository layout is valid.");
                return;
            }
            throw new InvalidOperationException("Apex Shift asset repository layout is invalid:\n- " + string.Join("\n- ", problems));
        }

        private static List<string> CollectProblems()
        {
            var problems = new List<string>();
            RequireFolder(CanonicalAudioPath, problems);
            RequireFolder(CanonicalPlaceablesPath, problems);
            RequireFolder(CanonicalIconsPath, problems);
            RequireFolder(CanonicalSettingsPath, problems);

            foreach (string path in RemovedTemplatePaths)
            {
                if (path.EndsWith(".meta", StringComparison.Ordinal))
                {
                    if (File.Exists(path)) problems.Add("Removed template metadata still exists: " + path);
                }
                else if (AssetDatabase.LoadMainAssetAtPath(path) != null || AssetDatabase.IsValidFolder(path))
                {
                    problems.Add("Removed Unity template asset still exists: " + path);
                }
            }

            foreach (string path in RemovedWrapperPaths)
            {
                if (AssetDatabase.LoadMainAssetAtPath(path) != null || AssetDatabase.IsValidFolder(path))
                    problems.Add("Legacy asset wrapper still exists: " + path);
            }

            if (AssetDatabase.GUIDToAssetPath(SampleSceneProfileGuid) != "Assets/Settings/SampleSceneProfile.asset")
                problems.Add("SampleSceneProfile GUID no longer resolves to Assets/Settings/SampleSceneProfile.asset.");

            if (AssetDatabase.LoadAssetAtPath<PrefabRegistry>("Assets/_Project/Data/World/PrefabRegistry.asset") == null)
                problems.Add("Project PrefabRegistry asset is missing.");

            if (UnityEngine.Resources.Load<Texture2D>("ApexShift2D/Art/Icons/Items/item_unknown") == null)
                problems.Add("Canonical item icon cannot be loaded from Resources.");
            if (UnityEngine.Resources.Load<Texture2D>("ApexShift2D/Art/Icons/Resources/resource_wood_log") == null)
                problems.Add("Canonical resource icon cannot be loaded from Resources.");
            return problems;
        }

        private static void RequireFolder(string path, List<string> problems)
        {
            if (!AssetDatabase.IsValidFolder(path)) problems.Add("Required canonical asset folder is missing: " + path);
        }
    }
}
