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
            AddBox(root, "ChestBase", new Vector3(0f, 0.10f, 0f), new Vector3(2.20f, 0.20f, 1.18f), WoodDark());
            AddBox(root, "ChestBody", new Vector3(0f, 0.52f, 0f), new Vector3(2.05f, 0.78f, 1.00f), Wood());
            AddBox(root, "ChestLid", new Vector3(0f, 1.02f, 0f), new Vector3(2.24f, 0.18f, 1.14f), WoodLight());
            AddBox(root, "ChestLidBack", new Vector3(0f, 1.08f, 0.36f), new Vector3(2.10f, 0.12f, 0.18f), Wood());
            AddBox(root, "MetalStrapA", new Vector3(-0.64f, 0.96f, 0f), new Vector3(0.14f, 0.58f, 1.10f), Metal());
            AddBox(root, "MetalStrapB", new Vector3(0.64f, 0.96f, 0f), new Vector3(0.14f, 0.58f, 1.10f), Metal());
            AddBox(root, "MetalBandFront", new Vector3(0f, 0.54f, -0.52f), new Vector3(1.78f, 0.16f, 0.08f), Metal());
            AddBox(root, "Latch", new Vector3(0f, 0.52f, -0.58f), new Vector3(0.28f, 0.30f, 0.12f), Metal());
            AddBox(root, "FootA", new Vector3(-0.84f, 0.05f, -0.36f), new Vector3(0.18f, 0.10f, 0.18f), WoodDark());
            AddBox(root, "FootB", new Vector3(0.84f, 0.05f, -0.36f), new Vector3(0.18f, 0.10f, 0.18f), WoodDark());
            AddBox(root, "FootC", new Vector3(-0.84f, 0.05f, 0.36f), new Vector3(0.18f, 0.10f, 0.18f), WoodDark());
            AddBox(root, "FootD", new Vector3(0.84f, 0.05f, 0.36f), new Vector3(0.18f, 0.10f, 0.18f), WoodDark());
        }

        private static void BuildCampfire(Transform root)
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = Mathf.PI * 2f * i / 10f;
                float radius = i % 2 == 0 ? 0.76f : 0.66f;
                AddBox(root, "Stone", new Vector3(Mathf.Cos(angle) * radius, 0.11f, Mathf.Sin(angle) * radius), new Vector3(0.30f, 0.22f, 0.24f), Stone(), Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f));
            }

            AddBox(root, "AshBed", new Vector3(0f, 0.05f, 0f), new Vector3(0.86f, 0.06f, 0.86f), Stone());
            AddBox(root, "Embers", new Vector3(0f, 0.11f, 0f), new Vector3(0.58f, 0.10f, 0.58f), Ember());
            for (int i = 0; i < 4; i++)
            {
                float angle = 45f + (i * 45f);
                AddBox(root, "Log", new Vector3(0f, 0.24f, 0f), new Vector3(0.18f, 0.18f, 1.00f), Wood(), Quaternion.Euler(18f, angle, 0f));
            }

            AddBox(root, "FlameCore", new Vector3(0f, 0.54f, 0f), new Vector3(0.34f, 0.58f, 0.34f), Flame());
            AddBox(root, "FlameSideA", new Vector3(-0.12f, 0.42f, 0.06f), new Vector3(0.16f, 0.34f, 0.16f), Flame(), Quaternion.Euler(0f, 0f, -16f));
            AddBox(root, "FlameSideB", new Vector3(0.12f, 0.40f, -0.08f), new Vector3(0.16f, 0.30f, 0.16f), Flame(), Quaternion.Euler(0f, 0f, 14f));
        }

        private static void BuildWall(Transform root)
        {
            for (int i = 0; i < 9; i++)
            {
                float x = (i - 4f) * 0.34f;
                float height = 1.28f + (Mathf.Abs(i - 4f) * 0.03f);
                AddBox(root, "Stake", new Vector3(x, height * 0.5f, 0f), new Vector3(0.22f, height, 0.22f), Wood());
                AddBox(root, "StakeTip", new Vector3(x, height + 0.12f, 0f), new Vector3(0.16f, 0.22f, 0.16f), WoodLight(), Quaternion.Euler(0f, 0f, 45f));
            }

            AddBox(root, "TopBraceFront", new Vector3(0f, 1.04f, -0.18f), new Vector3(3.08f, 0.14f, 0.14f), WoodDark());
            AddBox(root, "BottomBraceFront", new Vector3(0f, 0.44f, -0.18f), new Vector3(3.08f, 0.14f, 0.14f), WoodDark());
            AddBox(root, "TopBraceBack", new Vector3(0f, 1.00f, 0.18f), new Vector3(2.88f, 0.12f, 0.12f), WoodDark());
            AddBox(root, "BottomBraceBack", new Vector3(0f, 0.38f, 0.18f), new Vector3(2.88f, 0.12f, 0.12f), WoodDark());
            AddBox(root, "DiagonalBraceA", new Vector3(-0.24f, 0.74f, -0.26f), new Vector3(1.78f, 0.12f, 0.12f), Wood(), Quaternion.Euler(0f, 0f, 26f));
            AddBox(root, "DiagonalBraceB", new Vector3(0.24f, 0.74f, -0.26f), new Vector3(1.78f, 0.12f, 0.12f), Wood(), Quaternion.Euler(0f, 0f, -26f));
        }

        private static void BuildTrap(Transform root)
        {
            AddBox(root, "FrameA", new Vector3(0f, 0.12f, -0.72f), new Vector3(2.2f, 0.20f, 0.18f), WoodDark());
            AddBox(root, "FrameB", new Vector3(0f, 0.12f, 0.72f), new Vector3(2.2f, 0.20f, 0.18f), WoodDark());
            AddBox(root, "FrameC", new Vector3(-1.02f, 0.12f, 0f), new Vector3(0.18f, 0.20f, 1.62f), WoodDark());
            AddBox(root, "FrameD", new Vector3(1.02f, 0.12f, 0f), new Vector3(0.18f, 0.20f, 1.62f), WoodDark());
            AddBox(root, "Deck", new Vector3(0f, 0.20f, 0f), new Vector3(1.72f, 0.08f, 1.20f), Wood());
            for (int x = -2; x <= 2; x++)
            {
                AddBox(root, "SpikeRowFront", new Vector3(x * 0.28f, 0.56f, -0.18f), new Vector3(0.12f, 0.72f, 0.12f), WoodLight());
                AddBox(root, "SpikeRowBack", new Vector3(x * 0.28f, 0.56f, 0.18f), new Vector3(0.12f, 0.72f, 0.12f), WoodLight());
            }

            AddBox(root, "TriggerBar", new Vector3(0f, 0.34f, 0f), new Vector3(1.46f, 0.08f, 0.08f), Metal());
        }

        private static void BuildTent(Transform root)
        {
            AddBox(root, "LeftCanvas", new Vector3(-0.56f, 0.82f, 0f), new Vector3(0.12f, 1.30f, 1.72f), Cloth(), Quaternion.Euler(0f, 0f, -34f));
            AddBox(root, "RightCanvas", new Vector3(0.56f, 0.82f, 0f), new Vector3(0.12f, 1.30f, 1.72f), Cloth(), Quaternion.Euler(0f, 0f, 34f));
            AddBox(root, "BackCanvas", new Vector3(0f, 0.64f, 0.90f), new Vector3(1.36f, 0.92f, 0.12f), ClothDark());
            AddBox(root, "GroundSheet", new Vector3(0f, 0.04f, 0f), new Vector3(1.82f, 0.08f, 1.62f), ClothDark());
            AddBox(root, "RidgePole", new Vector3(0f, 1.34f, 0f), new Vector3(0.14f, 0.14f, 1.96f), WoodDark());
            AddBox(root, "FrontPoleLeft", new Vector3(-0.52f, 0.74f, -0.88f), new Vector3(0.12f, 1.56f, 0.12f), WoodDark(), Quaternion.Euler(0f, 0f, -30f));
            AddBox(root, "FrontPoleRight", new Vector3(0.52f, 0.74f, -0.88f), new Vector3(0.12f, 1.56f, 0.12f), WoodDark(), Quaternion.Euler(0f, 0f, 30f));
            AddBox(root, "RearPoleLeft", new Vector3(-0.52f, 0.74f, 0.88f), new Vector3(0.12f, 1.56f, 0.12f), WoodDark(), Quaternion.Euler(0f, 0f, -30f));
            AddBox(root, "RearPoleRight", new Vector3(0.52f, 0.74f, 0.88f), new Vector3(0.12f, 1.56f, 0.12f), WoodDark(), Quaternion.Euler(0f, 0f, 30f));
            AddBox(root, "DoorFlap", new Vector3(0f, 0.58f, -0.94f), new Vector3(0.72f, 0.92f, 0.08f), ClothLight());
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
        private static Material ClothDark() => CreateMaterial("building_cloth_dark", new Color(0.46f, 0.31f, 0.18f));
        private static Material ClothLight() => CreateMaterial("building_cloth_light", new Color(0.83f, 0.58f, 0.42f));

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
