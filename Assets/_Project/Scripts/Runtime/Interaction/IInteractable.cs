using UnityEngine;

namespace ApexShift.Runtime.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }
        int Priority { get; }
        float InteractionDuration { get; }
        bool CanInteract(GameObject actor);
        bool Interact(GameObject actor);
    }
}
