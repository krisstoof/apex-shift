# Analiza problemu namiotu i korekta v10.1

## Przyczyna problemu w v10

Najważniejszy błąd był matematyczny. Panel poszycia jest tworzony jako płaszczyzna w lokalnej osi X–Z. Aby położyć go na połaci dachu A-frame, należy obrócić go wokół osi X. Generator v10 obracał go wokół osi Y.

Skutek:

- panel przechylał się wzdłuż długości namiotu,
- kolejne elementy tworzyły chaotyczną ścianę,
- sylwetka nie przypominała dwóch równych połaci,
- podpory frontowe były prawie pionowe,
- wejście było przypadkową przerwą między panelami zamiast gable opening.

## Docelowa geometria

Przekrój poprzeczny:

```text
          ridgepole
             /\
            /  \
           /    \
          /      \
       eave      eave
```

Długość biegnie od przedniej do tylnej ramy A. Poszycie znajduje się wyłącznie na dwóch połaciach. Frontowa ściana nie jest zamknięta, dzięki czemu powstaje niski trójkątny otwór.

## Zastosowane korekty

1. Stopy każdej ramy rozsunięto do `y = ±0.82 m`.
2. Wierzchołki ram zbiegają się przy `z = 1.46 m`.
3. Dodano 6 par krokwi.
4. Dodano belki okapowe i płatwie pod poszyciem.
5. Panele dachu mają obrót `rotation_x = ±roof_angle`.
6. Każda połać ma 24 panele: 4 rzędy × 6 kolumn.
7. Panele zachodzą na siebie od okapu do kalenicy.
8. Tył jest zamknięty trójkątnym panelem.
9. Front pozostaje otwarty.
10. Próg i posłanie znajdują się wewnątrz, a nie przed namiotem.

## Wynik kontroli

Symulator wykrywa:

- 4 elementy głównej ramy,
- 1 ridgepole,
- 12 krokwi,
- 48 paneli poszycia,
- 4 płatwie,
- 2 belki okapowe,
- 1 tylne zamknięcie,
- 1 posłanie.

Automatyczny QA proxy: **10/10**. Jest to kontrola konstrukcji i sylwetki, nie ocena finalnych materiałów w Blenderze.
