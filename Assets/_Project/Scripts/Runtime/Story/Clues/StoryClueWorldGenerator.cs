using System;
using System.Collections.Generic;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Topography;
using UnityEngine;

namespace ApexShift.Runtime.Story.Clues
{
    public static class StoryClueWorldGenerator
    {
        private static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();

        public static void Generate(Transform landmarkRoot, IslandTopographyRuntime topography, int seed,
            Func<Vector3, float> surfaceHeight = null)
        {
            StoryClueRegistry.ClearForWorldRegeneration();
            if (landmarkRoot == null || topography == null) return;
            Transform old = landmarkRoot.Find("StoryClues");
            if (old != null)
            {
                old.gameObject.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(old.gameObject);
                else UnityEngine.Object.DestroyImmediate(old.gameObject);
            }
            var root = new GameObject("StoryClues").transform;
            root.SetParent(landmarkRoot, false);
            var crash = LandmarkRegistry.FindById("plane_crash");
            if (crash == null) { Debug.LogWarning("[StoryClue] Missing plane crash anchor."); return; }
            var planner = new StoryCluePlacementPlanner();
            foreach (var definition in StoryClueDefinition.Production())
            {
                var anchor = LandmarkRegistry.FindById(definition.AnchorLandmarkId);
                var destination = LandmarkRegistry.FindById(definition.DirectionLandmarkId);
                if (anchor == null || !planner.TryPlan(seed, definition, topography, anchor.transform.position,
                    crash.transform.position, destination != null ? (Vector3?)destination.transform.position : null,
                    out Vector3 position, surfaceHeight))
                {
                    Debug.LogWarning("[StoryClue] No safe placement for '" + definition.ClueId + "' (seed " + seed + ").");
                    continue;
                }
                CreateClue(root, definition, position);
            }
        }

        public static StoryClueRuntime CreateClue(Transform parent, StoryClueDefinition definition, Vector3 position)
        {
            var owner = new GameObject("Clue_" + definition.ClueId);
            owner.transform.SetParent(parent, false);
            owner.transform.position = position;
            var collider = owner.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.2f, 1f, 1.2f);
            collider.center = Vector3.up * 0.5f;
            var runtime = owner.AddComponent<StoryClueRuntime>();
            runtime.Configure(definition);
            // Temporary runtime primitives, not production art or imported/generated assets.
            switch (definition.ClueId)
            {
                case "smuggler_cache_manifest":
                    Box(owner.transform, "TransportCrate", new Vector3(0f, 0.3f, 0f), new Vector3(0.8f, 0.6f, 0.65f), "wood", new Color(0.45f, 0.3f, 0.15f));
                    Box(owner.transform, "ShippingLabel", new Vector3(0f, 0.61f, 0f), new Vector3(0.45f, 0.02f, 0.3f), "paper", new Color(0.8f, 0.77f, 0.6f));
                    Box(owner.transform, "PackingStrap", new Vector3(0.25f, 0.62f, 0f), new Vector3(0.06f, 0.025f, 0.66f), "dark", new Color(0.12f, 0.15f, 0.12f));
                    break;
                case "smuggler_camp_evidence":
                    Box(owner.transform, "FuelCan", new Vector3(-0.25f, 0.28f, 0f), new Vector3(0.35f, 0.56f, 0.2f), "fuel", new Color(0.4f, 0.42f, 0.15f));
                    Box(owner.transform, "CanHandle", new Vector3(-0.25f, 0.61f, 0f), new Vector3(0.2f, 0.1f, 0.12f), "dark", new Color(0.12f, 0.15f, 0.12f));
                    Box(owner.transform, "RadioParts", new Vector3(0.22f, 0.1f, 0.12f), new Vector3(0.42f, 0.2f, 0.3f), "dark", new Color(0.12f, 0.15f, 0.12f));
                    Box(owner.transform, "CutRope", new Vector3(0.15f, 0.04f, -0.25f), new Vector3(0.5f, 0.08f, 0.08f), "rope", new Color(0.65f, 0.55f, 0.3f));
                    break;
                default:
                    Box(owner.transform, "MarkedStake", new Vector3(0.25f, 0.45f, 0f), new Vector3(0.09f, 0.9f, 0.09f), "wood", new Color(0.45f, 0.3f, 0.15f));
                    Box(owner.transform, "PaintMarker", new Vector3(0.25f, 0.7f, -0.055f), new Vector3(0.12f, 0.1f, 0.02f), "paint", new Color(0.8f, 0.32f, 0.12f));
                    Box(owner.transform, "WaterproofRouteFragment", new Vector3(-0.15f, 0.03f, 0f), new Vector3(0.5f, 0.025f, 0.4f), "paper", new Color(0.8f, 0.77f, 0.6f));
                    break;
            }
            return runtime;
        }

        private static void Box(Transform parent, string name, Vector3 position, Vector3 size, string materialId, Color color)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = size;
            var collider = piece.GetComponent<Collider>();
            collider.enabled = false;
            if (Application.isPlaying) UnityEngine.Object.Destroy(collider); else UnityEngine.Object.DestroyImmediate(collider);
            if (!materials.TryGetValue(materialId, out Material material) || material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader) { color = color, name = "CluePrototype_" + materialId };
                materials[materialId] = material;
            }
            piece.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
