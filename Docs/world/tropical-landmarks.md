# Tropical landmarks (#100)

New games generate these six stable IDs, independent of seed or coordinates:

| ID | Type / display name | Initially discovered | Vegetation clearance |
| --- | --- | --- | --- |
| plane_crash | PlaneCrash / Plane Crash | yes | 20 |
| freshwater_source | FreshwaterSource / Freshwater Source | no | 10 |
| smuggler_cache | SmugglerCache / Smuggler Cache | no | 8 |
| smuggler_camp | SmugglerCamp / Smuggler Camp | no | 14 |
| base_entrance | BaseEntrance / Hidden Entrance | no | 12 |
| old_tree | OldTree / Great Old Tree | no | 18 |

## Placement and start anchor

LandmarkPlacementPlanner is a pure consumer of candidates sampled from the authoritative
dense EnvironmentSample cache every four world units. Positions receive the same
surface-height sampler used by the terrain. It never targets fixed X/Z coordinates,
queries legacy biome IDs, instantiates objects, or changes Unity Random state.

Serializable LandmarkPlacementProfile defines hard habitat/terrain eligibility,
slope/elevation/moisture/coast ranges, start/anchor distances and separation.
Scoring rewards preferred habitat/terrain, elevation, slope, moisture, coast bands
and relative anchor distance, with a small stable seed/ID/position hash jitter.
Coordinate tie-breaks make candidate iteration order irrelevant.

Crash prefers lowland/coast, coast distance 16–40, slope at most 14 degrees and no ridge.
Freshwater prefers moist land (not ocean), at least 30 units from crash.
Cache is at least 45 units away. Camp is at least 60 units away and prefers greater
interior depth and a distance around 60 from cache. Base prefers rocky upland/hills/ridge
and a start distance at least 90; controlled fallback retains an absolute minimum of 60.
These are spatial preferences, not a clue chain or mandatory Euclidean progression.

All landmarks reject water/shoreline, unsafe slopes and overlaps.
Separation uses the greater of each pair's configured radii (generic 18, crash/camp 25,
base 30). A second placement pass may relax preferred habitat/terrain and coast/start distance bands,
while still scoring the original environmental preferences. Base always remains at least
20 units inland. Preferred rocky terrain can therefore beat an unsuitable distant flat
site when the 90-unit preference cannot be satisfied in the interior. Hard constraints
are never relaxed. No valid candidate returns no placement with a warning,
not a pile of landmarks at safe spawn. Results expose habitat, terrain, physical fields,
score and UsedFallback for tests/diagnostics.

WorldGeneratorRuntime resolves a deterministic, revalidated 4.5-unit offset around
plane_crash, caches it for the generation, and shares it between player spawn,
vegetation start clearing and streaming initial target. If no safe crash offset exists,
it uses the existing topography safe-spawn fallback. Creature player-distance checks
consume the resulting actual player position.

## Discovery, maps and saves

LandmarkDiscoveryRuntime checks registered PlayerPresenceRuntime proximity (12 units),
without scans. Discover() is idempotent and emits Discovered once.
Both maps reject undiscovered/inactive landmarks, including Hidden Entrance.
All landmarks physically exist immediately; there are no unlock/objective/story systems.

GameSaveService captures discovery and position by stable ID. ApplyLoadedState regenerates
then applies saved state to existing IDs, preserving discovery without duplication.
Missing legacy entries are restored under the generation-owned LandmarkRoot.
OldTree=1, Ruins=2, Pond=3, Camp=4, CavePlaceholder=5 retain their serialized enum values.
ruins, pond, camp and cave_placeholder parsing/visual restoration remain solely for old
saves until the later compatibility cleanup; new games do not generate them.

## Temporary visuals and scope

Old tree retains its authored visual. Freshwater, camp and entrance reuse existing pond,
camp and cave visuals. Crash and cache use explicitly named prototype primitive visuals,
not final assets. No models are downloaded/generated and no authored art is modified.
BaseEntrance is an exterior marker only, with no underground/interior or story logic.

Regression coverage: synthetic planner constraints/determinism/legacy parsing; three-seed
full production generation, actual vegetation placement clearances, registered-player
proximity, both map visibility predicates, and serialized GameSaveService save/load
including legacy restore and owned-root cleanup. RuntimeWorld scene shell is not resaved.

## Validation (Unity 6000.6.2f1)

Full EditMode: 186/186 passed. Full PlayMode through Unity MCP: 302/302 passed.
The production integration test exercised seeds 12345, 81281 and 91284 and real
GameSaveService serialization/restoration, not just direct LandmarkRuntime DTO calls.

| Seed | Crash to cache | Crash to camp | Crash to entrance | Entrance environment |
| --- | ---: | ---: | ---: | --- |
| 12345 | 65.115 | 105.679 | 62.097 | jungle_interior / Hills |
| 81281 | 62.225 | 73.539 | 82.073 | lowland_jungle / Plain (fallback) |
| 91284 | 60.000 | 65.970 | 73.430 | rocky_upland / Ridge |

All three entrances use the controlled fallback: the preferred 90-unit rocky/interior
combination cannot be satisfied with the existing environment and landmark separation.
Seed 81281 also relaxes the preferred habitat/terrain, but never water, shoreline,
20-unit coast clearance, slope safety, 60-unit crash minimum or separation. No terrain
or habitat algorithm is changed to manufacture an ideal candidate.
