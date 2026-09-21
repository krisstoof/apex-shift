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
    /// Imports generated bushcraft models (Blender generator v10.1) and binds them into the
    /// runtime PrefabRegistry.
    ///
    /// Expected local generator output, relative to the Unity project root:
    /// - Tools/BlenderV10/ApexShift_Assets_v10_Output/{Items,Tools,Weapons,Ammo,Placeables,
    ///   Vegetation,WorldResources,Creatures,Landmarks}/&lt;asset_id&gt;.fbx
    ///
    /// Unlike the earlier v9 output, v10.1 asset files sit directly in their category folder
    /// (no "Models" subfolder) and are named after their plain asset_id (no "_stylized" suffix).
    ///
    /// The ecosystem/world generator uses PrefabRegistry.ResourcePrefabs, so this tool updates
    /// resourcePrefabs (sourced from Vegetation + WorldResources) as well as buildingPrefabs
    /// (sourced from Placeables). It intentionally keeps creaturePrefabs untouched -- the
    /// procedurally generated creature meshes have no rig/animation and are not drop-in
    /// replacements for the authored creature prefabs.
    ///
    /// Items/Tools/Weapons/Ammo have no ScriptableObject registry of their own -- instead
    /// ItemModelResolver resolves them at runtime via Resources.Load("Items/Models/{itemId}").
    /// This tool additionally copies those categories (flattened, by plain asset_id) into
    /// Assets/_Project/Resources/Items/Models so the resolver picks them up automatically,
    /// replacing the previous procedural/placeholder visuals for pickups, held items and
    /// projectiles. Landmarks are copied the same way into
    /// Assets/_Project/Resources/Landmarks/Models, consumed directly by LandmarkWorldGenerator.
    /// </summary>
    public static class BushcraftGeneratedAssetBinder
    {
        private const string RegistryAssetPath = "Assets/_Project/Data/World/PrefabRegistry.asset";

        private const string ArtRoot = "Assets/_Project/Art/Bushcraft";

        private const string ItemsDestination = ArtRoot + "/Items/Models";
        private const string ToolsDestination = ArtRoot + "/Tools/Models";
        private const string WeaponsDestination = ArtRoot + "/Weapons/Models";
        private const string AmmoDestination = ArtRoot + "/Ammo/Models";
        private const string PlaceablesDestination = ArtRoot + "/Placeables/Models";
        private const string VegetationDestination = ArtRoot + "/Vegetation/Models";
        private const string WorldResourcesDestination = ArtRoot + "/WorldResources/Models";
        private const string CreaturesDestination = ArtRoot + "/Creatures/Models";
        private const string LandmarksDestination = ArtRoot + "/Landmarks/Models";

        private const string ResourcesRoot = "Assets/_Project/Resources";
        private const string ItemsResourcesDestination = ResourcesRoot + "/Items/Models";
        private const string LandmarksResourcesDestination = ResourcesRoot + "/Landmarks/Models";

        private const string SourceRoot = "Tools/BlenderV10/ApexShift_Assets_v10_Output";

        // Maps generator output category folder name -> Unity destination folder.
        private static readonly Dictionary<string, string> CategoryDestinations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Items", ItemsDestination },
            { "Tools", ToolsDestination },
            { "Weapons", WeaponsDestination },
            { "Ammo", AmmoDestination },
            { "Placeables", PlaceablesDestination },
            { "Vegetation", VegetationDestination },
            { "WorldResources", WorldResourcesDestination },
            { "Creatures", CreaturesDestination },
            { "Landmarks", LandmarksDestination }
        };

        // Additional flattened copies placed under Resources so runtime code that resolves
        // models via Resources.Load (ItemModelResolver, LandmarkWorldGenerator) can find them.
        // Multiple source categories can map to the same flattened destination.
        private static readonly (string SourceCategory, string ResourcesDestination)[] ResourcesCopyMappings =
        {
            ("Items", ItemsResourcesDestination),
            ("Tools", ItemsResourcesDestination),
            ("Weapons", ItemsResourcesDestination),
            ("Ammo", ItemsResourcesDestination),
            ("Landmarks", LandmarksResourcesDestination)
        };

        private static readonly string[] ModelExtensions =
        {
            ".fbx"
        };

        private static readonly Dictionary<VegetationSpawnKind, string[]> ResourceModelsByKind = new Dictionary<VegetationSpawnKind, string[]>
        {
            {
                VegetationSpawnKind.ConiferTree,
                new[]
                {
                    "conifer_tree",
                    "conifer_tree_a",
                    "conifer_tree_b",
                    "conifer_tree_c",
                    "conifer_tree_d",
                    "conifer_sapling_a",
                    "conifer_sapling_b",
                    "conifer_sapling_c"
                }
            },
            {
                VegetationSpawnKind.LeafyTree,
                new[]
                {
                    "leafy_tree",
                    "leafy_tree_a",
                    "leafy_tree_b",
                    "leafy_tree_c",
                    "leafy_tree_d",
                    "leafy_sapling_a",
                    "leafy_sapling_b",
                    "leafy_sapling_c"
                }
            },
            {
                VegetationSpawnKind.DryTree,
                new[]
                {
                    "dry_tree",
                    "dry_tree_a",
                    "dry_tree_b",
                    "dry_tree_c",
                    "dry_sapling_a",
                    "dry_sapling_b"
                }
            },
            {
                VegetationSpawnKind.Rock,
                new[]
                {
                    "rock",
                    "rock_cluster_small",
                    "rock_cluster_large"
                }
            },
            {
                VegetationSpawnKind.GreenBush,
                new[]
                {
                    "green_bush",
                    "green_bush_a",
                    "green_bush_b",
                    "green_bush_c",
                    "green_bush_d",
                    "forest_shrub_a",
                    "forest_shrub_b",
                    "forest_shrub_c"
                }
            },
            {
                VegetationSpawnKind.DryBush,
                new[]
                {
                    "dry_bush",
                    "dry_bush_a",
                    "dry_bush_b",
                    "dry_bush_c",
                    "dry_bush_d"
                }
            },
            {
                VegetationSpawnKind.GrassOrFlower,
                new[]
                {
                    "grass_or_flower",
                    "tall_grass_clump_a",
                    "tall_grass_clump_b",
                    "tall_grass_clump_c",
                    "wildflower_patch_a",
                    "wildflower_patch_b",
                    "wildflower_patch_c",
                    "reed_patch_a",
                    "reed_patch_b",
                    "mushroom_patch_a",
                    "mushroom_patch_b",
                    "herb_patch_a",
                    "herb_patch_b"
                }
            },
            {
                VegetationSpawnKind.BerryBush,
                new[]
                {
                    "berry_bush",
                    "berry_bush_a",
                    "berry_bush_b",
                    "berry_bush_c",
                    "berry_bush_d"
                }
            }
        };

        private static readonly Dictionary<string, string[]> BuildingModelsById = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "campfire", new[] { "campfire" } },
            { "campfire_burned", new[] { "campfire_burned" } },
            { "storage_box", new[] { "storage_box" } },
            { "tent", new[] { "tent" } },
            { "wall", new[] { "wall" } },
            { "trap", new[] { "trap" } },
            { "deadfall_trap_heavy", new[] { "deadfall_trap_heavy" } },
            { "snare_trap", new[] { "snare_trap" } },
            { "spike_barrier", new[] { "spike_barrier" } },
            { "lean_to_shelter", new[] { "lean_to_shelter" } },
            { "tanning_rack", new[] { "tanning_rack" } },
            { "drying_rack", new[] { "drying_rack" } }
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

            Dictionary<string, GameObject> resourceModelLookup = BuildModelLookup(VegetationDestination, WorldResourcesDestination);
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

            string sourceRoot = Path.Combine(projectRoot, SourceRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(sourceRoot))
            {
                Debug.LogWarning($"Generated asset source root not found: {sourceRoot}");
                return;
            }

            int copied = 0;
            foreach (KeyValuePair<string, string> mapping in CategoryDestinations)
            {
                EnsureAssetDirectory(mapping.Value);
                copied += CopyModelFiles(Path.Combine(sourceRoot, mapping.Key), mapping.Value);
            }

            int copiedToResources = 0;
            foreach ((string sourceCategory, string resourcesDestination) in ResourcesCopyMappings)
            {
                EnsureAssetDirectory(resourcesDestination);
                copiedToResources += CopyModelFiles(Path.Combine(sourceRoot, sourceCategory), resourcesDestination);
            }

            Debug.Log($"Copied {copied} generated bushcraft model files into {ArtRoot}.");
            Debug.Log($"Copied {copiedToResources} generated bushcraft model files into {ResourcesRoot} for runtime resolution.");
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

        private static Dictionary<string, GameObject> BuildModelLookup(params string[] modelsRoots)
        {
            var validRoots = modelsRoots.Where(AssetDatabase.IsValidFolder).ToArray();
            if (validRoots.Length == 0)
            {
                return new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            }

            return AssetDatabase
                .FindAssets("t:Model", validRoots)
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
                        Debug.LogWarning($"Missing bushcraft resource model '{modelName}' for {mapping.Key} under {VegetationDestination} or {WorldResourcesDestination}.");
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
