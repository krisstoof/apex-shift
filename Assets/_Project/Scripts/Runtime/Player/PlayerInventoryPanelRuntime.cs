using ApexShift.Runtime.PlayerInput;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInventoryPanelRuntime : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private PlayerInventoryRuntime inventoryRuntime;

        private bool subscribed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeInput();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current == null)
            {
                return;
            }

            if (UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame)
            {
                InventoryPanelUI.Open(inventoryRuntime);
            }
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            if (inputReader == reader)
            {
                return;
            }

            UnsubscribeInput();
            inputReader = reader;
            if (enabled)
            {
                SubscribeInput();
            }
        }

        public void SetInventoryRuntime(PlayerInventoryRuntime runtime)
        {
            inventoryRuntime = runtime;
        }

        private void ResolveReferences()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (inventoryRuntime == null)
            {
                inventoryRuntime = GetComponent<PlayerInventoryRuntime>();
            }
        }

        private void SubscribeInput()
        {
            if (subscribed || inputReader == null)
            {
                return;
            }

            inputReader.OpenInventoryPressed += OnOpenInventoryPressed;
            subscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!subscribed || inputReader == null)
            {
                subscribed = false;
                return;
            }

            inputReader.OpenInventoryPressed -= OnOpenInventoryPressed;
            subscribed = false;
        }

        private void OnOpenInventoryPressed()
        {
            ResolveReferences();
            InventoryPanelUI.Open(inventoryRuntime);
        }
    }
}
