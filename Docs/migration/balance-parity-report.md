# Balance parity report - Godot source vs Unity runtime

Issue: #49 - `[MIGRATION] Validate migrated balance and species data against Godot source`

Status legend required by the issue:

| Status | Meaning |
|---|---|
| `bug` | Unity behavior/value is likely wrong or hardcoded in a place that should be config-driven. |
| `intentional` | Unity differs from Godot deliberately because of 3D/runtime migration needs. |
| `postponed` | Valid parity area, but not required for the current v0.1 Unity slice. |
| `needs playtest` | Value exists and is wired, but exact balance should be evaluated in gameplay. |

## Scope and source notes

This report does not re-port Godot data. It validates the Unity migrated balance surface and classifies known differences.

Godot baseline source of truth is the old Godot project referenced by migration docs:

```text
krisstoof/apex-shift-2d
```

The current Unity repo does not include a direct Godot data dump beside the Unity files, so this report uses the existing migration decisions and Unity runtime/config values as the review baseline. Where exact Godot numeric values are not present in this repo, the Unity status is marked `needs playtest` or `postponed` rather than pretending numeric parity is proven.

Related existing migration docs:

- `Docs/migration/intentional-deviations.md`
- `Docs/migration/unity-migration-status.md`

## Executive summary

| Area | Unity state | Status | Action |
|---|---|---:|---|
| Species health/hunger/diet | `SpeciesDefinition` ScriptableObject + fallback defaults exist. | `needs playtest` | Verify exact feel in gameplay; keep asset-backed. |
| Ecosystem biomass/population defaults | `EcosystemBalanceConfig` exists. | `needs playtest` | Runtime integration should be audited further. |
| Resource food/regrowth values | `ResourceBalanceConfig` exists, but some resource defaults still live in code. | `bug` | Move remaining resource balance constants into config or document as fallback-only. |
| Resource spawn density/safe distance | Main values are serialized fields on `WorldGeneratorRuntime`. | `needs playtest` | Accept for prototype; later move to `WorldGenerationSettings`/balance asset. |
| Varnak spawn scaling | Serialized fields exist and day scaling is implemented. | `needs playtest` | Validate day 2+ threat curve. |
| Creature hunger values | Default values are explicit per species. | `needs playtest` | Playtest hunger pacing and stage thresholds. |
| Fire protection | Torch/campfire radius/intensity/duration exist as serialized fields. | `needs playtest` | Validate radius readability and Varnak fear behavior. |
| Day/night | Runtime values are serialized fields. | `needs playtest` | Verify 120s day length against pacing. |
| Survival decay | Core defaults exist, currently code-defined. | `bug` | Move to config asset when balance stabilizes. |
| Crafting costs | Recipe database has explicit costs, currently code-defined. | `bug` | Move to recipe assets/config before content grows. |
| Evolution/generation model | Not active in v0.1. | `postponed` | Keep save fields safe; do not block v0.1. |

## Detailed comparison matrix

### 1. Resource spawn density

| Field | Godot intent | Unity value/location | Status | Notes |
|---|---|---|---:|---|
| General resource density | World should have readable but not overloaded resource distribution. | `WorldGeneratorRuntime` uses procedural generation and serialized spawn settings; exact current density is tied to biome entries and generator settings. | `needs playtest` | Needs gameplay pass after final map size/biomes stabilize. |
| Creature/resource density interaction | Resources should be enough to support herbivores and player crafting. | Resource generation and ecosystem food source bridge exist. | `needs playtest` | Validate with player start area, early crafting and animal hunger. |

Decision: keep current values for v0.1 smoke testing. Treat player inability to craft first tools within a short walk as a balance bug.

### 2. Resource safe distance

| Field | Godot intent | Unity value/location | Status | Notes |
|---|---|---|---:|---|
| Spawn away from invalid terrain | Resources should not spawn in water/invalid tiles. | `TryGetSafeResourcePoint(...)` checks `IslandTopographyRuntime.IsSafeForResourceAt(...)`. | `intentional` | Unity uses topography queries rather than Godot 2D tile checks. |
| Start clearing | Player should not start trapped inside resources. | `clearingRadius = 8f` in `WorldGeneratorRuntime`. | `needs playtest` | Validate if start area feels too empty or too cluttered. |

### 3. Varnak spawn distance and attempts

| Field | Godot intent | Unity value/location | Status | Notes |
|---|---|---|---:|---|
| Minimum Varnak distance from player | Varnaks should not spawn on top of the player. | `minVarnakDistanceFromPlayer = 48f`. | `needs playtest` | Good current safety value, but may be too safe on small maps. |
| General creature min distance | Creatures should not start directly beside player. | `minCreatureDistanceFromPlayer = 30f`. | `needs playtest` | Recent tuning reduced early chaos. |
| Spawn attempts | Should retry enough to find safe locations. | `creatureSpawnPositionAttempts = 36`, daily Varnak attempts `8`. | `needs playtest` | If Varnaks fail to appear on valid days, increase attempts or improve candidate selection. |
| Day scaling | Varnak pressure should rise over days. | `firstVarnakSpawnDay = 2`, `varnakAddEveryDays = 1`, absolute max `5`. | `needs playtest` | Intended Unity v0.1 pacing: low day 1 threat, increasing danger later. |

### 4. Small prey hunger

| Field | Godot intent | Unity default | Status | Notes |
|---|---|---:|---|---|
| Max health | Fragile prey. | `20` | `needs playtest` | Reasonable for prototype. |
| Max hunger | 0-100 Unity scale. | `100` | `intentional` | Existing docs accept Unity 0-100 scale while preserving stage ratios. |
| Hunger growth | Hungry often enough to drive food seeking. | `20` | `needs playtest` | Watch if prey constantly seeks food or rarely eats. |
| Hungry/starving/desperate | Staged behavior pressure. | `35 / 60 / 82` | `needs playtest` | Ratios look coherent. |
| Food search radius | Finds nearby plants. | `110`, desperate `160` | `needs playtest` | Validate against generated biome scale. |
| Initial hunger | Starts partly hungry. | `36-48` | `needs playtest` | Can make early AI active immediately. |

### 5. Grazer hunger

| Field | Godot intent | Unity default | Status | Notes |
|---|---|---:|---|---|
| Max health | Larger than small prey. | `45` | `needs playtest` | Good relative health. |
| Hunger growth | Grazers should need food more aggressively than prey. | `30` | `needs playtest` | Could drive too much constant eating if biomass is low. |
| Hungry/starving/desperate | Same stage model. | `35 / 60 / 82` | `needs playtest` | Stage parity OK. |
| Food search radius | Slightly larger than small prey. | `120`, desperate `170` | `needs playtest` | Validate movement cost and NavMesh paths. |
| Diet | Plant-first, can use fallback meat under stress. | plant `0.85`, meat `0.05`, scavenger `0.10` | `needs playtest` | Matches migration intent for omnivore fallback. |

### 6. Varnak hunger

| Field | Godot intent | Unity default | Status | Notes |
|---|---|---:|---|---|
| Max health | Apex predator, durable. | `90` | `needs playtest` | Must be dangerous but not impossible. |
| Hunger growth | Predator should hunt but not constantly attack. | `18` | `needs playtest` | Lower than grazers; probably intentional. |
| Hungry/starving/desperate | Predator pressure stages. | `32 / 58 / 80` | `needs playtest` | Slightly more aggressive thresholds. |
| Food/prey search | Long range predator search. | `140`, desperate `200` | `needs playtest` | Watch CPU and player pressure. |
| Diet | Meat-first. | plant `0`, meat `1`, scavenger `0.45` | `intentional` | Matches predator role. |

### 7. Ecosystem tick

| Field | Godot intent | Unity value/location | Status | Notes |
|---|---|---|---:|---|
| Runtime ecosystem tick | Ecosystem should advance over time/day. | `EcosystemDirectorRuntime.simulationTickSeconds = 12f`. | `needs playtest` | Runtime tick exists; pacing needs gameplay validation. |
| Day tick | Day progression should advance biomass/resource regrowth. | `DayNightRuntime` calls ecosystem tick on day change when enabled. | `intentional` | Unity event-driven implementation differs from Godot but preserves intent. |
| One biome per frame | Avoid stutter. | `processOneBiomePerFrame = true`. | `intentional` | Unity performance adaptation. |

### 8. Biomass values

| Field | Godot intent | Unity default | Status | Notes |
|---|---|---:|---|---|
| Max plant biomass | Biome food capacity. | `100` | `needs playtest` | Good normalized scale. |
| Plant regrowth/day | Ecosystem recovery. | `6` | `needs playtest` | Tune after animal counts stabilize. |
| Bush food value | Plant nutrition. | `6` | `needs playtest` | Exists in config. |
| Berry bush food value | Better plant nutrition. | `8` | `needs playtest` | Exists in config. |
| Grass/dense grass | Background herbivore food. | `5 / 10` | `needs playtest` | Exists in config. |
| Meat food value | Carnivore/scavenger food. | `10` | `needs playtest` | May be low relative to predator hunger. |

Risk: `ResourceBalanceConfig` exists, but several defaults are still also encoded in `ResourceDefinition.CreateDefault(...)`. This is a `bug` if production balancing is expected to happen through assets only.

### 9. Population caps

| Field | Godot intent | Unity default | Status | Notes |
|---|---|---:|---|---|
| Small prey population | Common prey baseline. | `4` | `needs playtest` | From `EcosystemBalanceConfig`. |
| Grazer population | Medium herbivore baseline. | `3` | `needs playtest` | From `EcosystemBalanceConfig`. |
| Varnak population | Rare predator baseline. | `1` | `needs playtest` | From `EcosystemBalanceConfig`; daily cap also exists in world generator. |
| Varnak absolute max | Prevent runaway predator count. | `5` | `needs playtest` | Serialized in `WorldGeneratorRuntime`, should become config later. |

### 10. Torch modifiers

| Field | Godot intent | Unity default | Status | Notes |
|---|---|---:|---|---|
| Torch duration | Temporary mobile safety. | `120s` | `needs playtest` | Should feel valuable but not permanent. |
| Torch protection radius | Varnak deterrent radius. | `9m` | `needs playtest` | Validate readability and AI response. |
| Torch intensity | Fire strength. | `0.85` | `needs playtest` | Used by `FireSourceRuntime`. |
| Consumes torch item | Resource cost for safety. | `true` | `intentional` | Keeps torch meaningful. |

Risk: torch values are serialized runtime fields, not central balance config. Acceptable for prototype; mark as `needs playtest`.

### 11. Campfire modifiers

| Field | Godot intent | Unity default | Status | Notes |
|---|---|---:|---|---|
| Fuel item | Wood fuels campfire. | `wood` | `intentional` | Matches survival crafting loop. |
| Seconds per fuel | Fire duration per wood. | `60s` | `needs playtest` | Tune with day length. |
| Max fuel | Prevent infinite stacking. | `300s` | `needs playtest` | Good cap for prototype. |
| Starting fuel | Newly placed/created campfire can start lit. | `90s` | `needs playtest` | Might be too generous. |
| Protection radius | Static safety area. | `13m` | `needs playtest` | Should be stronger than torch. |
| Intensity | Fire source strength. | `1.0` | `needs playtest` | Should scare Varnaks reliably nearby. |

### 12. Day/night parameters

| Field | Godot intent | Unity default | Status | Notes |
|---|---|---:|---|---|
| Day length | Fast prototype cycle. | `120s` | `needs playtest` | Tune with hunger/survival decay. |
| Starting day | Start at day 1. | `1` | `intentional` | OK. |
| Starting time | Start in morning/day. | `0.25` | `intentional` | OK for first spawn. |
| Night start/morning start | Uses core `DayNightState` defaults. | core defaults | `needs playtest` | Need compare to Godot exact hours when source is available. |
| Ecosystem day tick | Ecosystem advances on day changes. | `true` | `intentional` | Matches survival/ecosystem intent. |

### 13. Survival decay

| Field | Godot intent | Unity default | Status | Notes |
|---|---|---:|---|---|
| Hunger decay | Player hunger falls over time. | `0.75/s` | `needs playtest` | Likely fast; good for prototype feedback. |
| Rest decay | Rest falls over time. | `0.35/s` | `needs playtest` | Validate if sleep/rest loop exists. |
| Sprint rest decay | Sprint has extra rest cost. | `0.9/s` | `needs playtest` | Tune with stamina. |
| Sprint stamina cost | Sprint drains stamina. | `14/s` | `needs playtest` | OK for short sprint bursts. |
| Starvation damage | Low hunger damages player. | `1/s` | `needs playtest` | Validate death pacing. |
| Health regen | Passive regen. | `0.45/s` | `needs playtest` | Check with campfire multiplier. |
| Campfire regen multiplier | Campfire improves recovery. | `2.2 health`, `1.75 stamina` | `needs playtest` | Good survival reward. |
| Low hunger speed/stamina penalties | Hunger affects mobility/recovery. | speed `0.82`, stamina regen `0.45` | `needs playtest` | Should be visible but not too punishing. |

Risk: `SurvivalRules.CreateDefault()` is code-defined. This is a `bug` for long-term balancing because survival decay should be data/config-driven once v0.1 balance matters.

### 14. Crafting costs

| Recipe | Unity ingredients | Status | Notes |
|---|---|---:|---|
| `campfire` | wood `3`, stone `2` | `needs playtest` | Good early shelter cost. |
| `spear` | wood `2`, stone `1`, fiber `1` | `needs playtest` | Should be first weapon/tool target. |
| `torch` | wood `1`, fiber `1` | `needs playtest` | Cheap temporary safety. |
| `bow` | wood `3`, fiber `4`, bone `1` | `needs playtest` | Requires Varnak/bone loop; may be too late. |
| `arrow` | wood `1`, stone `1`, fiber `1` -> `5` arrows | `needs playtest` | Validate ammo economy. |
| `axe` | wood `2`, stone `2`, fiber `1` | `needs playtest` | Needed for big trees. |
| `pickaxe` | wood `2`, stone `3`, fiber `1` | `needs playtest` | Needed for big rocks. |
| `trap` | wood `2`, fiber `2` | `needs playtest` | Validate early defense. |
| `wall` | wood `3` | `needs playtest` | Cheap building piece. |
| `storage_box` | wood `4` | `needs playtest` | Early storage should be reachable. |
| `tent` | wood `4`, fiber `3` | `needs playtest` | Depends on rest/sleep design. |

Risk: recipes are currently defined in `RecipeDatabase.CreateDefault(...)`, not as assets. This is a `bug` for content scaling and balance iteration, but acceptable as a prototype fallback.

### 15. Species definitions

| Species | Unity definition state | Status | Notes |
|---|---|---:|---|
| `small_prey` | `SpeciesDefinition` asset/fallback exists. | `needs playtest` | Values mapped and runtime-resolvable. |
| `grazer` | `SpeciesDefinition` asset/fallback exists. | `needs playtest` | Values mapped and runtime-resolvable. |
| `varnak` | `SpeciesDefinition` asset/fallback exists. | `needs playtest` | Values mapped and runtime-resolvable. |

Acceptance check: Unity has direct equivalents for the important migrated species values:

- health,
- max hunger,
- hunger growth,
- hunger thresholds,
- food search radii,
- prey seek threshold,
- flee hunger threshold,
- initial hunger,
- plant/meat/scavenger diet preference.

## Hardcoded-data review

| Area | Current state | Status | Recommendation |
|---|---|---:|---|
| Species values | ScriptableObject assets + fallback defaults. | `intentional` | OK. Keep fallbacks for tests. |
| LOD values | Config asset exists. | `intentional` | OK. |
| Ecosystem values | Config asset exists, runtime integration should be verified. | `needs playtest` | Audit whether all runtime systems read the asset. |
| Resource food/regrowth values | Config exists, but `ResourceDefinition.CreateDefault(...)` also contains values. | `bug` | Move default resource balance to asset or mark code values as fallback only. |
| World spawn values | Serialized fields on `WorldGeneratorRuntime`. | `needs playtest` | Move to `WorldGenerationSettings` or balance config when stable. |
| Torch/campfire values | Serialized component fields. | `needs playtest` | Accept now; move to fire balance config if many fire sources are added. |
| Survival rules | Code-defined defaults. | `bug` | Add `SurvivalBalanceConfig` or equivalent. |
| Crafting recipes | Code-defined defaults. | `bug` | Add recipe assets/config before content expansion. |

## Tester guidance

Balance should be evaluated, but not all numbers should block v0.1.

### Block as bug

Report as `bug` if:

- player cannot craft first tool/weapon after short early-game gathering,
- Varnaks spawn on top of the player,
- Varnaks never appear after day progression,
- creatures repeatedly starve despite visible food nearby,
- resources spawn in water/invalid areas,
- save/load changes balance state such as hunger, population, biomass or crafted inventory,
- changing assigned species/balance config asset has no effect in runtime.

### Mark as needs playtest

Use `needs playtest` for:

- exact hunger pacing,
- exact Varnak pressure curve,
- exact torch/campfire feel,
- exact day length,
- exact biomass values,
- exact crafting feel once all first-tier tools exist.

### Postpone

Use `postponed` for:

- evolutionary genetics,
- long-term population simulation expansion,
- richer biome-composition balancing beyond the current v0.1 slice,
- advanced food chain content not yet in the Unity slice.

## Recommended follow-up

1. Move remaining balance fallbacks into assets/config where feasible.
2. Compare any future Godot source export or data dump against this table and downgrade `needs playtest` items only after a concrete test pass.
3. Keep this report updated whenever a serialized runtime field becomes a hardcoded default.
