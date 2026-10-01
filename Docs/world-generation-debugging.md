# World generation QA diagnostics

Developer diagnostics are opt-in and available only in the Editor or a development build. Enable them through the existing `RuntimeDebugSettings.SetDeveloperDiagnosticsEnabled(true)` switch (or the current diagnostics UI). The diagnostics bootstrap then attaches `WorldGenerationDebugPresenter` to the active generated world; release builds do not create the presenter or its overlay texture.

The generation panel shows the seed, elevation min/max/average, land/water/shore/ridge counts, tropical habitat cell distribution, vegetation totals/categories, rejection aggregates and current vegetation streaming/chunk counters. Press **F6** to cycle the cached map view: None, Elevation, Slope, Moisture, Habitat, Vegetation Density, Harvestable Trees, Vegetation Chunks. Tree view colors distinguish decorative, standing, streaming-active, falling and depleted trees. Chunk view outlines chunk bounds and distinguishes inactive, active vegetation and chunks with active gameplay trees.

`WorldGenerationReport.ToDeterministicString()` is intended for test/debug comparisons. It excludes timestamps, instance IDs and frame timing; keys are ordinally sorted and floats use invariant round-trip formatting. Slope histogram order is fixed at 0–10°, 10–20°, 20–30°, 30–40°, 40°+. Compare reports from identical seed/settings to identify unintended generator changes. Rejection counters are aggregated at the actual candidate rejection point (water, slope, biome, elevation, moisture, shoreline/clearing, spacing); they are diagnostics, not per-point logs.

Map texture generation occurs only when the mode changes, plus a one-second refresh for stateful tree/chunk modes. It does not create one debug object per placement and is absent when diagnostics are disabled.

The Habitat view and report use cached habitat/environment data; gameplay queries do not resample biome Perlin noise. `;habitats=` and `;habitatPercent=` are authoritative land distribution fields. Legacy vegetation profile IDs appear only in the compatibility placement/species breakdown during migration.
