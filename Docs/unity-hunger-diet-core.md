# Hunger diet core

Issue: `#13 [UNITY] Port HungerDiet core for creature decision making`

This layer is Core-only and depends on the ecosystem foundation from `#21`.

It does not create another runtime ecosystem registry. Instead it uses:

- `FoodKind`
- `CreatureNeedsState`
- `CreatureDietProfile`

from the ecosystem foundation.

## Responsibilities

- tick hunger through `CreatureNeedsState`
- expose a read-only `HungerState`
- choose preferred food type from availability and diet weights
- apply weighted nutrition when eating
- expose behavior parameters derived from hunger stage

## Not included

- GameObject queries
- NavMesh movement
- combat
- hunting
- animations
- full ecosystem simulation
