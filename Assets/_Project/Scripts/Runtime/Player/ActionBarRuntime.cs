using System;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class ActionBarRuntime : MonoBehaviour
    {
        public static ActionBarRuntime Active { get; private set; }
        [SerializeField] private int slotCount = 9;
        [SerializeField] private bool forceEquipOnSlotSelect = true;
        [SerializeField] private bool rejectNonActionItems = true;
        [SerializeField] private bool autoAssignTestItemsOnAwake = true;
        [SerializeField] private bool autoAssignTestItemsInEditMode = false;

        private PlayerInventoryRuntime inventoryRuntime;
        private ApexShift.Runtime.PlayerInput.PlayerInputReader inputReader;
        private ActionBarState state;

        public ActionBarState State => state;
        public int ActiveSlotIndex => state != null ? state.ActiveSlotIndex : -1;
        public string ActiveItemId => state != null ? state.ActiveItemId : string.Empty;
        public event Action<int, string> ActiveSlotChanged;
        public event Action StateChanged;

        private void Awake()
        {
            EnsureState();
            Active = this;
            state.ActiveSlotChanged += HandleActiveSlotChanged;
            state.Changed += HandleStateChanged;
            if (autoAssignTestItemsOnAwake && (autoAssignTestItemsInEditMode || Application.isPlaying))
            {
                AssignItemToSlot(0, "spear"); AssignItemToSlot(1, "bow"); AssignItemToSlot(2, "axe");
                AssignItemToSlot(3, "pickaxe"); AssignItemToSlot(4, "torch");
            }
        }

        private void OnEnable() { Active = this; SubscribeToInput(); }
        private void OnDisable() => UnsubscribeFromInput();

        private void OnDestroy()
        {
            UnsubscribeFromInput();
            if (state != null)
            {
                state.ActiveSlotChanged -= HandleActiveSlotChanged;
                state.Changed -= HandleStateChanged;
            }
            if (Active == this) Active = null;
        }

        public void SetInventoryRuntime(PlayerInventoryRuntime runtime) => inventoryRuntime = runtime;

        public void SetInputReader(ApexShift.Runtime.PlayerInput.PlayerInputReader reader)
        {
            UnsubscribeFromInput(); inputReader = reader; SubscribeToInput();
        }

        public bool SetActiveSlot(int slotIndex) { EnsureState(); return state.SetActiveSlot(slotIndex); }
        public void ClearActiveSlot() { EnsureState(); state.ClearActiveSlot(); }
        public bool IsSlotActive(int slotIndex) => ActiveSlotIndex == slotIndex;
        public string GetAssignedItemInSlot(int slotIndex) { EnsureState(); return state.GetItem(slotIndex); }

        public bool AssignItemToSlot(int slotIndex, string itemId)
        {
            EnsureState();
            if (rejectNonActionItems && !IsActionBarItem(itemId)) return false;
            return state.AssignItem(slotIndex, itemId);
        }

        public void SetSlotCountForTests(int count)
        {
            if (state != null)
            {
                state.ActiveSlotChanged -= HandleActiveSlotChanged;
                state.Changed -= HandleStateChanged;
            }
            slotCount = Mathf.Clamp(count, 1, 9);
            state = new ActionBarState(slotCount, rejectNonActionItems);
            state.ActiveSlotChanged += HandleActiveSlotChanged;
            state.Changed += HandleStateChanged;
        }

        private void EnsureState()
        {
            if (state == null) state = new ActionBarState(Mathf.Clamp(slotCount, 1, 9), rejectNonActionItems);
        }

        private void HandleActiveSlotChanged(int index, string itemId)
        {
            ActiveSlotChanged?.Invoke(index, itemId);
            if (!forceEquipOnSlotSelect) return;
            PlayerHeldItemRuntime held = GetComponent<PlayerHeldItemRuntime>() ?? gameObject.AddComponent<PlayerHeldItemRuntime>();
            held.SetActionBarRuntime(this); held.SetInventoryRuntime(inventoryRuntime); held.ForceEquipActionItem(itemId);
        }

        private void HandleStateChanged() => StateChanged?.Invoke();
        private void SubscribeToInput() { if (inputReader != null) { inputReader.ActionSlotPressed -= OnActionSlotPressed; inputReader.ActionSlotPressed += OnActionSlotPressed; } }
        private void UnsubscribeFromInput() { if (inputReader != null) inputReader.ActionSlotPressed -= OnActionSlotPressed; }
        private void OnActionSlotPressed(int slotIndex) => SetActiveSlot(slotIndex);
        public static bool IsActionBarItem(string itemId) => ActionBarItemPolicy.IsActionBarItem(itemId);
        public static bool IsBlockedResourceItem(string itemId) => ActionBarItemPolicy.IsBlockedResourceItem(itemId);
    }
}
