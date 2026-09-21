# Third-party asset inventory

This inventory records the repository layout after the asset audit for Issue #79.
Paths under `Assets/_Project` are project-owned runtime content; vendor roots remain
at the repository root until a separate, dependency-safe migration is justified.

| Root/package | Current use | Source/license evidence | Status |
| --- | --- | --- | --- |
| `Assets/_Project/Audio` | Runtime UI, combat, creature, footsteps and ambience audio | Generated procedural WAV pack; `SourceAssets/Audio/OGAReference/SOURCES.md` records OpenGameArt CC0 reference pages | Migrated from realistic audio wrapper; production paths updated |
| `Assets/_Project/Art/Placeables/Models` | `PrefabRegistry` building model lookup | Project-generated low-poly OBJ/MTL pack; source README described import and model mapping | Migrated from placeables wrapper; binder path updated |
| `Assets/_Project/Resources/ApexShift2D/Art/Icons` | Runtime `Resources.Load` item/resource/tool/UI icons | Icon wrapper README; exact upstream license not recorded in repository | Migrated; license UNKNOWN and must be verified before release |
| `SourceAssets/Audio/OGAReference` | Non-runtime OGA references, manifest and validation metadata | `SOURCES.md` lists OpenGameArt CC0 references | Outside `Assets`; never loaded by Unity |
| `Assets/EmbersStorm -  Free Nature Pack` | Vendor environment content used by project art/world tooling | Vendor README present; license terms not independently verified | Retained in place |
| `Assets/Low Poly Survival Pack` | Vendor survival/environment content | No definitive license record found in repository | Retained in place; license UNKNOWN |
| `Assets/Low Poly Trees & Nature Pack (70 Props)` | Vendor vegetation content | No definitive license record found in repository | Retained in place; license UNKNOWN |
| `Assets/ithappy`, `Assets/Kevin Iglesias`, `Assets/StylizedCore`, `Assets/YughuesFreeGroundMaterials` | Vendor art/material/character content referenced by project assets or tooling | Repository does not provide a complete license ledger | Retained in place; license UNKNOWN where not documented |
| `Assets/AI Toolkit`, `Assets/EmbersStorm-Magic Fantazy Armors Pack`, `Assets/Free_Nature_Ambient`, `Assets/MaleCharVocSFXLITE`, `Assets/Quirky Series Ultimate` | Vendor/demo content with no confirmed runtime dependency in this audit | License evidence incomplete in repository | Retained conservatively; candidate for a later dependency/licensing review |

## Removed template/demo leftovers

After `AssetDatabase.GetDependencies` and path/reference checks, these unreferenced
Unity template leftovers were removed: `Assets/Readme.asset`, `Assets/TutorialInfo`,
`Assets/Scenes/SampleScene.unity`, the empty `Assets/Editor` directory and its
orphan metadata. The imported wrappers for audio, placeables and icons were also
removed after runtime assets were migrated with Unity AssetDatabase operations.

`Assets/Settings/PC_RPAsset.asset`,
`Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`, and
`Assets/Settings/SampleSceneProfile.asset` were preserved. GUID
`10fc4df2da32a41aaa32d77bc913491c` still resolves to the SampleSceneProfile used
by the project render-pipeline configuration.
