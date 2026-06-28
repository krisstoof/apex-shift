using System;
using System.Collections.Generic;
using System.IO;
using ApexShift.Runtime.Bootstrap;
using ApexShift.EditorTools.Camera;
using ApexShift.Runtime.Camera;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.World;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Biomes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using CameraComponent = UnityEngine.Camera;
using Object = UnityEngine.Object;
using ApexShift.Presentation.HUD;

namespace ApexShift.EditorTools.World
{
    public static class HandcraftedBiomeWorldBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/BiomeWorldTest.unity";
        private const string MaterialFolder = "Assets/_Project/Materials/Biomes";
        private const string CatalogPath = "Assets/_Project/Data/Biomes/BiomeCatalog.asset";
        private const string PlayerPrefabPath = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Prefab/Player.prefab";
        private const string InputActionsPath = "Assets/_Project/Input/ApexShiftInputActions.inputactions";
        private const string PlayerControllerPath = "Assets/_Project/Animations/Player/PlayerPrototype.controller";
        private const float TileSize = 3.5f;
        private const float IslandRadiusX = 56f;
        private const float IslandRadiusZ = 43f;
        private static readonly System.Random Random = new System.Random(1337);

        private static BiomeCatalogAsset _catalog;

        [MenuItem("Tools/Apex Shift/World/Create Handcrafted Biome World")]
        public static void CreateHandcraftedBiomeWorld()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Create Handcrafted Biome World can only run in Edit mode.");
                return;
            }

            _catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(CatalogPath);
            if (_catalog == null)
            {
                Debug.LogWarning($"Biome Catalog not found at {CatalogPath}. Falling back to hardcoded values.");
            }

            EnsureFolders();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject gameRoot = new GameObject("Game");
            SceneManager.MoveGameObjectToScene(gameRoot, scene);

            CreateBootstrapper(gameRoot.transform);

            GameObject worldRoot = CreateChild(gameRoot.transform, "WorldRoot");
            GameObject terrainRoot = CreateChild(worldRoot.transform, "TerrainRoot");
            GameObject biomeRoot = CreateChild(worldRoot.transform, "BiomeRoot");
            GameObject vegetationRoot = CreateChild(worldRoot.transform, "VegetationRoot");
            GameObject transitionRoot = CreateChild(worldRoot.transform, "BiomeTransitionRoot");

            CreateChild(worldRoot.transform, "ResourceRoot");
            CreateChild(worldRoot.transform, "CreatureRoot");
            CreateChild(worldRoot.transform, "BuildingRoot");

            BiomeMaterials materials = CreateBiomeMaterials();
            Dictionary<BiomeKind, List<Vector3>> biomeTiles = CreateIslandTerrain(terrainRoot.transform, biomeRoot.transform, materials);
            List<Vector3> allTiles = FlattenTileCenters(biomeTiles);
            CreateOutsideVoid(terrainRoot.transform);
            VegetationCatalog vegetationCatalog = BuildVegetationCatalog();
            LogVegetationCatalog(vegetationCatalog);
            CreateVisualBoundary(worldRoot.transform, allTiles, vegetationCatalog.Rocks);
            GameObject worldBoundsObject = CreateChild(worldRoot.transform, "WorldBounds");
            WorldBounds worldBounds = worldBoundsObject.AddComponent<WorldBounds>();
            worldBounds.Configure(TileSize, allTiles);
            SpawnBiomeVegetation(vegetationRoot.transform, biomeTiles, vegetationCatalog);
            CreateBiomeMarkers(biomeRoot.transform);
            CreateStartClearing(terrainRoot.transform, vegetationRoot.transform, Vector3.zero, new Vector2(8f, 8f), materials.Get(BiomeKind.HearthMeadow));

            GameObject player = CreatePlayer(gameRoot.transform);
            GameObject cameraObject = CinemachineCameraSceneBuilder.CreateIsometricCameraRig(
                gameRoot.transform,
                player.transform,
                pitch: 35.264f,
                yaw: 45f,
                roll: 0f,
                orthographicSize: 14f,
                followDistance: 20f);
            ConfigurePlayerRuntime(player, cameraObject);
            CreateLight(gameRoot.transform);

            GameObject uiRoot = CreateHUD(gameRoot.transform, player);
            CreateChild(gameRoot.transform, "DebugRoot");

            NatureMaterialRepairUtility.RepairMaterialsUnder(worldRoot.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath);

            Debug.Log("Handcrafted biome world created at " + ScenePath);
            _catalog = null;
        }

        private static void CreateBiome(
            Transform terrainRoot,
            Transform biomeRoot,
            Transform vegetationRoot,
            string biomeName,
            Vector3 center,
            Vector2 size,
            Material material,
            IReadOnlyList<GameObject> primaryPrefabs,
            IReadOnlyList<GameObject> detailPrefabs,
            int primaryCount,
            int detailCount)
        {
            GameObject biomeMarker = CreateChild(biomeRoot, biomeName);
            biomeMarker.transform.position = center;

            GameObject vegetationParent = CreateChild(vegetationRoot, biomeName + "Vegetation");
            vegetationParent.transform.position = Vector3.zero;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = biomeName + "_Ground";
            ground.transform.SetParent(terrainRoot, false);
            ground.transform.position = center + new Vector3(0f, -0.05f, 0f);
            ground.transform.localScale = new Vector3(size.x, 0.1f, size.y);
            ApplyMaterial(ground, material);

            SpawnObjects(vegetationParent.transform, biomeName + "_Primary", center, size, primaryPrefabs, primaryCount, true);
            SpawnObjects(vegetationParent.transform, biomeName + "_Details", center, size, detailPrefabs, detailCount, false);
        }

        private static void CreateTransitionBiome(
            Transform terrainRoot,
            Transform transitionRoot,
            string biomeName,
            Vector3 center,
            Vector2 size,
            Material material,
            IReadOnlyList<GameObject> leftPrefabs,
            IReadOnlyList<GameObject> rightPrefabs,
            IReadOnlyList<GameObject> leftDetails,
            IReadOnlyList<GameObject> rightDetails)
        {
            GameObject transitionMarker = CreateChild(transitionRoot, biomeName);
            transitionMarker.transform.position = center;

            GameObject vegetationParent = CreateChild(transitionRoot, biomeName + "_Vegetation");

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = biomeName + "_Ground";
            ground.transform.SetParent(terrainRoot, false);
            ground.transform.position = center + new Vector3(0f, -0.05f, 0f);
            ground.transform.localScale = new Vector3(size.x, 0.1f, size.y);
            ApplyMaterial(ground, material);

            SpawnMixedObjects(vegetationParent.transform, biomeName + "_Primary", center, size, leftPrefabs, rightPrefabs, 14, true);
            SpawnMixedObjects(vegetationParent.transform, biomeName + "_Details", center, size, leftDetails, rightDetails, 16, false);
        }

        private static void SpawnObjects(
            Transform parent,
            string prefix,
            Vector3 center,
            Vector2 size,
            IReadOnlyList<GameObject> prefabs,
            int count,
            bool larger)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 position = RandomPoint(center, size, 2.5f);

                GameObject instance;
                GameObject prefab = Pick(prefabs);
                if (prefab != null)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.name = prefix + "_" + i.ToString("00");
                }
                else
                {
                    instance = CreateFallbackObject(prefix + "_" + i.ToString("00"), BiomeKind.HearthMeadow, VegetationRole.GreenBush, larger);
                }

                instance.transform.SetParent(parent, false);
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.Euler(0f, RandomRange(0f, 360f), 0f);

                float scale = larger ? RandomRange(0.85f, 1.35f) : RandomRange(0.45f, 0.9f);
                instance.transform.localScale = Vector3.one * scale;
            }
        }

        private static void SpawnMixedObjects(
            Transform parent,
            string prefix,
            Vector3 center,
            Vector2 size,
            IReadOnlyList<GameObject> leftPrefabs,
            IReadOnlyList<GameObject> rightPrefabs,
            int count,
            bool larger)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 position = RandomPoint(center, size, 1.75f);
                bool useLeft = Random.NextDouble() < 0.5;
                GameObject prefab = Pick(useLeft ? leftPrefabs : rightPrefabs);

                GameObject instance;
                if (prefab != null)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.name = prefix + "_" + i.ToString("00");
                }
                else
                {
                    instance = CreateFallbackObject(prefix + "_" + i.ToString("00"), BiomeKind.HearthMeadow, VegetationRole.GreenBush, larger);
                }

                instance.transform.SetParent(parent, false);
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.Euler(0f, RandomRange(0f, 360f), 0f);
                float scale = larger ? RandomRange(0.80f, 1.20f) : RandomRange(0.45f, 0.85f);
                instance.transform.localScale = Vector3.one * scale;
            }
        }

        private static void CreateStartClearing(
            Transform terrainRoot,
            Transform vegetationRoot,
            Vector3 center,
            Vector2 size,
            Material material)
        {
            GameObject clearing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            clearing.name = "HearthMeadow_StartClearing";
            clearing.transform.SetParent(terrainRoot, false);
            clearing.transform.position = center + new Vector3(0f, -0.045f, 0f);
            clearing.transform.localScale = new Vector3(size.x, 0.09f, size.y);
            ApplyMaterial(clearing, material);

            GameObject sparseDecor = CreateChild(vegetationRoot, "HearthMeadow_StartClearingDecor");
            SpawnObjects(sparseDecor.transform, "HearthMeadow_StartDetail", center, size, null, 3, false);
        }

        private static void CreatePathStrip(
            Transform terrainRoot,
            string name,
            Vector3 center,
            Vector2 size,
            Material material)
        {
            GameObject path = GameObject.CreatePrimitive(PrimitiveType.Cube);
            path.name = name;
            path.transform.SetParent(terrainRoot, false);
            path.transform.position = center + new Vector3(0f, -0.048f, 0f);
            path.transform.localScale = new Vector3(size.x, 0.06f, size.y);
            ApplyMaterial(path, material);
        }

        private static void CreateBiomeMarkers(Transform biomeRoot)
        {
            CreateBiomeMarker(biomeRoot, "Westwood", new Vector3(-34f, 0.1f, 0f));
            CreateBiomeMarker(biomeRoot, "Stoneback Ridge", new Vector3(0f, 0.1f, 28f));
            CreateBiomeMarker(biomeRoot, "Hearth Meadow", new Vector3(0f, 0.1f, 0f));
            CreateBiomeMarker(biomeRoot, "South Thicket", new Vector3(-4f, 0.1f, -24f));
            CreateBiomeMarker(biomeRoot, "Redfang Wilds", new Vector3(34f, 0.1f, -10f));
        }

        private static VegetationCatalog BuildVegetationCatalog()
        {
            VegetationPrefabSelectionRules.LogManualVegetationOverrides();
            return new VegetationCatalog
            {
                Conifers = VegetationPrefabSelectionRules.FindPrefabsForRole(VegetationRole.ConiferTree.ToString()),
                LeafyTrees = VegetationPrefabSelectionRules.FindPrefabsForRole(VegetationRole.LeafyTree.ToString()),
                DryTrees = VegetationPrefabSelectionRules.FindPrefabsForRole(VegetationRole.DryTree.ToString()),
                Rocks = VegetationPrefabSelectionRules.FindPrefabsForRole(VegetationRole.Rock.ToString()),
                GreenBushes = VegetationPrefabSelectionRules.FindPrefabsForRole(VegetationRole.GreenBush.ToString()),
                DryBushes = VegetationPrefabSelectionRules.FindPrefabsForRole(VegetationRole.DryBush.ToString()),
                GrassOrFlowers = VegetationPrefabSelectionRules.FindPrefabsForRole(VegetationRole.GrassOrFlower.ToString()),
                BerryBushes = VegetationPrefabSelectionRules.FindPrefabsForRole(VegetationRole.BerryBush.ToString())
            };
        }

        private static void LogVegetationCatalog(VegetationCatalog catalog)
        {
            Debug.Log($"Vegetation catalog: conifers={catalog.Conifers.Count}, leafy={catalog.LeafyTrees.Count}, dryTrees={catalog.DryTrees.Count}, rocks={catalog.Rocks.Count}, greenBushes={catalog.GreenBushes.Count}, dryBushes={catalog.DryBushes.Count}, grass={catalog.GrassOrFlowers.Count}, berries={catalog.BerryBushes.Count}");
        }

        private static IReadOnlyList<GameObject> FindPrefabsForRole(VegetationRole role)
        {
            return VegetationPrefabSelectionRules.FindPrefabsForRole(role.ToString());
        }

        private static void LogManualVegetationOverrides()
        {
            VegetationPrefabSelectionRules.LogManualVegetationOverrides();
        }

        private static bool TryGetManualVegetationRole(string assetPath, string prefabName, out VegetationRole role)
        {
            if (VegetationPrefabSelectionRules.TryGetManualVegetationRole(assetPath, prefabName, out string roleName) && Enum.TryParse(roleName, out VegetationRole parsedRole))
            {
                role = parsedRole;
                return true;
            }

            role = default;
            return false;
        }

        private static bool IsSnowVariant(string assetPath, string prefabName)
        {
            string normalizedPath = NormalizeAssetName(assetPath);
            string normalizedName = NormalizeAssetName(prefabName);
            if (IsSnowyPineVariant(normalizedPath, normalizedName))
            {
                return true;
            }
            return normalizedPath.Contains("_snow")
                || normalizedName.Contains("_snow")
                || normalizedPath.Contains("snow")
                || normalizedName.Contains("snow")
                || normalizedPath.Contains("winter")
                || normalizedName.Contains("winter");
        }

        private static bool IsSnowyPineVariant(string normalizedPath, string normalizedName)
        {
            if (!normalizedPath.Contains("/prefabs/trees/pine tree/") && !normalizedName.Contains("pine tree"))
            {
                return false;
            }

            return normalizedName.Contains("pine tree .024")
                || normalizedName.Contains("pine tree .025")
                || normalizedName.Contains("pine tree .026")
                || normalizedName.Contains("pine tree .027")
                || normalizedName.Contains("pine tree .028")
                || normalizedName.Contains("pine tree .029")
                || normalizedName.Contains("pine tree .030")
                || normalizedName.Contains("pine tree .031")
                || normalizedName.Contains("pine tree .032")
                || normalizedName.Contains("pine tree .033")
                || normalizedName.Contains("pine tree .034")
                || normalizedName.Contains("pine tree .035")
                || normalizedName.Contains("pine tree .036")
                || normalizedName.Contains("pine tree .037")
                || normalizedName.Contains("pine tree .038")
                || normalizedName.Contains("pine tree .039")
                || normalizedName.Contains("pine tree .040");
        }

        private static string NormalizeAssetName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string normalized = value.Replace('\\', '/').ToLowerInvariant();
            normalized = normalized.Replace('.', '_');
            return normalized;
        }

        private static string[] GetRoleKeywords(VegetationRole role)
        {
            return VegetationPrefabSelectionRules.GetRoleKeywords(role.ToString());
        }

        private static string[] GetManualOverrideNamesForRole(VegetationRole role)
        {
            return VegetationPrefabSelectionRules.GetManualOverrideNamesForRole(role.ToString());
        }

        private static int ScorePrefabForRole(string path, GameObject prefab, VegetationRole role, string keyword)
        {
            return 0;
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

        private static int ScoreRoleText(string text, VegetationRole role)
        {
            switch (role)
            {
                case VegetationRole.ConiferTree:
                    if (text.Contains("pine") || text.Contains("conifer") || text.Contains("spruce") || text.Contains("fir"))
                    {
                        return 60;
                    }
                    if (text.Contains("dead") || text.Contains("dry") || text.Contains("bare") || text.Contains("rock") || text.Contains("bush"))
                    {
                        return -80;
                    }
                    if (text.Contains("snow") || text.Contains("winter"))
                    {
                        return -40;
                    }
                    return text.Contains("tree") ? 10 : -30;

                case VegetationRole.LeafyTree:
                    if (text.Contains("oak") || text.Contains("leaf") || text.Contains("broadleaf") || text.Contains("deciduous"))
                    {
                        return 60;
                    }
                    if (text.Contains("pine") || text.Contains("conifer") || text.Contains("spruce") || text.Contains("fir"))
                    {
                        return -90;
                    }
                    if (text.Contains("dead") || text.Contains("dry") || text.Contains("bare") || text.Contains("rock"))
                    {
                        return -90;
                    }
                    if (text.Contains("snow") || text.Contains("winter"))
                    {
                        return -50;
                    }
                    return text.Contains("tree") ? 12 : -30;

                case VegetationRole.DryTree:
                    if (text.Contains("dead") || text.Contains("dry") || text.Contains("bare"))
                    {
                        return 80;
                    }
                    if (text.Contains("pine") || text.Contains("conifer") || text.Contains("spruce") || text.Contains("green") || text.Contains("leaf"))
                    {
                        return -80;
                    }
                    if (text.Contains("tree"))
                    {
                        return 10;
                    }
                    return -30;

                case VegetationRole.Rock:
                    if (text.Contains("rock") || text.Contains("stone") || text.Contains("boulder"))
                    {
                        return 80;
                    }
                    if (text.Contains("tree") || text.Contains("bush") || text.Contains("grass") || text.Contains("flower"))
                    {
                        return -100;
                    }
                    return -40;

                case VegetationRole.GreenBush:
                    if (text.Contains("bush") || text.Contains("shrub") || text.Contains("plant"))
                    {
                        return 50;
                    }
                    if (text.Contains("dead") || text.Contains("dry") || text.Contains("rock") || text.Contains("tree"))
                    {
                        return -60;
                    }
                    return -20;

                case VegetationRole.DryBush:
                    if ((text.Contains("dry") || text.Contains("dead")) && (text.Contains("bush") || text.Contains("shrub") || text.Contains("plant")))
                    {
                        return 80;
                    }
                    if (text.Contains("bush") || text.Contains("shrub"))
                    {
                        return 15;
                    }
                    if (text.Contains("green") || text.Contains("flower") || text.Contains("tree"))
                    {
                        return -60;
                    }
                    return -20;

                case VegetationRole.GrassOrFlower:
                    if (text.Contains("grass") || text.Contains("flower"))
                    {
                        return 70;
                    }
                    if (text.Contains("tree") || text.Contains("rock"))
                    {
                        return -80;
                    }
                    return text.Contains("plant") ? 15 : -30;

                case VegetationRole.BerryBush:
                    if (text.Contains("berry") || text.Contains("berries") || text.Contains("fruit"))
                    {
                        return 80;
                    }
                    if (text.Contains("bush"))
                    {
                        return 15;
                    }
                    if (text.Contains("tree") || text.Contains("rock") || text.Contains("dead") || text.Contains("dry"))
                    {
                        return -80;
                    }
                    return -30;

                default:
                    return 0;
            }
        }

        private static int ScoreRoleColor(GameObject prefab, VegetationRole role)
        {
            Color color = EstimatePrefabColor(prefab);
            if (color == Color.clear)
            {
                return 0;
            }

            bool greenDominant = color.g > color.r * 1.15f && color.g > color.b * 1.15f;
            bool grayDominant = Mathf.Abs(color.r - color.g) < 0.08f && Mathf.Abs(color.g - color.b) < 0.08f;
            bool warmDry = color.r > color.g * 1.15f && color.g >= color.b;

            switch (role)
            {
                case VegetationRole.ConiferTree:
                case VegetationRole.LeafyTree:
                case VegetationRole.GreenBush:
                case VegetationRole.GrassOrFlower:
                case VegetationRole.BerryBush:
                    return greenDominant ? 15 : 0;

                case VegetationRole.Rock:
                    return grayDominant ? 20 : 0;

                case VegetationRole.DryTree:
                case VegetationRole.DryBush:
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
                    if (material == null)
                    {
                        continue;
                    }

                    if (material.HasProperty("_BaseColor"))
                    {
                        return material.GetColor("_BaseColor");
                    }

                    if (material.HasProperty("_Color"))
                    {
                        return material.GetColor("_Color");
                    }
                }
            }

            return Color.clear;
        }

        private static string GetRoleId(VegetationRole role)
        {
            switch (role)
            {
                case VegetationRole.ConiferTree:
                    return "conifer_tree";
                case VegetationRole.LeafyTree:
                    return "leafy_tree";
                case VegetationRole.DryTree:
                    return "dry_tree";
                case VegetationRole.Rock:
                    return "rock";
                case VegetationRole.GreenBush:
                    return "green_bush";
                case VegetationRole.DryBush:
                    return "dry_bush";
                case VegetationRole.GrassOrFlower:
                    return "grass_or_flower";
                case VegetationRole.BerryBush:
                    return "berry_bush";
                default:
                    return role.ToString().ToLowerInvariant();
            }
        }

        private static float GetMinScaleForRole(BiomeIdentityProfile profile, VegetationRole role)
        {
            if (_catalog != null)
            {
                BiomeDefinitionAsset asset = _catalog.GetBiome(GetBiomeId(profile.Kind));
                if (asset != null)
                {
                    return asset.GetMinScale(GetRoleId(role), GetFallbackMinScale(role));
                }
            }

            return role switch
            {
                VegetationRole.ConiferTree => profile.MinTreeScale,
                VegetationRole.LeafyTree => profile.MinTreeScale,
                VegetationRole.DryTree => profile.MinTreeScale,
                VegetationRole.Rock => 0.8f,
                VegetationRole.GreenBush => 0.45f,
                VegetationRole.DryBush => 0.45f,
                VegetationRole.GrassOrFlower => 0.35f,
                VegetationRole.BerryBush => 0.45f,
                _ => 0.5f
            };
        }

        private static float GetFallbackMinScale(VegetationRole role)
        {
            return role switch
            {
                VegetationRole.ConiferTree => 0.8f,
                VegetationRole.LeafyTree => 0.8f,
                VegetationRole.DryTree => 0.8f,
                VegetationRole.Rock => 0.8f,
                VegetationRole.GreenBush => 0.45f,
                VegetationRole.DryBush => 0.45f,
                VegetationRole.GrassOrFlower => 0.35f,
                VegetationRole.BerryBush => 0.45f,
                _ => 0.5f
            };
        }

        private static float GetMaxScaleForRole(BiomeIdentityProfile profile, VegetationRole role)
        {
            if (_catalog != null)
            {
                BiomeDefinitionAsset asset = _catalog.GetBiome(GetBiomeId(profile.Kind));
                if (asset != null)
                {
                    return asset.GetMaxScale(GetRoleId(role), GetFallbackMaxScale(role));
                }
            }

            return role switch
            {
                VegetationRole.ConiferTree => profile.MaxTreeScale,
                VegetationRole.LeafyTree => profile.MaxTreeScale,
                VegetationRole.DryTree => profile.MaxTreeScale,
                VegetationRole.Rock => 1.35f,
                VegetationRole.GreenBush => 0.9f,
                VegetationRole.DryBush => 0.85f,
                VegetationRole.GrassOrFlower => 0.7f,
                VegetationRole.BerryBush => 0.95f,
                _ => 1.0f
            };
        }

        private static float GetFallbackMaxScale(VegetationRole role)
        {
            return role switch
            {
                VegetationRole.ConiferTree => 1.2f,
                VegetationRole.LeafyTree => 1.2f,
                VegetationRole.DryTree => 1.2f,
                VegetationRole.Rock => 1.35f,
                VegetationRole.GreenBush => 0.9f,
                VegetationRole.DryBush => 0.85f,
                VegetationRole.GrassOrFlower => 0.7f,
                VegetationRole.BerryBush => 0.95f,
                _ => 1.0f
            };
        }

        internal static class VegetationPrefabSelectionRules
        {
            internal static IReadOnlyList<GameObject> FindPrefabsForRole(string roleName)
            {
                List<ScoredPrefab> found = new List<ScoredPrefab>();
                HashSet<GameObject> seen = new HashSet<GameObject>();

                foreach (string exactName in GetManualOverrideNamesForRole(roleName))
                {
                    string[] guids = AssetDatabase.FindAssets(exactName + " t:Prefab");
                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null || seen.Contains(prefab) || IsSnowVariant(path, prefab.name))
                        {
                            continue;
                        }

                        if (!IsEmbersStormPrefab(path, prefab.name))
                        {
                            continue;
                        }

                        if (TryGetManualVegetationRole(path, prefab.name, out string manualRole) && manualRole == roleName)
                        {
                            found.Add(new ScoredPrefab(prefab, int.MaxValue));
                            seen.Add(prefab);
                        }
                    }
                }

                if (found.Count == 0)
                {
                    foreach (string keyword in GetRoleKeywords(roleName))
                    {
                        string[] guids = AssetDatabase.FindAssets(keyword + " t:Prefab");
                        foreach (string guid in guids)
                        {
                            string path = AssetDatabase.GUIDToAssetPath(guid);
                            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                            if (prefab == null || seen.Contains(prefab) || IsSnowVariant(path, prefab.name))
                            {
                                continue;
                            }

                            if (!IsEmbersStormPrefab(path, prefab.name))
                            {
                                continue;
                            }

                            if (TryGetManualVegetationRole(path, prefab.name, out string manualRole))
                            {
                                if (manualRole != roleName)
                                {
                                    continue;
                                }

                                found.Add(new ScoredPrefab(prefab, int.MaxValue));
                                seen.Add(prefab);
                                continue;
                            }

                            int score = ScorePrefabForRole(path, prefab, roleName, keyword);
                            if (score <= 0)
                            {
                                continue;
                            }

                            seen.Add(prefab);
                            found.Add(new ScoredPrefab(prefab, score));
                        }
                    }
                }

                found.Sort((a, b) => b.Score.CompareTo(a.Score));
                List<GameObject> result = new List<GameObject>();
                foreach (ScoredPrefab item in found)
                {
                    result.Add(item.Prefab);
                }

                return result;
            }

            internal static void LogManualVegetationOverrides()
            {
                string[] knownPaths =
                {
                    "tree_04.4",
                    "tree_02.1",
                    "tree_04",
                    "stone_01",
                    "bush_02.1",
                    "bush_02.2"
                };

                foreach (string known in knownPaths)
                {
                    bool found = false;
                    string[] guids = AssetDatabase.FindAssets(known + " t:Prefab");

                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab == null || IsSnowVariant(path, prefab.name))
                        {
                            continue;
                        }

                        if (TryGetManualVegetationRole(path, prefab.name, out string roleName))
                        {
                            Debug.Log($"Manual vegetation override: {prefab.name} -> {roleName}");
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        Debug.LogWarning($"Manual vegetation override missing: {known}");
                    }
                }
            }

            internal static string[] GetManualOverrideNamesForRole(string roleName)
            {
                return roleName switch
                {
                    "DryTree" => new[] { "tree_04.4" },
                    "ConiferTree" => new[] { "tree_02.1" },
                    "LeafyTree" => new[] { "tree_04" },
                    "Rock" => new[] { "stone_01" },
                    "GreenBush" => new[] { "bush_02.1" },
                    "DryBush" => new[] { "bush_02.2" },
                    _ => Array.Empty<string>()
                };
            }

            internal static string[] GetRoleKeywords(string roleName)
            {
                return roleName switch
                {
                    "ConiferTree" => new[] { "pine", "conifer", "spruce", "fir" },
                    "LeafyTree" => new[] { "oak", "leaf", "broadleaf", "deciduous", "tree" },
                    "DryTree" => new[] { "dead", "dry", "bare" },
                    "Rock" => new[] { "rock", "stone", "boulder" },
                    "GreenBush" => new[] { "bush", "shrub", "plant" },
                    "DryBush" => new[] { "dry", "dead", "bush", "shrub" },
                    "GrassOrFlower" => new[] { "grass", "flower", "plant" },
                    "BerryBush" => new[] { "berry", "berries", "fruit", "bush" },
                    _ => Array.Empty<string>()
                };
            }

            internal static bool TryGetManualVegetationRole(string assetPath, string prefabName, out string roleName)
            {
                string normalizedPath = NormalizeAssetName(assetPath);
                string normalizedName = NormalizeAssetName(prefabName);

                if (IsSnowVariant(normalizedPath, normalizedName))
                {
                    roleName = string.Empty;
                    return false;
                }

                if (normalizedName == "tree_04_4" || normalizedPath.Contains("/tree_04_4"))
                {
                    roleName = "DryTree";
                    return true;
                }

                if (normalizedName == "tree_02_1" || normalizedPath.Contains("/tree_02_1"))
                {
                    roleName = "ConiferTree";
                    return true;
                }

                if (normalizedName == "tree_04" || normalizedPath.Contains("/tree_04"))
                {
                    roleName = "LeafyTree";
                    return true;
                }

                if (normalizedName == "stone_01" || normalizedPath.Contains("/stone_01"))
                {
                    roleName = "Rock";
                    return true;
                }

                if (normalizedName == "bush_02_2" || normalizedPath.Contains("/bush_02_2"))
                {
                    roleName = "DryBush";
                    return true;
                }

                if (normalizedName == "bush_02_1" || normalizedPath.Contains("/bush_02_1"))
                {
                    roleName = "GreenBush";
                    return true;
                }

                roleName = string.Empty;
                return false;
            }

            private static int ScorePrefabForRole(string path, GameObject prefab, string roleName, string keyword)
            {
                string text = (path + " " + prefab.name).ToLowerInvariant();
                if (!text.Contains("embersstorm")) return -1000;
                if (IsForbiddenForAllNature(text)) return -1000;
                if (text.Contains("low poly")) return -1000;
                int score = 0;
                if (text.Contains(keyword.ToLowerInvariant())) score += 15;
                if (text.Contains("nature")) score += 25;
                if (text.Contains("nature pack")) score += 35;
                score += ScoreRoleText(text, roleName);
                score += ScoreRoleColor(prefab, roleName);
                return score;
            }

            private static bool IsEmbersStormPrefab(string path, string prefabName)
            {
                string text = (path + " " + prefabName).ToLowerInvariant();
                return text.Contains("embersstorm -  free nature pack");
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

            private static int ScoreRoleText(string text, string roleName)
            {
                switch (roleName)
                {
                    case "ConiferTree":
                        if (text.Contains("pine") || text.Contains("conifer") || text.Contains("spruce") || text.Contains("fir")) return 60;
                        if (text.Contains("dead") || text.Contains("dry") || text.Contains("bare") || text.Contains("rock") || text.Contains("bush")) return -80;
                        if (text.Contains("snow") || text.Contains("winter")) return -40;
                        return text.Contains("tree") ? 10 : -30;
                    case "LeafyTree":
                        if (text.Contains("oak") || text.Contains("leaf") || text.Contains("broadleaf") || text.Contains("deciduous")) return 60;
                        if (text.Contains("pine") || text.Contains("conifer") || text.Contains("spruce") || text.Contains("fir")) return -90;
                        if (text.Contains("dead") || text.Contains("dry") || text.Contains("bare") || text.Contains("rock")) return -90;
                        if (text.Contains("snow") || text.Contains("winter")) return -50;
                        return text.Contains("tree") ? 12 : -30;
                    case "DryTree":
                        if (text.Contains("dead") || text.Contains("dry") || text.Contains("bare")) return 80;
                        if (text.Contains("pine") || text.Contains("conifer") || text.Contains("spruce") || text.Contains("green") || text.Contains("leaf")) return -80;
                        return text.Contains("tree") ? 10 : -30;
                    case "Rock":
                        if (text.Contains("rock") || text.Contains("stone") || text.Contains("boulder")) return 80;
                        if (text.Contains("tree") || text.Contains("bush") || text.Contains("grass") || text.Contains("flower")) return -100;
                        return -40;
                    case "GreenBush":
                        if (text.Contains("bush") || text.Contains("shrub") || text.Contains("plant")) return 50;
                        if (text.Contains("dead") || text.Contains("dry") || text.Contains("rock") || text.Contains("tree")) return -60;
                        return -20;
                    case "DryBush":
                        if ((text.Contains("dry") || text.Contains("dead")) && (text.Contains("bush") || text.Contains("shrub") || text.Contains("plant"))) return 80;
                        if (text.Contains("bush") || text.Contains("shrub")) return 15;
                        if (text.Contains("green") || text.Contains("flower") || text.Contains("tree")) return -60;
                        return -20;
                    case "GrassOrFlower":
                        if (text.Contains("grass") || text.Contains("flower") || text.Contains("flover")) return 70;
                        if (text.Contains("tree") || text.Contains("rock")) return -80;
                        return text.Contains("plant") ? 15 : -30;
                    case "BerryBush":
                        if (text.Contains("berry") || text.Contains("berries") || text.Contains("fruit")) return 80;
                        if (text.Contains("bush")) return 15;
                        if (text.Contains("tree") || text.Contains("rock") || text.Contains("dead") || text.Contains("dry")) return -80;
                        return -30;
                    default:
                        return 0;
                }
            }

            private static int ScoreRoleColor(GameObject prefab, string roleName)
            {
                Color color = EstimatePrefabColor(prefab);
                if (color == Color.clear) return 0;
                bool greenDominant = color.g > color.r * 1.15f && color.g > color.b * 1.15f;
                bool grayDominant = Mathf.Abs(color.r - color.g) < 0.08f && Mathf.Abs(color.g - color.b) < 0.08f;
                bool warmDry = color.r > color.g * 1.15f && color.g >= color.b;
                return roleName switch
                {
                    "ConiferTree" or "LeafyTree" or "GreenBush" or "GrassOrFlower" or "BerryBush" => greenDominant ? 15 : 0,
                    "Rock" => grayDominant ? 20 : 0,
                    "DryTree" or "DryBush" => warmDry ? 20 : 0,
                    _ => 0
                };
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
            if (IsSnowyPineVariant(normalizedPath, normalizedName))
            {
                return true;
            }
            return normalizedPath.Contains("_snow")
                || normalizedName.Contains("_snow")
                || normalizedPath.Contains("snow")
                || normalizedName.Contains("snow")
                || normalizedPath.Contains("winter")
                || normalizedName.Contains("winter");
        }

        private static bool IsSnowyPineVariant(string normalizedPath, string normalizedName)
        {
            if (!normalizedPath.Contains("/prefabs/trees/pine tree/") && !normalizedName.Contains("pine tree"))
            {
                return false;
            }

            return normalizedName.Contains("pine tree .024")
                || normalizedName.Contains("pine tree .025")
                || normalizedName.Contains("pine tree .026")
                || normalizedName.Contains("pine tree .027")
                || normalizedName.Contains("pine tree .028")
                || normalizedName.Contains("pine tree .029")
                || normalizedName.Contains("pine tree .030")
                || normalizedName.Contains("pine tree .031")
                || normalizedName.Contains("pine tree .032")
                || normalizedName.Contains("pine tree .033")
                || normalizedName.Contains("pine tree .034")
                || normalizedName.Contains("pine tree .035")
                || normalizedName.Contains("pine tree .036")
                || normalizedName.Contains("pine tree .037")
                || normalizedName.Contains("pine tree .038")
                || normalizedName.Contains("pine tree .039")
                || normalizedName.Contains("pine tree .040");
        }

            private static string NormalizeAssetName(string value)
            {
                if (string.IsNullOrEmpty(value)) return string.Empty;
                string normalized = value.Replace('\\', '/').ToLowerInvariant();
                normalized = normalized.Replace('.', '_');
                return normalized;
            }

            private readonly struct ScoredPrefab
            {
                public ScoredPrefab(GameObject prefab, int score)
                {
                    Prefab = prefab;
                    Score = score;
                }

                public GameObject Prefab { get; }
                public int Score { get; }
            }
        }

        private static void CreateBiomeMarker(Transform parent, string name, Vector3 position)
        {
            GameObject marker = CreateChild(parent, name + "_Marker");
            marker.transform.position = position;
        }

        private static BiomeMaterials CreateBiomeMaterials()
        {
            return new BiomeMaterials
            {
                Westwood = LoadOrCreateBiomeMaterial(GetProfile(BiomeKind.Westwood)),
                SouthThicket = LoadOrCreateBiomeMaterial(GetProfile(BiomeKind.SouthThicket)),
                HearthMeadow = LoadOrCreateBiomeMaterial(GetProfile(BiomeKind.HearthMeadow)),
                StonebackRidge = LoadOrCreateBiomeMaterial(GetProfile(BiomeKind.StonebackRidge)),
                RedfangWilds = LoadOrCreateBiomeMaterial(GetProfile(BiomeKind.RedfangWilds))
            };
        }

        private static Dictionary<BiomeKind, List<Vector3>> CreateIslandTerrain(Transform terrainRoot, Transform biomeRoot, BiomeMaterials materials)
        {
            Dictionary<BiomeKind, List<Vector3>> tilesByBiome = new Dictionary<BiomeKind, List<Vector3>>
            {
                [BiomeKind.Westwood] = new List<Vector3>(),
                [BiomeKind.SouthThicket] = new List<Vector3>(),
                [BiomeKind.HearthMeadow] = new List<Vector3>(),
                [BiomeKind.StonebackRidge] = new List<Vector3>(),
                [BiomeKind.RedfangWilds] = new List<Vector3>()
            };

            foreach (BiomeKind biome in tilesByBiome.Keys)
            {
                CreateChild(biomeRoot, biome.ToString());
            }

            int xSteps = Mathf.CeilToInt(IslandRadiusX / TileSize);
            int zSteps = Mathf.CeilToInt(IslandRadiusZ / TileSize);

            for (int xi = -xSteps; xi <= xSteps; xi++)
            {
                for (int zi = -zSteps; zi <= zSteps; zi++)
                {
                    float x = xi * TileSize;
                    float z = zi * TileSize;

                    float centerBias = Mathf.Clamp01(1f - (Mathf.Abs(x) / IslandRadiusX + Mathf.Abs(z) / IslandRadiusZ) * 0.5f);
                    if (centerBias < 0.18f && Random.NextDouble() < 0.45f)
                    {
                        continue;
                    }

                    if (!IsInsideIsland(x, z))
                    {
                        continue;
                    }

                    Vector3 tilePosition = new Vector3(x, -0.05f, z);
                    BiomeKind biome = DetermineBiome(new Vector3(x, 0f, z));

                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = biome + "_GroundTile_" + xi.ToString("00") + "_" + zi.ToString("00");
                    tile.transform.SetParent(terrainRoot, false);
                    tile.transform.position = tilePosition;
                    tile.transform.localScale = new Vector3(TileSize, 0.1f, TileSize);
                    ApplyMaterial(tile, materials.Get(biome));

                    tilesByBiome[biome].Add(new Vector3(x, 0f, z));
                }
            }

            return tilesByBiome;
        }

        private static List<Vector3> FlattenTileCenters(Dictionary<BiomeKind, List<Vector3>> tilesByBiome)
        {
            List<Vector3> result = new List<Vector3>();
            foreach (List<Vector3> tiles in tilesByBiome.Values)
            {
                result.AddRange(tiles);
            }

            return result;
        }

        private static List<Vector3> FindEdgeTiles(IReadOnlyList<Vector3> allTiles)
        {
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
            Dictionary<Vector2Int, Vector3> byGrid = new Dictionary<Vector2Int, Vector3>();

            foreach (Vector3 tile in allTiles)
            {
                Vector2Int grid = ToGrid(tile);
                occupied.Add(grid);
                byGrid[grid] = tile;
            }

            List<Vector3> edges = new List<Vector3>();
            foreach (Vector2Int grid in occupied)
            {
                bool isEdge =
                    !occupied.Contains(grid + Vector2Int.up) ||
                    !occupied.Contains(grid + Vector2Int.down) ||
                    !occupied.Contains(grid + Vector2Int.left) ||
                    !occupied.Contains(grid + Vector2Int.right);

                if (isEdge && byGrid.TryGetValue(grid, out Vector3 tile))
                {
                    edges.Add(tile);
                }
            }

            return edges;
        }

        private static Vector2Int ToGrid(Vector3 position)
        {
            return new Vector2Int(
                Mathf.RoundToInt(position.x / TileSize),
                Mathf.RoundToInt(position.z / TileSize));
        }

        private static void CreateVisualBoundary(Transform worldRoot, IReadOnlyList<Vector3> allTiles, IReadOnlyList<GameObject> rockPrefabs)
        {
            GameObject boundaryRoot = CreateChild(worldRoot, "BoundaryRoot");
            Material boundaryMaterial = LoadOrCreateMaterial("World_Boundary_Edge", new Color(0.12f, 0.12f, 0.10f));
            List<Vector3> edgeTiles = FindEdgeTiles(allTiles);

            for (int i = 0; i < edgeTiles.Count; i++)
            {
                Vector3 position = edgeTiles[i];
                GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                edge.name = "WorldBoundary_" + i.ToString("000");
                edge.transform.SetParent(boundaryRoot.transform, false);
                edge.transform.position = new Vector3(position.x, -0.08f, position.z);
                edge.transform.localScale = new Vector3(TileSize * 1.02f, 0.08f, TileSize * 1.02f);

                MeshRenderer renderer = edge.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = boundaryMaterial;
                }
            }

            SpawnBoundaryRocks(boundaryRoot.transform, edgeTiles, rockPrefabs);
        }

        private static void SpawnBoundaryRocks(Transform boundaryRoot, IReadOnlyList<Vector3> edgeTiles, IReadOnlyList<GameObject> rockPrefabs)
        {
            if (edgeTiles == null || edgeTiles.Count == 0)
            {
                return;
            }

            int count = Mathf.Min(edgeTiles.Count / 3, 40);
            for (int i = 0; i < count; i++)
            {
                Vector3 position = edgeTiles[Random.Next(edgeTiles.Count)];
                GameObject prefab = Pick(rockPrefabs);
                if (prefab == null)
                {
                    continue;
                }

                GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                rock.name = "BoundaryRock_" + i.ToString("00");
                rock.transform.SetParent(boundaryRoot, false);
                rock.transform.position = position + new Vector3(
                    RandomRange(-TileSize * 0.3f, TileSize * 0.3f),
                    0f,
                    RandomRange(-TileSize * 0.3f, TileSize * 0.3f));
                rock.transform.rotation = Quaternion.Euler(0f, RandomRange(0f, 360f), 0f);
                rock.transform.localScale = Vector3.one * RandomRange(0.7f, 1.3f);
            }
        }

        private static void CreateOutsideVoid(Transform terrainRoot)
        {
            Material material = LoadOrCreateMaterial("World_Outside_Void", new Color(0.18f, 0.18f, 0.17f));

            GameObject outside = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outside.name = "OutsideVoid";
            outside.transform.SetParent(terrainRoot, false);
            outside.transform.position = new Vector3(0f, -0.14f, 0f);
            outside.transform.localScale = new Vector3(130f, 0.05f, 110f);

            MeshRenderer renderer = outside.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = outside.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Material LoadOrCreateBiomeMaterial(BiomeIdentityProfile profile)
        {
            Material packMaterial = FindBestMaterial(new[] { "ground", "dirt", "grass", "rock" });
            string materialPath = MaterialFolder + "/Ground_" + profile.Kind + ".mat";

            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader)
                {
                    name = "Ground_" + profile.Kind
                };

                AssetDatabase.CreateAsset(material, materialPath);
            }

            ApplyBiomeMaterialProperties(material, profile, packMaterial);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void ApplyBiomeMaterialProperties(Material target, BiomeIdentityProfile profile, Material source)
        {
            Color color = profile.GroundColor;
            Texture texture = source != null ? TryReadTexture(source) : null;

            if (target.HasProperty("_BaseColor"))
            {
                target.SetColor("_BaseColor", color);
            }
            else if (target.HasProperty("_Color"))
            {
                target.SetColor("_Color", color);
            }
            else
            {
                target.color = color;
            }

            if (texture != null)
            {
                SetFirstTexture(target, texture, "_BaseMap", "_BaseColorMap", "_MainTex", "_Albedo");
            }
        }

        private static Material FindBestMaterial(string[] keywords)
        {
            List<ScoredMaterial> candidates = new List<ScoredMaterial>();
            foreach (string keyword in keywords)
            {
                string[] guids = AssetDatabase.FindAssets(keyword + " t:Material");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        continue;
                    }

                    int score = ScorePackAsset(path, material.name, keyword);
                    if (score > 0)
                    {
                        candidates.Add(new ScoredMaterial(material, score));
                    }
                }
            }

            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            return candidates.Count > 0 ? candidates[0].Material : null;
        }

        private static int ScorePackAsset(string path, string name, string keyword)
        {
            string text = (path + " " + name).ToLowerInvariant();
            int score = 0;

            if (text.Contains(keyword.ToLowerInvariant()))
            {
                score += 10;
            }

            if (text.Contains("low poly"))
            {
                score += 25;
            }

            if (text.Contains("nature"))
            {
                score += 20;
            }

            if (text.Contains("tree"))
            {
                score += 10;
            }

            if (text.Contains("ground"))
            {
                score += 10;
            }

            if (text.Contains("grass"))
            {
                score += 10;
            }

            if (text.Contains("rock"))
            {
                score += 10;
            }

            if (text.Contains("dirt"))
            {
                score += 10;
            }

            if (text.Contains("snow") || text.Contains("winter"))
            {
                return 0;
            }

            if (text.Contains("stylizedwoodmonsters") || text.Contains("animationgallery"))
            {
                score -= 40;
            }

            return score;
        }

        private static Color TryReadColor(Material material, Color fallback)
        {
            if (material == null)
            {
                return fallback;
            }

            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return fallback;
        }

        private static Texture TryReadTexture(Material material)
        {
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty("_BaseMap"))
            {
                Texture texture = material.GetTexture("_BaseMap");
                if (texture != null) return texture;
            }

            if (material.HasProperty("_BaseColorMap"))
            {
                Texture texture = material.GetTexture("_BaseColorMap");
                if (texture != null) return texture;
            }

            if (material.HasProperty("_MainTex"))
            {
                Texture texture = material.GetTexture("_MainTex");
                if (texture != null) return texture;
            }

            if (material.HasProperty("_Albedo"))
            {
                Texture texture = material.GetTexture("_Albedo");
                if (texture != null) return texture;
            }

            return null;
        }

        private static void SetFirstTexture(Material target, Texture texture, params string[] propertyNames)
        {
            if (target == null || texture == null || propertyNames == null)
            {
                return;
            }

            foreach (string property in propertyNames)
            {
                if (string.IsNullOrWhiteSpace(property) || !target.HasProperty(property))
                {
                    continue;
                }

                target.SetTexture(property, texture);
                return;
            }
        }

        private static void SpawnBiomeVegetation(Transform vegetationRoot, Dictionary<BiomeKind, List<Vector3>> tilesByBiome)
        {
            VegetationCatalog catalog = BuildVegetationCatalog();
            LogVegetationCatalog(catalog);
            SpawnBiomeVegetation(vegetationRoot, tilesByBiome, catalog);
        }

        private static void SpawnBiomeVegetation(
            Transform vegetationRoot,
            Dictionary<BiomeKind, List<Vector3>> tilesByBiome,
            VegetationCatalog catalog)
        {
            SpawnBiomeVegetationForProfile(vegetationRoot, tilesByBiome, catalog, GetProfile(BiomeKind.Westwood));
            SpawnBiomeVegetationForProfile(vegetationRoot, tilesByBiome, catalog, GetProfile(BiomeKind.SouthThicket));
            SpawnBiomeVegetationForProfile(vegetationRoot, tilesByBiome, catalog, GetProfile(BiomeKind.HearthMeadow));
            SpawnBiomeVegetationForProfile(vegetationRoot, tilesByBiome, catalog, GetProfile(BiomeKind.StonebackRidge));
            SpawnBiomeVegetationForProfile(vegetationRoot, tilesByBiome, catalog, GetProfile(BiomeKind.RedfangWilds));
        }

        private static void SpawnBiomeVegetationForProfile(
            Transform vegetationRoot,
            Dictionary<BiomeKind, List<Vector3>> tilesByBiome,
            VegetationCatalog catalog,
            BiomeIdentityProfile profile)
        {
            if (!tilesByBiome.TryGetValue(profile.Kind, out List<Vector3> tiles) || tiles.Count == 0)
            {
                return;
            }

            Transform parent = CreateChild(vegetationRoot, profile.DisplayName.Replace(" ", string.Empty) + "Vegetation").transform;

            float avoidCenterRadius = profile.Kind == BiomeKind.HearthMeadow ? 14f : 5f;

            SpawnRole(parent, profile, tiles, catalog, VegetationRole.ConiferTree, profile.ConiferTreeCount, true, avoidCenterRadius);
            SpawnRole(parent, profile, tiles, catalog, VegetationRole.LeafyTree, profile.LeafyTreeCount, true, avoidCenterRadius);
            SpawnRole(parent, profile, tiles, catalog, VegetationRole.DryTree, profile.DryTreeCount, true, avoidCenterRadius);
            SpawnRole(parent, profile, tiles, catalog, VegetationRole.Rock, profile.RockCount, false, avoidCenterRadius);
            SpawnRole(parent, profile, tiles, catalog, VegetationRole.GreenBush, profile.GreenBushCount, false, avoidCenterRadius);
            SpawnRole(parent, profile, tiles, catalog, VegetationRole.DryBush, profile.DryBushCount, false, avoidCenterRadius);
            SpawnRole(parent, profile, tiles, catalog, VegetationRole.GrassOrFlower, profile.GrassOrFlowerCount, false, avoidCenterRadius);
            SpawnRole(parent, profile, tiles, catalog, VegetationRole.BerryBush, profile.BerryBushCount, false, avoidCenterRadius);
        }

        private static void SpawnRole(
            Transform parent,
            BiomeIdentityProfile profile,
            IReadOnlyList<Vector3> tiles,
            VegetationCatalog catalog,
            VegetationRole role,
            int count,
            bool larger,
            float avoidCenterRadius)
        {
            if (count <= 0)
            {
                return;
            }

            SpawnObjectsFromTiles(
                parent,
                profile.Kind + "_" + role,
                tiles,
                catalog.Get(role),
                count,
                larger,
                avoidCenterRadius,
                GetMinScaleForRole(profile, role),
                GetMaxScaleForRole(profile, role),
                profile.Kind,
                role);
        }

        private static void SpawnObjectsFromTiles(
            Transform parent,
            string prefix,
            IReadOnlyList<Vector3> tilePositions,
            IReadOnlyList<GameObject> prefabs,
            int count,
            bool larger,
            float avoidCenterRadius,
            float minScale,
            float maxScale,
            BiomeKind biome,
            VegetationRole role)
        {
            if (tilePositions == null || tilePositions.Count == 0)
            {
                return;
            }

            int spawned = 0;
            int attempts = count * 8;
            for (int attempt = 0; attempt < attempts && spawned < count; attempt++)
            {
                Vector3 basePosition = tilePositions[Random.Next(tilePositions.Count)];
                if (new Vector2(basePosition.x, basePosition.z).magnitude < avoidCenterRadius)
                {
                    continue;
                }

                Vector3 position = basePosition + new Vector3(
                    RandomRange(-TileSize * 0.35f, TileSize * 0.35f),
                    0f,
                    RandomRange(-TileSize * 0.35f, TileSize * 0.35f));

                GameObject prefab = Pick(prefabs);
                GameObject instance;
                if (prefab != null)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    instance.name = prefix + "_" + spawned.ToString("00");
                }
                else
                {
                    instance = CreateFallbackObject(prefix + "_" + spawned.ToString("00"), biome, role, larger);
                }

                instance.transform.SetParent(parent, false);
                instance.transform.position = position;
                instance.transform.rotation = Quaternion.Euler(0f, RandomRange(0f, 360f), 0f);
                instance.transform.localScale = Vector3.one * RandomRange(minScale, maxScale);
                BindAsResource(instance, role);
                spawned++;
            }
        }

        private static void BindAsResource(GameObject instance, VegetationRole role)
        {
            string resourceKind = role switch
            {
                VegetationRole.ConiferTree => "conifer_tree",
                VegetationRole.LeafyTree => "leafy_tree",
                VegetationRole.DryTree => "dry_tree",
                VegetationRole.Rock => "rock",
                VegetationRole.GreenBush => "bush",
                VegetationRole.DryBush => "dry_bush",
                _ => null
            };

            if (resourceKind == null) return;

            float radius = role switch
            {
                VegetationRole.ConiferTree => 2.4f,
                VegetationRole.LeafyTree => 2.4f,
                VegetationRole.DryTree => 2.2f,
                VegetationRole.Rock => 1.8f,
                VegetationRole.GreenBush => 1.4f,
                VegetationRole.DryBush => 1.4f,
                _ => 1.5f
            };

            ResourceNodeView view = instance.GetComponent<ResourceNodeView>();
            if (view == null)
            {
                view = instance.AddComponent<ResourceNodeView>();
            }

            view.ConfigureDefault(resourceKind);

            SerializedObject so = new SerializedObject(view);
            so.FindProperty("interactionRadius").floatValue = radius;
            so.ApplyModifiedProperties();

            SphereCollider trigger = instance.GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = instance.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.radius = radius;

            if (role == VegetationRole.ConiferTree || role == VegetationRole.LeafyTree || role == VegetationRole.DryTree)
            {
                GameObject stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stump.name = "Stump";
                stump.transform.SetParent(instance.transform, false);
                stump.transform.localPosition = new Vector3(0, 0.25f, 0);
                stump.transform.localScale = new Vector3(0.4f, 0.25f, 0.4f);
                stump.SetActive(false);

                // Use a darker brown and unique material name to prevent color sharing in the material repair utility
                ApplyTint(stump, new Color(0.25f, 0.15f, 0.08f), instance.name + "_Stump_Material"); 

                // Remove the collider from the stump visual to prevent physics issues
                Object.DestroyImmediate(stump.GetComponent<Collider>());

                so.FindProperty("depletedVisual").objectReferenceValue = stump;
                so.ApplyModifiedProperties();
            }
        }

        private static bool IsInsideIsland(float x, float z)
        {
            float normalizedX = x / IslandRadiusX;
            float normalizedZ = z / IslandRadiusZ;
            float distance = normalizedX * normalizedX + normalizedZ * normalizedZ;
            float edgeNoiseA = Mathf.PerlinNoise((x + 100f) * 0.08f, (z + 100f) * 0.08f);
            float edgeNoiseB = Mathf.PerlinNoise((x + 250f) * 0.15f, (z + 250f) * 0.11f);
            float edgeNoiseC = Mathf.PerlinNoise((x + 510f) * 0.21f, (z + 510f) * 0.18f);
            float radiusModifier = Mathf.Lerp(0.80f, 1.14f, edgeNoiseA);
            radiusModifier += (edgeNoiseB - 0.5f) * 0.11f;
            radiusModifier += (edgeNoiseC - 0.5f) * 0.05f;
            return distance <= radiusModifier;
        }

        private static BiomeKind DetermineBiome(Vector3 position)
        {
            float borderNoise = Mathf.PerlinNoise((position.x + 200f) * 0.055f, (position.z + 200f) * 0.055f) - 0.5f;
            float x = position.x + borderNoise * 12f;
            float z = position.z + borderNoise * 10f;

            float centerDistance = Mathf.Sqrt(x * x + z * z);
            if (centerDistance < 10f)
            {
                return BiomeKind.HearthMeadow;
            }

            if (x > 24f || (x > 10f && z < -6f))
            {
                return BiomeKind.RedfangWilds;
            }

            if (z > 20f)
            {
                return BiomeKind.StonebackRidge;
            }

            if (x < -16f)
            {
                return BiomeKind.Westwood;
            }

            if (z < -6f || x < 6f)
            {
                return BiomeKind.SouthThicket;
            }

            return BiomeKind.RedfangWilds;
        }

        private static void CreateCornerBlend(
            Transform terrainRoot,
            Transform transitionRoot,
            string name,
            Vector3 center,
            Vector2 size,
            Material material,
            IReadOnlyList<GameObject> primaryPrefabs,
            IReadOnlyList<GameObject> detailPrefabs)
        {
            GameObject blendMarker = CreateChild(transitionRoot, name);
            blendMarker.transform.position = center;

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = name + "_Ground";
            ground.transform.SetParent(terrainRoot, false);
            ground.transform.position = center + new Vector3(0f, -0.047f, 0f);
            ground.transform.localScale = new Vector3(size.x, 0.094f, size.y);
            ApplyMaterial(ground, material);

            GameObject vegetationParent = CreateChild(transitionRoot, name + "_Vegetation");
            SpawnMixedObjects(vegetationParent.transform, name + "_Primary", center, size, primaryPrefabs, detailPrefabs, 6, true);
            SpawnObjects(vegetationParent.transform, name + "_Details", center, size, detailPrefabs, 4, false);
        }

        private static GameObject CreateFallbackObject(string name, bool larger)
        {
            if (larger)
            {
                GameObject root = new GameObject(name);

                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(root.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                trunk.transform.localScale = new Vector3(0.25f, 0.75f, 0.25f);

                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Crown";
                crown.transform.SetParent(root.transform, false);
                crown.transform.localPosition = new Vector3(0f, 1.8f, 0f);
                crown.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

                return root;
            }

            GameObject detail = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            detail.name = name;
            detail.transform.localScale = new Vector3(0.8f, 0.35f, 0.8f);
            return detail;
        }

        private static GameObject CreateFallbackObject(string name, BiomeKind biome, VegetationRole role, bool larger)
        {
            if ((biome == BiomeKind.StonebackRidge || role == VegetationRole.Rock) && !larger)
            {
                larger = true;
            }

            if (larger)
            {
                GameObject root = new GameObject(name);
                Color fallbackColor = GetFallbackColor(biome, role);

                if (role == VegetationRole.Rock || biome == BiomeKind.StonebackRidge)
                {
                    GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    rock.name = "Rock";
                    rock.transform.SetParent(root.transform, false);
                    rock.transform.localPosition = Vector3.zero;
                    rock.transform.localScale = Vector3.one * 1.1f;
                    ApplyTint(rock, fallbackColor);
                    return root;
                }

                GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trunk.name = "Trunk";
                trunk.transform.SetParent(root.transform, false);
                trunk.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                trunk.transform.localScale = new Vector3(0.25f, 0.75f, 0.25f);
                ApplyTint(trunk, fallbackColor * 0.7f);

                GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                crown.name = "Crown";
                crown.transform.SetParent(root.transform, false);
                crown.transform.localPosition = new Vector3(0f, 1.7f, 0f);
                crown.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                ApplyTint(crown, fallbackColor);

                if (biome == BiomeKind.RedfangWilds)
                {
                    crown.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
                    trunk.transform.localScale = new Vector3(0.22f, 0.85f, 0.22f);
                }

                return root;
            }

            GameObject detail = GameObject.CreatePrimitive(biome == BiomeKind.StonebackRidge ? PrimitiveType.Sphere : PrimitiveType.Sphere);
            detail.name = name;
            detail.transform.localScale = biome == BiomeKind.StonebackRidge
                ? new Vector3(1.0f, 0.8f, 1.0f)
                : new Vector3(0.8f, 0.35f, 0.8f);
            ApplyTint(detail, GetFallbackColor(biome, role));
            return detail;
        }

        private static Color GetFallbackColor(BiomeKind biome, VegetationRole role)
        {
            return role switch
            {
                VegetationRole.ConiferTree => new Color(0.06f, 0.24f, 0.10f),
                VegetationRole.LeafyTree => new Color(0.16f, 0.46f, 0.16f),
                VegetationRole.DryTree => new Color(0.52f, 0.34f, 0.16f),
                VegetationRole.Rock => new Color(0.45f, 0.45f, 0.42f),
                VegetationRole.GreenBush => new Color(0.18f, 0.50f, 0.18f),
                VegetationRole.DryBush => new Color(0.48f, 0.33f, 0.14f),
                VegetationRole.GrassOrFlower => new Color(0.38f, 0.68f, 0.24f),
                VegetationRole.BerryBush => new Color(0.20f, 0.50f, 0.20f),
                _ => biome switch
                {
                    BiomeKind.Westwood => new Color(0.05f, 0.26f, 0.10f),
                    BiomeKind.SouthThicket => new Color(0.18f, 0.45f, 0.14f),
                    BiomeKind.HearthMeadow => new Color(0.35f, 0.65f, 0.22f),
                    BiomeKind.StonebackRidge => new Color(0.45f, 0.45f, 0.42f),
                    BiomeKind.RedfangWilds => new Color(0.45f, 0.28f, 0.12f),
                    _ => Color.green
                }
            };
        }

        private static void ApplyTint(GameObject target, Color color, string materialName = null)
        {
            if (target == null)
            {
                return;
            }

            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material sourceMaterial = renderer.sharedMaterial;
            Material material = sourceMaterial != null ? new Material(sourceMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (material == null || material.shader == null)
            {
                material = new Material(Shader.Find("Standard"));
            }

            if (string.IsNullOrEmpty(materialName))
            {
                material.name = target.name + "_Tinted_Material";
            }
            else
            {
                material.name = materialName;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            else
            {
                material.color = color;
            }

            renderer.sharedMaterial = material;
        }

        private static float GetFirstScale(BiomeDefinitionAsset asset, bool min, float fallback)
        {
            if (asset == null)
            {
                return fallback;
            }

            string[] treeRoles = { "conifer_tree", "leafy_tree", "dry_tree" };
            foreach (string role in treeRoles)
            {
                if (asset.GetVegetationCount(role) > 0)
                {
                    return min ? asset.GetMinScale(role, fallback) : asset.GetMaxScale(role, fallback);
                }
            }

            return fallback;
        }

        private static string GetBiomeId(BiomeKind biome)
        {
            switch (biome)
            {
                case BiomeKind.Westwood:
                    return "westwood";
                case BiomeKind.SouthThicket:
                    return "south_thicket";
                case BiomeKind.HearthMeadow:
                    return "hearth_meadow";
                case BiomeKind.StonebackRidge:
                    return "stoneback_ridge";
                case BiomeKind.RedfangWilds:
                    return "redfang_wilds";
                default:
                    return biome.ToString().ToLowerInvariant();
            }
        }

        private static BiomeIdentityProfile GetProfile(BiomeKind biome)
        {
            if (_catalog != null)
            {
                BiomeDefinitionAsset asset = _catalog.GetBiome(GetBiomeId(biome));
                if (asset != null)
                {
                    return new BiomeIdentityProfile
                    {
                        Kind = biome,
                        DisplayName = asset.DisplayName,
                        GroundColor = asset.GroundColor,
                        ConiferTreeCount = asset.GetVegetationCount(GetRoleId(VegetationRole.ConiferTree)),
                        LeafyTreeCount = asset.GetVegetationCount(GetRoleId(VegetationRole.LeafyTree)),
                        DryTreeCount = asset.GetVegetationCount(GetRoleId(VegetationRole.DryTree)),
                        RockCount = asset.GetVegetationCount(GetRoleId(VegetationRole.Rock)),
                        GreenBushCount = asset.GetVegetationCount(GetRoleId(VegetationRole.GreenBush)),
                        DryBushCount = asset.GetVegetationCount(GetRoleId(VegetationRole.DryBush)),
                        GrassOrFlowerCount = asset.GetVegetationCount(GetRoleId(VegetationRole.GrassOrFlower)),
                        BerryBushCount = asset.GetVegetationCount(GetRoleId(VegetationRole.BerryBush)),
                        MinTreeScale = GetFirstScale(asset, true, 0.8f),
                        MaxTreeScale = GetFirstScale(asset, false, 1.2f)
                    };
                }
            }

            return biome switch
            {
                BiomeKind.Westwood => new BiomeIdentityProfile
                {
                    Kind = BiomeKind.Westwood,
                    DisplayName = "Westwood",
                    GroundColor = new Color(0.055f, 0.18f, 0.075f),
                    ConiferTreeCount = 95,
                    LeafyTreeCount = 2,
                    DryTreeCount = 0,
                    RockCount = 6,
                    GreenBushCount = 28,
                    DryBushCount = 0,
                    GrassOrFlowerCount = 18,
                    BerryBushCount = 12,
                    MinTreeScale = 0.9f,
                    MaxTreeScale = 1.35f
                },
                BiomeKind.SouthThicket => new BiomeIdentityProfile
                {
                    Kind = BiomeKind.SouthThicket,
                    DisplayName = "South Thicket",
                    GroundColor = new Color(0.20f, 0.43f, 0.13f),
                    ConiferTreeCount = 0,
                    LeafyTreeCount = 55,
                    DryTreeCount = 0,
                    RockCount = 5,
                    GreenBushCount = 45,
                    DryBushCount = 2,
                    GrassOrFlowerCount = 38,
                    BerryBushCount = 8,
                    MinTreeScale = 0.75f,
                    MaxTreeScale = 1.2f
                },
                BiomeKind.HearthMeadow => new BiomeIdentityProfile
                {
                    Kind = BiomeKind.HearthMeadow,
                    DisplayName = "Hearth Meadow",
                    GroundColor = new Color(0.50f, 0.68f, 0.36f),
                    ConiferTreeCount = 0,
                    LeafyTreeCount = 8,
                    DryTreeCount = 0,
                    RockCount = 3,
                    GreenBushCount = 10,
                    DryBushCount = 0,
                    GrassOrFlowerCount = 32,
                    BerryBushCount = 6,
                    MinTreeScale = 0.7f,
                    MaxTreeScale = 1.1f
                },
                BiomeKind.StonebackRidge => new BiomeIdentityProfile
                {
                    Kind = BiomeKind.StonebackRidge,
                    DisplayName = "Stoneback Ridge",
                    GroundColor = new Color(0.30f, 0.32f, 0.30f),
                    ConiferTreeCount = 10,
                    LeafyTreeCount = 0,
                    DryTreeCount = 4,
                    RockCount = 85,
                    GreenBushCount = 3,
                    DryBushCount = 14,
                    GrassOrFlowerCount = 4,
                    BerryBushCount = 0,
                    MinTreeScale = 0.75f,
                    MaxTreeScale = 1.25f
                },
                BiomeKind.RedfangWilds => new BiomeIdentityProfile
                {
                    Kind = BiomeKind.RedfangWilds,
                    DisplayName = "Redfang Wilds",
                    GroundColor = new Color(0.55f, 0.36f, 0.16f),
                    ConiferTreeCount = 0,
                    LeafyTreeCount = 0,
                    DryTreeCount = 45,
                    RockCount = 24,
                    GreenBushCount = 2,
                    DryBushCount = 48,
                    GrassOrFlowerCount = 3,
                    BerryBushCount = 0,
                    MinTreeScale = 0.75f,
                    MaxTreeScale = 1.25f
                },
                _ => throw new ArgumentOutOfRangeException(nameof(biome), biome, null)
            };
        }

        private static Vector3 RandomPoint(Vector3 center, Vector2 size, float safeCenterRadius)
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                float x = RandomRange(-size.x * 0.45f, size.x * 0.45f);
                float z = RandomRange(-size.y * 0.45f, size.y * 0.45f);
                Vector3 point = center + new Vector3(x, 0f, z);

                if (new Vector2(point.x - center.x, point.z - center.z).magnitude > safeCenterRadius)
                {
                    return point;
                }
            }

            return center + new Vector3(RandomRange(3f, size.x * 0.4f), 0f, RandomRange(3f, size.y * 0.4f));
        }

        private static GameObject CreatePlayer(Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject player;

            if (prefab != null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                player.name = "Player";
            }
            else
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
            }

            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(0f, 0.05f, 0f);
            player.transform.localScale = Vector3.one * 0.85f;
            player.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            RemoveDemoViewerComponents(player);

            return player;
        }

        private static void CreateLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Directional Light");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
        }

        private static void CreateBootstrapper(Transform parent)
        {
            GameObject bootstrapper = new GameObject("GameBootstrapper");
            bootstrapper.transform.SetParent(parent, false);
            bootstrapper.AddComponent<GameBootstrapper>();
        }

        private static void ApplyMaterial(GameObject ground, Material material)
        {
            if (material == null)
            {
                return;
            }

            MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static Material LoadOrCreateMaterial(string name, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.color = color;
                EditorUtility.SetDirty(material);
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = name,
                color = color
            };

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Pick(IReadOnlyList<GameObject> prefabs)
        {
            if (prefabs == null || prefabs.Count == 0)
            {
                return null;
            }

            return prefabs[Random.Next(prefabs.Count)];
        }

        private static float RandomRange(float min, float max)
        {
            return min + (float)Random.NextDouble() * (max - min);
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void RemoveDemoViewerComponents(GameObject player)
        {
            MonoBehaviour[] components = player.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                if (component != null && component.GetType().Name == "UniversalAnimationViewer")
                {
                    Object.DestroyImmediate(component);
                }
            }
        }

        private static void ConfigurePlayerRuntime(GameObject player, GameObject cameraObject)
        {
            PlayerInputReader inputReader = player.GetComponent<PlayerInputReader>();
            if (inputReader == null)
            {
                inputReader = player.AddComponent<PlayerInputReader>();
            }

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            if (inputActions == null)
            {
                inputActions = InputSystem.actions;
            }

            if (inputActions != null)
            {
                inputReader.SetInputActions(inputActions);
            }

            IsometricPlayerController playerController = player.GetComponent<IsometricPlayerController>();
            if (playerController == null)
            {
                playerController = player.AddComponent<IsometricPlayerController>();
            }
            playerController.SetInputReader(inputReader);

            PlayerAnimationDriver animationDriver = player.GetComponent<PlayerAnimationDriver>();
            if (animationDriver == null)
            {
                animationDriver = player.AddComponent<PlayerAnimationDriver>();
            }
            animationDriver.SetInputReader(inputReader);
            animationDriver.SetAnimator(player.GetComponentInChildren<Animator>());

            PlayerActionFeedback feedback = player.GetComponent<PlayerActionFeedback>();
            if (feedback == null)
            {
                feedback = player.AddComponent<PlayerActionFeedback>();
            }
            feedback.SetInputReader(inputReader);

            PlayerMotionVisualFeedback motionFeedback = player.GetComponent<PlayerMotionVisualFeedback>();
            if (motionFeedback == null)
            {
                motionFeedback = player.AddComponent<PlayerMotionVisualFeedback>();
            }
            motionFeedback.SetInputReader(inputReader);
            motionFeedback.SetVisualRoot(ResolvePlayerVisualRoot(player));

            PlayerActionDebugLog debugLog = player.GetComponent<PlayerActionDebugLog>();
            if (debugLog == null)
            {
                debugLog = player.AddComponent<PlayerActionDebugLog>();
            }
            debugLog.SetInputReader(inputReader);
            debugLog.SetWatchedTarget(player.transform);
            debugLog.SetSecondaryTarget(cameraObject != null ? cameraObject.transform : null);
            debugLog.SetMovementController(playerController);
            debugLog.SetMotionFeedback(motionFeedback);
            debugLog.SetCameraFollow(cameraObject != null ? cameraObject.GetComponent<IsometricCameraFollow>() : null);

            PlayerInventoryRuntime inventory = player.GetComponent<PlayerInventoryRuntime>();
            if (inventory == null)
            {
                inventory = player.AddComponent<PlayerInventoryRuntime>();
            }

            PlayerInteractionController interactionController = player.GetComponent<PlayerInteractionController>();
            if (interactionController == null)
            {
                interactionController = player.AddComponent<PlayerInteractionController>();
            }
            interactionController.SetInputReader(inputReader);

            PlayerSurvivalRuntime survival = player.GetComponent<PlayerSurvivalRuntime>();
            if (survival == null)
            {
                survival = player.AddComponent<PlayerSurvivalRuntime>();
            }
            survival.SetInputReader(inputReader);

            Animator animator = player.GetComponentInChildren<Animator>();
            RuntimeAnimatorController runtimeController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PlayerControllerPath);
            if (animator != null && runtimeController != null)
            {
                animator.runtimeAnimatorController = runtimeController;
                animationDriver.SetAnimator(animator);
            }
        }

        private static Transform ResolvePlayerVisualRoot(GameObject player)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = player.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshRenderer != null)
            {
                return skinnedMeshRenderer.transform;
            }

            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.transform != player.transform)
            {
                return animator.transform;
            }

            if (player.transform.childCount > 0)
            {
                return player.transform.GetChild(0);
            }

            return player.transform;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Scenes");
            EnsureFolder("Assets/_Project/Materials");
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/_Project/Scripts");
            EnsureFolder("Assets/_Project/Scripts/Editor");
            EnsureFolder("Assets/_Project/Scripts/Editor/World");
        }

        private static GameObject CreateHUD(Transform parent, GameObject player)
        {
            GameObject uiGo = CreateChild(parent, "UI");
            
            GameObject hudGo = new GameObject("PlayerHUD");
            hudGo.transform.SetParent(uiGo.transform, false);
            
            Canvas canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            CanvasScaler scaler = hudGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            
            hudGo.AddComponent<GraphicRaycaster>();
            
            PlayerHUDController hudController = hudGo.AddComponent<PlayerHUDController>();
            
            Font uiFont = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Create Stats Panel (Top Left)
            GameObject statsPanel = CreateUIPanel(hudGo.transform, "StatsPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(320, 228), new Vector2(24, -24));
            
            StatBarUI healthBar = CreateStatBar(statsPanel.transform, "HealthBar", "Health", Color.red, new Vector2(16, 168), uiFont);
            StatBarUI hungerBar = CreateStatBar(statsPanel.transform, "HungerBar", "Hunger", new Color(1f, 0.5f, 0f), new Vector2(16, 124), uiFont);
            StatBarUI staminaBar = CreateStatBar(statsPanel.transform, "StaminaBar", "Stamina", Color.yellow, new Vector2(16, 80), uiFont);
            StatBarUI restBar = CreateStatBar(statsPanel.transform, "RestBar", "Rest", Color.blue, new Vector2(16, 36), uiFont);

            // Create Resources Panel (Top Right)
            GameObject resourcePanel = CreateUIPanel(hudGo.transform, "ResourcePanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(240, 220), new Vector2(-24, -24));
            
            ResourceCounterUI woodCounter = CreateResourceCounter(resourcePanel.transform, "WoodCounter", "wood", "Wood", new Vector2(0, 0), uiFont);
            ResourceCounterUI stoneCounter = CreateResourceCounter(resourcePanel.transform, "StoneCounter", "stone", "Stone", new Vector2(0, -46), uiFont);
            ResourceCounterUI fiberCounter = CreateResourceCounter(resourcePanel.transform, "FiberCounter", "fiber", "Fiber", new Vector2(0, -92), uiFont);

            GameObject minimapPanel = CreateUIPanel(hudGo.transform, "MiniMapPanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(180, 180), new Vector2(-24, -252));
            var minimapGo = minimapPanel.AddComponent<MiniMapUI>();
            minimapGo.Configure(player.transform, 140f);

            // Link to controller
            SerializedObject so = new SerializedObject(hudController);
            so.FindProperty("survivalRuntime").objectReferenceValue = player.GetComponent<PlayerSurvivalRuntime>();
            so.FindProperty("inventoryRuntime").objectReferenceValue = player.GetComponent<PlayerInventoryRuntime>();
            so.FindProperty("healthBar").objectReferenceValue = healthBar;
            so.FindProperty("hungerBar").objectReferenceValue = hungerBar;
            so.FindProperty("staminaBar").objectReferenceValue = staminaBar;
            so.FindProperty("restBar").objectReferenceValue = restBar;
            
            SerializedProperty countersProp = so.FindProperty("resourceCounters");
            countersProp.ClearArray();
            countersProp.arraySize = 3;
            countersProp.GetArrayElementAtIndex(0).objectReferenceValue = woodCounter;
            countersProp.GetArrayElementAtIndex(1).objectReferenceValue = stoneCounter;
            countersProp.GetArrayElementAtIndex(2).objectReferenceValue = fiberCounter;
            so.ApplyModifiedProperties();

            // Add EventSystem
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            es.transform.SetParent(uiGo.transform, false);

            return uiGo;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            
            // Add a semi-transparent background
            UnityEngine.UI.Image bg = panel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0, 0, 0, 0.24f);
            
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return panel;
        }

        private static StatBarUI CreateStatBar(Transform parent, string name, string labelText, Color color, Vector2 pos, Font font)
        {
            GameObject bar = new GameObject(name);
            bar.transform.SetParent(parent, false);
            RectTransform rt = bar.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.sizeDelta = new Vector2(276, 34);
            rt.anchoredPosition = pos;

            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(bar.transform, false);
            UnityEngine.UI.Image bgImg = bg.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(bar.transform, false);
            UnityEngine.UI.Image fillImg = fill.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = color;
            // Use simple scaling if no sprite is available for 'Filled'
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;

            GameObject lbl = new GameObject("Label");
            lbl.transform.SetParent(bar.transform, false);
            Text t = lbl.AddComponent<Text>();
            t.text = labelText;
            t.font = font;
            t.fontSize = 17;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = Color.white;
            RectTransform lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.sizeDelta = new Vector2(-40, 0);
            lblRt.anchoredPosition = new Vector2(30, 0);

            StatBarUI ui = bar.AddComponent<StatBarUI>();
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("fillImage").objectReferenceValue = fillImg;
            so.FindProperty("label").objectReferenceValue = t;
            so.FindProperty("statName").stringValue = labelText;
            so.ApplyModifiedProperties();

            return ui;
        }

        private static ResourceCounterUI CreateResourceCounter(Transform parent, string name, string itemId, string labelText, Vector2 pos, Font font)
        {
            GameObject counter = new GameObject(name);
            counter.transform.SetParent(parent, false);
            RectTransform rt = counter.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(168, 44);
            rt.anchoredPosition = pos;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);

            GameObject lbl = new GameObject("Label");
            lbl.transform.SetParent(counter.transform, false);
            Text tL = lbl.AddComponent<Text>();
            tL.text = labelText + ":";
            tL.font = font;
            tL.fontSize = 17;
            tL.alignment = TextAnchor.MiddleRight;
            tL.color = Color.white;
            RectTransform lblRt = lbl.GetComponent<RectTransform>();
            lblRt.anchorMin = new Vector2(0, 0);
            lblRt.anchorMax = new Vector2(0.7f, 1);
            lblRt.sizeDelta = Vector2.zero;

            GameObject val = new GameObject("Value");
            val.transform.SetParent(counter.transform, false);
            Text tV = val.AddComponent<Text>();
            tV.text = "0";
            tV.font = font;
            tV.fontSize = 17;
            tV.fontStyle = FontStyle.Bold;
            tV.alignment = TextAnchor.MiddleLeft;
            tV.color = Color.yellow;
            RectTransform valRt = val.GetComponent<RectTransform>();
            valRt.anchorMin = new Vector2(0.75f, 0);
            valRt.anchorMax = new Vector2(1, 1);
            valRt.sizeDelta = Vector2.zero;

            ResourceCounterUI ui = counter.AddComponent<ResourceCounterUI>();
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("itemId").stringValue = itemId;
            so.FindProperty("countText").objectReferenceValue = tV;
            so.ApplyModifiedProperties();

            return ui;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private sealed class BiomeIdentityProfile
        {
            public BiomeKind Kind { get; set; }

            public string DisplayName { get; set; }

            public Color GroundColor { get; set; }

            public int ConiferTreeCount { get; set; }

            public int LeafyTreeCount { get; set; }

            public int DryTreeCount { get; set; }

            public int RockCount { get; set; }

            public int GreenBushCount { get; set; }

            public int DryBushCount { get; set; }

            public int GrassOrFlowerCount { get; set; }

            public int BerryBushCount { get; set; }

            public float MinTreeScale { get; set; } = 0.8f;

            public float MaxTreeScale { get; set; } = 1.2f;
        }

        private enum VegetationRole
        {
            ConiferTree,
            LeafyTree,
            DryTree,
            Rock,
            GreenBush,
            DryBush,
            GrassOrFlower,
            BerryBush
        }

        private enum BiomeKind
        {
            Westwood,
            SouthThicket,
            HearthMeadow,
            StonebackRidge,
            RedfangWilds
        }

        private readonly struct ScoredMaterial
        {
            public ScoredMaterial(Material material, int score)
            {
                Material = material;
                Score = score;
            }

            public Material Material { get; }

            public int Score { get; }
        }

        private readonly struct ScoredPrefab
        {
            public ScoredPrefab(GameObject prefab, int score)
            {
                Prefab = prefab;
                Score = score;
            }

            public GameObject Prefab { get; }

            public int Score { get; }
        }

        private sealed class VegetationCatalog
        {
            public IReadOnlyList<GameObject> Conifers { get; set; }
            public IReadOnlyList<GameObject> LeafyTrees { get; set; }
            public IReadOnlyList<GameObject> DryTrees { get; set; }
            public IReadOnlyList<GameObject> Rocks { get; set; }
            public IReadOnlyList<GameObject> GreenBushes { get; set; }
            public IReadOnlyList<GameObject> DryBushes { get; set; }
            public IReadOnlyList<GameObject> GrassOrFlowers { get; set; }
            public IReadOnlyList<GameObject> BerryBushes { get; set; }

            public IReadOnlyList<GameObject> Get(VegetationRole role)
            {
                return role switch
                {
                    VegetationRole.ConiferTree => Conifers,
                    VegetationRole.LeafyTree => LeafyTrees,
                    VegetationRole.DryTree => DryTrees,
                    VegetationRole.Rock => Rocks,
                    VegetationRole.GreenBush => GreenBushes,
                    VegetationRole.DryBush => DryBushes,
                    VegetationRole.GrassOrFlower => GrassOrFlowers,
                    VegetationRole.BerryBush => BerryBushes,
                    _ => Array.Empty<GameObject>()
                };
            }
        }

        private sealed class BiomeMaterials
        {
            public Material Westwood { get; set; }

            public Material SouthThicket { get; set; }

            public Material HearthMeadow { get; set; }

            public Material StonebackRidge { get; set; }

            public Material RedfangWilds { get; set; }

            public Material Get(BiomeKind biome)
            {
                return biome switch
                {
                    BiomeKind.Westwood => Westwood,
                    BiomeKind.SouthThicket => SouthThicket,
                    BiomeKind.HearthMeadow => HearthMeadow,
                    BiomeKind.StonebackRidge => StonebackRidge,
                    BiomeKind.RedfangWilds => RedfangWilds,
                    _ => HearthMeadow
                };
            }
        }

    }
}
