# Import paczki ikon do Unity

Jeżeli `.unitypackage` powoduje błąd `NullReferenceException` w oknie importu Unity, użyj wariantu ZIP.

## Najbezpieczniejszy import

1. Rozpakuj ZIP.
2. Skopiuj folder `Assets/ApexShift2D` do folderu `Assets` w swoim projekcie Unity.
3. Poczekaj, aż Unity przeindeksuje assety.
4. Ikony znajdziesz w:
   - `Assets/ApexShift2D/Art/Icons/Resources`
   - `Assets/ApexShift2D/Art/Icons/Items`
   - `Assets/ApexShift2D/Art/Icons/Tools`

## Import .unitypackage

Wersja `v0_2_safe.unitypackage` zawiera tylko pliki assetów, bez osobnych wpisów folderów.
To ogranicza ryzyko błędu w `PackageImportTreeView`.
