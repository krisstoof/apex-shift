# Unity Project Foundation

**Status:** historical foundation document, refreshed for the current Unity migration delta.  
**Current source of truth:** [`Docs/migration/unity-migration-status.md`](migration/unity-migration-status.md).

This document describes the original Unity foundation goals and the base scene conventions. It is no longer a live checklist of missing gameplay systems.

## Project Goal

Establish the Unity foundation for Apex Shift, a 3D isometric survival game with a clean separation between deterministic core logic and Unity runtime code.

The foundation has since grown beyond a placeholder scene. Current Unity work should use the migration status matrix before creating new tasks, so Codex or a developer does not recreate systems that are already present.

## Related docs

- [Unity migration status matrix](migration/unity-migration-status.md)
- [Intentional deviations from Godot parity](migration/intentional-deviations.md)
- [Intentional deviations one-pager](migration/intentional-deviations-one-pager.md)
- [Original Unity migration design document](../apex_shift_unity_migration_documentation.md)

## Technical Direction

- Unity 3D
- URP / Universal Render Pipeline
- Orthographic isometric camera
- Godot prototype as design/parity reference, not as a code structure to copy 1:1

## Coordinate Convention

- The world plane uses `X/Z`.
- `Y` is height.
- Godot `Vector2.x` maps to Unity `X`.
- Godot `Vector2.y` maps to Unity `Z`.

## Architecture Layers

- Core
- Runtime
- Presentation
- Infrastructure
- Tests

## Rules

- Core must not depend on `UnityEngine`.
- UI must not directly scan the world.
- Runtime adapts Core types to Unity scene objects.
- Do not recreate Godot's large `World` god object.
- Do not open generic migration tasks for systems already marked as `ported` or `partial` in the migration status matrix.

## Current Unity State

As of the migration delta, Unity already contains meaningful implementations or foundations for:

- inventory and item definitions,
- crafting and recipe data,
- resource nodes and regrowth foundations,
- world generation, generated biome regions and biome data,
- ecosystem state and creature needs,
- hunger/diet logic,
- small prey, grazer and Varnak behavior foundations,
- world query / lookup facade,
- save/load DTOs and runtime save service,
- UI snapshot, HUD and debug foundations,
- automated unit/regression tests.

Current gaps are tracked as concrete follow-up issues, not as broad re-porting work:

- day/night runtime and persistence: #41,
- placeable structures: #42,
- storage box container flow: #43,
- player combat runtime: #44,
- torch/campfire protection sources: #45,
- remaining scene scan cleanup: #46,
- Unity PlayMode smoke test: #47,
- Unity tester build: #48,
- balance/species validation: #49.

## Current Non-Goals

- Recreating inventory, crafting, save/load or ecosystem from scratch.
- Copying Godot node hierarchy, groups, singleton patterns or scene scans 1:1.
- Treating `apex-shift-2d` as a direct implementation blueprint instead of a parity/design reference.
- Solving post-v0.1 full evolution/generation complexity before the Unity gameplay loop is stable.

## Suggested Next Work

Use the migration status matrix to pick the next concrete delta issue. The immediate sequence is:

1. Refresh outdated docs (#40).
2. Implement day/night (#41).
3. Implement building placement and storage (#42, #43).
4. Implement player combat and fire protection (#44, #45).
5. Clean remaining scans and add smoke/build validation (#46, #47, #48).

## How to create and test the base playable scene

1. Open the repository in Unity.
2. Wait for compilation.
3. Click `Tools > Apex Shift > Create Base Playable Scene`.
4. Open `Assets/_Project/Scenes/Game.unity`.
5. Press Play.
6. Move the player with `WASD` or arrow keys.
7. Confirm that the ground, player, light and isometric camera are visible.

The base scene started as a placeholder test space:

- `X/Z` is the movement plane.
- `Y` is height.
- It defines the root hierarchy used by later systems.
- Current scenes may contain runtime gameplay systems layered on top of this foundation.

## Base playable scene

- Scene path: `Assets/_Project/Scenes/Game.unity`
- Hierarchy:
  - `Game`
  - `GameBootstrapper`
  - `WorldRoot`
  - `TerrainRoot`
  - `Ground`
  - `ResourceRoot`
  - `CreatureRoot`
  - `BuildingRoot`
  - `Player`
  - `Main Camera`
  - `Directional Light`
  - `UI`
  - `DebugRoot`
- Ground setup:
  - Placeholder object named `Ground`
  - Parent: `WorldRoot/TerrainRoot`
  - Collider: `BoxCollider`
  - Material: `Ground_Test_Material`
- Camera setup:
  - Orthographic projection
  - Position near `(0, 18, -18)`
  - Rotation near `(35.264, 45, 0)`
  - Orthographic size `14`
  - Uses `IsometricCameraFollow` to frame the player
  - Camera script also enforces orthographic mode, size, and isometric rotation at runtime
- Player placeholder:
  - Capsule placeholder above ground
  - Uses `IsometricPlayerController`
  - Moves on `X/Z`
  - Leaves `Y` as height
- Historical out of scope for the first placeholder scene:
  - combat,
  - final art,
  - full procedural world polish,
  - final map/minimap UX,
  - final building/storage/fire gameplay.

### Manual Unity Editor notes

- If needed, add layers manually for `Ground`, `Player`, `Interactable`, `Resource`, `Creature`, and `Building`.
- If the scene builder menu item is not visible, reimport scripts or wait for compilation to finish.
- If the test ground is not visible after generation, rerun the menu item and verify the scene uses the `Assets/_Project/Materials/Ground_Test_Material.mat` asset.
