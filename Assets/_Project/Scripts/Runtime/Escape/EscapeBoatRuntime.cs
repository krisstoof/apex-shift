using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Flow;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Interiors;
using UnityEngine;

namespace ApexShift.Runtime.Escape
{
    [DisallowMultipleComponent]
    public sealed class EscapeBoatRuntime : MonoBehaviour, IInteractable
    {
        public const string SubjectId = "smuggler_boat";
        private const string ObjectiveOwner = "escape_boat";
        private WorldGenerationContext context;
        private SmugglerBaseInteriorRuntime interior;
        private GameObject player;
        private StoryProgressionRuntime story;
        private readonly List<BoatRequirementSourceRuntime> sources = new List<BoatRequirementSourceRuntime>();
        private Coroutine escapeRoutine;
        private bool movementWasEnabled;
        private bool refreshing;
        private bool interacting;
        public PlayerInventoryRuntime Inventory { get; private set; }
        public IReadOnlyList<BoatRequirementSourceRuntime> Sources => sources.AsReadOnly();
        public EscapeBoatState State { get; private set; } = EscapeBoatState.Unavailable;
        public bool EscapeInProgress { get; private set; }
        public string Prompt => State == EscapeBoatState.Ready && IsPrepared ? "Escape island"
            : !IsDiscovered ? "Inspect boat" : State == EscapeBoatState.Ready ? "Install fuel and battery" : "Inspect boat requirements";
        public int Priority => 60;
        public float InteractionDuration => 0.4f;
        private bool IsDiscovered => story != null && story.HasMilestone(EscapeBoatMilestoneIds.BoatDiscovered);
        private bool IsPrepared => story != null && story.HasMilestone(StorySignalIds.BoatPrepared);
        private bool IsCompleted => story != null && (story.CurrentStageId == StoryStageIds.Completed || story.HasMilestone(StorySignalIds.IslandEscaped));

        public void Configure(WorldGenerationContext generation, SmugglerBaseInteriorRuntime baseInterior)
        { context = generation; interior = baseInterior; }
        public void RegisterSource(BoatRequirementSourceRuntime source)
        { if (source != null && !sources.Contains(source)) sources.Add(source); }

        public void BindPlayer(GameObject actor)
        {
            CancelEscape();
            Unsubscribe();
            player = actor;
            Inventory = player != null ? player.GetComponent<PlayerInventoryRuntime>() : null;
            Inventory?.EnsureInitialized();
            story = context?.StoryProgression;
            if (Inventory != null) Inventory.Inventory.InventoryChanged += Refresh;
            if (story != null) story.StateChanged += Refresh;
            Refresh();
        }
        private void Unsubscribe()
        {
            if (Inventory != null) Inventory.Inventory.InventoryChanged -= Refresh;
            if (story != null) { story.StateChanged -= Refresh; story.ClearObjectiveDetail(ObjectiveOwner); }
        }
        private void OnEnable() => Refresh();
        private void OnDisable() => CancelEscape();
        private void OnDestroy() { CancelEscape(); Unsubscribe(); }

        public bool CanUseSource(GameObject actor) => actor != null && actor == player && actor == context?.Player && Inventory != null
            && interior != null && interior.IsInsidePlayer(actor) && !EscapeInProgress && !IsCompleted;
        public bool CanInteract(GameObject actor) => isActiveAndEnabled && !interacting && CanUseSource(actor)
            && story != null && State != EscapeBoatState.Unavailable && State != EscapeBoatState.Escaped;
        public bool Interact(GameObject actor)
        {
            if (!CanInteract(actor)) return false;
            interacting = true;
            try
            {
                if (!IsDiscovered && !IsPrepared)
                {
                    GameEventBus.PublishStorySignal(EscapeBoatMilestoneIds.BoatDiscovered, SubjectId);
                    Refresh(); return true;
                }
                if (!IsPrepared)
                {
                    var cost = BoatRequirementDefinition.Production.ToDictionary(r => r.ItemId, _ => 1);
                    if (!Inventory.Inventory.TryConsumeItems(cost)) { Refresh(); return false; }
                    GameEventBus.PublishStorySignal(StorySignalIds.BoatPrepared, SubjectId);
                    Refresh(); return true;
                }
                if (story.CurrentStageId != StoryStageIds.EscapeIsland) return false;
                EscapeInProgress = true;
                var controller = player.GetComponent<IsometricPlayerController>();
                movementWasEnabled = controller.MovementEnabled;
                controller.SetMovementEnabled(false);
                GameEventBus.PublishFinalEscapeStarted(transform.position);
                escapeRoutine = StartCoroutine(FinalEscape());
                return true;
            }
            finally { interacting = false; }
        }
        private IEnumerator FinalEscape()
        {
            yield return new WaitForSecondsRealtime(2f);
            if (!IsCompleted && story != null && story.CurrentStageId == StoryStageIds.EscapeIsland)
                GameEventBus.PublishStorySignal(StorySignalIds.IslandEscaped, SubjectId);
            escapeRoutine = null;
            EscapeInProgress = false;
            if (!IsCompleted && player != null) player.GetComponent<IsometricPlayerController>().SetMovementEnabled(movementWasEnabled);
            Refresh();
        }
        private void CancelEscape()
        {
            if (!EscapeInProgress) return;
            if (escapeRoutine != null) StopCoroutine(escapeRoutine);
            escapeRoutine = null; EscapeInProgress = false;
            if (!IsCompleted && player != null) player.GetComponent<IsometricPlayerController>()?.SetMovementEnabled(movementWasEnabled);
        }
        public void Refresh()
        {
            if (refreshing || story == null) return;
            refreshing = true;
            try
            {
                if (IsCompleted)
                {
                    State = EscapeBoatState.Escaped;
                    story.ClearObjectiveDetail(ObjectiveOwner);
                    player?.GetComponent<IsometricPlayerController>()?.SetMovementEnabled(false);
                    GameSessionState.EndGameplay(); Time.timeScale = 0f;
                }
                else if (IsPrepared)
                { State = EscapeBoatState.Ready; story.ClearObjectiveDetail(ObjectiveOwner); }
                else if (story.CurrentStageId != StoryStageIds.PrepareBoat)
                { State = EscapeBoatState.Unavailable; story.ClearObjectiveDetail(ObjectiveOwner); }
                else if (!IsDiscovered)
                { State = EscapeBoatState.Discovered; story.ClearObjectiveDetail(ObjectiveOwner); }
                else
                {
                    var missing = BoatRequirementDefinition.Production.Where(r => Inventory == null || Inventory.Inventory.GetAmount(r.ItemId) < 1).ToArray();
                    State = missing.Length == 0 ? EscapeBoatState.Ready : EscapeBoatState.MissingRequirements;
                    story.SetObjectiveDetail(ObjectiveOwner, missing.Length == 0
                        ? "Return to the boat and install the fuel and battery."
                        : "Missing: " + string.Join(", ", missing.Select(r => r.DisplayName)) + ".");
                }
            }
            finally { refreshing = false; }
        }
        public EscapeBoatSaveData CaptureSaveData() => new EscapeBoatSaveData {
            collectedRequirementSourceIds = sources.Where(s => s.IsCollected).Select(s => s.SourceId)
                .OrderBy(id => id, System.StringComparer.Ordinal).ToList()
        };
        public void RestoreSaveData(EscapeBoatSaveData data)
        {
            CancelEscape();
            var collected = new HashSet<string>((data ?? EscapeBoatSaveData.Default).CollectedRequirementSourceIds);
            foreach (var source in sources) source.RestoreCollected(collected.Contains(source.SourceId));
            Refresh();
        }
    }
}
