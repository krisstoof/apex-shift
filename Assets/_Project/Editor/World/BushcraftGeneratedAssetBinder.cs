using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Generation;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.World
{
    /// <summary>
    /// Imports generated bushcraft models and binds them into the runtime PrefabRegistry.
    ///
    /// Expected local generator output, relative to the Unity project root:
    /// - ApexShift_Bushcraft_Output_v2/
    /// - ApexShift_Bushcraft_Output_v2_3_vegetation/
    ///
    /// The ecosystem/world generator uses PrefabRegistry.ResourcePrefabs, so this tool updates
    /// resourcePrefabs as well as buildingPrefabs. It intentionally keeps creaturePrefabs untouched.
    /// </summary>
    public static class BushcraftGeneratedAssetBinder
    {
        private const string RegistryAssetPath = "Assets/_Project/Data/World/PrefabRegistry.asset";

        private const string ItemsDestination = "Assets/_Project/Art/Bushcraft/Items/Models";
        private const string ResourcesDestination = "Assets/_Project/Art/Bushcraft/Resources/Models";
        private const string PlaceablesDestination = "Assets/_Project/Art/Bushcraft/Placeables/Models";

        private static readonly string[] SourceRoots =
        {
            "ApexShift_Bushcraft_Output_v2",
            "ApexShift_Bushcraft_Output_v2_3_vegetation"
        };

        private static readonly string[] ModelExtensions =
        {
            ".fbx",
            ".obj",
            ".mtl"
        };

        private static readonly Dictionary<VegetationSpawnKind, string[]> ResourceModelsByKind = new Dictionary<VegetationSpawnKind, string[]>
        {
            {
                VegetationSpawnKind.ConiferTree,
                new[]
                {
                    "conifer_tree_stylized",
                    "conifer_tree_a_stylized",
                    "conifer_tree_b_stylized",
                    "conifer_tree_c_stylized",
                    "conifer_sapling_a_stylized",
                    "conifer_sapling_b_stylized",
                    "conifer_sapling_c_stylized"
                }
            },
            {
                VegetationSpawnKind.LeafyTree,
                new[]
                {
                    "leafy_tree_stylized",
                    "leafy_tree_a_stylized",
                    "leafy_tree_b_stylized",
                    "leafy_tree_c_stylized",
                    "leafy_sapling_a_stylized",
                    "leafy_sapling_b_stylized",
                    "leafy_sapling_c_stylized"
                }
            },
            {
                VegetationSpawnKind.DryTree,
                new[]
                {
                    "dry_tree_stylized",
                    "dry_tree_a_stylized",
                    "dry_tree_b_stylized",
                    "dry_tree_c_stylized",
                    "dry_sapling_a_stylized",
                    "dry_sapling_b_stylized"
                }
            },
            {
                VegetationSpawnKind.Rock,
                new[]
                {
                    "rock_stylized"
                }
            },
            {
                VegetationSpawnKind.GreenBush,
                new[]
                {
                    "green_bush_stylized",
                    "green_bush_a_stylized",
                    "green_bush_b_stylized",
                    "green_bush_c_stylized",
                    "forest_shrub_a_stylized",
                    "forest_shrub_b_stylized"
                }
            },
            {
                VegetationSpawnKind.DryBush,
                new[]
                {
                    "dry_bush_stylized",
                    "dry_bush_a_stylized",
                    "dry_bush_b_stylized",
                    "dry_bush_c_stylized"
                }
            },
            {
                VegetationSpawnKind.GrassOrFlower,
                new[]
                {
                    "grass_or_flower_stylized",
                    "tall_grass_clump_a_stylized",
                    "tall_grass_clump_b_stylized",
                    "wildflower_patch_a_stylized",
                    "wildflower_patch_b_stylized"
                }
            },
            {
                VegetationSpawnKind.BerryBush,
                new[]
                {
                    "berry_bush_stylized",
                    "berry_bush_a_stylized",
                    "berry_bush_b_stylized",
                    "berry_bush_c_stylized"
                }
            }
        };

        private static readonly Dictionary<string, string[]> BuildingModelsById = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "campfire", new[] { "campfire_stylized" } },
            { "storage_box", new[] { "storage_box_stylized" } },
            { "tent", new[] { "tent_stylized" } },
            { "wall", new[] { "wall_stylized" } },
            { "trap", new[] { "trap_stylized" } }
        };

        [MenuItem("Apex Shift/Art/Bushcraft/Import Generated Assets And Bind PrefabRegistry")]
        public static void ImportGeneratedAssetsAndBindPrefabRegistry()
        {
            CopyGeneratedModelsIntoAssets();
            AssetDatabase.Refresh();
            BindExistingImportedAssets();
        }

        [MenuItem("Apex Shift/Art/Bushcraft/Bind Existing Imported Assets To PrefabRegistry")]
        public static void BindExistingImportedAssets()
        {
            PrefabRegistry registry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(RegistryAssetPath);
            if (registry == null)
            {
                Debug.LogError($"PrefabRegistry not found at {RegistryAssetPath}.");
                return;
            }

            Dictionary<string, GameObject> resourceModelLookup = BuildModelLookup(ResourcesDestination);
            Dictionary<string, GameObject> placeableModelLookup = BuildModelLookup(PlaceablesDestination);

            List<ResourcePrefabEntry> resourceEntries = BuildResourcePrefabEntries(resourceModelLookup);
            List<BuildingPrefabEntry> buildingEntries = BuildBuildingPrefabEntries(placeableModelLookup);

            SetField(registry, "resourcePrefabs", resourceEntries);
            SetField(registry, "buildingPrefabs", buildingEntries);

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Bushcraft PrefabRegistry binding complete. Resources={resourceEntries.Count}, Buildings={buildingEntries.Count}. Creature prefabs were preserved.");
        }

        private static void CopyGeneratedModelsIntoAssets()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                Debug.LogError("Could not resolve Unity project root from Application.dataPath.");
                return;
            }

            EnsureAssetDirectory(ItemsDestination);
            EnsureAssetDirectory(ResourcesDestination);
            EnsureAssetDirectory(PlaceablesDestination);

            int copied = 0;
            foreach (string sourceRootName in SourceRoots)
            {
                string sourceRoot = Path.Combine(projectRoot, sourceRootName);
                if (!Directory.Exists(sourceRoot))
                {
                    Debug.LogWarning($"Generated asset source root not found: {sourceRoot}");
                    continue;
                }

                copied += CopyModelFiles(Path.Combine(sourceRoot, "Items", "Models"), ItemsDestination);
                copied += CopyModelFiles(Path.Combine(sourceRoot, "Resources", "Models"), ResourcesDestination);
                copied += CopyModelFiles(Path.Combine(sourceRoot, "Placeables", "Models"), PlaceablesDestination);
            }

            Debug.Log($"Copied {copied} generated bushcraft model files into Assets/_Project/Art/Bushcraft.");
        }

        private static int CopyModelFiles(string sourceDirectory, string unityDestinationDirectory)
        {
            if (!Directory.Exists(sourceDirectory))
            {
                return 0;
            }

            string absoluteDestination = ToAbsolutePath(unityDestinationDirectory);
            Directory.CreateDirectory(absoluteDestination);

            int copied = 0;
            foreach (string file in Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.TopDirectoryOnly))
            {
                string extension = Path.GetExtension(file);
                if (!ModelExtensions.Any(candidate => string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string destination = Path.Combine(absoluteDestination, Path.GetFileName(file));
                File.Copy(file, destination, overwrite: true);
                copied++;
            }

            return copied;
        }

        private static Dictionary<string, GameObject> BuildModelLookup(string modelsRoot)
        {
            if (!AssetDatabase.IsValidFolder(modelsRoot))
            {
                return new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            }

            return AssetDatabase
                .FindAssets("t:Model", new[] { modelsRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => new { path, prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) })
                .Where(item => item.prefab != null)
                .GroupBy(item => NormalizeKey(Path.GetFileNameWithoutExtension(item.path)))
                .ToDictionary(group => group.Key, group => group.First().prefab, StringComparer.OrdinalIgnoreCase);
        }

        private static List<ResourcePrefabEntry> BuildResourcePrefabEntries(Dictionary<string, GameObject> modelLookup)
        {
            List<ResourcePrefabEntry> entries = new List<ResourcePrefabEntry>();
            foreach (KeyValuePair<VegetationSpawnKind, string[]> mapping in ResourceModelsByKind)
            {
                foreach (string modelName in mapping.Value)
                {
                    if (!modelLookup.TryGetValue(NormalizeKey(modelName), out GameObject prefab))
                    {
                        Debug.LogWarning($"Missing bushcraft resource model '{modelName}' for {mapping.Key} under {ResourcesDestination}.");
                        continue;
                    }

                    ResourcePrefabEntry entry = new ResourcePrefabEntry();
                    SetField(entry, "kind", mapping.Key);
                    SetField(entry, "prefab", prefab);
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private static List<BuildingPrefabEntry> BuildBuildingPrefabEntries(Dictionary<string, GameObject> modelLookup)
        {
            List<BuildingPrefabEntry> entries = new List<BuildingPrefabEntry>();
            foreach (KeyValuePair<string, string[]> mapping in BuildingModelsById)
            {
                foreach (string modelName in mapping.Value)
                {
                    if (!modelLookup.TryGetValue(NormalizeKey(modelName), out GameObject prefab))
                    {
                        Debug.LogWarning($"Missing bushcraft building model '{modelName}' for id '{mapping.Key}' under {PlaceablesDestination}.");
                        continue;
                    }

                    BuildingPrefabEntry entry = new BuildingPrefabEntry();
                    SetField(entry, "buildingId", mapping.Key);
                    SetField(entry, "prefab", prefab);
                    entries.Add(entry);
                }
            }

            return entries;
        }

        private static void EnsureAssetDirectory(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string[] parts = assetPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            if (!assetPath.StartsWith("Assets", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Expected Unity asset path starting with Assets, got '{assetPath}'.");
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new InvalidOperationException("Could not resolve Unity project root from Application.dataPath.");
            }

            string relative = assetPath.Substring("Assets".Length).TrimStart('/', '\\');
            return Path.Combine(projectRoot, "Assets", relative.Replace('/', Path.DirectorySeparatorChar));
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
