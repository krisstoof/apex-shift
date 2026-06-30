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

        private SphereCollider triggerCollider;

        public string ItemId => itemId;
        public int Amount => Mathf.Max(1, amount);
        public string Prompt => string.IsNullOrWhiteSpace(itemId) ? "Take item" : $"Take {itemId} x{Amount}";
        public int Priority => 35;
        public float InteractionDuration => 0.05f;

        private void Awake()
        {
            EnsureTrigger();
        }

        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);
            pickupRadius = Mathf.Max(0.1f, pickupRadius);
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
    }
}
