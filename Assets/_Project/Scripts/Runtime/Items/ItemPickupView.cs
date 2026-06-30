using ApexShift.Core.Items;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Player;
using UnityEngine;

namespace ApexShift.Runtime.Items
{
    [DisallowMultipleComponent]
    public sealed class ItemPickupView : MonoBehaviour, IInteractable
    {
        [SerializeField] private string itemId;
        [SerializeField] private int amount = 1;
        [SerializeField] private float pickupRadius = 1.5f;
        [SerializeField] private bool destroyOnPickup = true;
        [SerializeField] private float maxLifetimeSeconds = 900f;
        [SerializeField] private float fadeStartSeconds = 720f;

        private SphereCollider triggerCollider;
        private Renderer[] cachedRenderers;
        private Color[] baseColors;
        private float spawnTime;

        public string ItemId => itemId;
        public int Amount => Mathf.Max(1, amount);
        public string Prompt => string.IsNullOrWhiteSpace(itemId) ? "Take item" : $"Take {itemId} x{Amount}";
        public int Priority => 35;
        public float InteractionDuration => 0.05f;

        private void Awake()
        {
            spawnTime = Time.time;
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
            CacheBaseColors();
            EnsureTrigger();
        }

        private void Update()
        {
            if (maxLifetimeSeconds <= 0f)
            {
                return;
            }

            float age = Time.time - spawnTime;
            if (age >= maxLifetimeSeconds)
            {
                Destroy(gameObject);
                return;
            }

            if (age >= fadeStartSeconds)
            {
                float t = Mathf.InverseLerp(fadeStartSeconds, maxLifetimeSeconds, age);
                ApplyFade(1f - t);
            }
        }

        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);
            pickupRadius = Mathf.Max(0.1f, pickupRadius);
            maxLifetimeSeconds = Mathf.Max(0f, maxLifetimeSeconds);
            fadeStartSeconds = Mathf.Clamp(fadeStartSeconds, 0f, maxLifetimeSeconds <= 0f ? fadeStartSeconds : maxLifetimeSeconds);
        }

        public void Configure(string newItemId, int newAmount)
        {
            itemId = string.IsNullOrWhiteSpace(newItemId) ? string.Empty : newItemId.Trim().ToLowerInvariant();
            amount = Mathf.Max(1, newAmount);
            EnsureTrigger();
        }

        public bool CanInteract(GameObject actor)
        {
            return actor != null && isActiveAndEnabled && !string.IsNullOrWhiteSpace(itemId);
        }

        public bool Interact(GameObject actor)
        {
            if (actor == null)
            {
                return false;
            }

            PlayerInventoryRuntime inventory = actor.GetComponent<PlayerInventoryRuntime>() ?? actor.GetComponentInParent<PlayerInventoryRuntime>();
            if (inventory == null || inventory.Inventory == null)
            {
                return false;
            }

            int remainder = inventory.Inventory.AddItem(itemId, Amount);
            int added = Amount - remainder;
            if (added <= 0)
            {
                return false;
            }

            if (remainder <= 0)
            {
                if (destroyOnPickup)
                {
                    Destroy(gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }
                return true;
            }

            amount = remainder;
            return true;
        }

        private void EnsureTrigger()
        {
            triggerCollider = GetComponent<SphereCollider>();
            if (triggerCollider == null)
            {
                triggerCollider = gameObject.AddComponent<SphereCollider>();
            }

            triggerCollider.isTrigger = true;
            triggerCollider.radius = pickupRadius;
        }

        private void CacheBaseColors()
        {
            if (cachedRenderers == null || cachedRenderers.Length == 0)
            {
                baseColors = System.Array.Empty<Color>();
                return;
            }

            baseColors = new Color[cachedRenderers.Length];
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer != null && renderer.sharedMaterial != null)
                {
                    baseColors[i] = renderer.sharedMaterial.HasProperty("_BaseColor") ? renderer.sharedMaterial.GetColor("_BaseColor") : renderer.sharedMaterial.color;
                }
                else
                {
                    baseColors[i] = Color.white;
                }
            }
        }

        private void ApplyFade(float alphaMultiplier)
        {
            if (cachedRenderers == null || baseColors == null)
            {
                return;
            }

            alphaMultiplier = Mathf.Clamp01(alphaMultiplier);
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                Renderer renderer = cachedRenderers[i];
                if (renderer == null || renderer.material == null)
                {
                    continue;
                }

                Color baseColor = i < baseColors.Length ? baseColors[i] : Color.white;
                Color faded = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alphaMultiplier);
                if (renderer.material.HasProperty("_BaseColor"))
                {
                    renderer.material.SetColor("_BaseColor", faded);
                }
                else
                {
                    renderer.material.color = faded;
                }
            }
        }
    }
}
