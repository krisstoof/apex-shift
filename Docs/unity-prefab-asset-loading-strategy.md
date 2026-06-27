# Unity prefab and asset loading strategy

Issue: #16 `[UNITY] Add asset and prefab loading strategy`

## Goal

Keep prefab references in one explicit configuration asset instead of scattering hard references and string/path lookups across scenes and runtime systems.

The current strategy is intentionally simple:

```text
ScriptableObject PrefabRegistry first
legacy scene lists as temporary fallback
fallback primitives only for development
Addressables later, not now
```

## Current registry

Create a registry asset in Unity:

```text
Create -> Apex Shift -> World -> Prefab Registry
```

The registry class is:

```text
Assets/_Project/Scripts/Runtime/World/Generation/PrefabRegistry.cs
```

It owns three groups:

```text
ResourcePrefabEntry[]
CreaturePrefabEntry[]
BuildingPrefabEntry[]
```

Runtime lookup methods:

```csharp
TryGetResourcePrefab(VegetationSpawnKind kind, out GameObject prefab)
TryGetCreaturePrefab(string creatureId, out GameObject prefab)
TryGetBuildingPrefab(string buildingId, out GameObject prefab)
```

The registry may contain multiple variants for the same key. The runtime can pick a random matching prefab, which is useful for visual variety.

## World generator usage

`WorldGeneratorRuntime` should reference one `PrefabRegistry` asset.

Preferred flow:

```text
WorldGeneratorRuntime
  -> PrefabRegistry
    -> resource / creature / building prefab
```

The generator may keep old serialized lists temporarily:

```text
Legacy Prefab Lists - prefer PrefabRegistry
```

These lists exist only as migration fallback. New prefab configuration should go into the registry asset.

## Resource prefabs

Resource prefabs are keyed by `VegetationSpawnKind`.

Examples:

```text
GrassOrFlower
ConiferTree
LeafyTree
DryTree
Rock
BerryBush
GreenBush
DryBush
```

Use this for vegetation, rocks, bushes, and other world resource visuals.

## Creature prefabs

Creature prefabs are keyed by stable creature id.

Examples:

```text
small_prey
grazer
varnak
```

The id should match biome spawn data and creature runtime configuration.

## Building prefabs

Building prefabs are keyed by stable building id.

Examples:

```text
campfire
storage_box
shelter
ruins
old_tree_landmark
```

Do not rely on scene object names as ids.

## Rules

Use these rules for new systems:

1. Do not load prefabs by hardcoded asset paths in gameplay code.
2. Do not spread unrelated prefab lists across many scene components.
3. Do not use `GameObject.Find` to locate prefab assets.
4. Prefer stable ids or enum keys over display names.
5. Keep runtime fallback primitives only as prototype safety, not as final content.
6. Keep the registry small and explicit until the project actually needs heavier asset loading.

## When to consider Addressables

Do not introduce Addressables yet.

Consider Addressables later when at least one of these becomes true:

```text
build size becomes a real problem
assets must be loaded/unloaded by biome or region
large memory pressure appears on target hardware
loading screens are dominated by asset creation/loading
DLC or modding becomes a real requirement
asset bundles become necessary
registry assets become too large to maintain comfortably
```

Until then, a `ScriptableObject` registry is simpler, more transparent, easier to debug, and enough for the current prototype/vertical slice stage.

## Out of scope

This issue does not include:

```text
streaming assets
asset bundles
DLC
modding
hot reload
custom asset manager
full Addressables migration
```

## Acceptance checklist

`#16` is complete when:

```text
PrefabRegistry exists as ScriptableObject
ResourcePrefabEntry exists
CreaturePrefabEntry exists
BuildingPrefabEntry exists
WorldGeneratorRuntime can get resource prefab by VegetationSpawnKind
WorldGeneratorRuntime can get creature prefab by creature id
legacy scene lists remain only as fallback
documentation explains current strategy and future Addressables threshold
```
