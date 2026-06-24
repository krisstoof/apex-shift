using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Biomes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApexShift.Editor.World
{
    public static class WorldGeneratorRuntimeSceneBuilder
    {
        private const string CatalogPath = "Assets/_Project/Data/Biomes/BiomeCatalog.asset";

        [MenuItem("Tools/Apex Shift/World/Create Runtime World Generator Scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Cannot create scene while in play mode.");
                return;
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            GameObject generatorGo = new GameObject("RuntimeWorldGenerator");
            WorldGeneratorRuntime generator = generatorGo.AddComponent<WorldGeneratorRuntime>();

            BiomeCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(CatalogPath);
            if (catalog != null)
            {
                generator.SetBiomeCatalog(catalog);
            }
            else
            {
                Debug.LogWarning($"Biome Catalog not found at {CatalogPath}. Please ensure it exists.");
            }

            // Optional: Create a light and camera so the scene is viewable
            CreateDefaultEnvironment(newScene);

            generator.Generate();

            EditorSceneManager.MarkSceneDirty(newScene);
            Debug.Log("Runtime World Generator scene created.");
        }

        private static void CreateDefaultEnvironment(Scene scene)
        {
            GameObject lightGo = new GameObject("Directional Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject camGo = new GameObject("Main Camera");
            Camera cam = camGo.AddComponent<Camera>();
            camGo.transform.position = new Vector3(0, 50, -60);
            camGo.transform.rotation = Quaternion.Euler(45, 0, 0);
            camGo.tag = "MainCamera";
        }
    }
}
