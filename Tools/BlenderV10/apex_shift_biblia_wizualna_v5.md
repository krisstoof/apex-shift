# Apex Shift — kompletna biblia wizualna wszystkich assetów

## Edycja v5: internetowe referencje + analiza natury + wymagania generatora

**Zakres:** 98 assetów obejmujących przedmioty, narzędzia, konstrukcje, zasoby świata, roślinność, stworzenia i landmarki.

Dokument nie traktuje zdjęć jako wzoru do kopiowania 1:1. Każda referencja służy do wyciągnięcia realnej logiki: sylwetki, proporcji, punktów styku, hierarchii gałęzi, anatomii, materiału lub mechaniki konstrukcji. Docelowym językiem pozostaje **hand-painted realism w izometrycznej grze survivalowej**, a nie fotorealizm i nie low-poly placeholder.

> Obrazy internetowe są osadzone z Wikimedia Commons. Dokładny autor i licencja są podane na stronie każdego pliku oraz w `reference_sources.csv`. Do paczki dołączono `download_references.py`, który może pobrać wszystkie wybrane referencje lokalnie.

## 1. Lokalne grafiki koncepcyjne projektu

### Prymitywne narzędzia — kierunek formy i materiałów

![Prymitywne narzędzia — kierunek formy i materiałów](style_refs/primitive_tools_concept.png)

### Pożywienie leśne i krzewy — kierunek biologiczny

![Pożywienie leśne i krzewy — kierunek biologiczny](style_refs/forest_food_and_shrubs_concept.png)

### Pierwszy obóz — kierunek konstrukcji bushcraftowych

![Pierwszy obóz — kierunek konstrukcji bushcraftowych](style_refs/first_camp_concept.png)

### Zasoby świata — kierunek środowiskowy

![Zasoby świata — kierunek środowiskowy](style_refs/world_resources_concept.png)

## 2. Nadrzędne zasady art direction

1. **Najpierw realna struktura, potem stylizacja.** Generator nie może osiągać „realizmu” wyłącznie przez dokładanie kolejnych kul, walców i płaszczyzn. Najpierw musi powstać poprawny szkielet, anatomia lub konstrukcja.
2. **Każda część musi być osadzona.** Jagody mają szypułki, liście mają ogonki, grot ma tuleję i oplot, krokwie opierają się na ridgepole, kończyny łączą się przez stawy.
3. **Sylwetka musi działać z kamery gry.** Główne cechy są delikatnie wzmacniane, ale nie zmieniają się w karykaturę: grot może być o 10–15% większy, nie trzykrotnie grubszy.
4. **Natura jest asymetryczna, lecz nie losowa.** Odchylenia wynikają ze wzrostu, ciężaru, światła, pogody i użytkowania, a nie z bezkierunkowego randomu.
5. **Materiały mają osobną tożsamość.** Drewno, kora, kamień, kość, skóra, futro, mięso, włókno i liście muszą różnić się nie tylko kolorem, ale roughness, normalem, krawędzią i sposobem zużycia.
6. **Wariant nie jest skalą.** Warianty A–D powinny różnić się architekturą sylwetki, rozmieszczeniem mas i stanem biologicznym, nie tylko skalą globalną.
7. **Modele organiczne wymagają bazowej siatki.** Stworzenia i zaawansowane drzewa powinny używać ręcznie przygotowanego base mesha/krzywych/Geometry Nodes; Python odpowiada za parametryzację i eksport, nie za składanie anatomii z prymitywów.

## 3. Dziesięciokrotna kontrola każdego assetu

1. Czy bez materiału i w czarnej sylwetce natychmiast wiadomo, czym jest asset?
2. Czy proporcje odpowiadają zdjęciu referencyjnemu i realnej skali?
3. Czy wszystkie elementy dotykają się lub są połączone w wiarygodny sposób?
4. Czy konstrukcja/anatomia ma hierarchię od dużych form do drobnych?
5. Czy wariant ma logiczne asymetrie, a nie przypadkowy chaos?
6. Czy materiały są odróżnialne bez przesadnie nasyconych kolorów?
7. Czy asset stoi na ziemi, ma poprawny pivot i nie lewituje?
8. Czy jest czytelny z docelowej kamery izometrycznej i przy docelowym rozmiarze na ekranie?
9. Czy liczba detali jest podporządkowana formie, a nie użyta do maskowania złej sylwetki?
10. Czy model pasuje do lokalnych grafik koncepcyjnych Apex Shift i nie wygląda jak placeholder?

## 4. Opisy wszystkich assetów

### 4.1. Drewno opałowe (`wood`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/A_bundle_of_fire_wood.JPG?width=1100" alt="Referencja: Wiązka drewna opałowego" width="680">

**Internetowa referencja:** [Wiązka drewna opałowego](https://commons.wikimedia.org/wiki/File:A_bundle_of_fire_wood.JPG)  
**Kategoria:** Przedmiot  
**Rola w grze:** Podstawowy surowiec do ognia, konstrukcji i craftingu.  
**Docelowa skala:** 0,55–0,85 m długości całej wiązki  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Czytelne są różne średnice polan, obecność kory, nieregularne końce oraz zwarty układ wynikający z ciężaru i związania.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Zwarta wiązka 5–8 polan o różnych średnicach; dwa miejsca związania i widoczne przekroje.
 Bryła powinna wynikać z ciężaru i kontaktu polan. Każde polano ma osobny promień, lekkie wygięcie, korę i przekrój.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Brązy kory 0.16–0.35 wartości, świeże drewno wyraźnie jaśniejsze; sznur matowy i włóknisty.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać identycznych cylindrów, idealnej symetrii ani jednej gładkiej tekstury na wszystkich kawałkach.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.2. Kamień do podnoszenia (`stone`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/River_stones.jpg?width=1100" alt="Referencja: Kamienie rzeczne" width="680">

**Internetowa referencja:** [Kamienie rzeczne](https://commons.wikimedia.org/wiki/File:River_stones.jpg)  
**Kategoria:** Przedmiot  
**Rola w grze:** Podstawowy surowiec kamienny.  
**Docelowa skala:** 0,15–0,30 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Kamienie są obłe, ale nie idealnie kuliste; różnią się wielkością, spłaszczeniem, kolorem i stopniem wygładzenia.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

3–5 kamieni polnych lub rzecznych; jeden dominujący, reszta tworzy wspierający klaster.
 Kamienie muszą mieć spłaszczone i wypukłe płaszczyzny; klaster opiera się na ziemi w kilku punktach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Chłodne i ciepłe szarości z delikatnym zabrudzeniem ziemią; brak czystej bieli.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie generować szarych kul ani jednakowych kamyków rozłożonych równym okręgiem.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.3. Włókno roślinne (`fiber`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Flax_fibers.JPG?width=1100" alt="Referencja: Włókno lniane" width="680">

**Internetowa referencja:** [Włókno lniane](https://commons.wikimedia.org/wiki/File:Flax_fibers.JPG)  
**Kategoria:** Przedmiot  
**Rola w grze:** Surowiec na linę, wiązania i proste konstrukcje.  
**Docelowa skala:** 0,35–0,65 m długości pasm  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Włókna tworzą długie, równoległe pasma; jako pickup powinny wyglądać jak pęk cienkich włókien, a nie jak kilka grubych rurek.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Pęk długich, cienkich włókien z jednym przewiązaniem; końcówki muszą się strzępić.
 Wiele cienkich pasm powinno układać się kierunkowo, ale kilka końcówek ma odstawać i skręcać się.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Słoma, len, suche zielenie; materiał bardzo matowy i lekko półprzezroczysty tylko na najcieńszych włóknach.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie robić z włókna kilku grubych zielonych prętów.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.4. Zebrana trawa (`grass`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Clump_of_grass_%2814267486846%29.png?width=1100" alt="Referencja: Kępa trawy" width="680">

**Internetowa referencja:** [Kępa trawy](https://commons.wikimedia.org/wiki/File:Clump_of_grass_%2814267486846%29.png)  
**Kategoria:** Przedmiot  
**Rola w grze:** Materiał na posłanie, rozpałkę i krycie schronień.  
**Docelowa skala:** 0,25–0,45 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Kępa wyrasta z ciasnego środka; źdźbła mają wiele kierunków, długości i łagodnych zgięć, ale wspólną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Ścięta lub wyrwana kępa ułożona płasko; wyraźne cienkie źdźbła, nie zielone patyczki.
 Źdźbła mają wspólny kierunek po ścięciu lub wspólną podstawę po wyrwaniu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zielony z 20–35% suchych końcówek; cienkie karty lub krzywe, nie bryły.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie stosować prostokątnych zielonych listew bez taperu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.5. Surowe mięso (`meat`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Raw_Meat_%286737753521%29.jpg?width=1100" alt="Referencja: Surowe mięso" width="680">

**Internetowa referencja:** [Surowe mięso](https://commons.wikimedia.org/wiki/File:Raw_Meat_%286737753521%29.jpg)  
**Kategoria:** Przedmiot  
**Rola w grze:** Pożywienie i łup ze zwierząt.  
**Docelowa skala:** 0,18–0,35 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Bryła mięsa jest nieregularna, ma warstwy mięśni, tłuszczu i wilgotne załamania; nie powinna przypominać czerwonej kapsuły.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Nieregularny fragment mięśnia z pasem tłuszczu; jeden mocny kontur zamiast czerwonej kuli.
 Mięso ma jeden główny fragment, nie kilka kul. Powierzchnia pokazuje kierunek włókien i nierówny cięty brzeg.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Ciemna czerwień, róż, tłuszcz kremowy; ograniczony połysk, aby nie wyglądało jak plastik.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie stosować symetrycznej kapsuły ani jednego płaskiego czerwonego materiału.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.6. Skóra zwierzęca (`hide`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Furbearing_animal_Pelts.jpg?width=1100" alt="Referencja: Skóry zwierzęce" width="680">

**Internetowa referencja:** [Skóry zwierzęce](https://commons.wikimedia.org/wiki/File:Furbearing_animal_Pelts.jpg)  
**Kategoria:** Przedmiot  
**Rola w grze:** Surowiec do craftingu, posłania i pokryć.  
**Docelowa skala:** 0,45–0,90 m po rozłożeniu  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Skóra ma miękkie zagięcia, nieregularny obrys i różnicę między stroną futrzaną a mizdrą; zwinięcie nie jest geometrycznie idealne.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Złożony płat o nieregularnym obrysie; jedna strona futrzana, druga bardziej matowa i skórzasta.
 Płat ma nieregularną krawędź, ciężkie zagięcia i cienką, ale widoczną grubość.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Futro w kilku długościach i tonach; mizdra jaśniejsza, sucha, miejscami zabrudzona.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać idealnego prostokąta lub płaskiej kartki.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.7. Kość długa (`bone`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Animal-leg-bone.jpg?width=1100" alt="Referencja: Kość długa zwierzęcia" width="680">

**Internetowa referencja:** [Kość długa zwierzęcia](https://commons.wikimedia.org/wiki/File:Animal-leg-bone.jpg)  
**Kategoria:** Przedmiot  
**Rola w grze:** Surowiec kostny i łup.  
**Docelowa skala:** 0,35–0,70 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Kość długa ma zwężony trzon i poszerzone, asymetryczne nasady. Nie jest walcem zakończonym czterema kulami.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Pojedyncza długa kość: zwężony trzon, dwie różne nasady, lekka krzywizna.
 Trzon zwęża się i lekko skręca; końce są różne i wynikają z anatomii stawu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kość kremowa, miejscami szara lub brązowa; porowate końce, wygładzony trzon.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć „hantla”: walec plus cztery identyczne kule.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.8. Zebrane jagody (`berries`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Blueberries_on_the_branches_of_a_bush.jpg?width=1100" alt="Referencja: Jagody na gałęziach krzewu" width="680">

**Internetowa referencja:** [Jagody na gałęziach krzewu](https://commons.wikimedia.org/wiki/File:Blueberries_on_the_branches_of_a_bush.jpg)  
**Kategoria:** Przedmiot  
**Rola w grze:** Pożywienie zbierane z krzewów.  
**Docelowa skala:** 0,12–0,24 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Owoce są przyczepione cienkimi szypułkami do gałązki, występują w skupiskach o różnym stopniu dojrzałości i są częściowo zasłonięte liśćmi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Dwie małe kiście nadal przyczepione do gałązki z 2–4 liśćmi; owoce o różnej dojrzałości.
 Gałązka i szypułki są szkieletem; owoce wiszą pod własnym ciężarem, a liście zasłaniają część skupiska.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Owoce w 2–3 tonach dojrzałości, matowy bloom na niebieskich jagodach lub delikatny połysk czerwonych.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Każda lewitująca jagoda dyskwalifikuje asset.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.9. Wiązka rozpałki (`kindling_bundle`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/A_bundle_of_fire_wood.JPG?width=1100" alt="Referencja: Wiązka drewna opałowego" width="680">

**Internetowa referencja:** [Wiązka drewna opałowego](https://commons.wikimedia.org/wiki/File:A_bundle_of_fire_wood.JPG)  
**Kategoria:** Przedmiot  
**Rola w grze:** Szybka rozpałka i drobny surowiec drzewny.  
**Docelowa skala:** 0,35–0,60 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Czytelne są różne średnice polan, obecność kory, nieregularne końce oraz zwarty układ wynikający z ciężaru i związania.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

10–16 cienkich gałązek i szczap, luźniej związanych niż drewno opałowe.
 Bryła powinna wynikać z ciężaru i kontaktu polan. Każde polano ma osobny promień, lekkie wygięcie, korę i przekrój.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Brązy kory 0.16–0.35 wartości, świeże drewno wyraźnie jaśniejsze; sznur matowy i włóknisty.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać identycznych cylindrów, idealnej symetrii ani jednej gładkiej tekstury na wszystkich kawałkach.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.10. Odłupki krzemienne (`flint_shard`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Flint_flakes_%28FindID_88882%29.jpg?width=1100" alt="Referencja: Krzemień i odłupki" width="680">

**Internetowa referencja:** [Krzemień i odłupki](https://commons.wikimedia.org/wiki/File:Flint_flakes_%28FindID_88882%29.jpg)  
**Kategoria:** Przedmiot  
**Rola w grze:** Surowiec na groty i ostrza.  
**Docelowa skala:** 0,06–0,16 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Odłupki mają ostre krawędzie, powierzchnie negatywów po odbiciu i cienki profil, którego nie wolno zastępować szarą kulą.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Klaster cienkich, ostrych odłupków z widocznymi fasetami i różną grubością.
 Odłupek ma cienki profil, ostrą krawędź i fasety powstałe po odbijaniu kamienia.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Grafit, ciemna szarość, kredowe miejsca kory; ostrze może łapać jaśniejszy highlight.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie zastępować krzemienia miękkim, obłym otoczakiem.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.11. Wiązka trzciny (`reed_bundle`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Bundles_of_reeds_on_the_dike_-_geograph.org.uk_-_1803149.jpg?width=1100" alt="Referencja: Wiązki trzcin" width="680">

**Internetowa referencja:** [Wiązki trzcin](https://commons.wikimedia.org/wiki/File:Bundles_of_reeds_on_the_dike_-_geograph.org.uk_-_1803149.jpg)  
**Kategoria:** Przedmiot  
**Rola w grze:** Materiał na maty, poszycie i wiązania.  
**Docelowa skala:** 0,70–1,10 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Trzciny są długie, smukłe, prawie równoległe i związane w jednym lub kilku miejscach; końce powinny tworzyć nieregularny wachlarz.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Smukła pionowa wiązka wielu łodyg związanych w dwóch miejscach; nierówny górny brzeg.
 Łodygi są smukłe, puste i prawie równoległe; wiązanie ściska środek, a końce rozszerzają się.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Suche żółtozielone i beżowe tony, miejscami ciemniejsze węzły.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać krótkich, identycznych walców.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.12. Płonąca pochodnia (`torch`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Torch.jpg?width=1100" alt="Referencja: Pochodnia" width="680">

**Internetowa referencja:** [Pochodnia](https://commons.wikimedia.org/wiki/File:Torch.jpg)  
**Kategoria:** Narzędzie  
**Rola w grze:** Źródło światła i odstraszanie części stworzeń.  
**Docelowa skala:** 0,85–1,15 m  
**Stany gameplayowe:** nowa / płonąca / dogasająca / wypalona

#### Analiza obrazu referencyjnego

Najważniejszy jest czytelny podział na drewniany chwyt, owiniętą głowicę, zwęglenie i źródło płomienia.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Długi kij, mocna owinięta głowica, czarne zwęglenie, żar i niejednolity płomień.
 Cięższa owinięta głowica siedzi na lżejszym kiju; oplot nakłada się warstwami i ma zwęglony szczyt.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno, ciemny brąz/charcoal, ciepły żar; płomień ma gradient i nieregularny kontur.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać jednej pomarańczowej kapsuły jako płomienia.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.13. Nieodpalona pochodnia (`torch_unlit`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Torch.jpg?width=1100" alt="Referencja: Pochodnia" width="680">

**Internetowa referencja:** [Pochodnia](https://commons.wikimedia.org/wiki/File:Torch.jpg)  
**Kategoria:** Narzędzie  
**Rola w grze:** Wersja przed zapaleniem.  
**Docelowa skala:** 0,85–1,15 m  
**Stany gameplayowe:** sucha / przygotowana / częściowo zużyta

#### Analiza obrazu referencyjnego

Najważniejszy jest czytelny podział na drewniany chwyt, owiniętą głowicę, zwęglenie i źródło płomienia.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Ten sam model bazowy bez płomienia; owinięcie czytelniejsze, głowica ciemna od smoły lub tłuszczu.
 Cięższa owinięta głowica siedzi na lżejszym kiju; oplot nakłada się warstwami i ma zwęglony szczyt.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno, ciemny brąz/charcoal, ciepły żar; płomień ma gradient i nieregularny kontur.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać jednej pomarańczowej kapsuły jako płomienia.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.14. Kamienna włócznia (`spear`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Stone_Spear_Point_%2830028326496%29.jpg?width=1100" alt="Referencja: Kamienny grot włóczni" width="680">

**Internetowa referencja:** [Kamienny grot włóczni](https://commons.wikimedia.org/wiki/File:Stone_Spear_Point_%2830028326496%29.jpg)  
**Kategoria:** Broń  
**Rola w grze:** Podstawowa broń dystansowa do pchnięć.  
**Docelowa skala:** 1,55–1,85 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Grot ma cienki profil, ostrą krawędź, odłupane fasety i wyraźną podstawę do osadzenia. Powinien wyglądać jak narzędzie, nie pionowy otoczak.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Smukłe drzewce, płaski liściowaty grot, rozszczepiona tuleja i dwie warstwy wiązania.
 Oś grotu, tulei i drzewca musi być wspólna. Grot jest cienki i płaski, a wiązanie faktycznie obejmuje podstawę.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno ciepłe, kamień chłodny, włókna beżowe; odłupania odczytane przez normal/baked detail.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie robić dużego pionowego kamienia na końcu patyka.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.15. Ciężka włócznia (`spear_heavy`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Stone_Spear_Point_%2830028326496%29.jpg?width=1100" alt="Referencja: Kamienny grot włóczni" width="680">

**Internetowa referencja:** [Kamienny grot włóczni](https://commons.wikimedia.org/wiki/File:Stone_Spear_Point_%2830028326496%29.jpg)  
**Kategoria:** Broń  
**Rola w grze:** Mocniejszy wariant przeciw dużym celom.  
**Docelowa skala:** 1,75–2,05 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Grot ma cienki profil, ostrą krawędź, odłupane fasety i wyraźną podstawę do osadzenia. Powinien wyglądać jak narzędzie, nie pionowy otoczak.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Grubsze drzewce, szerszy grot i dłuższa strefa oplotu; środek ciężkości przesunięty do przodu.
 Oś grotu, tulei i drzewca musi być wspólna. Grot jest cienki i płaski, a wiązanie faktycznie obejmuje podstawę.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno ciepłe, kamień chłodny, włókna beżowe; odłupania odczytane przez normal/baked detail.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie robić dużego pionowego kamienia na końcu patyka.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.16. Łuk prosty (`bow`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Pacific_Yew_Selfbow.jpg?width=1100" alt="Referencja: Drewniany self bow" width="680">

**Internetowa referencja:** [Drewniany self bow](https://commons.wikimedia.org/wiki/File:Pacific_Yew_Selfbow.jpg)  
**Kategoria:** Broń  
**Rola w grze:** Broń dystansowa.  
**Docelowa skala:** 1,15–1,45 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Łuk jest jednym ciągłym prętem: środek jest grubszy, ramiona zwężają się do końcówek, a cięciwa łączy oba tipy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Jedna ciągła gięta listwa; gruby grip, zwężające się ramiona, cienka cięciwa między tipami.
 Łuk jest jedną ciągłą krzywą z wyraźnie grubszym gripem i zwężającymi się ramionami.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno z kierunkowym usłojeniem, grip skórzany lub włóknisty, cięciwa ciemna i cienka.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie generować kilku osobnych gałęzi ani cięciwy, która nie dotyka końcówek.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.17. Krótki łuk survivalowy (`bow_short`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Pacific_Yew_Selfbow.jpg?width=1100" alt="Referencja: Drewniany self bow" width="680">

**Internetowa referencja:** [Drewniany self bow](https://commons.wikimedia.org/wiki/File:Pacific_Yew_Selfbow.jpg)  
**Kategoria:** Broń  
**Rola w grze:** Mobilny, słabszy wariant łuku.  
**Docelowa skala:** 0,90–1,15 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Łuk jest jednym ciągłym prętem: środek jest grubszy, ramiona zwężają się do końcówek, a cięciwa łączy oba tipy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Krótsza, mocniej zakrzywiona sylwetka, wyraźny grip i proporcjonalnie mocniejsze końcówki.
 Łuk jest jedną ciągłą krzywą z wyraźnie grubszym gripem i zwężającymi się ramionami.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno z kierunkowym usłojeniem, grip skórzany lub włóknisty, cięciwa ciemna i cienka.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie generować kilku osobnych gałęzi ani cięciwy, która nie dotyka końcówek.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.18. Pojedyncza strzała (`arrow`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/A_wooden_arrow..jpg?width=1100" alt="Referencja: Drewniana strzała" width="680">

**Internetowa referencja:** [Drewniana strzała](https://commons.wikimedia.org/wiki/File:A_wooden_arrow..jpg)  
**Kategoria:** Amunicja  
**Rola w grze:** Amunicja do łuku.  
**Docelowa skala:** 0,65–0,90 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Strzała ma prosty promień, mały grot, lotki i nock. Każdy z tych elementów powinien być widoczny nawet po lekkim pogrubieniu pod izometrię.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Prosty promień, mały kamienny grot, trzy lotki i nock; prezentować ukośnie, aby była czytelna.
 Promień jest prosty; grot, shaft, lotki i nock tworzą jeden ciągły obiekt.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno jasne, grot kamienny, lotki stonowane; wiązanie nieco jaśniejsze od shaftu.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie prezentować jako nieczytelnej pojedynczej kreski bez grotu i lotek.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.19. Wiązka strzał (`arrow_bundle`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/A_wooden_arrow..jpg?width=1100" alt="Referencja: Drewniana strzała" width="680">

**Internetowa referencja:** [Drewniana strzała](https://commons.wikimedia.org/wiki/File:A_wooden_arrow..jpg)  
**Kategoria:** Amunicja  
**Rola w grze:** Pickup kilku strzał.  
**Docelowa skala:** 0,70–0,95 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Strzała ma prosty promień, mały grot, lotki i nock. Każdy z tych elementów powinien być widoczny nawet po lekkim pogrubieniu pod izometrię.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

4–7 strzał z przesuniętymi grotami i lotkami, związanych w środku cienką linką.
 Strzały są podobne, lecz nie współliniowe; groty i lotki lekko się rozchodzą.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Wspólna paleta jak pojedyncza strzała; sznurek wyraźny w środku.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie nakładać wszystkich strzał idealnie na siebie.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.20. Kamienna siekiera (`axe`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Hafted_stone_axe_from_Robenhausen_lake-dwelling_Wellcome_M0015187.jpg?width=1100" alt="Referencja: Haftowana kamienna siekiera" width="680">

**Internetowa referencja:** [Haftowana kamienna siekiera](https://commons.wikimedia.org/wiki/File:Hafted_stone_axe_from_Robenhausen_lake-dwelling_Wellcome_M0015187.jpg)  
**Kategoria:** Narzędzie  
**Rola w grze:** Ścinanie drzew i walka awaryjna.  
**Docelowa skala:** 0,70–0,95 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Kamienna głowica jest osadzona mechanicznie w trzonku, a nie przyklejona do jego końca. Ważne są klin, rozszczepienie lub oprawa i mocne wiązanie.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Gruba rękojeść, poprzeczna asymetryczna głowica, klin lub rozszczepienie oraz widoczne oploty.
 Głowica jest poprzeczna i mechanicznie osadzona. Chwyt może być lekko wygięty i grubszy przy dłoni.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kamień chłodny z ostrzejszą jasną krawędzią, drewno ciepłe, oplot matowy.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie robić młotka z prostokątną szarą kostką.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.21. Ciężka kamienna siekiera (`axe_heavy`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Hafted_stone_axe_from_Robenhausen_lake-dwelling_Wellcome_M0015187.jpg?width=1100" alt="Referencja: Haftowana kamienna siekiera" width="680">

**Internetowa referencja:** [Haftowana kamienna siekiera](https://commons.wikimedia.org/wiki/File:Hafted_stone_axe_from_Robenhausen_lake-dwelling_Wellcome_M0015187.jpg)  
**Kategoria:** Narzędzie  
**Rola w grze:** Wydajniejsze ścinanie dużych drzew.  
**Docelowa skala:** 0,85–1,10 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Kamienna głowica jest osadzona mechanicznie w trzonku, a nie przyklejona do jego końca. Ważne są klin, rozszczepienie lub oprawa i mocne wiązanie.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Większa głowica i dłuższy trzonek; bryła ma wyglądać ciężko, ale pozostawać możliwa do użycia oburącz.
 Głowica jest poprzeczna i mechanicznie osadzona. Chwyt może być lekko wygięty i grubszy przy dłoni.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kamień chłodny z ostrzejszą jasną krawędzią, drewno ciepłe, oplot matowy.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie robić młotka z prostokątną szarą kostką.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.22. Prymitywny kilof (`pickaxe`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Hafted_stone_pick.jpg?width=1100" alt="Referencja: Kamienny kilof / oskard" width="680">

**Internetowa referencja:** [Kamienny kilof / oskard](https://commons.wikimedia.org/wiki/File:Hafted_stone_pick.jpg)  
**Kategoria:** Narzędzie  
**Rola w grze:** Rozbijanie skał i wydobywanie surowców.  
**Docelowa skala:** 0,85–1,15 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Głowica jest długa i poprzeczna względem trzonka; jeden koniec może być bardziej ostry, drugi klinowy lub tępy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Poprzeczna długa głowica: jeden koniec ostrzejszy, drugi klinowy; mocne wiązanie na skrzyżowaniu.
 Długi poprzeczny element musi mieć wyraźny profil roboczy i siedzieć dokładnie nad osią rękojeści.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kamień ciemniejszy niż w axe, lokalne otarcia; grip wytarty w dolnej połowie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć dwóch przypadkowych kamieni wystających z drewnianego klocka.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.23. Ognisko (`campfire`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Camp_fire_fire_pit.jpg?width=1100" alt="Referencja: Ognisko w kamiennym kręgu" width="680">

**Internetowa referencja:** [Ognisko w kamiennym kręgu](https://commons.wikimedia.org/wiki/File:Camp_fire_fire_pit.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Gotowanie, ciepło, światło i sen w obozie.  
**Docelowa skala:** 1,0–1,4 m średnicy  
**Stany gameplayowe:** niezapalane / rozpalone / żar / wygaszone

#### Analiza obrazu referencyjnego

Krąg nie jest idealny; kamienie różnią się rozmiarem, a wewnątrz widać popiół, żar i drewno ułożone zgodnie z logiką spalania.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Nieregularny kamienny krąg, żar, popiół i dwa poziomy drewna; płomień składa się z kilku cienkich języków.
 Kamienie tworzą nieregularny krąg kontaktujący się z ziemią. Drewno krzyżuje się i opiera na węglach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Szarości kamieni, czarny węgiel, szary popiół, ciepłe drewno i światło ognia.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać idealnego pierścienia i pionowej pomarańczowej kapsuły.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.24. Wygaszone palenisko (`campfire_burned`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Camp_fire_fire_pit.jpg?width=1100" alt="Referencja: Ognisko w kamiennym kręgu" width="680">

**Internetowa referencja:** [Ognisko w kamiennym kręgu](https://commons.wikimedia.org/wiki/File:Camp_fire_fire_pit.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Stan po wypaleniu ogniska.  
**Docelowa skala:** 1,0–1,4 m średnicy  
**Stany gameplayowe:** ciepłe / zimne / porzucone

#### Analiza obrazu referencyjnego

Krąg nie jest idealny; kamienie różnią się rozmiarem, a wewnątrz widać popiół, żar i drewno ułożone zgodnie z logiką spalania.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Zachowany krąg, szary popiół, czarne węgle i zapadnięte nadpalone kawałki drewna.
 Stan po spaleniu powinien zachować historię układu drewna, lecz z zapadniętymi i zwęglonymi elementami.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Dominują czernie, szarości i chłodne popioły; brak aktywnego pomarańczowego płomienia.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie ograniczać stanu do wyłączenia światła na niezmienionym ognisku.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.25. Skrzynia magazynowa (`storage_box`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Wooden_chest.jpg?width=1100" alt="Referencja: Drewniana skrzynia" width="680">

**Internetowa referencja:** [Drewniana skrzynia](https://commons.wikimedia.org/wiki/File:Wooden_chest.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Przechowywanie przedmiotów.  
**Docelowa skala:** 0,9–1,2 m szerokości  
**Stany gameplayowe:** zamknięta / otwarta / uszkodzona

#### Analiza obrazu referencyjnego

Skrzynia czyta się dzięki ścianom z desek, narożnym wzmocnieniom i osobnej pokrywie. Nie może wyglądać jak przypadkowa wiązka bali.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Prawdziwa skrzynia z desek: osobna pokrywa, narożne listwy, widoczne łączenia, peg lub skórzany uchwyt.
 Deski zamykają realną objętość. Pokrywa ma zawias/obrót, naroża są wzmocnione, dno ma podparcie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Ciepłe, zużyte drewno, ciemniejsze szczeliny, kołki i skórzane detale.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie budować skrzyni jak stosu bali bez ścian i wnętrza.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.26. Schronienie A-frame (`tent`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Bushcraft_Shelter_%2851648427088%29.jpg?width=1100" alt="Referencja: Schronienie bushcraftowe" width="680">

**Internetowa referencja:** [Schronienie bushcraftowe](https://commons.wikimedia.org/wiki/File:Bushcraft_Shelter_%2851648427088%29.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Sen, zapis/odpoczynek i ochrona przed pogodą.  
**Docelowa skala:** 2,0–2,5 m długości; 1,3–1,6 m wysokości  
**Stany gameplayowe:** zbudowane / używane / uszkodzone / mokre

#### Analiza obrazu referencyjnego

Konstrukcja ma ridgepole, podpory i gęste poszycie z naturalnego materiału. Warstwy opierają się na stelażu i schodzą blisko ziemi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Dwie rozwidlone podpory, ridgepole, 5–7 par krokwi, gęste nakładające się poszycie, niski trójkątny otwór i posłanie.
 Najpierw stelaż, potem poszycie. Każda warstwa opiera się na krokwi i zachodzi na warstwę niższą.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kora, skóra, gałęzie i suche liście; wnętrze ciemniejsze, posłanie miękkie i matowe.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać identycznych prostokątnych dachówek i nie zasłaniać całkowicie wejścia.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.27. Schronienie lean-to (`lean_to_shelter`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Lean_to_Shelter_%2817035853790%29.jpg?width=1100" alt="Referencja: Schronienie lean-to" width="680">

**Internetowa referencja:** [Schronienie lean-to](https://commons.wikimedia.org/wiki/File:Lean_to_Shelter_%2817035853790%29.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Prostsza osłona przed wiatrem i deszczem.  
**Docelowa skala:** 1,8–2,4 m szerokości  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Lean-to ma jedną dominującą połać, otwarty front oraz wyraźny rygiel i krokwie; nie jest połową sztywnego domku.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Otwarty front, jedna połać oparta na ryglu, warstwowa kora i gałęzie, niskie posłanie z traw.
 Jedna główna połać opiera się na ryglu; przód pozostaje otwarty i czytelny.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Naturalne, miejscami mokre drewno i kora, jaśniejsze posłanie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie robić symetrycznego A-frame ani płaskiej deski jako całej połaci.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.28. Palisada (`wall`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/%D0%9F%D0%B0%D0%BB%D1%96%D1%81%D0%B0%D0%B4.jpg?width=1100" alt="Referencja: Drewniana palisada" width="680">

**Internetowa referencja:** [Drewniana palisada](https://commons.wikimedia.org/wiki/File:%D0%9F%D0%B0%D0%BB%D1%96%D1%81%D0%B0%D0%B4.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Obrona obozu i prowadzenie ruchu stworzeń.  
**Docelowa skala:** 2,4–3,2 m szerokości sekcji  
**Stany gameplayowe:** pełna / uszkodzona / przełamana

#### Analiza obrazu referencyjnego

Palisada składa się z pionowo osadzonych, zaostrzonych pali o niewielkich różnicach wysokości i średnicy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

10–14 pali o różnej wysokości, zaostrzone końce, dwa tylne rygle i regularne, ale nieidentyczne wiązania.
 Pale wchodzą w ziemię i mają różne średnice. Rygle leżą z tyłu, a oploty przechodzą wokół obu części.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno ciemniejsze u ziemi, świeże jasne ostrza na górze, beżowe wiązania.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie ustawiać identycznych stożków w idealnym rytmie.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.29. Bariera z kolców (`spike_barrier`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/%D0%9F%D0%B0%D0%BB%D1%96%D1%81%D0%B0%D0%B4.jpg?width=1100" alt="Referencja: Drewniana palisada" width="680">

**Internetowa referencja:** [Drewniana palisada](https://commons.wikimedia.org/wiki/File:%D0%9F%D0%B0%D0%BB%D1%96%D1%81%D0%B0%D0%B4.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Niska przeszkoda obronna.  
**Docelowa skala:** 1,6–2,2 m szerokości  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Palisada składa się z pionowo osadzonych, zaostrzonych pali o niewielkich różnicach wysokości i średnicy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Pochylone zaostrzone żerdzie związane z niskim ryglem; środek ciężkości blisko ziemi.
 Pochylone pale tworzą niski kierunkowy grzebień i są zakotwiczone w ryglu lub podstawie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Podobnie jak palisada, lecz więcej świeżo ciętych jasnych końców.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie zawieszać kolców nad ziemią i nie łączyć ich bez podstawy.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.30. Pułapka deadfall (`trap`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Paiute_Deadfall_Trap.JPG?width=1100" alt="Referencja: Pułapka deadfall" width="680">

**Internetowa referencja:** [Pułapka deadfall](https://commons.wikimedia.org/wiki/File:Paiute_Deadfall_Trap.JPG)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Łapanie małych zwierząt.  
**Docelowa skala:** 0,9–1,3 m  
**Stany gameplayowe:** uzbrojona / uruchomiona / pusta

#### Analiza obrazu referencyjnego

Mechanizm musi czytać się jako ciężar podparty zestawem patyków i zwalniany przez trigger. Punkty kontaktu są ważniejsze niż liczba detali.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Ciężki pień lub płyta, pionowa podpórka, ukośny trigger i patyk z przynętą; wszystkie kontakty muszą być widoczne.
 Ciężar, support, diagonal i bait stick muszą pozostawać w widocznym kontakcie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno i kamień/ciężki pień; przynęta jest małym kolorystycznym akcentem.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Mechanizm nieczytelny lub lewitujący należy odrzucić bez dalszego detalu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.31. Ciężka pułapka deadfall (`deadfall_trap_heavy`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Paiute_Deadfall_Trap.JPG?width=1100" alt="Referencja: Pułapka deadfall" width="680">

**Internetowa referencja:** [Pułapka deadfall](https://commons.wikimedia.org/wiki/File:Paiute_Deadfall_Trap.JPG)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Pułapka na większą zdobycz.  
**Docelowa skala:** 1,2–1,7 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Mechanizm musi czytać się jako ciężar podparty zestawem patyków i zwalniany przez trigger. Punkty kontaktu są ważniejsze niż liczba detali.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Większy ciężar, grubsze podpory i szersza strefa rażenia; nadal czytelny figure-4.
 Ciężar, support, diagonal i bait stick muszą pozostawać w widocznym kontakcie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno i kamień/ciężki pień; przynęta jest małym kolorystycznym akcentem.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Mechanizm nieczytelny lub lewitujący należy odrzucić bez dalszego detalu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.32. Sidła (`snare_trap`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/SnareSpringPole.jpg?width=1100" alt="Referencja: Sidła sprężynowe" width="680">

**Internetowa referencja:** [Sidła sprężynowe](https://commons.wikimedia.org/wiki/File:SnareSpringPole.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Lekka pułapka na ścieżkach zwierząt.  
**Docelowa skala:** 0,4–0,8 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Pętla, kołek, trigger i napięty element sprężysty muszą tworzyć jeden zrozumiały układ.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Pętla przy ziemi, kołek, trigger i napięty elastyczny pęd; cienka lina powinna być lekko pogrubiona pod kamerę.
 Pętla leży na ścieżce, trigger jest przy kołku, a napięcie wynika z wygiętej gałęzi lub linki.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Cienkie włókno lekko rozjaśnione względem podłoża, aby nie znikło w izometrii.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie przedstawiać samej obręczy bez mechanizmu napięcia.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.33. Stelaż do skóry (`tanning_rack`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Process_of_Tanning_Hides._-_DPLA_-_77c9a4dc5173bca0badad7bb7cd9d085.jpg?width=1100" alt="Referencja: Proces garbowania skór" width="680">

**Internetowa referencja:** [Proces garbowania skór](https://commons.wikimedia.org/wiki/File:Process_of_Tanning_Hides._-_DPLA_-_77c9a4dc5173bca0badad7bb7cd9d085.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Przetwarzanie skór.  
**Docelowa skala:** 1,2–1,7 m wysokości  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Skóra jest rozpięta na ramie lub poddawana obróbce; w assetach ważne jest napięcie płata i punkty mocowania.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Prosta prostokątna rama z napiętym płatem skóry, kilkunastoma punktami wiązania i widocznym nierównym obrysem.
 Rama przenosi napięcie skóry; linki biegną promieniście od nieregularnego obrysu do belek.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Surowe drewno, jasna sucha skóra, ciemniejsze mokre krawędzie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Skóra nie może wisieć luźno jak zasłona, jeśli asset przedstawia naciąg.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.34. Stojak do suszenia mięsa (`drying_rack`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Caribou_meat_drying_on_rack_%2838724%29.jpg?width=1100" alt="Referencja: Mięso suszone na stojaku" width="680">

**Internetowa referencja:** [Mięso suszone na stojaku](https://commons.wikimedia.org/wiki/File:Caribou_meat_drying_on_rack_%2838724%29.jpg)  
**Kategoria:** Konstrukcja  
**Rola w grze:** Konserwowanie żywności.  
**Docelowa skala:** 1,2–1,7 m wysokości  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Stojak ma pionowe podpory, poziomy rygiel i zwisające pasy mięsa rozdzielone tak, aby powietrze mogło przepływać.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Dwie podpory, poziomy rygiel i rozdzielone paski mięsa; lekkie ugięcie rygla pod ciężarem.
 Rygiel jest wsparty po obu stronach; paski wiszą pionowo i nie nachodzą na siebie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Drewno matowe, mięso ciemniejsze i bardziej suche niż surowy pickup.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie wieszać kul mięsa ani identycznych prostokątów.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.35. Drzewo liściaste bazowe (`leafy_tree`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Oak_tree.jpg?width=1100" alt="Referencja: Dojrzałe drzewo liściaste" width="680">

**Internetowa referencja:** [Dojrzałe drzewo liściaste](https://commons.wikimedia.org/wiki/File:Oak_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4–7 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Pień rozdziela się na hierarchię konarów, a korona jest szeroka, nieregularna i cięższa na niektórych stronach.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Kanoniczny, średniej wysokości okaz z szeroką, nieregularną koroną.
 Pień ma flare, następnie konary pierwszego rzędu, mniejsze gałęzie i dopiero skupiska liści.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kora z pionowym rytmem, korona z 3–5 tonami zieleni; dół ciemniejszy, szczyt bardziej nasłoneczniony.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać pnia-walca i kulistej korony bez struktury gałęzi.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.36. Drzewo liściaste A (`leafy_tree_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Oak_tree.jpg?width=1100" alt="Referencja: Dojrzałe drzewo liściaste" width="680">

**Internetowa referencja:** [Dojrzałe drzewo liściaste](https://commons.wikimedia.org/wiki/File:Oak_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4–7 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Pień rozdziela się na hierarchię konarów, a korona jest szeroka, nieregularna i cięższa na niektórych stronach.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Młodszy dojrzały okaz: prostszy pień i bardziej zwarta korona.
 Pień ma flare, następnie konary pierwszego rzędu, mniejsze gałęzie i dopiero skupiska liści.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kora z pionowym rytmem, korona z 3–5 tonami zieleni; dół ciemniejszy, szczyt bardziej nasłoneczniony.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać pnia-walca i kulistej korony bez struktury gałęzi.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.37. Drzewo liściaste B (`leafy_tree_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Oak_tree.jpg?width=1100" alt="Referencja: Dojrzałe drzewo liściaste" width="680">

**Internetowa referencja:** [Dojrzałe drzewo liściaste](https://commons.wikimedia.org/wiki/File:Oak_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4–7 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Pień rozdziela się na hierarchię konarów, a korona jest szeroka, nieregularna i cięższa na niektórych stronach.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wyższy okaz leśny: dłuższy bezgałęziowy odcinek pnia i korona przesunięta ku górze.
 Pień ma flare, następnie konary pierwszego rzędu, mniejsze gałęzie i dopiero skupiska liści.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kora z pionowym rytmem, korona z 3–5 tonami zieleni; dół ciemniejszy, szczyt bardziej nasłoneczniony.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać pnia-walca i kulistej korony bez struktury gałęzi.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.38. Drzewo liściaste C (`leafy_tree_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Oak_tree.jpg?width=1100" alt="Referencja: Dojrzałe drzewo liściaste" width="680">

**Internetowa referencja:** [Dojrzałe drzewo liściaste](https://commons.wikimedia.org/wiki/File:Oak_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4–7 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Pień rozdziela się na hierarchię konarów, a korona jest szeroka, nieregularna i cięższa na niektórych stronach.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Szeroki, asymetryczny okaz rosnący na otwartej przestrzeni; niższe konary.
 Pień ma flare, następnie konary pierwszego rzędu, mniejsze gałęzie i dopiero skupiska liści.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kora z pionowym rytmem, korona z 3–5 tonami zieleni; dół ciemniejszy, szczyt bardziej nasłoneczniony.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać pnia-walca i kulistej korony bez struktury gałęzi.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.39. Drzewo liściaste D (`leafy_tree_d`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Oak_tree.jpg?width=1100" alt="Referencja: Dojrzałe drzewo liściaste" width="680">

**Internetowa referencja:** [Dojrzałe drzewo liściaste](https://commons.wikimedia.org/wiki/File:Oak_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4–7 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Pień rozdziela się na hierarchię konarów, a korona jest szeroka, nieregularna i cięższa na niektórych stronach.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Największy standardowy wariant: grubszy pień, cięższa korona i częściowo odsłonięte korzenie.
 Pień ma flare, następnie konary pierwszego rzędu, mniejsze gałęzie i dopiero skupiska liści.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kora z pionowym rytmem, korona z 3–5 tonami zieleni; dół ciemniejszy, szczyt bardziej nasłoneczniony.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać pnia-walca i kulistej korony bez struktury gałęzi.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.40. Drzewo iglaste bazowe (`conifer_tree`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Spruce_tree.jpg?width=1100" alt="Referencja: Dojrzały świerk" width="680">

**Internetowa referencja:** [Dojrzały świerk](https://commons.wikimedia.org/wiki/File:Spruce_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4,5–7,5 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Świerk ma wyraźny leader, stożkową sylwetkę, piętra konarów i opadające drobne gałęzie; masa igieł rośnie ku dołowi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Kanoniczny świerk: stożkowa sylwetka i wyraźne piętra.
 Jeden leader, piętra konarów i masy igieł rozłożone również wzdłuż gałęzi, nie tylko na końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Ciemne chłodne igły wewnątrz, jaśniejsze końcówki, brązowy pień widoczny między piętrami.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie budować korony jako kilku poziomych wentylatorów lub przypadkowych kart.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.41. Drzewo iglaste A (`conifer_tree_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Spruce_tree.jpg?width=1100" alt="Referencja: Dojrzały świerk" width="680">

**Internetowa referencja:** [Dojrzały świerk](https://commons.wikimedia.org/wiki/File:Spruce_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4,5–7,5 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Świerk ma wyraźny leader, stożkową sylwetkę, piętra konarów i opadające drobne gałęzie; masa igieł rośnie ku dołowi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Zwarte, młodsze drzewo z gęstymi dolnymi boughami.
 Jeden leader, piętra konarów i masy igieł rozłożone również wzdłuż gałęzi, nie tylko na końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Ciemne chłodne igły wewnątrz, jaśniejsze końcówki, brązowy pień widoczny między piętrami.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie budować korony jako kilku poziomych wentylatorów lub przypadkowych kart.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.42. Drzewo iglaste B (`conifer_tree_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Spruce_tree.jpg?width=1100" alt="Referencja: Dojrzały świerk" width="680">

**Internetowa referencja:** [Dojrzały świerk](https://commons.wikimedia.org/wiki/File:Spruce_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4,5–7,5 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Świerk ma wyraźny leader, stożkową sylwetkę, piętra konarów i opadające drobne gałęzie; masa igieł rośnie ku dołowi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wysoki, smukły wariant leśny z bardziej odsłoniętym pniem.
 Jeden leader, piętra konarów i masy igieł rozłożone również wzdłuż gałęzi, nie tylko na końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Ciemne chłodne igły wewnątrz, jaśniejsze końcówki, brązowy pień widoczny między piętrami.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie budować korony jako kilku poziomych wentylatorów lub przypadkowych kart.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.43. Drzewo iglaste C (`conifer_tree_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Spruce_tree.jpg?width=1100" alt="Referencja: Dojrzały świerk" width="680">

**Internetowa referencja:** [Dojrzały świerk](https://commons.wikimedia.org/wiki/File:Spruce_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4,5–7,5 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Świerk ma wyraźny leader, stożkową sylwetkę, piętra konarów i opadające drobne gałęzie; masa igieł rośnie ku dołowi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Niższy, szerszy wariant przypominający sosnę lub świerk rosnący na otwartej przestrzeni.
 Jeden leader, piętra konarów i masy igieł rozłożone również wzdłuż gałęzi, nie tylko na końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Ciemne chłodne igły wewnątrz, jaśniejsze końcówki, brązowy pień widoczny między piętrami.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie budować korony jako kilku poziomych wentylatorów lub przypadkowych kart.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.44. Drzewo iglaste D (`conifer_tree_d`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Spruce_tree.jpg?width=1100" alt="Referencja: Dojrzały świerk" width="680">

**Internetowa referencja:** [Dojrzały świerk](https://commons.wikimedia.org/wiki/File:Spruce_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zasób drewna i element biomu.  
**Docelowa skala:** 4,5–7,5 m  
**Stany gameplayowe:** pełne / ścięte / pień / odrastanie

#### Analiza obrazu referencyjnego

Świerk ma wyraźny leader, stożkową sylwetkę, piętra konarów i opadające drobne gałęzie; masa igieł rośnie ku dołowi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Duży stary okaz z nieregularnymi, częściowo suchymi dolnymi gałęziami.
 Jeden leader, piętra konarów i masy igieł rozłożone również wzdłuż gałęzi, nie tylko na końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Ciemne chłodne igły wewnątrz, jaśniejsze końcówki, brązowy pień widoczny między piętrami.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie budować korony jako kilku poziomych wentylatorów lub przypadkowych kart.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.45. Suche drzewo bazowe (`dry_tree`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dead_Tree_Standing.jpg?width=1100" alt="Referencja: Stojące martwe drzewo" width="680">

**Internetowa referencja:** [Stojące martwe drzewo](https://commons.wikimedia.org/wiki/File:Dead_Tree_Standing.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Martwy zasób drewna i czytelny element krajobrazu.  
**Docelowa skala:** 3–6 m  
**Stany gameplayowe:** stojące / złamane / pień

#### Analiza obrazu referencyjnego

Martwe drzewo nadal ma hierarchię gałęzi, złamane końcówki, miejscami odchodzącą korę i ciężką, nieregularną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Martwe drzewo o zachowanej głównej hierarchii konarów.
 Złamane gałęzie nadal wynikają z realnego drzewa; grubość maleje od pnia do końców.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Wyblakłe drewno, ciemne ubytki, ślady kory i grzybów.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć gwiazdy z jednakowych ostrych patyków.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.46. Suche drzewo A (`dry_tree_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dead_Tree_Standing.jpg?width=1100" alt="Referencja: Stojące martwe drzewo" width="680">

**Internetowa referencja:** [Stojące martwe drzewo](https://commons.wikimedia.org/wiki/File:Dead_Tree_Standing.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Martwy zasób drewna i czytelny element krajobrazu.  
**Docelowa skala:** 3–6 m  
**Stany gameplayowe:** stojące / złamane / pień

#### Analiza obrazu referencyjnego

Martwe drzewo nadal ma hierarchię gałęzi, złamane końcówki, miejscami odchodzącą korę i ciężką, nieregularną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Niższy, mocno rozgałęziony okaz liściasty.
 Złamane gałęzie nadal wynikają z realnego drzewa; grubość maleje od pnia do końców.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Wyblakłe drewno, ciemne ubytki, ślady kory i grzybów.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć gwiazdy z jednakowych ostrych patyków.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.47. Suche drzewo B (`dry_tree_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dead_Tree_Standing.jpg?width=1100" alt="Referencja: Stojące martwe drzewo" width="680">

**Internetowa referencja:** [Stojące martwe drzewo](https://commons.wikimedia.org/wiki/File:Dead_Tree_Standing.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Martwy zasób drewna i czytelny element krajobrazu.  
**Docelowa skala:** 3–6 m  
**Stany gameplayowe:** stojące / złamane / pień

#### Analiza obrazu referencyjnego

Martwe drzewo nadal ma hierarchię gałęzi, złamane końcówki, miejscami odchodzącą korę i ciężką, nieregularną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wysoki pień po uderzeniu pioruna, z jednym dominującym złamaniem.
 Złamane gałęzie nadal wynikają z realnego drzewa; grubość maleje od pnia do końców.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Wyblakłe drewno, ciemne ubytki, ślady kory i grzybów.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć gwiazdy z jednakowych ostrych patyków.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.48. Suche drzewo C (`dry_tree_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dead_Tree_Standing.jpg?width=1100" alt="Referencja: Stojące martwe drzewo" width="680">

**Internetowa referencja:** [Stojące martwe drzewo](https://commons.wikimedia.org/wiki/File:Dead_Tree_Standing.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Martwy zasób drewna i czytelny element krajobrazu.  
**Docelowa skala:** 3–6 m  
**Stany gameplayowe:** stojące / złamane / pień

#### Analiza obrazu referencyjnego

Martwe drzewo nadal ma hierarchię gałęzi, złamane końcówki, miejscami odchodzącą korę i ciężką, nieregularną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Martwy iglak z krótszymi, opadającymi kikutami gałęzi.
 Złamane gałęzie nadal wynikają z realnego drzewa; grubość maleje od pnia do końców.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Wyblakłe drewno, ciemne ubytki, ślady kory i grzybów.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć gwiazdy z jednakowych ostrych patyków.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.49. Sadzonka liściasta A (`leafy_sapling_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Oak_tree.jpg?width=1100" alt="Referencja: Dojrzałe drzewo liściaste" width="680">

**Internetowa referencja:** [Dojrzałe drzewo liściaste](https://commons.wikimedia.org/wiki/File:Oak_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Młody etap drzewa lub drobny zasób.  
**Docelowa skala:** 0,8–2,0 m  
**Stany gameplayowe:** żywa / uszkodzona / sucha

#### Analiza obrazu referencyjnego

Pień rozdziela się na hierarchię konarów, a korona jest szeroka, nieregularna i cięższa na niektórych stronach.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Bardzo młoda, 2–3 główne pędy i mała korona.
 Sadzonka ma smukły elastyczny pień, niewiele gałęzi i małą koronę proporcjonalną do wieku.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Jaśniejsza kora i młodsze, bardziej nasycone liście.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie skalować po prostu dorosłego drzewa do 25%.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.50. Sadzonka liściasta B (`leafy_sapling_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Oak_tree.jpg?width=1100" alt="Referencja: Dojrzałe drzewo liściaste" width="680">

**Internetowa referencja:** [Dojrzałe drzewo liściaste](https://commons.wikimedia.org/wiki/File:Oak_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Młody etap drzewa lub drobny zasób.  
**Docelowa skala:** 0,8–2,0 m  
**Stany gameplayowe:** żywa / uszkodzona / sucha

#### Analiza obrazu referencyjnego

Pień rozdziela się na hierarchię konarów, a korona jest szeroka, nieregularna i cięższa na niektórych stronach.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Smukła sadzonka z koroną zaczynającą się w połowie wysokości.
 Sadzonka ma smukły elastyczny pień, niewiele gałęzi i małą koronę proporcjonalną do wieku.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Jaśniejsza kora i młodsze, bardziej nasycone liście.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie skalować po prostu dorosłego drzewa do 25%.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.51. Sadzonka liściasta C (`leafy_sapling_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Oak_tree.jpg?width=1100" alt="Referencja: Dojrzałe drzewo liściaste" width="680">

**Internetowa referencja:** [Dojrzałe drzewo liściaste](https://commons.wikimedia.org/wiki/File:Oak_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Młody etap drzewa lub drobny zasób.  
**Docelowa skala:** 0,8–2,0 m  
**Stany gameplayowe:** żywa / uszkodzona / sucha

#### Analiza obrazu referencyjnego

Pień rozdziela się na hierarchię konarów, a korona jest szeroka, nieregularna i cięższa na niektórych stronach.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Szersza, rozkrzewiona sadzonka na otwartym stanowisku.
 Sadzonka ma smukły elastyczny pień, niewiele gałęzi i małą koronę proporcjonalną do wieku.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Jaśniejsza kora i młodsze, bardziej nasycone liście.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie skalować po prostu dorosłego drzewa do 25%.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.52. Sadzonka iglasta A (`conifer_sapling_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Spruce_tree.jpg?width=1100" alt="Referencja: Dojrzały świerk" width="680">

**Internetowa referencja:** [Dojrzały świerk](https://commons.wikimedia.org/wiki/File:Spruce_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Młody etap drzewa lub drobny zasób.  
**Docelowa skala:** 0,8–2,0 m  
**Stany gameplayowe:** żywa / uszkodzona / sucha

#### Analiza obrazu referencyjnego

Świerk ma wyraźny leader, stożkową sylwetkę, piętra konarów i opadające drobne gałęzie; masa igieł rośnie ku dołowi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Mały, gęsty stożek z krótkim leaderem.
 Leader dominuje, a piętra są jeszcze krótkie i nieregularnie rozwinięte.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Młode igły jaśniejsze, pień bardzo cienki, ale widoczny.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć zwartego stożka bez gałęzi.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.53. Sadzonka iglasta B (`conifer_sapling_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Spruce_tree.jpg?width=1100" alt="Referencja: Dojrzały świerk" width="680">

**Internetowa referencja:** [Dojrzały świerk](https://commons.wikimedia.org/wiki/File:Spruce_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Młody etap drzewa lub drobny zasób.  
**Docelowa skala:** 0,8–2,0 m  
**Stany gameplayowe:** żywa / uszkodzona / sucha

#### Analiza obrazu referencyjnego

Świerk ma wyraźny leader, stożkową sylwetkę, piętra konarów i opadające drobne gałęzie; masa igieł rośnie ku dołowi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wyższa i rzadsza sadzonka leśna.
 Leader dominuje, a piętra są jeszcze krótkie i nieregularnie rozwinięte.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Młode igły jaśniejsze, pień bardzo cienki, ale widoczny.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć zwartego stożka bez gałęzi.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.54. Sadzonka iglasta C (`conifer_sapling_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Spruce_tree.jpg?width=1100" alt="Referencja: Dojrzały świerk" width="680">

**Internetowa referencja:** [Dojrzały świerk](https://commons.wikimedia.org/wiki/File:Spruce_tree.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Młody etap drzewa lub drobny zasób.  
**Docelowa skala:** 0,8–2,0 m  
**Stany gameplayowe:** żywa / uszkodzona / sucha

#### Analiza obrazu referencyjnego

Świerk ma wyraźny leader, stożkową sylwetkę, piętra konarów i opadające drobne gałęzie; masa igieł rośnie ku dołowi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Niższa, szeroka sadzonka z mocnym dolnym piętrem.
 Leader dominuje, a piętra są jeszcze krótkie i nieregularnie rozwinięte.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Młode igły jaśniejsze, pień bardzo cienki, ale widoczny.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć zwartego stożka bez gałęzi.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.55. Sucha sadzonka A (`dry_sapling_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dead_Tree_Standing.jpg?width=1100" alt="Referencja: Stojące martwe drzewo" width="680">

**Internetowa referencja:** [Stojące martwe drzewo](https://commons.wikimedia.org/wiki/File:Dead_Tree_Standing.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Młody etap drzewa lub drobny zasób.  
**Docelowa skala:** 0,8–2,0 m  
**Stany gameplayowe:** żywa / uszkodzona / sucha

#### Analiza obrazu referencyjnego

Martwe drzewo nadal ma hierarchię gałęzi, złamane końcówki, miejscami odchodzącą korę i ciężką, nieregularną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Cienki martwy pęd z kilkoma krótkimi rozwidleniami.
 Cienki pęd ma kilka logicznych rozwidleń i złamanie o włóknistym końcu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Jasne suche drewno z ciemną podstawą.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać jednego pionowego czarnego patyka.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.56. Sucha sadzonka B (`dry_sapling_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dead_Tree_Standing.jpg?width=1100" alt="Referencja: Stojące martwe drzewo" width="680">

**Internetowa referencja:** [Stojące martwe drzewo](https://commons.wikimedia.org/wiki/File:Dead_Tree_Standing.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Młody etap drzewa lub drobny zasób.  
**Docelowa skala:** 0,8–2,0 m  
**Stany gameplayowe:** żywa / uszkodzona / sucha

#### Analiza obrazu referencyjnego

Martwe drzewo nadal ma hierarchię gałęzi, złamane końcówki, miejscami odchodzącą korę i ciężką, nieregularną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Złamana sadzonka z odsłoniętym jasnym drewnem.
 Cienki pęd ma kilka logicznych rozwidleń i złamanie o włóknistym końcu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Jasne suche drewno z ciemną podstawą.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać jednego pionowego czarnego patyka.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.57. Skała zasobowa (`rock`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Weathered_rock_outcrop_-_geograph.org.uk_-_1414636.jpg?width=1100" alt="Referencja: Zwietrzała wychodnia skalna" width="680">

**Internetowa referencja:** [Zwietrzała wychodnia skalna](https://commons.wikimedia.org/wiki/File:Weathered_rock_outcrop_-_geograph.org.uk_-_1414636.jpg)  
**Kategoria:** Zasób świata  
**Rola w grze:** Duży węzeł kamienia do rozbijania.  
**Docelowa skala:** 1,2–2,2 m  
**Stany gameplayowe:** pełna / popękana / rozbita

#### Analiza obrazu referencyjnego

Wychodnia nie jest jedną gładką kulą; ma warstwy, uskoki, pęknięcia i mniejsze odłamy u podstawy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wychodnia z 3–6 połączonych mas skalnych, pęknięciami, odłamami i mchem u podstawy.
 Masy skalne nakładają się zgodnie z warstwami i ciężarem; mniejsze odłamy zbierają się u podstawy.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kilka szarości, chłodne zacienienia, mech i zabrudzenie w szczelinach.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać jednej ogromnej gładkiej sfery.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.58. Mały klaster skał (`rock_cluster_small`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Weathered_rock_outcrop_-_geograph.org.uk_-_1414636.jpg?width=1100" alt="Referencja: Zwietrzała wychodnia skalna" width="680">

**Internetowa referencja:** [Zwietrzała wychodnia skalna](https://commons.wikimedia.org/wiki/File:Weathered_rock_outcrop_-_geograph.org.uk_-_1414636.jpg)  
**Kategoria:** Zasób świata  
**Rola w grze:** Mniejszy zasób lub dekoracja.  
**Docelowa skala:** 0,5–1,0 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Wychodnia nie jest jedną gładką kulą; ma warstwy, uskoki, pęknięcia i mniejsze odłamy u podstawy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Jeden kamień dominujący i 3–5 mniejszych fragmentów zakotwiczonych w ziemi.
 Klaster powinien mieć jeden dominujący głaz i logiczne mniejsze odłamy dotykające podłoża.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Różnice koloru są subtelne; krawędzie mogą być jaśniejsze, szczeliny ciemniejsze.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie układać kamieni jak koralików wokół głównego obiektu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.59. Duży klaster skał (`rock_cluster_large`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Weathered_rock_outcrop_-_geograph.org.uk_-_1414636.jpg?width=1100" alt="Referencja: Zwietrzała wychodnia skalna" width="680">

**Internetowa referencja:** [Zwietrzała wychodnia skalna](https://commons.wikimedia.org/wiki/File:Weathered_rock_outcrop_-_geograph.org.uk_-_1414636.jpg)  
**Kategoria:** Zasób świata  
**Rola w grze:** Duży zasób i przeszkoda terenowa.  
**Docelowa skala:** 1,6–2,8 m  
**Stany gameplayowe:** standardowy / użyty / uszkodzony

#### Analiza obrazu referencyjnego

Wychodnia nie jest jedną gładką kulą; ma warstwy, uskoki, pęknięcia i mniejsze odłamy u podstawy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Warstwowy układ kilku głazów; czytelne pęknięcia, nawis i rozrzucony gruz.
 Klaster powinien mieć jeden dominujący głaz i logiczne mniejsze odłamy dotykające podłoża.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Różnice koloru są subtelne; krawędzie mogą być jaśniejsze, szczeliny ciemniejsze.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie układać kamieni jak koralików wokół głównego obiektu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.60. Zielony krzew bazowy (`green_bush`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/JAPANESE_FOREST_SHRUBS.jpg?width=1100" alt="Referencja: Leśny krzew" width="680">

**Internetowa referencja:** [Leśny krzew](https://commons.wikimedia.org/wiki/File:JAPANESE_FOREST_SHRUBS.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Element podszytu i osłona wizualna.  
**Docelowa skala:** 0,6–1,4 m  
**Stany gameplayowe:** pełny / przycięty / suchy

#### Analiza obrazu referencyjnego

Krzew ma wiele pędów wychodzących z podstawy oraz liście rozłożone na zewnętrznej powłoce, a nie jedną kulistą koronę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Kanoniczny krzew: 8–12 pędów, zwarta lecz nieregularna powłoka liści.
 Szkielet pędów jest widoczny w centrum i pod spodem, liście skupiają się przy końcach i na zewnętrzu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

3–4 tony zieleni, młode liście jaśniejsze, gałązki ciepłe i ciemne.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć krzewu z wielu zielonych kul.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.61. Zielony krzew A (`green_bush_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/JAPANESE_FOREST_SHRUBS.jpg?width=1100" alt="Referencja: Leśny krzew" width="680">

**Internetowa referencja:** [Leśny krzew](https://commons.wikimedia.org/wiki/File:JAPANESE_FOREST_SHRUBS.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Element podszytu i osłona wizualna.  
**Docelowa skala:** 0,6–1,4 m  
**Stany gameplayowe:** pełny / przycięty / suchy

#### Analiza obrazu referencyjnego

Krzew ma wiele pędów wychodzących z podstawy oraz liście rozłożone na zewnętrznej powłoce, a nie jedną kulistą koronę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Niski i zwarty, bardziej okrągły, ale z przerwami ujawniającymi gałęzie.
 Szkielet pędów jest widoczny w centrum i pod spodem, liście skupiają się przy końcach i na zewnętrzu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

3–4 tony zieleni, młode liście jaśniejsze, gałązki ciepłe i ciemne.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć krzewu z wielu zielonych kul.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.62. Zielony krzew B (`green_bush_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/JAPANESE_FOREST_SHRUBS.jpg?width=1100" alt="Referencja: Leśny krzew" width="680">

**Internetowa referencja:** [Leśny krzew](https://commons.wikimedia.org/wiki/File:JAPANESE_FOREST_SHRUBS.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Element podszytu i osłona wizualna.  
**Docelowa skala:** 0,6–1,4 m  
**Stany gameplayowe:** pełny / przycięty / suchy

#### Analiza obrazu referencyjnego

Krzew ma wiele pędów wychodzących z podstawy oraz liście rozłożone na zewnętrznej powłoce, a nie jedną kulistą koronę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Rozłożysty, z dwoma dominującymi kierunkami wzrostu.
 Szkielet pędów jest widoczny w centrum i pod spodem, liście skupiają się przy końcach i na zewnętrzu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

3–4 tony zieleni, młode liście jaśniejsze, gałązki ciepłe i ciemne.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć krzewu z wielu zielonych kul.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.63. Zielony krzew C (`green_bush_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/JAPANESE_FOREST_SHRUBS.jpg?width=1100" alt="Referencja: Leśny krzew" width="680">

**Internetowa referencja:** [Leśny krzew](https://commons.wikimedia.org/wiki/File:JAPANESE_FOREST_SHRUBS.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Element podszytu i osłona wizualna.  
**Docelowa skala:** 0,6–1,4 m  
**Stany gameplayowe:** pełny / przycięty / suchy

#### Analiza obrazu referencyjnego

Krzew ma wiele pędów wychodzących z podstawy oraz liście rozłożone na zewnętrznej powłoce, a nie jedną kulistą koronę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wyższy i bardziej ażurowy, z odsłoniętymi pionowymi pędami.
 Szkielet pędów jest widoczny w centrum i pod spodem, liście skupiają się przy końcach i na zewnętrzu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

3–4 tony zieleni, młode liście jaśniejsze, gałązki ciepłe i ciemne.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć krzewu z wielu zielonych kul.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.64. Zielony krzew D (`green_bush_d`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/JAPANESE_FOREST_SHRUBS.jpg?width=1100" alt="Referencja: Leśny krzew" width="680">

**Internetowa referencja:** [Leśny krzew](https://commons.wikimedia.org/wiki/File:JAPANESE_FOREST_SHRUBS.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Element podszytu i osłona wizualna.  
**Docelowa skala:** 0,6–1,4 m  
**Stany gameplayowe:** pełny / przycięty / suchy

#### Analiza obrazu referencyjnego

Krzew ma wiele pędów wychodzących z podstawy oraz liście rozłożone na zewnętrznej powłoce, a nie jedną kulistą koronę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Duży, gęsty okaz nadający się jako zasłona środowiskowa.
 Szkielet pędów jest widoczny w centrum i pod spodem, liście skupiają się przy końcach i na zewnętrzu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

3–4 tony zieleni, młode liście jaśniejsze, gałązki ciepłe i ciemne.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć krzewu z wielu zielonych kul.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.65. Leśny podszyt A (`forest_shrub_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/JAPANESE_FOREST_SHRUBS.jpg?width=1100" alt="Referencja: Leśny krzew" width="680">

**Internetowa referencja:** [Leśny krzew](https://commons.wikimedia.org/wiki/File:JAPANESE_FOREST_SHRUBS.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Wypełnianie dna lasu i miękkie prowadzenie kompozycji.  
**Docelowa skala:** 0,35–1,0 m  
**Stany gameplayowe:** zielony / przywiędły

#### Analiza obrazu referencyjnego

Krzew ma wiele pędów wychodzących z podstawy oraz liście rozłożone na zewnętrznej powłoce, a nie jedną kulistą koronę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Niska wielowarstwowa roślina z szerokimi liśćmi.
 Podszyt łączy różne wysokości i typy liści, ale ma wspólną podstawę w glebie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zielenie bardziej chłodne i ciemne niż na otwartym terenie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie rozrzucać niezależnych kart bez łodyg.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.66. Leśny podszyt B (`forest_shrub_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/JAPANESE_FOREST_SHRUBS.jpg?width=1100" alt="Referencja: Leśny krzew" width="680">

**Internetowa referencja:** [Leśny krzew](https://commons.wikimedia.org/wiki/File:JAPANESE_FOREST_SHRUBS.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Wypełnianie dna lasu i miękkie prowadzenie kompozycji.  
**Docelowa skala:** 0,35–1,0 m  
**Stany gameplayowe:** zielony / przywiędły

#### Analiza obrazu referencyjnego

Krzew ma wiele pędów wychodzących z podstawy oraz liście rozłożone na zewnętrznej powłoce, a nie jedną kulistą koronę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wyższe pędy i liście skupione przy końcach.
 Podszyt łączy różne wysokości i typy liści, ale ma wspólną podstawę w glebie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zielenie bardziej chłodne i ciemne niż na otwartym terenie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie rozrzucać niezależnych kart bez łodyg.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.67. Leśny podszyt C (`forest_shrub_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/JAPANESE_FOREST_SHRUBS.jpg?width=1100" alt="Referencja: Leśny krzew" width="680">

**Internetowa referencja:** [Leśny krzew](https://commons.wikimedia.org/wiki/File:JAPANESE_FOREST_SHRUBS.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Wypełnianie dna lasu i miękkie prowadzenie kompozycji.  
**Docelowa skala:** 0,35–1,0 m  
**Stany gameplayowe:** zielony / przywiędły

#### Analiza obrazu referencyjnego

Krzew ma wiele pędów wychodzących z podstawy oraz liście rozłożone na zewnętrznej powłoce, a nie jedną kulistą koronę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Gęsta plama runa łącząca krzew, paprociowe liście i martwe gałązki.
 Podszyt łączy różne wysokości i typy liści, ale ma wspólną podstawę w glebie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zielenie bardziej chłodne i ciemne niż na otwartym terenie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie rozrzucać niezależnych kart bez łodyg.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.68. Suchy krzew bazowy (`dry_bush`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dry_bush_at_Wiyoni.jpg?width=1100" alt="Referencja: Suchy krzew" width="680">

**Internetowa referencja:** [Suchy krzew](https://commons.wikimedia.org/wiki/File:Dry_bush_at_Wiyoni.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Martwy podszyt, rozpałka i różnicowanie biomu.  
**Docelowa skala:** 0,4–1,2 m  
**Stany gameplayowe:** suchy / połamany / zebrany

#### Analiza obrazu referencyjnego

Suchy krzew jest ażurowy: większość jego obrazu tworzą cienkie, rozgałęzione pędy i puste przestrzenie.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Ażurowy układ cienkich, rozgałęzionych pędów.
 Większość bryły stanowią rozgałęzione linie i puste przestrzenie; grubsze pędy są przy podstawie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Suche brązy, szarości i pojedyncze jasne końcówki.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać losowego radialnego starburstu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.69. Suchy krzew A (`dry_bush_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dry_bush_at_Wiyoni.jpg?width=1100" alt="Referencja: Suchy krzew" width="680">

**Internetowa referencja:** [Suchy krzew](https://commons.wikimedia.org/wiki/File:Dry_bush_at_Wiyoni.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Martwy podszyt, rozpałka i różnicowanie biomu.  
**Docelowa skala:** 0,4–1,2 m  
**Stany gameplayowe:** suchy / połamany / zebrany

#### Analiza obrazu referencyjnego

Suchy krzew jest ażurowy: większość jego obrazu tworzą cienkie, rozgałęzione pędy i puste przestrzenie.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Niski, szeroki krzew o wielu krótkich forkach.
 Większość bryły stanowią rozgałęzione linie i puste przestrzenie; grubsze pędy są przy podstawie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Suche brązy, szarości i pojedyncze jasne końcówki.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać losowego radialnego starburstu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.70. Suchy krzew B (`dry_bush_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dry_bush_at_Wiyoni.jpg?width=1100" alt="Referencja: Suchy krzew" width="680">

**Internetowa referencja:** [Suchy krzew](https://commons.wikimedia.org/wiki/File:Dry_bush_at_Wiyoni.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Martwy podszyt, rozpałka i różnicowanie biomu.  
**Docelowa skala:** 0,4–1,2 m  
**Stany gameplayowe:** suchy / połamany / zebrany

#### Analiza obrazu referencyjnego

Suchy krzew jest ażurowy: większość jego obrazu tworzą cienkie, rozgałęzione pędy i puste przestrzenie.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wyższy, rzadki okaz z pionowymi pędami.
 Większość bryły stanowią rozgałęzione linie i puste przestrzenie; grubsze pędy są przy podstawie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Suche brązy, szarości i pojedyncze jasne końcówki.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać losowego radialnego starburstu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.71. Suchy krzew C (`dry_bush_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dry_bush_at_Wiyoni.jpg?width=1100" alt="Referencja: Suchy krzew" width="680">

**Internetowa referencja:** [Suchy krzew](https://commons.wikimedia.org/wiki/File:Dry_bush_at_Wiyoni.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Martwy podszyt, rozpałka i różnicowanie biomu.  
**Docelowa skala:** 0,4–1,2 m  
**Stany gameplayowe:** suchy / połamany / zebrany

#### Analiza obrazu referencyjnego

Suchy krzew jest ażurowy: większość jego obrazu tworzą cienkie, rozgałęzione pędy i puste przestrzenie.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Połamana, jednostronna sylwetka po wysuszeniu lub wietrze.
 Większość bryły stanowią rozgałęzione linie i puste przestrzenie; grubsze pędy są przy podstawie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Suche brązy, szarości i pojedyncze jasne końcówki.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać losowego radialnego starburstu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.72. Suchy krzew D (`dry_bush_d`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Dry_bush_at_Wiyoni.jpg?width=1100" alt="Referencja: Suchy krzew" width="680">

**Internetowa referencja:** [Suchy krzew](https://commons.wikimedia.org/wiki/File:Dry_bush_at_Wiyoni.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Martwy podszyt, rozpałka i różnicowanie biomu.  
**Docelowa skala:** 0,4–1,2 m  
**Stany gameplayowe:** suchy / połamany / zebrany

#### Analiza obrazu referencyjnego

Suchy krzew jest ażurowy: większość jego obrazu tworzą cienkie, rozgałęzione pędy i puste przestrzenie.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Duży, stary krzew z grubszym szkieletem i nielicznymi strąkami.
 Większość bryły stanowią rozgałęzione linie i puste przestrzenie; grubsze pędy są przy podstawie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Suche brązy, szarości i pojedyncze jasne końcówki.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać losowego radialnego starburstu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.73. Krzew jagodowy bazowy (`berry_bush`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Blueberries_on_the_branches_of_a_bush.jpg?width=1100" alt="Referencja: Jagody na gałęziach krzewu" width="680">

**Internetowa referencja:** [Jagody na gałęziach krzewu](https://commons.wikimedia.org/wiki/File:Blueberries_on_the_branches_of_a_bush.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zbieralne źródło jedzenia.  
**Docelowa skala:** 0,7–1,5 m  
**Stany gameplayowe:** pełny / zebrany / odrastający / suchy

#### Analiza obrazu referencyjnego

Owoce są przyczepione cienkimi szypułkami do gałązki, występują w skupiskach o różnym stopniu dojrzałości i są częściowo zasłonięte liśćmi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Kanoniczny zbieralny krzew z umiarkowaną liczbą owoców.
 Wiele canes wychodzi z korony korzeniowej; liście i owoce siedzą na rzeczywistych węzłach i końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zielenie w kilku tonach, owoce w 2–3 stadiach dojrzałości, gałęzie ciemniejsze w środku.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Owoce lub liście bez widocznej relacji do pędu dyskwalifikują model.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.74. Krzew jagodowy A (`berry_bush_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Blueberries_on_the_branches_of_a_bush.jpg?width=1100" alt="Referencja: Jagody na gałęziach krzewu" width="680">

**Internetowa referencja:** [Jagody na gałęziach krzewu](https://commons.wikimedia.org/wiki/File:Blueberries_on_the_branches_of_a_bush.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zbieralne źródło jedzenia.  
**Docelowa skala:** 0,7–1,5 m  
**Stany gameplayowe:** pełny / zebrany / odrastający / suchy

#### Analiza obrazu referencyjnego

Owoce są przyczepione cienkimi szypułkami do gałązki, występują w skupiskach o różnym stopniu dojrzałości i są częściowo zasłonięte liśćmi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Zwarty krzew z czerwonymi owocami na zewnętrznych końcach pędów.
 Wiele canes wychodzi z korony korzeniowej; liście i owoce siedzą na rzeczywistych węzłach i końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zielenie w kilku tonach, owoce w 2–3 stadiach dojrzałości, gałęzie ciemniejsze w środku.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Owoce lub liście bez widocznej relacji do pędu dyskwalifikują model.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.75. Krzew jagodowy B (`berry_bush_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Blueberries_on_the_branches_of_a_bush.jpg?width=1100" alt="Referencja: Jagody na gałęziach krzewu" width="680">

**Internetowa referencja:** [Jagody na gałęziach krzewu](https://commons.wikimedia.org/wiki/File:Blueberries_on_the_branches_of_a_bush.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zbieralne źródło jedzenia.  
**Docelowa skala:** 0,7–1,5 m  
**Stany gameplayowe:** pełny / zebrany / odrastający / suchy

#### Analiza obrazu referencyjnego

Owoce są przyczepione cienkimi szypułkami do gałązki, występują w skupiskach o różnym stopniu dojrzałości i są częściowo zasłonięte liśćmi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Rozłożysty krzew z niebieskimi owocami i większymi przerwami w koronie.
 Wiele canes wychodzi z korony korzeniowej; liście i owoce siedzą na rzeczywistych węzłach i końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zielenie w kilku tonach, owoce w 2–3 stadiach dojrzałości, gałęzie ciemniejsze w środku.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Owoce lub liście bez widocznej relacji do pędu dyskwalifikują model.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.76. Krzew jagodowy C (`berry_bush_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Blueberries_on_the_branches_of_a_bush.jpg?width=1100" alt="Referencja: Jagody na gałęziach krzewu" width="680">

**Internetowa referencja:** [Jagody na gałęziach krzewu](https://commons.wikimedia.org/wiki/File:Blueberries_on_the_branches_of_a_bush.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zbieralne źródło jedzenia.  
**Docelowa skala:** 0,7–1,5 m  
**Stany gameplayowe:** pełny / zebrany / odrastający / suchy

#### Analiza obrazu referencyjnego

Owoce są przyczepione cienkimi szypułkami do gałązki, występują w skupiskach o różnym stopniu dojrzałości i są częściowo zasłonięte liśćmi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wyższy, mieszany wariant z owocami na różnych etapach dojrzałości.
 Wiele canes wychodzi z korony korzeniowej; liście i owoce siedzą na rzeczywistych węzłach i końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zielenie w kilku tonach, owoce w 2–3 stadiach dojrzałości, gałęzie ciemniejsze w środku.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Owoce lub liście bez widocznej relacji do pędu dyskwalifikują model.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.77. Krzew jagodowy D (`berry_bush_d`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Blueberries_on_the_branches_of_a_bush.jpg?width=1100" alt="Referencja: Jagody na gałęziach krzewu" width="680">

**Internetowa referencja:** [Jagody na gałęziach krzewu](https://commons.wikimedia.org/wiki/File:Blueberries_on_the_branches_of_a_bush.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zbieralne źródło jedzenia.  
**Docelowa skala:** 0,7–1,5 m  
**Stany gameplayowe:** pełny / zebrany / odrastający / suchy

#### Analiza obrazu referencyjnego

Owoce są przyczepione cienkimi szypułkami do gałązki, występują w skupiskach o różnym stopniu dojrzałości i są częściowo zasłonięte liśćmi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Duży, bardzo owocny krzew będący lokalnym hotspotem zasobów.
 Wiele canes wychodzi z korony korzeniowej; liście i owoce siedzą na rzeczywistych węzłach i końcach.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zielenie w kilku tonach, owoce w 2–3 stadiach dojrzałości, gałęzie ciemniejsze w środku.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Owoce lub liście bez widocznej relacji do pędu dyskwalifikują model.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.78. Trawa i kwiaty bazowe (`grass_or_flower`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Wildflower_meadow.jpg?width=1100" alt="Referencja: Łąka kwiatowa" width="680">

**Internetowa referencja:** [Łąka kwiatowa](https://commons.wikimedia.org/wiki/File:Wildflower_meadow.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Bazowy patch runa.  
**Docelowa skala:** 0,2–0,5 m  
**Stany gameplayowe:** świeży / suchy

#### Analiza obrazu referencyjnego

Kwiaty występują na różnych wysokościach i w nieregularnych skupiskach; dominującą masę nadal tworzą trawy i łodygi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Mieszanka dominujących traw, 3–6 kwiatów i niewielkiej ilości kamieni.
 Patch ma gęstszy środek i luźniejszą krawędź; kwiaty są dodatkiem do traw.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Trawa w kilku tonach, kwiaty ograniczone do czytelnych akcentów.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie tworzyć równej siatki kwiatów na identycznych łodygach.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.79. Kępa wysokiej trawy A (`tall_grass_clump_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Clump_of_grass_%2814267486846%29.png?width=1100" alt="Referencja: Kępa trawy" width="680">

**Internetowa referencja:** [Kępa trawy](https://commons.wikimedia.org/wiki/File:Clump_of_grass_%2814267486846%29.png)  
**Kategoria:** Roślinność  
**Rola w grze:** Runa, maskowanie przejść terenu i zbieralna trawa.  
**Docelowa skala:** 0,45–1,1 m  
**Stany gameplayowe:** zielona / sucha / zebrana

#### Analiza obrazu referencyjnego

Kępa wyrasta z ciasnego środka; źdźbła mają wiele kierunków, długości i łagodnych zgięć, ale wspólną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Średnio gęsta kępa 30–45 źdźbeł, lekko pochylona w jednym kierunku.
 Źdźbła wyrastają z kilku ciasnych punktów, mają taper i delikatne zgięcia.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Mieszanka zieleni i 15–30% suchych tonów.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać płaskich prostokątów o jednakowej wysokości.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.80. Kępa wysokiej trawy B (`tall_grass_clump_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Clump_of_grass_%2814267486846%29.png?width=1100" alt="Referencja: Kępa trawy" width="680">

**Internetowa referencja:** [Kępa trawy](https://commons.wikimedia.org/wiki/File:Clump_of_grass_%2814267486846%29.png)  
**Kategoria:** Roślinność  
**Rola w grze:** Runa, maskowanie przejść terenu i zbieralna trawa.  
**Docelowa skala:** 0,45–1,1 m  
**Stany gameplayowe:** zielona / sucha / zebrana

#### Analiza obrazu referencyjnego

Kępa wyrasta z ciasnego środka; źdźbła mają wiele kierunków, długości i łagodnych zgięć, ale wspólną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wyższa, wilgotna trawa z częścią suchych źdźbeł.
 Źdźbła wyrastają z kilku ciasnych punktów, mają taper i delikatne zgięcia.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Mieszanka zieleni i 15–30% suchych tonów.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać płaskich prostokątów o jednakowej wysokości.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.81. Kępa wysokiej trawy C (`tall_grass_clump_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Clump_of_grass_%2814267486846%29.png?width=1100" alt="Referencja: Kępa trawy" width="680">

**Internetowa referencja:** [Kępa trawy](https://commons.wikimedia.org/wiki/File:Clump_of_grass_%2814267486846%29.png)  
**Kategoria:** Roślinność  
**Rola w grze:** Runa, maskowanie przejść terenu i zbieralna trawa.  
**Docelowa skala:** 0,45–1,1 m  
**Stany gameplayowe:** zielona / sucha / zebrana

#### Analiza obrazu referencyjnego

Kępa wyrasta z ciasnego środka; źdźbła mają wiele kierunków, długości i łagodnych zgięć, ale wspólną podstawę.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Najgęstszy wariant z czytelnym ciemnym środkiem i luźną krawędzią.
 Źdźbła wyrastają z kilku ciasnych punktów, mają taper i delikatne zgięcia.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Mieszanka zieleni i 15–30% suchych tonów.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie używać płaskich prostokątów o jednakowej wysokości.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.82. Łąka kwiatowa A (`wildflower_patch_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Wildflower_meadow.jpg?width=1100" alt="Referencja: Łąka kwiatowa" width="680">

**Internetowa referencja:** [Łąka kwiatowa](https://commons.wikimedia.org/wiki/File:Wildflower_meadow.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Ozdobny patch runa i akcent biomu.  
**Docelowa skala:** 0,25–0,65 m  
**Stany gameplayowe:** kwitnący / przekwitły

#### Analiza obrazu referencyjnego

Kwiaty występują na różnych wysokościach i w nieregularnych skupiskach; dominującą masę nadal tworzą trawy i łodygi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Rzadszy patch z białymi i żółtymi kwiatami.
 Każdy kwiat ma łodygę, kielich/środek i płatki; wysokości i kierunki są zróżnicowane.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Biel, żółć i fiolet w ograniczonej liczbie; trawy spajają całość.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie stosować kul na patykach ani jednakowych rozet.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.83. Łąka kwiatowa B (`wildflower_patch_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Wildflower_meadow.jpg?width=1100" alt="Referencja: Łąka kwiatowa" width="680">

**Internetowa referencja:** [Łąka kwiatowa](https://commons.wikimedia.org/wiki/File:Wildflower_meadow.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Ozdobny patch runa i akcent biomu.  
**Docelowa skala:** 0,25–0,65 m  
**Stany gameplayowe:** kwitnący / przekwitły

#### Analiza obrazu referencyjnego

Kwiaty występują na różnych wysokościach i w nieregularnych skupiskach; dominującą masę nadal tworzą trawy i łodygi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Gęstszy patch z fioletem i większą różnicą wysokości.
 Każdy kwiat ma łodygę, kielich/środek i płatki; wysokości i kierunki są zróżnicowane.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Biel, żółć i fiolet w ograniczonej liczbie; trawy spajają całość.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie stosować kul na patykach ani jednakowych rozet.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.84. Łąka kwiatowa C (`wildflower_patch_c`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Wildflower_meadow.jpg?width=1100" alt="Referencja: Łąka kwiatowa" width="680">

**Internetowa referencja:** [Łąka kwiatowa](https://commons.wikimedia.org/wiki/File:Wildflower_meadow.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Ozdobny patch runa i akcent biomu.  
**Docelowa skala:** 0,25–0,65 m  
**Stany gameplayowe:** kwitnący / przekwitły

#### Analiza obrazu referencyjnego

Kwiaty występują na różnych wysokościach i w nieregularnych skupiskach; dominującą masę nadal tworzą trawy i łodygi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Najbardziej kolorowy wariant używany oszczędnie przy landmarkach.
 Każdy kwiat ma łodygę, kielich/środek i płatki; wysokości i kierunki są zróżnicowane.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Biel, żółć i fiolet w ograniczonej liczbie; trawy spajają całość.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie stosować kul na patykach ani jednakowych rozet.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.85. Trzciny A (`reed_patch_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Fredonian_Pond_Reeds.jpg?width=1100" alt="Referencja: Trzciny nad wodą" width="680">

**Internetowa referencja:** [Trzciny nad wodą](https://commons.wikimedia.org/wiki/File:Fredonian_Pond_Reeds.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Roślinność stawu i terenów podmokłych.  
**Docelowa skala:** 0,8–1,8 m  
**Stany gameplayowe:** zielone / suche / ścięte

#### Analiza obrazu referencyjnego

Trzciny rosną w gęstych pasach, mają pionowe łodygi, długie liście i miejscami suche wiechy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Młode zielone trzciny z niewielką liczbą suchych łodyg.
 Trzciny tworzą pas lub gniazdo, mają pionowe łodygi, długie wąskie liście i niekiedy wiechy.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zieleń, oliwka i suche beże, ciemniejsza podstawa przy wodzie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie generować pojedynczych rurek równomiernie w kole.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.86. Trzciny B (`reed_patch_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Fredonian_Pond_Reeds.jpg?width=1100" alt="Referencja: Trzciny nad wodą" width="680">

**Internetowa referencja:** [Trzciny nad wodą](https://commons.wikimedia.org/wiki/File:Fredonian_Pond_Reeds.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Roślinność stawu i terenów podmokłych.  
**Docelowa skala:** 0,8–1,8 m  
**Stany gameplayowe:** zielone / suche / ścięte

#### Analiza obrazu referencyjnego

Trzciny rosną w gęstych pasach, mają pionowe łodygi, długie liście i miejscami suche wiechy.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wyższy, gęstszy i bardziej suchy patch przy brzegu wody.
 Trzciny tworzą pas lub gniazdo, mają pionowe łodygi, długie wąskie liście i niekiedy wiechy.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zieleń, oliwka i suche beże, ciemniejsza podstawa przy wodzie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie generować pojedynczych rurek równomiernie w kole.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.87. Grzyby A (`mushroom_patch_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Mushrooms_in_a_forest.jpg?width=1100" alt="Referencja: Grzyby na dnie lasu" width="680">

**Internetowa referencja:** [Grzyby na dnie lasu](https://commons.wikimedia.org/wiki/File:Mushrooms_in_a_forest.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Drobny zbieralny zasób i detal dna lasu.  
**Docelowa skala:** 0,08–0,25 m  
**Stany gameplayowe:** świeże / zebrane / zwiędłe

#### Analiza obrazu referencyjnego

Grzyby wyrastają grupami o różnej wielkości; kapelusze mają profil, spód i łodygę, a część okazów jest przechylona.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Małe brązowe jadalne grzyby w ciasnej rodzinnej grupie.
 Kapelusz i trzon muszą stykać się, a grupa ma różne etapy wzrostu i kąty.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kolor kapelusza różny od trzonu; ziemia i mech przy podstawie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie robić idealnych półkul na identycznych walcach.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.88. Grzyby B (`mushroom_patch_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Mushrooms_in_a_forest.jpg?width=1100" alt="Referencja: Grzyby na dnie lasu" width="680">

**Internetowa referencja:** [Grzyby na dnie lasu](https://commons.wikimedia.org/wiki/File:Mushrooms_in_a_forest.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Drobny zbieralny zasób i detal dna lasu.  
**Docelowa skala:** 0,08–0,25 m  
**Stany gameplayowe:** świeże / zebrane / zwiędłe

#### Analiza obrazu referencyjnego

Grzyby wyrastają grupami o różnej wielkości; kapelusze mają profil, spód i łodygę, a część okazów jest przechylona.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Mniejsza grupa o wyraźniejszym kolorze, traktowana jako inny gatunek.
 Kapelusz i trzon muszą stykać się, a grupa ma różne etapy wzrostu i kąty.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kolor kapelusza różny od trzonu; ziemia i mech przy podstawie.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie robić idealnych półkul na identycznych walcach.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.89. Zioła A (`herb_patch_a`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/A_common_herb_leaf.jpg?width=1100" alt="Referencja: Roślina zielna" width="680">

**Internetowa referencja:** [Roślina zielna](https://commons.wikimedia.org/wiki/File:A_common_herb_leaf.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zbieralne zioła lub składnik craftingu.  
**Docelowa skala:** 0,15–0,45 m  
**Stany gameplayowe:** pełne / zebrane / odrastające

#### Analiza obrazu referencyjnego

Liście wyrastają z węzłów lub rozety i mają powtarzalny, ale nie identyczny kształt; ważniejsza jest botanika niż liczba płaszczyzn.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Niska rozeta szerszych liści z czytelnym środkiem wzrostu.
 Liście mają powtarzalną botaniczną logikę: rozeta, pary lub okółki; każdy liść łączy się z łodygą.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zieleń świeża, czasem jaśniejszy nerw i ciemniejsza podstawa.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie rozrzucać prostokątnych kart bez węzłów wzrostu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.90. Zioła B (`herb_patch_b`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/A_common_herb_leaf.jpg?width=1100" alt="Referencja: Roślina zielna" width="680">

**Internetowa referencja:** [Roślina zielna](https://commons.wikimedia.org/wiki/File:A_common_herb_leaf.jpg)  
**Kategoria:** Roślinność  
**Rola w grze:** Zbieralne zioła lub składnik craftingu.  
**Docelowa skala:** 0,15–0,45 m  
**Stany gameplayowe:** pełne / zebrane / odrastające

#### Analiza obrazu referencyjnego

Liście wyrastają z węzłów lub rozety i mają powtarzalny, ale nie identyczny kształt; ważniejsza jest botanika niż liczba płaszczyzn.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Wyższe pędy z parami liści i bardziej aromatycznym charakterem.
 Liście mają powtarzalną botaniczną logikę: rozeta, pary lub okółki; każdy liść łączy się z łodygą.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Zieleń świeża, czasem jaśniejszy nerw i ciemniejsza podstawa.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.

#### Błędy dyskwalifikujące

- Nie rozrzucać prostokątnych kart bez węzłów wzrostu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.91. Mała zdobycz (`small_prey`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Rabbit_side_view.JPG?width=1100" alt="Referencja: Królik / zając z boku" width="680">

**Internetowa referencja:** [Królik / zając z boku](https://commons.wikimedia.org/wiki/File:Rabbit_side_view.JPG)  
**Kategoria:** Stworzenie  
**Rola w grze:** Podstawowy mały roślinożerca i źródło mięsa.  
**Docelowa skala:** 0,45–0,75 m długości  
**Stany gameplayowe:** żywy / martwy / zraniony

#### Analiza obrazu referencyjnego

Mały ssak ma mocny zad i tylne kończyny, krótsze przednie łapy, małą głowę, długie uszy i zaokrągloną linię grzbietu.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Zając/królik: silny zad, duże zgięte tylne kończyny, krótkie przednie łapy, długie uszy i mały ogon.
 Masa skupia się w zadzie; tylne kończyny są zgięte i znacznie mocniejsze od przednich.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Futro o miękkim roughness, jaśniejszy brzuch, ciemniejsze końcówki uszu i oko jako mały kontrast.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.
6. **Wymaganie specjalne:** Wymaga jednej spójnej siatki bazowej i riggu; nie budować z osobnych walców i kul.

#### Błędy dyskwalifikujące

- Osobne kule na ciało i walce na nogi bez przejścia form są niedopuszczalne.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.92. Grazer (`grazer`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Deer_in_field_%28side_view%29_2.jpg?width=1100" alt="Referencja: Jeleń / sarna z boku" width="680">

**Internetowa referencja:** [Jeleń / sarna z boku](https://commons.wikimedia.org/wiki/File:Deer_in_field_%28side_view%29_2.jpg)  
**Kategoria:** Stworzenie  
**Rola w grze:** Średni roślinożerca ekosystemu.  
**Docelowa skala:** 1,2–1,8 m długości  
**Stany gameplayowe:** żywy / martwy / głodny / zraniony

#### Analiza obrazu referencyjnego

Grazer ma długie smukłe nogi, głęboką klatkę piersiową, węższą talię, szyję i wydłużony pysk.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Sarna/koza/antylopa: długa noga, głęboka klatka, węższy brzuch, smukła szyja, mała głowa i rozdzielone kopyto.
 Kręgosłup, klatka, miednica i nogi tworzą jedną anatomiczną całość; stawy muszą zginać się poprawnie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Naturalne brązy/szarości, jaśniejszy spód i pysk, ciemne kopyta.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.
6. **Wymaganie specjalne:** Wymaga poprawnego quadruped base mesh, riggu i blendshape/parametrów wariantów.

#### Błędy dyskwalifikujące

- Nie tworzyć tułowia-kuli na czterech cylindrach.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.93. Varnak (`varnak`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Wolf_%289854d05a-69d1-4bba-abd7-c704a29e2017%29.jpg?width=1100" alt="Referencja: Wilk z boku" width="680">

**Internetowa referencja:** [Wilk z boku](https://commons.wikimedia.org/wiki/File:Wolf_%289854d05a-69d1-4bba-abd7-c704a29e2017%29.jpg)  
**Kategoria:** Stworzenie  
**Rola w grze:** Główny drapieżnik świata.  
**Docelowa skala:** 1,4–2,1 m długości  
**Stany gameplayowe:** spokojny / polujący / ranny / martwy

#### Analiza obrazu referencyjnego

Sylwetkę drapieżnika budują głęboka klatka, mocne barki, węższy brzuch, wydłużony pysk i ogon będący przedłużeniem linii grzbietu.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Fantastyczny drapieżnik oparty na wilku/hienie: ciężkie barki, głęboka klatka, opadająca linia grzbietu, długi pysk i mocne przednie kończyny.
 Fantastyczność wynika z proporcji i detalu, ale fundamentem jest wiarygodny drapieżny czworonóg.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Ciemne futro z cieplejszymi barkami, jaśniejsze blizny/kość, ograniczony kontrast fantasy.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.
6. **Wymaganie specjalne:** Fantastyczne cechy dopiero po zbudowaniu wiarygodnej anatomii bazowej; kolce i kły nie mogą zastępować sylwetki.

#### Błędy dyskwalifikujące

- Nie dodawać kolców do niepoprawnej anatomii w celu ukrycia problemu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.94. Wielkie stare drzewo (`old_tree_landmark`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Auchenskeith_tree_roots.jpg?width=1100" alt="Referencja: Stare drzewo z korzeniami i dziuplą" width="680">

**Internetowa referencja:** [Stare drzewo z korzeniami i dziuplą](https://commons.wikimedia.org/wiki/File:Auchenskeith_tree_roots.jpg)  
**Kategoria:** Landmark  
**Rola w grze:** Główny naturalny punkt orientacyjny.  
**Docelowa skala:** 8–14 m wysokości  
**Stany gameplayowe:** żywe / skażone / uszkodzone

#### Analiza obrazu referencyjnego

Landmark starego drzewa wymaga ogromnego odziomka, widocznych korzeni, deformacji pnia i miejscowej pustki/ubytku.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Monumentalny odziomek, korzenie jak przypory, dziupla, ślady piorunów, martwe konary i żywa korona utrzymywana przez część pnia.
 Odziomek i korzenie dominują nad sylwetką człowieka; część pnia jest pusta, ale konstrukcja nadal wygląda stabilnie.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Stara ciemna kora, jasne drewno ubytków, mech i porosty, korona wielotonowa.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.
6. **Wymaganie specjalne:** Hero asset: osobne LOD-y, uproszczony collider korzeni, punkty na VFX i ewentualne interakcje.

#### Błędy dyskwalifikujące

- Nie powiększać zwykłego drzewa bez deformacji wieku.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.95. Ruiny (`ruins_landmark`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Kamoa_Point_overgrown_ruins.jpg?width=1100" alt="Referencja: Zarośnięte kamienne ruiny" width="680">

**Internetowa referencja:** [Zarośnięte kamienne ruiny](https://commons.wikimedia.org/wiki/File:Kamoa_Point_overgrown_ruins.jpg)  
**Kategoria:** Landmark  
**Rola w grze:** Punkt orientacyjny i miejsce eksploracji.  
**Docelowa skala:** 8–18 m średnicy  
**Stany gameplayowe:** nienaruszone fragmenty / splądrowane / porośnięte

#### Analiza obrazu referencyjnego

Ruiny są czytelne dzięki fragmentom ścian i rytmowi dawnych konstrukcji, ale ich krawędzie są zawalone, porośnięte i otoczone gruzem.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Fragmenty dawnych ścian tworzą czytelny plan, ale są zawalone, przerośnięte korzeniami, porostem i rozsypanym gruzem.
 Ściany i filary pokazują dawny porządek, a gruz pokazuje sposób zawalenia. Rośliny wrastają w szczeliny.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kamień chłodny, mech zielony, ziemia ciemna; krawędzie zużyte i niejednorodne.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.
6. **Wymaganie specjalne:** Modułowy zestaw ścian, filarów, bloków i gruzu; collidery uproszczone, ale zgodne z przejściami.

#### Błędy dyskwalifikujące

- Nie układać przypadkowych sześcianów bez czytelnego planu ruin.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.96. Staw (`pond_landmark`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/The_Reed_Pond_-_geograph.org.uk_-_441917.jpg?width=1100" alt="Referencja: Staw z trzcinami" width="680">

**Internetowa referencja:** [Staw z trzcinami](https://commons.wikimedia.org/wiki/File:The_Reed_Pond_-_geograph.org.uk_-_441917.jpg)  
**Kategoria:** Landmark  
**Rola w grze:** Źródło wody, landmark i ograniczenie spawnu.  
**Docelowa skala:** 5–14 m średnicy  
**Stany gameplayowe:** pełny / niski poziom / zarośnięty

#### Analiza obrazu referencyjnego

Naturalny staw ma nieregularną linię brzegu, strefę błota, kamienie, rośliny przybrzeżne i wodę osadzoną poniżej terenu.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Nieregularna niecka wodna poniżej terenu, błotnisty brzeg, trzciny w skupiskach, kamienie i strefa płytkiej wody.
 Brzeg ma strefy: sucha ziemia, błoto, płytka woda i głębszy środek. Rośliny grupują się według wilgotności.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Woda chłodna i lekko zielona, błoto ciemne, trzciny żółtozielone.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.
6. **Wymaganie specjalne:** Woda jako osobny mesh/shader; maska spawn-blocking dokładnie zgodna z linią brzegu.

#### Błędy dyskwalifikujące

- Nie używać płaskiego niebieskiego dysku na poziomie gruntu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.97. Opuszczony obóz (`camp_landmark`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Bushcraft_Shelter_%2851648427088%29.jpg?width=1100" alt="Referencja: Schronienie bushcraftowe" width="680">

**Internetowa referencja:** [Schronienie bushcraftowe](https://commons.wikimedia.org/wiki/File:Bushcraft_Shelter_%2851648427088%29.jpg)  
**Kategoria:** Landmark  
**Rola w grze:** Punkt narracyjny i miejsce z lootem.  
**Docelowa skala:** 6–12 m średnicy  
**Stany gameplayowe:** świeży / opuszczony / splądrowany

#### Analiza obrazu referencyjnego

Konstrukcja ma ridgepole, podpory i gęste poszycie z naturalnego materiału. Warstwy opierają się na stelażu i schodzą blisko ziemi.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Zużyte palenisko, częściowo zapadnięte schronienie, skrzynia lub resztki stojaka, siedziska z bali i ślady dawnego użytkowania.
 Landmark jest kompozycją logicznie używanych obiektów: ognisko, schronienie, składowanie, siedzenie i ślady ruchu.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Spójna paleta drewna i ziemi, lokalne czernie paleniska i wypłowiałe skóry.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.
6. **Wymaganie specjalne:** Kompozycja z wielu prefabów, ale z jednym wspólnym pivotem landmarku i prostym colliderem obszaru.

#### Błędy dyskwalifikujące

- Nie ustawiać prefabów w równym katalogowym szeregu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

### 4.98. Wejście do jaskini (`cave_landmark`)

<img src="https://commons.wikimedia.org/wiki/Special:Redirect/file/Cave_Opening_in_Mossy_Rock.jpg?width=1100" alt="Referencja: Naturalne wejście do jaskini" width="680">

**Internetowa referencja:** [Naturalne wejście do jaskini](https://commons.wikimedia.org/wiki/File:Cave_Opening_in_Mossy_Rock.jpg)  
**Kategoria:** Landmark  
**Rola w grze:** Punkt orientacyjny i przyszłe wejście do lokacji.  
**Docelowa skala:** 4–10 m szerokości  
**Stany gameplayowe:** zamknięta / otwarta / zawalona

#### Analiza obrazu referencyjnego

Wejście jest ciemną, nieregularną przerwą między masami skały; mech i gruz wzmacniają skalę oraz zakotwiczenie w terenie.
 Obraz należy analizować przede wszystkim pod kątem relacji dużych mas, miejsc połączeń oraz cech rozpoznawczych; drobny szum fotograficzny nie powinien być kopiowany jako geometria.

#### Docelowy opis graficzny

Ciemna nieregularna szczelina otoczona nawarstwioną skałą, gruzem, mchem i korzeniami; nie pojedynczy szary głaz.
 Ciemna pustka musi być osadzona za przednim obrysem skał; obramowanie ma różne głębokości.
 Model ma wyglądać na wykonany, wyrośnięty lub zużyty w świecie Apex Shift, a nie wyjęty z katalogu nowoczesnych przedmiotów. Krawędzie i powierzchnie powinny mieć kontrolowaną nieregularność, która pozostaje czytelna w ujęciu izometrycznym.

#### Materiały, kolor i powierzchnia

Kamień chłodny, głębia niemal czarna, mech i wilgoć przy wejściu.
 Hand-painted shading powinien wzmacniać formę: jaśniejsze górne płaszczyzny, chłodniejsze lub ciemniejsze zagłębienia i delikatne akcenty zużycia w miejscach kontaktu. Nie stosować jednolitego koloru na wszystkich częściach tej samej rodziny.

#### Czytelność z kamery i kompozycja

Najważniejsza cecha identyfikacyjna powinna być widoczna z kąta 35–45° nad horyzontem oraz z obrotu zbliżonego do kamery gry. Asset należy wyrenderować co najmniej z czterech stron, w clay renderze i z materiałem. Drobne elementy istotne dla rozpoznania mogą być pogrubione o około 10–20%, ale nie mogą zmieniać realnej mechaniki lub anatomii.

#### Zasady dla generatora

1. Zbudować główny szkielet lub bryłę z krzywych/base mesha, a dopiero potem dodać elementy drugorzędne.
2. Sprawdzać odległość i styczność wszystkich elementów podporządkowanych; części bez parenta lub bez logicznego kontaktu zgłaszać jako błąd.
3. Warianty randomizować w ograniczonych zakresach opisanych dla rodziny, zachowując wspólną tożsamość prefabów.
4. Automatycznie generować pivot przy podstawie, prosty collider gameplayowy, LOD-y oraz miniaturę w tej samej skali prezentacyjnej.
5. Porównać finalny render z internetową referencją i lokalną grafiką koncepcyjną przed dopuszczeniem do eksportu.
6. **Wymaganie specjalne:** Wymaga osobnego ciemnego interior card/mesh, skał modularnych i strefy blokującej nawigację.

#### Błędy dyskwalifikujące

- Nie przedstawiać jaskini jako jednego głazu bez otworu.
- Elementy lewitujące, przenikające się w sposób niekonstrukcyjny lub nieposiadające widocznego punktu mocowania.
- Sylwetka, której nie da się rozpoznać bez nazwy pliku lub koloru materiału.
- Nadmierna liczba prymitywów użyta zamiast jednej poprawnie zaprojektowanej formy.

## 5. Standard techniczny dla Unity

- **Pivot:** na styku z podłożem; dla narzędzi dodatkowy grip socket zgodny z dłonią postaci.
- **Skala:** 1 jednostka = 1 metr; wszystkie warianty sprawdzane przy modelu gracza.
- **LOD:** itemy 2 LOD-y, krzewy 3, drzewa i landmarki 3–4, stworzenia 3.
- **Collidery:** uproszczone i stabilne; nie kopiować pełnej siatki renderującej. Rośliny najczęściej capsule/compound; landmarki modułowe box/convex.
- **Materiały:** atlasowane w obrębie rodzin; leaf cards z alpha clippingiem tylko tam, gdzie realnie poprawiają krawędź liścia/igieł.
- **Warianty stanu:** osobne prefaby lub deterministyczne grupy obiektów, bez ręcznego wyłączania przypadkowych childów w scenie.
- **Preview:** 1024×1024, ten sam focal length, neutralne światło, clay + final material + wireframe statistics.

## 6. Źródła internetowych referencji

Pełna tabela źródeł, nazw plików, adresów i not licencyjnych znajduje się w `reference_sources.csv`. Mapowanie każdego z 98 assetów do referencji znajduje się w `asset_reference_map.csv`.
