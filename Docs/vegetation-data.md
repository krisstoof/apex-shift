# Vegetation data authoring

## Current production architecture

Production uses tropical habitat profiles. Legacy five-biome vegetation tables
remain development/serialized compatibility content and do not drive placement.
The #106 [migration validation](testing/issue106-migration-validation.md) checks
actual generated instances against cached environment and species constraints,
alongside the existing planner rejection tests.

Production placement uses `HabitatVegetationCatalog.asset` and neutral
`HabitatVegetationProfileAsset` profiles in
`Assets/_Project/Data/Vegetation/Habitats/`. The active relative densities are:

| Habitat | Density | Mix |
| --- | ---: | --- |
| coast | 0.45 | Sparse broadleaf trees (0.25 multiplier), shrubs (0.6), groundcover |
| lowland_jungle | 1.55 | Broadleaf trees, shrubs, groundcover |
| jungle_interior | 1.85 | Broadleaf trees, shrubs, groundcover |
| wet_jungle | 2.10 | Broadleaf trees, shrubs, groundcover |
| rocky_upland | 0.45 | Shrubs and groundcover; no large trees |

No vegetation profile exists for water. Existing conifer and dead-tree species
assets remain available but are not in the active tropical mix. No new models or
placeholder tropical assets are generated.

`VegetationCatalog.asset` resolves stable species IDs. Each
`VegetationSpeciesAsset` declares allowed habitat IDs, slope, normalized
elevation, moisture, minimum/maximum `DistanceToCoast`, and optional allowed
`TerrainType` values. An empty terrain list accepts all land terrain types,
never water; the default maximum coast distance is unbounded (`float.MaxValue`).
`ResourceKind` names a ResourceDefinition kind, not its dropped item ID.

`VegetationForm` describes canopy/understory trees, palms, shrubs, ferns,
broadleaf understory, vines, fallen logs, groundcover and standing dead trees.
It is separate from the unchanged coarse `VegetationCategory` values used by
streaming budgets, spacing and harvesting. Future forms require approved assets;
a conifer is not treated as a palm.

The planner consumes cached authoritative environment samples directly from
IslandTopographyRuntime, without legacy biome mapping or shoreline-point scans.
Both species coast limits and category coastal clearance must pass. Water,
shoreline, incompatible terrain/habitat and environmental ranges are rejected;
player and landmark clearings and spatial spacing remain enforced.

WorldGeneratorRuntime uses an explicitly assigned habitat catalog first, then
the transitional `BiomeCatalogAsset.HabitatVegetationCatalog` reference. This
data anchor lets the existing serialized RuntimeWorld setup use habitat
vegetation without rewriting the scene. BiomeVegetationProfileAsset and
`Data/Vegetation/Biomes/` are legacy compatibility data, not production placement
sources; broader removal belongs to #106. Fauna/resource biome systems are
unchanged.

Create/update with `Apex Shift/World/Create or Update Vegetation Data`. Repeated
runs do not duplicate assets and preserve assigned VisualPrefab and
DepletedVisualPrefab references. Validate with
`Apex Shift/Validation/Validate Vegetation Data`; validation reports missing
dependencies and malformed data without repairing assets.

Chunking, streaming, pooling and gameplay budgets remain unchanged: distant
decorations need not have GameObjects, while streamed-out harvestable trees
retain their state. Stable InstanceId uses seed, species ID and quantized X/Z,
not habitat naming. TreeId equals placement InstanceId and remains stable across
stream out/in and save/load; the tree-save DTO and lifecycle are unchanged.
Reports/debug counters use habitat/species keys and habitat/terrain/coast
rejection reasons.
When VisualPrefab is unassigned, the creator binds existing repository wrappers
under `Assets/_Project/Prefabs/World/Resources/Embersstorm/`:
`tree_leafy_01` → `ES_LeafyTree`, `tree_conifer_01` → `ES_ConiferTree`,
`tree_dead_01` → `ES_DryTree`, `shrub_forest_01` → `ES_BerryBush`,
and `groundcover_forest_01` → `ES_GrassPatch`. These provide the current vertical
slice, not new SpeedTree models. Existing manual visuals and depleted/stump
references remain untouched. Missing defaults cause an explicit authoring error.
Conifers and dead trees remain outside the active tropical mix.
