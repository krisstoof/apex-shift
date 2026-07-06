# Bushcraft Models Manifest

## Scope
- Pipeline moved from low-poly placeholders to a stylized hand-painted generator workflow.
- Generator entry point: `Tools/Blender/bushcraft_asset_generator.py`
- Shared builders: `Tools/Blender/bushcraft_asset_library.py`
- Validation and preview checks: `Tools/Blender/bushcraft_render_validation.py`

## Output Naming
- `{asset_id}_stylized.blend`
- `{asset_id}_stylized.fbx`
- `{asset_id}_stylized.obj`
- `{asset_id}_stylized_preview.png`

## Categories
- Items: `wood`, `stone`, `fiber`, `grass`, `meat`, `hide`, `bone`, `berries`, `torch`, `spear`, `bow`
- Placeables: `campfire`, `storage_box`, `tent`, `wall`, `trap`
- Resources: `conifer_tree`, `leafy_tree`, `dry_tree`, `rock`, `green_bush`, `dry_bush`, `grass_or_flower`, `berry_bush`

## Quality Direction
- stylized realistic
- hand-painted
- semi-realistic bushcraft
- readable in isometric camera
- natural materials with visible construction logic

## Export Targets
- `Assets/_Project/Art/Bushcraft/Items/Models/`
- `Assets/_Project/Art/Bushcraft/Resources/Models/`
- `Assets/_Project/Art/Bushcraft/Placeables/Models/`
- `Assets/_Project/Art/Bushcraft/Source/Blend/`

## Preview / Review Targets
- `Docs/art/bushcraft-items-sheet.png`
- `Docs/art/bushcraft-placeables-sheet.png`
- `Docs/art/bushcraft-resources-sheet.png`
- `Docs/art/bushcraft-validation-report.md`

## Notes
- First approval pass should focus on `wood`, `spear`, and `campfire`.
- Existing low-poly files remain in the repo as earlier iterations, but the new pipeline targets stylized outputs.
