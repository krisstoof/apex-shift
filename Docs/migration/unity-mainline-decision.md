# Unity mainline decision

## Decision

`krisstoof/apex-shift` is now the active Unity mainline for Apex Shift.

`krisstoof/apex-shift-2d` remains a historical and parity reference. It should not receive new gameplay features unless a future issue explicitly requires backporting or comparison work.

## Why

The Unity version now has enough runtime foundation to continue as the primary project:

- world generation,
- resources,
- inventory,
- crafting,
- save/load,
- ecosystem foundation,
- creature AI foundation,
- day/night runtime,
- survival loop,
- food consumption,
- fatigue/exhaustion,
- death/game-over,
- building/storage,
- tent sleep,
- map/minimap/landmark foundation,
- settings/audio/graphics foundation,
- player animation binding foundation.

## What is still incomplete

The migration phase is not the same as a finished game. Remaining work is now product/gameplay polish, not generic porting:

- first 15 minutes balance,
- combat feel,
- camera feel,
- interaction feedback,
- UI/UX polish,
- animation polish,
- performance/stuttering validation,
- tester build packaging,
- content density and procedural world expansion,
- audio/visual polish.

## Next milestone

Recommended milestone:

`0.1.0 Unity Gameplay Polish`

Focus:

- stable tester build,
- first 15 minutes playable loop,
- readable UI,
- movement/camera feel,
- resource gathering/crafting clarity,
- survival pressure from hunger/fatigue/night,
- basic combat readability,
- map/minimap usefulness,
- save/load stability,
- acceptable FPS and no major stuttering.

## Codex / agent instruction

From this point forward:

- do not create broad "port from Godot" issues,
- do not reimplement systems that already have Unity runtime foundation,
- create concrete issues for gameplay, polish, UX, content, balancing, tests or performance,
- use `apex-shift-2d` only as parity reference when needed,
- target `apex-shift` Unity mainline for new work.

## Migration closure checklist

- [x] Unity mainline selected.
- [x] Godot/2D prototype demoted to reference.
- [x] Migration status matrix updated.
- [x] Tester checklist created.
- [x] Remaining gaps described as concrete follow-up work.
