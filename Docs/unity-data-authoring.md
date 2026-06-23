# Unity Data Authoring

Item and recipe data are authored through ScriptableObject assets in `Assets/_Project/Data`.

## Asset paths

- Item assets: `Assets/_Project/Data/Items/`
- Recipe assets: `Assets/_Project/Data/Recipes/`

## Menu item

Use `Tools > Apex Shift > Data > Create Default Item And Recipe Assets` to create or update the default assets.

## Core mapping

- `ItemDefinitionAsset` maps to `ApexShift.Core.Items.ItemDefinition`
- `RecipeDefinitionAsset` maps to `ApexShift.Core.Crafting.RecipeDefinition`
- Mapper classes convert asset collections into `ItemDatabase` and `RecipeDatabase`
- `ApexShift.Core` remains Unity-free and does not reference `UnityEngine`

## Manual test steps

1. Open Unity.
2. Click `Tools > Apex Shift > Data > Create Default Item And Recipe Assets`.
3. Inspect `Assets/_Project/Data/Items`.
4. Inspect `Assets/_Project/Data/Recipes`.
5. Confirm item and recipe values are editable in Inspector.
6. Run unit tests.
