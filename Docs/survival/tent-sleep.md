# Tent sleep survival loop

Implements issue #71: sleeping in a placed tent.

## Runtime flow

A placed `tent` is still represented by `PlaceableStructureRuntime` and saved through the existing building save/load pipeline.

When a structure is configured with `buildingId == "tent"`, it now receives:

```text
TentSleepRuntime
```

The interaction flow is:

1. `PlayerInteractionController` finds `PlaceableStructureRuntime`.
2. `PlaceableStructureRuntime` forwards tent interactions to `TentSleepRuntime`.
3. `TentSleepRuntime` validates sleep conditions.
4. `PlayerSurvivalRuntime.ApplySleepRecovery()` restores rest/stamina and applies hunger cost.
5. `DayNightRuntime.AdvanceHours()` advances the clock and dispatches day/night/morning events.

## Prototype rules

Default sleep rules:

- prompt: `Sleep in tent`
- interaction duration: `1.2s`
- max use distance: `3m`
- minimum rest after sleep: `80`
- minimum stamina after sleep: `70`
- hunger cost: `14`
- minimum hunger to sleep: `18`
- small health bonus: `2`
- if it is night: sleep until around `06:15`
- if it is day: nap for `3h`
- block sleep if a living Varnak is within `34m`

## Failure cases

Sleep is blocked when:

- the actor is missing,
- the player is too far from the tent,
- `PlayerSurvivalRuntime` is missing,
- the player is dead,
- hunger is too low,
- a Varnak is nearby.

Failures are logged and routed through `PlayerActionFeedback.ShowMessage()` when available.

## Save/load

No extra save data is required for the first prototype.

The tent itself is already saved as a building. On restore, `PlaceableStructureRuntime.Configure()` runs again and re-attaches `TentSleepRuntime` for `buildingId == "tent"`.

## Manual test

1. Start New Game.
2. Craft/place a `tent`.
3. Approach the tent.
4. Confirm prompt says `Sleep in tent`.
5. Lower rest/stamina/hunger through debug or gameplay.
6. Interact with the tent.
7. Confirm rest and stamina increase.
8. Confirm hunger decreases.
9. Confirm time advances through `DayNightRuntime`.
10. Try sleeping while a Varnak is nearby and confirm it is blocked.
11. Save/load and verify the restored tent still allows sleeping.

## Future improvements

- authored sleep UI instead of log/flash feedback,
- sleep interruption events,
- comfort values per shelter type,
- weather/night risk modifiers,
- sleep cooldown or fatigue debt,
- special behavior for sleeping near campfire.
