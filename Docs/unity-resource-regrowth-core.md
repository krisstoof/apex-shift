# Resource Regrowth Core

This document describes the core logic for resource regrowth in Apex Shift, ported from the original Godot implementation.

## Overview

Resources in the world (trees, bushes, etc.) can regrow over time after being harvested. The regrowth system is purely logical and does not handle rendering or prefab switching directly.

## Components

### ResourceGrowthStage
An enum representing the current stage of growth:
- `Harvested` (0): Just gathered, barely visible or stump.
- `Sprout` (1): Small initial growth.
- `Young` (2): Medium growth, not yet harvestable.
- `Mature` (3): Full size, harvestable.

### ResourceGrowthState
Maintains the runtime state of a specific resource instance's growth.
- Tracks `GrowthStage`, `GrowthProgressDays`, and `DaysSinceHarvested`.
- Includes `ToSaveData()` and `FromSaveData()` for persistence.

### ResourceRegrowthRules
Defines the regrowth parameters for different resource kinds.
- `conifer_tree`, `leafy_tree`, `tree`: 3 days
- `dry_tree`: 15 days
- `bush`, `small_bush`, `berry_bush`: 2 days
- `dry_bush`: 3 days
- `grass_patch`, `dense_grass`: 1 day
- `rock`: No regrowth

### ResourceRegrowthSystem
The logic engine that advances growth.
- `MarkHarvested`: Transition a resource to the harvested state.
- `AdvanceDays`: Moves growth forward by a number of days.
- `AdvanceTime`: Convenience method to advance growth using seconds and a seconds-per-day conversion.
- `ForceFullRegrowth`: Immediately restore a resource to its mature state.

## Usage Example

```csharp
// Initialization
var rules = new ResourceRegrowthRules();
var system = new ResourceRegrowthSystem(rules);
var resource = ResourceDefinition.CreateDefault("tree").CreateState();
var growth = new ResourceGrowthState("tree_01", "tree", resource.MaxAmount);

// When harvested
system.MarkHarvested(resource, growth);

// Over time
system.AdvanceDays(resource, growth, 1.0f);

if (growth.GrowthStage == ResourceGrowthStage.Mature) {
    // Resource is restored and harvestable again
}
```

## Godot Reference
This system is based on the logic found in:
- `scripts/core/resources/resource_regrowth_system.gd`
- `scripts/core/resources/resource_state.gd`
- `scripts/core/resources/resource_drop_table.gd`
