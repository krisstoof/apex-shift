# Environmental smuggler clue chain (#103)

This adds evidence inspection between raft failure and physical discovery of the
hidden entrance. No journal, waypoint, compass arrow, clue map pins, door/key
gameplay or final escape is added. #102 raft behavior is unchanged.

## Production clues

| Stable clue ID | Display | Anchor / location | Clearance |
| --- | --- | --- | --- |
| smuggler_cache_manifest | Shipping Markings | 1–3.5 units from smuggler_cache | 2.5 |
| smuggler_camp_evidence | Abandoned Equipment | 1.5–4.5 units from smuggler_camp | 3.0 |
| smuggler_route_fragment | Marked Route | 30–60% toward base_entrance from camp, safe local search | 2.0 |

Definitions and inspection text have a single source in
`Runtime/Story/Clues/StoryClueDefinition.cs`:

- Shipping Markings: “These containers are too recent to have washed ashore
  years ago. The same shipping marks appear on several crates. Someone used
  this place regularly.” This establishes human presence.
- Abandoned Equipment: “Fuel cans, radio parts and cut rope were left behind.
  This looks like a working camp, not the shelter of a stranded traveler.”
  This suggests organized transport and repeated activity.
- Marked Route: “A marked route continues inland toward the rocky high ground.
  One annotation refers to a lower entrance.” This suggests exploration, not GPS.

The small crate/label, can/radio/rope and map/stake visuals are temporary runtime
primitives, not final art. No downloaded assets, Blender generation or saved
scene content is involved. A 1.2-unit root trigger is found by normal player
interaction; priority 60, duration 0.3 seconds.

## Placement and ownership

Clues are generated immediately after landmarks, before vegetation, under
`GenerationRoot/LandmarkRoot/StoryClues`. Existing landmark positions and
placement rules are untouched. Seed plus stable clue ID controls a deterministic
ring search. The planner only queries cached environment data, never global
Unity Random. Final Y uses the same authoritative surface-height callback as
landmarks and terrain.

All candidates must be land, not water, inside the physical environment grid,
with slope at most 22°. Route candidates also exclude shoreline and must be
closer to the base than camp, on its facing side, and more than 16 units from
the entrance to avoid prematurely triggering its proximity discovery. A failed
search along the route falls back to safe points 4–12 units from camp facing
inward. Missing anchors or safe placements produce an explicit warning.

Crash separation: cache >=40, camp >=55, route >=55 units. Clue clearances join
the existing VegetationLandmarkClearance list; no separate vegetation algorithm
or broad clearing is introduced. Clear/regenerate destroys owned objects and
clears registry entries. Clues are not added to map/minimap registries.

## Story and inspection

First Discover publishes `clue_discovered:<trimmed-lowercase-id>` through the
existing GameEventBus. Cache or camp evidence also publishes `human_traces_found`;
the second evidence does not republish the shared group milestone.

`investigate_human_traces + human_traces_found -> locate_smuggler_base` replaces
the former cache/camp proximity conditions. Landmark proximity still discovers
landmarks, but is not proof of inspected evidence. The existing
`locate_smuggler_base + landmark_discovered:base_entrance -> gain_base_access`
rule is unchanged. Inspecting any/all clues never discovers base_entrance.

Out-of-order inspection is retained in the normal milestone set. Evidence found
at BuildRaft does not skip raft gameplay; after raft failure the existing
reevaluation engine advances through investigation without reinspection.

Repeated Interact always emits the appended `StoryClueInspected` event, carrying
the clue ID in subjectId, but Discover returns false and does not replay story
signals. StoryClueHUDView resolves registered data and shows title/text at the
bottom center for seven real-time seconds. The menu shell and unknown IDs remain
hidden; subscriptions are removed on disable/destruction. There is no world
polling or direct runtime-to-presentation reference.

## Save/load

The additive Core DTO StoryClueSaveData stores ID, discovered flag and XYZ in
`WorldSaveData.clueStates`. Capture is stable-ID ordered. GameSaveService restores
landmarks, then clue state, then story state after normal world regeneration.
Restore updates existing generated objects by ID; it never spawns duplicates or
calls Discover, inspection events or story signals. Unknown saved IDs warn and
are skipped. Old saves without clueStates load with three generated,
undiscovered clues; no null exception occurs. Story state is restored separately,
without reevaluation or transition replay.

## Validation baseline

Unity 6000.6.2f1: full EditMode **219/219 PASS**, full PlayMode **327/327 PASS**.
Production generation was checked on seeds **12345, 81281, 91284**, including
Generate -> Clear -> Generate identity/position determinism and tree clearances.
The acceptance test uses normal PlayerInteractionController physics selection,
timed interaction and the provisioned inspection HUD. Save/load covers both
current clue state and an old save without clueStates, with zero completion,
inspection or stage-transition replay.

Seed 12345 (XZ distances, units):

| Clue ID | Position XYZ | Crash distance |
| --- | --- | --- |
| smuggler_cache_manifest | (25.013, 0.868, 26.160) | 64.116 |
| smuggler_camp_evidence | (-6.973, 0.679, -27.142) | 72.154 |
| smuggler_route_fragment | (19.856, 0.000, -15.658) | 78.908 |

Camp -> base: **64.622**; route clue -> base: **36.774**.
Local XML reports: `Logs/Issue103/EditMode.xml`, `Logs/Issue103/PlayMode.xml`
(ignored). RuntimeWorld, generated animation and the pre-existing paused time
setting are preserved from the start of this task. No manual playthrough is claimed.
