using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.World.Landmarks;
using UnityEngine;

namespace ApexShift.Runtime.Story.Clues
{
    [DisallowMultipleComponent]
    public sealed class StoryClueRuntime : MonoBehaviour, IInteractable
    {
        private StoryClueDefinition definition;
        [SerializeField] private bool discovered;
        public string ClueId => definition?.ClueId ?? string.Empty;
        public string DisplayName => definition?.DisplayName ?? string.Empty;
        public string InspectionText => definition?.InspectionText ?? string.Empty;
        public string AnchorLandmarkId => definition?.AnchorLandmarkId ?? string.Empty;
        public float VegetationClearanceRadius => definition?.VegetationClearanceRadius ?? 0f;
        public string AdditionalMilestoneId => definition?.AdditionalMilestoneId ?? string.Empty;
        public bool IsDiscovered => discovered;
        public string Prompt => "Inspect " + DisplayName;
        public int Priority => 60;
        public float InteractionDuration => 0.3f;

        public void Configure(StoryClueDefinition data)
        {
            StoryClueRegistry.Unregister(this);
            definition = data ?? throw new System.ArgumentNullException(nameof(data));
            discovered = false;
            if (enabled && gameObject.activeInHierarchy) StoryClueRegistry.Register(this);
        }
        private void OnEnable() => StoryClueRegistry.Register(this);
        private void OnDisable() => StoryClueRegistry.Unregister(this);
        private void OnDestroy() => StoryClueRegistry.Unregister(this);

        public bool CanInteract(GameObject actor)
            => enabled && gameObject.activeInHierarchy && definition != null && actor != null
                && actor.GetComponent<IsometricPlayerController>() != null;
        public bool Interact(GameObject actor)
        {
            if (!CanInteract(actor)) return false;
            Discover();
            GameEventBus.PublishStoryClueInspected(transform.position, ClueId);
            return true;
        }

        public bool Discover()
        {
            if (!enabled || !gameObject.activeInHierarchy || definition == null || discovered) return false;
            bool groupAlreadyFound = StoryClueRegistry.Clues.Any(c => c != this && c.IsDiscovered
                && c.AdditionalMilestoneId == AdditionalMilestoneId);
            discovered = true;
            GameEventBus.PublishStorySignal(StoryMilestoneIds.ClueDiscovered(ClueId), ClueId);
            if (AdditionalMilestoneId.Length > 0 && !groupAlreadyFound)
                GameEventBus.PublishStorySignal(AdditionalMilestoneId, ClueId);
            // Route hints never discover the destination or expose its map marker.
            if (!definition.IsRoute) LandmarkRegistry.FindById(AnchorLandmarkId)?.Discover();
            return true;
        }

        public void SetDiscovered(bool value) => discovered = value;
        public StoryClueSaveData ToSaveData()
        {
            Vector3 p = transform.position;
            return new StoryClueSaveData(ClueId, discovered, p.x, p.y, p.z);
        }
        public void ApplySaveData(StoryClueSaveData data)
        {
            if (data == null || data.ClueId != ClueId) return;
            discovered = data.Discovered;
            transform.position = new Vector3(data.X, data.Y, data.Z);
        }
    }
}
