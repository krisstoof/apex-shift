using ApexShift.Core.Resources;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Audio;
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

        [Header("Godot ResourceNode parity")]
        [SerializeField]
        private bool edibleByHerbivores;

        [SerializeField]
        private float foodValue;

        [SerializeField]
        private bool renderOnly;

        [SerializeField]
        private bool pondVegetation;

        [SerializeField]
        private int pickupPriority;

        [SerializeField]
        private int regrowthDays;

        [SerializeField]
        private bool bridgeToFoodSource = true;

        [SerializeField]
        private GameObject depletedVisual;

        [SerializeField]
        private float interactionRadius = 2.25f;

        [Header("Tool gating")]
        [SerializeField] private string requiredToolItemId = string.Empty;
        [SerializeField] private bool autoResolveToolRequirement = true;
        [SerializeField] private string missingToolMessage = string.Empty;

        private ResourceDefinition definition;
        private ResourceState state;
        private FoodSourceView foodSourceView;
        private readonly HarvestSystem harvestSystem = new HarvestSystem();

        public string Prompt
        {
            get
            {
                EnsureState();
                string prompt = harvestSystem.GetPrompt(state);
                string requiredTool = ResolveRequiredToolItemId();
                if (!string.IsNullOrWhiteSpace(prompt) && !string.IsNullOrWhiteSpace(requiredTool))
                {
                    return $"{prompt} ({FormatRequiredTool(requiredTool)} required)";
                }

                return prompt;
            }
        }

        public int Priority { get { EnsureState(); return state.PickupPriority; } }
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
            foodValue = Mathf.Max(0f, foodValue);
            pickupPriority = Mathf.Max(0, pickupPriority);
            regrowthDays = Mathf.Max(0, regrowthDays);
        }

        public void ConfigureDefault(string kind)
        {
            ResourceDefinition defaultDefinition = ResourceDefinition.CreateDefault(kind);
            resourceKind = defaultDefinition.Id.ToString();
            displayName = defaultDefinition.DisplayName;
            itemId = defaultDefinition.ItemId;
            amount = defaultDefinition.HarvestAmount;
            playerHarvestable = defaultDefinition.PlayerHarvestable;
            edibleByHerbivores = defaultDefinition.EdibleByHerbivores;
            foodValue = defaultDefinition.FoodValue;
            renderOnly = defaultDefinition.RenderOnly;
            pondVegetation = defaultDefinition.PondVegetation;
            pickupPriority = defaultDefinition.PickupPriority;
            regrowthDays = defaultDefinition.RegrowthDays;
            bridgeToFoodSource = defaultDefinition.EdibleByHerbivores;
            deactivateOnHarvest = defaultDefinition.RemoveWhenHarvested;
            definition = null;
            state = null;
            EnsureState();
        }

        public bool CanInteract(GameObject actor)
        {
            EnsureState();
            if (state.RenderOnly)
            {
                return false;
            }

            if (!TryResolveInventory(actor, out PlayerInventoryRuntime inventoryRuntime))
            {
                return false;
            }

            if (!HasRequiredTool(inventoryRuntime, out _))
            {
                return false;
            }

            return harvestSystem.CanHarvest(state, inventoryRuntime.Inventory, out _);
        }

        public bool Interact(GameObject actor)
        {
            EnsureState();
            if (!TryResolveInventory(actor, out PlayerInventoryRuntime inventoryRuntime))
            {
                Debug.Log("Cannot harvest resource: no PlayerInventoryRuntime found on actor.", this);
                return false;
            }

            if (!HasRequiredTool(inventoryRuntime, out string missingTool))
            {
                Debug.Log($"[ResourceNode] Cannot harvest {displayName}: requires {missingTool}.", this);
                return false;
            }

            HarvestResult result = harvestSystem.Harvest(state, inventoryRuntime.Inventory);
            if (!result.Success)
            {
                Debug.Log($"[ResourceNode] Harvest failed: {result.Message}", this);
                return false;
            }

            Debug.Log($"[ResourceNode] Harvested: {result.Message}. Total in inventory: {inventoryRuntime.Inventory.GetAmount(state.ItemId)}", this);
            WorldActionAudio.PlayPickup(transform.position);
            GameEventBus.PublishResourceHarvested(
                transform.position,
                ResolveEventBiomeId(),
                result.ResourceId,
                result.ItemId,
                result.AddedAmount,
                result.Message);
            RefreshFoodSourceBridge();
            if (result.ShouldRemoveNode)
            {
                ApplyDepletedVisualState();
            }

            return true;
        }

        public void LoadState(int currentAmount, bool depleted)
        {
            LoadState(currentAmount, depleted, depleted ? 0f : 1f);
        }

        public void LoadState(int currentAmount, bool depleted, float growthProgress)
        {
            EnsureState();
            if (depleted || currentAmount <= 0)
            {
                state.MarkDepleted();
                state.SetGrowthProgress(growthProgress);
                ApplyDepletedVisualState();
            }
            else
            {
                state.SetAmount(currentAmount);
                state.SetGrowthProgress(growthProgress);
                SetVisualsEnabled(true);
                if (depletedVisual != null) depletedVisual.SetActive(false);
            }

            RefreshFoodSourceBridge();
        }

        public bool AdvanceGrowthDays(int days)
        {
            EnsureState();
            bool changed = state.AdvanceGrowthDays(days);
            if (!changed)
            {
                return false;
            }

            if (!state.IsDepleted)
            {
                SetVisualsEnabled(true);
                if (depletedVisual != null) depletedVisual.SetActive(false);
            }

            RefreshFoodSourceBridge();
            return true;
        }

        private string ResolveEventBiomeId()
        {
            EcosystemDirectorRuntime director = EcosystemDirectorRuntime.Active;
            if (director == null)
            {
                return "default";
            }

            string biomeId = director.GetBiomeIdForPosition(transform.position);
            return string.IsNullOrWhiteSpace(biomeId) ? "default" : biomeId;
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
                        deactivateOnHarvest || destroyOnHarvest,
                        edibleByHerbivores,
                        foodValue,
                        renderOnly,
                        pondVegetation,
                        pickupPriority,
                        regrowthDays);
            }

            if (state == null)
            {
                state = definition.CreateState();
            }

            EnsureFoodSourceBridge();
        }

        private bool HasRequiredTool(PlayerInventoryRuntime inventoryRuntime, out string missingTool)
        {
            missingTool = string.Empty;
            string requiredTool = ResolveRequiredToolItemId();
            if (string.IsNullOrWhiteSpace(requiredTool))
            {
                return true;
            }

            if (inventoryRuntime == null || inventoryRuntime.Inventory == null || inventoryRuntime.Inventory.GetAmount(requiredTool) <= 0)
            {
                missingTool = requiredTool;
                missingToolMessage = $"Requires {FormatRequiredTool(requiredTool)}";
                return false;
            }

            missingToolMessage = string.Empty;
            return true;
        }

        private string ResolveRequiredToolItemId()
        {
            if (!autoResolveToolRequirement && !string.IsNullOrWhiteSpace(requiredToolItemId))
            {
                return requiredToolItemId.Trim().ToLowerInvariant();
            }

            EnsureState();
            string kind = state != null ? state.ResourceId : resourceKind;
            kind = string.IsNullOrWhiteSpace(kind) ? string.Empty : kind.Trim().ToLowerInvariant();

            switch (kind)
            {
                case "big_tree":
                    return "axe";
                case "big_rock":
                    return "pickaxe";
                default:
                    return string.IsNullOrWhiteSpace(requiredToolItemId)
                        ? string.Empty
                        : requiredToolItemId.Trim().ToLowerInvariant();
            }
        }

        private static string FormatRequiredTool(string toolItemId)
        {
            switch (string.IsNullOrWhiteSpace(toolItemId) ? string.Empty : toolItemId.Trim().ToLowerInvariant())
            {
                case "axe":
                    return "axe";
                case "pickaxe":
                    return "pickaxe";
                default:
                    return toolItemId;
            }
        }

        public void ConfigureToolRequirement(string toolItemId)
        {
            requiredToolItemId = string.IsNullOrWhiteSpace(toolItemId)
                ? string.Empty
                : toolItemId.Trim().ToLowerInvariant();
            autoResolveToolRequirement = string.IsNullOrWhiteSpace(requiredToolItemId);
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

        private void EnsureFoodSourceBridge()
        {
            if (!bridgeToFoodSource || state == null || !state.EdibleByHerbivores || state.FoodValue <= 0f)
            {
                return;
            }

            foodSourceView = GetComponent<FoodSourceView>();
            if (foodSourceView == null)
            {
                foodSourceView = gameObject.AddComponent<FoodSourceView>();
            }

            foodSourceView.Configure(
                state.ResourceId,
                state.DisplayName,
                FoodKind.Plants,
                Mathf.Max(0.01f, state.FoodValue),
                1f);

            RefreshFoodSourceBridge();
        }

        private void RefreshFoodSourceBridge()
        {
            if (foodSourceView == null)
            {
                return;
            }

            bool available = state != null && state.IsFoodAvailableForHerbivores;
            foodSourceView.enabled = available;
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
