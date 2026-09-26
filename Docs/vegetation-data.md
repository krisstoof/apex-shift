# Vegetation data authoring

The vegetation data model introduced for #90 is authored independently from
the current legacy vegetation spawner. `VegetationCatalog.asset` resolves stable
species IDs; biome profiles reference catalog species by asset and define their
weights and density multipliers. `BiomeDefinitionAsset.VegetationProfile` is
the canonical profile reference. Its previous vegetation list remains intact
for compatibility until the later spawning migration.

Create or update the catalog, five species and five biome profiles with
`Apex Shift/World/Create or Update Vegetation Data`. The operation is
idempotent and preserves manually assigned presentation prefab references.
Validate via `Apex Shift/Validation/Validate Vegetation Data`; validation never
repairs assets.

Species IDs: `tree_leafy_01`, `tree_conifer_01`, `tree_dead_01`,
`shrub_forest_01`, and `groundcover_forest_01`. Profiles are provided for
`hearth_meadow`, `westwood`, `south_thicket`, `stoneback_ridge`, and
`redfang_wilds`.

This checkout does not contain verified SpeedTree presentation prefabs for
these species. The generated species assets intentionally have unassigned
visual prefabs; the validator reports each as a content dependency. Do not
replace them with primitives or unrelated vegetation prefabs. Assign approved
SpeedTree prefabs when those dependencies are added. No runtime spawner,
harvesting behavior, or legacy vegetation assets are changed here.
