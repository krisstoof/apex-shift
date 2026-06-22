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

