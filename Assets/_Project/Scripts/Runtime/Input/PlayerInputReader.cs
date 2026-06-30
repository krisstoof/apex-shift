using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.PlayerInput
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset inputActions;

        [SerializeField]
        private ApexShift.Runtime.Buildings.BuildingPlacementRuntime buildingPlacementRuntime;

        public Vector2 Move { get; private set; }
        public Vector2 LookScreenPosition { get; private set; }
        public bool SprintHeld { get; private set; }

        public event System.Action InteractPressed;
        public event System.Action AttackPressed;
        public event System.Action OpenInventoryPressed;
        public event System.Action OpenCraftingPressed;
        public event System.Action ToggleMapPressed;
        public event System.Action PausePressed;

        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction interactAction;
        private InputAction attackAction;
        private InputAction sprintAction;
        private InputAction openInventoryAction;
        private InputAction openCraftingAction;
        private InputAction toggleMapAction;
        private InputAction pauseAction;

        public void SetInputActions(InputActionAsset actions)
        {
            inputActions = actions;
            CacheActions();
        }

        public void SetBuildingPlacementRuntime(ApexShift.Runtime.Buildings.BuildingPlacementRuntime runtime)
        {
            buildingPlacementRuntime = runtime;
        }

        private void Awake()
        {
            if (buildingPlacementRuntime == null)
            {
                buildingPlacementRuntime = GetComponent<ApexShift.Runtime.Buildings.BuildingPlacementRuntime>();
            }

            if (inputActions == null)
            {
                inputActions = InputSystem.actions;
                if (inputActions != null) Debug.Log("[Input] Fallback to InputSystem.actions", this);
            }
            CacheActions();
        }

        private void OnEnable()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("PlayerInputReader is missing InputActionAsset. Input will not work until one is assigned.", this);
                return;
            }

            if (gameplayMap == null)
            {
                CacheActions();
            }

            if (!HasRequiredActions())
            {
                Debug.LogWarning("PlayerInputReader is missing required Player action map/actions. Input will not work until a complete InputActionAsset is assigned.", this);
                return;
            }

            moveAction.performed += OnMove;
            moveAction.canceled += OnMove;
            lookAction.performed += OnLook;
            lookAction.canceled += OnLook;
            sprintAction.performed += OnSprint;
            sprintAction.canceled += OnSprint;
            interactAction.performed += OnInteract;
            attackAction.performed += OnAttack;
            openInventoryAction.performed += OnOpenInventory;
            openCraftingAction.performed += OnOpenCrafting;
            toggleMapAction.performed += OnToggleMap;
            pauseAction.performed += OnPause;

            inputActions.Enable();
        }

        private void OnDisable()
        {
            if (inputActions == null || gameplayMap == null)
            {
                return;
            }

            inputActions.Disable();
            if (moveAction != null) moveAction.performed -= OnMove;
            if (moveAction != null) moveAction.canceled -= OnMove;
            if (lookAction != null) lookAction.performed -= OnLook;
            if (lookAction != null) lookAction.canceled -= OnLook;
            if (sprintAction != null) sprintAction.performed -= OnSprint;
            if (sprintAction != null) sprintAction.canceled -= OnSprint;
            if (interactAction != null) interactAction.performed -= OnInteract;
            if (attackAction != null) attackAction.performed -= OnAttack;
            if (openInventoryAction != null) openInventoryAction.performed -= OnOpenInventory;
            if (openCraftingAction != null) openCraftingAction.performed -= OnOpenCrafting;
            if (toggleMapAction != null) toggleMapAction.performed -= OnToggleMap;
            if (pauseAction != null) pauseAction.performed -= OnPause;
        }

        private void CacheActions()
        {
            if (inputActions == null)
            {
                return;
            }

            gameplayMap = inputActions.FindActionMap("Player", false);
            moveAction = gameplayMap?.FindAction("Move", false);
            lookAction = gameplayMap?.FindAction("Look", false);
            interactAction = gameplayMap?.FindAction("Interact", false);
            attackAction = gameplayMap?.FindAction("Attack", false);
            sprintAction = gameplayMap?.FindAction("Sprint", false);
            openInventoryAction = gameplayMap?.FindAction("OpenInventory", false);
            openCraftingAction = gameplayMap?.FindAction("OpenCrafting", false);
            toggleMapAction = gameplayMap?.FindAction("ToggleMap", false);
            pauseAction = gameplayMap?.FindAction("Pause", false);
        }

        private bool HasRequiredActions()
        {
            return gameplayMap != null
                   && moveAction != null
                   && lookAction != null
                   && interactAction != null
                   && attackAction != null
                   && sprintAction != null
                   && openInventoryAction != null
                   && openCraftingAction != null
                   && toggleMapAction != null
                   && pauseAction != null;
        }

        private void OnMove(InputAction.CallbackContext context) => Move = context.ReadValue<Vector2>();
        private void OnLook(InputAction.CallbackContext context)
        {
            LookScreenPosition = context.ReadValue<Vector2>();
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[Input] Look Position: {LookScreenPosition}");
            }
        }
        private void OnSprint(InputAction.CallbackContext context) => SprintHeld = context.ReadValueAsButton();
        private void OnInteract(InputAction.CallbackContext context)
        {
            Debug.Log("[Input] Interact Key Pressed!");
            InteractPressed?.Invoke();
        }
        private void OnAttack(InputAction.CallbackContext context)
        {
            if (buildingPlacementRuntime == null)
            {
                buildingPlacementRuntime = GetComponent<ApexShift.Runtime.Buildings.BuildingPlacementRuntime>();
            }

            if (buildingPlacementRuntime != null && buildingPlacementRuntime.BlocksPlayerPrimaryAction)
            {
                return;
            }

            AttackPressed?.Invoke();
        }
        private void OnOpenInventory(InputAction.CallbackContext context) => OpenInventoryPressed?.Invoke();
        private void OnOpenCrafting(InputAction.CallbackContext context) => OpenCraftingPressed?.Invoke();
        private void OnToggleMap(InputAction.CallbackContext context) => ToggleMapPressed?.Invoke();
        private void OnPause(InputAction.CallbackContext context) => PausePressed?.Invoke();
    }
}
