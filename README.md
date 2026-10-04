# Apex Shift

Apex Shift is a Unity 3D isometric survival project.

This repository is the Unity continuation of the original Godot Apex Shift prototype.

Current production world is one procedural tropical island. Cached environment
samples and `HabitatId` drive terrain, vegetation and fauna; `SpeciesDefinition`
and `CreatureRole` define creature behavior and population.

The main story flow is plane crash -> survival -> raft -> failed escape -> human
traces -> smuggler base -> prepare boat -> escape. Story milestones, clues,
interior location and boat requirements persist through save/load; completed
runs restore the completion screen without resuming gameplay.

Legacy/development compatibility includes the Godot reference, handcrafted
`BiomeWorldTest`, old biome profiles and save aliases. These do not define the
production island's environment. Save format remains `1.0.0` with defensive
normalization of missing fields.

Regression coverage and manual smoke checklist:
[Tropical world/story migration validation](Docs/testing/issue106-migration-validation.md).

The existing Godot prototype is a reference for design intent, behavior and balance. It is not a codebase to copy directly or a reason to recreate systems that already exist in Unity.

Migration notes:
- [Migration status matrix](Docs/migration/unity-migration-status.md)
- [Intentional deviations from Godot parity](Docs/migration/intentional-deviations.md)
- [Intentional deviations one-pager](Docs/migration/intentional-deviations-one-pager.md)
- [Unity project foundation](Docs/unity-project-foundation.md)
- [Original Unity migration design document](Docs/migration/unity-migration-design-history.md)
