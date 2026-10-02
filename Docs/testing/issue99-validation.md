# Issue #99 validation

Validation performed locally through Unity MCP in Unity 6000.6.2f1 on main.

- Full EditMode: 166 passed, 0 failed (2026-10-02 14:18:12–14:18:25 UTC).
- Full PlayMode: 299 passed, 0 failed (2026-10-02 14:19:06–14:19:31 UTC).
- Local NUnit XML: Logs/Issue99_EditMode.xml and Logs/Issue99_PlayMode.xml (ignored artifacts, not GitHub Actions results).
- Production RuntimeWorld New Game: day 1 had 8 island_small_prey and 5 island_forager, all on land; player and owned GenerationRoot existed.
- Advancing one day at a time: day 2 added one island_predator; days 3–5 retained one, below the configured day cap. Capacity is a ceiling, not a guaranteed daily count.
- No Console exceptions during the production smoke check. This was a runtime/API smoke check, not a full manual movement/combat playthrough.
- Existing parity, save/load, registry and generator lifecycle tests were included in the full suites.
- Planner coverage verifies water/outside/shoreline/habitat/slope/range/player-distance rejection, caps, generic growth, deterministic initial/daily placements and unchanged Unity random state.
- Lifecycle coverage verifies role-based prey/tent/feeding, saved species/role/health/needs/habitat/cooldown/niche/hunt drive, legacy varnak+westwood migration, and generated population caps.

## Scope and retained compatibility

RuntimeWorld.unity, vegetation, terrain, habitat thresholds and generated animation assets are not changed by this commit. The pre-existing local AnimatorController modification is preserved byte-for-byte and excluded from #99. Test-resaved BiomeWorldTest was backed up locally and restored to its pre-test version.

Legacy species IDs remain only at compatibility/config input boundaries, in audio asset collection/path names, serialization aliases, historical diagnostics/event labels and legacy ecosystem DTOs. Production behavior, hitboxes, drops, tent, LOD and map decisions use role/profile data, not those IDs. WorldGeneratorRuntime has no legacy biome creature entries or Varnak population flow.

## Changed files

Paired .meta files are included for new Unity scripts/assets.

- ssets/_Project/Config/GameBalanceConfig.asset
- Assets/_Project/Data/World/PrefabRegistry.asset
- Assets/_Project/Scripts/Core/Save/CreatureSaveData.cs
- Assets/_Project/Scripts/Editor/World/WorldGeneratorRuntimeSceneBuilder.cs
- Assets/_Project/Scripts/Presentation/HUD/MapScreenUI.cs
- Assets/_Project/Scripts/Presentation/HUD/MiniMapUI.cs
- Assets/_Project/Scripts/Runtime/Buildings/TentSleepRuntime.cs
- Assets/_Project/Scripts/Runtime/Config/CreatureSimulationLodConfig.cs
- Assets/_Project/Scripts/Runtime/Config/EcosystemBalanceConfig.cs
- Assets/_Project/Scripts/Runtime/Config/GameBalanceConfig.cs
- Assets/_Project/Scripts/Runtime/Config/SpeciesDefinition.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreatureAgentView.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreatureAudioRuntime.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreatureBehaviorBrain.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreatureHealthRuntime.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreatureHitboxRuntime.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreatureMeatDropFactory.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreaturePlayerAwarenessBehavior.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreatureSimulationLodRuntime.cs
- Assets/_Project/Scripts/Runtime/Ecosystem/CreatureNeedsRuntime.cs
- Assets/_Project/Scripts/Runtime/Ecosystem/EcosystemRuntime.cs
- Assets/_Project/Scripts/Runtime/Save/GameSaveService.cs
- Assets/_Project/Scripts/Runtime/World/Generation/PrefabRegistry.cs
- Assets/_Project/Scripts/Runtime/World/Generation/WorldGeneratorRuntime.cs
- Assets/_Project/Scripts/Runtime/World/Generation/WorldSpawnService.cs
- Assets/_Project/Scripts/Runtime/World/Query/WorldQueryRuntime.cs
- Assets/_Project/Scripts/Tests/Editor/WorldGeneratorBiomeSpawnTests.cs
- Assets/_Project/Scripts/Tests/GodotParity/CreatureSpeciesGodotParityTests.cs
- Assets/_Project/Scripts/Tests/Regression/DebugDataParityTests.cs
- Assets/_Project/Scripts/Tests/Regression/GrazerBehaviorParityTests.cs
- Assets/_Project/Scripts/Tests/Regression/SaveLoadParityTests.cs
- Assets/_Project/Scripts/Tests/Regression/SmallPreyBehaviorParityTests.cs
- Assets/_Project/Scripts/Tests/Unit/Creatures/CreatureSimulationLodRuntimeTests.cs
- Docs/migration/balance-parity-report.md
- Docs/survival/tent-sleep.md
- Docs/testing/tester-build-checklist.md
- Docs/unity-prefab-asset-loading-strategy.md
- Assets/_Project/Config/SpeciesDefinition_island_forager.asset
- Assets/_Project/Config/SpeciesDefinition_island_forager.asset.meta
- Assets/_Project/Config/SpeciesDefinition_island_predator.asset
- Assets/_Project/Config/SpeciesDefinition_island_predator.asset.meta
- Assets/_Project/Config/SpeciesDefinition_island_small_prey.asset
- Assets/_Project/Config/SpeciesDefinition_island_small_prey.asset.meta
- Assets/_Project/Scripts/Runtime/Config/CreaturePopulationRules.cs
- Assets/_Project/Scripts/Runtime/Config/CreaturePopulationRules.cs.meta
- Assets/_Project/Scripts/Runtime/Creatures/CreaturePopulationPlanner.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreaturePopulationPlanner.cs.meta
- Assets/_Project/Scripts/Runtime/Creatures/CreatureRole.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreatureRole.cs.meta
- Assets/_Project/Scripts/Runtime/Creatures/CreatureSpeciesCompatibility.cs
- Assets/_Project/Scripts/Runtime/Creatures/CreatureSpeciesCompatibility.cs.meta
- Assets/_Project/Scripts/Tests/Editor/CreaturePopulationPlannerTests.cs
- Assets/_Project/Scripts/Tests/Editor/CreaturePopulationPlannerTests.cs.meta
- Assets/_Project/Scripts/Tests/Regression/HabitatCreatureLifecycleTests.cs
- Assets/_Project/Scripts/Tests/Regression/HabitatCreatureLifecycleTests.cs.meta
- Docs/creature-habitat-profiles.md
- Docs/testing/issue99-validation.md
