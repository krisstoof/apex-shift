using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.Art
{
    /// <summary>
    /// Relinks original Embersstorm Texture2D assets into Material BaseMap/MainTex slots.
    ///
    /// Use when prefab materials render with flat/cyan/pink colors because albedo/base texture
    /// references were lost after import, extraction, quarantine, or prefab wrapping.
    /// </summary>
    public sealed class EmbersstormTextureRelinkerWindow : EditorWindow
    {
        private const string ChangedLabel = "embersstorm_texture_relinked";

        [SerializeField] private string prefabRoot = "Assets/_Project/Prefabs/World/Resources";
        [SerializeField] private string textureRoot = "Assets";
        [SerializeField] private string materialRoot = "Assets";
        [SerializeField] private bool scanPrefabMaterials = true;
        [SerializeField] private bool scanAllMaterialsInMaterialRoot;
        [SerializeField] private bool scanAllMaterialsAsFallback = true;
        [SerializeField] private bool scanPrefabRegistryAssets = true;
        [SerializeField] private bool overwriteExistingBaseMaps;
        [SerializeField] private bool replaceSuspiciousExistingBaseMaps = true;
        [SerializeField] private bool ignoreSnowWinterIceTextures;
        [SerializeField] private bool preferNonSnowTexturesForSnowMaterials = true;
        [SerializeField] private bool ignoreNonAlbedoTextures = true;
        [SerializeField] private bool neutralizeMaterialTintOnRelink = true;
        [SerializeField] private bool dryRun = true;
        [SerializeField] private int minimumScoreToRelink = 45;

        private readonly List<TextureCandidate> textures = new List<TextureCandidate>();
        private readonly List<RelinkCandidate> relinks = new List<RelinkCandidate>();
        private Vector2 scroll;
        private string status = "Ready.";

        [MenuItem("Apex Shift/Art/Embersstorm Texture Relinker")]
        public static void Open()
        {
            GetWindow<EmbersstormTextureRelinkerWindow>("Embersstorm Texture Relinker");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Embersstorm Texture Relinker", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Finds original albedo/base textures and assigns them back to Material BaseMap/MainTex. Start with Dry Run enabled.",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                prefabRoot = EditorGUILayout.TextField("Prefab root", prefabRoot);
                textureRoot = EditorGUILayout.TextField("Texture root", textureRoot);
                materialRoot = EditorGUILayout.TextField("Material root", materialRoot);
                scanPrefabMaterials = EditorGUILayout.Toggle("Scan prefab materials", scanPrefabMaterials);
                scanAllMaterialsInMaterialRoot = EditorGUILayout.Toggle("Scan all materials in root", scanAllMaterialsInMaterialRoot);
                scanAllMaterialsAsFallback = EditorGUILayout.Toggle("Fallback: scan all materials when prefab scan finds none", scanAllMaterialsAsFallback);
                scanPrefabRegistryAssets = EditorGUILayout.Toggle("Scan PrefabRegistry assets", scanPrefabRegistryAssets);
                overwriteExistingBaseMaps = EditorGUILayout.Toggle("Overwrite existing BaseMaps", overwriteExistingBaseMaps);
                replaceSuspiciousExistingBaseMaps = EditorGUILayout.Toggle("Replace suspicious existing BaseMaps", replaceSuspiciousExistingBaseMaps);
                ignoreSnowWinterIceTextures = EditorGUILayout.Toggle("Ignore snow/winter/ice textures", ignoreSnowWinterIceTextures);
                preferNonSnowTexturesForSnowMaterials = EditorGUILayout.Toggle("Prefer non-snow textures for snow materials", preferNonSnowTexturesForSnowMaterials);
                ignoreNonAlbedoTextures = EditorGUILayout.Toggle("Ignore normal/ORM/mask textures", ignoreNonAlbedoTextures);
                neutralizeMaterialTintOnRelink = EditorGUILayout.Toggle("Neutralize material tint on relink", neutralizeMaterialTintOnRelink);
                minimumScoreToRelink = EditorGUILayout.IntSlider("Minimum match score", minimumScoreToRelink, 10, 95);
                dryRun = EditorGUILayout.Toggle("Dry run only", dryRun);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("1. Build texture index", GUILayout.Height(28f)))
                {
                    BuildTextureIndex();
                }

                GUI.enabled = textures.Count > 0;
                if (GUILayout.Button("2. Find relinks", GUILayout.Height(28f)))
                {
                    FindRelinks();
                }

                GUI.enabled = relinks.Count > 0;
                if (GUILayout.Button(dryRun ? "3. Dry run" : "3. Apply relinks", GUILayout.Height(28f)))
                {
                    ApplyRelinks();
                }
                GUI.enabled = true;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Quick setup: snow -> non-snow", GUILayout.Height(24f)))
                {
                    ignoreSnowWinterIceTextures = false;
                    preferNonSnowTexturesForSnowMaterials = true;
                    scanAllMaterialsAsFallback = true;
                    scanPrefabRegistryAssets = true;
                    replaceSuspiciousExistingBaseMaps = true;
                    neutralizeMaterialTintOnRelink = true;
                    dryRun = true;
                    minimumScoreToRelink = 30;
                    status = "Quick setup applied. This includes registry prefabs and suspicious existing BaseMaps. Build texture index, then find relinks.";
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField($"Textures indexed: {textures.Count} | Relink candidates: {relinks.Count}", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (RelinkCandidate candidate in relinks)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(candidate.MaterialPath, EditorStyles.wordWrappedLabel);
                        if (GUILayout.Button("Mat", GUILayout.Width(44f))) Ping(candidate.MaterialPath);
                        if (GUILayout.Button("Tex", GUILayout.Width(44f))) Ping(candidate.TexturePath);
                    }

                    EditorGUILayout.LabelField($"score:{candidate.Score} {candidate.MaterialName} -> {candidate.TextureName}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(candidate.Reason, EditorStyles.wordWrappedMiniLabel);
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private static void Ping(string path)
        {
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj == null) return;
            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        private void BuildTextureIndex()
        {
            textures.Clear();
            relinks.Clear();

            if (!AssetDatabase.IsValidFolder(textureRoot))
            {
                status = $"Invalid texture root: {textureRoot}";
                return;
            }

            int skipped = 0;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { textureRoot });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Indexing original textures", path, guids.Length == 0 ? 1f : (float)i / guids.Length);

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                {
                    skipped++;
                    continue;
                }

                string name = Path.GetFileNameWithoutExtension(path);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ignoreNonAlbedoTextures && importer != null && importer.textureType == TextureImporterType.NormalMap)
                {
                    skipped++;
                    continue;
                }

                if (ignoreNonAlbedoTextures && LooksLikeNonAlbedo(name))
                {
                    skipped++;
                    continue;
                }

                if (ignoreSnowWinterIceTextures && LooksLikeSnow(name))
                {
                    skipped++;
                    continue;
                }

                textures.Add(new TextureCandidate(path, texture.name, Tokens(name), GuessKind(name)));
            }

            EditorUtility.ClearProgressBar();
            status = $"Texture index complete. Indexed {textures.Count}, skipped {skipped}.";
            Repaint();
        }

        private void FindRelinks()
        {
            relinks.Clear();
            HashSet<string> materialPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int prefabMaterialCount = 0;

            if (scanPrefabMaterials)
            {
                prefabMaterialCount = CollectPrefabMaterials(materialPaths);
            }

            if (scanPrefabRegistryAssets)
            {
                prefabMaterialCount += CollectPrefabRegistryMaterials(materialPaths);
            }

            if ((scanAllMaterialsInMaterialRoot || (scanAllMaterialsAsFallback && materialPaths.Count == 0)) && AssetDatabase.IsValidFolder(materialRoot))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { materialRoot }))
                {
                    materialPaths.Add(AssetDatabase.GUIDToAssetPath(guid));
                }
            }

            int checkedMaterials = 0;
            foreach (string materialPath in materialPaths.OrderBy(x => x))
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) continue;
                checkedMaterials++;

                Texture existingTexture = ReadBaseTexture(material);
                if (!ShouldProcessMaterial(material, existingTexture))
                {
                    continue;
                }

                RelinkCandidate candidate = FindBest(materialPath, material);
                if (candidate != null && candidate.Score >= minimumScoreToRelink)
                {
                    relinks.Add(candidate);
                }
            }

            relinks.Sort((a, b) => b.Score.CompareTo(a.Score));
            status = $"Prefab materials: {prefabMaterialCount}. Checked {checkedMaterials} materials. Found {relinks.Count} candidates.";
            Repaint();
        }

        private int CollectPrefabMaterials(HashSet<string> materialPaths)
        {
            if (!AssetDatabase.IsValidFolder(prefabRoot))
            {
                status = $"Invalid prefab root: {prefabRoot}";
                return 0;
            }

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabRoot });
            int added = 0;
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                EditorUtility.DisplayProgressBar("Collecting prefab materials", prefabPath, prefabGuids.Length == 0 ? 1f : (float)i / prefabGuids.Length);
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(prefabPath);
                    foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer == null || renderer is ParticleSystemRenderer) continue;
                        foreach (Material material in renderer.sharedMaterials)
                        {
                            string path = AssetDatabase.GetAssetPath(material);
                            if (!string.IsNullOrWhiteSpace(path) && materialPaths.Add(path))
                            {
                                added++;
                            }
                        }
                    }
                }
                finally
                {
                    if (root != null) PrefabUtility.UnloadPrefabContents(root);
                }
            }

            EditorUtility.ClearProgressBar();
            return added;
        }

        private int CollectPrefabRegistryMaterials(HashSet<string> materialPaths)
        {
            int added = 0;
            string[] registryGuids = AssetDatabase.FindAssets("t:PrefabRegistry");
            foreach (string guid in registryGuids)
            {
                string registryPath = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object registry = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(registryPath);
                if (registry == null)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(registry);
                SerializedProperty resourcePrefabs = serialized.FindProperty("resourcePrefabs");
                if (resourcePrefabs == null || !resourcePrefabs.isArray)
                {
                    continue;
                }

                for (int i = 0; i < resourcePrefabs.arraySize; i++)
                {
                    SerializedProperty entry = resourcePrefabs.GetArrayElementAtIndex(i);
                    SerializedProperty prefabProp = entry.FindPropertyRelative("prefab");
                    GameObject prefab = prefabProp != null ? prefabProp.objectReferenceValue as GameObject : null;
                    if (prefab == null)
                    {
                        continue;
                    }

                    foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer == null || renderer is ParticleSystemRenderer)
                        {
                            continue;
                        }

                        foreach (Material material in renderer.sharedMaterials)
                        {
                            string materialPath = AssetDatabase.GetAssetPath(material);
                            if (!string.IsNullOrWhiteSpace(materialPath) && materialPaths.Add(materialPath))
                            {
                                added++;
                            }
                        }
                    }
                }
            }

            return added;
        }

        private RelinkCandidate FindBest(string materialPath, Material material)
        {
            string materialName = Path.GetFileNameWithoutExtension(materialPath);
            string[] materialTokens = Tokens(materialName);
            TextureKind preferredKind = GuessKind(materialName);

            bool materialLooksSnow = LooksLikeSnow(materialName);
            TextureCandidate best = null;
            int bestScore = 0;
            string bestReason = "";

            foreach (TextureCandidate texture in textures)
            {
                int score = Score(materialTokens, preferredKind, materialLooksSnow, texture, out string reason);
                if (score > bestScore)
                {
                    best = texture;
                    bestScore = score;
                    bestReason = reason;
                }
            }

            return best == null ? null : new RelinkCandidate(materialPath, material.name, best.Path, best.Name, bestScore, bestReason);
        }

        private int Score(string[] materialTokens, TextureKind preferredKind, bool materialLooksSnow, TextureCandidate texture, out string reason)
        {
            HashSet<string> a = new HashSet<string>(materialTokens);
            HashSet<string> b = new HashSet<string>(texture.Tokens);
            int overlap = a.Intersect(b).Count();
            int score = overlap * 14;
            List<string> reasons = new List<string>();
            if (overlap > 0) reasons.Add($"token overlap:{overlap}");

            if (preferredKind != TextureKind.Unknown && texture.Kind == preferredKind)
            {
                score += 35;
                reasons.Add($"kind:{preferredKind}");
            }

            if (materialLooksSnow)
            {
                if (LooksLikeSnow(texture.Name))
                {
                    score -= 35;
                    reasons.Add("snow-like target");
                }
                else if (preferNonSnowTexturesForSnowMaterials)
                {
                    score += 22;
                    reasons.Add("non-snow preferred");
                }
            }

            if (b.Contains("albedo") || b.Contains("base") || b.Contains("diffuse") || b.Contains("color") || b.Contains("col"))
            {
                score += 20;
                reasons.Add("albedo-like");
            }

            if (a.Contains("leaf") && b.Contains("leaves")) score += 12;
            if (a.Contains("leaves") && b.Contains("leaf")) score += 12;
            if (a.Contains("trunk") && b.Contains("bark")) score += 16;
            if (a.Contains("bark") && b.Contains("trunk")) score += 16;
            if (a.Contains("rock") && b.Contains("stone")) score += 12;
            if (a.Contains("stone") && b.Contains("rock")) score += 12;

            reason = string.Join(", ", reasons);
            return score;
        }

        private void ApplyRelinks()
        {
            if (dryRun)
            {
                status = $"Dry run only. {relinks.Count} candidates listed; no files changed.";
                return;
            }

            bool confirm = EditorUtility.DisplayDialog(
                "Apply Embersstorm texture relinks?",
                $"Assign Texture2D assets to {relinks.Count} material BaseMap/MainTex slots.\n\nOverwrite existing maps: {overwriteExistingBaseMaps}",
                "Apply",
                "Cancel");

            if (!confirm) return;

            int changed = 0;
            int skipped = 0;
            foreach (RelinkCandidate candidate in relinks)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(candidate.MaterialPath);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(candidate.TexturePath);
                if (material == null || texture == null)
                {
                    skipped++;
                    continue;
                }

                if (!overwriteExistingBaseMaps && ReadBaseTexture(material) != null)
                {
                    skipped++;
                    continue;
                }

                AssignBaseTexture(material, texture);
                if (neutralizeMaterialTintOnRelink)
                {
                    NeutralizeMaterialTint(material);
                }
                HashSet<string> labels = new HashSet<string>(AssetDatabase.GetLabels(material));
                labels.Add(ChangedLabel);
                AssetDatabase.SetLabels(material, labels.ToArray());
                EditorUtility.SetDirty(material);
                changed++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            status = $"Relink complete. Changed {changed}, skipped {skipped}.";
        }

        private static Texture ReadBaseTexture(Material material)
        {
            if (material == null) return null;
            if (material.HasProperty("_BaseMap"))
            {
                Texture texture = material.GetTexture("_BaseMap");
                if (texture != null) return texture;
            }
            if (material.HasProperty("_MainTex"))
            {
                Texture texture = material.GetTexture("_MainTex");
                if (texture != null) return texture;
            }
            return material.mainTexture;
        }

        private bool ShouldProcessMaterial(Material material, Texture existingTexture)
        {
            if (material == null)
            {
                return false;
            }

            if (overwriteExistingBaseMaps || existingTexture == null)
            {
                return true;
            }

            if (!replaceSuspiciousExistingBaseMaps)
            {
                return false;
            }

            string materialName = material.name ?? string.Empty;
            string textureName = existingTexture.name ?? string.Empty;
            return LooksLikeSnow(materialName)
                   || LooksLikeSnow(textureName)
                   || LooksLikeNonAlbedo(textureName)
                   || LooksLikeSuspiciousTint(material);
        }

        private static void AssignBaseTexture(Material material, Texture texture)
        {
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            material.mainTexture = texture;
        }

        private static void NeutralizeMaterialTint(Material material)
        {
            if (material == null)
            {
                return;
            }

            Color white = Color.white;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", white);
            material.color = white;
        }

        private static bool LooksLikeSuspiciousTint(Material material)
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

        private static TextureKind GuessKind(string value)
        {
            string v = (value ?? "").ToLowerInvariant();
            if (Has(v, "leaf", "leaves", "foliage", "crown")) return TextureKind.Leaves;
            if (Has(v, "trunk", "bark", "wood", "branch")) return TextureKind.Trunk;
            if (Has(v, "rock", "stone", "boulder")) return TextureKind.Rock;
            if (Has(v, "grass", "reed", "plant", "bush", "shrub")) return TextureKind.Grass;
            if (Has(v, "flower", "petal", "blossom")) return TextureKind.Flower;
            if (Has(v, "berry", "fruit")) return TextureKind.Berry;
            return TextureKind.Unknown;
        }

        private static string[] Tokens(string value)
        {
            return (value ?? "")
                .ToLowerInvariant()
                .Replace('-', '_')
                .Replace(' ', '_')
                .Replace('.', '_')
                .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 1)
                .Where(x => x != "mat" && x != "material" && x != "tex" && x != "texture")
                .ToArray();
        }

        private static bool LooksLikeNonAlbedo(string value)
        {
            string v = (value ?? "").ToLowerInvariant();
            return Has(v, "_nrm", "_normal", "normal", "_orm", "_mask", "_metal", "metallic", "roughness", "_ao", "ambientocclusion");
        }

        private static bool LooksLikeSnow(string value)
        {
            string v = (value ?? "").ToLowerInvariant();
            return Has(v, "snow", "winter", "ice", "frost", "frozen");
        }

        private static bool Has(string value, params string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
            {
                if (value.Contains(needles[i])) return true;
            }
            return false;
        }

        private enum TextureKind
        {
            Unknown,
            Leaves,
            Trunk,
            Rock,
            Grass,
            Flower,
            Berry
        }

        private sealed class TextureCandidate
        {
            public TextureCandidate(string path, string name, string[] tokens, TextureKind kind)
            {
                Path = path;
                Name = name;
                Tokens = tokens ?? Array.Empty<string>();
                Kind = kind;
            }

            public string Path { get; }
            public string Name { get; }
            public string[] Tokens { get; }
            public TextureKind Kind { get; }
        }

        private sealed class RelinkCandidate
        {
            public RelinkCandidate(string materialPath, string materialName, string texturePath, string textureName, int score, string reason)
            {
                MaterialPath = materialPath;
                MaterialName = materialName;
                TexturePath = texturePath;
                TextureName = textureName;
                Score = score;
                Reason = reason;
            }

            public string MaterialPath { get; }
            public string MaterialName { get; }
            public string TexturePath { get; }
            public string TextureName { get; }
            public int Score { get; }
            public string Reason { get; }
        }
    }
}
