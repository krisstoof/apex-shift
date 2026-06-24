using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.PlayerInput
{
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset inputActions;

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

        private void Awake()
        {
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

            if (gameplayMap == null)
            {
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
            moveAction.performed -= OnMove;
            moveAction.canceled -= OnMove;
            lookAction.performed -= OnLook;
            lookAction.canceled -= OnLook;
            sprintAction.performed -= OnSprint;
            sprintAction.canceled -= OnSprint;
            interactAction.performed -= OnInteract;
            attackAction.performed -= OnAttack;
            openInventoryAction.performed -= OnOpenInventory;
            openCraftingAction.performed -= OnOpenCrafting;
            toggleMapAction.performed -= OnToggleMap;
            pauseAction.performed -= OnPause;
        }

        private void CacheActions()
        {
            if (inputActions == null)
            {
                return;
            }

            gameplayMap = inputActions.FindActionMap("Gameplay", true);
            moveAction = gameplayMap.FindAction("Move", true);
            lookAction = gameplayMap.FindAction("Look", true);
            interactAction = gameplayMap.FindAction("Interact", true);
            attackAction = gameplayMap.FindAction("Attack", true);
            sprintAction = gameplayMap.FindAction("Sprint", true);
            openInventoryAction = gameplayMap.FindAction("OpenInventory", true);
            openCraftingAction = gameplayMap.FindAction("OpenCrafting", true);
            toggleMapAction = gameplayMap.FindAction("ToggleMap", true);
            pauseAction = gameplayMap.FindAction("Pause", true);
        }

        private void OnMove(InputAction.CallbackContext context) => Move = context.ReadValue<Vector2>();
        private void OnLook(InputAction.CallbackContext context) => LookScreenPosition = context.ReadValue<Vector2>();
        private void OnSprint(InputAction.CallbackContext context) => SprintHeld = context.ReadValueAsButton();
        private void OnInteract(InputAction.CallbackContext context) => InteractPressed?.Invoke();
        private void OnAttack(InputAction.CallbackContext context) => AttackPressed?.Invoke();
        private void OnOpenInventory(InputAction.CallbackContext context) => OpenInventoryPressed?.Invoke();
        private void OnOpenCrafting(InputAction.CallbackContext context) => OpenCraftingPressed?.Invoke();
        private void OnToggleMap(InputAction.CallbackContext context) => ToggleMapPressed?.Invoke();
        private void OnPause(InputAction.CallbackContext context) => PausePressed?.Invoke();
    }
}
