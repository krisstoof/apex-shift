using ApexShift.Runtime.Interaction;
using UnityEngine;

namespace ApexShift.Runtime.Escape
{
    [DisallowMultipleComponent]
    public sealed class BoatRequirementSourceRuntime : MonoBehaviour, IInteractable
    {
        private EscapeBoatRuntime boat;
        private BoatRequirementDefinition requirement;
        private bool collecting;
        public bool IsCollected { get; private set; }
        public string SourceId => requirement.SourceId;
        public string ItemId => requirement.ItemId;
        public string Prompt => "Take " + requirement.DisplayName;
        public int Priority => 50;
        public float InteractionDuration => 0.15f;
        public void Configure(EscapeBoatRuntime runtime, BoatRequirementDefinition definition)
        { boat = runtime; requirement = definition; }
        public bool CanInteract(GameObject actor) => isActiveAndEnabled && !collecting && !IsCollected
            && boat != null && boat.CanUseSource(actor) && boat.Inventory.Inventory.CanAddItem(ItemId, 1);
        public bool Interact(GameObject actor)
        {
            if (!CanInteract(actor)) return false;
            collecting = true;
            try
            {
                // Capture from InventoryChanged already sees collection as completed.
                IsCollected = true;
                if (boat.Inventory.Inventory.AddItem(ItemId, 1) != 0) { IsCollected = false; return false; }
                gameObject.SetActive(false);
                return true;
            }
            finally { collecting = false; }
        }
        public void RestoreCollected(bool value)
        {
            IsCollected = value;
            gameObject.SetActive(!value);
        }
    }
}
