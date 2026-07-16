using System;
using UnityEngine;
using ApexShift.Runtime.World.Topography;

namespace ApexShift.Runtime.World.Landmarks
{
    public static class LandmarkWorldGenerator
    {
        public static void Generate(Transform parent, IslandTopographyRuntime topography, int seed)
        {
            LandmarkRegistry.ClearForWorldRegeneration();
            ClearExistingLandmarkChildren(parent);
            if (parent == null || topography == null || !topography.IsBuilt || topography.GetGridReadOnly() == null) return;

            CreateLandmarkObject(parent, "old_tree", LandmarkType.OldTree, "Great Old Tree", "A huge ancient tree used as a natural orientation point.", PickCell(topography, c => c.IsLand && c.TerrainType == TerrainType.Forest, new Vector2(-46f, 8f), seed + 11), true);
            CreateLandmarkObject(parent, "ruins", LandmarkType.Ruins, "Overgrown Ruins", "Collapsed stone remains from an older settlement.", PickCell(topography, c => c.IsLand && (c.TerrainType == TerrainType.Hills || c.TerrainType == TerrainType.Ridge), new Vector2(42f, 30f), seed + 23), true);
            CreateLandmarkObject(parent, "pond", LandmarkType.Pond, "Freshwater Pond", "A small pond near the island interior.", PickCell(topography, c => c.IsWater || c.IsBeach, new Vector2(-18f, -36f), seed + 37), true);
            CreateLandmarkObject(parent, "camp", LandmarkType.Camp, "Abandoned Camp", "A small abandoned camp with signs of previous survivors.", PickCell(topography, c => c.IsSafeForPlayerSpawn && c.TerrainType == TerrainType.Plain, new Vector2(22f, -10f), seed + 41), true);
            CreateLandmarkObject(parent, "cave_placeholder", LandmarkType.CavePlaceholder, "Sealed Cave", "A blocked cave entrance prepared for future exploration content.", PickCell(topography, c => c.IsLand && c.TerrainType == TerrainType.Ridge, new Vector2(8f, 54f), seed + 53), true);
        }

        public static LandmarkRuntime CreateLandmarkObject(Transform parent, string id, LandmarkType type, string displayName, string description, Vector3 position, bool discovered)
        {
            GameObject go = new GameObject($"Landmark_{id}");
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = position;
            LandmarkRuntime runtime = go.AddComponent<LandmarkRuntime>();
            runtime.Configure(id, type, displayName, description, discovered);
            BuildVisual(runtime.transform, type);
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

        private static Vector3 PickCell(IslandTopographyRuntime topography, Predicate<TopographyCell> predicate, Vector2 target, int salt)
        {
            TopographyCell[,] grid = topography.GetGridReadOnly();
            int gridSize = topography.GridSize;
            TopographyCell best = null;
            float bestScore = float.PositiveInfinity;
            float noiseBias = Mathf.Abs(Mathf.Sin(salt * 12.9898f) * 43758.5453f) % 1f;
            for (int z = 0; z < gridSize; z++)
            for (int x = 0; x < gridSize; x++)
            {
                TopographyCell cell = grid[x, z];
                if (cell == null || predicate == null || !predicate(cell)) continue;
                float dx = cell.WorldCenter.x - target.x;
                float dz = cell.WorldCenter.z - target.y;
                float score = dx * dx + dz * dz + Mathf.Abs(Mathf.Sin((x + 1) * 7.13f + (z + 1) * 3.91f + noiseBias)) * 4f;
                if (score < bestScore) { best = cell; bestScore = score; }
            }
            return (best != null ? best.WorldCenter : topography.GetSafePlayerSpawnPoint()) + Vector3.up * 0.08f;
        }

        private static void BuildVisual(Transform root, LandmarkType type)
        {
            switch (type)
            {
                case LandmarkType.OldTree:
                    AddCylinder(root, "Trunk", new Vector3(0f, 1.2f, 0f), new Vector3(0.55f, 1.2f, 0.55f), new Color(0.30f, 0.18f, 0.09f));
                    AddSphere(root, "Canopy", new Vector3(0f, 2.35f, 0f), new Vector3(2.0f, 1.3f, 2.0f), new Color(0.10f, 0.38f, 0.13f));
                    break;
                case LandmarkType.Ruins:
                    AddCube(root, "StoneA", new Vector3(-0.65f, 0.35f, 0f), new Vector3(0.45f, 0.7f, 1.2f), new Color(0.46f, 0.45f, 0.40f));
                    AddCube(root, "StoneB", new Vector3(0.48f, 0.55f, 0.18f), new Vector3(0.40f, 1.1f, 0.35f), new Color(0.40f, 0.39f, 0.35f));
                    AddCube(root, "StoneSlab", new Vector3(0f, 0.95f, 0f), new Vector3(1.5f, 0.22f, 0.38f), new Color(0.36f, 0.35f, 0.32f));
                    break;
                case LandmarkType.Pond:
                    AddCylinder(root, "PondBank", new Vector3(0f, 0.01f, 0f), new Vector3(2.15f, 0.03f, 2.15f), new Color(0.32f, 0.26f, 0.15f));
                    AddCylinder(root, "PondWater", new Vector3(0f, 0.03f, 0f), new Vector3(1.8f, 0.04f, 1.8f), new Color(0.10f, 0.35f, 0.62f, 0.72f));
                    break;
                case LandmarkType.Camp:
                    AddCube(root, "CampLogA", new Vector3(-0.35f, 0.12f, 0f), Quaternion.Euler(0f, 25f, 0f), new Vector3(0.18f, 0.18f, 1.15f), new Color(0.38f, 0.22f, 0.11f));
                    AddCube(root, "CampLogB", new Vector3(0.35f, 0.12f, 0f), Quaternion.Euler(0f, -25f, 0f), new Vector3(0.18f, 0.18f, 1.15f), new Color(0.38f, 0.22f, 0.11f));
                    AddSphere(root, "AshPit", new Vector3(0f, 0.08f, 0f), new Vector3(0.55f, 0.12f, 0.55f), new Color(0.12f, 0.12f, 0.11f));
                    break;
                case LandmarkType.CavePlaceholder:
                    AddCube(root, "RockWall", new Vector3(0f, 0.85f, 0.12f), new Vector3(1.6f, 1.7f, 0.38f), new Color(0.28f, 0.27f, 0.25f));
                    AddCube(root, "BlockedEntrance", new Vector3(0f, 0.55f, -0.12f), new Vector3(0.8f, 0.9f, 0.28f), new Color(0.10f, 0.09f, 0.08f));
                    break;
                default:
                    AddSphere(root, "Marker", new Vector3(0f, 0.45f, 0f), Vector3.one * 0.6f, new Color(0.8f, 0.8f, 0.75f));
                    break;
            }
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
