# Landmark runtime and player animation polish

This document covers Batch D:

- #54 landmark runtime and map markers
- #60 player character animation binding

## Landmark runtime

Runtime landmark support is based on:

```text
Assets/_Project/Scripts/Runtime/World/Landmarks/LandmarkRuntime.cs
Assets/_Project/Scripts/Runtime/World/Landmarks/LandmarkRegistry.cs
Assets/_Project/Scripts/Runtime/World/Landmarks/LandmarkType.cs
Assets/_Project/Scripts/Runtime/World/Landmarks/LandmarkWorldGenerator.cs
```

Generated landmark types:

- `old_tree`
- `ruins`
- `pond`
- `camp`
- `cave_placeholder`

Each landmark stores:

- stable id
- type
- display name
- description
- world position
- `discovered` flag for future fog-of-war/discovery work

## Map/minimap

Landmark markers are read from `LandmarkRegistry.Landmarks`.

Integrated views:

```text
Assets/_Project/Scripts/Presentation/HUD/MapScreenUI.cs
Assets/_Project/Scripts/Presentation/HUD/MiniMapUI.cs
```

The full map and minimap use landmark colors/sizes based on `LandmarkType`.

## Save/load

Landmarks are serialized through:

```text
Assets/_Project/Scripts/Core/Save/LandmarkSaveData.cs
Assets/_Project/Scripts/Core/Save/WorldSaveData.cs
Assets/_Project/Scripts/Runtime/Save/GameSaveService.cs
```

`GameSaveService` captures `LandmarkRegistry.CaptureSaveData()` into `WorldSaveData.landmarkStates` and restores missing landmarks through `LandmarkRegistry.RestoreFromSaveData()`.

## Snapshot/debug

`WorldDebugSnapshot` now stores both:

- `landmarkCount`
- `discoveredLandmarkCount`

`GameSnapshotProvider` fills these from `LandmarkRegistry`.

## Player animation binding

Player animation binding uses:

```text
Assets/_Project/Scripts/Runtime/Player/PlayerAnimationDriver.cs
Assets/_Project/Scripts/Runtime/Player/KevinIglesiasPlayerAnimationBinder.cs
```

The runtime flow is:

1. `WorldGeneratorRuntime` creates/configures the player.
2. `PlayerAnimationDriver` receives movement/input state.
3. `KevinIglesiasPlayerAnimationBinder` resolves or generates a controller in Editor when imported animation clips are available.
4. Combat and item use call animation triggers through `PlayerAnimationDriver.TriggerItemUse()`.

Supported semantic states/triggers:

- idle
- walk
- run/sprint
- swim
- attack
- spear attack
- bow attack
- axe/chop
- pickaxe/mine
- torch use
- gather/interact
- hurt/death when matching clips exist

## Editor report

Use:

```text
Apex Shift -> Animation -> Player Binding Report
```

This scans imported animation clips, reports which roles were resolved, and can regenerate the generated player controller.

## Manual test

### Landmark test

1. Start New Game.
2. Confirm at least one `Landmark_*` object exists under `LandmarkRoot`.
3. Open map and confirm landmark markers are visible.
4. Check minimap for nearby landmark markers.
5. Save and load.
6. Confirm landmarks are restored and markers still appear.
7. Check debug/snapshot count for `landmarkCount`.

### Animation test

1. Open `Apex Shift -> Animation -> Player Binding Report`.
2. Confirm Idle/Walk/Run clips are resolved or see which clips are missing.
3. Generate/refresh the controller.
4. Start New Game.
5. Check idle, walk, run/sprint.
6. Try spear/bow/axe/pickaxe/torch actions and confirm triggers are sent.
7. Confirm missing clips do not break gameplay and do not spam NullReferenceException.
