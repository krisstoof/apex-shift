# Intentional deviations from Godot parity

Status: migration decision log for Unity v0.1  
Source of truth baseline: Godot repository `krisstoof/apex-shift-2d`  
Target implementation: Unity repository `krisstoof/apex-shift`

Current migration status matrix: [`Docs/migration/unity-migration-status.md`](unity-migration-status.md)

This document exists so that migration review, QA and Codex work can separate real parity bugs from intentional Unity changes.

## Decision labels

| Label | Meaning |
|---|---|
| `must match Godot` | Unity behavior should match Godot behavior at gameplay level. Differences are bugs unless explicitly accepted here. |
| `adapted for Unity 3D` | The rule or intent is preserved, but implementation differs because Unity is 3D, component-based, NavMesh-driven or uses different rendering/data systems. |
| `intentionally removed` | The Godot behavior is not part of Unity v0.1 by design. Re-adding it requires a new issue. |
| `post-v0.1` | Valid feature, but not required for the v0.1 Unity parity milestone. |
| `technical fallback` | Unity has a fallback for empty scenes, missing config, tests or partial prefab setup. Production scenes should use the full system. |

## High-level migration principles

1. Godot is the design source of truth for v0.1 survival/ecosystem behavior.
2. Unity should preserve gameplay intent, not literal implementation details.
3. Unity 3D may replace Godot-specific mechanisms such as 2D movement, node groups, dictionary save blobs and redraw-driven visuals.
4. Any difference affecting gameplay balance, save/load, AI decisions, spawning, resource availability or debug visibility must be documented here.
5. If a difference is not listed here, treat it as a suspected migration bug.

## Summary matrix

| Area | Unity decision | Status | Related migration issue |
|---|---|---|---|
| Movement and navigation | Replace Godot 2D steering with Unity 3D movement/NavMesh/navigation adapters. | `adapted for Unity 3D` | #24, #26, #29, #30, #31 |
| World queries | Replace Godot groups/direct node scans with `WorldQueryRuntime`, registries and typed lookup. | `adapted for Unity 3D` | #26, #28 |
| Resources | Preserve resource identity, harvestability, edible vegetation, depletion, regrowth and save fields. | `must match Godot` | #27, #32, #35 |
| Hunger and diet | Preserve hunger stages, diet preferences, risk drive and preferred food selection. | `must match Godot` | #24, #29, #30, #31, #35, #36 |
| Small prey AI | Preserve plant seeking/eating, flee behavior, death meat drop and debug memory. | `must match Godot` | #29 |
| Grazer AI | Preserve plant-first behavior, stress scavenging/predation, niche debug and meat fallback. | `must match Godot` | #30 |
| Varnak AI | Preserve predator role, meat priority, prey hunting, player chase/attack and fire safety semantics. | `must match Godot`, partly `adapted for Unity 3D` | #31, #34 |
| Ecosystem director | Preserve per-biome biomass, populations, generations, niche/status and day tick effects. | `must match Godot` | #28, #32, #34, #35 |
| Save/load | Replace Godot dictionaries with typed C# DTOs while preserving gameplay state. | `adapted for Unity 3D` | #32 |
| EventBus | Replace Godot node EventBus with static C# event bus + snapshot/recent log. | `adapted for Unity 3D` | #34 |
| Debug UI | Preserve debug data visibility, but cache/throttle Unity UI to avoid stutter. | `adapted for Unity 3D` | #33 |
| Balance data | Move magic numbers toward ScriptableObject config assets with fallback defaults. | `adapted for Unity 3D` | #36 |
| Visuals | Replace Godot 2D redraw/resource atlas with Unity prefabs, renderers, materials and animation. | `adapted for Unity 3D` | #27, asset swap work |
| Evolution/generation model | Full evolutionary trait mutation is not required for Unity v0.1. | `post-v0.1` / `intentionally removed for v0.1` | v0.1 scope decision |

---

## Detailed deviations

## 1. 2D movement versus Unity 3D navigation

**Godot behavior:** creatures move in a 2D world using Godot node transforms, 2D distances and simple steering behavior.

**Unity decision:** Unity uses 3D transforms, horizontal distance checks and navigation adapters/NavMesh-compatible movement.

**Status:** `adapted for Unity 3D`

**Reason:** Literal 2D movement would not fit the target Unity 3D version. The gameplay requirement is not identical path math; it is that creatures seek food, flee threats, chase targets, remain inside playable space and do not spawn/move into invalid areas.

**Bug if:**

- creatures cannot reach valid nearby food/prey,
- creatures ignore flee/chase conditions,
- agents regularly move outside world bounds,
- agents spawn in water or invalid areas,
- NavMesh differences remove intended Godot behavior rather than adapting it.

**Related issues:** #24, #26, #29, #30, #31.

## 2. Godot groups versus Unity registries and world query facade

**Godot behavior:** many systems use groups, scene-tree lookups and node naming conventions.

**Unity decision:** Unity uses typed components, runtime registries, `EcosystemRuntime`, `EcosystemDirectorRuntime` and `WorldQueryRuntime`.

**Status:** `adapted for Unity 3D`

**Reason:** Unity scene lookups by tag/name every tick would be fragile and expensive. A facade keeps AI code close to Godot behavior while avoiding repeated full scene scans.

**Bug if:**

- a registered creature/resource cannot be found through `WorldQueryRuntime`,
- AI falls back to expensive scene scans as its normal path,
- plant/meat/prey lookup ignores biome or availability state,
- systems require exact GameObject names where a typed component should exist.

**Related issues:** #26, #28, #29, #30, #31, #33.

## 3. Resource visuals: Godot redraw/atlas versus Unity prefabs/materials

**Godot behavior:** `resource_node.gd` uses 2D visuals, atlas columns, colors and redraw-like visual state.

**Unity decision:** Unity resources are prefab instances with `ResourceNodeView`, colliders, renderers, materials and optional `FoodSourceView` bridge.

**Status:** `adapted for Unity 3D`

**Reason:** Unity 3D needs prefab-based visuals and physical scene representation. The gameplay fields must match, but visual implementation does not.

**Must match Godot at gameplay level:**

- resource kind/id,
- position,
- amount,
- harvest/depleted state,
- growth/regrowth state,
- edible vegetation state,
- pond vegetation state,
- item/drop identity,
- food value and pickup priority where relevant.

**Acceptable differences:**

- exact mesh shape,
- exact material,
- exact color,
- exact 2D atlas position,
- collider dimensions, as long as interaction and gameplay remain valid.

**Bug if:**

- berry bushes or grass are not edible for herbivores,
- depleted vegetation is still treated as available food,
- harvested/regrowth state is lost on save/load,
- meat drops are duplicated after reload,
- prefab material changes break gameplay components.

**Related issues:** #27, #32, #35.

## 4. Hunger and diet scale

**Godot behavior:** Godot uses normalized hunger/energy values and thresholds such as hungry/starving/desperate.

**Unity decision:** Unity runtime uses a 0-100 hunger scale in some components, while preserving the same threshold ratios and behavior stages.

**Status:** `adapted for Unity 3D`

**Reason:** Existing Unity UI/runtime systems use 0-100 style values. The migration requirement is stage parity and behavioral parity, not identical numeric storage scale.

**Must match Godot at gameplay level:**

- movement increases hunger faster than idle,
- hunger has `Satisfied/Hungry/Starving/Desperate` stages,
- diet preference changes effective nutrition,
- risk drive increases with hunger and low energy,
- desperate state increases food-seeking pressure.

**Bug if:**

- stage thresholds are inverted or skipped,
- eating non-preferred food is as effective as preferred food without design reason,
- starvation/desperation never influences behavior,
- save/load changes hunger stage unexpectedly.

**Related issues:** #24, #29, #30, #31, #35, #36.

## 5. Small prey behavior

**Godot behavior:** small prey seeks vegetation when hungry, eats plants, flees player/Varnak, records debug reason/last food and drops meat on death.

**Unity decision:** Preserve behavior in `CreatureBehaviorBrain`, using Unity world queries and runtime components.

**Status:** `must match Godot`

**Known Unity adaptation:** exact movement path and target acquisition are Unity-specific.

**Bug if:**

- hungry small prey does not seek available plant food,
- eating does not reduce hunger,
- last food/debug reason is not updated,
- small prey does not flee nearby Varnak/player,
- death does not create exactly one meat drop.

**Related issue:** #29.

## 6. Grazer behavior

**Godot behavior:** grazer primarily eats plants, can scavenge or hunt small prey under stress/low biomass/desperation, has herbivore/omnivore niche state and debug data.

**Unity decision:** Preserve plant-first logic and fallback meat/predation behavior in `CreatureBehaviorBrain`.

**Status:** `must match Godot`

**Known Unity adaptation:** hunting movement/attack is simplified until full combat/damage semantics mature.

**Bug if:**

- grazer prefers meat when valid plant food is available,
- grazer never uses meat under starvation/desperation,
- grazer hunts while plant food is available and not under stress,
- niche/debug state does not reflect stress/biomass conditions.

**Related issue:** #30.

## 7. Varnak behavior

**Godot behavior:** Varnak is the apex predator. It prioritizes meat drops, hunts prey, can stalk/chase/attack player, reacts to fire/campfire/torch safety, has aggression/fire/hunt debug values and cooldowns.

**Unity decision:** Preserve predator gameplay intent, with simplified Unity combat/fire hooks where necessary.

**Status:** `must match Godot` for gameplay intent; `adapted for Unity 3D` for exact combat/fire integration.

**Accepted Unity deviations for v0.1:**

- fire/campfire safety can be represented through player/campfire state and event bus, not necessarily exact Godot fire node lookup,
- attack events may be decoupled from final damage tuning,
- exact attack arc math can differ while preserving cooldown/range intent,
- pack coordination/trap awareness may be simplified unless implemented by a specific issue.

**Bug if:**

- hungry Varnak ignores meat drops when they are available,
- Varnak hunts prey when population is marked critical,
- Varnak attack event spams every tick without cooldown,
- Varnak has no way to express fire/campfire safety,
- debug data does not expose target/aggression/hunt/fire state where the behavior exists.

**Related issues:** #31, #34.

## 8. Ecosystem director and per-biome state

**Godot behavior:** ecosystem director tracks biome-level biomass, plant regrowth, population pressure, generations, niche/status and day progression.

**Unity decision:** Preserve per-biome state in typed C# state objects and save DTOs.

**Status:** `must match Godot`

**Accepted Unity deviations:**

- Unity may use generated biome regions and 3D positions instead of Godot 2D biome IDs,
- ecosystem metadata is typed rather than dictionary-driven,
- runtime event notifications use `GameEventBus` rather than Godot node signals.

**Bug if:**

- plant consumption does not reduce biome biomass,
- day progression does not regrow biomass/resources,
- populations/generations are lost on save/load,
- current biome/home biome/population biome are not restorable for creatures,
- biome query returns default for valid mapped regions without reason.

**Related issues:** #28, #32, #34, #35.

## 9. Save/load: Godot dictionaries versus typed C# DTOs

**Godot behavior:** save systems collect dictionaries for resources, creatures and ecosystem state.

**Unity decision:** use typed DTO classes such as resource save data, creature save data, biome ecosystem save data and world save data.

**Status:** `adapted for Unity 3D`

**Reason:** Typed C# DTOs are safer, easier to test and easier to evolve under Unity serialization/file storage.

**Must match Godot at gameplay level:**

- resource growth/depleted state,
- creature species/generation/position/health/hunger/energy/state,
- creature biome memory,
- ecosystem biomass/populations/generations,
- no duplicate meat drops after reload,
- dead creatures do not respawn alive if save marks them dead.

**Accepted Unity deviations:**

- save field names can differ,
- DTOs can contain extra metadata,
- missing/corrupted save can fallback safely instead of reproducing Godot error behavior,
- creature restore currently matches existing generated creatures by species/position rather than fully instantiating every missing creature from prefab.

**Bug if:**

- save/load loses resource depletion/growth,
- creature hunger/health/state changes unexpectedly after reload,
- ecosystem state resets silently,
- dead creature returns alive,
- dynamic meat drops duplicate.

**Related issue:** #32.

## 10. EventBus and runtime messages

**Godot behavior:** scene/root `EventBus` and message posting are used by several systems.

**Unity decision:** use static C# `GameEventBus`, typed event payloads, subscriptions and a bounded recent-event log exposed to snapshots.

**Status:** `adapted for Unity 3D`

**Reason:** Unity systems benefit from typed payloads and no scene-root singleton dependency.

**Must match Godot at gameplay level:**

- resource harvest event exists,
- plant consumption/scavenge/hunt events exist where gameplay action happens,
- ecosystem biomass/population/tick events exist,
- fire/campfire safety can be surfaced,
- event bus does not crash without listeners.

**Accepted Unity deviations:**

- event names are enum values instead of strings,
- event log is bounded,
- some events may originate from a related Unity component rather than the exact Godot node that emitted them.

**Bug if:**

- gameplay events are emitted repeatedly without cooldown where action has cooldown,
- listeners can crash the event bus globally,
- critical events are not visible to debug/snapshot systems.

**Related issue:** #34.

## 11. Debug UI and performance counters

**Godot behavior:** debug panel exposes AI/ecosystem data and refreshes on an interval.

**Unity decision:** use `CreatureDebugData`, cached overlay text, global debug settings and per-biome ecosystem overlay.

**Status:** `adapted for Unity 3D`

**Reason:** Unity `OnGUI` and text generation can cause stutter if rebuilt every repaint. Debug must be useful without harming performance.

**Must match Godot at visibility level:**

- creature state,
- decision reason,
- target,
- hunger stage,
- diet,
- biome memory,
- LOD/background simulation info,
- ecosystem biomass/population/generation state.

**Accepted Unity deviations:**

- layout differs,
- overlay style differs,
- data is cached/throttled,
- global debug settings may hide overlays entirely.

**Bug if:**

- debug UI allocates/rebuilds large strings every frame,
- hidden debug UI still performs expensive work,
- AI decisions cannot be inspected,
- ecosystem state cannot be inspected during migration testing.

**Related issue:** #33.

## 12. Balance data and magic numbers

**Godot behavior:** `game_balance.gd` and species scripts hold many constants.

**Unity decision:** move major species and LOD values into `ScriptableObject` configs, with fallback defaults for tests and empty scenes.

**Status:** `adapted for Unity 3D`

**Accepted for #36:**

- `SpeciesDefinition` controls species health/hunger/diet values,
- `CreatureSimulationLodConfig` controls LOD thresholds and intervals,
- `GameBalanceConfig` validates the expected assets,
- missing config produces fallback defaults and a warning rather than a crash,
- `EcosystemBalanceConfig` and `ResourceBalanceConfig` may initially be asset-ready before full runtime integration.

**Bug if:**

- changing a `SpeciesDefinition` has no runtime effect where it is assigned,
- missing config hard-crashes tests or empty scenes,
- balance config silently accepts missing required species,
- prefab/scenes cannot be wired to balance assets.

**Related issue:** #36.

## 13. Evolution, adaptation and generational traits

**Godot behavior:** original concept included evolving/adapting species over generations.

**Unity v0.1 decision:** full evolutionary trait mutation is not required for the current Unity parity milestone.

**Status:** `post-v0.1` / `intentionally removed for v0.1`

**Reason:** The current target is a stable playable Unity 3D survival/ecosystem slice. Full evolution can multiply balance, save/load and AI complexity before the core loop is stable.

**Still preserved in v0.1:**

- generation fields can exist in save/ecosystem state,
- populations can change over days,
- niche/status/debug can expose ecosystem pressure,
- future systems can build on the saved fields.

**Not a bug for v0.1:**

- no visible trait mutation over generations,
- no procedural new species behavior,
- no complex adaptation tree.

**Bug if:**

- generation fields break save/load,
- missing evolution causes crashes in code that assumes it exists,
- UI/debug claims evolution is active when it is not.

**Related issues:** #28, #32, #35, #36.

## 14. Asset pack visuals and material differences

**Godot behavior:** source project uses custom/simple 2D resource visuals.

**Unity decision:** Unity may use external 3D asset packs, wrapper prefabs, material relinking and texture cleanup tools.

**Status:** `adapted for Unity 3D`

**Reason:** Visual fidelity and 3D readability are Unity-specific. Godot visual parity is not required.

**Bug if:**

- visual prefab lacks required gameplay component,
- visual swap breaks `ResourceNodeView`,
- berry/grass visual swap removes `FoodSourceView`,
- colliders or scale make resource interaction impossible,
- material cleanup deletes required textures without replacement.

**Not a bug:**

- tree mesh differs from Godot tree icon,
- grass uses different material,
- resource color differs,
- Unity uses Embersstorm/other asset pack instead of Godot atlas.

**Related issues:** #27 and asset-swap work.

## 15. Known technical fallbacks

These fallbacks are intentional for tests and migration stability. They should not hide production setup problems forever.

| Fallback | Status | Why it exists | Follow-up expectation |
|---|---|---|---|
| Missing `GameBalanceConfig` resolves species defaults. | `technical fallback` | Empty test scenes and partial prefabs should not crash. | Production scene should use real config assets. |
| Creature save/load restores nearest existing matching creature. | `technical fallback` | Avoids premature prefab spawn manager complexity. | Later hardening should instantiate missing saved creatures. |
| EventBus works without listeners. | `technical fallback` | Systems should emit safely before UI/debug listeners exist. | UI/debug can subscribe when needed. |
| Debug settings can hide overlays. | `technical fallback` | Prevent stutter and noise in normal play. | QA can enable overlays per test run. |
| Runtime primitive meat drops exist. | `technical fallback` | Guarantees gameplay drop even before final art. | Replace with final prefab/art later. |

## QA rules

When testing migrated behavior:

1. First check whether the difference is listed here.
2. If listed as `must match Godot`, report as bug when behavior differs.
3. If listed as `adapted for Unity 3D`, test gameplay intent rather than exact implementation.
4. If listed as `post-v0.1`, do not block v0.1 unless it crashes, corrupts saves or is falsely exposed as active.
5. If not listed, treat as suspected migration bug and add a note to this document when resolved.

## Codex rules

When asking Codex to continue migration:

- Reference this document before changing behavior.
- Do not reintroduce literal Godot implementation details when Unity has an accepted adaptation.
- Do not remove fallback behavior unless tests and production config paths cover the replacement.
- Add or update tests whenever a decision moves from `post-v0.1` to active runtime.
- Add a new entry here for every intentional gameplay deviation.

## Migration issue index

| Issue | Purpose |
|---|---|
| #23 | Initial audit and parity gap discovery. |
| #24 | Hunger/diet behavior parity. |
| #25 | Creature simulation LOD/background simulation. |
| #26 | World query/spatial lookup semantics. |
| #27 | ResourceNode behavior parity. |
| #28 | EcosystemDirector biome state model. |
| #29 | SmallPrey behavior parity. |
| #30 | Grazer behavior parity. |
| #31 | Varnak behavior parity. |
| #32 | Save/load parity for resources, creatures and ecosystem. |
| #33 | Debug data, overlays and AI performance counters. |
| #34 | Gameplay event bus and message parity. |
| #35 | Godot parity tests for migrated systems. |
| #36 | GameBalance/species data config assets. |
| #37 | Intentional deviations documentation. |

## Current close criteria for #37

- This document exists at `Docs/migration/intentional-deviations.md`.
- The main known Unity deviations are classified.
- Each intentional difference has a reason.
- QA has rules for deciding bug versus accepted deviation.
- Codex has rules for future migration prompts.
