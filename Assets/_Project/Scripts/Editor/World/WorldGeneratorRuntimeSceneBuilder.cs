using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Debugging;
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
using ApexShift.EditorTools.World;

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
            generatorGo.AddComponent<WorldMapDebugWindow>();

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

            // Build NavMesh
            BuildNavMesh(generatorGo);

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

            const string scenePath = "Assets/_Project/Scenes/RuntimeWorld.unity";
            EditorSceneManager.SetActiveScene(newScene);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                AssetDatabase.DeleteAsset(scenePath);
            }
            AssetDatabase.Refresh();

            EditorSceneManager.MarkSceneDirty(newScene);
            EditorSceneManager.SaveScene(newScene, scenePath);
            EditorSceneManager.OpenScene(scenePath);
            Debug.Log("Runtime World Generator scene created and saved at Assets/_Project/Scenes/RuntimeWorld.unity");
        }

        private static void PopulateResourcePrefabs(SerializedObject so)
        {
            var prop = so.FindProperty("resourcePrefabs");
            prop.ClearArray();

            // Runtime world can be procedural, but vegetation identity must use the same
            // role resolver as HandcraftedBiomeWorldBuilder.
            AddResourceEntries(prop, VegetationSpawnKind.ConiferTree);
            AddResourceEntries(prop, VegetationSpawnKind.LeafyTree);
            AddResourceEntries(prop, VegetationSpawnKind.DryTree);
            AddResourceEntries(prop, VegetationSpawnKind.Rock);
            AddResourceEntries(prop, VegetationSpawnKind.GreenBush);
            AddResourceEntries(prop, VegetationSpawnKind.DryBush);
            AddResourceEntries(prop, VegetationSpawnKind.BerryBush);
            AddResourceEntries(prop, VegetationSpawnKind.GrassOrFlower);
        }

        private static void AddResourceEntries(SerializedProperty listProp, VegetationSpawnKind kind)
        {
            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            List<ScoredResourcePrefab> prefabs = FindResourcePrefabsForKind(kind);

            if (prefabs.Count == 0)
            {
                Debug.LogWarning($"Runtime vegetation catalog: no prefab found for {kind}. Fallback primitives will be used.");
                return;
            }

            foreach (ScoredResourcePrefab scored in prefabs)
            {
                UpgradeMaterialsToURP(scored.Prefab, urpLit);

                int index = listProp.arraySize;
                listProp.InsertArrayElementAtIndex(index);
                var entry = listProp.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("kind").enumValueIndex = (int)kind;
                entry.FindPropertyRelative("prefab").objectReferenceValue = scored.Prefab;
            }

            Debug.Log($"Runtime vegetation catalog: {kind} prefabs={prefabs.Count} best={prefabs[0].Prefab.name} score={prefabs[0].Score}");
        }

        private static List<ScoredResourcePrefab> FindResourcePrefabsForKind(VegetationSpawnKind kind)
        {
            IReadOnlyList<GameObject> prefabs = HandcraftedBiomeWorldBuilder.VegetationPrefabSelectionRules.FindPrefabsForRole(kind.ToString());
            List<ScoredResourcePrefab> result = new List<ScoredResourcePrefab>();
            foreach (GameObject prefab in prefabs)
            {
                result.Add(new ScoredResourcePrefab(prefab, 0));
            }

            return result;
        }

        private static string[] GetManualOverrideNamesForKind(VegetationSpawnKind kind)
        {
            return kind switch
            {
                VegetationSpawnKind.DryTree => new[] { "tree_04.4" },
                VegetationSpawnKind.ConiferTree => new[] { "tree_02.1" },
                VegetationSpawnKind.LeafyTree => new[] { "tree_04" },
                VegetationSpawnKind.Rock => new[] { "stone_01" },
                VegetationSpawnKind.GreenBush => new[] { "bush_02.1" },
                VegetationSpawnKind.DryBush => new[] { "bush_02.2" },
                _ => System.Array.Empty<string>()
            };
        }

        private static string[] GetKindKeywords(VegetationSpawnKind kind)
        {
            return kind switch
            {
                VegetationSpawnKind.ConiferTree => new[] { "pine", "conifer", "spruce", "fir", "tree" },
                VegetationSpawnKind.LeafyTree => new[] { "oak", "leaf", "broadleaf", "deciduous", "tree" },
                VegetationSpawnKind.DryTree => new[] { "dead", "dry", "bare", "tree" },
                VegetationSpawnKind.Rock => new[] { "rock", "stone", "boulder" },
                VegetationSpawnKind.GreenBush => new[] { "bush", "shrub", "plant" },
                VegetationSpawnKind.DryBush => new[] { "dry", "dead", "bush", "shrub" },
                VegetationSpawnKind.GrassOrFlower => new[] { "grass", "flower", "flover", "plant" },
                VegetationSpawnKind.BerryBush => new[] { "berry", "berries", "fruit", "bush" },
                _ => System.Array.Empty<string>()
            };
        }

        private static bool TryGetManualVegetationKind(string assetPath, string prefabName, out VegetationSpawnKind kind)
        {
            string normalizedPath = NormalizeAssetName(assetPath);
            string normalizedName = NormalizeAssetName(prefabName);

            if (IsSnowVariant(normalizedPath, normalizedName))
            {
                kind = default;
                return false;
            }

            if (normalizedName == "tree_04_4" || normalizedPath.Contains("/tree_04_4"))
            {
                kind = VegetationSpawnKind.DryTree;
                return true;
            }

            if (normalizedName == "tree_02_1" || normalizedPath.Contains("/tree_02_1"))
            {
                kind = VegetationSpawnKind.ConiferTree;
                return true;
            }

            if (normalizedName == "tree_04" || normalizedPath.Contains("/tree_04"))
            {
                kind = VegetationSpawnKind.LeafyTree;
                return true;
            }

            if (normalizedName == "stone_01" || normalizedPath.Contains("/stone_01"))
            {
                kind = VegetationSpawnKind.Rock;
                return true;
            }

            if (normalizedName == "bush_02_2" || normalizedPath.Contains("/bush_02_2"))
            {
                kind = VegetationSpawnKind.DryBush;
                return true;
            }

            if (normalizedName == "bush_02_1" || normalizedPath.Contains("/bush_02_1"))
            {
                kind = VegetationSpawnKind.GreenBush;
                return true;
            }

            kind = default;
            return false;
        }

        private static int ScorePrefabForKind(string path, GameObject prefab, VegetationSpawnKind kind, string keyword)
        {
            string text = (path + " " + prefab.name).ToLowerInvariant();

            if (IsForbiddenForAllNature(text))
            {
                return -1000;
            }

            int score = 0;
            if (text.Contains(keyword.ToLowerInvariant())) score += 15;
            if (text.Contains("nature")) score += 25;
            if (text.Contains("nature pack")) score += 35;

            score += ScoreKindText(text, kind);
            score += ScoreKindColor(prefab, kind);
            return score;
        }

        private static bool IsForbiddenForAllNature(string text)
        {
            return text.Contains("stylizedwoodmonsters")
                || text.Contains("animationgallery")
                || text.Contains("player")
                || text.Contains("character")
                || text.Contains("enemy")
                || text.Contains("monster")
                || text.Contains("vfx")
                || text.Contains("effect");
        }

        private static int ScoreKindText(string text, VegetationSpawnKind kind)
        {
            switch (kind)
            {
                case VegetationSpawnKind.ConiferTree:
                    if (text.Contains("pine") || text.Contains("conifer") || text.Contains("spruce") || text.Contains("fir")) return 60;
                    if (text.Contains("dead") || text.Contains("dry") || text.Contains("bare") || text.Contains("rock") || text.Contains("bush")) return -80;
                    if (text.Contains("snow") || text.Contains("winter")) return -40;
                    return text.Contains("tree") ? 10 : -30;

                case VegetationSpawnKind.LeafyTree:
                    if (text.Contains("oak") || text.Contains("leaf") || text.Contains("broadleaf") || text.Contains("deciduous")) return 60;
                    if (text.Contains("pine") || text.Contains("conifer") || text.Contains("spruce") || text.Contains("fir")) return -90;
                    if (text.Contains("dead") || text.Contains("dry") || text.Contains("bare") || text.Contains("rock")) return -90;
                    if (text.Contains("snow") || text.Contains("winter")) return -50;
                    return text.Contains("tree") ? 12 : -30;

                case VegetationSpawnKind.DryTree:
                    if (text.Contains("dead") || text.Contains("dry") || text.Contains("bare")) return 80;
                    if (text.Contains("pine") || text.Contains("conifer") || text.Contains("spruce") || text.Contains("green") || text.Contains("leaf")) return -80;
                    return text.Contains("tree") ? 10 : -30;

                case VegetationSpawnKind.Rock:
                    if (text.Contains("rock") || text.Contains("stone") || text.Contains("boulder")) return 80;
                    if (text.Contains("tree") || text.Contains("bush") || text.Contains("grass") || text.Contains("flower")) return -100;
                    return -40;

                case VegetationSpawnKind.GreenBush:
                    if (text.Contains("bush") || text.Contains("shrub") || text.Contains("plant")) return 50;
                    if (text.Contains("dead") || text.Contains("dry") || text.Contains("rock") || text.Contains("tree")) return -60;
                    return -20;

                case VegetationSpawnKind.DryBush:
                    if ((text.Contains("dry") || text.Contains("dead")) && (text.Contains("bush") || text.Contains("shrub") || text.Contains("plant"))) return 80;
                    if (text.Contains("bush") || text.Contains("shrub")) return 15;
                    if (text.Contains("green") || text.Contains("flower") || text.Contains("tree")) return -60;
                    return -20;

                case VegetationSpawnKind.GrassOrFlower:
                    if (text.Contains("grass") || text.Contains("flower") || text.Contains("flover")) return 70;
                    if (text.Contains("tree") || text.Contains("rock")) return -80;
                    return text.Contains("plant") ? 15 : -30;

                case VegetationSpawnKind.BerryBush:
                    if (text.Contains("berry") || text.Contains("berries") || text.Contains("fruit")) return 80;
                    if (text.Contains("bush")) return 15;
                    if (text.Contains("tree") || text.Contains("rock") || text.Contains("dead") || text.Contains("dry")) return -80;
                    return -30;

                default:
                    return 0;
            }
        }

        private static int ScoreKindColor(GameObject prefab, VegetationSpawnKind kind)
        {
            Color color = EstimatePrefabColor(prefab);
            if (color == Color.clear) return 0;

            bool greenDominant = color.g > color.r * 1.15f && color.g > color.b * 1.15f;
            bool grayDominant = Mathf.Abs(color.r - color.g) < 0.08f && Mathf.Abs(color.g - color.b) < 0.08f;
            bool warmDry = color.r > color.g * 1.15f && color.g >= color.b;

            switch (kind)
            {
                case VegetationSpawnKind.ConiferTree:
                case VegetationSpawnKind.LeafyTree:
                case VegetationSpawnKind.GreenBush:
                case VegetationSpawnKind.GrassOrFlower:
                case VegetationSpawnKind.BerryBush:
                    return greenDominant ? 15 : 0;
                case VegetationSpawnKind.Rock:
                    return grayDominant ? 20 : 0;
                case VegetationSpawnKind.DryTree:
                case VegetationSpawnKind.DryBush:
                    return warmDry ? 20 : 0;
                default:
                    return 0;
            }
        }

        private static Color EstimatePrefabColor(GameObject prefab)
        {
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null) continue;
                    if (material.HasProperty("_BaseColor")) return material.GetColor("_BaseColor");
                    if (material.HasProperty("_Color")) return material.GetColor("_Color");
                }
            }

            return Color.clear;
        }

        private static bool IsSnowVariant(string assetPath, string prefabName)
        {
            string normalizedPath = NormalizeAssetName(assetPath);
            string normalizedName = NormalizeAssetName(prefabName);
            return normalizedPath.Contains("_snow") || normalizedName.Contains("_snow") || normalizedPath.Contains("snow") || normalizedName.Contains("snow");
        }

        private static string NormalizeAssetName(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            string normalized = value.Replace('\\', '/').ToLowerInvariant();
            normalized = normalized.Replace('.', '_');
            return normalized;
        }
        private readonly struct ScoredResourcePrefab
        {
            public ScoredResourcePrefab(GameObject prefab, int score)
            {
                Prefab = prefab;
                Score = score;
            }

            public GameObject Prefab { get; }

            public int Score { get; }
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
            var adapters = GameObject.FindObjectsByType<CreatureNavigationAdapter>(FindObjectsInactive.Exclude);
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

            // Use the EmbersStorm palette texture as fallback for older materials.
            Texture2D naturePalette = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EmbersStorm -  Free Nature Pack/Texture/Texture.png");

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
