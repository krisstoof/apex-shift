# Final smuggler boat escape (#105)

This vertical slice has exactly two requirements: `boat_fuel` (Boat Fuel) and
`boat_battery` (Boat Battery), each with maximum stack size 1. They use ordinary
player inventory and its existing save/load, are non-edible, and have no recipes.
This is not a general vehicle system or full motorboat physics.

## Sources and layout

The #104 room layout and anchor coordinates stay unchanged. The interior builder
adds an EscapeBoatRuntime to SmugglerBoatPlaceholder below BoatAnchor, and creates
two children using cached anchor references:

| Source ID | Item ID | Anchor | Prompt |
| --- | --- | --- | --- |
| smuggler_base_fuel | boat_fuel | FuelAnchor | Take Boat Fuel |
| smuggler_base_battery | boat_battery | BatteryAnchor | Take Boat Battery |

Sources have priority 50 and interaction duration 0.15 seconds. Only the current
generation's player inside this base can collect them. Capacity is checked before
AddItem; a full inventory leaves the source available. Successful collection
records its persistent source ID and hides the entire source, including its
visuals and trigger collider. Repeat or reentrant collection cannot duplicate an
item. Prototype geometry represents a can with handle/cap and battery terminals.
BoatKeyAnchor remains available for future work and is not a requirement.

## Boat state and interaction

`BoatRequirementDefinition.Production` centralizes the two items and source IDs.
State is derived from story and inventory; enum indices are never persisted:

- Unavailable: story is before PrepareBoat without a prepared/escaped milestone.
- Discovered: PrepareBoat is available, but the boat has not been inspected.
- MissingRequirements: inspected boat still needs one or both items.
- Ready: both items are present for installation, or BoatPrepared is persistent.
- Escaped: story is Completed or IslandEscaped has been recorded.

The first valid interaction publishes `boat_discovered` with subject
`smuggler_boat`. It never consumes items, even if both are already carried.
Another interaction with both items atomically validates and consumes the entire
cost, then publishes BoatPrepared once through GameEventBus. Inventory observers
receive one change event after both removals, preventing partial consumption or
reentrant callbacks between fuel and battery. Story's existing transition moves
PrepareBoat to EscapeIsland. Later interactions do not consume extra items.

## Objective detail

StoryProgressionRuntime combines its normal stage text with an optional newline
and ObjectiveDetail. `escape_boat` owns this derived presentation detail; another
owner cannot clear it. It is not saved. The generic ObjectiveHUDView continues
reading CurrentObjectiveText and listens to StateChanged.

| Inventory after inspection | Detail |
| --- | --- |
| Neither item | Missing: Boat Fuel, Boat Battery. |
| Battery only | Missing: Boat Fuel. |
| Fuel only | Missing: Boat Battery. |
| Both items | Return to the boat and install the fuel and battery. |

EscapeBoatRuntime binds the generated player after inventory configuration.
InventoryChanged and story StateChanged recalculate state and detail without
Update polling. The generation-owned subscriptions remain alive when the interior
is inactive, so collecting/dropping inventory outside the base updates the
objective too. Destruction/rebinding removes them. Stage transitions, preparation
and completion clear the detail. The HUD has sufficient height for two lines.

## Controlled final escape

A prepared boat in EscapeIsland offers `Escape island`. This is a separate,
deliberate interaction after installation. It disables player movement, marks a
runtime-only escapeInProgress guard, and publishes FinalEscapeStarted (appended
to GameplayEventKind). RunCompletionHUDView shows `Leaving the island...`.
After two WaitForSecondsRealtime seconds, the boat publishes IslandEscaped once
and the existing story transition enters Completed. Duplicate interactions,
source collection, and interior exit are blocked during the sequence.

Completed state displays `RUN COMPLETE` / `You escaped the island.` and a
`Return to Main Menu` button using GameStartupController.ShowMainMenu.
GameSessionState.EndGameplay and Time.timeScale=0 stop the run; player movement
also remains disabled. No voyage, controls, buoyancy or engine simulation exists.

## Persistence

WorldSaveData.escapeBoatState is additive. EscapeBoatSaveData contains only
collectedRequirementSourceIds. Its property trims/lowercases, ignores empty IDs,
deduplicates and sorts ordinally. Prepared, escaped and story stage remain solely
authoritative in StorySaveData. Capture sorts collected IDs deterministically.

After regeneration, load restores story, inventory, source state, and player area.
Source restoration only changes availability; it never adds items or publishes
story signals. The boat recomputes its detail from the restored state.

- No collected sources: both remain available.
- Fuel collected: fuel remains hidden and inventory contains its saved item;
  battery remains available.
- Both collected before installation: both remain hidden, items remain carried,
  and the objective instructs returning to the boat.
- Prepared save: consumed items stay consumed, story remains EscapeIsland and
  the boat remains Ready without requiring fuel/battery again.
- Save during escape: no coroutine progress is saved. Load restores Ready,
  escapeInProgress=false and normal player movement, allowing a fresh attempt.
- Completed save: completion remains visible and gameplay stays stopped.
  StartGameplay checks the restored story before BeginGameplay or setting time
  to 1, so Continue/Load/Resume cannot restart a completed run.
- Old saves without escapeBoatState default to an empty collected list. A saved
  BoatPrepared milestone still makes the boat Ready without repeating requirements.

RuntimeWorld.unity and Generated assets are not modified by this implementation.

## Validation

Unity 6000.6.2f1, isolated project copy with a headless mouse fixture:
- EditMode: 232/232 passed.
- PlayMode: 329/330 passed, including the production boat acceptance test.
- The sole failure is CreatureAnimationDriverTests.DriverRaisesBlendStateWhenCreatureChases,
  also reproduced before this change (paused-menu blend value 0).

The boat acceptance test covers collection and regeneration, preparation,
save/load during the realtime escape sequence, completion at timeScale=0,
Continue of a completed save, and return to the main menu.
