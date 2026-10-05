# Issue #109 SpeedTree vegetation integration validation

Status: **#109 NOT COMPLETE — SpeedTree authoring blocked**. This is the repo-side preparation path; no production art activation is claimed. Preparation commit message: `#109 prepare SpeedTree vegetation integration` (SHA is reported in the delivery message).

## Environment and scope

Unity 6000.6.2f1 / URP 17.6.0. Tests ran in a durable isolated copy under ignored `Logs/Issue106Project`, using the existing cached Library. This copy name is historical; it contains the final #109 sources, manifest and folder structure. Existing user animation binder/controller/RuntimeWorld/TimeManager edits and the two unrelated untracked animation test files were excluded, restoring those copy paths to HEAD. The main user's Unity process was not used or stopped.

Modeler 10.2.0 was installed by the user during the task. No production vegetation was authored in it. A discovered local dcc-mcp-speedtree 0.1.1 adapter was queried through its registered discovery/export tools. Capabilities report experimental SPM/STT generator-file edits plus CLI export, with no live editing, material/Fan wind editing or collision authoring, and preset-driven LOD export only. A Games/Blank.spm diagnostic CLI export returned exit 0 and wrote 2,288 bytes with no textures/materials; the adapter failed its final report replacement with PermissionError/WinError 5. The empty native diagnostic file was removed; audit reports remain only in ignored Logs. No sample/vendor vegetation or textures were copied into Assets. License entitlement, real authored silhouettes and animated Games wind are not certified by that diagnostic.

## Test evidence

| Run | Result | XML / log under ignored Logs |
| --- | --- | --- |
| Target integration + existing vegetation data editor tests | 27/27 PASS, 0 skipped | issue109-target-edit-2.xml / .log |
| Production food/tree gameplay + vegetation streaming + tropical landmarks | 9/9 PASS, 0 skipped | issue109-target-play.xml / .log |
| Final full EditMode (including 15 new SpeedTree integration cases) | 246/246 PASS, 0 skipped | issue109-final-edit-2.xml / .log |
| Final full PlayMode (including 3 new production gameplay cases) | 336/336 PASS, 0 skipped | issue109-final-play.xml / .log |

Final full suites use the same Unity version and EditMode/PlayMode platforms as `.github/workflows/unity-tests.yml`; this is the issue's accepted **local full-run equivalent**, not a remote GitHub CI result or Linux rendering/import validation. No workflow changes or SpeedTree Modeler dependency were introduced in CI. Compilation succeeds. Existing editor HarvestableTreeGameplayTests and full play-mode save/story/world/streaming regressions are included.

The initial target editor run found a transient fixture-state error: AddComponent.Reset initialized a conifer state before serializing the berry/grass template data. The test now checks a fresh instance, which rebuilds nonserialized runtime state from copied fields; final target/full runs pass. The initial compilation used two outdated APIs; these were corrected for current NUnit/Unity EntityId. No fixture-native model or fake ST9 was added to make tests pass.

A later code review found that ResourceNodeView.Awake reapplies interactionRadius. Builder and validator therefore convert that serialized distance as well as trigger center/radius from the original scaled template to an identity root. Final PlayMode assertions check the actual world radii after Awake (.45 m berry and .3375 m grass), preventing a 2.25 m regression after future activation.

## Delivery report

1. Commit: preparation message above; exact SHA in delivery response.
2. Installed Modeler: 10.2.0 (request originally targeted 9).
3. Actual use: only official CLI empty-project diagnostic through the installed adapter, not vegetation authoring.
4. Production `.st9` created: none. All 26 required exports below remain absent.
5. Wrapper prefabs created: none. Canonical directories and native-only builder are prepared, with stable-path/GUID-preserving rebuild behavior.
6. New active species assets: none. The manifest stages the 21 additional IDs listed below; activation will reuse four current species plus compatibility conifer.
7. Stable IDs/GUIDs retained: tree_leafy_01, tree_dead_01, shrub_forest_01, groundcover_forest_01, tree_conifer_01. Production data assets were not modified.
8. Current exact active mix: coast/lowland_jungle/jungle_interior/wet_jungle each use tree_leafy_01, shrub_forest_01, groundcover_forest_01 with relative weights 1/1/1; rocky_upland uses shrub_forest_01 and groundcover_forest_01 with weights 1/1. Coast tree/shrub density multipliers are .25/.6. Overall densities remain .45/1.55/1.85/2.10/.45. Water has no profile; conifer/dead remain outside active profiles. The exact **planned** 11/21/19/19/8-entry tropical mixes, weights, eligibility and densities are in the [authoring guide](../art/speedtree-tropical-vegetation.md) and JSON manifest; they are not activated.
9. Berry/grass retained: wrapper builder copies existing ResourceNodeView/FoodSourceView contracts and root layer/tag, converts legacy trigger/interaction dimensions to meters, copies no old visual, and rejects drift/disabled food or extra blocking colliders. Production PlayMode verifies plant-food registration, biomass/nutrition consumption, removal, resource/item/regrowth flags and nonblocking grass. No item/resource balance changes.
10. Harvestable pipeline: actual production catalog tree prefabs are spawned through VegetationSpawner, promoted, registered with stable placement/TreeId and the existing resource kind, then unregistered on destruction. Existing harvestable axe/fall/depleted/regrowth/save tests and streaming state tests pass. Palm-specific/native-model assertions require genuine delivered assets; the same production loop will include them after activation.
11. LOD: builder preserves nested native LODGroup; readiness requires three tree/dead/hero levels with valid renderers. Actual SpeedTree LOD distances/transitions/billboards are unverified because art is absent. Existing production LOD retention and streaming tests pass.
12. Wind: no new wind system or scene WindZone. Games wind review flag is pending for every export. No animated SpeedTree wind has been certified.
13. Materials: canonical validator requires native URP SpeedTree9_URP shader and MainTex atlas, scripts/meshes/material/texture dependencies; shader/property were verified in local URP source. No new vegetation material/texture is claimed; visual shader QA remains pending.
14. EditMode: final table above.
15. PlayMode: final table above.
16. CI: no remote run triggered for this task; final complete local two-suite run is used as the allowed equivalent. CI does not need Modeler to import committed native assets later.
17. Smoke seeds: 12345, 81281, 91284 were exercised by automated production world/landmark/environment/vegetation regressions. There was no graphical SpeedTree visual smoke approval.
18. Performance: existing full streaming benchmark uses 649 placements/81 chunks and checks pool reuse, budget limits, LOD retention and stateful tree save/streaming. Prior full run measured avgFrameMs 16.666, p95 16.774, maxStreamingMs .32; values are headless fixture observations, not a new-art performance baseline. Final values are appended after completion. No planner/budget/density changes; SpeedTree art comparison is unavailable.
19. Legacy retained deliberately: all Bushcraft and Embersstorm assets/default wrappers, compatibility conifer and dead species, legacy biome vegetation tables, all existing depleted visuals. No asset cleanup was performed.
20. RuntimeWorld untouched by this task. Its preexisting user content SHA256 remains `594196efabbb593485008520413c0bda126d081213de3095223b2220cf1099b4`; animation controller remains `824c117c1072f65567dfeed7d820b3fce529aac2b7ba33e0713c24d62205caf3`. Neither file is in the preparation commit. User MCP configuration/launcher/README changes are also excluded from this commit.
21. Limitations: no production Modeler-authored set, license entitlement not independently certified, partial adapter coverage/report I/O error, no measured trunks/texture provenance/graphical wind/LOD/pivot/asset-performance QA. Positive native import/build/bind branches remain scaffold until genuine assets exist; negative blocked/rollback/contract paths run without Ignore or fake assets. Binder preflights the entire set before touching production and rolls back failed data updates.
22. Exact missing exports: listed below. For each, deliver the real native export plus its accessible textures, provenance/license, authoring/wind review and measured trunk dimensions where harvestable; then build its canonical wrapper and bind only after all 26 are ready.

## New IDs staged only

`palm_tall_01`, `palm_tall_02`, `palm_curved_01`, `tree_broadleaf_02`, `tree_broadleaf_03`, `tree_wet_01`, `tree_wet_02`, `tree_upland_01`, `tree_dead_02`, `sapling_tropical_01`, `sapling_tropical_02`, `shrub_tropical_02`, `shrub_tropical_03`, `fern_large_01`, `fern_large_02`, `broadleaf_understory_01`, `broadleaf_understory_02`, `fern_small_01`, `fern_small_02`, `vine_ground_01`, `groundcover_tropical_02`.

## Exact missing native exports

All paths are relative to `Assets/_Project/Art/SpeedTree/Tropical/`.

```text
Canopy/ST_Palm_Tall_A.st9
Canopy/ST_Palm_Tall_B.st9
Canopy/ST_Palm_Curved_A.st9
Canopy/ST_Broadleaf_Canopy_A.st9
Canopy/ST_Broadleaf_Canopy_B.st9
Canopy/ST_Broadleaf_Canopy_C.st9
Canopy/ST_Wet_Canopy_A.st9
Canopy/ST_Wet_Canopy_B.st9
Canopy/ST_Upland_Tree_A.st9
Canopy/ST_Dead_Tropical_A.st9
Canopy/ST_Dead_Tropical_B.st9
Understory/ST_Sapling_A.st9
Understory/ST_Sapling_B.st9
Understory/ST_Berry_Shrub_A.st9
Understory/ST_Shrub_A.st9
Understory/ST_Shrub_B.st9
Understory/ST_Fern_Large_A.st9
Understory/ST_Fern_Large_B.st9
Understory/ST_Broadleaf_Understory_A.st9
Understory/ST_Broadleaf_Understory_B.st9
GroundCover/ST_Fern_Small_A.st9
GroundCover/ST_Fern_Small_B.st9
GroundCover/ST_Vine_Ground_A.st9
GroundCover/ST_Groundcover_Grass_A.st9
GroundCover/ST_Groundcover_Tropical_B.st9
Hero/ST_OldTree_Hero_Base_A.st9
```

Final full-suite completion: EditMode 2026-10-05 07:45:07Z, PlayMode 2026-10-05 07:50:42Z. Final streaming fixture observation: placements=649, chunks=81, avgFrameMs=16.667, p95FrameMs=16.687, avgFps=60, maxStreamingMs=.221. No new-art performance or graphical wind assessment is inferred.
