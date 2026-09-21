# Apex Shift Blender Generator v10.1 — poprawka namiotu

## Główna zmiana

W wersji v10 problem namiotu nie wynikał wyłącznie z liczby paneli. Panele poszycia były obracane wokół niewłaściwej osi. W efekcie układały się w chaotyczną ścianę zamiast podążać po dwóch połaciach dachu A-frame.

W v10.1:

- podpory frontowa i tylna są prawdziwymi ramami **A**, z szeroko rozstawionymi stopami,
- panele obracają się wokół osi **X**, zgodnie z rzeczywistym spadkiem dachu,
- dach ma 4 nakładające się rzędy po 6 paneli na każdej stronie,
- dodano 6 par krokwi,
- dodano dwie belki okapowe i cztery ukryte płatwie,
- front pozostaje otwartym trójkątnym wejściem,
- tył ma osobne trójkątne zamknięcie,
- posłanie i próg są cofnięte do wnętrza,
- podpory frontowe są lekko wysunięte, aby rama wejścia była czytelna.

## Zawartość

- `apex_shift_blender_generator_v10_1.py` — generator Blendera,
- `apex_shift_profiles_v10.py` — komplet 98 profili,
- `apex_shift_proxy_simulator_v10_1.py` — symulator kontrolny,
- `apex_shift_asset_visual_specs_v5.json` — specyfikacja assetów,
- `apex_shift_biblia_wizualna_v5.md` — biblia wizualna,
- `simulation_output_v10_1/` — nowa symulacja i raport QA,
- `TENT_FIX_ANALYSIS.md` — dokładne wyjaśnienie poprawki.

## Test samego namiotu w Blenderze

```bash
blender --background --python apex_shift_blender_generator_v10_1.py -- \
  --only tent
```

## Test namiotu i elementów obozu

```bash
blender --background --python apex_shift_blender_generator_v10_1.py -- \
  --only tent,campfire,storage_box
```

## Pełna paczka 98 assetów

```bash
blender --background --python apex_shift_blender_generator_v10_1.py
```

## Ważne

Symulacja proxy potwierdza poprawę konstrukcji, przekroju A-frame, otwartego wejścia oraz liczby wymaganych elementów. Finalny shading i dokładne wzajemne przenikanie paneli nadal należy obejrzeć w prawdziwym renderze Eevee lub Cycles.
