# #106 tropical world/story migration validation

## Current production architecture

Production generates one procedural tropical island. `HabitatClassifier` and
cached `EnvironmentSample` data are authoritative. Environment IDs are `water`,
`coast`, `lowland_jungle`, `jungle_interior`, `wet_jungle`, `rocky_upland`.
`HabitatVegetationCatalog` supplies vegetation eligibility; `SpeciesDefinition`,
`CreatureRole` and `CreaturePopulationRules` supply fauna behavior and counts.

The production story is plane crash -> survival -> raft -> failed escape ->
human traces -> smuggler base -> prepare boat -> escape -> completed. Existing
#97–#105 implementations remain in place; #106 adds defensive save defaults and
extends their regression coverage without changing world visuals or balance.

## Save compatibility

Format remains `1.0.0` (no major-version bump or separate migration framework).
`UnityJsonGameSaveSerializer` invokes `GameSaveData.EnsureDefaults` on both
serialization and deserialization. This now calls `WorldSaveData.EnsureDefaults`.
`GameSaveService.ApplyLoadedState` also normalizes direct DTO input.

Normalization repairs the serialized fields, without requiring getter reads:

- Missing/null world, inventory, survival and version receive existing defaults.
- Missing/null resource, tree, pickup, biome, creature, building, landmark, clue,
  inventory-slot, milestone and boat-source collections become empty lists.
- Missing/null story becomes `survive_crash` with no milestones. Blank stage IDs
  become the initial stage; unknown nonblank IDs retain the runtime's existing
  validation path.
- Missing/null location becomes island; missing/null boat state becomes empty.
- Day is at least 1. Time wraps into [0,1); NaN/infinity become zero.
  Invalid ecosystem tick timers become zero and empty state source is `generated`.
- Existing legacy `biomeStates` and unknown compatible identifiers are retained.
  Restore does not synthesize story signals, grant items or replay completion.

## Automated coverage checklist

Each row maps to production behavior in an existing test suite. Planner tests
remain useful for rejection boundaries; production tests also inspect generated
objects and authoritative environment samples.

| Check | Coverage |
| --- | --- |
| Tropical generation deterministic | `TerrainHeightfieldAndTopographyTests`; `TropicalLandmarkPlayModeTests.ProductionGeneration_ThreeSeedsStartAtCrashAndPreserveDiscoveryThroughSaveLoad` compares sampled habitat/terrain/height/elevation/slope/moisture/temperature/coast data, landmark positions and crash spawn after regeneration on three seeds |
| No production legacy five-biome generation | Canonical habitat assertions in that production test; `CreaturePopulationPlannerTests.ProductionGeneratorCannotRegressToLegacyPopulationOrBiomeSpawning` guards old classifier/population entry points |
| Safe crash spawn | Three-seed production test checks crash distance and the dense topography safety rules (land/nonwater/nonshoreline, slope, nonridge, bounds), plus deterministic regenerated spawn |
| Vegetation water/shoreline rejection | Three-seed production test samples all debug placements and actual generated instances; `VegetationPlacementPlannerTests.EnvironmentConstraintsRejectInvalidCandidates` covers rejection boundaries |
| Vegetation slope/habitat/bounds/catalog | Actual generated instances are checked against topography, world bounds and species eligibility in the tropical habitat catalog; planner tests cover wet-jungle/coast constraints |
| Landmark hard constraints | Three-seed production test checks unique stable IDs, habitat, terrain, slope, elevation, moisture, coast band, crash/anchor distance, separation and regeneration |
| Story idempotency | `StoryProgressionTests.CompleteFlowIsTerminalAndCaptureIsSortedAndIndependent` exercises all nine transitions, repeats every signal and counts stage/milestone/presentation/completion notifications; `StoryProgressionPlayModeTests` checks bus subscription lifecycle |
| Clue save/load | `ProductionStoryCluePlayModeTests.SaveLoadUpdatesGeneratedCluesWithoutReplayAndOldSaveRemainsCompatible` checks discovered state, clue/human-traces milestones, restored stage, duplicate Discover and no replay |
| Raft failure save/load | `ProductionRaftEscapePlayModeTests.GeneratedWorld_CraftBoardEscapeFailAndLoadKeepsPlayerAliveAndStory` crosses the real serializer, retains failed milestone/stage and asserts no restored failure signal |
| Base interior save/load | `SmugglerBaseInteriorPlayModeTests.ProductionTraversalSaveLoadPreservesStateAndDoesNotReplaySignals` checks location/bounds, one player/interior, retained access, no replay and old location default |
| Boat partial/ready state save/load | `EscapeBoatPlayModeTests.FullProductionPreparationEscapeAndCompletedContinuePersist` checks collected/available sources, missing requirement state, prepared milestone, ready state and escape stage |
| Completed run save/load | Same production boat test checks completion HUD, stopped session/time, milestone, no second escape event and rejected repeated interaction after Continue |
| Save during escape | Same boat test restores Ready and normal movement instead of coroutine progress |
| Old save without story fields | `SaveDtoSerializationTests.LegacySaveWithoutStoryLoadsFreshInitialState` uses pre-story JSON; explicit-null normalization test inspects raw fields |
| Old save with biomeStates | Legacy JSON DTO test preserves westwood/varnak data; production clue persistence test loads that legacy schema through `GameSaveService` |
| Current full story round-trip | `SaveDtoSerializationTests.FullStorySaveRoundTripsLocationCluesAndBoatSources` checks escape and completed stages, all milestones, clues, interior position and source IDs |
| Generate -> Clear -> Generate | `WorldGeneratorRuntimeLifecyclePlayModeTests` checks owned root/component counts, destroyed old objects, emptied registries, old story subscription disposal, new story response and preservation of unrelated roots |
| Generic creature population | `CreaturePopulationPlannerTests`; `HabitatCreatureLifecycleTests.GeneratedPopulationRespectsCapEnvironmentAndDailyRegistryCount` and custom predator-role tests |
| No Varnak-only production path | Existing forbidden-field/source guard, generic role behavior and legacy species canonicalization tests |
| Debug terminology/input isolation | `DebugDataParityTests` checks habitat overlay data and safe pointer queries; animation test explicitly enables scaled simulation time and restores the prior value |

## Legacy/development reference audit

Repo-wide audit covers old five-biome IDs, `BiomeId`,
`LegacyBiomeCompatibility`, `BiomeDefinitionAsset`, `BiomeCatalogAsset` and
`varnak` in scripts, documentation, authoring tools and serialized content.
The raw local reference report is `Logs/issue106-legacy-reference-audit.txt`.

| Category | References and disposition |
| --- | --- |
| Production environment | `WorldGeneratorRuntime` builds topography with `HabitatClassifier`, never `BiomeClassifier`; `NaturalTerrainBuilder` receives environment/height queries and neutral materials. No five-biome terrain classifier is reinstated. Vegetation and creature generation use habitat/species catalogs. |
| Production compatibility profiles | Generated legacy catalog regions support resource distributions, ecosystem biomass, audio and old event/query consumers. `EcosystemDirectorRuntime`, resource/building water queries and creature biomass memory still reference those APIs. They are live compatibility paths, not terrain authority; retained to preserve balance. |
| Save/serialized compatibility | `biomeStates`, legacy creature biome fields, `CreatureSpeciesCompatibility` (`varnak -> island_predator`), `FormerlySerializedAs`, old event enum values and audio clip collection names remain needed for old saves/assets. No ID-specific predator population branch exists. |
| Debug presentation | Creature overlay now displays actual habitat IDs. Compatibility biome fields remain available in `CreatureDebugData`; API names are not renamed for cosmetics. |
| Development/test | `BiomeClassifier`, `BiomeDefinitionAsset`, `BiomeCatalogAsset`, five-biome profiles and `BiomeWorldTest.unity` are used by handcrafted tooling, compatibility tests or live profile consumers; retained. |
| History/assets | Godot references, old docs, models, scene assets and generated content remain. No third-party download, asset removal, scene cleanup or balance change. |

No dead Varnak-only runtime population code required removal: the existing
generic population/role implementation already replaced it. Legacy combat
event names and messages are kept as compatibility contracts.

## Manual smoke checklist

Not claimed as performed by the automated batch runs.

1. Start a new game and confirm the tropical island.
2. Confirm spawn beside the plane crash on safe land.
3. Walk through coast, jungle and upland; inspect water edges for vegetation.
4. Progress survival and build the raft.
5. Trigger the failed escape; save/load and confirm the retained stage.
6. Discover a human clue; save/load and confirm no second discovery event.
7. Locate/open the smuggler base and enter its interior.
8. Save/load inside; confirm bounds, one player and no second access event.
9. Discover the boat, collect one requirement and save/load.
10. Collect the other requirement, prepare the boat and save/load Ready.
11. Start escape; save/load during the sequence and confirm a fresh attempt.
12. Escape, confirm completion, save/load/Continue and confirm stopped gameplay.
13. Confirm a second interaction cannot complete the run again.
14. Return to menu; clear/regenerate and inspect for duplicate objects/events.

## Validation runs

Full EditMode and PlayMode run with Unity 6000.6.2f1 in an isolated copy of the
current workspace, then repeated for the exact commit scope with unrelated
user changes excluded only from the isolated copy. No headless mouse fixture,
ignored tests, extended timeouts or weakened assertions are used.

| Full run | EditMode | PlayMode |
| --- | --- | --- |
| Exact #106 commit scope | 231/231 PASS | 333/333 PASS |
| Workspace including the user's two untracked animation tests | 232/232 PASS | 334/334 PASS |

Reports: `Logs/issue106-editmode-commit.xml`,
`Logs/issue106-playmode-commit.xml`, `Logs/issue106-editmode-final.xml` and
`Logs/issue106-playmode.xml`, with matching `.log` files (ignored artifacts).
The original workspace's RuntimeWorld scene, Generated controller, player binder,
TimeManager and untracked animation tests remain untouched and outside #106.
The prior animation failure was fixed by restoring simulation-time isolation in
its existing fixture; production animation behavior and assertions are unchanged.

Existing CI: `.github/workflows/unity-tests.yml` uses the same Unity version and
EditMode/PlayMode matrix on push/PR/manual dispatch. A remote workflow requires
the commit on GitHub; validation can use an isolated branch and draft PR without
merging into remote main. Remote CI status is reported separately from local
test results and may depend on repository Unity license secrets.
