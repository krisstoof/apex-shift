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
Every production profile supplies a nonempty AllowedHabitats set; neither placement
pass may relax it. The helper rejects an empty production habitat list.
Scoring rewards preferred habitat/terrain, elevation, slope, moisture, coast bands
and relative anchor distance, with a small stable seed/ID/position hash jitter.
Coordinate tie-breaks make candidate iteration order irrelevant.

Crash permits only lowland/coast and a hard coast distance of 8–60, preferring 16–40,
with slope at most 14 degrees and no ridge. Base permits only rocky_upland/jungle_interior.
Freshwater prefers moist land (not ocean), at least 30 units from crash.
Cache is at least 45 units away. Camp is at least 60 units away and prefers greater
interior depth and a distance around 60 from cache. Base prefers rocky upland/hills/ridge
and a start distance at least 90; controlled fallback retains an absolute minimum of 60.
These are spatial preferences, not a clue chain or mandatory Euclidean progression.
Placement reserves crash first, then the constrained base site, followed by freshwater,
cache, camp and old tree. This prevents flexible landmarks from consuming every legal
base candidate; no safety/separation constraint is weakened and discovery order is unchanged.

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

Full EditMode: 189/189 passed. Full PlayMode through Unity MCP: 302/302 passed.
The production integration test exercised seeds 12345, 81281 and 91284 and real
GameSaveService serialization/restoration, not just direct LandmarkRuntime DTO calls.

| Seed | Crash to cache | Crash to camp | Crash to entrance | Entrance environment |
| --- | ---: | ---: | ---: | --- |
| 12345 | 65.115 | 71.554 | 100.320 | rocky_upland / Ridge |
| 81281 | 60.000 | 68.000 | 60.926 | rocky_upland / Ridge |
| 91284 | 60.000 | 75.472 | 64.498 | rocky_upland / Ridge |

Entrances for 81281 and 91284 use the controlled preferred-distance fallback, retaining
the hard habitat restriction, 20-unit coast clearance, slope safety, 60-unit crash minimum
and separation. No terrain or habitat algorithm is changed to manufacture an ideal candidate.

For all three seeds: crash/cache/camp are lowland_jungle, freshwater is wet_jungle,
base is rocky_upland/Ridge, and old tree is jungle_interior. Crash DistanceToCoast:
12345 = 32.03, 81281 = 39.07, 91284 = 39.13 (hard maximum 60).
