using ApexShift.Runtime.Interaction;
using UnityEngine;

namespace ApexShift.Runtime.World.Interiors
{
    [DisallowMultipleComponent]
    public sealed class BaseEntranceInteractionRuntime : MonoBehaviour, IInteractable
    {
        private SmugglerBaseInteriorRuntime interior;
        public string Prompt => "Enter hidden base";
        public int Priority => 60;
        public float InteractionDuration => 0.4f;
        public void Configure(SmugglerBaseInteriorRuntime runtime)
        {
            interior = runtime;
            var collider = GetComponent<BoxCollider>();
            if (collider == null) collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = Vector3.up;
            collider.size = new Vector3(3f, 3f, 3f);
        }
        public bool CanInteract(GameObject actor) => isActiveAndEnabled && interior != null && interior.CanEnter(actor);
        public bool Interact(GameObject actor) => CanInteract(actor) && interior.TryEnter(actor);
    }
}
