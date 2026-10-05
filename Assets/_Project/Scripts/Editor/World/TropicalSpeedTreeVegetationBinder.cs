using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexShift.EditorTools.Validation;
using ApexShift.Runtime.World.Vegetation;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.World
{
    public static class TropicalSpeedTreeVegetationBinder
    {
        private const string DataRoot = "Assets/_Project/Data/Vegetation";
        private const string AnchorPath = "Assets/_Project/Data/Biomes/BiomeCatalog.asset";
        public static bool IsActivated
        {
            get
            {
                var catalog = AssetDatabase.LoadAssetAtPath<VegetationCatalogAsset>(DataRoot + "/VegetationCatalog.asset");
                // A new ID in the canonical catalog records activation, independently of a subsequently edited visual.
                return catalog != null && catalog.GetSpecies("palm_tall_01") != null;
            }
        }

        [MenuItem("Apex Shift/World/Bind Tropical SpeedTree Vegetation")]
        public static void BindFromMenu() => Bind();

        public static void RequireReady(TropicalSpeedTreeManifest manifest)
        {
            var errors = TropicalSpeedTreeValidator.CollectReadinessProblems(manifest);
            if (AssetDatabase.LoadMainAssetAtPath(AnchorPath) == null) errors.Add("Missing world data anchor: " + AnchorPath);
            if (errors.Count != 0)
                throw new InvalidOperationException("Tropical SpeedTree binding aborted; production data unchanged:\n" + string.Join("\n", errors));
        }

        public static void Bind()
        {
            var manifest = TropicalSpeedTreeManifest.Load();
            // Do not call legacy EnsureAssets before preflight: even a failed bind must leave manual production data untouched.
            RequireReady(manifest);
            RunDataTransaction(() =>
            {
                VegetationDataAssetCreator.EnsureTropicalAssets(manifest, true);
                var errors = VegetationDataValidator.CollectProblems();
                if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
            });
            Debug.Log("Bound complete tropical SpeedTree vegetation set. Compatibility and depleted visuals retained.");
        }

        internal static void SaveDataAssets()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataRoot }))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AnchorPath));
        }

        internal static void RunDataTransaction(Action update)
        {
            var paths = AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataRoot })
                .Select(AssetDatabase.GUIDToAssetPath).Append(AnchorPath).Distinct().ToArray();
            var backups = new Dictionary<string, string>();
            var diskBackups = new Dictionary<string, byte[]>();
            var dirty = new Dictionary<string, bool>();
            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset != null)
                {
                    backups.Add(path, EditorJsonUtility.ToJson(asset));
                    diskBackups.Add(path, File.ReadAllBytes(path));
                    dirty.Add(path, EditorUtility.IsDirty(asset));
                }
            }
            try
            {
                update();
                SaveDataAssets();
            }
            catch
            {
                foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataRoot }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!backups.ContainsKey(path)) AssetDatabase.DeleteAsset(path);
                }
                foreach (var pair in backups)
                {
                    if (!File.ReadAllBytes(pair.Key).SequenceEqual(diskBackups[pair.Key]))
                        File.WriteAllBytes(pair.Key, diskBackups[pair.Key]);
                    var asset = AssetDatabase.LoadMainAssetAtPath(pair.Key);
                    EditorJsonUtility.FromJsonOverwrite(pair.Value, asset);
                    // Reset the catalog's nonserialized ID cache after restoring its serialized list.
                    if (asset is VegetationCatalogAsset catalog) catalog.SetSpecies(catalog.Species.ToArray());
                    if (asset is HabitatVegetationCatalogAsset habitats) habitats.SetProfiles(habitats.Profiles.ToArray());
                    if (dirty[pair.Key]) EditorUtility.SetDirty(asset);
                    else EditorUtility.ClearDirty(asset);
                }
                throw;
            }
        }
    }
}
