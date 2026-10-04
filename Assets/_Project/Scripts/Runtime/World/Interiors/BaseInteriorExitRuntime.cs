using ApexShift.Runtime.Interaction;
using UnityEngine;

namespace ApexShift.Runtime.World.Interiors
{
    [DisallowMultipleComponent]
    public sealed class BaseInteriorExitRuntime : MonoBehaviour, IInteractable
    {
        private SmugglerBaseInteriorRuntime interior;
        public string Prompt => "Return to island";
        public int Priority => 60;
        public float InteractionDuration => 0.4f;
        public void Configure(SmugglerBaseInteriorRuntime runtime) => interior = runtime;
        public bool CanInteract(GameObject actor) => isActiveAndEnabled && interior != null && interior.IsInsidePlayer(actor);
        public bool Interact(GameObject actor) => CanInteract(actor) && interior.TryExit(actor);
    }
}
