# Persistent story progression (#101)

#101 is framework only: no raft, survival detector, keys, interior, boat,
rewards or ending cinematic are implemented. World placement is unchanged.

## Central production definition

Stable string IDs, objective texts and rules live in
`Runtime/Story/StoryProgressionDefinition.cs`. The HUD never defines objectives.

| Current stage ID | Required milestone | Next stage ID |
| --- | --- | --- |
| survive_crash | crash_survived | establish_survival |
| establish_survival | survival_established | build_raft |
| build_raft | raft_built | attempt_raft_escape |
| attempt_raft_escape | raft_escape_failed | investigate_human_traces |
| investigate_human_traces | landmark_discovered:smuggler_cache | locate_smuggler_base |
| investigate_human_traces | landmark_discovered:smuggler_camp | locate_smuggler_base |
| locate_smuggler_base | landmark_discovered:base_entrance | gain_base_access |
| gain_base_access | base_access_gained | prepare_boat |
| prepare_boat | boat_prepared | escape_island |
| escape_island | island_escaped | completed |

Cache **or** camp is sufficient. Discovering the entrance does not grant access.
`completed` has no outgoing rule. A generated, already-discovered plane crash
does not imply `crash_survived`: every new generation starts at `survive_crash`
with no milestones.

## Events and persistent milestones

`LandmarkRuntime.Discover()` publishes one `LandmarkDiscovered` event on the
existing `GameEventBus`, using its position and normalized landmark ID.
Repeated discovery and landmark save restoration do not publish discovery.
Landmarks have no direct dependency on story runtime.

`StoryProgressionRuntime` subscribes while enabled and unsubscribes on disable
or destruction. It handles only discovery and story-signal events.
`StoryMilestoneIds.LandmarkDiscovered(id)` builds
`landmark_discovered:<trimmed-lowercase-id>`.

Milestones form an independent, permanent HashSet, not transient triggers.
`Signal(id)` returns true only for a new nonempty milestone; duplicates are
no-ops. StageChanged fires once per transition and MilestoneCompleted once per
new milestone. StateChanged notifies presentation after evaluation.

Out-of-order cache/entrance discoveries are retained. Once preceding milestones
arrive, rules reevaluate repeatedly and may traverse several stages without
rediscovery. A stage-count-based limit prevents a malformed cycle from hanging.

## Generation ownership and save/load

Each `WorldGenerationContext.StoryProgression` belongs to that generation's
GenerationRoot. Clear/regenerate destroys the old runtime and bus subscription.
No persistent singleton or DontDestroyOnLoad is used.

`WorldSaveData.storyState` contains this additive, version-compatible DTO:

```json
{
  "currentStageId": "locate_smuggler_base",
  "completedMilestoneIds": [
    "crash_survived",
    "landmark_discovered:smuggler_cache",
    "raft_built",
    "raft_escape_failed",
    "survival_established"
  ]
}
```

Capture sorts IDs ordinally. Old saves without storyState default to
`survive_crash` and an empty milestone list. Core DTOs do not depend on Runtime.
GameSaveService prefers the current generation's runtime, with a standalone
fallback when no generator exists.

After generation and landmark restoration, RestoreSaveData restores the saved
stage and normalized/deduplicated milestones **without reevaluating rules**.
Unknown stages warn and fall back to the initial stage; valid unknown milestones
are retained. Restore publishes no gameplay completion events, StageChanged or
MilestoneCompleted, only one presentation StateChanged.

## Objective HUD

RuntimeHUDProvisioner creates a top-center ObjectivePanel, hidden in the menu
shell. ObjectiveHUDView binds explicitly to the current generation, displays
CurrentObjectiveText immediately, and updates through StateChanged, not polling.
Disable, unbind and destruction remove subscriptions; enabling refreshes state.
Load updates the already-bound HUD after restoring the story.

## Future producer API (#102–#105)

Gameplay producers use the existing bus, without finding the story runtime:

```csharp
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Story;

// Future #102 raft construction / failed attempt:
GameEventBus.PublishStorySignal(StorySignalIds.RaftBuilt);
GameEventBus.PublishStorySignal(StorySignalIds.RaftEscapeFailed);

// Future #104 actual access:
GameEventBus.PublishStorySignal(StorySignalIds.BaseAccessGained);

// Future #105 boat readiness / completed escape:
GameEventBus.PublishStorySignal(StorySignalIds.BoatPrepared);
GameEventBus.PublishStorySignal(StorySignalIds.IslandEscaped);
```

An optional subjectId identifies the originating object; it is not the persistent
milestone key. CrashSurvived and SurvivalEstablished are also accepted stable
signals, but this issue provides no automatic producers. #103 can rely on
existing landmark discovery; no clue chain or journal is added.
