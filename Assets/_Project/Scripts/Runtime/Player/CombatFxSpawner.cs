using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public static class CombatFxSpawner
    {
        public static void SpawnHitBurst(Vector3 position, Color color, float scale = 0.18f, float lifetime = 0.25f)
        {
            GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "CombatHitFx";
            fx.transform.position = position;
            fx.transform.localScale = Vector3.one * Mathf.Max(0.05f, scale);

            Collider collider = fx.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            Renderer renderer = fx.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (material.shader == null)
                {
                    material.shader = Shader.Find("Standard");
                }

                Color tint = new Color(color.r, color.g, color.b, 0.9f);
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", tint);
                }
                else
                {
                    material.color = tint;
                }

                renderer.sharedMaterial = material;
            }

            Object.Destroy(fx, Mathf.Max(0.05f, lifetime));
        }

        public static void SpawnMuzzleFlash(Vector3 position, Vector3 forward)
        {
            GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fx.name = "CombatMuzzleFx";
            fx.transform.position = position;
            fx.transform.rotation = Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward, Vector3.up);
            fx.transform.localScale = new Vector3(0.05f, 0.2f, 0.05f);

            Collider collider = fx.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            Renderer renderer = fx.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (material.shader == null)
                {
                    material.shader = Shader.Find("Standard");
                }

                Color tint = new Color(1f, 0.8f, 0.35f, 0.85f);
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", tint);
                }
                else
                {
                    material.color = tint;
                }

                renderer.sharedMaterial = material;
            }

            Object.Destroy(fx, 0.12f);
        }

        public static void SpawnSlashArc(Vector3 origin, Vector3 forward, float range, bool spear, float lifetime = 0.16f)
        {
            Vector3 direction = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            float safeRange = Mathf.Max(0.35f, range);

            GameObject root = new GameObject(spear ? "SpearSlashFx" : "AttackSlashFx");
            root.transform.position = origin;
            root.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            GameObject slash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slash.name = "SlashBar";
            slash.transform.SetParent(root.transform, false);
            slash.transform.localPosition = Vector3.forward * safeRange * 0.5f;
            slash.transform.localRotation = Quaternion.Euler(0f, 0f, spear ? 0f : 16f);
            slash.transform.localScale = spear
                ? new Vector3(0.10f, 0.08f, safeRange)
                : new Vector3(0.35f, 0.08f, safeRange * 0.55f);

            Collider collider = slash.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            Renderer renderer = slash.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (material.shader == null)
                {
                    material.shader = Shader.Find("Standard");
                }

                Color tint = spear ? new Color(1f, 0.78f, 0.30f, 0.95f) : new Color(0.95f, 0.95f, 0.68f, 0.88f);
                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", tint);
                }
                else
                {
                    material.color = tint;
                }

                renderer.sharedMaterial = material;
            }

            Object.Destroy(root, Mathf.Max(0.05f, lifetime));
        }
    }
}
