using ApexShift.Core.Items;
using UnityEngine;

namespace ApexShift.Runtime.Items
{
    public static class ItemPickupSpawner
    {
        public static GameObject Spawn(string itemId, int amount, Vector3 position, Quaternion rotation)
        {
            string normalizedItemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
            GameObject go = new GameObject($"Item_{normalizedItemId}");
            go.name = $"Item_{normalizedItemId}";
            go.transform.SetPositionAndRotation(position, rotation);
            bool usedAuthoredModel = BuildPickupVisual(go.transform, normalizedItemId);

            SphereCollider collider = go.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.38f;
            collider.center = new Vector3(0f, 0.18f, 0f);

            ItemPickupView pickup = go.GetComponent<ItemPickupView>() ?? go.AddComponent<ItemPickupView>();
            pickup.Configure(normalizedItemId, amount);

            if (!usedAuthoredModel)
            {
                GameObject iconGo = new GameObject("Icon", typeof(SpriteRenderer));
                iconGo.transform.SetParent(go.transform, false);
                iconGo.transform.localPosition = new Vector3(0f, 0.22f, 0.01f);
                iconGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                iconGo.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);

                SpriteRenderer spriteRenderer = iconGo.GetComponent<SpriteRenderer>();
                spriteRenderer.sprite = LoadPickupIcon(normalizedItemId);
                spriteRenderer.color = Color.white;
                spriteRenderer.sortingOrder = 10;
                spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                spriteRenderer.receiveShadows = false;
            }

            return go;
        }

        private static bool BuildPickupVisual(Transform root, string itemId)
        {
            if (TryAddAuthoredItemModel(root, itemId)) return true;
            if (TryAddCraftingModel(root, itemId))
            {
                return true;
            }

            if (itemId == "axe" || itemId == "pickaxe" || itemId == "arrow")
            {
                Debug.LogError($"[ItemPickup] Missing crafting model for '{itemId}'. Procedural fallback disabled.");
                return false;
            }

            switch (itemId)
            {
                case "spear":
                    AddPart(root, "Shaft", PrimitiveType.Cylinder, new Vector3(0f, 0.10f, 0f), Quaternion.Euler(90f, 0f, 28f), new Vector3(0.03f, 0.42f, 0.03f), new Color(0.50f, 0.33f, 0.17f));
                    AddPart(root, "Tip", PrimitiveType.Cube, new Vector3(0.18f, 0.18f, 0f), Quaternion.Euler(0f, 0f, 45f), new Vector3(0.10f, 0.10f, 0.18f), new Color(0.74f, 0.78f, 0.80f));
                    break;
                case "bow":
                    AddPart(root, "Grip", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0f), Quaternion.identity, new Vector3(0.08f, 0.18f, 0.10f), new Color(0.36f, 0.22f, 0.12f));
                    AddPart(root, "UpperLimb", PrimitiveType.Cylinder, new Vector3(-0.08f, 0.28f, 0f), Quaternion.Euler(0f, 0f, 30f), new Vector3(0.03f, 0.24f, 0.03f), new Color(0.48f, 0.28f, 0.14f));
                    AddPart(root, "LowerLimb", PrimitiveType.Cylinder, new Vector3(-0.08f, 0.00f, 0f), Quaternion.Euler(0f, 0f, -30f), new Vector3(0.03f, 0.24f, 0.03f), new Color(0.48f, 0.28f, 0.14f));
                    AddPart(root, "String", PrimitiveType.Cylinder, new Vector3(0.06f, 0.14f, 0f), Quaternion.identity, new Vector3(0.008f, 0.26f, 0.008f), new Color(0.86f, 0.82f, 0.70f));
                    break;
                case "torch":
                    AddPart(root, "Handle", PrimitiveType.Cylinder, new Vector3(0f, 0.10f, 0f), Quaternion.Euler(90f, 0f, 18f), new Vector3(0.03f, 0.24f, 0.03f), new Color(0.34f, 0.20f, 0.10f));
                    AddPart(root, "Head", PrimitiveType.Cube, new Vector3(0.10f, 0.22f, 0f), Quaternion.identity, new Vector3(0.10f, 0.12f, 0.10f), new Color(0.44f, 0.24f, 0.12f));
                    AddPart(root, "Flame", PrimitiveType.Sphere, new Vector3(0.14f, 0.34f, 0f), Quaternion.identity, new Vector3(0.10f, 0.14f, 0.10f), new Color(1f, 0.56f, 0.14f));
                    break;
                default:
                    AddPart(root, "Body", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), Quaternion.identity, new Vector3(0.34f, 0.12f, 0.34f), GetColor(itemId));
                    break;
            }

            return false;
        }

        private static bool TryAddAuthoredItemModel(Transform root, string itemId)
        {
            if (itemId == "torch")
            {
                return false;
            }

            if (!ItemModelResolver.TryInstantiateItemModel(itemId, root, out GameObject model))
            {
                return false;
            }

            return ItemModelResolver.NormalizeModelToBounds(
                model,
                root,
                0.72f,
                new Vector3(0f, 0.20f, 0f),
                Quaternion.identity);
        }

        private static bool TryAddCraftingModel(Transform root, string itemId)
        {
            if (itemId != "axe" && itemId != "pickaxe" && itemId != "arrow") return false;

            GameObject prefab = UnityEngine.Resources.Load<GameObject>($"Crafting/Models/craft_{itemId}");
            if (prefab == null)
            {
                return false;
            }

            GameObject model = Object.Instantiate(prefab, root, false);
            model.name = $"CraftModel_{itemId}";
            foreach (Collider modelCollider in model.GetComponentsInChildren<Collider>(true)) Object.Destroy(modelCollider);

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Object.Destroy(model);
                return false;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float scale = 0.72f / Mathf.Max(0.001f, bounds.size.y);
            model.transform.localScale = Vector3.one * scale;
            model.transform.localRotation = Quaternion.Euler(0f, 0f, -70f);
            model.transform.localPosition = new Vector3(0f, 0.08f - bounds.min.y * scale, 0f);
            return true;
        }

        private static GameObject AddPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;

            Collider partCollider = go.GetComponent<Collider>();
            if (partCollider != null)
            {
                Object.Destroy(partCollider);
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null)
                {
                    mat.shader = Shader.Find("Standard");
                }

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color);
                }
                else
                {
                    mat.color = color;
                }

                renderer.sharedMaterial = mat;
            }

            return go;
        }

        private static Sprite LoadPickupIcon(string itemId)
        {
            string normalized = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
            string path = normalized switch
            {
                "wood" => "ApexShift2D/Art/Icons/Resources/resource_wood_log",
                "stone" => "ApexShift2D/Art/Icons/Resources/resource_stone",
                "fiber" => "ApexShift2D/Art/Icons/Resources/resource_fiber",
                "meat" => "ApexShift2D/Art/Icons/Resources/resource_raw_meat",
                "hide" => "ApexShift2D/Art/Icons/Resources/resource_hide",
                "bone" => "ApexShift2D/Art/Icons/Resources/resource_bone",
                "berries" => "ApexShift2D/Art/Icons/Resources/resource_berries",
                "grass" => "ApexShift2D/Art/Icons/Resources/resource_leaf",
                "torch" => "ApexShift2D/Art/Icons/Items/item_torch",
                "storage_box" => "ApexShift2D/Art/Icons/Items/item_storage_box",
                "campfire" => "ApexShift2D/Art/Icons/Items/item_campfire",
                "bow" => "ApexShift2D/Art/Icons/Tools/tool_bow",
                "spear" => "ApexShift2D/Art/Icons/Tools/tool_spear",
                _ => $"ApexShift2D/Art/Icons/Items/item_{normalized}",
            };

            return UnityEngine.Resources.Load<Sprite>(path) ?? UnityEngine.Resources.Load<Sprite>("ApexShift2D/Art/Icons/Items/item_unknown");
        }

        private static Color GetColor(string itemId)
        {
            switch (itemId)
            {
                case "wood": return new Color(0.58f, 0.36f, 0.18f);
                case "stone": return new Color(0.55f, 0.55f, 0.58f);
                case "fiber": return new Color(0.20f, 0.52f, 0.20f);
                case "meat": return new Color(0.62f, 0.16f, 0.14f);
                case "hide": return new Color(0.65f, 0.54f, 0.32f);
                case "bone": return new Color(0.92f, 0.92f, 0.88f);
                case "berries": return new Color(0.58f, 0.16f, 0.42f);
                case "grass": return new Color(0.32f, 0.62f, 0.22f);
                case "torch": return new Color(0.84f, 0.60f, 0.18f);
                case "storage_box": return new Color(0.36f, 0.22f, 0.12f);
                default: return new Color(0.72f, 0.72f, 0.72f);
            }
        }
    }
}
