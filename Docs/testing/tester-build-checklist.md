# Tester build checklist

## Build info

- Build version:
- Branch / commit:
- Platform:
- Tester:
- Date:
- Session length:

## What this build is for

This build validates the current Unity mainline prototype. Focus on stability, basic gameplay flow, obvious UX blockers, save/load, interaction clarity and first 15 minutes.

## What this build is not for

Do not treat final balance, final visuals, final audio, final combat feel or final UI styling as complete.

## How to report bugs

Include:

- build version / commit,
- reproduction steps,
- expected result,
- actual result,
- screenshot/video if useful,
- console error if present,
- save file if relevant,
- whether it happens after fresh New Game or after Load.

## Checklist

| Area | Steps | Expected result | Status | Notes |
| --- | --- | --- | --- | --- |
| Start game / New Game | Launch the build and start a fresh New Game. | Game loads into the playable world without crash, player spawns correctly and HUD appears. | Not tested | |
| Continue / Load | Relaunch the build and choose Continue or Load from an existing save. | Saved run loads without corruption, player position/state are restored and play can continue. | Not tested | |
| Save/load | Start a run, gather or craft something, save, quit to menu or desktop, then load. | Inventory, survival state, world state and placed objects remain consistent after load. | Not tested | |
| Player movement | Move in all directions, rotate camera if available and interact with nearby objects. | Player movement is responsive, collision is stable and interactions remain reachable. | Not tested | |
| Sprint/stamina | Sprint until stamina changes, stop sprinting and wait. | Sprint consumes stamina and recovery works without locking movement. | Not tested | |
| Swimming/water | Enter and leave water or shoreline areas. | Player transitions through water areas without getting stuck or breaking camera/movement. | Not tested | |
| Gathering resources | Gather wood, stone, food or another nearby resource node. | Resource is collected, node feedback is visible and inventory updates. | Not tested | |
| Inventory | Open inventory, inspect items, move/select usable items if available. | Inventory opens consistently and item counts match gathered/crafted items. | Not tested | |
| Food consumption | Collect edible food, consume it from inventory or action flow. | Hunger/food state improves and the consumed item count decreases. | Not tested | |
| Crafting | Gather recipe ingredients and craft at least one available item. | Craft succeeds, ingredients are consumed and crafted item appears. | Not tested | |
| Action bar / held item | Assign or select a tool/item, switch slots and use the held item. | Held item/action bar state changes correctly and item use triggers expected behavior. | Not tested | |
| Combat | Attack a creature with an available weapon or tool. | Creature can take damage, player feedback is readable and combat does not soft-lock. | Not tested | |
| Animals AI | Observe nearby animals for movement, fleeing, chasing or needs behavior. | Animals move and react without freezing, jittering endlessly or spamming errors. | Not tested | |
| Varnak days 1-5 | Progress or simulate the first five days and observe Varnak behavior, especially at night. | Varnak pressure appears without immediate unavoidable failure or broken AI state. | Not tested | |
| Death/game over | Let survival stats or combat reduce player health to failure. | Death/game-over flow triggers and the game can return to menu or restart cleanly. | Not tested | |
| Fatigue/exhaustion | Stay active long enough to reduce rest/stamina or trigger fatigue. | Fatigue/exhaustion changes player state without corrupting movement or HUD. | Not tested | |
| Tent sleep | Place a tent, lower rest/stamina, interact with the tent. | Player sleeps, time advances, rest/stamina increase, hunger decreases. Sleep is blocked if threatened. | Not tested | |
| Torch/campfire | Craft/equip a torch or place/light a campfire near the player. | Fire source appears, stays registered and affects nearby survival/threat logic as expected. | Not tested | |
| Building placement | Craft/select a placeable structure and attempt valid/invalid placement. | Valid placement succeeds, invalid placement is blocked clearly and no ghost objects remain. | Not tested | |
| Storage box | Place a storage box, deposit and withdraw items, then save/load. | Storage accepts items, returns them correctly and persists after load. | Not tested | |
| Minimap | Play while moving through terrain and near resources/landmarks. | Minimap updates position/orientation and markers remain readable enough for testing. | Not tested | |
| Full map | Open the full map after exploring. | Map opens/closes cleanly and shows useful world/marker information. | Not tested | |
| Landmarks | Find or navigate near at least one landmark marker/location. | Landmark appears in world/map systems and does not break navigation or markers. | Not tested | |
| Options graphics/audio | Open options, change graphics/audio settings and return to play. | Settings apply without crash and persist where expected. | Not tested | |
| FPS/stuttering | Play the first 15 minutes while watching for hitching, especially after generation/save/load. | Performance is acceptable enough for manual testing and no repeated long stalls occur. | Not tested | |
| Console errors/crashes | Keep console/log visible while running the checklist. | No crash occurs and no console error repeats every frame. | Not tested | |

## Known limitations

- Combat feel is prototype-level.
- Animation coverage depends on imported clips.
- UI is functional but not final.
- Audio mix is not final.
- Survival values are not final balance.
- Map/minimap visuals are not final.
- Procedural world/content density still needs tuning.

## Do not report as bug yet

Do not report these unless they cause a crash/blocker:

- placeholder visuals,
- rough animation transitions,
- unbalanced hunger/fatigue values,
- non-final audio volume balance,
- non-final map/minimap styling,
- lack of final tutorialization,
- imperfect first-pass terrain/resource distribution.

## Tester build release gate

Build can be sent to tester only if:

- New Game starts without crash.
- Player can move and interact.
- Inventory opens.
- Gathering works.
- At least one craft flow works.
- Save/load does not corrupt the run.
- No console error repeats every frame.
- FPS is acceptable enough for manual testing.
- Game can be closed and reopened.
