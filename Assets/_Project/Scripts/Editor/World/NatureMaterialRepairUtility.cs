using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ApexShift.EditorTools.World
{
    public static class NatureMaterialRepairUtility
    {
        private const string GeneratedFolder = "Assets/_Project/Materials/Generated/Nature";

        public static void RepairMaterialsUnder(Transform root)
        {
            if (root == null)
            {
                return;
            }

            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Materials");
            EnsureFolder("Assets/_Project/Materials/Generated");
            EnsureFolder(GeneratedFolder);

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            Dictionary<string, Material> cache = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    continue;
                }

                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    Material repaired = RepairMaterial(source, renderer.name, i, cache);
                    if (repaired != source)
                    {
                        materials[i] = repaired;
                        changed = true;
                    }
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static Material RepairMaterial(Material source, string rendererName, int materialIndex, Dictionary<string, Material> cache)
        {
            string cacheKey = BuildCacheKey(source, rendererName, materialIndex);
            if (cache.TryGetValue(cacheKey, out Material cached))
            {
                return cached;
            }

            string materialName = string.IsNullOrWhiteSpace(source?.name)
                ? rendererName + "_Mat_" + materialIndex.ToString("00")
                : source.name;

            string assetPath = GeneratedFolder + "/" + SanitizeFileName(materialName) + ".mat";
            Material repaired = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (repaired == null)
            {
                Shader shader = PickCompatibleShader(source);
                repaired = new Material(shader)
                {
                    name = materialName
                };
                AssetDatabase.CreateAsset(repaired, assetPath);
            }

            CopySurfaceData(source, repaired);
            EditorUtility.SetDirty(repaired);
            cache[cacheKey] = repaired;
            return repaired;
        }

        private static Shader PickCompatibleShader(Material source)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader != null)
            {
                return shader;
            }

            shader = Shader.Find("Standard");
            if (shader != null)
            {
                return shader;
            }

            return Shader.Find("Diffuse");
        }

        private static void CopySurfaceData(Material source, Material destination)
        {
            if (destination == null)
            {
                return;
            }

            if (source == null)
            {
                destination.color = Color.white;
                return;
            }

            if (source.HasProperty("_BaseColor"))
            {
                destination.SetColor("_BaseColor", source.GetColor("_BaseColor"));
            }
            else if (source.HasProperty("_Color"))
            {
                Color color = source.GetColor("_Color");
                if (destination.HasProperty("_BaseColor"))
                {
                    destination.SetColor("_BaseColor", color);
                }
                else
                {
                    destination.color = color;
                }
            }
            else
            {
                destination.color = source.color;
            }

            TryCopyTexture(source, destination, "_BaseMap", "_BaseMap");
            TryCopyTexture(source, destination, "_MainTex", "_BaseMap");
            TryCopyTexture(source, destination, "_BumpMap", "_BumpMap");
            TryCopyTexture(source, destination, "_MetallicGlossMap", "_MetallicGlossMap");
            TryCopyTexture(source, destination, "_OcclusionMap", "_OcclusionMap");

            if (source.HasProperty("_Smoothness"))
            {
                CopyFloatIfExists(source, destination, "_Smoothness");
            }
        }

        private static void TryCopyTexture(Material source, Material destination, string sourceProperty, string destinationProperty)
        {
            if (source == null || destination == null)
            {
                return;
            }

            if (!source.HasProperty(sourceProperty) || !destination.HasProperty(destinationProperty))
            {
                return;
            }

            Texture texture = source.GetTexture(sourceProperty);
            if (texture != null)
            {
                destination.SetTexture(destinationProperty, texture);
            }
        }

        private static void CopyFloatIfExists(Material source, Material destination, string propertyName)
        {
            if (source.HasProperty(propertyName) && destination.HasProperty(propertyName))
            {
                destination.SetFloat(propertyName, source.GetFloat(propertyName));
            }
        }

        private static string BuildCacheKey(Material source, string rendererName, int materialIndex)
        {
            string sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
            return sourcePath + "|" + rendererName + "|" + materialIndex.ToString();
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace("/", "_").Replace("\\", "_");
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
    }
}
