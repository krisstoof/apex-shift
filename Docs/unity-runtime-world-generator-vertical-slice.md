# Runtime World Generator Vertical Slice

This document describes the runtime world generation system implemented in the Apex Shift project.

## Overview

The `WorldGeneratorRuntime` component provides a data-driven way to generate game worlds at runtime using `BiomeDefinitionAsset` and `BiomeCatalogAsset`. It serves as a vertical slice for procedural generation, utilizing Unity prefabs and terrain planes.

## Key Components

### WorldGeneratorRuntime
The compatibility `MonoBehaviour` facade. Its public generation/save API remains stable, while lifecycle work is delegated to:

```text
WorldGeneratorRuntime
        |
        v
WorldGenerationCoordinator
        +-- RuntimeCompositionRoot
        +-- terrain/biome generation
        +-- WorldSpawnService
        +-- RuntimeCameraSetup
        +-- RuntimeAudioSetup
        +-- WorldNavMeshBuildStage
        |
        v
WorldRuntimeOwner
```

`WorldGenerationContext` represents one generation session. `WorldRuntimeOwner` owns its `GenerationRoot` and clears only that hierarchy, so unrelated objects named `Player`, `TerrainRoot`, or `Main Camera` are not removed.

The coordinator runs deterministic stages in order: preparation, roots, composition, terrain/biomes, resources, landmarks, player, camera, NavMesh, creatures, and finalization. Unity's random state is restored after every generation.

The main facade still provides:
- **Compatibility API**: `Generate()`, `ClearGeneratedWorld()`, `SetSeed()`, `SetBiomeCatalog()`, `GetLastResult()`, `OnGenerationComplete`.
- **Fixed Layout**: Generates five regions (Westwood, Stoneback Ridge, Hearth Meadow, South Thicket, Redfang Wilds) in a cross formation.
- **Terrain Generation**: Creates terrain planes using biome-specific colors or materials.
- **Resource Spawning**: Spawns vegetation and resources within each biome region based on its definition.
- **Fallbacks**: If specific prefabs are not configured, the system uses primitives (cylinders for trees, cubes for rocks, spheres for bushes).

### Data Structures
- **WorldGenerationSettings**: Controls region size and padding.
- **PrefabRegistry**: The canonical source for resource and creature prefabs.
- **WorldGenerationResult**: Stores metadata about the last generation pass (seed, biome count, resource count).

## How to Use

1. **Editor Menu**: Go to `Tools > Apex Shift > World > Create Runtime World Generator Scene` to quickly set up a test environment.
2. **Manual Setup**:
   - Add a `WorldGeneratorRuntime` component to a GameObject.
   - Assign a `BiomeCatalogAsset`.
   - Configure the project `PrefabRegistry` with your desired prefabs.
   - Press "Generate World" in the component's context menu or enable "Generate On Start".

## Interaction Integration

Spawned resources are automatically configured with `ResourceNodeView`.
- **Resource Kind**: Passed from the `VegetationSpawnEntryAsset`.
- **Colliders**: Each resource receives a `SphereCollider` trigger for interaction detection, following the interaction system conventions.

## Debugging

- **Gizmos**: The generator draws colored wireframes in the Scene view to visualize biome region bounds.
- **WorldGenerationDebugPresenter**: A debug overlay displays the current seed and generated counts (biomes and resources) in the Game view without coupling diagnostics to the generator facade.
