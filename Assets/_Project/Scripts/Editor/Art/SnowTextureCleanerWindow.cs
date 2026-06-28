using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.Art
{
    /// <summary>
    /// Editor-only helper for cleaning imported nature asset packs that include snow variants.
    ///
    /// The tool does not delete anything automatically. It scans Texture2D assets by color,
    /// labels likely snow textures, and can move them into a quarantine folder for manual review.
    /// </summary>
    public sealed class SnowTextureCleanerWindow : EditorWindow
    {
        private const string CandidateLabel = "snow_candidate";
        private const int MaxScanSize = 192;

        [SerializeField] private string scanRoot = "Assets";
        [SerializeField] private string quarantineFolder = "Assets/_Project/Quarantine/SnowTextures";
        [SerializeField] private float minimumSnowPixelRatio = 0.18f;
        [SerializeField] private float brightValueThreshold = 0.72f;
        [SerializeField] private float lowSaturationThreshold = 0.30f;
        [SerializeField] private float coldHueMin = 0.48f;
        [SerializeField] private float coldHueMax = 0.72f;
        [SerializeField] private bool skipNormalMaps = true;
        [SerializeField] private bool skipMaskLikeTextures = true;
        [SerializeField] private bool includeAlreadyQuarantined;

        private readonly List<SnowCandidate> candidates = new List<SnowCandidate>();
        private Vector2 scroll;
        private string status = "Ready.";

        [MenuItem("Apex Shift/Art/Snow Texture Cleaner")]
        public static void Open()
        {
            GetWindow<SnowTextureCleanerWindow>("Snow Texture Cleaner");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Snow Texture Cleaner", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scans Texture2D assets by color. It is heuristic: use it to quarantine likely snow textures, not to delete files blindly.",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                scanRoot = EditorGUILayout.TextField("Scan root", scanRoot);
                quarantineFolder = EditorGUILayout.TextField("Quarantine folder", quarantineFolder);

                minimumSnowPixelRatio = EditorGUILayout.Slider("Min snow pixel ratio", minimumSnowPixelRatio, 0.01f, 0.80f);
                brightValueThreshold = EditorGUILayout.Slider("Bright value threshold", brightValueThreshold, 0.45f, 0.98f);
                lowSaturationThreshold = EditorGUILayout.Slider("Low saturation threshold", lowSaturationThreshold, 0.05f, 0.65f);
                coldHueMin = EditorGUILayout.Slider("Cold hue min", coldHueMin, 0.00f, 1.00f);
                coldHueMax = EditorGUILayout.Slider("Cold hue max", coldHueMax, 0.00f, 1.00f);

                skipNormalMaps = EditorGUILayout.Toggle("Skip normal maps", skipNormalMaps);
                skipMaskLikeTextures = EditorGUILayout.Toggle("Skip mask/ORM/AO-like names", skipMaskLikeTextures);
                includeAlreadyQuarantined = EditorGUILayout.Toggle("Include quarantined", includeAlreadyQuarantined);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan textures", GUILayout.Height(28f)))
                {
                    Scan();
                }

                GUI.enabled = candidates.Count > 0;
                if (GUILayout.Button("Label candidates", GUILayout.Height(28f)))
                {
                    LabelCandidates();
                }

                if (GUILayout.Button("Move to quarantine", GUILayout.Height(28f)))
                {
                    MoveCandidatesToQuarantine();
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField($"Candidates: {candidates.Count}", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (SnowCandidate candidate in candidates)
            {
                DrawCandidate(candidate);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawCandidate(SnowCandidate candidate)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
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
                    $"snow:{candidate.SnowRatio:0.000} cold:{candidate.ColdWhiteRatio:0.000} avgV:{candidate.AverageValue:0.000} score:{candidate.Score:0.000} size:{candidate.Width}x{candidate.Height}",
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

            string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { scanRoot });
            int scanned = 0;
            int skipped = 0;

            try
            {
                for (int i = 0; i < textureGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(textureGuids[i]);
                    EditorUtility.DisplayProgressBar("Scanning textures for snow colors", path, textureGuids.Length == 0 ? 1f : (float)i / textureGuids.Length);

                    if (ShouldSkipPath(path))
                    {
                        skipped++;
                        continue;
                    }

                    TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null)
                    {
                        skipped++;
                        continue;
                    }

                    if (skipNormalMaps && importer.textureType == TextureImporterType.NormalMap)
                    {
                        skipped++;
                        continue;
                    }

                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture == null)
                    {
                        skipped++;
                        continue;
                    }

                    scanned++;
                    SnowCandidate candidate = AnalyzeTexture(path, texture);
                    if (candidate != null && candidate.SnowRatio >= minimumSnowPixelRatio)
                    {
                        candidates.Add(candidate);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            candidates.Sort((a, b) => b.Score.CompareTo(a.Score));
            status = $"Scan complete. Scanned {scanned}, skipped {skipped}, candidates {candidates.Count}.";
            Repaint();
        }

        private bool ShouldSkipPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return true;
            }

            if (!includeAlreadyQuarantined && !string.IsNullOrWhiteSpace(quarantineFolder)
                                          && path.StartsWith(quarantineFolder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!skipMaskLikeTextures)
            {
                return false;
            }

            string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
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

        private SnowCandidate AnalyzeTexture(string path, Texture2D source)
        {
            Texture2D readable = null;
            try
            {
                readable = CreateReadableThumbnail(source, MaxScanSize);
                if (readable == null)
                {
                    return null;
                }

                Color32[] pixels = readable.GetPixels32();
                if (pixels == null || pixels.Length == 0)
                {
                    return null;
                }

                int visible = 0;
                int snow = 0;
                int coldWhite = 0;
                float valueSum = 0f;
                float saturationSum = 0f;
                float blueBiasSum = 0f;

                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 p = pixels[i];
                    if (p.a < 16)
                    {
                        continue;
                    }

                    visible++;
                    float r = p.r / 255f;
                    float g = p.g / 255f;
                    float b = p.b / 255f;
                    float max = Mathf.Max(r, Mathf.Max(g, b));
                    float min = Mathf.Min(r, Mathf.Min(g, b));
                    float saturation = max <= 0.0001f ? 0f : (max - min) / max;
                    float hue;
                    float hsvS;
                    float hsvV;
                    Color.RGBToHSV(new Color(r, g, b, 1f), out hue, out hsvS, out hsvV);

                    valueSum += max;
                    saturationSum += saturation;
                    blueBiasSum += b - Mathf.Max(r, g);

                    bool bright = max >= brightValueThreshold;
                    bool lowSat = saturation <= lowSaturationThreshold;
                    bool neutralSnow = bright && saturation <= Mathf.Min(lowSaturationThreshold, 0.18f);
                    bool coldHue = IsHueInRange(hue, coldHueMin, coldHueMax);
                    bool blueBiasedWhite = bright && lowSat && b + 0.03f >= r && b + 0.03f >= g;
                    bool coldSnow = bright && lowSat && (coldHue || blueBiasedWhite);

                    if (neutralSnow || coldSnow)
                    {
                        snow++;
                    }

                    if (coldSnow)
                    {
                        coldWhite++;
                    }
                }

                if (visible == 0)
                {
                    return null;
                }

                float snowRatio = snow / (float)visible;
                float coldRatio = coldWhite / (float)visible;
                float avgValue = valueSum / visible;
                float avgSat = saturationSum / visible;
                float avgBlueBias = blueBiasSum / visible;
                float score = snowRatio * 0.70f + coldRatio * 0.20f + Mathf.Clamp01(avgValue - avgSat) * 0.10f;

                string reason = $"Detected bright low-saturation pixels. avgSat:{avgSat:0.000} avgBlueBias:{avgBlueBias:0.000}";
                return new SnowCandidate(path, source.width, source.height, snowRatio, coldRatio, avgValue, score, reason);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SnowTextureCleaner] Could not analyze '{path}': {ex.Message}");
                return null;
            }
            finally
            {
                if (readable != null)
                {
                    DestroyImmediate(readable);
                }
            }
        }

        private static Texture2D CreateReadableThumbnail(Texture2D source, int maxSize)
        {
            if (source == null)
            {
                return null;
            }

            int width = source.width;
            int height = source.height;
            if (width <= 0 || height <= 0)
            {
                return null;
            }

            float scale = Mathf.Min(1f, maxSize / (float)Mathf.Max(width, height));
            int targetWidth = Mathf.Max(1, Mathf.RoundToInt(width * scale));
            int targetHeight = Mathf.Max(1, Mathf.RoundToInt(height * scale));

            RenderTexture previous = RenderTexture.active;
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D readable = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false, false);
            try
            {
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                readable.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
                readable.Apply(false, false);
                return readable;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
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
            foreach (SnowCandidate candidate in candidates)
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
                status = "No candidates to move.";
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Move snow texture candidates?",
                $"Move {candidates.Count} texture assets to:\n{quarantineFolder}\n\nThis is safer than deleting. Unity will move .meta files too.",
                "Move",
                "Cancel");

            if (!confirm)
            {
                return;
            }

            EnsureFolder(quarantineFolder);
            int moved = 0;
            int failed = 0;

            foreach (SnowCandidate candidate in candidates.ToList())
            {
                if (!File.Exists(candidate.AssetPath))
                {
                    failed++;
                    continue;
                }

                string fileName = Path.GetFileName(candidate.AssetPath);
                string target = AssetDatabase.GenerateUniqueAssetPath($"{quarantineFolder}/{fileName}");
                string error = AssetDatabase.MoveAsset(candidate.AssetPath, target);
                if (string.IsNullOrEmpty(error))
                {
                    moved++;
                }
                else
                {
                    failed++;
                    Debug.LogWarning($"[SnowTextureCleaner] Failed to move '{candidate.AssetPath}' -> '{target}': {error}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            candidates.Clear();
            status = $"Moved {moved} candidates to quarantine. Failed: {failed}.";
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

        private sealed class SnowCandidate
        {
            public SnowCandidate(
                string assetPath,
                int width,
                int height,
                float snowRatio,
                float coldWhiteRatio,
                float averageValue,
                float score,
                string reason)
            {
                AssetPath = assetPath;
                Width = width;
                Height = height;
                SnowRatio = snowRatio;
                ColdWhiteRatio = coldWhiteRatio;
                AverageValue = averageValue;
                Score = score;
                Reason = reason;
            }

            public string AssetPath { get; }
            public int Width { get; }
            public int Height { get; }
            public float SnowRatio { get; }
            public float ColdWhiteRatio { get; }
            public float AverageValue { get; }
            public float Score { get; }
            public string Reason { get; }
        }
    }
}
