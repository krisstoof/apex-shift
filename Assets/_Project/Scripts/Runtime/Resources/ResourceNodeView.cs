using ApexShift.Core.Resources;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Player;
using UnityEngine;

namespace ApexShift.Runtime.Resources
{
    [DisallowMultipleComponent]
    public sealed class ResourceNodeView : MonoBehaviour, IInteractable
    {
        [SerializeField]
        private string resourceKind = "conifer_tree";

        [SerializeField]
        private string displayName = "Resource";

        [SerializeField]
        private string itemId = string.Empty;

        [SerializeField]
        private int amount = 1;

        [SerializeField]
        private bool playerHarvestable = true;

        [SerializeField]
        private bool deactivateOnHarvest = true;

        [SerializeField]
        private bool destroyOnHarvest;

        [SerializeField]
        private GameObject depletedVisual;

        [SerializeField]
        private float interactionRadius = 2.25f;

        private ResourceDefinition definition;
        private ResourceState state;
        private readonly HarvestSystem harvestSystem = new HarvestSystem();

        public string Prompt
        {
            get
            {
                EnsureState();
                return harvestSystem.GetPrompt(state);
            }
        }

        public int Priority => 0;
        public float InteractionDuration => 1.5f;

        public ResourceState State
{
            get
            {
                EnsureState();
                return state;
            }
        }

        private void Awake()
        {
            EnsureState();
            EnsureInteractionCollider();
        }

        private void Reset()
        {
            ConfigureDefault(resourceKind);
            EnsureInteractionCollider();
        }

        private void OnValidate()
        {
            amount = Mathf.Max(1, amount);
            interactionRadius = Mathf.Max(0.1f, interactionRadius);
        }

        public void ConfigureDefault(string kind)
        {
            ResourceDefinition defaultDefinition = ResourceDefinition.CreateDefault(kind);
            resourceKind = defaultDefinition.Id.ToString();
            displayName = defaultDefinition.DisplayName;
            itemId = defaultDefinition.ItemId;
            amount = defaultDefinition.HarvestAmount;
            playerHarvestable = defaultDefinition.PlayerHarvestable;
            deactivateOnHarvest = defaultDefinition.RemoveWhenHarvested;
            definition = null;
            state = null;
            EnsureState();
        }

        public bool CanInteract(GameObject actor)
        {
            EnsureState();
            return TryResolveInventory(actor, out PlayerInventoryRuntime inventoryRuntime)
                   && harvestSystem.CanHarvest(state, inventoryRuntime.Inventory, out _);
        }

        public bool Interact(GameObject actor)
        {
            EnsureState();
            if (!TryResolveInventory(actor, out PlayerInventoryRuntime inventoryRuntime))
            {
                Debug.Log("Cannot harvest resource: no PlayerInventoryRuntime found on actor.", this);
                return false;
            }

            HarvestResult result = harvestSystem.Harvest(state, inventoryRuntime.Inventory);
            if (!result.Success)
            {
                Debug.Log($"[ResourceNode] Harvest failed: {result.Message}", this);
                return false;
            }

            Debug.Log($"[ResourceNode] Harvested: {result.Message}. Total in inventory: {inventoryRuntime.Inventory.GetAmount(state.ItemId)}", this);
            if (result.ShouldRemoveNode)
            {
                ApplyDepletedVisualState();
            }

            return true;
        }

        public void LoadState(int currentAmount, bool depleted)
        {
            EnsureState();
            state.SetAmount(currentAmount);
            if (depleted || currentAmount <= 0)
            {
                state.MarkDepleted();
                ApplyDepletedVisualState();
            }
            else
            {
                SetVisualsEnabled(true);
                if (depletedVisual != null) depletedVisual.SetActive(false);
            }
        }

        private void EnsureState()
        {
            if (definition == null)
            {
                string resolvedKind = string.IsNullOrWhiteSpace(resourceKind) ? "conifer_tree" : resourceKind;
                definition = string.IsNullOrWhiteSpace(itemId)
                    ? ResourceDefinition.CreateDefault(resolvedKind)
                    : new ResourceDefinition(
                        new ResourceId(resolvedKind),
                        string.IsNullOrWhiteSpace(displayName) ? resolvedKind : displayName,
                        itemId,
                        Mathf.Max(1, amount),
                        playerHarvestable,
                        deactivateOnHarvest || destroyOnHarvest);
            }

            if (state == null)
            {
                state = definition.CreateState();
            }
        }

        private void EnsureInteractionCollider()
        {
            SphereCollider trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
            }

            trigger.isTrigger = true;
            trigger.radius = Mathf.Max(0.1f, interactionRadius);
        }

        private void ApplyDepletedVisualState()
        {
            if (destroyOnHarvest)
            {
                Destroy(gameObject);
                return;
            }

            if (deactivateOnHarvest)
            {
                if (depletedVisual != null)
                {
                    SetVisualsEnabled(false);
                    depletedVisual.SetActive(true);
                }
                else
                {
                    gameObject.SetActive(false);
                }
                return;
            }

            SetVisualsEnabled(false);
            if (depletedVisual != null)
            {
                depletedVisual.SetActive(true);
            }
        }

        private void SetVisualsEnabled(bool enabled)
        {
            foreach (Renderer rendererComponent in GetComponentsInChildren<Renderer>(true))
            {
                if (depletedVisual != null && (rendererComponent.gameObject == depletedVisual || rendererComponent.transform.IsChildOf(depletedVisual.transform)))
                {
                    continue;
                }
                rendererComponent.enabled = enabled;
            }

            foreach (Collider colliderComponent in GetComponentsInChildren<Collider>(true))
            {
                if (depletedVisual != null && (colliderComponent.gameObject == depletedVisual || colliderComponent.transform.IsChildOf(depletedVisual.transform)))
                {
                    continue;
                }
                // Don't disable the root trigger collider if it's the one we're using for interaction
                if (colliderComponent == GetComponent<SphereCollider>() && colliderComponent.isTrigger)
                {
                    continue;
                }
                colliderComponent.enabled = enabled;
            }
        }

        private static bool TryResolveInventory(GameObject actor, out PlayerInventoryRuntime inventoryRuntime)
        {
            inventoryRuntime = null;
            if (actor == null)
            {
                return false;
            }

            inventoryRuntime = actor.GetComponent<PlayerInventoryRuntime>();
            if (inventoryRuntime == null)
            {
                inventoryRuntime = actor.GetComponentInParent<PlayerInventoryRuntime>();
            }

            if (inventoryRuntime == null && actor.transform.root != null)
            {
                inventoryRuntime = actor.transform.root.GetComponentInChildren<PlayerInventoryRuntime>(true);
            }

            if (inventoryRuntime != null)
            {
                inventoryRuntime.EnsureInitialized();
            }

            return inventoryRuntime != null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, interactionRadius));
        }
    }
}
