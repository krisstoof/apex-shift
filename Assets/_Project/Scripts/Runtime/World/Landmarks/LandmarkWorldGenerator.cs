using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ApexShift.Runtime.World.Topography;

namespace ApexShift.Runtime.World.Landmarks
{
    public static class LandmarkWorldGenerator
    {
        public static IReadOnlyList<LandmarkPlacementResult> Generate(Transform parent,
            IslandTopographyRuntime topography, int seed, Func<Vector3, float> surfaceHeight = null)
        {
            LandmarkRegistry.ClearForWorldRegeneration();
            ClearExistingLandmarkChildren(parent);
            var results = new List<LandmarkPlacementResult>();
            if (parent == null || topography == null || !topography.IsBuilt) return results;

            var candidates = new List<LandmarkPlacementCandidate>();
            Bounds bounds = topography.WorldBounds;
            // Dense cache is the authority, including coastline cells. No biome/region lookup.
            for (float z = bounds.min.z + 2f; z < bounds.max.z; z += 4f)
            for (float x = bounds.min.x + 2f; x < bounds.max.x; x += 4f)
            {
                Vector3 position = new Vector3(x, 0f, z);
                if (topography.TryGetEnvironmentAt(position, out ApexShift.Runtime.World.Environment.EnvironmentSample sample))
                {
                    position.y = surfaceHeight != null ? surfaceHeight(position) : sample.Height;
                    candidates.Add(new LandmarkPlacementCandidate(position, sample));
                }
            }
            var planner = new LandmarkPlacementPlanner();
            Vector3? start = null;
            Vector3? cache = null;
            // Reserve scarce rocky/interior sites before the more flexible landmarks.
            // This is placement priority, not discovery/story progression.
            foreach (LandmarkPlacementProfile profile in LandmarkPlacementProfile.Production()
                .OrderBy(p => p.Type == LandmarkType.PlaneCrash ? 0 : p.Type == LandmarkType.BaseEntrance ? 1 : 2))
            {
                LandmarkPlacementResult result = planner.Plan(seed, candidates, profile, results, start,
                    profile.Type == LandmarkType.SmugglerCamp ? cache : null);
                if (result == null)
                {
                    Debug.LogWarning($"No safe landmark placement for '{profile.LandmarkId}' (seed {seed}).");
                    continue;
                }
                results.Add(result);
                CreateLandmarkObject(parent, profile.LandmarkId, profile.Type, null, null,
                    result.Position, profile.Type == LandmarkType.PlaneCrash);
                if (profile.Type == LandmarkType.PlaneCrash) start = result.Position;
                if (profile.Type == LandmarkType.SmugglerCache) cache = result.Position;
            }
            return results;
        }

        public static LandmarkRuntime CreateLandmarkObject(Transform parent, string id, LandmarkType type, string displayName, string description, Vector3 position, bool discovered)
        {
            GameObject go = new GameObject($"Landmark_{id}");
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = position;
            LandmarkRuntime runtime = go.AddComponent<LandmarkRuntime>();
            runtime.Configure(id, type, displayName, description, discovered);
            BuildVisual(runtime.transform, type);
            go.AddComponent<LandmarkDiscoveryRuntime>();
            return runtime;
        }

        private static void ClearExistingLandmarkChildren(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                GameObject go = child.gameObject;
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(go);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }
        }

        private static readonly Dictionary<LandmarkType, string> AuthoredModelNames = new Dictionary<LandmarkType, string>
        {
            { LandmarkType.OldTree, "old_tree_landmark" },
            { LandmarkType.Ruins, "ruins_landmark" },
            { LandmarkType.Pond, "pond_landmark" },
            { LandmarkType.Camp, "camp_landmark" },
            { LandmarkType.CavePlaceholder, "cave_landmark" },
            { LandmarkType.FreshwaterSource, "pond_landmark" },
            { LandmarkType.SmugglerCamp, "camp_landmark" },
            { LandmarkType.BaseEntrance, "cave_landmark" }
        };

        private static void BuildVisual(Transform root, LandmarkType type)
        {
            if (TryBuildAuthoredVisual(root, type))
            {
                return;
            }

            switch (type)
            {
                // Temporary prototype visuals only; no final art is generated here.
                case LandmarkType.PlaneCrash:
                    AddSphere(root, "PrototypeFuselage", new Vector3(0f, 0.65f, 0f), new Vector3(1.2f, 1.1f, 4.2f), new Color(0.75f, 0.77f, 0.78f));
                    AddCube(root, "PrototypeBrokenWing", new Vector3(-1.4f, 0.35f, 0f), Quaternion.Euler(0f, 18f, -12f), new Vector3(3.2f, 0.15f, 0.85f), new Color(0.7f, 0.71f, 0.72f));
                    AddCube(root, "PrototypeTail", new Vector3(0f, 0.9f, -1.7f), new Vector3(0.15f, 1.3f, 0.8f), new Color(0.55f, 0.15f, 0.1f));
                    break;
                case LandmarkType.SmugglerCache:
                    AddCube(root, "PrototypeCrateA", new Vector3(-0.6f, 0.45f, 0f), Vector3.one * 0.9f, new Color(0.4f, 0.27f, 0.12f));
                    AddCube(root, "PrototypeCrateB", new Vector3(0.5f, 0.35f, 0.2f), Vector3.one * 0.7f, new Color(0.48f, 0.32f, 0.13f));
                    AddCube(root, "PrototypeTarp", new Vector3(0f, 0.85f, 0.1f), new Vector3(1.7f, 0.05f, 1.1f), new Color(0.22f, 0.28f, 0.15f));
                    break;
                case LandmarkType.OldTree:
                    AddCylinder(root, "Trunk", new Vector3(0f, 1.2f, 0f), new Vector3(0.55f, 1.2f, 0.55f), new Color(0.30f, 0.18f, 0.09f));
                    AddSphere(root, "Canopy", new Vector3(0f, 2.35f, 0f), new Vector3(2.0f, 1.3f, 2.0f), new Color(0.10f, 0.38f, 0.13f));
                    break;
                case LandmarkType.Ruins:
                    AddCube(root, "StoneA", new Vector3(-0.65f, 0.35f, 0f), new Vector3(0.45f, 0.7f, 1.2f), new Color(0.46f, 0.45f, 0.40f));
                    AddCube(root, "StoneB", new Vector3(0.48f, 0.55f, 0.18f), new Vector3(0.40f, 1.1f, 0.35f), new Color(0.40f, 0.39f, 0.35f));
                    AddCube(root, "StoneSlab", new Vector3(0f, 0.95f, 0f), new Vector3(1.5f, 0.22f, 0.38f), new Color(0.36f, 0.35f, 0.32f));
                    break;
                case LandmarkType.FreshwaterSource:
                case LandmarkType.Pond:
                    AddCylinder(root, "PondBank", new Vector3(0f, 0.01f, 0f), new Vector3(2.15f, 0.03f, 2.15f), new Color(0.32f, 0.26f, 0.15f));
                    AddCylinder(root, "PondWater", new Vector3(0f, 0.03f, 0f), new Vector3(1.8f, 0.04f, 1.8f), new Color(0.10f, 0.35f, 0.62f, 0.72f));
                    break;
                case LandmarkType.SmugglerCamp:
                case LandmarkType.Camp:
                    AddCube(root, "CampLogA", new Vector3(-0.35f, 0.12f, 0f), Quaternion.Euler(0f, 25f, 0f), new Vector3(0.18f, 0.18f, 1.15f), new Color(0.38f, 0.22f, 0.11f));
                    AddCube(root, "CampLogB", new Vector3(0.35f, 0.12f, 0f), Quaternion.Euler(0f, -25f, 0f), new Vector3(0.18f, 0.18f, 1.15f), new Color(0.38f, 0.22f, 0.11f));
                    AddSphere(root, "AshPit", new Vector3(0f, 0.08f, 0f), new Vector3(0.55f, 0.12f, 0.55f), new Color(0.12f, 0.12f, 0.11f));
                    break;
                case LandmarkType.BaseEntrance:
                case LandmarkType.CavePlaceholder:
                    AddCube(root, "RockWall", new Vector3(0f, 0.85f, 0.12f), new Vector3(1.6f, 1.7f, 0.38f), new Color(0.28f, 0.27f, 0.25f));
                    AddCube(root, "BlockedEntrance", new Vector3(0f, 0.55f, -0.12f), new Vector3(0.8f, 0.9f, 0.28f), new Color(0.10f, 0.09f, 0.08f));
                    break;
                default:
                    AddSphere(root, "Marker", new Vector3(0f, 0.45f, 0f), Vector3.one * 0.6f, new Color(0.8f, 0.8f, 0.75f));
                    break;
            }
        }

        private static bool TryBuildAuthoredVisual(Transform root, LandmarkType type)
        {
            if (!AuthoredModelNames.TryGetValue(type, out string modelName))
            {
                return false;
            }

            GameObject prefab = UnityEngine.Resources.Load<GameObject>($"Landmarks/Models/{modelName}");
            if (prefab == null)
            {
                return false;
            }

            GameObject model = UnityEngine.Object.Instantiate(prefab, root, false);
            model.name = "AuthoredModel";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            return true;
        }

        private static GameObject AddCube(Transform parent, string name, Vector3 pos, Vector3 scale, Color color) => AddCube(parent, name, pos, Quaternion.identity, scale, color);
        private static GameObject AddCube(Transform parent, string name, Vector3 pos, Quaternion rot, Vector3 scale, Color color) => AddPrimitive(parent, name, PrimitiveType.Cube, pos, rot, scale, color);
        private static GameObject AddSphere(Transform parent, string name, Vector3 pos, Vector3 scale, Color color) => AddPrimitive(parent, name, PrimitiveType.Sphere, pos, Quaternion.identity, scale, color);
        private static GameObject AddCylinder(Transform parent, string name, Vector3 pos, Vector3 scale, Color color) => AddPrimitive(parent, name, PrimitiveType.Cylinder, pos, Quaternion.identity, scale, color);

        private static GameObject AddPrimitive(Transform parent, string name, PrimitiveType primitive, Vector3 pos, Quaternion rot, Vector3 scale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(collider);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                Material material = shader != null ? new Material(shader) : renderer.sharedMaterial;
                if (material != null)
                {
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    else material.color = color;
                    renderer.sharedMaterial = material;
                }
            }
            return go;
        }
    }
}
