# Bushcraft Validation Report

Automated validation is now driven by `Tools/Blender/bushcraft_render_validation.py`.

## Current State
- Status: `pipeline_ready`
- Validation target: stylized, hand-painted bushcraft assets
- Scene quality target: mid-poly, readable from isometric camera, natural handcrafted forms

## What The Validator Checks
- object is not a trivial single-primitive placeholder
- UVs exist
- at least one material is assigned
- origin is reasonable for gameplay use
- scale is logical for Unity import
- preview image exists
- naming follows `{asset_id}_stylized`

## Manual Polish Expected
- `wood`
- `spear`
- `campfire`
- `tent`
- `wall`
- `trap`

These are the highest-value assets for style approval against the concept sheets.
