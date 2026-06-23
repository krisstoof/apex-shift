using System.Collections.Generic;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;

namespace ApexShift.Runtime.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private Transform interactionOrigin;
        [SerializeField] private float interactionRadius = 2.25f;
        [SerializeField] private LayerMask interactionMask = Physics.DefaultRaycastLayers;
        [SerializeField] private int maxOverlapResults = 16;
        private readonly List<IInteractable> nearbyInteractables = new List<IInteractable>();
        private Collider[] overlapResults;
        private bool subscribed;

        public IInteractable CurrentInteractable { get; private set; }
        public string CurrentPrompt { get; private set; } = string.Empty;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (interactionOrigin == null)
            {
                interactionOrigin = transform;
            }

            overlapResults = new Collider[Mathf.Max(1, maxOverlapResults)];
        }

        private void OnEnable()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
            nearbyInteractables.Clear();
            CurrentInteractable = null;
            CurrentPrompt = string.Empty;
        }

        private void Update()
        {
            RefreshNearbyInteractables();
            UpdateCurrentInteractable();
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

        public void SetInteractionOrigin(Transform origin)
        {
            interactionOrigin = origin != null ? origin : transform;
        }

        public void TryInteract()
        {
            RefreshNearbyInteractables();
            UpdateCurrentInteractable();
            if (CurrentInteractable != null && CurrentInteractable.CanInteract(gameObject))
            {
                CurrentInteractable.Interact(gameObject);
                RefreshNearbyInteractables();
                UpdateCurrentInteractable();
                return;
            }

            Debug.Log("No interactable resource in range.", this);
        }

        private void SubscribeInput()
        {
            if (subscribed || inputReader == null)
            {
                return;
            }

            inputReader.InteractPressed += TryInteract;
            subscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!subscribed || inputReader == null)
            {
                subscribed = false;
                return;
            }

            inputReader.InteractPressed -= TryInteract;
            subscribed = false;
        }

        private void RefreshNearbyInteractables()
        {
            nearbyInteractables.Clear();
            Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;

            if (overlapResults == null || overlapResults.Length != Mathf.Max(1, maxOverlapResults))
            {
                overlapResults = new Collider[Mathf.Max(1, maxOverlapResults)];
            }

            int count = Physics.OverlapSphereNonAlloc(
                origin,
                Mathf.Max(0.1f, interactionRadius),
                overlapResults,
                interactionMask,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = overlapResults[i];
                overlapResults[i] = null;
                if (hit == null)
                {
                    continue;
                }

                MonoBehaviour[] behaviours = hit.GetComponentsInParent<MonoBehaviour>(true);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is IInteractable interactable && !nearbyInteractables.Contains(interactable))
                    {
                        nearbyInteractables.Add(interactable);
                    }
                }
            }
        }

        private void UpdateCurrentInteractable()
        {
            CurrentInteractable = null;
            CurrentPrompt = string.Empty;
            int highestPriority = int.MinValue;
            foreach (IInteractable interactable in nearbyInteractables)
            {
                if (interactable == null || !interactable.CanInteract(gameObject)) continue;
                if (interactable.Priority > highestPriority)
                {
                    highestPriority = interactable.Priority;
                    CurrentInteractable = interactable;
                    CurrentPrompt = interactable.Prompt;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Transform origin = interactionOrigin != null ? interactionOrigin : transform;
            Gizmos.DrawWireSphere(origin.position, Mathf.Max(0.1f, interactionRadius));
        }
    }
}
