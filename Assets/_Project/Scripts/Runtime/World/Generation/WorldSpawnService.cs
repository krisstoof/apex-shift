using UnityEngine;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Biomes;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>Centralizes instantiation rules for generated world objects.</summary>
    public sealed class WorldSpawnService
    {
        public GameObject Spawn(GameObject prefab, Vector3 position, float yaw, Transform parent)
        {
            if (prefab == null) return null;
            Quaternion rotation = WorldSpawnRotation.ComposeYawWithPrefabRotation(prefab, yaw);
            return Object.Instantiate(prefab, position, rotation, parent);
        }

        public GameObject SpawnResource(GameObject prefab, Vector3 position, float yaw, Transform parent, VegetationSpawnKind kind)
        {
            if (prefab != null) return Spawn(prefab, position, yaw, parent);
            GameObject fallback = CreateResourceFallback(kind, position);
            fallback.transform.SetParent(parent, true);
            fallback.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return fallback;
        }

        public GameObject SpawnCreature(GameObject prefab, string creatureId, Vector3 position, float yaw, Transform parent)
        {
            if (prefab != null) return Spawn(prefab, position, yaw, parent);
            GameObject fallback = CreateCreatureFallback(creatureId, position);
            fallback.transform.SetParent(parent, true);
            fallback.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return fallback;
        }

        private static GameObject CreateResourceFallback(VegetationSpawnKind kind, Vector3 position)
        {
            PrimitiveType primitive = PrimitiveType.Sphere;
            Vector3 scale = Vector3.one * 0.5f;
            Vector3 offset = Vector3.up * 0.25f;
            Color color = new Color(0.35f, 0.65f, 0.22f);
            switch (kind)
            {
                case VegetationSpawnKind.ConiferTree:
                    primitive = PrimitiveType.Cylinder; scale = new Vector3(0.5f, 1f, 0.5f); offset = Vector3.up; color = new Color(0.06f, 0.24f, 0.10f); break;
                case VegetationSpawnKind.LeafyTree:
                    primitive = PrimitiveType.Cylinder; scale = new Vector3(0.5f, 1f, 0.5f); offset = Vector3.up; color = new Color(0.16f, 0.46f, 0.16f); break;
                case VegetationSpawnKind.DryTree:
                    primitive = PrimitiveType.Cylinder; scale = new Vector3(0.5f, 1f, 0.5f); offset = Vector3.up; color = new Color(0.52f, 0.34f, 0.16f); break;
                case VegetationSpawnKind.Rock:
                    primitive = PrimitiveType.Cube; scale = Vector3.one; offset = Vector3.up * 0.5f; color = new Color(0.45f, 0.45f, 0.42f); break;
                case VegetationSpawnKind.BerryBush:
                    scale = Vector3.one * 0.6f; color = new Color(0.20f, 0.50f, 0.20f); break;
                case VegetationSpawnKind.DryBush:
                    scale = Vector3.one * 0.5f; color = new Color(0.48f, 0.33f, 0.14f); break;
            }
            GameObject root = new GameObject($"{kind}_Fallback");
            root.transform.position = position;
            GameObject visual = GameObject.CreatePrimitive(primitive);
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = offset;
            visual.transform.localScale = scale;
            ApplyMaterial(visual, color);
            RemoveCollider(visual);
            return root;
        }

        private static GameObject CreateCreatureFallback(string creatureId, Vector3 position)
        {
            PrimitiveType primitive = creatureId == "small_prey" ? PrimitiveType.Sphere : PrimitiveType.Capsule;
            Color color = creatureId == "varnak" ? Color.red : creatureId == "grazer" ? new Color(0.6f, 0.4f, 0.2f) : new Color(0.9f, 0.9f, 0.9f);
            Vector3 scale = creatureId == "small_prey" ? Vector3.one * 0.5f : creatureId == "grazer" ? new Vector3(0.8f, 0.8f, 0.8f) : new Vector3(1f, 1.2f, 1f);
            GameObject root = new GameObject($"Creature_{creatureId}_Fallback");
            root.transform.position = position;
            GameObject visual = GameObject.CreatePrimitive(primitive);
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.up * (scale.y * 0.5f);
            visual.transform.localScale = scale;
            ApplyMaterial(visual, color);
            RemoveCollider(visual);
            return root;
        }

        private static void RemoveCollider(GameObject instance)
        {
            Collider collider = instance.GetComponent<Collider>();
            if (collider == null) return;
            if (Application.isPlaying) Object.Destroy(collider);
            else Object.DestroyImmediate(collider);
        }

        private static void ApplyMaterial(GameObject instance, Color color)
        {
            Renderer renderer = instance.GetComponent<Renderer>();
            if (renderer == null) return;
            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (material.shader == null) material.shader = Shader.Find("Standard");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            renderer.sharedMaterial = material;
        }
    }
}
