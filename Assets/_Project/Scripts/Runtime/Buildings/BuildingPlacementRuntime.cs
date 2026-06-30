using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World;
using ApexShift.Runtime.World.Generation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.Buildings
{
    [DisallowMultipleComponent]
    public sealed class BuildingPlacementRuntime : MonoBehaviour
    {
        [SerializeField] private PlayerInventoryRuntime inventoryRuntime;
        [SerializeField] private PrefabRegistry prefabRegistry;
        [SerializeField] private BuildingRegistry buildingRegistry;
        [SerializeField] private Transform placementOrigin;
        [SerializeField] private Transform buildingParent;
        [SerializeField] private List<PlaceableDefinition> definitions = new List<PlaceableDefinition>();
        [SerializeField] private float placementDistance = 3f;
        [SerializeField] private float maxSurfaceRayDistance = 500f;
        [SerializeField] private float surfaceSnapHeight = 80f;
        [SerializeField] private LayerMask blockingMask = Physics.DefaultRaycastLayers;
        [SerializeField] private LayerMask surfaceMask = Physics.DefaultRaycastLayers;
        [SerializeField] private LayerMask placementRaycastMask = Physics.DefaultRaycastLayers;
        [SerializeField] private bool enableKeyboardShortcuts = true;
        [SerializeField] private KeyCode placeKey = KeyCode.Mouse0;
        [SerializeField] private Key toggleMenuKey = Key.B;
        [SerializeField] private string[] quickSelectItems = { "storage_box", "campfire", "wall", "trap", "tent" };

        private readonly List<PlaceableDefinition> runtimeDefinitions = new List<PlaceableDefinition>();
        private PlacementPreview preview;
        private BuildingSelectionPanelUI selectionPanel;
        private PlaceableDefinition selectedDefinition;
        private PlacementValidationResult currentValidation = PlacementValidationResult.Invalid("nothing selected");
        private Vector3 currentPosition;
        private Quaternion currentRotation;

        public string SelectedItemId => selectedDefinition != null ? selectedDefinition.ItemId : string.Empty;
        public string SelectedBuildingId => selectedDefinition != null ? selectedDefinition.BuildingId : string.Empty;
        public bool IsBuildModeActive => selectedDefinition != null;
        public bool BlocksPlayerPrimaryAction => IsBuildModeActive;
        public PlacementValidationResult CurrentValidation => currentValidation;
        public IReadOnlyList<PlaceableDefinition> AvailableDefinitions
        {
            get
            {
                EnsureRuntimeDefinitions();
                return definitions.Concat(runtimeDefinitions).Where(definition => definition != null).ToList();
            }
        }

        private void Awake()
        {
            ResolveReferences();
            EnsureRuntimeDefinitions();
        }

        private void Update()
        {
            ResolveReferences();
            if (enableKeyboardShortcuts)
            {
                HandleKeyboardShortcuts();
            }

            UpdatePreview();
        }

        public void SetInventoryRuntime(PlayerInventoryRuntime runtime) => inventoryRuntime = runtime;
        public void SetPrefabRegistry(PrefabRegistry registry) => prefabRegistry = registry;
        public void SetBuildingRegistry(BuildingRegistry registry) => buildingRegistry = registry;
        public void SetPlacementOrigin(Transform origin) => placementOrigin = origin != null ? origin : transform;
        public void SetBuildingParent(Transform parent) => buildingParent = parent;
        public void SetSelectionPanel(BuildingSelectionPanelUI panel) => selectionPanel = panel;

        public bool SelectItem(string itemId)
        {
            EnsureRuntimeDefinitions();
            selectedDefinition = FindDefinitionForItem(itemId);
            currentValidation = selectedDefinition != null ? PlacementValidationResult.Valid : PlacementValidationResult.Invalid("unknown placeable item");
            if (selectedDefinition == null)
            {
                preview?.Hide();
            }

            return selectedDefinition != null;
        }

        public bool SelectBuilding(string buildingId)
        {
            EnsureRuntimeDefinitions();
            selectedDefinition = FindDefinitionForBuilding(buildingId);
            currentValidation = selectedDefinition != null ? PlacementValidationResult.Valid : PlacementValidationResult.Invalid("unknown placeable building");
            if (selectedDefinition == null)
            {
                preview?.Hide();
            }

            return selectedDefinition != null;
        }

        public string GetSelectedDisplayName()
        {
            return selectedDefinition != null ? selectedDefinition.DisplayName : string.Empty;
        }

        public string GetSelectedHint()
        {
            if (selectedDefinition == null)
            {
                return "No building selected";
            }

            string costStatus = FormatMaterialStatus(selectedDefinition);
            string status = currentValidation.isValid ? "ready" : currentValidation.reason;
            return $"{selectedDefinition.DisplayName} [{costStatus}] - {status}";
        }

        public bool HasSelectedItemInInventory()
        {
            return selectedDefinition != null && HasRequiredMaterials(selectedDefinition);
        }

        public void ClearSelection()
        {
            selectedDefinition = null;
            currentValidation = PlacementValidationResult.Invalid("nothing selected");
            preview?.Hide();
        }

        public bool TryPlace(string itemId)
        {
            return SelectItem(itemId) && TryPlaceSelected();
        }

        public bool TryPlaceSelected()
        {
            ResolveReferences();
            if (selectedDefinition == null)
            {
                return false;
            }

            if (!UpdatePlacementPose())
            {
                currentValidation = PlacementValidationResult.Invalid("no valid ground under cursor");
                Debug.Log($"[BuildingPlacement] Cannot place {selectedDefinition.BuildingId}: {currentValidation.reason}", this);
                return false;
            }
            currentValidation = ValidatePlacement(selectedDefinition, currentPosition, currentRotation);
            if (!currentValidation.isValid)
            {
                Debug.Log($"[BuildingPlacement] Cannot place {selectedDefinition.BuildingId}: {currentValidation.reason}", this);
                return false;
            }

            if (!RuntimeDebugSettings.FreeBuildingEnabled && !HasRequiredMaterials(selectedDefinition))
            {
                currentValidation = PlacementValidationResult.Invalid("missing build materials");
                return false;
            }

            GameObject prefab = ResolvePrefab(selectedDefinition);
            GameObject instance = prefab != null
                ? Instantiate(prefab, currentPosition, currentRotation, buildingParent)
                : PlaceableFallbackFactory.CreateFallback(selectedDefinition.BuildingId, currentPosition, currentRotation, buildingParent);

            if (instance == null)
            {
                currentValidation = PlacementValidationResult.Invalid("spawn failed");
                return false;
            }

            PlaceableStructureRuntime structure = instance.GetComponent<PlaceableStructureRuntime>();
            if (structure == null)
            {
                structure = instance.AddComponent<PlaceableStructureRuntime>();
            }

            structure.Configure(selectedDefinition.BuildingId, null, selectedDefinition.FootprintSize);
            buildingRegistry?.Register(structure);

            if (!RuntimeDebugSettings.FreeBuildingEnabled)
            {
                ConsumeBuildMaterials(selectedDefinition);
            }
            Debug.Log($"[BuildingPlacement] Placed {selectedDefinition.BuildingId}.", instance);

            if (!RuntimeDebugSettings.FreeBuildingEnabled && !HasRequiredMaterials(selectedDefinition))
            {
                ClearSelection();
            }

            return true;
        }

        public PlacementValidationResult ValidatePlacement(PlaceableDefinition definition, Vector3 position, Quaternion rotation)
        {
            if (definition == null)
            {
                return PlacementValidationResult.Invalid("missing definition");
            }

            if (definition.RequireWorldBounds && WorldBounds.Active != null && !WorldBounds.Active.Contains(position))
            {
                return PlacementValidationResult.Invalid("outside world bounds");
            }

            if (definition.RejectWater && IsWater(position))
            {
                return PlacementValidationResult.Invalid("water placement rejected");
            }

            Transform origin = placementOrigin != null ? placementOrigin : transform;
            if (Vector3.Distance(new Vector3(origin.position.x, 0f, origin.position.z), new Vector3(position.x, 0f, position.z)) < definition.MinDistanceFromPlayer)
            {
                return PlacementValidationResult.Invalid("too close to player");
            }

            if (definition.RequireNavMeshSample && !NavMesh.SamplePosition(position, out _, 1.5f, NavMesh.AllAreas))
            {
                return PlacementValidationResult.Invalid("not on navmesh");
            }

            if (definition.BlocksPlacement && HasBlockingOverlap(definition, position, rotation))
            {
                return PlacementValidationResult.Invalid("blocked by another object");
            }

            return PlacementValidationResult.Valid;
        }

        private void ResolveReferences()
        {
            if (inventoryRuntime == null) inventoryRuntime = GetComponent<PlayerInventoryRuntime>();
            if (placementOrigin == null) placementOrigin = transform;
            if (buildingRegistry == null) buildingRegistry = BuildingRegistry.Active;
            if (preview == null) preview = GetComponent<PlacementPreview>() ?? gameObject.AddComponent<PlacementPreview>();
        }

        private void HandleKeyboardShortcuts()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            for (int i = 0; i < quickSelectItems.Length && i < 9; i++)
            {
                Key key = (Key)((int)Key.Digit1 + i);
                if (Keyboard.current[key].wasPressedThisFrame)
                {
                    SelectItem(quickSelectItems[i]);
                }
            }

            if (Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                ClearSelection();
            }

            if (Keyboard.current[toggleMenuKey].wasPressedThisFrame)
            {
                selectionPanel?.ToggleCollapsed();
            }

            if (placeKey == KeyCode.Mouse0)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI())
                {
                    TryPlaceSelected();
                }
                else if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && !IsPointerOverUI())
                {
                    ClearSelection();
                }
            }
            else if (Keyboard.current[(Key)placeKey].wasPressedThisFrame)
            {
                TryPlaceSelected();
            }
        }

        private void UpdatePreview()
        {
            if (selectedDefinition == null || preview == null)
            {
                preview?.Hide();
                return;
            }

            if (!UpdatePlacementPose())
            {
                currentValidation = PlacementValidationResult.Invalid("no valid ground under cursor");
                preview.Hide();
                return;
            }

            currentValidation = ValidateCurrentPlacement(selectedDefinition, currentPosition, currentRotation);
            preview.UpdatePreview(currentPosition, currentRotation, selectedDefinition.FootprintSize, currentValidation.isValid);
        }

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            if (Mouse.current != null)
            {
                return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);
            }

            return EventSystem.current.IsPointerOverGameObject();
        }

        private bool UpdatePlacementPose()
        {
            Transform origin = placementOrigin != null ? placementOrigin : transform;
            if (!TryGetMouseGroundPoint(out Vector3 target))
            {
                Vector3 forward = origin.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f)
                {
                    forward = Vector3.forward;
                }

                target = origin.position + forward.normalized * Mathf.Max(0.5f, placementDistance);
                Vector3 rayOrigin = target + Vector3.up * 20f;
                if (TrySnapToSurface(rayOrigin, out Vector3 snapped))
                {
                    target = snapped;
                }
            }

            currentPosition = target;
            currentRotation = Quaternion.Euler(0f, origin.eulerAngles.y, 0f);
            return WorldBounds.Active == null || WorldBounds.Active.Contains(currentPosition);
        }

        private bool TryGetMouseGroundPoint(out Vector3 point)
        {
            point = default;
            if (Mouse.current == null)
            {
                return false;
            }

            UnityEngine.Camera camera = UnityEngine.Camera.main != null ? UnityEngine.Camera.main : Object.FindAnyObjectByType<UnityEngine.Camera>();
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Max(10f, maxSurfaceRayDistance), surfaceMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (IsPlacementSurfaceHit(hit))
                {
                    point = hit.point;
                    return true;
                }
            }

            Transform origin = placementOrigin != null ? placementOrigin : transform;
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, origin.position.y, 0f));
            if (!groundPlane.Raycast(ray, out float enter))
            {
                return false;
            }

            Vector3 projected = ray.GetPoint(enter);
            if (TrySnapToSurface(projected + Vector3.up * surfaceSnapHeight, out Vector3 snapped))
            {
                point = snapped;
                return true;
            }

            if (WorldBounds.Active != null && !WorldBounds.Active.Contains(projected))
            {
                return false;
            }

            point = projected;
            return true;
        }

        private bool TrySnapToSurface(Vector3 rayOrigin, out Vector3 point)
        {
            point = default;
            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, surfaceSnapHeight * 2f, surfaceMask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                if (IsPlacementSurfaceHit(hit))
                {
                    point = hit.point;
                    return true;
                }
            }

            return false;
        }

        private bool IsPlacementSurfaceHit(RaycastHit hit)
        {
            Collider collider = hit.collider;
            if (collider == null || hit.normal.y < 0.25f)
            {
                return false;
            }

            if (IsIgnoredPlacementCollider(collider))
            {
                return false;
            }

            string objectName = collider.gameObject.name ?? string.Empty;
            if (objectName.StartsWith("Terrain_", System.StringComparison.OrdinalIgnoreCase) ||
                objectName.IndexOf("ground", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                objectName.IndexOf("terrain", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return WorldBounds.Active == null || WorldBounds.Active.Contains(hit.point);
        }

        private bool IsIgnoredPlacementCollider(Collider collider)
        {
            if (collider == null)
            {
                return true;
            }

            if (placementOrigin != null && collider.transform.IsChildOf(placementOrigin))
            {
                return true;
            }

            if (collider.GetComponentInParent<PlacementPreview>() != null)
            {
                return true;
            }

            return collider.GetComponentInParent<PlaceableStructureRuntime>() != null
                   || collider.GetComponentInParent<ResourceNodeView>() != null
                   || collider.GetComponentInParent<CreatureAgentView>() != null
                   || collider.GetComponentInParent<IsometricPlayerController>() != null;
        }

        private bool IsWater(Vector3 position)
        {
            EcosystemDirectorRuntime director = EcosystemDirectorRuntime.Active;
            if (director == null || !director.Initialized)
            {
                return false;
            }

            return string.Equals(director.GetBiomeIdForPosition(position), "water", System.StringComparison.OrdinalIgnoreCase);
        }

        private bool HasBlockingOverlap(PlaceableDefinition definition, Vector3 position, Quaternion rotation)
        {
            Vector3 halfExtents = definition.FootprintSize * 0.5f;
            halfExtents.y = Mathf.Max(0.25f, halfExtents.y * 0.5f);
            Vector3 center = position + Vector3.up * halfExtents.y;
            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, blockingMask, QueryTriggerInteraction.Ignore);
            return hits.Any(IsBlockingCollider);
        }

        private bool IsBlockingCollider(Collider collider)
        {
            if (collider == null || collider.isTrigger)
            {
                return false;
            }

            if (collider.transform.IsChildOf(transform))
            {
                return false;
            }

            string colliderName = collider.gameObject.name;
            if (colliderName.StartsWith("Terrain_", System.StringComparison.OrdinalIgnoreCase) ||
                colliderName.StartsWith("IslandWall", System.StringComparison.OrdinalIgnoreCase) ||
                colliderName.IndexOf("water", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (collider.GetComponentInParent<PlacementPreview>() != null)
            {
                return false;
            }

            return collider.GetComponentInParent<PlaceableStructureRuntime>() != null
                   || collider.GetComponentInParent<ResourceNodeView>() != null
                   || collider.GetComponentInParent<CreatureAgentView>() != null
                   || collider.GetComponentInParent<IsometricPlayerController>() != null;
        }

        private bool IsPlacementSurface(Collider collider)
        {
            if (collider == null)
            {
                return false;
            }

            if (collider.transform.IsChildOf(transform))
            {
                return false;
            }

            string colliderName = collider.gameObject.name;
            if (colliderName.StartsWith("Terrain_", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return collider.GetComponentInParent<PlaceableStructureRuntime>() == null
                   && collider.GetComponentInParent<ResourceNodeView>() == null
                   && collider.GetComponentInParent<CreatureAgentView>() == null
                   && collider.GetComponentInParent<IsometricPlayerController>() == null;
        }

        private int GetInventoryAmount(string itemId)
        {
            if (inventoryRuntime == null || inventoryRuntime.Inventory == null || string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            return inventoryRuntime.Inventory.GetAmount(itemId);
        }

        private PlacementValidationResult ValidateCurrentPlacement(PlaceableDefinition definition, Vector3 position, Quaternion rotation)
        {
            PlacementValidationResult spatial = ValidatePlacement(definition, position, rotation);
            if (!spatial.isValid)
            {
                return spatial;
            }

            return RuntimeDebugSettings.FreeBuildingEnabled || HasRequiredMaterials(definition)
                ? PlacementValidationResult.Valid
                : PlacementValidationResult.Invalid("missing build materials");
        }

        private bool HasRequiredMaterials(PlaceableDefinition definition)
        {
            if (RuntimeDebugSettings.FreeBuildingEnabled)
            {
                return true;
            }

            return definition != null
                   && inventoryRuntime != null
                   && inventoryRuntime.Inventory != null
                   && definition.MaterialCosts.All(cost => cost != null && inventoryRuntime.Inventory.HasItem(cost.ItemId, cost.Amount));
        }

        private string FormatMaterialStatus(PlaceableDefinition definition)
        {
            if (RuntimeDebugSettings.FreeBuildingEnabled)
            {
                return "free";
            }

            if (definition == null || definition.MaterialCosts == null || definition.MaterialCosts.Count == 0)
            {
                return "free";
            }

            bool hasAll = HasRequiredMaterials(definition);
            if (hasAll)
            {
                return definition.BuildCostText;
            }

            return FormatMissingMaterials(definition);
        }

        private string FormatMissingMaterials(PlaceableDefinition definition)
        {
            if (definition == null || definition.MaterialCosts == null || definition.MaterialCosts.Count == 0)
            {
                return "free";
            }

            List<string> missing = new List<string>();
            foreach (PlaceableDefinition.PlaceableBuildCost cost in definition.MaterialCosts)
            {
                if (cost == null || string.IsNullOrWhiteSpace(cost.ItemId))
                {
                    continue;
                }

                int owned = GetInventoryAmount(cost.ItemId);
                if (owned < cost.Amount)
                {
                    missing.Add($"{cost.ItemId} {owned}/{cost.Amount}");
                }
            }

            return missing.Count > 0 ? string.Join(", ", missing) : definition.BuildCostText;
        }

        private void ConsumeBuildMaterials(PlaceableDefinition definition)
        {
            if (definition == null || inventoryRuntime == null || inventoryRuntime.Inventory == null || definition.MaterialCosts == null)
            {
                return;
            }

            foreach (PlaceableDefinition.PlaceableBuildCost cost in definition.MaterialCosts)
            {
                if (cost == null || string.IsNullOrWhiteSpace(cost.ItemId))
                {
                    continue;
                }

                inventoryRuntime.Inventory.RemoveItem(cost.ItemId, cost.Amount);
            }
        }

        private GameObject ResolvePrefab(PlaceableDefinition definition)
        {
            if (definition.Prefab != null)
            {
                return definition.Prefab;
            }

            if (prefabRegistry != null && prefabRegistry.TryGetBuildingPrefab(definition.BuildingId, out GameObject prefab))
            {
                return prefab;
            }

#if UNITY_EDITOR
            if (prefabRegistry != null && prefabRegistry.TryGetBuildingModelPrefab(definition.BuildingId, out GameObject modelPrefab))
            {
                return modelPrefab;
            }

            string normalizedId = definition.BuildingId?.Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(normalizedId))
            {
                string modelPath = $"Assets/apex_shift_placeables_3d_v2_unity_obj/Assets/_Project/Art/Placeables/Models/{normalizedId}_low_poly.obj";
                GameObject editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (editorPrefab != null)
                {
                    return editorPrefab;
                }
            }
#endif

            return null;
        }

        private PlaceableDefinition FindDefinitionForItem(string itemId)
        {
            return definitions.Concat(runtimeDefinitions).FirstOrDefault(definition => definition != null && definition.MatchesItem(itemId));
        }

        private PlaceableDefinition FindDefinitionForBuilding(string buildingId)
        {
            string normalized = string.IsNullOrWhiteSpace(buildingId) ? string.Empty : buildingId.Trim();
            return definitions.Concat(runtimeDefinitions).FirstOrDefault(definition => definition != null && string.Equals(definition.BuildingId, normalized, System.StringComparison.OrdinalIgnoreCase));
        }

        private void EnsureRuntimeDefinitions()
        {
            if (runtimeDefinitions.Count > 0)
            {
                return;
            }

            runtimeDefinitions.Add(PlaceableDefinition.CreateRuntime("storage_box", "Storage Box", PlaceableFallbackFactory.GetDefaultFootprint("storage_box"), Costs(("wood", 4))));
            runtimeDefinitions.Add(PlaceableDefinition.CreateRuntime("campfire", "Campfire", PlaceableFallbackFactory.GetDefaultFootprint("campfire"), Costs(("wood", 3), ("stone", 2))));
            runtimeDefinitions.Add(PlaceableDefinition.CreateRuntime("wall", "Wall", PlaceableFallbackFactory.GetDefaultFootprint("wall"), Costs(("wood", 3))));
            runtimeDefinitions.Add(PlaceableDefinition.CreateRuntime("trap", "Trap", PlaceableFallbackFactory.GetDefaultFootprint("trap"), Costs(("wood", 2), ("fiber", 2))));
            runtimeDefinitions.Add(PlaceableDefinition.CreateRuntime("tent", "Tent", PlaceableFallbackFactory.GetDefaultFootprint("tent"), Costs(("wood", 4), ("fiber", 3))));
        }

        private static IEnumerable<PlaceableDefinition.PlaceableBuildCost> Costs(params (string itemId, int amount)[] costs)
        {
            List<PlaceableDefinition.PlaceableBuildCost> result = new List<PlaceableDefinition.PlaceableBuildCost>();
            foreach ((string itemId, int amount) cost in costs)
            {
                result.Add(PlaceableDefinition.PlaceableBuildCost.Create(cost.itemId, cost.amount));
            }

            return result;
        }
    }
}
