# Hidden smuggler base (#104)

The current `WorldGenerationContext` owns `InteriorRoot` directly below its
`GenerationRoot`. During the existing GenerateLandmarks stage, after production
landmarks and clues, `SmugglerBaseInteriorBuilder.Build` creates one fixed base
and attaches its entrance interaction to the existing `base_entrance` landmark.
The root starts inactive. Normal traversal never regenerates the world.
There is no SceneManager transition, no second world runtime, and no procedural
room generation. The island, player, inventory, survival, story and day/night
instances remain the same throughout ordinary entry and exit.

## Fixed layout

All coordinates below are relative to InteriorRoot. Floors have their surface at
Y=0, solid colliders, 2m walls, and 3m doorways between rooms. There is no ceiling
over the play area; warm point lamps light each room. Temporary stone, timber,
metal and utility props communicate a practical smuggler hideout.

| Room | Center X/Y/Z | Width × length |
| --- | --- | --- |
| EntryTunnel | 0 / 0 / 4 | 4 × 12 |
| StorageRoom | 0 / 0 / 14 | 9 × 8 |
| OperationsRoom | 0 / 0 / 22.5 | 10 × 9 |
| DockChamber | 0 / 0 / 34 | 16 × 14 |

InteriorRoot is placed at topography WorldBounds.max + (220,30,220), physically
separated from island geometry. Its position does not depend on the seed's room
layout; building does not consume Unity Random state.

| Stable anchor | Local X/Y/Z | Room |
| --- | --- | --- |
| EntrySpawn | 0 / 0.1 / 0 | EntryTunnel |
| IslandExitInteraction | 0 / 1 / -1 | EntryTunnel |
| StorageLootAnchor | -3 / 0.1 / 14 | StorageRoom |
| OperationsClueAnchor | -3 / 0.1 / 23.5 | OperationsRoom |
| FuelAnchor | 3 / 0.1 / 15 | StorageRoom |
| BatteryAnchor | 3 / 0.1 / 23.5 | OperationsRoom |
| BoatKeyAnchor | -3 / 0.1 / 20.5 | OperationsRoom |
| BoatAnchor | 3.5 / 0.1 / 36 | DockChamber |

The runtime caches anchor references. `SmugglerBoatPlaceholder` is a child of
BoatAnchor with hull, gunwales, bow, transom, console and engine silhouettes.
It is visual only. #105 owns boat preparation mechanics, production fuel/battery/
key items, repair, controls and escape.

## Traversal

`Enter hidden base` (priority 60, 0.4s) requires the actual generation player,
a discovered entrance, and either GainBaseAccess or the persistent
BaseAccessGained milestone. Earlier discovery alone does not unlock traversal.
Active raft attempts reject entry. Successful entry stores a safe land return
point near the player/entrance, enables InteriorRoot, configures the existing
controller, and teleports to EntrySpawn. Only the first successful entry publishes
`GameEventBus.PublishStorySignal(BaseAccessGained, "base_entrance")`; the story
then advances to PrepareBoat. Re-entry uses the milestone without replaying it.

`Return to island` restores the return point and disables InteriorRoot. A
deterministic ring search near the exterior entrance handles unsafe return
coordinates; the generation's player spawn is the final fallback.

The controller uses a runtime rectangular X/Z movement bounds override instead
of changing WorldBounds.Active. Walls are the primary physical constraint.
Topography-water queries and water-trigger entry are suppressed while inside,
swimming is cleared immediately, and water queries are restored on exit.
Building placement is cleared and disabled inside; its previous enabled state
is restored on exit. Stats and inventory are never reset. Teleport temporarily
disables CharacterController, resets vertical velocity, syncs physics, restores
the controller's enabled state, and calls SetTarget/SnapToTarget on the same camera.
For the default Cinemachine rig it reports the target warp, invalidates damping
history, and updates the existing brain immediately.

## Save and restore

`WorldSaveData.playerLocation` is additive and defaults to island. Persistent
area IDs are strings `island` and `smuggler_base`.

`PlayerLocationSaveData` stores `areaId`, `hasLocalPosition`, `localX/localY/localZ`,
`hasIslandReturnPosition`, and `islandReturnX/islandReturnY/islandReturnZ`.
Inside, local coordinates are relative to InteriorRoot; SurvivalSaveData position
holds the safe island return point. On the island the existing survival position
remains authoritative. Raft safe-return capture retains its existing precedence.

Load regenerates once, restores world objects, clues, story, clock, inventory and
survival stats, then restores player area. Interior restoration activates and
configures the generated base without calling TryEnter or emitting gameplay
signals. It requires the saved access milestone. Invalid/missing area access
falls back to the island with a warning. Missing, nonfinite, out-of-bounds,
unsupported or obstructed local positions use EntrySpawn with a warning.
Old saves without playerLocation restore on the island. Builder calls on the
same context are idempotent; owned generation cleanup removes the entire interior.

RuntimeWorld.unity and Generated assets are outside this implementation's scope.

## Validation (Unity 6000.6.2f1)

Tests ran in an isolated project copy, including production assets and Docs.
The headless runner received a test-only Input System Mouse because existing
DebugUIBounds otherwise falls back to a disabled legacy input API. This fixture
and its assembly reference are confined to the test copy, outside this commit.

- Full EditMode: 221 passed, 0 failed.
- Full PlayMode: 328 passed, 1 failed; both #104 tests passed.
- Comparison with #104 removed: 326 passed, 1 failed (327 total).
- The identical existing failure is
  `CreatureAnimationDriverTests.DriverRaisesBlendStateWhenCreatureChases`:
  expected blend > 0.25, observed 0.0. Full-suite green remains blocked by this
  pre-existing failure; #104 does not claim an entirely green PlayMode suite.

Coverage includes fixed layout and Random-state preservation, early access
rejection, discovery without entry, repeated traversal and single signal,
walkable room connections, immediate camera snap, inventory/survival preservation,
movement overrides, water-trigger suppression, interior save/load, invalid local
coordinates, unknown/inaccessible areas, and old-save defaults.
