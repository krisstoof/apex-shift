# Apex Shift Bushcraft Brief

## Cel
Zdefiniować spójny kierunek wizualny dla modeli 3D, grafik referencyjnych i dalszej integracji w Unity.

## Słowa kluczowe
- bushcraft survival
- handmade primitive tools
- natural materials only
- low-poly, isometric readability
- rough asymmetry
- warm earthy palette
- no modern gear
- no plastic, nylon, polished factory hardware

## Wrażenie docelowe
Gracz ma od razu rozumieć, że wszystko w świecie powstaje z natury, improwizacji i prostego craftingu.

## Zasady stylu
- Modele mają wyglądać jak wykonane ręcznie z drewna, kamienia, kości, włókien, skóry i trawy.
- Bryły powinny być czytelne z izometrycznej kamery.
- Drobne detale mają wspierać sylwetkę, a nie ją komplikować.
- Każdy obiekt powinien mieć jednoznaczną funkcję gameplayową.

## Modelowanie

### Items i pickups
- `wood`: nierówne patyki lub krótkie polana.
- `stone`: jeden lub kilka nieregularnych kamieni.
- `fiber`: zwinięte włókna, sucha trawa albo prymitywny sznur.
- `meat`: mały surowy kawałek mięsa bez przesadnego gore.
- `hide`: zwinięta lub rozłożona skóra.
- `bone`: uproszczona kość, jasna i czytelna.
- `berries`: mała garść czerwonych lub ciemnych jagód.
- `grass`: mała niska kępka trawy.

### Tools i weapons
- `torch`: kij owinięty włóknem z ciemną końcówką.
- `spear`: długi kij z kamiennym lub kostnym grotem i wiązaniem z fiber.
- `bow`: prosty łuk z giętej gałęzi i cięciwą z włókna.

### Placeables
- `campfire`: kamienny krąg, patyki i żar/popiół.
- `trap`: survivalowa pułapka z patyków i włókien.
- `wall`: nieregularna palisada z bali i gałęzi.
- `storage_box`: skrzynia z nierównych desek i wiązań.
- `tent`: prowizoryczne schronienie z gałęzi, trawy, skóry lub liści.

### Resources
- `conifer_tree`: stożkowa, ciemniejsza sylwetka.
- `leafy_tree`: szersza i bardziej zaokrąglona korona.
- `dry_tree`: martwe, szare, mało liści lub bez liści.
- `rock`: większa skała zasobowa, odróżnialna od pickup `stone`.
- `green_bush`: gęsty zielony krzew.
- `dry_bush`: rzadszy, żółtawo-brązowy krzak.
- `grass_or_flower`: niska roślinność z ewentualnym drobnym kwiatem.
- `berry_bush`: niski krzak z wyraźnymi jagodami.

## Kolor i materiał
- Drewno: ciepłe brązy, miejscami ciemniejsza kora.
- Kamień: szarości, lekko chłodne i zróżnicowane.
- Fiber i trawa: suche zielenie i słomkowe żółcie.
- Skóra: brązy, ochra, przygaszone beże.
- Mięso: ciemny czerwony z umiarkowanym kontrastem.
- Jagody: czerwone lub ciemnoczerwone akcenty.
- Ogień i żar: małe, ciepłe akcenty, bez przesady.

## Kompozycje grafik
- Plansza zbiorcza: cały pakiet assetów w układzie katalogowym.
- Raw Materials: wood, stone, fiber, grass.
- Animal Drops: meat, hide, bone.
- Foraged Food: berries, berry_bush, green_bush, dry_bush.
- Primitive Tools: torch, spear, bow.
- First Camp: campfire, storage_box, tent.
- Defensive Bushcraft: wall, trap.
- World Resources: conifer_tree, leafy_tree, dry_tree, rock, green_bush, dry_bush, grass_or_flower, berry_bush.
- Crafting Lineage: surowce połączone ze swoją recepturą.
- Scale & Pivot Sheet: porównanie ze skalą postaci i markerami pivotów.
- Unity Integration: mapowanie do `PrefabRegistry`, `ResourcePrefabEntry`, `BuildingPrefabEntry`.

## Techniczne przypomnienia
- Preferowany format: `.obj + .mtl`.
- Skala Unity, Y-up, pivot logiczny.
- Pickupy: pivot w środku.
- Narzędzia: pivot w chwycie.
- Budowle i resource visuals: pivot przy podstawie.
- Bez Addressables.
- Integracja przez `PrefabRegistry`.

## Zakazy
- brak nowoczesnego sprzętu
- brak plastiku
- brak metalowych, fabrycznych detali
- brak fantasy run i magicznych efektów
- brak przesadnego gore
- brak nadmiaru detali niewidocznych z izometrii

## Docelowe wrażenie
To świat, w którym przetrwanie zależy od tego, co gracz zbierze, zwiąże, zbuduje i obroni.

## Production Pipeline

### 1. Reference
- Użyj plansz koncepcyjnych jako źródła kierunku.
- Każdy model ma odpowiadać jednej z grup: item, tool, placeable, resource.
- Nie dodawaj niczego spoza aktualnego zestawu ID w repo.

### 2. Modeling
- Trzymaj bryły proste i czytelne.
- Buduj formę od sylwetki, nie od detalu.
- Preferuj asymetrię i ręczny charakter.
- Nie używaj zbyt cienkich elementów, jeśli znikają z izometrii.

### 3. Scale and Pivot
- Pickupy: pivot w środku obiektu.
- Narzędzia: pivot w miejscu chwytu.
- Budowle: pivot przy podstawie, centralnie.
- Resources: pivot przy podstawie.
- Utrzymuj skalę zgodną z Unity w metrach.

### 4. Materials
- Jeden prosty materiał bazowy jest lepszy niż rozbudowany setup.
- Paleta ma pozostać ziemista i naturalna.
- Ogień, żar i jagody mają być akcentem, nie dominującym efektem.

### 5. Export
- Preferuj `.obj + .mtl`, jeśli model jest prosty i statyczny.
- Nie eksportuj zbędnych kamer, świateł ani helperów.
- Nazewnictwo trzymaj zgodne z game ID i `*_low_poly`.

### 6. Unity Integration
- Item visuals podpinaj przez istniejący pickup / preview flow.
- Resources mapuj przez `ResourcePrefabEntry`.
- Buildings mapuj przez `BuildingPrefabEntry`.
- Centrum integracji ma pozostać `PrefabRegistry`.
- Nie dodawaj Addressables.

### 7. QA
- Sprawdź czy model ma poprawny pivot.
- Sprawdź czy sylwetka czyta się z izometrii.
- Sprawdź czy materiał nie rozbija stylu.
- Sprawdź czy collidery nie są większe niż potrzeba.

### 8. Acceptance
- Model wygląda handmade, nie fabrycznie.
- Model jest low-poly i czytelny.
- Model pasuje do bushcraft survival.
- Model może wejść do Unity bez dodatkowego ratowania wyglądu.
