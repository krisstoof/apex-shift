using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.Art
{
    /// <summary>
    /// Scans prefabs for renderers whose shared materials are missing a base texture.
    /// Useful for finding Embersstorm prefabs that lost albedo/BaseMap assignments.
    /// </summary>
    public sealed class EmbersstormMissingTexturePrefabAuditWindow : EditorWindow
    {
        [SerializeField] private string prefabRoot = "Assets/_Project/Prefabs/World/Resources";
        [SerializeField] private string materialRoot = "Assets";
        [SerializeField] private bool scanAllPrefabsInRoot;
        [SerializeField] private bool ignoreMaterialsWithNoBaseTextureIfNameDoesNotSuggestTexture;
        [SerializeField] private bool onlyEmbersstormPrefabs;
        [SerializeField] private bool flagSuspiciousSnowOrTintedMaterials = true;

        private readonly List<PrefabIssue> issues = new List<PrefabIssue>();
        private Vector2 scroll;
        private string status = "Ready.";

        [MenuItem("Apex Shift/Art/Embersstorm Missing Texture Audit")]
        public static void Open()
        {
            GetWindow<EmbersstormMissingTexturePrefabAuditWindow>("Embersstorm Missing Texture Audit");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Embersstorm Missing Texture Audit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Scans prefabs and reports materials on renderers that have no _BaseMap/_MainTex assigned.",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                prefabRoot = EditorGUILayout.TextField("Prefab root", prefabRoot);
                materialRoot = EditorGUILayout.TextField("Material root", materialRoot);
                scanAllPrefabsInRoot = EditorGUILayout.Toggle("Scan all prefabs in root", scanAllPrefabsInRoot);
                ignoreMaterialsWithNoBaseTextureIfNameDoesNotSuggestTexture = EditorGUILayout.Toggle(
                    "Ignore generic empty materials",
                    ignoreMaterialsWithNoBaseTextureIfNameDoesNotSuggestTexture);
                onlyEmbersstormPrefabs = EditorGUILayout.Toggle("Only Embersstorm prefabs", onlyEmbersstormPrefabs);
                flagSuspiciousSnowOrTintedMaterials = EditorGUILayout.Toggle("Flag suspicious snow/tint materials", flagSuspiciousSnowOrTintedMaterials);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scan prefabs", GUILayout.Height(28f)))
                {
                    Scan();
                }

                GUI.enabled = issues.Count > 0;
                if (GUILayout.Button("Ping first issue", GUILayout.Height(28f)))
                {
                    PingIssue(issues[0]);
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField($"Issues found: {issues.Count}", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (PrefabIssue issue in issues)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(issue.PrefabPath, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.LabelField($"Renderer: {issue.RendererPath}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Material: {issue.MaterialPath} | Missing: {issue.MissingSlots}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(issue.Reason, EditorStyles.wordWrappedMiniLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Ping prefab", GUILayout.Width(90f))) PingPath(issue.PrefabPath);
                        if (GUILayout.Button("Ping material", GUILayout.Width(100f))) PingPath(issue.MaterialPath);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void Scan()
        {
            issues.Clear();

            if (!AssetDatabase.IsValidFolder(prefabRoot))
            {
                status = $"Invalid prefab root: {prefabRoot}";
                return;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabRoot });
            int scannedPrefabs = 0;
            int scannedMaterials = 0;

            try
            {
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    if (onlyEmbersstormPrefabs && !IsEmbersstormPrefab(prefabPath))
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar("Scanning prefabs for missing textures", prefabPath, prefabGuids.Length == 0 ? 1f : (float)i / prefabGuids.Length);

                    GameObject root = null;
                    try
                    {
                        root = PrefabUtility.LoadPrefabContents(prefabPath);
                        if (root == null)
                        {
                            continue;
                        }

                        scannedPrefabs++;
                        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                        {
                            if (renderer == null || renderer is ParticleSystemRenderer)
                            {
                                continue;
                            }

                            string rendererPath = GetHierarchyPath(renderer.transform, root.transform);
                            foreach (Material material in renderer.sharedMaterials)
                            {
                                if (material == null)
                                {
                                    continue;
                                }

                                scannedMaterials++;
                                string materialPath = AssetDatabase.GetAssetPath(material);
                                if (string.IsNullOrWhiteSpace(materialPath))
                                {
                                    continue;
                                }

                                Texture baseTexture = ReadBaseTexture(material);
                                bool suspiciousTint = flagSuspiciousSnowOrTintedMaterials && LooksSuspiciousTint(material);
                                bool suspiciousSnowName = flagSuspiciousSnowOrTintedMaterials && LooksLikeSnow(material.name);

                                bool baseTextureMissing = baseTexture == null;
                                bool reportMissing = baseTextureMissing;
                                bool reportSuspicious = !baseTextureMissing && (suspiciousTint || suspiciousSnowName);

                                if (!reportMissing && !reportSuspicious)
                                {
                                    continue;
                                }

                                if (baseTextureMissing
                                    && ignoreMaterialsWithNoBaseTextureIfNameDoesNotSuggestTexture
                                    && !LooksLikeTextureMaterial(material.name)
                                    && !suspiciousTint
                                    && !suspiciousSnowName)
                                {
                                    continue;
                                }

                                string missingSlots = GetMissingSlots(material);
                                string reason = baseTextureMissing
                                    ? $"Material '{material.name}' has no base texture on this prefab instance."
                                    : suspiciousSnowName
                                        ? $"Material '{material.name}' looks like a snow/winter variant on this prefab instance."
                                        : $"Material '{material.name}' has a suspicious tint on this prefab instance.";
                                issues.Add(new PrefabIssue(
                                    prefabPath,
                                    rendererPath,
                                    materialPath,
                                    missingSlots,
                                    reason));
                            }
                        }
                    }
                    finally
                    {
                        if (root != null)
                        {
                            PrefabUtility.UnloadPrefabContents(root);
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            issues.Sort((a, b) => string.CompareOrdinal(a.PrefabPath, b.PrefabPath));
            status = $"Scan complete. Prefabs {scannedPrefabs}, materials checked {scannedMaterials}, issues {issues.Count}.";
            Repaint();
        }

        private static Texture ReadBaseTexture(Material material)
        {
            if (material == null)
            {
                return null;
            }

            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
            {
                return material.GetTexture("_BaseMap");
            }

            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
            {
                return material.GetTexture("_MainTex");
            }

            return material.mainTexture;
        }

        private static string GetMissingSlots(Material material)
        {
            List<string> missing = new List<string>();
            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") == null)
            {
                missing.Add("_BaseMap");
            }

            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") == null)
            {
                missing.Add("_MainTex");
            }

            if (material.mainTexture == null)
            {
                missing.Add("mainTexture");
            }

            return string.Join(", ", missing.Distinct());
        }

        private static bool LooksLikeTextureMaterial(string value)
        {
            string text = (value ?? string.Empty).ToLowerInvariant();
            return text.Contains("texture")
                   || text.Contains("albedo")
                   || text.Contains("diffuse")
                   || text.Contains("base")
                   || text.Contains("color")
                   || text.Contains("col")
                   || text.Contains("leaf")
                   || text.Contains("bark")
                   || text.Contains("trunk")
                   || text.Contains("grass")
                   || text.Contains("rock")
                   || text.Contains("plant")
                   || text.Contains("berry")
                   || text.Contains("flower");
        }

        private static bool IsEmbersstormPrefab(string prefabPath)
        {
            string normalized = (prefabPath ?? string.Empty).ToLowerInvariant();
            return normalized.Contains("embersstorm") || normalized.Contains("embers storm");
        }

        private static bool LooksLikeSnow(string value)
        {
            string text = (value ?? string.Empty).ToLowerInvariant();
            return text.Contains("snow")
                   || text.Contains("winter")
                   || text.Contains("ice")
                   || text.Contains("frost")
                   || text.Contains("frozen");
        }

        private static bool LooksSuspiciousTint(Material material)
        {
            if (material == null)
            {
                return false;
            }

            Color color = material.HasProperty("_BaseColor")
                ? material.GetColor("_BaseColor")
                : material.HasProperty("_Color")
                    ? material.GetColor("_Color")
                    : material.color;

            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            bool cyanBlue = hue >= 0.45f && hue <= 0.72f && saturation >= 0.18f && value >= 0.45f;
            bool pink = (hue <= 0.05f || hue >= 0.88f) && saturation >= 0.18f && value >= 0.45f;
            return cyanBlue || pink;
        }

        private static string GetHierarchyPath(Transform transform, Transform root)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>();
            Transform current = transform;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            if (root != null)
            {
                parts.Add(root.name);
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static void PingPath(string path)
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj == null)
            {
                return;
            }

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        private static void PingIssue(PrefabIssue issue)
        {
            if (issue == null)
            {
                return;
            }

            PingPath(issue.PrefabPath);
        }

        private sealed class PrefabIssue
        {
            public PrefabIssue(string prefabPath, string rendererPath, string materialPath, string missingSlots, string reason)
            {
                PrefabPath = prefabPath;
                RendererPath = rendererPath;
                MaterialPath = materialPath;
                MissingSlots = missingSlots;
                Reason = reason;
            }

            public string PrefabPath { get; }
            public string RendererPath { get; }
            public string MaterialPath { get; }
            public string MissingSlots { get; }
            public string Reason { get; }
        }
    }
}
