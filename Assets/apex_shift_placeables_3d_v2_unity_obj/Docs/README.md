# Apex Shift — Low Poly Placeables 3D v2

Realne modele 3D do importu w Unity jako `.obj` + `.mtl`.

## Import

1. Skopiuj folder `Assets/_Project/Art/Placeables/Models` do repo Unity.
2. Unity powinno automatycznie zaimportować `.obj` i powiązać materiały z `apex_shift_placeables_lowpoly.mtl`.
3. Utwórz prefab dla każdego modelu i przypisz go w `PrefabRegistry.BuildingPrefabs`:
   - `storage_box` → `storage_box_low_poly.obj`
   - `campfire` → `campfire_low_poly.obj`
   - `wall` → `wall_low_poly.obj`
   - `trap` → `trap_low_poly.obj`
   - `tent` → `tent_low_poly.obj`
4. Dodaj collider zgodnie z `placeable_asset_manifest.json`.
5. Do prefaba dodaj / zostaw komponent runtime z patcha #42: `PlaceableStructureRuntime`.

## Założenia techniczne

- Y-up, metry, pivot przy środku podstawy.
- Modele są niskopoligonowe, proceduralnie stworzone, bez tekstur bitmapowych.
- Materiały są płaskie i stylizowane przez `.mtl`.
- To są modele roboczo-produkcyjne do gameplayu/prototypu, nie finalny art pass.

## Sugestia dalszego polishu

W Unity można później zamienić materiały na URP/Lit z ręcznie dobranymi kolorami, dodać lekkie normal/texture pass albo zrobić prefaby z oddzielnymi colliderami i VFX dla ogniska.
