# Unity Project Foundation

## Project Goal

Establish the initial Unity foundation for Apex Shift, a 3D isometric survival game with a clean separation between deterministic core logic and Unity runtime code.

## Technical Direction

- Unity 3D
- URP / Universal Render Pipeline
- Orthographic isometric camera

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

## Current Non-Goals

- No procedural world yet.
- No ecosystem yet.
- No inventory yet.
- No crafting yet.
- No Varnaks yet.
- No save/load yet.

## Suggested Next Issue

- Port inventory and item model into `ApexShift.Core`.

## How to create and test the base playable scene

1. Open the repository in Unity.
2. Wait for compilation.
3. Click `Tools > Apex Shift > Create Base Playable Scene`.
4. Open `Assets/_Project/Scenes/Game.unity`.
5. Press Play.
6. Move the player with `WASD` or arrow keys.
7. Confirm that the ground, player, light and isometric camera are visible.

The scene is a placeholder test space:

- `X/Z` is the movement plane.
- `Y` is height.
- No gameplay systems are included yet.
- Inventory, crafting, resources, AI, Varnaks, ecosystem, save/load, procedural world generation, Unity Terrain, NavMesh and final UI are intentionally out of scope.

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
- Still placeholder:
  - No inventory
  - No crafting
  - No resources
  - No creatures
  - No ecosystem
  - No save/load
- Out of scope:
  - No procedural world generation
  - No NavMesh setup
  - No combat
  - No final art

### Manual Unity Editor notes

- If needed, add layers manually for `Ground`, `Player`, `Interactable`, `Resource`, `Creature`, and `Building`.
- If the scene builder menu item is not visible, reimport scripts or wait for compilation to finish.
- If the test ground is not visible after generation, rerun the menu item and verify the scene uses the `Assets/_Project/Materials/Ground_Test_Material.mat` asset.
