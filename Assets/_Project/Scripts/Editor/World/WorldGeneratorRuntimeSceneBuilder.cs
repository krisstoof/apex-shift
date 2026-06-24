using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.Creatures;
using ApexShift.Presentation.HUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Collections.Generic;
using System.Linq;

namespace ApexShift.Editor.World
{
    public static class WorldGeneratorRuntimeSceneBuilder
    {
        private const string CatalogPath = "Assets/_Project/Data/Biomes/BiomeCatalog.asset";
        private const string InputActionsPath = "Assets/_Project/Input/ApexShiftInputActions.inputactions";
        private const string PlayerPrefabPath = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Prefab/Player.prefab";
        private const string PlayerACPath = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Animations/Animations Controllers/AC_Player.controller";

        [MenuItem("Tools/Apex Shift/World/Create Runtime World Generator Scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Cannot create scene while in play mode.");
                return;
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            BiomeCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(CatalogPath);
            if (catalog == null)
            {
                BiomeDataAssetCreator.CreateDefaultAssets();
                catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(CatalogPath);
            }

            GameObject generatorGo = new GameObject("RuntimeWorldGenerator");
            WorldGeneratorRuntime generator = generatorGo.AddComponent<WorldGeneratorRuntime>();
            generatorGo.AddComponent<RuntimeHUDProvisioner>();

            generator.SetBiomeCatalog(catalog);
            
            // Disable auto-generate on start to prevent double generation in Play mode
            var soGenerator = new SerializedObject(generator);
            soGenerator.FindProperty("generateOnStart").boolValue = false;
            soGenerator.ApplyModifiedProperties();

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);

            // Populate Assets
            var so = new SerializedObject(generator);
            if (inputActions != null)
            {
                so.FindProperty("inputActions").objectReferenceValue = inputActions;
            }

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab != null)
            {
                so.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;
            }

            RuntimeAnimatorController playerAC = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerACPath);
            if (playerAC != null)
            {
                so.FindProperty("playerAnimatorController").objectReferenceValue = playerAC;
            }

            PopulateResourcePrefabs(so);
            PopulateCreaturePrefabs(so);
            
            so.ApplyModifiedProperties();

            // Generate once in Edit mode
            generator.Generate();

            // Manually trigger HUD creation for Edit Mode visibility
            GameObject player = GameObject.Find("Player");
            if (player != null)
            {
                var provisioner = generatorGo.GetComponent<RuntimeHUDProvisioner>();
                if (provisioner != null)
                {
                    provisioner.CreateHUD(player);
                }
            }

            EditorSceneManager.MarkSceneDirty(newScene);
Debug.Log("Runtime World Generator scene created with Player model and URP shader conversion.");
        }

        private static void PopulateResourcePrefabs(SerializedObject so)
        {
            var prop = so.FindProperty("resourcePrefabs");
            prop.ClearArray();

            // Conifers - Pine trees
            AddResourceEntries(prop, VegetationSpawnKind.ConiferTree, "Tree_01", "Tree_01.1");
            AddResourceEntries(prop, VegetationSpawnKind.ConiferTree, "Tree_02.1"); 

            // Leafy Trees
            AddResourceEntries(prop, VegetationSpawnKind.LeafyTree, "Tree_04", "Tree_05");
            
            // Dry Trees
            AddResourceEntries(prop, VegetationSpawnKind.DryTree, "Tree_03", "Tree_04_4");

            // Rocks
            AddResourceEntries(prop, VegetationSpawnKind.Rock, "Stone_01", "Stone_02", "Stone_03", "Stone_04");

            // Bushes
            AddResourceEntries(prop, VegetationSpawnKind.GreenBush, "Bush_01", "Bush_02_1");
            AddResourceEntries(prop, VegetationSpawnKind.DryBush, "Bush_02", "Bush_02_2");
            AddResourceEntries(prop, VegetationSpawnKind.BerryBush, "Bush_01.3", "Bush_01.2");

            // Flowers/Grass
            AddResourceEntries(prop, VegetationSpawnKind.GrassOrFlower, "Grass_01", "Flover_01", "Flover_02", "Flover_03");
        }

        private static void AddResourceEntries(SerializedProperty listProp, VegetationSpawnKind kind, params string[] searchNames)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

            foreach (var name in searchNames)
            {
                string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.Contains("LOD") && !path.Contains("_snow"))
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null) continue;

                        bool isMatch = false;
                        string pName = prefab.name;

                        if (pName.Equals(name, System.StringComparison.OrdinalIgnoreCase) ||
                            pName.StartsWith(name + ".", System.StringComparison.OrdinalIgnoreCase) ||
                            pName.StartsWith(name + "_", System.StringComparison.OrdinalIgnoreCase))
                        {
                            isMatch = true;
                        }

                        if (isMatch)
                        {
                            UpgradeMaterialsToURP(prefab, urpLit);

                            int index = listProp.arraySize;
                            listProp.InsertArrayElementAtIndex(index);
                            var entry = listProp.GetArrayElementAtIndex(index);
                            entry.FindPropertyRelative("kind").enumValueIndex = (int)kind;
                            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                        }
                    }
                }
            }
        }

        private static void BuildNavMesh(GameObject generatorGo)
        {
            Transform terrainRoot = generatorGo.transform.Find("TerrainRoot");
            if (terrainRoot == null) return;

            NavMeshSurface surface = terrainRoot.gameObject.GetComponent<NavMeshSurface>();
            if (surface == null) surface = terrainRoot.gameObject.AddComponent<NavMeshSurface>();

            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            // Warp creatures to nearest NavMesh
            var adapters = GameObject.FindObjectsByType<CreatureNavigationAdapter>(FindObjectsSortMode.None);
            foreach (var adapter in adapters)
            {
                adapter.WarpToNearestNavMesh();
            }
        }

        private static void PopulateCreaturePrefabs(SerializedObject so)
        {
            var prop = so.FindProperty("creaturePrefabs");
            prop.ClearArray();

            // Using reliable animal models from ithappy Animals_FREE
            AddCreatureEntries(prop, "small_prey", "Chicken_001", "Dog_001");
            AddCreatureEntries(prop, "grazer", "Deer_001");
            AddCreatureEntries(prop, "varnak", "Tiger_001");
        }

        private static void AddCreatureEntries(SerializedProperty listProp, string creatureId, params string[] searchNames)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");

            foreach (var name in searchNames)
            {
                string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.Contains("LOD"))
                    {
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null && prefab.name.Contains(name))
                        {
                            UpgradeMaterialsToURP(prefab, urpLit);

                            int index = listProp.arraySize;
                            listProp.InsertArrayElementAtIndex(index);
                            var entry = listProp.GetArrayElementAtIndex(index);
                            entry.FindPropertyRelative("creatureId").stringValue = creatureId;
                            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                        }
                    }
                }
            }
        }

        private static void UpgradeMaterialsToURP(GameObject prefab, Shader urpLit)
        {
            if (urpLit == null) return;

            // Load the nature palette texture as fallback
            Texture2D naturePalette = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Low Poly Trees & Nature Pack (70 Props)/Textures/Main_texture.png");

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat != null)
                    {
                        string sName = mat.shader.name;
                        bool isURP = sName.Contains("Universal Render Pipeline/Lit");
                        bool isLegacy = sName == "Standard" || sName.Contains("Built-in") || sName.Contains("Toon");

                        if (isURP || isLegacy)
                        {
                            Undo.RecordObject(mat, "Upgrade to URP Lit");
                            
                            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                            if (mainTex == null && mat.HasProperty("_BaseMap"))
                                mainTex = mat.GetTexture("_BaseMap");

                            // If nature prefab/material and no texture, assign palette
                            bool isNature = prefab.name.ToLower().Contains("tree") || prefab.name.ToLower().Contains("bush") || 
                                           prefab.name.ToLower().Contains("stone") || mat.name.ToLower().Contains("standard");
                            
                            if (mainTex == null && isNature)
                            {
                                mainTex = naturePalette;
                            }

                            Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                            if (mainColor == Color.white && mat.HasProperty("_BaseColor"))
                                mainColor = mat.GetColor("_BaseColor");

                            if (isLegacy)
                            {
                                mat.shader = urpLit;
                            }
                            
                            if (mainTex != null)
                            {
                                if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                                if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
                            }
                            
                            if (mat.HasProperty("_BaseColor"))
                                mat.SetColor("_BaseColor", mainColor);
                        }
                    }
                }
            }
        }


    }
}
