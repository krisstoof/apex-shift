# Habitat fauna configuration

Gameplay behavior is selected by CreatureRole, independently of SpeciesId:
SmallPrey, HerbivoreOmnivore, Predator, or Scavenger. SpeciesDefinition owns
health/hunger/diet, environment eligibility, physical hitbox, meat/bone yield,
and CreaturePopulationRules. Scavenger is supported without an active species.

Production GameBalanceConfig references:

| Species | Role | Eligible habitats | Initial population / absolute cap |
| --- | --- | --- | --- |
| island_small_prey | SmallPrey | coast, lowland_jungle, jungle_interior, wet_jungle | 4–8 / 8 |
| island_forager | HerbivoreOmnivore | lowland_jungle, jungle_interior, wet_jungle | 3–6 / 6 |
| island_predator | Predator | jungle_interior, wet_jungle, rocky_upland | 0 / 5 |

Predator population capacity first grows on day 2, then by one every two days,
capped at five. Daily attempts/chance and minimum player spawn distance are
per-species data (30 units for prey/forager, 48 for predator). Coast prey are
restricted by shoreline exclusion and minimum coast distance; upland is excluded.

CreaturePopulationPlanner uses a local RNG derived from seed, day and SpeciesId.
It validates cached EnvironmentSample land/water/shoreline, habitat, terrain,
elevation, moisture, slope, distance to coast, XZ bounds and player clearance.
Placement Y is the authoritative sample Height. Initial count is sampled from
the configured range; daily additions cannot exceed the day cap. Living, enabled
entries in EcosystemRuntime.Creatures are the population source, not a cumulative
spawn counter or legacy BiomeDefinition creature entries.

PrefabRegistry uses the three new IDs and existing animal prefabs as a temporary
vertical slice; no new models are required. WorldSpawnService fallbacks select
visual size by role. Tent danger, LOD, prey eligibility, audio and map markers
also select by role. Existing plant-first forager behavior and emergency
scavenging/predation remain intact.

CreatureSpeciesCompatibility centralizes load/config aliases:
small_prey → island_small_prey, grazer → island_forager,
varnak → island_predator. Newly generated instances and saves use canonical IDs.
CreatureId is a compatibility alias of SpeciesId, not a second identity.

New saves persist currentHabitatId, homeHabitatId and populationHabitatId.
On load these take precedence over legacy biome fields. Old biome values are
converted by LegacyBiomeCompatibility (westwood → jungle_interior, hearth_meadow
→ coast, south_thicket → wet_jungle, stoneback_ridge → rocky_upland).
Restore uses the generator's prefab registry/composition, with generic fallback
only when unavailable, then restores health, needs, behavior, habitat memory,
cooldown, niche, hunt drive and death.

Legacy species assets, Godot ecosystem biomass DTOs/event names and audio clip
collections remain for compatibility. EcosystemBalanceConfig's old population
fields describe legacy biomass simulation only; they do not drive production
creature counts. Terrain, habitat thresholds and vegetation are unchanged.
