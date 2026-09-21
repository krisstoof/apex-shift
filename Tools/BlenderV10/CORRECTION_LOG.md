# Apex Shift Generator v10 — dziennik korekt po symulacji

## Zakres symulacji

Pełny katalog zawiera **98 assetów**. Ponieważ w tym środowisku nie ma programu Blender ani modułu `bpy`, nie dało się uruchomić prawdziwego renderu Eevee/Cycles. Został więc przygotowany niezależny renderer proxy oparty o `trimesh` i `matplotlib`.

Proxy wykorzystuje te same:

- proporcje,
- punkty połączeń,
- liczbę elementów konstrukcyjnych,
- hierarchię sylwetki,
- reguły rodzin assetów,
- wymagane części semantyczne.

Wyrenderowano 12 najbardziej problematycznych i reprezentatywnych assetów:

- `wood`,
- `berries`,
- `spear`,
- `bow`,
- `axe`,
- `campfire`,
- `tent`,
- `trap`,
- `berry_bush_a`,
- `conifer_tree_b`,
- `small_prey`,
- `old_tree_landmark`.

## Problemy wykryte w pierwszej symulacji

### Łuk

Pierwszy układ punktów tworzył zamkniętą pętlę. Był to ten sam typ błędu, który pojawiał się we wcześniejszych generatorach: konstrukcja była technicznie kompletna, lecz nie przypominała jednego wygiętego łęczyska.

**Korekta:**

- zastąpiono pętlę jedną ciągłą linią w kształcie litery C,
- końce łęczyska połączono pojedynczą prostą cięciwą,
- uchwyt umieszczono na najgrubszej środkowej części,
- ograniczono liczbę segmentów i poprawiono ich ciągłość.

### Namiot

Pierwsza symulacja miała zbyt dużo nakładających się prostokątnych paneli. Wejście było niewidoczne, a bryła wyglądała jak chaotyczna ściana.

**Korekta:**

- zmniejszono liczbę rzędów pokrycia,
- zwiększono kontrolowany overlap,
- usunięto dolne panele przy wejściu,
- pozostawiono czytelną konstrukcję A-frame,
- utrzymano widoczne podpory, ridgepole i krokwie.

### Włócznia

Grot był poprawnie zamocowany, ale zbyt mały względem widoku izometrycznego.

**Korekta:**

- powiększono grot o około 15%,
- zachowano jego płaski, liściowaty profil,
- pozostawiono widoczny oplot pod grotem.

### Pułapka deadfall

Ciężar był ułożony zbyt poziomo i mechanizm nie pokazywał napięcia.

**Korekta:**

- uniesiono jeden koniec ciężaru,
- ciężar opiera się wizualnie na układzie triggera,
- przynęta pozostaje na końcu poziomego patyka,
- punkt podparcia jest czytelniejszy.

### Krzew jagodowy

Pierwsza wersja miała za wąską podstawę i zbyt mało masy bocznej.

**Korekta:**

- zwiększono promień rozłożenia pędów,
- zwiększono liczbę liści na zakończeniach,
- zachowano owoce przyczepione szypułkami,
- utrzymano prześwity pokazujące drewniany szkielet krzewu.

### Drzewo iglaste

Korona była czytelna, ale masy igieł były za drobne.

**Korekta:**

- powiększono masy igieł rozmieszczone wzdłuż konarów,
- pozostawiono szerokie dolne piętra,
- zachowano jeden wyraźny leader,
- nie użyto jednej stożkowej bryły zastępującej koronę.

### Wielkie stare drzewo

Pierwsza wersja była zbyt podobna do zwykłego drzewa z powiększoną skalą.

**Korekta:**

- zwiększono średnicę pnia,
- poszerzono korzenie,
- obniżono pierwsze główne konary,
- rozszerzono koronę,
- zwiększono objętość skupisk liści.

## Wynik korekty proxy

Po korektach wszystkie 12 assetów przechodzi automatyczne kontrole:

- kontakt z ziemią,
- obwiednia skali,
- minimalna liczba części,
- obecność elementów wymaganych dla danej rodziny,
- ciągłość najważniejszych cech konstrukcyjnych.

Wynik **10/10** w raporcie proxy oznacza przejście kontroli strukturalnych. Nie oznacza automatycznie jakości rzeźby, UV, tekstur ani finalnego wyglądu w Eevee/Cycles.

## Co zmieniono w generatorze Blendera

- generator jest samodzielny i czyta 98 profili z JSON,
- wszystkie 55 rodzin assetów mają przypisany generator,
- warianty nie są tworzone wyłącznie przez skalowanie,
- gałęzie, łuki i sznur używają krzywych albo siatek kierunkowych,
- liście są osobnymi siatkami o kształcie soczewkowatym,
- groty i kamienne głowice używają nieregularnej siatki typu flaked stone,
- polana posiadają korę oraz osobne materiały przekrojów,
- owoce posiadają szypułki,
- konstrukcje posiadają czytelne podpory i punkty styku,
- stworzenia szukają zatwierdzonego base mesha w kolekcji `ASSET_BASES`,
- fallback stworzeń jest tylko rozwiązaniem technicznym i jest oznaczany ostrzeżeniem,
- po generacji wykonywana jest automatyczna korekta skali i ustawienie na ziemi,
- generator tworzy audyt, manifest, eksport GLB/FBX/BLEND i cztery widoki kontrolne.
