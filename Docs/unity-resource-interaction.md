# Unity Resource Interaction

Issue: `#8 [UNITY] Add resource interaction and harvesting foundation`

## What was added

Core:

- `ResourceId`
- `ResourceDefinition`
- `ResourceState`
- `HarvestResult`
- `HarvestSystem`

Runtime:

- `IInteractable`
- `PlayerInteractionController`
- `ResourceNodeView`
- `PlayerInventoryRuntime`
- `ResourcePlaceholderPrefabBuilder`

## Runtime setup

1. Add `PlayerInventoryRuntime` to the player.
2. Add `PlayerInteractionController` to the player.
3. Make sure the player already has `PlayerInputReader`.
4. Add `ResourceNodeView` to the resource object.
5. Add a trigger collider to the resource object, or let the component create one during setup.
6. Press Play, move into range, and press Interact.

## Inspector checklist

For the player:

- `PlayerInputReader`
- `PlayerInventoryRuntime`
- `PlayerInteractionController`

For a resource prefab:

- `ResourceNodeView`
- trigger collider
- `resourceKind` set to a supported kind like `conifer_tree`, `rock`, or `bush`

## Default drops

- `conifer_tree` and `tree`: `wood x4`
- `leafy_tree`: `wood x4`
- `dry_tree`: `wood x3`
- `rock`: `stone x2`
- `bush`: `fiber x2`
- `dry_bush`: `fiber x1`
- `small_bush`: `fiber x1`
- `berry_bush`: non-player-harvestable

## Placeholder prefabs

Use:

```text
Tools/Apex Shift/Create Resource Placeholder Prefabs
```

This creates simple placeholder resource prefabs under:

```text
Assets/_Project/Prefabs/Resources
```

## Notes

- Harvesting is intentionally simple for this slice.
- The runtime uses inventory transfer only.
- Regrowth, tool checks, and final art assets remain future work.
