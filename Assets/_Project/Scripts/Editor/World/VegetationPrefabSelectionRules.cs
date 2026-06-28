using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ApexShift.EditorTools.World
{
    internal static class VegetationPrefabSelectionRules
    {
        private const string EmbersStormNatureRoot = "embersstorm -  free nature pack";

        internal static IReadOnlyList<GameObject> FindPrefabsForRole(string roleName)
        {
            List<ScoredPrefab> found = new List<ScoredPrefab>();
            HashSet<GameObject> seen = new HashSet<GameObject>();
            List<ScoredPrefab> emberOnly = new List<ScoredPrefab>();

            foreach (string exactName in GetManualOverrideNamesForRole(roleName))
            {
                foreach (GameObject prefab in FindPrefabsByExactName(exactName))
                {
                    if (prefab == null || seen.Contains(prefab))
                    {
                        continue;
                    }

                        if (TryGetManualVegetationRole(prefab, out string manualRole) && manualRole == roleName)
                        {
                        int manualScore = IsEmbersStormPrefab(prefab) ? int.MaxValue : -1000;
                        if (manualScore > 0)
                        {
                            emberOnly.Add(new ScoredPrefab(prefab, manualScore));
                        }
                        seen.Add(prefab);
                        }
                    }
            }

            if (found.Count == 0)
            {
                foreach (string keyword in GetRoleKeywords(roleName))
                {
                    foreach (GameObject prefab in FindPrefabsByKeyword(keyword))
                    {
                        if (prefab == null || seen.Contains(prefab))
                        {
                            continue;
                        }

                        if (TryGetManualVegetationRole(prefab, out string manualRole))
                        {
                            if (manualRole != roleName)
                            {
                                continue;
                            }

                            int manualScore = IsEmbersStormPrefab(prefab) ? int.MaxValue : -1000;
                            if (manualScore > 0)
                            {
                                emberOnly.Add(new ScoredPrefab(prefab, manualScore));
                            }
                            seen.Add(prefab);
                            continue;
                        }

                        int score = ScorePrefabForRole(prefab, roleName, keyword);
                        if (score <= 0)
                        {
                            continue;
                        }

                        if (IsEmbersStormPrefab(prefab))
                        {
                            found.Add(new ScoredPrefab(prefab, score));
                        }
                        seen.Add(prefab);
                    }
                }
            }

            if (emberOnly.Count > 0)
            {
                emberOnly.Sort((a, b) => b.Score.CompareTo(a.Score));
                List<GameObject> emberResult = new List<GameObject>();
                foreach (ScoredPrefab item in emberOnly)
                {
                    emberResult.Add(item.Prefab);
                }
                return emberResult;
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

                foreach (GameObject prefab in FindPrefabsByExactName(known))
                {
                    if (prefab == null || IsSnowVariant(prefab))
                    {
                        continue;
                    }

                    if (TryGetManualVegetationRole(prefab, out string roleName))
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

        internal static bool TryGetManualVegetationRole(GameObject prefab, out string roleName)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            return TryGetManualVegetationRole(path, prefab.name, out roleName);
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

        private static IEnumerable<GameObject> FindPrefabsByExactName(string exactName)
        {
            string[] guids = AssetDatabase.FindAssets(exactName + " t:Prefab");
            foreach (string guid in guids)
            {
                GameObject prefab = LoadPrefab(guid);
                if (prefab != null)
                {
                    yield return prefab;
                }
            }
        }

        private static IEnumerable<GameObject> FindPrefabsByKeyword(string keyword)
        {
            string[] guids = AssetDatabase.FindAssets(keyword + " t:Prefab");
            foreach (string guid in guids)
            {
                GameObject prefab = LoadPrefab(guid);
                if (prefab != null)
                {
                    yield return prefab;
                }
            }
        }

        private static GameObject LoadPrefab(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (IsSnowVariant(path, path) || IsSnowVariant(path, Path.GetFileNameWithoutExtension(path)))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static int ScorePrefabForRole(GameObject prefab, string roleName, string keyword)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            string text = (path + " " + prefab.name).ToLowerInvariant();

            if (!IsEmbersStormPrefab(prefab))
            {
                return -1000;
            }

            if (IsForbiddenForAllNature(text))
            {
                return -1000;
            }

            if (text.Contains("low poly"))
            {
                return -1000;
            }

            int score = 0;
            if (text.Contains(EmbersStormNatureRoot))
            {
                score += 300;
            }

            score += ScoreEmbersStormFolderMatch(text, roleName);
            if (text.Contains(keyword.ToLowerInvariant())) score += 15;
            if (text.Contains("nature")) score += 25;
            if (text.Contains("nature pack")) score += 35;

            score += ScoreRoleText(text, roleName);
            score += ScoreRoleColor(prefab, roleName);
            return score;
        }

        private static bool IsEmbersStormPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            string path = NormalizeAssetName(AssetDatabase.GetAssetPath(prefab));
            string name = NormalizeAssetName(prefab.name);
            return path.Contains(EmbersStormNatureRoot) || name.Contains(EmbersStormNatureRoot);
        }

        private static int ScoreEmbersStormFolderMatch(string text, string roleName)
        {
            if (!text.Contains(EmbersStormNatureRoot))
            {
                return 0;
            }

            return roleName switch
            {
                "ConiferTree" => text.Contains("/prefabs/trees/pine tree/") ? 300 : text.Contains("/prefabs/trees/") ? 180 : 0,
                "LeafyTree" => text.Contains("/prefabs/trees/oak/") ? 300 : text.Contains("/prefabs/trees/") ? 180 : 0,
                "DryTree" => text.Contains("/prefabs/trees/dead trees/") ? 300 : text.Contains("/prefabs/trees/") ? 180 : 0,
                "Rock" => text.Contains("/prefabs/rocks/") ? 300 : 0,
                "GreenBush" => text.Contains("/prefabs/trees/bush/") ? 260 : text.Contains("/prefabs/plants/plant/") ? 160 : 0,
                "DryBush" => text.Contains("/prefabs/trees/dead trees/") ? 260 : text.Contains("/prefabs/trees/bush/") ? 160 : 0,
                "GrassOrFlower" => text.Contains("/prefabs/plants/grass/") ? 300 : text.Contains("/prefabs/plants/flowers/") ? 220 : 160,
                "BerryBush" => text.Contains("/prefabs/plants/plant/") ? 220 : text.Contains("/prefabs/trees/bush/") ? 160 : 0,
                _ => 0
            };
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
                    if (text.Contains("grass") || text.Contains("flower") || text.Contains("plant")) return 70;
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

            switch (roleName)
            {
                case "ConiferTree":
                case "LeafyTree":
                case "GreenBush":
                case "GrassOrFlower":
                case "BerryBush":
                    return greenDominant ? 15 : 0;
                case "Rock":
                    return grayDominant ? 20 : 0;
                case "DryTree":
                case "DryBush":
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

        private static bool IsSnowVariant(GameObject prefab)
        {
            return IsSnowVariant(AssetDatabase.GetAssetPath(prefab), prefab.name);
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
}
