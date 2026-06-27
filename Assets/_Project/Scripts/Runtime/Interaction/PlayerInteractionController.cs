using System.Collections.Generic;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.UI;
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
        
        private float interactionTimer = 0f;
        private float currentInteractionDuration = 1f;
        private bool isInteracting = false;
        private InteractionProgressUI progressUI;

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
            
            // Create the progress UI
            progressUI = InteractionProgressUI.Create(transform);
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
            CancelInteraction();
            nearbyInteractables.Clear();
            CurrentInteractable = null;
            CurrentPrompt = string.Empty;
        }

        private void Update()
        {
            if (isInteracting)
            {
                UpdateInteraction();
                return;
            }

            RefreshNearbyInteractables();
            UpdateCurrentInteractable();
        }

        private void UpdateInteraction()
        {
            if (CurrentInteractable == null || !CurrentInteractable.CanInteract(gameObject))
            {
                CancelInteraction();
                return;
            }

            // Check if player moved
            if (inputReader != null && inputReader.Move.sqrMagnitude > 0.01f)
            {
                CancelInteraction();
                return;
            }

            interactionTimer += Time.deltaTime;
            if (progressUI != null)
            {
                progressUI.UpdateProgress(interactionTimer / currentInteractionDuration);
            }

            if (interactionTimer >= currentInteractionDuration)
            {
                CompleteInteraction();
            }
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
            if (isInteracting) return;

            RefreshNearbyInteractables();
            UpdateCurrentInteractable();
            
            if (CurrentInteractable != null && CurrentInteractable.CanInteract(gameObject))
            {
                StartInteraction();
            }
            else
            {
                Debug.Log("No interactable resource in range or interaction conditions not met.", this);
            }
        }

        private void StartInteraction()
        {
            if (CurrentInteractable == null) return;

            isInteracting = true;
            interactionTimer = 0f;
            currentInteractionDuration = Mathf.Max(0.1f, CurrentInteractable.InteractionDuration);

            if (progressUI != null)
            {
                progressUI.Show(transform, CurrentPrompt);
            }
            Debug.Log($"[Interaction] Starting interaction with: {CurrentPrompt} (Duration: {currentInteractionDuration}s)");
        }

        private void CompleteInteraction()
        {
            if (CurrentInteractable != null)
            {
                Debug.Log($"[Interaction] Completed interaction with: {CurrentPrompt}");
                CurrentInteractable.Interact(gameObject);
            }
            
            isInteracting = false;
            if (progressUI != null)
            {
                progressUI.Hide();
            }
            
            RefreshNearbyInteractables();
            UpdateCurrentInteractable();
        }

        private void CancelInteraction()
        {
            if (!isInteracting) return;
            
            isInteracting = false;
            if (progressUI != null)
            {
                progressUI.Hide();
            }
            Debug.Log("[Interaction] Interaction cancelled.");
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
