using System;
using System.Collections.Generic;
using ApexShift.Core.Resources;
using ApexShift.Core.Save;
using ApexShift.Runtime.Items;
using ApexShift.Runtime.Resources;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    public enum TreeLifecycleState
    {
        Standing,
        Falling,
        Depleted
    }

    [DisallowMultipleComponent]
    public sealed class HarvestableTreeRuntime : MonoBehaviour, IAxeHitTarget
    {
        private const float FallAngleDegrees = 85f;
        private static readonly HashSet<string> warnedMissingStumpSpecies = new HashSet<string>(StringComparer.Ordinal);

        [SerializeField] private string treeId = string.Empty;
        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private string biomeId = string.Empty;
        [SerializeField] private string resourceKind = string.Empty;
        [SerializeField] private ResourceNodeView resourceNode;
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float currentHealth = 100f;
        [SerializeField] private TreeLifecycleState lifecycleState = TreeLifecycleState.Standing;
        [SerializeField, Min(0)] private int regrowthDays = 5;
        [SerializeField, Range(0f, 1f)] private float regrowthProgress = 1f;
        [SerializeField, Min(0f)] private float fallDuration = 1.2f;
        [SerializeField] private bool dropsSpawned;
        [SerializeField] private GameObject depletedVisualPrefab;
        [SerializeField] private bool streamingGameplayActive = true;

        [NonSerialized] private GameObject depletedVisualInstance;
        [NonSerialized] private Renderer[] standingRenderers;
        [NonSerialized] private bool[] standingRendererEnabled;
        [NonSerialized] private Collider[] trunkColliders;
        [NonSerialized] private Collider interactionCollider;
        [NonSerialized] private Vector3 standingPosition;
        [NonSerialized] private Quaternion standingRotation;
        [NonSerialized] private Vector3 fallAxis;
        [NonSerialized] private float fallElapsed;

        public string TreeId => treeId;
        public string SpeciesId => speciesId;
        public string BiomeId => biomeId;
        public string ResourceKind => resourceKind;
        public ResourceNodeView ResourceNode => resourceNode;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public TreeLifecycleState LifecycleState => lifecycleState;
        public float RegrowthProgress => regrowthProgress;
        public bool DropsSpawned => dropsSpawned;
        public int RegrowthDays => regrowthDays;
        public float FallDuration => fallDuration;
        public bool StreamingGameplayActive => streamingGameplayActive;
        public int StreamingColliderCount => (trunkColliders != null ? trunkColliders.Length : 0) + (interactionCollider != null ? 1 : 0);
        public int ActiveStreamingColliderCount
        {
            get
            {
                int count = 0;
                if (trunkColliders != null)
                    for (int i = 0; i < trunkColliders.Length; i++) if (trunkColliders[i] != null && trunkColliders[i].enabled) count++;
                if (interactionCollider != null && interactionCollider.enabled) count++;
                return count;
            }
        }
        public IReadOnlyList<Collider> TrunkColliders => trunkColliders ?? Array.Empty<Collider>();

        public void Configure(VegetationInstanceRuntime marker, VegetationSpeciesAsset species, ResourceNodeView node)
        {
            if (marker == null || string.IsNullOrWhiteSpace(marker.InstanceId) || species == null || node == null)
            {
                Debug.LogError("HarvestableTreeRuntime requires a stable vegetation ID, species configuration, and ResourceNodeView.", this);
                return;
            }

            bool newIdentity = !string.Equals(treeId, marker.InstanceId, StringComparison.Ordinal);
            HarvestableTreeRegistry.Unregister(this);

            treeId = marker.InstanceId;
            speciesId = marker.SpeciesId;
            biomeId = marker.BiomeId;
            resourceKind = string.IsNullOrWhiteSpace(species.ResourceKind) ? marker.ResourceKind : species.ResourceKind.Trim().ToLowerInvariant();
            resourceNode = node;
            maxHealth = Mathf.Max(1f, species.TreeMaxHealth);
            regrowthDays = Mathf.Max(0, species.TreeRegrowthDays);
            fallDuration = Mathf.Max(0f, species.TreeFallDuration);
            depletedVisualPrefab = species.DepletedVisualPrefab;

            if (newIdentity)
            {
                currentHealth = maxHealth;
                lifecycleState = TreeLifecycleState.Standing;
                regrowthProgress = 1f;
                dropsSpawned = false;
            }
            else
            {
                currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            }

            CacheRuntimeComponents();
            HarvestableTreeRegistry.Register(this);
            SetStreamingGameplayActive(streamingGameplayActive);
            enabled = lifecycleState == TreeLifecycleState.Falling;
        }

        public bool CanAxeHit(GameObject actor)
        {
            return lifecycleState == TreeLifecycleState.Standing
                   && streamingGameplayActive
                   && currentHealth > 0f
                   && resourceNode != null
                   && resourceNode.HasRequiredTool(actor);
        }

        public bool TryAxeHit(in AxeHitContext hit)
        {
            if (!CanAxeHit(hit.Actor) || hit.Damage <= 0f || float.IsNaN(hit.Damage) || float.IsInfinity(hit.Damage))
                return false;

            currentHealth = Mathf.Max(0f, currentHealth - hit.Damage);
            if (currentHealth <= 0f) BeginFalling(hit.Direction);
            return true;
        }

        public TreeSaveData CaptureSaveData()
        {
            return new TreeSaveData(treeId, speciesId, resourceKind, lifecycleState.ToString(),
                currentHealth, maxHealth, regrowthProgress, dropsSpawned);
        }

        public void RestoreSaveData(TreeSaveData saved)
        {
            if (saved == null) return;
            if (!string.Equals(saved.TreeId, treeId, StringComparison.Ordinal))
            {
                Debug.LogWarning($"Skipping tree save with ID '{saved.TreeId}'; this generated tree is '{treeId}'.", this);
                return;
            }

            if (!string.IsNullOrWhiteSpace(saved.SpeciesId)
                && !string.Equals(saved.SpeciesId, speciesId, StringComparison.Ordinal))
                Debug.LogWarning($"Tree '{treeId}' save species '{saved.SpeciesId}' differs from generated species '{speciesId}'.", this);

            if (saved.MaxHealth > 0f) maxHealth = saved.MaxHealth;
            currentHealth = Mathf.Clamp(saved.CurrentHealth, 0f, maxHealth);
            if (!string.IsNullOrWhiteSpace(saved.ResourceKind)) resourceKind = saved.ResourceKind;
            dropsSpawned = saved.DropsSpawned;
            regrowthProgress = Mathf.Clamp01(saved.RegrowthProgress);
            if (!Enum.TryParse(saved.LifecycleState, true, out TreeLifecycleState restoredState))
                restoredState = TreeLifecycleState.Standing;

            CacheRuntimeComponents();
            switch (restoredState)
            {
                case TreeLifecycleState.Falling:
                    lifecycleState = TreeLifecycleState.Falling;
                    SettleDepletedState(spawnMissingDrops: true);
                    break;
                case TreeLifecycleState.Depleted:
                    lifecycleState = TreeLifecycleState.Depleted;
                    SettleDepletedState(spawnMissingDrops: true);
                    regrowthProgress = Mathf.Clamp01(saved.RegrowthProgress);
                    break;
                default:
                    lifecycleState = TreeLifecycleState.Standing;
                    if (currentHealth <= 0f) currentHealth = Mathf.Max(0.01f, maxHealth);
                    regrowthProgress = 1f;
                    standingPosition = transform.position;
                    standingRotation = transform.rotation;
                    SetStumpVisible(false);
                    SetStandingVisuals(true);
                    SetStreamingColliders();
                    enabled = false;
                    if (resourceNode != null) resourceNode.State.RestoreToFull();
                    break;
            }
        }

        public bool AdvanceGrowthDays(int days)
        {
            if (lifecycleState != TreeLifecycleState.Depleted || days <= 0 || regrowthDays <= 0) return false;

            regrowthProgress = Mathf.Clamp01(regrowthProgress + days / (float)regrowthDays);
            if (regrowthProgress < 1f) return true;

            currentHealth = maxHealth;
            lifecycleState = TreeLifecycleState.Standing;
            dropsSpawned = false;
            standingPosition = transform.position;
            standingRotation = transform.rotation;
            SetStumpVisible(false);
            SetStandingVisuals(true);
            SetStreamingColliders();
            enabled = false;
            if (resourceNode != null) resourceNode.State.RestoreToFull();
            return true;
        }

        public void SetStreamingGameplayActive(bool active)
        {
            streamingGameplayActive = active;
            if (active && lifecycleState == TreeLifecycleState.Falling) enabled = true;
            SetStreamingColliders();
        }

        public void PrepareForStreamingDeactivation()
        {
            if (lifecycleState == TreeLifecycleState.Falling)
                SettleDepletedState(spawnMissingDrops: true);
        }

        private void Awake()
        {
            standingPosition = transform.position;
            standingRotation = transform.rotation;
            CacheRuntimeComponents();
        }

        private void OnEnable()
        {
            if (standingRenderers == null) CacheRuntimeComponents();
            if (!string.IsNullOrWhiteSpace(treeId)) HarvestableTreeRegistry.Register(this);
        }

        private void Update()
        {
            if (lifecycleState != TreeLifecycleState.Falling) return;

            fallElapsed += Time.deltaTime;
            float progress = fallDuration <= 0f ? 1f : Mathf.Clamp01(fallElapsed / fallDuration);
            transform.position = standingPosition;
            transform.rotation = Quaternion.AngleAxis(FallAngleDegrees * progress, fallAxis) * standingRotation;
            if (progress >= 1f) SettleDepletedState(spawnMissingDrops: true);
        }

        private void BeginFalling(Vector3 hitDirection)
        {
            if (lifecycleState != TreeLifecycleState.Standing) return;

            lifecycleState = TreeLifecycleState.Falling;
            standingPosition = transform.position;
            standingRotation = transform.rotation;
            Vector3 horizontalDirection = hitDirection;
            horizontalDirection.y = 0f;
            if (horizontalDirection.sqrMagnitude < 0.0001f) horizontalDirection = transform.forward;
            horizontalDirection.y = 0f;
            horizontalDirection.Normalize();
            fallAxis = Vector3.Cross(Vector3.up, horizontalDirection).normalized;
            if (fallAxis.sqrMagnitude < 0.0001f) fallAxis = transform.right;
            fallElapsed = 0f;
            enabled = true;
            SetStreamingColliders();

            if (fallDuration <= 0f) SettleDepletedState(spawnMissingDrops: true);
        }

        private void SettleDepletedState(bool spawnMissingDrops)
        {
            transform.position = standingPosition;
            transform.rotation = standingRotation;
            currentHealth = 0f;
            lifecycleState = TreeLifecycleState.Depleted;
            streamingGameplayActive = false;
            regrowthProgress = 0f;
            SetStandingVisuals(false);
            SetStreamingColliders();
            enabled = false;
            if (resourceNode != null) resourceNode.State.MarkDepleted();
            SetStumpVisible(true);

            if (spawnMissingDrops && !dropsSpawned)
            {
                // Set before spawning so callbacks/re-entry cannot duplicate this yield.
                dropsSpawned = true;
                SpawnDrops();
            }
        }

        private void SpawnDrops()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault(resourceKind);
            ItemPickupSpawner.Spawn(definition.ItemId, definition.HarvestAmount,
                transform.position + Vector3.up * 0.15f, Quaternion.identity);
        }

        private void SetStumpVisible(bool visible)
        {
            if (visible)
            {
                if (depletedVisualPrefab == null)
                {
                    if (warnedMissingStumpSpecies.Add(speciesId ?? string.Empty))
                    Debug.LogWarning($"Harvestable tree species '{speciesId}' has no depleted/stump visual prefab assigned; tree lifecycle will continue without one.", this);
                    return;
                }
                if (depletedVisualInstance == null)
                {
                    depletedVisualInstance = Instantiate(depletedVisualPrefab, transform, false);
                    depletedVisualInstance.name = "TreeStump";
                    depletedVisualInstance.transform.localPosition = Vector3.zero;
                    depletedVisualInstance.transform.localRotation = Quaternion.identity;
                }
                depletedVisualInstance.SetActive(true);
                return;
            }

            if (depletedVisualInstance == null) return;
            GameObject oldStump = depletedVisualInstance;
            depletedVisualInstance = null;
            oldStump.SetActive(false);
            if (Application.isPlaying) Destroy(oldStump);
            else DestroyImmediate(oldStump);
        }

        private void SetStandingVisuals(bool visible)
        {
            if (standingRenderers == null) CacheRuntimeComponents();
            for (int i = 0; i < standingRenderers.Length; i++)
            {
                Renderer renderer = standingRenderers[i];
                if (renderer != null) renderer.enabled = visible && standingRendererEnabled[i];
            }
        }

        private void SetTrunkColliders(bool enabled)
        {
            if (trunkColliders == null) CacheRuntimeComponents();
            for (int i = 0; i < trunkColliders.Length; i++)
                if (trunkColliders[i] != null) trunkColliders[i].enabled = enabled;
        }

        private void SetInteractionCollider(bool enabled)
        {
            if (interactionCollider == null && resourceNode != null)
                interactionCollider = resourceNode.GetComponent<SphereCollider>();
            if (interactionCollider != null) interactionCollider.enabled = enabled;
        }

        private void SetStreamingColliders()
        {
            bool enabledForStanding = streamingGameplayActive && lifecycleState == TreeLifecycleState.Standing;
            SetTrunkColliders(enabledForStanding);
            SetInteractionCollider(enabledForStanding);
        }

        private void CacheRuntimeComponents()
        {
            if (depletedVisualInstance == null)
            {
                Transform existingStump = transform.Find("TreeStump");
                if (existingStump != null) depletedVisualInstance = existingStump.gameObject;
            }

            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            var visualRenderers = new List<Renderer>(allRenderers.Length);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer renderer = allRenderers[i];
                if (renderer == null || (depletedVisualInstance != null
                    && (renderer.transform == depletedVisualInstance.transform || renderer.transform.IsChildOf(depletedVisualInstance.transform)))) continue;
                visualRenderers.Add(renderer);
            }
            standingRenderers = visualRenderers.ToArray();
            standingRendererEnabled = new bool[standingRenderers.Length];
            for (int i = 0; i < standingRenderers.Length; i++)
                standingRendererEnabled[i] = standingRenderers[i] != null && standingRenderers[i].enabled;

            var foundTrunkColliders = new List<Collider>();
            VegetationTrunkCollider[] markedTrunks = GetComponentsInChildren<VegetationTrunkCollider>(true);
            for (int i = 0; i < markedTrunks.Length; i++)
            {
                if (markedTrunks[i] == null) continue;
                Collider[] colliders = markedTrunks[i].GetComponents<Collider>();
                for (int c = 0; c < colliders.Length; c++)
                    if (colliders[c] != null && !foundTrunkColliders.Contains(colliders[c])) foundTrunkColliders.Add(colliders[c]);
            }
            trunkColliders = foundTrunkColliders.ToArray();
            if (resourceNode != null) interactionCollider = resourceNode.GetComponent<SphereCollider>();
        }

        private void OnDestroy()
        {
            HarvestableTreeRegistry.Unregister(this);
        }
    }
}
