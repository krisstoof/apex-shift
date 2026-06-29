# Unity migration status matrix

**Issue:** #39 - `[MIGRATION] Update Unity migration status matrix`  
**Repo:** `krisstoof/apex-shift`  
**Reference project:** `krisstoof/apex-shift-2d`  
**Status date:** 2026-06-29

This document is the current migration status matrix for the Unity version of Apex Shift. It is meant to prevent duplicate migration work by separating:

- systems already ported to Unity,
- systems that are present but still partial,
- intentional Unity-side deviations from the Godot prototype,
- missing runtime features,
- features postponed outside the current migration delta.

It complements #37 (`[MIGRATION] Verify intentional deviations from Godot and document them`). #37 should explain *why* a difference exists; this file records *what state each migrated system is currently in*.

---

## Status legend

| Status | Meaning |
| --- | --- |
| `ported` | Unity has a working implementation or foundation that replaces the Godot system for the current milestone. |
| `partial` | Unity has meaningful code/data, but parity is incomplete or runtime integration is not finished. |
| `intentional deviation` | Unity intentionally differs from Godot because the target is 3D/isometric, data-driven, or architecturally cleaner. |
| `missing` | No meaningful Unity runtime implementation was found. |
| `postponed` | Not required for the current migration delta; keep as future work. |

---

## Summary

| Area | Current status | Main follow-up |
| --- | --- | --- |
| Core item/inventory/crafting | `ported` | polish and UX only |
| Resources and regrowth | `partial` | #27, #38, #46 |
| World generation and biomes | `partial` | #49, later procedural/world polish |
| Ecosystem and creature needs | `partial` | #28, #35, #49 |
| Small prey / grazer / Varnak AI | `partial` | #29, #30, #31, #35 |
| Save/load | `partial` | #32, #41, #42, #43 |
| Debug/UI snapshots | `ported` | #33, #40 |
| Minimap/map | `partial` | later UX task |
| Day/night | `missing` | #41 |
| Player combat | `missing` | #44 |
| Torch/campfire runtime | `partial` | #45 |
| Buildings/storage runtime | `missing` / `partial` | #42, #43 |
| Tester build | `missing` | #48 |
| Automated tests | `partial` | #35, #47 |

---

## Detailed matrix

| System | Status | Unity evidence | Godot reference / issue | Notes |
| --- | --- | --- | --- | --- |
| Inventory | `ported` | [`Assets/_Project/Scripts/Core/Inventory/ItemStack.cs`](../../Assets/_Project/Scripts/Core/Inventory/ItemStack.cs), [`Assets/_Project/Scripts/Core/Inventory/InventorySlot.cs`](../../Assets/_Project/Scripts/Core/Inventory/InventorySlot.cs), [`Assets/_Project/Scripts/Runtime/Player/PlayerInventoryRuntime.cs`](../../Assets/_Project/Scripts/Runtime/Player/PlayerInventoryRuntime.cs) | #2 | Core inventory exists as C# logic and is connected to player runtime. Do not create another inventory task. |
| Item definitions | `ported` | [`Assets/_Project/Scripts/Core/Items/ItemDatabase.cs`](../../Assets/_Project/Scripts/Core/Items/ItemDatabase.cs), [`Assets/_Project/Scripts/Infrastructure/Data/Items/ItemDefinitionAsset.cs`](../../Assets/_Project/Scripts/Infrastructure/Data/Items/ItemDefinitionAsset.cs), [`Assets/_Project/Data/Items`](../../Assets/_Project/Data/Items) | #2, #4 | Item data is represented through Core models and Unity assets. |
| Crafting | `ported` | [`Assets/_Project/Scripts/Core/Crafting/CraftingSystem.cs`](../../Assets/_Project/Scripts/Core/Crafting/CraftingSystem.cs), [`Assets/_Project/Scripts/Core/Crafting/RecipeDatabase.cs`](../../Assets/_Project/Scripts/Core/Crafting/RecipeDatabase.cs), [`Assets/_Project/Scripts/Runtime/Player/PlayerCraftingRuntime.cs`](../../Assets/_Project/Scripts/Runtime/Player/PlayerCraftingRuntime.cs) | #3, #4 | Crafting has Core logic and player runtime integration. Missing work should be UI/UX or new recipes, not another core port. |
| Recipe data | `ported` | [`Assets/_Project/Scripts/Infrastructure/Data/Recipes/RecipeDefinitionAsset.cs`](../../Assets/_Project/Scripts/Infrastructure/Data/Recipes/RecipeDefinitionAsset.cs), [`Assets/_Project/Data/Recipes`](../../Assets/_Project/Data/Recipes) | #3, #4 | Recipes exist as data assets. Balance validation belongs to #49. |
| Resource nodes | `partial` | [`Assets/_Project/Scripts/Runtime/Resources/ResourceNodeView.cs`](../../Assets/_Project/Scripts/Runtime/Resources/ResourceNodeView.cs), [`Assets/_Project/Scripts/Core/Resources`](../../Assets/_Project/Scripts/Core/Resources) | #8, #27, #38 | Harvesting, depletion, food-source bridge and events exist. Full Godot `ResourceNode` parity and Embersstorm prefab cleanup remain follow-up work. |
| Resource regrowth | `partial` | [`Assets/_Project/Scripts/Runtime/Resources/ResourceNodeView.cs`](../../Assets/_Project/Scripts/Runtime/Resources/ResourceNodeView.cs), [`Assets/_Project/Scripts/Tests/Unit/Resources/ResourceRegrowthSystemTests.cs`](../../Assets/_Project/Scripts/Tests/Unit/Resources/ResourceRegrowthSystemTests.cs) | #9, #27, #46 | Regrowth exists, but remaining scene scans should be moved to registries in #46. |
| World generation | `partial` | [`Assets/_Project/Scripts/Runtime/World/Generation/WorldGeneratorRuntime.cs`](../../Assets/_Project/Scripts/Runtime/World/Generation/WorldGeneratorRuntime.cs), [`Assets/_Project/Scripts/Runtime/World/Generation/WorldGenerationResult.cs`](../../Assets/_Project/Scripts/Runtime/World/Generation/WorldGenerationResult.cs) | #10, #11, #20 | Unity has a generator/vertical slice, roots, player/camera spawn, NavMesh build and creature spawning. It is not a 1:1 Godot world port. |
| Biomes | `ported` | [`Assets/_Project/Scripts/Core/World/Biomes/BiomeDefinition.cs`](../../Assets/_Project/Scripts/Core/World/Biomes/BiomeDefinition.cs), [`Assets/_Project/Data/Biomes/BiomeCatalog.asset`](../../Assets/_Project/Data/Biomes/BiomeCatalog.asset), [`Assets/_Project/Scripts/Runtime/World/Generation/GeneratedBiomeRegion.cs`](../../Assets/_Project/Scripts/Runtime/World/Generation/GeneratedBiomeRegion.cs) | #10, #11, #20 | Biome definitions and generated regions exist. Balance and density tuning belong to #49 or a later world-polish task. |
| Landmarks | `missing` | Docs only: [`apex_shift_unity_migration_documentation.md`](../../apex_shift_unity_migration_documentation.md) | Godot: `scripts/world/world_config.gd`, `WorldConfig.LANDMARKS` | No clear Unity runtime landmark system was found. Keep this as a concrete missing area instead of another generic world task. |
| Water / ponds | `partial` | [`Assets/_Project/Data/Biomes/water.asset`](../../Assets/_Project/Data/Biomes/water.asset), [`Assets/_Project/Materials/Biomes/Water_Material.mat`](../../Assets/_Project/Materials/Biomes/Water_Material.mat) | Godot: `World.is_position_in_water`, pond landmarks | Unity has water biome/data/material assets, but no full Godot-style pond landmark and water-query parity was found. |
| Ecosystem state | `partial` | [`Assets/_Project/Scripts/Runtime/Ecosystem/EcosystemDirectorRuntime.cs`](../../Assets/_Project/Scripts/Runtime/Ecosystem/EcosystemDirectorRuntime.cs), [`Assets/_Project/Scripts/Core/Ecosystem/BiomeEcosystemState.cs`](../../Assets/_Project/Scripts/Core/Ecosystem/BiomeEcosystemState.cs), [`Assets/_Project/Scripts/Runtime/Ecosystem/EcosystemRuntime.cs`](../../Assets/_Project/Scripts/Runtime/Ecosystem/EcosystemRuntime.cs) | #21, #22, #28 | Biome ecosystem state exists and can be saved/restored. Full parity and balance validation remain covered by #28, #35 and #49. |
| Creature needs | `ported` | [`Assets/_Project/Scripts/Runtime/Ecosystem/CreatureNeedsRuntime.cs`](../../Assets/_Project/Scripts/Runtime/Ecosystem/CreatureNeedsRuntime.cs), [`Assets/_Project/Scripts/Core/Ecosystem/CreatureNeedsState.cs`](../../Assets/_Project/Scripts/Core/Ecosystem/CreatureNeedsState.cs) | #21, #22 | Runtime needs and Core state exist. |
| Hunger/diet | `ported` | [`Assets/_Project/Scripts/Core/Ecosystem/HungerDietSystem.cs`](../../Assets/_Project/Scripts/Core/Ecosystem/HungerDietSystem.cs), [`Assets/_Project/Scripts/Core/Ecosystem/DietProfile.cs`](../../Assets/_Project/Scripts/Core/Ecosystem/DietProfile.cs), [`Assets/_Project/Scripts/Tests/Unit/Ecosystem/HungerDietSystemTests.cs`](../../Assets/_Project/Scripts/Tests/Unit/Ecosystem/HungerDietSystemTests.cs) | #13, #24 | Core hunger/diet model and tests exist. Further work should be parity/balance, not re-porting. |
| Small prey behavior | `partial` | [`Assets/_Project/Scripts/Runtime/Creatures/CreatureBehaviorBrain.cs`](../../Assets/_Project/Scripts/Runtime/Creatures/CreatureBehaviorBrain.cs), [`Assets/_Project/Scripts/Tests/Regression/SmallPreyBehaviorParityTests.cs`](../../Assets/_Project/Scripts/Tests/Regression/SmallPreyBehaviorParityTests.cs) | #22, #29, #35 | Behavior exists in shared brain. Keep parity validation in #29/#35. |
| Grazer behavior | `partial` | [`Assets/_Project/Scripts/Runtime/Creatures/CreatureBehaviorBrain.cs`](../../Assets/_Project/Scripts/Runtime/Creatures/CreatureBehaviorBrain.cs) | #22, #30, #35 | Grazer plant preference, scavenging and emergency predation are present, but parity should remain tracked by #30/#35. |
| Varnak behavior | `partial` | [`Assets/_Project/Scripts/Runtime/Creatures/CreatureBehaviorBrain.cs`](../../Assets/_Project/Scripts/Runtime/Creatures/CreatureBehaviorBrain.cs), [`Assets/_Project/Scripts/Runtime/Creatures/CreatureHealthRuntime.cs`](../../Assets/_Project/Scripts/Runtime/Creatures/CreatureHealthRuntime.cs) | #22, #31, #41, #44, #45 | Varnak can stalk/chase/attack, but night, fire/torch/campfire and final player-combat integration are still missing/partial. |
| Creature navigation | `ported` | [`Assets/_Project/Scripts/Runtime/Creatures/CreatureAgentView.cs`](../../Assets/_Project/Scripts/Runtime/Creatures/CreatureAgentView.cs), [`Assets/_Project/Scripts/Runtime/Creatures/CreatureWanderBehavior.cs`](../../Assets/_Project/Scripts/Runtime/Creatures/CreatureWanderBehavior.cs), [`Assets/_Project/Scripts/Runtime/Creatures/CreatureSimulationLodRuntime.cs`](../../Assets/_Project/Scripts/Runtime/Creatures/CreatureSimulationLodRuntime.cs) | #12, #25 | Unity intentionally uses NavMesh/agent-style movement rather than Godot 2D movement. This is an intentional engine adaptation. |
| World query / spatial lookup | `partial` | [`Assets/_Project/Scripts/Runtime/World/Query/WorldQueryRuntime.cs`](../../Assets/_Project/Scripts/Runtime/World/Query/WorldQueryRuntime.cs), [`Assets/_Project/Scripts/Tests/Unit/World/WorldQueryRuntimeTests.cs`](../../Assets/_Project/Scripts/Tests/Unit/World/WorldQueryRuntimeTests.cs) | #26, #46 | A stable query facade exists. It still needs registry/spatial backing cleanup so save/regrowth/debug do not rely on scene scans. |
| Save/load | `partial` | [`Assets/_Project/Scripts/Runtime/Save/GameSaveService.cs`](../../Assets/_Project/Scripts/Runtime/Save/GameSaveService.cs), [`Assets/_Project/Scripts/Core/Save/GameSaveData.cs`](../../Assets/_Project/Scripts/Core/Save/GameSaveData.cs), [`Assets/_Project/Scripts/Core/Save/WorldSaveData.cs`](../../Assets/_Project/Scripts/Core/Save/WorldSaveData.cs) | #14, #32, #41, #42, #43 | Save/load covers inventory, survival, resources, biome ecosystem state and creature state. Day/night and buildings/storage remain missing follow-ups. |
| Debug panel | `ported` | [`Assets/_Project/Scripts/Runtime/UI/DebugPanelPresenter.cs`](../../Assets/_Project/Scripts/Runtime/UI/DebugPanelPresenter.cs), [`Assets/_Project/Scripts/Runtime/Debugging/WorldMapDebugWindow.cs`](../../Assets/_Project/Scripts/Runtime/Debugging/WorldMapDebugWindow.cs), [`Assets/_Project/Scripts/Runtime/Creatures/CreatureDebugOverlay.cs`](../../Assets/_Project/Scripts/Runtime/Creatures/CreatureDebugOverlay.cs) | #15, #33, #40 | Unity debug foundation exists. #40 should refresh outdated docs around this. |
| UI snapshot | `ported` | [`Assets/_Project/Scripts/Runtime/UI/GameSnapshot.cs`](../../Assets/_Project/Scripts/Runtime/UI/GameSnapshot.cs), [`Assets/_Project/Scripts/Runtime/UI/GameSnapshotProvider.cs`](../../Assets/_Project/Scripts/Runtime/UI/GameSnapshotProvider.cs), [`Assets/_Project/Scripts/Runtime/UI/InventorySnapshot.cs`](../../Assets/_Project/Scripts/Runtime/UI/InventorySnapshot.cs) | #15, #33 | Snapshot model exists and should remain the source for HUD/debug instead of direct world scans. |
| HUD | `ported` | [`Assets/_Project/Scripts/Presentation/HUD/PlayerHUDController.cs`](../../Assets/_Project/Scripts/Presentation/HUD/PlayerHUDController.cs), [`Assets/_Project/Scripts/Presentation/HUD/RuntimeHUDProvisioner.cs`](../../Assets/_Project/Scripts/Presentation/HUD/RuntimeHUDProvisioner.cs), [`Assets/_Project/Scripts/Presentation/HUD/PlayerSurvivalOverlay.cs`](../../Assets/_Project/Scripts/Presentation/HUD/PlayerSurvivalOverlay.cs) | #15, #18 | Runtime HUD exists. Polish should be separate from migration foundation. |
| Minimap / map screen | `partial` | [`Assets/_Project/Scripts/Presentation/HUD/MiniMapUI.cs`](../../Assets/_Project/Scripts/Presentation/HUD/MiniMapUI.cs), [`Assets/_Project/Scripts/Runtime/Debugging/WorldMapDebugWindow.cs`](../../Assets/_Project/Scripts/Runtime/Debugging/WorldMapDebugWindow.cs) | Godot: `scripts/ui/minimap.gd`, `scripts/ui/map_screen.gd` | Minimap exists, but a final map screen equivalent is not clearly present. Current full-map work appears debug-oriented. |
| Day/night | `missing` | Not found as Unity runtime code. | #41 | Add `DayNightRuntime`, persistence and AI/debug hooks in #41. |
| Player combat | `missing` | Not found as dedicated player combat runtime. | #44 | Add spear, bow/projectile and damage flow in #44. Creature combat behavior already exists separately. |
| Torch/campfire | `partial` | [`Assets/_Project/Data/Items/torch.asset`](../../Assets/_Project/Data/Items/torch.asset), [`Assets/_Project/Data/Recipes/torch.asset`](../../Assets/_Project/Data/Recipes/torch.asset), [`Assets/_Project/Data/Items/campfire.asset`](../../Assets/_Project/Data/Items/campfire.asset), [`Assets/_Project/Data/Recipes/campfire.asset`](../../Assets/_Project/Data/Recipes/campfire.asset), [`Assets/_Project/Scripts/Runtime/Player/PlayerSurvivalRuntime.cs`](../../Assets/_Project/Scripts/Runtime/Player/PlayerSurvivalRuntime.cs) | #31, #41, #45 | Data and some fire-related events exist, but real fire-source gameplay objects and Varnak protection radius are still #45. |
| Buildings | `missing` | Item/recipe data only for some building-like items. | #42 | Placement, validation, spawned structures and building save/load are missing. |
| Storage box | `partial` | [`Assets/_Project/Data/Items/storage_box.asset`](../../Assets/_Project/Data/Items/storage_box.asset), [`Assets/_Project/Data/Recipes/storage_box.asset`](../../Assets/_Project/Data/Recipes/storage_box.asset) | #42, #43 | Item and recipe exist, but storage container runtime and transfer UI are missing. |
| Tester build | `missing` | No Unity tester package evidence in code/docs. | #48 | Separate Unity tester package should be produced; Godot tester packages do not close this. |
| Automated tests | `partial` | [`Assets/_Project/Scripts/Tests`](../../Assets/_Project/Scripts/Tests), [`Assets/_Project/Scripts/Tests/ApexShift.Tests.asmdef`](../../Assets/_Project/Scripts/Tests/ApexShift.Tests.asmdef) | #17, #35, #47 | Unit/regression tests exist. A PlayMode vertical-slice smoke test is still #47. |

---

## Older migration issues covered by current implementation

The following older issues should be treated as already represented in the current Unity codebase and should not be duplicated as fresh migration tasks:

- #1 - Unity project foundation.
- #2 - item definitions and inventory core.
- #3 - crafting core and recipe model.
- #4 - ScriptableObject data layer for items and recipes.
- #5 - Unity Input System action maps.
- #6 - Cinemachine/isometric camera foundation.
- #7 - survival stats core and player runtime adapter.
- #8 - resource interaction and harvesting foundation.
- #9 - resource regrowth core.
- #10 - world and biome data foundation.
- #11 - world generation vertical slice.
- #12 - creature navigation foundation.
- #13 - HungerDiet core.
- #14 - save/load DTO foundation.
- #15 - UI snapshot and debug foundation.
- #16 - prefab loading strategy.
- #17 - automated tests for ported Core systems.
- #18 - first playable gather/craft/survive slice.
- #19 - base playable scene.
- #20 - handcrafted biome world identity.
- #21 - ecosystem biomass and creature needs foundation.
- #22 - animals and ecosystem behavior migration foundation.
- #23 - Godot-to-Unity parity audit.
- #24 - HungerDiet parity.
- #25 - creature simulation LOD/background simulation.
- #26 - world query/spatial lookup semantics.
- #27 - ResourceNode behavior parity foundation.
- #28 - EcosystemDirector biome state model.
- #29 - SmallPrey behavior parity foundation.
- #30 - Grazer behavior parity foundation.
- #31 - Varnak behavior parity foundation.
- #32 - save/load parity for resources, creatures and ecosystem.
- #33 - debug data, overlays and AI counters.
- #34 - EventBus gameplay events and message parity.
- #35 - Godot parity tests for migrated systems.
- #36 - GameBalance and species data reconciliation.

These issues may still have polish or follow-up work, but their broad migration scope is no longer a reason to create another generic porting issue.

---

## Current migration delta issues

These issues represent the remaining concrete migration-delta work after the broad port:

- #37 - intentional deviations from Godot.
- #38 - Embersstorm resource prefab replacement.
- #40 - refresh outdated Unity foundation documentation.
- #41 - Unity day/night runtime and persistence.
- #42 - placeable structures runtime.
- #43 - storage box inventory flow.
- #44 - player combat runtime.
- #45 - torch and campfire protection sources.
- #46 - replace remaining scene scans with registries.
- #47 - Unity PlayMode smoke test.
- #48 - Unity migration tester build.
- #49 - balance and species data validation.
- #50 - close migration delta and define next milestone.

---

## Practical next order

1. Finish this matrix (#39).
2. Refresh outdated docs (#40), especially files that still say inventory/crafting/ecosystem/save do not exist.
3. Implement missing runtime foundations: day/night (#41), placement/buildings (#42), storage (#43), player combat (#44), torch/campfire (#45).
4. Clean remaining scene scans (#46).
5. Add PlayMode smoke (#47).
6. Prepare tester build (#48).
7. Validate balance (#49).
8. Close migration delta (#50).

---

## Notes for Codex / future issue generation

Do not generate new generic tasks named:

- `Port inventory to Unity`,
- `Port crafting to Unity`,
- `Add resource nodes`,
- `Add ecosystem director`,
- `Add save/load foundation`,
- `Add creature AI foundation`,
- `Add debug panel foundation`,
- `Add unit tests for inventory/crafting`.

Those areas already exist in Unity. New tasks should target the remaining concrete gaps listed above or polish specific behavior with a named owner, expected result and test path.
