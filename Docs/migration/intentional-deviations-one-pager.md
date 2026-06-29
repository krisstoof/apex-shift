# Intentional deviations one-pager

This is the quick-read companion to `docs/migration/intentional-deviations.md`.

## Must match Godot

- Hunger stages and diet intent.
- Small prey, grazer and Varnak behavior at gameplay level.
- Resource identity, depletion, regrowth and food availability.
- Ecosystem biomass, populations, generations and save/load state.
- Gameplay events that drive AI, resource flow and debug visibility.

## Adapted for Unity 3D

- 2D steering becomes 3D transforms, NavMesh and adapters.
- Scene-tree/group lookup becomes typed registries and `WorldQueryRuntime`.
- Visuals become prefabs, renderers, colliders and materials.
- Save/load becomes typed DTOs instead of Godot dictionaries.
- Debug UI is cached/throttled to avoid stutter.
- Balance values move into `ScriptableObject` assets.

## Technical fallbacks

- Missing `GameBalanceConfig` falls back to defaults for empty scenes/tests.
- Missing listeners do not break the event bus.
- Primitive fallback meat drops exist before final art.
- Debug overlays may be hidden in normal play.

## Not required for v0.1

- Full evolution/adaptation tree.
- Literal Godot implementation details when Unity already has an accepted equivalent.

## Review rule

- If it is not listed here or in the full doc, treat it as a suspected migration bug.
