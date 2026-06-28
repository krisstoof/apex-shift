using System.Collections.Generic;
using System.IO;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Generation;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.World
{
    public static class EmbersStormPrefabSwapTool
    {
        private const string SourceRoot = "Assets/EmbersStorm -  Free Nature Pack/Prefabs";
        private const string WrapperRoot = "Assets/_Project/Prefabs/World/Resources/Embersstorm";
        private const string RegistryPath = "Assets/_Project/Data/World/PrefabRegistry.asset";

        [MenuItem("Tools/Apex Shift/World/Generate EmbersStorm Wrappers")]
        public static void GenerateWrappers()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/World");
            EnsureFolder("Assets/_Project/Prefabs/World/Resources");
            EnsureFolder(WrapperRoot);
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder("Assets/_Project/Data/World");

            PrefabRegistry registry = LoadOrCreateRegistry();
            SerializedObject registrySo = new SerializedObject(registry);
            SerializedProperty resourcesProp = registrySo.FindProperty("resourcePrefabs");
            resourcesProp.ClearArray();

            AddWrappersForKind(resourcesProp, VegetationSpawnKind.ConiferTree, "Trees/Pine tree", "ES_ConiferTree");
            AddWrappersForKind(resourcesProp, VegetationSpawnKind.LeafyTree, "Trees/Oak", "ES_LeafyTree");
            AddWrappersForKind(resourcesProp, VegetationSpawnKind.DryTree, "Trees/Dead Trees", "ES_DryTree");
            AddWrappersForKind(resourcesProp, VegetationSpawnKind.Rock, "Rocks", "ES_Rock");
            AddWrappersForKind(resourcesProp, VegetationSpawnKind.GreenBush, "Trees/Bush", "ES_GreenBush");
            AddWrappersForKind(resourcesProp, VegetationSpawnKind.DryBush, "Trees/Dead Trees", "ES_DryBush");
            AddWrappersForKind(resourcesProp, VegetationSpawnKind.BerryBush, "Plants/Plant", "ES_BerryBush");
            AddWrappersForKind(resourcesProp, VegetationSpawnKind.GrassOrFlower, "Plants/Grass", "ES_GrassPatch");

            registrySo.ApplyModifiedProperties();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"EmbersStorm prefab wrappers generated and registry updated at {RegistryPath}");
        }

        private static void AddWrappersForKind(SerializedProperty resourcesProp, VegetationSpawnKind kind, string sourceFolder, string wrapperPrefix)
        {
            string sourcePath = FindBestSourcePrefab(sourceFolder, kind);
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                Debug.LogWarning($"No EmbersStorm source prefab found for {kind} in {sourceFolder}");
                return;
            }

            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (sourcePrefab == null)
            {
                Debug.LogWarning($"Unable to load source prefab for {kind}: {sourcePath}");
                return;
            }

            string wrapperPath = $"{WrapperRoot}/{wrapperPrefix}.prefab";
            GameObject wrapper = BuildWrapperPrefab(sourcePrefab, kind, wrapperPrefix);
            if (wrapper == null)
            {
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(wrapper, wrapperPath);
            Object.DestroyImmediate(wrapper);

            int index = resourcesProp.arraySize;
            resourcesProp.InsertArrayElementAtIndex(index);
            SerializedProperty entry = resourcesProp.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("kind").enumValueIndex = (int)kind;
            entry.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(wrapperPath);
        }

        private static GameObject BuildWrapperPrefab(GameObject sourcePrefab, VegetationSpawnKind kind, string wrapperName)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                return null;
            }

            instance.name = wrapperName;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = GetBaseScale(kind);

            ResizeNatureTextureScale(instance);

            ResourceNodeView node = instance.GetComponent<ResourceNodeView>() ?? instance.AddComponent<ResourceNodeView>();
            node.ConfigureDefault(GetResourceKindId(kind));

            return instance;
        }

        private static string GetResourceKindId(VegetationSpawnKind kind)
        {
            return kind switch
            {
                VegetationSpawnKind.ConiferTree => "conifer_tree",
                VegetationSpawnKind.LeafyTree => "leafy_tree",
                VegetationSpawnKind.DryTree => "dry_tree",
                VegetationSpawnKind.Rock => "rock",
                VegetationSpawnKind.GreenBush => "green_bush",
                VegetationSpawnKind.DryBush => "dry_bush",
                VegetationSpawnKind.GrassOrFlower => "grass_patch",
                VegetationSpawnKind.BerryBush => "berry_bush",
                _ => kind.ToString().ToLowerInvariant()
            };
        }

        private static Vector3 GetBaseScale(VegetationSpawnKind kind)
        {
            return kind switch
            {
                VegetationSpawnKind.ConiferTree => Vector3.one * 0.20f,
                VegetationSpawnKind.LeafyTree => Vector3.one * 0.20f,
                VegetationSpawnKind.DryTree => Vector3.one * 0.18f,
                VegetationSpawnKind.Rock => Vector3.one * 0.22f,
                VegetationSpawnKind.GreenBush => Vector3.one * 0.25f,
                VegetationSpawnKind.DryBush => Vector3.one * 0.22f,
                VegetationSpawnKind.GrassOrFlower => Vector3.one * 0.15f,
                VegetationSpawnKind.BerryBush => Vector3.one * 0.20f,
                _ => Vector3.one * 0.2f
            };
        }

        private static void ResizeNatureTextureScale(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    string name = material.name.ToLowerInvariant();
                    if (!name.Contains("tree") && !name.Contains("bush") && !name.Contains("rock") && !name.Contains("plant") && !name.Contains("grass"))
                    {
                        continue;
                    }

                    Vector2 scale = name.Contains("rock") ? new Vector2(0.7f, 0.7f) : new Vector2(0.55f, 0.55f);
                    if (material.HasProperty("_BaseMap"))
                    {
                        material.SetTextureScale("_BaseMap", scale);
                    }

                    if (material.HasProperty("_MainTex"))
                    {
                        material.SetTextureScale("_MainTex", scale);
                    }
                }
            }
        }

        private static PrefabRegistry LoadOrCreateRegistry()
        {
            PrefabRegistry registry = AssetDatabase.LoadAssetAtPath<PrefabRegistry>(RegistryPath);
            if (registry != null)
            {
                return registry;
            }

            registry = ScriptableObject.CreateInstance<PrefabRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            return registry;
        }

        private static string FindBestSourcePrefab(string folder, VegetationSpawnKind kind)
        {
            string searchRoot = $"{SourceRoot}/{folder}";
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchRoot });
            string bestPath = string.Empty;
            int bestScore = int.MinValue;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (name.Contains("snow") || name.Contains("winter") || path.ToLowerInvariant().Contains("snow") || path.ToLowerInvariant().Contains("winter"))
                {
                    continue;
                }
                int score = ScorePrefabPath(name, kind);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPath = path;
                }
            }

            return bestPath;
        }

        private static int ScorePrefabPath(string name, VegetationSpawnKind kind)
        {
            if (name.Contains("pine tree .024")
                || name.Contains("pine tree .025")
                || name.Contains("pine tree .026")
                || name.Contains("pine tree .027")
                || name.Contains("pine tree .028")
                || name.Contains("pine tree .029")
                || name.Contains("pine tree .030")
                || name.Contains("pine tree .031")
                || name.Contains("pine tree .032")
                || name.Contains("pine tree .033")
                || name.Contains("pine tree .034")
                || name.Contains("pine tree .035")
                || name.Contains("pine tree .036")
                || name.Contains("pine tree .037")
                || name.Contains("pine tree .038")
                || name.Contains("pine tree .039")
                || name.Contains("pine tree .040"))
            {
                return -1000;
            }

            switch (kind)
            {
                case VegetationSpawnKind.ConiferTree:
                    return name.Contains("pine") ? 100 : name.Contains("tree") ? 50 : 0;
                case VegetationSpawnKind.LeafyTree:
                    return name.Contains("oak") ? 100 : name.Contains("tree") ? 50 : 0;
                case VegetationSpawnKind.DryTree:
                    return name.Contains("dead") ? 100 : name.Contains("tree") ? 50 : 0;
                case VegetationSpawnKind.Rock:
                    return name.Contains("rock") ? 100 : 0;
                case VegetationSpawnKind.GreenBush:
                    return name.Contains("bush") ? 100 : name.Contains("plant") ? 40 : 0;
                case VegetationSpawnKind.DryBush:
                    return name.Contains("dead") || name.Contains("dry") ? 100 : name.Contains("bush") ? 40 : 0;
                case VegetationSpawnKind.GrassOrFlower:
                    return name.Contains("grass") ? 100 : name.Contains("flower") ? 70 : 20;
                case VegetationSpawnKind.BerryBush:
                    return name.Contains("berry") ? 100 : name.Contains("plant") ? 50 : 0;
                default:
                    return 0;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (!string.IsNullOrWhiteSpace(parent))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
