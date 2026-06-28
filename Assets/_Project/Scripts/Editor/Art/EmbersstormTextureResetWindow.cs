using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.Art
{
    /// <summary>
    /// Reverts materials previously changed by EmbersstormTextureRelinkerWindow.
    /// Clears the tracked relink label and removes BaseMap/MainTex assignments.
    /// </summary>
    public sealed class EmbersstormTextureResetWindow : EditorWindow
    {
        private const string ChangedLabel = "embersstorm_texture_relinked";

        [SerializeField] private string scanRoot = "Assets";
        [SerializeField] private bool onlyMaterialsWithRelinkLabel = true;
        [SerializeField] private bool includeMaterialsWithoutBaseTextures = true;
        [SerializeField] private bool clearMainTex = true;
        [SerializeField] private bool clearBaseMap = true;
        [SerializeField] private bool clearMainTextureFallback = false;

        private readonly List<string> materialPaths = new List<string>();
        private Vector2 scroll;
        private string status = "Ready.";

        [MenuItem("Apex Shift/Art/Embersstorm Texture Reset")]
        public static void Open()
        {
            GetWindow<EmbersstormTextureResetWindow>("Embersstorm Texture Reset");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Embersstorm Texture Reset", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Finds materials touched by the relinker and removes their assigned base textures. Use this to restore the prefab look before relinking.",
                MessageType.Info);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                scanRoot = EditorGUILayout.TextField("Scan root", scanRoot);
                onlyMaterialsWithRelinkLabel = EditorGUILayout.Toggle("Only relinker-labeled materials", onlyMaterialsWithRelinkLabel);
                includeMaterialsWithoutBaseTextures = EditorGUILayout.Toggle("Include materials even when already empty", includeMaterialsWithoutBaseTextures);
                clearBaseMap = EditorGUILayout.Toggle("Clear _BaseMap", clearBaseMap);
                clearMainTex = EditorGUILayout.Toggle("Clear _MainTex", clearMainTex);
                clearMainTextureFallback = EditorGUILayout.Toggle("Clear material.mainTexture fallback", clearMainTextureFallback);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("1. Find materials", GUILayout.Height(28f)))
                {
                    FindMaterials();
                }

                GUI.enabled = materialPaths.Count > 0;
                if (GUILayout.Button("2. Reset materials", GUILayout.Height(28f)))
                {
                    ResetMaterials();
                }
                GUI.enabled = true;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(status, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField($"Materials queued: {materialPaths.Count}", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (string path in materialPaths)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(path, EditorStyles.wordWrappedLabel);
                    if (GUILayout.Button("Ping", GUILayout.Width(52f)))
                    {
                        Ping(path);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void FindMaterials()
        {
            materialPaths.Clear();

            if (string.IsNullOrWhiteSpace(scanRoot) || !AssetDatabase.IsValidFolder(scanRoot))
            {
                status = $"Invalid scan root: {scanRoot}";
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { scanRoot });
            int scanned = 0;
            int labeled = 0;
            int withTextures = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Finding relinker materials", path, guids.Length == 0 ? 1f : (float)i / guids.Length);

                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    continue;
                }

                scanned++;

                if (onlyMaterialsWithRelinkLabel)
                {
                    string[] labels = AssetDatabase.GetLabels(material);
                    if (labels == null || !labels.Contains(ChangedLabel))
                    {
                        continue;
                    }

                    labeled++;
                }

                if (HasAnyBaseTexture(material))
                {
                    withTextures++;
                    materialPaths.Add(path);
                }
                else if (includeMaterialsWithoutBaseTextures)
                {
                    materialPaths.Add(path);
                }
            }

            EditorUtility.ClearProgressBar();
            materialPaths.Sort(System.StringComparer.OrdinalIgnoreCase);
            status = $"Scanned {scanned} materials. Labeled {labeled}. With textures {withTextures}. Queued {materialPaths.Count}.";
            Repaint();
        }

        private void ResetMaterials()
        {
            if (!EditorUtility.DisplayDialog(
                    "Reset Embersstorm materials?",
                    $"Clear texture assignments on {materialPaths.Count} materials and remove the '{ChangedLabel}' label?",
                    "Reset",
                    "Cancel"))
            {
                return;
            }

            int changed = 0;
            int skipped = 0;
            foreach (string path in materialPaths.ToList())
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    skipped++;
                    continue;
                }

                bool modified = false;
                if (clearBaseMap && material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
                {
                    material.SetTexture("_BaseMap", null);
                    modified = true;
                }

                if (clearMainTex && material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
                {
                    material.SetTexture("_MainTex", null);
                    modified = true;
                }

                if (clearMainTextureFallback && material.mainTexture != null)
                {
                    material.mainTexture = null;
                    modified = true;
                }

                if (modified)
                {
                    string[] labels = AssetDatabase.GetLabels(material);
                    if (labels != null && labels.Length > 0)
                    {
                        HashSet<string> remaining = new HashSet<string>(labels.Where(label => label != ChangedLabel));
                        AssetDatabase.SetLabels(material, remaining.ToArray());
                    }

                    EditorUtility.SetDirty(material);
                    changed++;
                }
                else
                {
                    skipped++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            status = $"Reset complete. Changed {changed}, skipped {skipped}.";
            Repaint();
        }

        private static bool HasAnyBaseTexture(Material material)
        {
            if (material == null)
            {
                return false;
            }

            if (material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap") != null)
            {
                return true;
            }

            if (material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null)
            {
                return true;
            }

            return material.mainTexture != null;
        }

        private static void Ping(string path)
        {
            Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
            if (obj == null)
            {
                return;
            }

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }
    }
}
