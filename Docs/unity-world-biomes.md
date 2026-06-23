# Unity World Biomes

The handcrafted biome world is generated through:

`Tools > Apex Shift > World > Create Handcrafted Biome World`

Generated scene:

`Assets/_Project/Scenes/BiomeWorldTest.unity`

## Biome identities

### Westwood

Dense coniferous forest. Uses pine/conifer/spruce-like assets where available.

### South Thicket

Deciduous forest. Uses broadleaf/tree/shrub assets where available.

### Hearth Meadow

Open meadow and central player start area. Sparse trees and meadow details.

### Stoneback Ridge

Rocky ridge with sparse conifers. Uses rock/boulder/stone assets and a few conifers.

### Redfang Wilds

Dry wildlands. Uses dry/dead trees, dry shrubs and rocky details where available.

## Asset discovery

The builder uses `AssetDatabase.FindAssets` to discover available nature prefabs by vegetation role, not by one broad `tree` keyword. It prefers results that look like `Low Poly Trees & Nature Pack`, but it still falls back to role-matching nature prefabs and then to primitive placeholders if nothing suitable is installed.

If the package is missing, the builder falls back to simple primitive placeholder objects.

## Vegetation role system

Vegetation is now assigned through explicit roles so each biome keeps its own identity instead of mixing random green assets together.

Roles:

`ConiferTree`, `LeafyTree`, `DryTree`, `Rock`, `GreenBush`, `DryBush`, `GrassOrFlower`, `BerryBush`

Each biome profile contains fixed counts per role. The builder scores prefabs against the role, logs the prefab pool sizes, and only spawns from the matching pool.

Biome role targets:

- Westwood: conifer-heavy, a few leafy trees, some rocks, green bushes, grass, and berries
- South Thicket: leafy thicket with bushes and grass
- Hearth Meadow: sparse trees, light greenery, and open ground
- Stoneback Ridge: rocks dominate, with only a few trees and hardy shrubs
- Redfang Wilds: dry trees, dry bushes, and rocks, with almost no healthy green vegetation

This prevents generic broadleaf, conifer, and bush prefabs from bleeding into the wrong biome.

## Map shape

The handcrafted biome world is generated as one continuous oval/blob-like landmass.
Biomes are assigned per terrain tile inside the landmass. This avoids the old cross-shaped layout caused by five rectangular biome patches.

## Biome identity source

The biome identities are based on the old Godot prototype `WorldConfig.BIOME_ZONES`.

### Westwood

Dense coniferous forest. Dark green ground. Mostly pine/conifer trees.

### Stoneback Ridge

Rocky ridge. Gray/stone ground. Rocks dominate, sparse conifers and dry shrubs.

### Hearth Meadow

Central meadow. Open grassland with sparse deciduous trees, bushes and berries.

### South Thicket

Deciduous thicket. Leafy trees, bushes, small bushes and grass.

### Redfang Wilds

Dry dangerous wildlands. Brown/dry ground. Dry trees, dry bushes and rocks.

## Asset material matching

The biome builder searches Low Poly Trees & Nature Pack materials by keyword and applies them to generated biome ground materials where possible. If no matching material is found, it uses the old Godot biome color as fallback.

## Biome material rule

Low Poly Trees & Nature Pack materials can provide textures, but biome identity color comes from the old Godot prototype biome profile.

This prevents all ground areas from turning into the same green material when the asset pack returns similar grass/ground materials for several biomes.

## Troubleshooting

### Pink or magenta materials

Pink materials usually mean Unity could not use the shader on that material in the current render pipeline. The world builder now repairs renderer materials into generated URP-compatible assets under `Assets/_Project/Materials/Generated/Nature`.

If you still see magenta objects, check that:

1. The project is using URP.
2. The relevant prefabs are imported correctly.
3. The Console does not report missing shader or material errors.

### Camera not centered on the player

The handcrafted world uses a snap-to-target isometric camera that focuses on the player and keeps the player near the center of the view. If the player still appears off-center, make sure you open the generated scene from `Assets/_Project/Scenes/BiomeWorldTest.unity` and press Play in that scene, not in an older test scene.

## World boundaries

The handcrafted biome world stores generated land tile centers in a `WorldBounds` runtime component.  
The player controller checks this component before applying movement, so the player cannot leave the generated landmass.

The builder also creates a visual `BoundaryRoot` to make the edge of the world readable.

## Camera-relative movement

Player movement is camera-relative.  
W/S/A/D moves the player relative to the current isometric camera view instead of raw Unity X/Z axes.

## Manual test

1. Open Unity.
2. Click `Tools > Apex Shift > World > Create Handcrafted Biome World`.
3. Open `Assets/_Project/Scenes/BiomeWorldTest.unity`.
4. Press Play.
5. Walk through every biome.
6. Confirm:
   - Westwood is dense and coniferous.
   - South Thicket is broadleaf/deciduous.
   - Hearth Meadow is open.
   - Stoneback Ridge is rocky.
   - Redfang Wilds looks dry and wild.
7. Confirm the camera follows the player.
8. Confirm there are no Console errors.
