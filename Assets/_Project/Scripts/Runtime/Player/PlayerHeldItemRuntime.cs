using System;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerHeldItemRuntime : MonoBehaviour
    {
        [SerializeField] private ActionBarRuntime actionBarRuntime;
        [SerializeField] private PlayerInventoryRuntime inventoryRuntime;
        [SerializeField] private Transform handAnchor;
        [SerializeField] private bool useExplicitHandBoneOnly = true;
        [SerializeField] private Vector3 fallbackLocalPosition = new Vector3(0.62f, 1.22f, 0.70f);
        [SerializeField] private Vector3 fallbackLocalEuler = new Vector3(8f, 32f, -12f);
        [SerializeField] private bool requireItemInInventory = false;
        [SerializeField] private bool logEquips;
        [SerializeField] private float heldVisualScale = 1.6f;
        [SerializeField] private float torchFlickerSpeed = 13f;
        [SerializeField] private float torchFlickerAmount = 0.22f;
        [SerializeField] private float torchFlickerRangeAmount = 0.35f;

        private GameObject heldRoot;
        private Transform fallbackAnchor;
        private Light torchLight;
        private float torchBaseIntensity;
        private float torchBaseRange;
        private string currentItemId = string.Empty;
        private int lastSlotIndex = -2;

        private void Awake()
        {
            ResolveReferences();
            EnsureHeldRoot();
            RefreshHeldItem(force: true);
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshHeldItem(force: true);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Update()
        {
            ResolveReferences();
            UpdateTorchFlicker();
            RefreshHeldItem(force: false);
        }

        public void SetActionBarRuntime(ActionBarRuntime runtime)
        {
            if (actionBarRuntime == runtime)
            {
                return;
            }

            Unsubscribe();
            actionBarRuntime = runtime;
            Subscribe();
            RefreshHeldItem(force: true);
        }

        public void SetInventoryRuntime(PlayerInventoryRuntime runtime)
        {
            inventoryRuntime = runtime;
            RefreshHeldItem(force: true);
        }

        public void ForceEquipActionItem(string itemId)
        {
            ResolveReferences();
            EnsureHeldRoot();

            string normalized = Normalize(itemId);
            if (!ActionBarRuntime.IsActionBarItem(normalized))
            {
                normalized = string.Empty;
            }

            lastSlotIndex = actionBarRuntime != null ? actionBarRuntime.ActiveSlotIndex : lastSlotIndex;
            Equip(normalized);
        }

        private void Subscribe()
        {
            if (actionBarRuntime != null)
            {
                actionBarRuntime.ActiveSlotChanged -= OnActiveSlotChanged;
                actionBarRuntime.ActiveSlotChanged += OnActiveSlotChanged;
            }
        }

        private void Unsubscribe()
        {
            if (actionBarRuntime != null)
            {
                actionBarRuntime.ActiveSlotChanged -= OnActiveSlotChanged;
            }
        }

        private void OnActiveSlotChanged(int slotIndex, string itemId)
        {
            RefreshHeldItem(force: true);
        }

        private void ResolveReferences()
        {
            if (actionBarRuntime == null)
            {
                actionBarRuntime = GetComponent<ActionBarRuntime>() ?? ActionBarRuntime.Active;
            }

            if (inventoryRuntime == null)
            {
                inventoryRuntime = GetComponent<PlayerInventoryRuntime>();
            }

            if (handAnchor == null)
            {
                handAnchor = useExplicitHandBoneOnly ? EnsureFallbackAnchor() : FindLikelyHandAnchor();
            }
        }

        private Transform FindLikelyHandAnchor()
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in transforms)
            {
                if (t == null || t == transform)
                {
                    continue;
                }

                string n = NormalizeName(t.name);
                if ((n.Contains("right") || n.Contains("r")) && n.Contains("hand"))
                {
                    return t;
                }
            }

            foreach (Transform t in transforms)
            {
                if (t == null || t == transform)
                {
                    continue;
                }

                string n = NormalizeName(t.name);
                if (n.Contains("hand"))
                {
                    return t;
                }
            }

            return transform;
        }

        private void EnsureHeldRoot()
        {
            if (heldRoot != null)
            {
                return;
            }

            heldRoot = new GameObject("HeldItemRoot");
            Transform parent = handAnchor != null ? handAnchor : EnsureFallbackAnchor();
            heldRoot.transform.SetParent(parent, false);
            ApplyAnchorPose();
            heldRoot.SetActive(false);
        }

        private Transform EnsureFallbackAnchor()
        {
            if (fallbackAnchor != null)
            {
                return fallbackAnchor;
            }

            GameObject anchorGo = new GameObject("HeldItemFallbackAnchor");
            anchorGo.transform.SetParent(transform, false);
            anchorGo.transform.localPosition = fallbackLocalPosition;
            anchorGo.transform.localRotation = Quaternion.Euler(fallbackLocalEuler);
            fallbackAnchor = anchorGo.transform;
            return fallbackAnchor;
        }

        private void ApplyAnchorPose()
        {
            if (heldRoot == null)
            {
                return;
            }

            Transform expectedParent = handAnchor != null ? handAnchor : EnsureFallbackAnchor();
            if (heldRoot.transform.parent != expectedParent) heldRoot.transform.SetParent(expectedParent, false);

            heldRoot.transform.localPosition = Vector3.zero;
            heldRoot.transform.localRotation = Quaternion.identity;

            heldRoot.transform.localScale = Vector3.one * Mathf.Max(0.1f, heldVisualScale);
        }

        private void RefreshHeldItem(bool force)
        {
            EnsureHeldRoot();

            int activeSlot = actionBarRuntime != null ? actionBarRuntime.ActiveSlotIndex : -1;
            string activeItem = Normalize(actionBarRuntime != null ? actionBarRuntime.ActiveItemId : string.Empty);

            if (!string.IsNullOrWhiteSpace(activeItem) && !ActionBarRuntime.IsActionBarItem(activeItem))
            {
                activeItem = string.Empty;
            }

            if (requireItemInInventory && !string.IsNullOrWhiteSpace(activeItem) && !HasItem(activeItem))
            {
                activeItem = string.Empty;
            }

            if (!force && activeSlot == lastSlotIndex && string.Equals(activeItem, currentItemId, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            lastSlotIndex = activeSlot;
            Equip(activeItem);
        }

        private bool HasItem(string itemId)
        {
            return inventoryRuntime != null &&
                   inventoryRuntime.Inventory != null &&
                   inventoryRuntime.Inventory.GetAmount(itemId) > 0;
        }

        private void Equip(string itemId)
        {
            currentItemId = Normalize(itemId);
            ClearHeldChildren();

            if (heldRoot != null)
            {
                heldRoot.name = string.IsNullOrWhiteSpace(currentItemId) ? "HeldItemRoot_empty" : $"HeldItemRoot_{currentItemId}";
            }

            if (string.IsNullOrWhiteSpace(currentItemId))
            {
                heldRoot.SetActive(false);
                return;
            }

            heldRoot.SetActive(true);
            ApplyAnchorPose();

            switch (currentItemId)
            {
                case "spear":
                    BuildSpear();
                    break;
                case "bow":
                    BuildBow();
                    break;
                case "axe":
                    BuildAxe();
                    break;
                case "pickaxe":
                    BuildPickaxe();
                    break;
                case "torch":
                    BuildTorch();
                    break;
                default:
                    heldRoot.SetActive(false);
                    return;
            }

            if (logEquips)
            {
                Debug.Log($"[HeldItem] Equipped {currentItemId}", this);
            }
        }

        private void ClearHeldChildren()
        {
            if (heldRoot == null)
            {
                return;
            }

            torchLight = null;
            for (int i = heldRoot.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = heldRoot.transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void BuildSpear()
        {
            CreatePart("SpearShaft", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.22f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.05f, 1.18f, 0.05f), new Color(0.48f, 0.31f, 0.16f));
            CreatePart("SpearTip", PrimitiveType.Cube, new Vector3(0f, 0f, 1.45f), Quaternion.Euler(45f, 0f, 0f), new Vector3(0.18f, 0.18f, 0.34f), new Color(0.78f, 0.82f, 0.78f));
        }

        private void BuildBow()
        {
            CreatePart("BowBody", PrimitiveType.Cylinder, new Vector3(0.04f, 0f, 0.18f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.045f, 0.86f, 0.045f), new Color(0.42f, 0.24f, 0.10f));
            CreatePart("BowString", PrimitiveType.Cylinder, new Vector3(0.22f, 0f, 0.18f), Quaternion.Euler(0f, 0f, 0f), new Vector3(0.012f, 0.78f, 0.012f), new Color(0.86f, 0.82f, 0.70f));
        }

        private void BuildAxe()
        {
            CreatePart("AxeHandle", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.14f), Quaternion.Euler(10f, 0f, 0f), new Vector3(0.05f, 0.66f, 0.05f), new Color(0.42f, 0.25f, 0.12f));
            CreatePart("AxeHead", PrimitiveType.Cube, new Vector3(0f, 0.62f, 0.10f), Quaternion.identity, new Vector3(0.52f, 0.19f, 0.16f), new Color(0.68f, 0.70f, 0.66f));
        }

        private void BuildPickaxe()
        {
            CreatePart("PickaxeHandle", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0.14f), Quaternion.Euler(10f, 0f, 0f), new Vector3(0.05f, 0.70f, 0.05f), new Color(0.38f, 0.23f, 0.12f));
            CreatePart("PickaxeHead", PrimitiveType.Cube, new Vector3(0f, 0.66f, 0.10f), Quaternion.identity, new Vector3(0.76f, 0.12f, 0.14f), new Color(0.62f, 0.64f, 0.60f));
        }

        private void BuildTorch()
        {
            CreatePart("TorchHandle", PrimitiveType.Cylinder, new Vector3(0f, -0.02f, 0.05f), Quaternion.Euler(8f, 0f, 0f), new Vector3(0.045f, 0.50f, 0.045f), new Color(0.34f, 0.20f, 0.10f));
            CreatePart("TorchFlame", PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0.04f), Quaternion.identity, new Vector3(0.18f, 0.25f, 0.18f), new Color(1f, 0.42f, 0.08f));

            GameObject lightGo = new GameObject("HeldTorchLight");
            lightGo.transform.SetParent(heldRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.58f, 0.04f);
            torchLight = lightGo.AddComponent<Light>();
            torchLight.type = LightType.Point;
            torchLight.color = new Color(1f, 0.68f, 0.28f);
            torchLight.range = 7.5f;
            torchLight.intensity = 2.0f;
            torchLight.shadows = LightShadows.Soft;
            torchLight.bounceIntensity = 0.8f;
            torchBaseIntensity = torchLight.intensity;
            torchBaseRange = torchLight.range;
        }

        private void UpdateTorchFlicker()
        {
            if (!string.Equals(currentItemId, "torch", StringComparison.OrdinalIgnoreCase) || torchLight == null)
            {
                return;
            }

            float noise = Mathf.PerlinNoise(Time.time * torchFlickerSpeed, 0.37f);
            float pulse = Mathf.Sin(Time.time * (torchFlickerSpeed * 0.8f)) * 0.5f + 0.5f;
            float flicker = Mathf.Clamp01((noise * 0.7f + pulse * 0.3f));
            torchLight.intensity = torchBaseIntensity * (1f - torchFlickerAmount) + torchBaseIntensity * torchFlickerAmount * flicker;
            torchLight.range = torchBaseRange * (1f - torchFlickerRangeAmount) + torchBaseRange * torchFlickerRangeAmount * flicker;
        }

        private GameObject CreatePart(string name, PrimitiveType primitive, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Color color)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(heldRoot.transform, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = localRotation;
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                else material.color = color;
                renderer.sharedMaterial = material;
            }

            return part;
        }

        private static string Normalize(string itemId)
        {
            return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
        }

        private static string NormalizeName(string name)
        {
            return string.IsNullOrWhiteSpace(name) ? string.Empty : name.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
