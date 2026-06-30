using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    public static class PlaceableFallbackFactory
    {
        private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();

        public static GameObject CreateFallback(string buildingId, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            string id = Normalize(buildingId);
            GameObject root = new GameObject($"Building_{id}_Fallback");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);

            switch (id)
            {
                case "storage_box": BuildStorageBox(root.transform); break;
                case "campfire": BuildCampfire(root.transform); break;
                case "wall": BuildWall(root.transform); break;
                case "trap": BuildTrap(root.transform); break;
                case "tent": BuildTent(root.transform); break;
                default: AddBox(root.transform, "FallbackBlock", Vector3.up * 0.5f, new Vector3(1f, 1f, 1f), Wood()); break;
            }

            PlaceableStructureRuntime structure = root.AddComponent<PlaceableStructureRuntime>();
            structure.Configure(id, null, GetDefaultFootprint(id));
            return root;
        }

        public static Vector3 GetDefaultFootprint(string buildingId)
        {
            switch (Normalize(buildingId))
            {
                case "storage_box": return new Vector3(2.4f, 1.4f, 1.4f);
                case "campfire": return new Vector3(2.0f, 1.2f, 2.0f);
                case "wall": return new Vector3(3.2f, 2.2f, 0.6f);
                case "trap": return new Vector3(2.4f, 1.1f, 2.0f);
                case "tent": return new Vector3(3.0f, 2.0f, 2.4f);
                default: return new Vector3(1.5f, 1f, 1.5f);
            }
        }

        private static void BuildStorageBox(Transform root)
        {
            AddBox(root, "ChestBody", new Vector3(0f, 0.45f, 0f), new Vector3(2.2f, 0.8f, 1.1f), Wood());
            AddBox(root, "ChestLid", new Vector3(0f, 0.95f, 0f), new Vector3(2.35f, 0.28f, 1.18f), WoodLight());
            AddBox(root, "MetalStrapA", new Vector3(-0.75f, 1.12f, 0f), new Vector3(0.14f, 0.12f, 1.3f), Metal());
            AddBox(root, "MetalStrapB", new Vector3(0.75f, 1.12f, 0f), new Vector3(0.14f, 0.12f, 1.3f), Metal());
            AddBox(root, "Latch", new Vector3(0f, 0.55f, -0.58f), new Vector3(0.36f, 0.32f, 0.08f), Metal());
        }

        private static void BuildCampfire(Transform root)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = Mathf.PI * 2f * i / 8f;
                AddBox(root, "Stone", new Vector3(Mathf.Cos(angle) * 0.78f, 0.12f, Mathf.Sin(angle) * 0.78f), new Vector3(0.42f, 0.24f, 0.32f), Stone(), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
            }

            AddBox(root, "Embers", new Vector3(0f, 0.11f, 0f), new Vector3(0.75f, 0.08f, 0.75f), Ember());
            for (int i = 0; i < 5; i++)
            {
                float angle = Mathf.PI * 2f * i / 5f;
                AddBox(root, "Log", new Vector3(Mathf.Cos(angle) * 0.18f, 0.33f, Mathf.Sin(angle) * 0.18f), new Vector3(0.22f, 0.22f, 1.05f), Wood(), Quaternion.Euler(28f, angle * Mathf.Rad2Deg, 0f));
            }

            AddBox(root, "Flame", new Vector3(0f, 0.72f, 0f), new Vector3(0.55f, 0.9f, 0.55f), Flame());
        }

        private static void BuildWall(Transform root)
        {
            for (int i = 0; i < 8; i++)
            {
                float x = (i - 3.5f) * 0.34f;
                AddBox(root, "Stake", new Vector3(x, 0.75f, 0f), new Vector3(0.25f, 1.5f, 0.25f), Wood());
                AddBox(root, "StakeTip", new Vector3(x, 1.58f, 0f), new Vector3(0.20f, 0.28f, 0.20f), WoodLight());
            }

            AddBox(root, "TopBrace", new Vector3(0f, 1.10f, -0.16f), new Vector3(2.9f, 0.20f, 0.18f), WoodDark());
            AddBox(root, "BottomBrace", new Vector3(0f, 0.42f, -0.16f), new Vector3(2.9f, 0.20f, 0.18f), WoodDark());
            AddBox(root, "DiagonalBrace", new Vector3(0f, 0.74f, -0.24f), new Vector3(2.9f, 0.20f, 0.16f), Wood(), Quaternion.Euler(0f, 0f, 21f));
        }

        private static void BuildTrap(Transform root)
        {
            AddBox(root, "FrameA", new Vector3(0f, 0.15f, -0.75f), new Vector3(2.2f, 0.28f, 0.22f), Wood());
            AddBox(root, "FrameB", new Vector3(0f, 0.15f, 0.75f), new Vector3(2.2f, 0.28f, 0.22f), Wood());
            AddBox(root, "FrameC", new Vector3(-1.05f, 0.15f, 0f), new Vector3(0.22f, 0.28f, 1.7f), Wood());
            AddBox(root, "FrameD", new Vector3(1.05f, 0.15f, 0f), new Vector3(0.22f, 0.28f, 1.7f), Wood());
            for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
            {
                AddBox(root, "Spike", new Vector3(x * 0.45f, 0.58f, z * 0.35f), new Vector3(0.16f, 0.9f, 0.16f), WoodLight());
            }
        }

        private static void BuildTent(Transform root)
        {
            AddBox(root, "Cloth", new Vector3(0f, 0.72f, 0f), new Vector3(2.2f, 1.35f, 1.6f), Cloth(), Quaternion.Euler(0f, 0f, 0f));
            AddBox(root, "RidgePole", new Vector3(0f, 1.45f, 0f), new Vector3(0.14f, 0.14f, 1.9f), WoodDark());
            AddBox(root, "LeftPole", new Vector3(-0.72f, 0.75f, -0.85f), new Vector3(0.14f, 1.7f, 0.14f), WoodDark(), Quaternion.Euler(0f, 0f, -28f));
            AddBox(root, "RightPole", new Vector3(0.72f, 0.75f, -0.85f), new Vector3(0.14f, 1.7f, 0.14f), WoodDark(), Quaternion.Euler(0f, 0f, 28f));
        }

        private static GameObject AddBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Quaternion? localRotation = null)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = localRotation ?? Quaternion.identity;
            cube.transform.localScale = localScale;
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            return cube;
        }

        private static Material Wood() => CreateMaterial("building_wood", new Color(0.52f, 0.31f, 0.14f));
        private static Material WoodLight() => CreateMaterial("building_wood_light", new Color(0.73f, 0.48f, 0.24f));
        private static Material WoodDark() => CreateMaterial("building_wood_dark", new Color(0.32f, 0.18f, 0.08f));
        private static Material Metal() => CreateMaterial("building_metal", new Color(0.28f, 0.29f, 0.29f));
        private static Material Stone() => CreateMaterial("building_stone", new Color(0.36f, 0.36f, 0.35f));
        private static Material Ember() => CreateMaterial("building_ember", new Color(0.9f, 0.18f, 0.02f));
        private static Material Flame() => CreateMaterial("building_flame", new Color(1f, 0.46f, 0.02f));
        private static Material Cloth() => CreateMaterial("building_cloth", new Color(0.62f, 0.46f, 0.26f));

        private static Material CreateMaterial(string name, Color color)
        {
            if (materialCache.TryGetValue(name, out Material cached) && cached != null)
            {
                return cached;
            }

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (material.shader == null)
            {
                material.shader = Shader.Find("Standard");
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            materialCache[name] = material;
            return material;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim().ToLowerInvariant();
        }
    }
}
