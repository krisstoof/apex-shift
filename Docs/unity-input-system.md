# Unity Input System

Canonical InputActionAsset:

`Assets/_Project/Input/ApexShiftInputActions.inputactions`

`Gameplay` is the only gameplay action map. `PlayerInputReader` does not
fall back to the legacy `Player` map or the removed template asset. The
global `InputSystem.actions` configuration points to this same canonical
asset, and `Assets/_Project/Scenes/RuntimeWorld.unity` is the canonical
production/startup scene. It contains the runtime world generator, authored
terrain/resources, creatures, player, camera and survival HUD. `Game.unity`
remains the base playable prototype scene used by `ApexShiftSceneBuilder`, not
the production world build scene.

Project-owned runtime content is kept under `_Project`: audio is in
`Assets/_Project/Audio`, placeable models in
`Assets/_Project/Art/Placeables/Models`, and icon resources in
`Assets/_Project/Resources/ApexShift2D/Art/Icons`. The old Unity template and
import wrappers are not runtime asset locations.

## Gameplay actions

- Move
- Look
- Interact
- Attack
- Sprint
- OpenInventory
- OpenCrafting
- ToggleMap
- Pause

## UI actions

- Navigate
- Submit
- Cancel

`PlayerInputReader` is the only runtime gameplay component that reads Unity Input System actions directly.

Player movement stays camera-relative and uses the generated `WorldBounds`.

## Developer diagnostics

`RuntimeDiagnosticsBootstrap` owns optional developer diagnostics. It is gated by
`UNITY_EDITOR || DEVELOPMENT_BUILD` and `RuntimeDebugSettings.DeveloperDiagnosticsEnabled`.
Without that switch, `GameSnapshotProvider` keeps auto refresh disabled and the
runtime does not create debug presenters, creature overlays, or action logging.
Free building and crafting use the same master diagnostic gate. `FpsCounterUI`
remains part of the tester-facing HUD and is intentionally not developer-only.

When enabled, the bootstrap attaches `PlayerActionDebugLog` to the active player.

The overlay shows recent actions:

- Move Started
- Move Stopped
- Sprint Started
- Sprint Stopped
- Interact
- Attack
- Open Inventory
- Open Crafting
- Toggle Map
- Pause

The overlay is drawn with `OnGUI`, so it works in Play Mode without TextMeshPro or UI Toolkit.
It listens to `PlayerInputReader` events and values only, and it can optionally mirror entries to the Console.
It also supports:

- `F1` to toggle the overlay
- `F2` to reset the panel position
- dragging the window by its title bar
- `Clear` to remove the current log entries
- `Reset Position` to move the panel back to the default corner

## Current action behavior

Input actions are wired to debug feedback first.

At this stage:

- Move controls player movement.
- Sprint increases movement speed.
- Interact, Attack, Open Inventory, Open Crafting, Toggle Map and Pause produce debug and visual placeholder feedback.
- Final gameplay systems for those actions are implemented in later issues.

## Animation status

The current prototype supports two animation feedback paths:

1. Animator Controller path:
   - `Speed`
   - `IsMoving`
   - `IsSprinting`
   - `Attack`
   - `Interact`

2. Visual fallback path:
   - subtle movement bob when moving,
   - faster bob when sprinting.

This keeps player feedback visible even when final animation clips are not ready.

## Manual test

1. Open Unity.
2. Open `Assets/_Project/Scenes/RuntimeWorld.unity`.
3. Press Play.
4. Move with WASD.
5. Move with arrow keys.
6. Confirm movement is camera-relative.
7. Move mouse and confirm player rotates toward cursor.
8. Press E and confirm no error.
9. Press I, C, M, Escape and confirm no error.
10. Confirm player cannot leave world bounds.
11. Confirm the debug overlay shows recent player actions in the top-left corner.
12. Press `F1` and confirm the overlay hides and shows again.
13. Press `F2` or click `Reset Position` and confirm the panel returns to the default corner.
