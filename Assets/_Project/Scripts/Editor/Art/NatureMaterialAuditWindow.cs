using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.Art
{
    /// <summary>
    /// Audits imported nature materials for winter/snow/cold palettes and suspicious texture assignments.
    /// This complements SnowTextureCleanerWindow.
    /// </summary>
    public sealed class NatureMaterialAuditWindow : EditorWindow
    {
        private const string CandidateLabel = "nature_material_review";

        [SerializeField] private string scanRoot = "Assets";
        [SerializeField] private string quarantineFolder = "Assets/_Project/Quarantine/NatureMaterials";
        [SerializeField] private bool flagByName = true;
        [SerializeField] private bool flagColdBaseColors = true;
        [SerializeField] private bool flagWrongBaseMapAssignments = true;
        [SerializeField] private bool flagMissingBaseMapWhenNamedTextureMaterial;
        [SerializeField] private float brightValueThreshold = 0.55f;
        [SerializeField] private float cyanHueMin = 0.42f;
        [SerializeField] private float cyanHueMax = 0.62f;
        [SerializeField] private float blueHueMin = 0.58f;
        [SerializeField] private float blueHueMax = 0.72f;
        [SerializeField] private float maxGreenDominanceForColdCandidate = 0.10f;
        [SerializeField] private float minColdSaturation = 0.12f;

        private readonly List<MaterialCandidate> candidates = new List<MaterialCandidate>();
        private Vector2 scroll;
        private string status = "Ready.";

        [MenuItem("Apex Shift/Art/Nature Material Audit")]
        public static void Open()
        {
            GetWindow<NatureMaterialAuditWindow>("Nature Material Audit");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Nature Material Audit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use this after swapping nature asset packs. It finds winter/snow/ice palettes stored in Material colors and suspicious albedo texture assignments.",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                scanRoot = EditorGUILayout.TextField("Scan root", scanRoot);
                quarantineFolder = EditorGUILayout.TextField("Quarantine folder", quarantineFolder);
                flagByName = EditorGUILayout.Toggle("Flag snow/winter names", flagByName);
                flagColdBaseColors = EditorGUILayout.Toggle("Flag cold base colors", flagColdBaseColors);
                flagWrongBaseMapAssignments = EditorGUILayout.Toggle("Flag normal/ORM/mask as base map", flagWrongBaseMapAssignments);
                flagMissingBaseMapWhenNamedTextureMaterial = EditorGUILayout.Toggle("Flag missing base map if material name suggests texture", flagMissingBaseMapWhenNamedTextureMaterial);

                brightValueThreshold = EditorGUILayout.Slider("Cold color min value", brightValueThreshold, 0.20f, 0.95f);
                minColdSaturation = EditorGUILayout.Slider("Cold color min saturation", minColdSaturation, 0.00f, 0.80f);
                maxGreenDominanceForColdCandidate = EditorGUILayout.Slider("Max green dominance", maxGreenDominanceForColdCandidate, -0.25f, 0.50f);
                cyanHueMin = EditorGUILayout.Slider("Cyan hue min", cyanHueMin, 0.00f, 1.00f);
                cyanHueMax = EditorGUILayout.Slider("Cyan hue max", cyanHueMax, 0.00f, 1.00f);
                blueHueMin = EditorGUILayout.Slider("Blue hue min", blueHueMin, 0.00f, 1.00f);
                blueHueMax = EditorGUILayout.Slider("Blue hue max", blueHueMax, 0.00f, 1.00f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan materials", GUILayout.Height(28f)))
                {
                    Scan();
                }

                GUI.enabled = candidates.Count > 0;
                if (GUILayout.Button("Label candidates", GUILayout.Height(28f)))
                {
                    LabelCandidates();
                }

                if (GUILayout.Button("Move materials to quarantine", GUILayout.Height(28f)))
                {
                    MoveCandidatesToQuarantine();
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField($"Candidates: {candidates.Count}", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (MaterialCandidate candidate in candidates)
            {
                DrawCandidate(candidate);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawCandidate(MaterialCandidate candidate)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Rect swatch = GUILayoutUtility.GetRect(28f, 28f, GUILayout.Width(28f));
                    EditorGUI.DrawRect(swatch, candidate.BaseColor);
                    EditorGUILayout.LabelField(candidate.AssetPath, EditorStyles.wordWrappedLabel);

                    if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                    {
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(candidate.AssetPath);
                        if (obj != null)
                        {
                            EditorGUIUtility.PingObject(obj);
                            Selection.activeObject = obj;
                        }
                    }
                }

                EditorGUILayout.LabelField(
                    $"score:{candidate.Score:0.000} hue:{candidate.Hue:0.000} sat:{candidate.Saturation:0.000} val:{candidate.Value:0.000} baseMap:{candidate.BaseMapName}",
                    EditorStyles.miniLabel);
                EditorGUILayout.LabelField(candidate.Reason, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void Scan()
        {
            candidates.Clear();

            if (string.IsNullOrWhiteSpace(scanRoot) || !AssetDatabase.IsValidFolder(scanRoot))
            {
                status = $"Invalid scan root: {scanRoot}";
                Repaint();
                return;
            }

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { scanRoot });
            int scanned = 0;

            for (int i = 0; i < materialGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(materialGuids[i]);
                EditorUtility.DisplayProgressBar("Auditing nature materials", path, materialGuids.Length == 0 ? 1f : (float)i / materialGuids.Length);
                try
                {
                    Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        continue;
                    }

                    scanned++;
                    MaterialCandidate candidate = AnalyzeMaterial(path, material);
                    if (candidate != null)
                    {
                        candidates.Add(candidate);
                    }
                }
                finally
                {
                    if (i == materialGuids.Length - 1)
                    {
                        EditorUtility.ClearProgressBar();
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            status = $"Scan complete. Scanned {scanned}, candidates {candidates.Count}.";
            Repaint();
        }

        private MaterialCandidate AnalyzeMaterial(string path, Material material)
        {
            if (!LooksLikeNatureMaterial(path, material))
            {
                return null;
            }

            List<string> reasons = new List<string>();
            float score = 0f;
            string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

            if (flagByName && LooksLikeWinterName(file))
            {
                reasons.Add("name suggests snow/winter/ice/frost variant");
                score += 0.65f;
            }

            Color baseColor = ReadBaseColor(material);
            Color.RGBToHSV(baseColor, out float hue, out float saturation, out float value);
            bool coldColor = flagColdBaseColors && IsColdNatureColor(baseColor, hue, saturation, value);
            if (coldColor)
            {
                reasons.Add($"base color looks cold/cyan/blue ({ColorUtility.ToHtmlStringRGB(baseColor)})");
                score += 0.45f + Mathf.Clamp01(saturation) * 0.20f;
            }

            Texture baseTexture = ReadBaseTexture(material);
            string baseTextureName = baseTexture != null ? baseTexture.name : "none";
            if (flagWrongBaseMapAssignments && baseTexture != null && LooksLikeNonAlbedoTexture(baseTexture.name))
            {
                reasons.Add($"base map appears to be a non-albedo texture: {baseTexture.name}");
                score += 0.85f;
            }

            if (flagMissingBaseMapWhenNamedTextureMaterial && baseTexture == null && LooksLikeTextureMaterial(file))
            {
                reasons.Add("material name suggests texture/albedo but base map is empty");
                score += 0.30f;
            }

            if (reasons.Count == 0)
            {
                return null;
            }

            return new MaterialCandidate(path, baseColor, hue, saturation, value, baseTextureName, score, string.Join("; ", reasons));
        }

        private static Color ReadBaseColor(Material material)
        {
            if (material == null)
            {
                return Color.white;
            }

            if (material.HasProperty("_BaseColor"))
            {
                return material.GetColor("_BaseColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return material.color;
        }

        private static Texture ReadBaseTexture(Material material)
        {
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty("_BaseMap"))
            {
                return material.GetTexture("_BaseMap");
            }

            if (material.HasProperty("_MainTex"))
            {
                return material.GetTexture("_MainTex");
            }

            return material.mainTexture;
        }

        private static bool LooksLikeNatureMaterial(string path, Material material)
        {
            string text = ((path ?? string.Empty) + " " + (material != null ? material.name : string.Empty))
                .ToLowerInvariant();

            if (text.Contains("skybox") || text.Contains("terrain") || text.Contains("water"))
            {
                return false;
            }

            return text.Contains("nature")
                   || text.Contains("tree")
                   || text.Contains("bush")
                   || text.Contains("grass")
                   || text.Contains("flower")
                   || text.Contains("rock")
                   || text.Contains("plant")
                   || text.Contains("stone")
                   || text.Contains("embersstorm");
        }

        private bool IsColdNatureColor(Color color, float hue, float saturation, float value)
        {
            if (value < brightValueThreshold || saturation < minColdSaturation)
            {
                return false;
            }

            float greenDominance = color.g - Mathf.Max(color.r, color.b);
            if (greenDominance > maxGreenDominanceForColdCandidate)
            {
                return false;
            }

            bool cyan = IsHueInRange(hue, cyanHueMin, cyanHueMax);
            bool blue = IsHueInRange(hue, blueHueMin, blueHueMax);
            bool blueBiased = color.b > color.g * 0.88f && color.b > color.r * 1.10f;
            bool veryLightLowSat = value > 0.78f && saturation < 0.28f;
            return cyan || blue || blueBiased || veryLightLowSat;
        }

        private static bool LooksLikeWinterName(string value)
        {
            return value.Contains("snow")
                   || value.Contains("winter")
                   || value.Contains("ice")
                   || value.Contains("frost")
                   || value.Contains("frozen");
        }

        private static bool LooksLikeTextureMaterial(string value)
        {
            return value.Contains("albedo")
                   || value.Contains("diffuse")
                   || value.Contains("base")
                   || value.Contains("texture");
        }

        private static bool LooksLikeNonAlbedoTexture(string value)
        {
            string file = (value ?? string.Empty).Trim().ToLowerInvariant();
            return file.Contains("_nrm")
                   || file.Contains("_normal")
                   || file.Contains("normal")
                   || file.Contains("_orm")
                   || file.Contains("_mask")
                   || file.Contains("_metal")
                   || file.Contains("metallic")
                   || file.Contains("roughness")
                   || file.Contains("_ao")
                   || file.Contains("ambientocclusion");
        }

        private static bool IsHueInRange(float hue, float min, float max)
        {
            min = Mathf.Repeat(min, 1f);
            max = Mathf.Repeat(max, 1f);
            hue = Mathf.Repeat(hue, 1f);
            if (min <= max)
            {
                return hue >= min && hue <= max;
            }

            return hue >= min || hue <= max;
        }

        private void LabelCandidates()
        {
            int changed = 0;
            foreach (MaterialCandidate candidate in candidates)
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(candidate.AssetPath);
                if (obj == null)
                {
                    continue;
                }

                HashSet<string> labels = new HashSet<string>(AssetDatabase.GetLabels(obj));
                if (labels.Add(CandidateLabel))
                {
                    AssetDatabase.SetLabels(obj, labels.ToArray());
                    changed++;
                }
            }

            status = $"Labeled {changed} candidates with '{CandidateLabel}'.";
        }

        private void MoveCandidatesToQuarantine()
        {
            if (candidates.Count == 0)
            {
                status = "No material candidates to move.";
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Move material candidates?",
                $"Move {candidates.Count} material assets to:\n{quarantineFolder}\n\nThis can break prefab references. Prefer labeling and manual replacement first.",
                "Move",
                "Cancel");

            if (!confirm)
            {
                return;
            }

            EnsureFolder(quarantineFolder);
            int moved = 0;
            int failed = 0;

            foreach (MaterialCandidate candidate in candidates.ToList())
            {
                string target = AssetDatabase.GenerateUniqueAssetPath($"{quarantineFolder}/{Path.GetFileName(candidate.AssetPath)}");
                string error = AssetDatabase.MoveAsset(candidate.AssetPath, target);
                if (string.IsNullOrEmpty(error))
                {
                    moved++;
                }
                else
                {
                    failed++;
                    Debug.LogWarning($"[NatureMaterialAudit] Failed to move '{candidate.AssetPath}' -> '{target}': {error}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            candidates.Clear();
            status = $"Moved {moved} material candidates to quarantine. Failed: {failed}.";
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string[] parts = folder.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
            {
                throw new InvalidOperationException("Quarantine folder must be inside Assets.");
            }

            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private sealed class MaterialCandidate
        {
            public MaterialCandidate(string assetPath, Color baseColor, float hue, float saturation, float value, string baseMapName, float score, string reason)
            {
                AssetPath = assetPath;
                BaseColor = baseColor;
                Hue = hue;
                Saturation = saturation;
                Value = value;
                BaseMapName = baseMapName;
                Score = score;
                Reason = reason;
            }

            public string AssetPath { get; }
            public Color BaseColor { get; }
            public float Hue { get; }
            public float Saturation { get; }
            public float Value { get; }
            public string BaseMapName { get; }
            public float Score { get; }
            public string Reason { get; }
        }
    }
}
