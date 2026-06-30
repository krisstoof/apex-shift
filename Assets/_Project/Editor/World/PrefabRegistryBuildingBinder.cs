using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexShift.Runtime.World.Generation;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.World
{
    public static class PrefabRegistryBuildingBinder
    {
        private const string RegistryAssetPath = "Assets/_Project/Data/World/PrefabRegistry.asset";
        private const string ModelsRoot = "Assets/apex_shift_placeables_3d_v2_unity_obj/Assets/_Project/Art/Placeables/Models";

        [MenuItem("Apex Shift/World/Refresh Building Prefabs")]
        public static void RefreshBuildingPrefabs()
        {
            PrefabRegistry registry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(RegistryAssetPath);
            if (registry == null)
            {
                Debug.LogError($"PrefabRegistry not found at {RegistryAssetPath}.");
                return;
            }

            Dictionary<string, GameObject> modelLookup = AssetDatabase
                .FindAssets("t:Model", new[] { ModelsRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => new { path, go = AssetDatabase.LoadAssetAtPath<GameObject>(path) })
                .Where(item => item.go != null)
                .ToDictionary(item => NormalizeKey(Path.GetFileNameWithoutExtension(item.path)), item => item.go);

            List<BuildingPrefabEntry> entries = new List<BuildingPrefabEntry>();
            foreach (string id in new[] { "storage_box", "campfire", "wall", "trap", "tent" })
            {
                if (!modelLookup.TryGetValue(NormalizeKey(id), out GameObject prefab))
                {
                    Debug.LogWarning($"Missing building model for '{id}' under {ModelsRoot}.");
                    continue;
                }

                BuildingPrefabEntry entry = new BuildingPrefabEntry();
                SetField(entry, "buildingId", id);
                SetField(entry, "prefab", prefab);
                entries.Add(entry);
            }

            SetField(registry, "buildingPrefabs", entries);
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"PrefabRegistry buildingPrefabs refreshed with {entries.Count} entries.");
        }

        [MenuItem("Apex Shift/World/Refresh Building Prefabs", true)]
        public static bool CanRefreshBuildingPrefabs()
        {
            return AssetDatabase.LoadAssetAtPath<PrefabRegistry>(RegistryAssetPath) != null;
        }

        private static string NormalizeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace("-", "_");
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field == null)
            {
                throw new InvalidOperationException($"Could not resolve field '{fieldName}' on {target.GetType().Name}.");
            }

            field.SetValue(target, value);
        }
    }
}
