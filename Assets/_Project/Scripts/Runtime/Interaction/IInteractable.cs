using UnityEngine;

namespace ApexShift.Runtime.Interaction
{
    public interface IInteractable
    {
        string Prompt { get; }
        int Priority { get; }
        bool CanInteract(GameObject actor);
        bool Interact(GameObject actor);
    }
}
