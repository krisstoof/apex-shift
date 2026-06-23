using System.Collections.Generic;
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
        [SerializeField] private bool showDebugOverlay;
        [SerializeField] private Rect panelRect = new Rect(12f, 348f, 380f, 120f);

        private readonly List<IInteractable> nearbyInteractables = new List<IInteractable>();
        private Collider[] overlapResults;
        private const int PanelWindowId = 431074;

        public IInteractable CurrentInteractable { get; private set; }
        public string CurrentPrompt { get; private set; } = string.Empty;

        private void Awake()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (interactionOrigin == null) interactionOrigin = transform;
            overlapResults = new Collider[Mathf.Max(1, maxOverlapResults)];
        }

        private void OnEnable()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (inputReader != null) inputReader.InteractPressed += TryInteract;
        }

        private void OnDisable()
        {
            if (inputReader != null) inputReader.InteractPressed -= TryInteract;
        }

        private void Update()
        {
            RefreshNearbyInteractables();
            UpdateCurrentInteractable();
        }

        private void OnGUI()
        {
            if (!showDebugOverlay) return;
            panelRect = GUI.Window(PanelWindowId, panelRect, DrawWindowContents, "Interaction");
        }

        public void SetInputReader(PlayerInputReader reader) => inputReader = reader;
        public void SetInteractionOrigin(Transform origin) => interactionOrigin = origin != null ? origin : transform;

        private void TryInteract()
        {
            RefreshNearbyInteractables();
            UpdateCurrentInteractable();
            if (CurrentInteractable != null && CurrentInteractable.CanInteract(gameObject))
            {
                CurrentInteractable.Interact(gameObject);
            }
        }

        private void RefreshNearbyInteractables()
        {
            nearbyInteractables.Clear();
            Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
            Collider[] colliders = Physics.OverlapSphere(origin, interactionRadius, interactionMask, QueryTriggerInteraction.Collide);
            foreach (Collider collider in colliders)
            {
                if (collider == null) continue;
                MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>(true);
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

        private void DrawWindowContents(int windowId)
        {
            GUILayout.Label(string.IsNullOrEmpty(CurrentPrompt) ? "No interactable target." : CurrentPrompt);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
        }
    }
}
