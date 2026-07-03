using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ApexShift.Runtime.Items
{
    public static class ItemModelResolver
    {
        private static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

        public static bool TryInstantiateItemModel(string itemId, Transform parent, out GameObject model)
        {
            model = null;
            string normalized = Normalize(itemId);
            if (string.IsNullOrWhiteSpace(normalized) || parent == null)
            {
                return false;
            }

            GameObject prefab = ResolvePrefab(normalized);
            if (prefab == null)
            {
                return false;
            }

            model = UnityEngine.Object.Instantiate(prefab, parent, false);
            model.name = $"ItemModel_{normalized}";
            SanitizeForItemVisual(model, normalized);

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                UnityEngine.Object.Destroy(model);
                model = null;
                Debug.LogWarning($"[ItemModelResolver] Resolved model for '{normalized}', but it has no renderers.");
                return false;
            }

            return true;
        }

        public static bool NormalizeModelToBounds(GameObject model, Transform referenceParent, float targetMaxDimension, Vector3 desiredCenter, Quaternion localRotation)
        {
            if (model == null || referenceParent == null)
            {
                return false;
            }

            targetMaxDimension = Mathf.Max(0.01f, targetMaxDimension);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = localRotation;
            model.transform.localScale = Vector3.one;

            if (!TryGetLocalRendererBounds(model, referenceParent, out Bounds bounds))
            {
                return false;
            }

            float maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
            if (maxDimension <= 0.001f)
            {
                return false;
            }

            float scale = targetMaxDimension / maxDimension;
            model.transform.localScale = Vector3.one * scale;

            if (!TryGetLocalRendererBounds(model, referenceParent, out bounds))
            {
                return false;
            }

            model.transform.localPosition += desiredCenter - bounds.center;
            return true;
        }

        public static void ClearCache()
        {
            Cache.Clear();
        }

        private static GameObject ResolvePrefab(string normalizedItemId)
        {
            if (Cache.TryGetValue(normalizedItemId, out GameObject cached))
            {
                return cached;
            }

            GameObject prefab = LoadFromResources(normalizedItemId);
#if UNITY_EDITOR
            if (prefab == null)
            {
                prefab = LoadFromAssetDatabase(normalizedItemId);
            }
#endif
            Cache[normalizedItemId] = prefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[ItemModelResolver] No authored item model found for '{normalizedItemId}'. Falling back to procedural visual.");
            }

            return prefab;
        }

        private static GameObject LoadFromResources(string normalizedItemId)
        {
            string[] paths =
            {
                $"Items/Models/{normalizedItemId}",
                $"Items/Models/item_{normalizedItemId}",
                $"Items/Models/tool_{normalizedItemId}",
                $"Items/Models/weapon_{normalizedItemId}",
                $"ItemModels/{normalizedItemId}",
                $"ItemModels/item_{normalizedItemId}",
                $"ItemModels/tool_{normalizedItemId}",
                $"ItemModels/weapon_{normalizedItemId}",
                $"Models/Items/{normalizedItemId}",
                $"Models/Items/item_{normalizedItemId}",
                $"Models/Items/tool_{normalizedItemId}",
                $"Models/Items/weapon_{normalizedItemId}",
                $"Models/Weapons/{normalizedItemId}",
                $"Models/Weapons/item_{normalizedItemId}",
                $"Models/Weapons/tool_{normalizedItemId}",
                $"Models/Weapons/weapon_{normalizedItemId}",
                $"Weapons/{normalizedItemId}",
                $"Weapons/weapon_{normalizedItemId}",
                $"Crafting/Models/{normalizedItemId}",
                $"Crafting/Models/craft_{normalizedItemId}",
                $"ApexShift/Items/{normalizedItemId}",
                $"ApexShift/Items/item_{normalizedItemId}",
            };

            for (int i = 0; i < paths.Length; i++)
            {
                GameObject prefab = UnityEngine.Resources.Load<GameObject>(paths[i]);
                if (prefab != null)
                {
                    return prefab;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private static GameObject LoadFromAssetDatabase(string normalizedItemId)
        {
            List<Candidate> candidates = new List<Candidate>();
            string[] searchTerms = GetAssetSearchTerms(normalizedItemId);
            HashSet<string> visitedGuids = new HashSet<string>();

            for (int termIndex = 0; termIndex < searchTerms.Length; termIndex++)
            {
                string[] guids = AssetDatabase.FindAssets($"{searchTerms[termIndex]} t:GameObject");
                for (int i = 0; i < guids.Length; i++)
                {
                    if (!visitedGuids.Add(guids[i]))
                    {
                        continue;
                    }

                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }

                    string fileName = Normalize(Path.GetFileNameWithoutExtension(path));
                    string normalizedPath = Normalize(path);
                    int score = ScoreCandidate(fileName, normalizedPath, normalizedItemId);
                    if (score <= 0)
                    {
                        continue;
                    }

                    GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (asset != null)
                    {
                        candidates.Add(new Candidate(asset, score, path));
                    }
                }
            }

            candidates.Sort((a, b) =>
            {
                int score = b.Score.CompareTo(a.Score);
                return score != 0 ? score : string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
            });

            return candidates.Count > 0 ? candidates[0].Prefab : null;
        }

        private static int ScoreCandidate(string fileName, string path, string itemId)
        {
            string singular = itemId.EndsWith("s", StringComparison.OrdinalIgnoreCase) ? itemId.Substring(0, itemId.Length - 1) : itemId;
            int score = 0;
            if (fileName == itemId) score += 100;
            if (fileName == singular) score += 96;
            if (fileName == $"item{itemId}") score += 95;
            if (fileName == $"tool{itemId}") score += 95;
            if (fileName == $"weapon{itemId}") score += 95;
            if (fileName == $"craft{itemId}") score += 90;
            if (fileName == $"resource{itemId}") score += 90;
            if (fileName.Contains(itemId)) score += 50;
            if (fileName.Contains(singular)) score += 35;
            if (path.Contains("item")) score += 20;
            if (path.Contains("tool")) score += 20;
            if (path.Contains("weapon")) score += 20;
            if (path.Contains("model")) score += 20;
            if (path.Contains("lowpoly") || path.Contains("lowpoly")) score += 10;
            if (path.Contains("resources")) score += 10;
            if (path.Contains("editor")) score -= 30;
            if (path.Contains("animation") || path.Contains("anim")) score -= 25;
            if (path.Contains("material") || path.EndsWith(".mat")) score -= 100;
            return score;
        }

        private static string[] GetAssetSearchTerms(string normalizedItemId)
        {
            List<string> terms = new List<string>();
            AddIfMissing(terms, normalizedItemId);
            AddIfMissing(terms, $"item {normalizedItemId}");
            AddIfMissing(terms, $"tool {normalizedItemId}");
            AddIfMissing(terms, $"weapon {normalizedItemId}");

            switch (normalizedItemId)
            {
                case "bow":
                    AddIfMissing(terms, "longbow");
                    AddIfMissing(terms, "shortbow");
                    AddIfMissing(terms, "bow weapon");
                    break;
                case "spear":
                    AddIfMissing(terms, "javelin");
                    AddIfMissing(terms, "polearm");
                    AddIfMissing(terms, "spear weapon");
                    break;
                case "axe":
                    AddIfMissing(terms, "hatchet");
                    AddIfMissing(terms, "axe tool");
                    AddIfMissing(terms, "axe weapon");
                    break;
                case "pickaxe":
                    AddIfMissing(terms, "pick axe");
                    AddIfMissing(terms, "pick");
                    AddIfMissing(terms, "mining tool");
                    break;
                case "torch":
                    AddIfMissing(terms, "fire torch");
                    AddIfMissing(terms, "torch tool");
                    break;
                case "arrow":
                    AddIfMissing(terms, "arrow projectile");
                    break;
            }

            return terms.ToArray();
        }

        private static void AddIfMissing(List<string> terms, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !terms.Contains(value))
            {
                terms.Add(value);
            }
        }

        private readonly struct Candidate
        {
            public readonly GameObject Prefab;
            public readonly int Score;
            public readonly string Path;

            public Candidate(GameObject prefab, int score, string path)
            {
                Prefab = prefab;
                Score = score;
                Path = path;
            }
        }
#endif

        private static void SanitizeForItemVisual(GameObject model, string itemId)
        {
            StripRuntimeColliders(model);

            Rigidbody[] rigidbodies = model.GetComponentsInChildren<Rigidbody>(true);
            for (int i = rigidbodies.Length - 1; i >= 0; i--) DestroyComponent(rigidbodies[i]);

            Animator[] animators = model.GetComponentsInChildren<Animator>(true);
            for (int i = animators.Length - 1; i >= 0; i--) DestroyComponent(animators[i]);

            AudioSource[] audioSources = model.GetComponentsInChildren<AudioSource>(true);
            for (int i = audioSources.Length - 1; i >= 0; i--) DestroyComponent(audioSources[i]);

            UnityEngine.Camera[] cameras = model.GetComponentsInChildren<UnityEngine.Camera>(true);
            for (int i = cameras.Length - 1; i >= 0; i--) DestroyComponent(cameras[i]);

            SpriteRenderer[] sprites = model.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = sprites.Length - 1; i >= 0; i--) DestroyComponent(sprites[i]);

            Canvas[] canvases = model.GetComponentsInChildren<Canvas>(true);
            for (int i = canvases.Length - 1; i >= 0; i--) DestroyComponent(canvases[i]);
        }

        private static void DestroyComponent(Component component)
        {
            if (component == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(component);
            else UnityEngine.Object.DestroyImmediate(component);
        }

        private static void StripRuntimeColliders(GameObject model)
        {
            Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
            for (int i = colliders.Length - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(colliders[i]);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(colliders[i]);
                }
            }
        }

        private static bool TryGetLocalRendererBounds(GameObject root, Transform referenceParent, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool initialized = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                Vector3 min = worldBounds.min;
                Vector3 max = worldBounds.max;
                Vector3[] corners =
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z),
                };

                for (int c = 0; c < corners.Length; c++)
                {
                    Vector3 local = referenceParent.InverseTransformPoint(corners[c]);
                    if (!initialized)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        initialized = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            return initialized;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim()
                .ToLowerInvariant()
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty);
        }
    }
}
