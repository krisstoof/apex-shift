using ApexShift.Core.Items;
using UnityEngine;

namespace ApexShift.Runtime.Items
{
    public static class ItemPickupSpawner
    {
        public static GameObject Spawn(string itemId, int amount, Vector3 position, Quaternion rotation)
        {
            string normalizedItemId = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Item_{normalizedItemId}";
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = new Vector3(0.34f, 0.12f, 0.34f);

            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            ItemPickupView pickup = go.GetComponent<ItemPickupView>() ?? go.AddComponent<ItemPickupView>();
            pickup.Configure(normalizedItemId, amount);

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (mat.shader == null)
                {
                    mat.shader = Shader.Find("Standard");
                }

                Color color = GetColor(normalizedItemId);
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
