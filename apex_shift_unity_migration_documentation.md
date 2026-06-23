# Apex Shift — dokumentacja gry i plan migracji logiki do Unity

**Status dokumentu:** wersja robocza v1  
**Data:** 2026-06-22  
**Źródła:** branch `develop` repozytorium `krisstoof/apex-shift-2d`, istniejące dokumenty techniczne projektu, aktualny kod Godot oraz dotychczasowe ustalenia projektowe z rozmów.  
**Cel:** zachować dorobek prototypu Godot i przenieść sens gry, systemy, balans, dane i reguły do Unity bez zaczynania całkowicie od zera.

---

## 1. Najważniejsze założenie

Apex Shift nie powinien być traktowany jako projekt do prostego przepisania 1:1 z Godota do Unity. Obecny prototyp jest wartościowy, ponieważ zawiera:

- sprawdzony kierunek gameplayu,
- strukturę systemów survivalowych,
- zalążek żywego świata,
- system zasobów, jedzenia i ekosystemu,
- Varnaki jako główne zagrożenie,
- bazową pętlę eksploracji, zbierania, craftingu, przetrwania i presji ekosystemu,
- pierwsze próby oddzielenia logiki core od adapterów Godota.

Jednocześnie obecny kod zawiera dług techniczny, bugi i niektóre niespójności. Dlatego migracja do Unity powinna polegać na przeniesieniu **modelu domenowego i reguł**, a nie na wiernym kopiowaniu obecnej struktury scen, węzłów i lookupów Godota.

Najważniejsza decyzja migracyjna:

> Godotowy prototyp jest źródłem intencji, zachowań, balansu, danych i testów. Unity powinno dostać czystszą architekturę: Core Gameplay Logic + Unity Runtime Adapters + Presentation/Rendering.

---

## 2. Wizja gry

### 2.1. Główna fantazja

Gracz zaczyna w świecie, w którym człowiek jest na szczycie łańcucha pokarmowego, ale z czasem ta pozycja przestaje być oczywista. Zwierzęta, zwłaszcza Varnaki, adaptują się, uczą się zachowania gracza i zaczynają odpowiadać na jego dominację.

Motyw przewodni:

> **Apex Shift** — przesunięcie pozycji szczytowej.  
> Człowiek nie zawsze pozostaje najgroźniejszym drapieżnikiem.

To nie ma być zwykły survival, w którym świat jest tylko planszą z zasobami. Świat ma sprawiać wrażenie żywego organizmu, który reaguje na działania gracza.

### 2.2. Filary projektu

1. **Żywy świat**
   - Biomy mają własny stan.
   - Roślinność, populacje i presja drapieżników zmieniają się w czasie.
   - Działania gracza mają lokalne konsekwencje.

2. **Przetrwanie przez adaptację**
   - Gracz zdobywa zasoby, craftuje, buduje i walczy.
   - Same narzędzia nie wystarczą, bo zagrożenia uczą się i zmieniają.
   - Przetrwanie wymaga obserwowania świata, a nie tylko farmienia surowców.

3. **Utrata dominacji**
   - Na początku gracz ma przewagę.
   - Z czasem Varnaki i inne stworzenia mogą stać się coraz bardziej niebezpieczne.
   - Świat ma przechodzić od „jestem łowcą” do „jestem obserwowany, ścigany i wypychany”.

4. **Czytelna symulacja, nie pełna biologia**
   - Model ekosystemu nie musi być naukowo dokładny.
   - Ma być zrozumiały dla gracza.
   - Gracz powinien widzieć, że wycinanie roślin, polowanie i presja drapieżników zmieniają biom.

5. **Modularność pod przyszłą wersję 3D**
   - Logika nie powinna być zależna od 2D.
   - Docelowo Unity może być wersją 3D albo hybrydową, ale systemy powinny być możliwe do testowania bez renderingu.
   - Core ma działać jako czysta logika C#.

---

## 3. Obecny stan prototypu

### 3.1. Typ gry

Obecny projekt to top-down survival prototype w Godot. Gracz eksploruje biome-based world, zbiera zasoby, craftuje narzędzia i struktury, zarządza zdrowiem, głodem, staminą i odpoczynkiem, unika lub zwalcza Varnaki oraz obserwuje ekosystem składający się z roślinności, małych ofiar, grazerów, drapieżników, wody, wzgórz i presji biomów.

### 3.2. Obecna pętla gry

1. Start z menu.
2. Nowa gra albo kontynuacja zapisu.
3. Generacja świata.
4. Rozmieszczenie gracza w bezpiecznej pozycji startowej.
5. Eksploracja biomów.
6. Zbieranie zasobów:
   - drewno,
   - kamień,
   - włókno,
   - mięso,
   - kości,
   - jagody / food resources,
   - trawa / roślinność jadalna dla stworzeń.
7. Crafting:
   - ognisko,
   - włócznia,
   - pochodnia,
   - łuk,
   - pułapka,
   - ściana,
   - skrzynia,
   - namiot.
8. Przetrwanie:
   - zdrowie,
   - głód,
   - stamina,
   - odpoczynek,
   - noc,
   - ognisko,
   - pochodnia.
9. Interakcja z żywym światem:
   - roślinność może znikać i odrastać,
   - SmallPrey jedzą rośliny,
   - Grazery jedzą rośliny, padlinę albo małe ofiary, jeśli presja głodu jest duża,
   - Varnaki polują i adaptują się.
10. Save/load.
11. Debug panel i benchmark służą do walidacji.

---

## 4. Obecne wejścia projektu Godot

### 4.1. Główne pliki

| Obszar | Pliki |
|---|---|
| Konfiguracja Godot | `project.godot` |
| Scene startowa | `scenes/ui/start_menu.tscn` |
| Runtime scene | `scenes/main.tscn` |
| Autoloady | `EventBus`, `GameSession`, `GraphicsSettings` |
| Orkiestracja | `scripts/systems/game_manager.gd` |
| Świat | `scripts/world/world.gd`, `scripts/world/world_config.gd` |
| Zasoby | `scripts/world/resource_node.gd`, `scripts/core/resources/*` |
| Player | `scripts/player/player.gd`, `scripts/player/player_stats.gd` |
| Inventory | `scripts/player/inventory.gd`, `scripts/core/inventory/inventory_state.gd` |
| Ekosystem | `scripts/systems/ecosystem_director.gd`, `scripts/core/ecosystem/ecosystem_simulation.gd` |
| Stworzenia | `scripts/creatures/small_prey.gd`, `scripts/creatures/grazer.gd`, `scripts/creatures/varnak.gd` |
| Dieta/głód stworzeń | `scripts/creatures/hunger_diet.gd`, `scripts/core/survival/hunger_diet.gd` |
| Save/load | `scripts/systems/save_system.gd`, `scripts/core/save/*` |
| UI | `scripts/ui/hud.gd`, `scripts/ui/minimap.gd`, `scripts/ui/map_screen.gd`, `scripts/ui/debug_panel.gd` |
| Dane | `data/items.json`, `data/recipes.json`, `data/species/*.json` |
| Testy | `tests/unit`, `tests/integration`, `tests/regression` |

### 4.2. Autoloady

Obecnie projekt używa trzech globalnych singletonów:

| Autoload | Rola |
|---|---|
| `EventBus` | Wysyłanie eventów gry i krótkich komunikatów UI. |
| `GameSession` | Przechowanie intencji startu gry: nowa gra, kontynuacja, seed, bootstrap save. |
| `GraphicsSettings` | Ustawienia runtime dla grafiki, debug overlayów, cullingu i wydajności. |

W Unity odpowiednikiem nie powinny być globalne singletony bez kontroli. Lepiej wprowadzić jeden `GameCompositionRoot`, który tworzy i przekazuje zależności.

---

## 5. Obecna topologia runtime

Scena runtime ma obecnie logicznie taki układ:

```text
Main
├── EvolutionDirector
├── DayNightSystem
├── EcosystemDirector
├── SaveSystem
├── World
├── Player
├── HUD
└── GameManager
```

Dodatkowo poza sceną działają autoloady:

```text
/root/EventBus
/root/GameSession
/root/GraphicsSettings
```

### 5.1. Problem obecnej topologii

W Godot część zależności idzie przez:

- bezpośrednie referencje node,
- sibling lookups,
- `get_tree().current_scene`,
- grupy,
- autoloady,
- cache w UI,
- rejestry i query services,
- event bus.

To jest normalne w prototypie, ale w Unity nie warto powtarzać tego wzorca. Unity powinno dostać jawne zależności wstrzyknięte na starcie.

---

## 6. Właściciele stanu

| Domena | Obecny właściciel w Godot | Docelowy właściciel w Unity |
|---|---|---|
| Intencja startu gry | `GameSession` | `GameSessionState` + `GameBootstrapper` |
| Eventy i komunikaty | `EventBus` | `IGameEventBus` |
| Layout świata | `World` | `WorldState` + `WorldGenerationService` |
| Topografia | `WorldTopography`, `WorldConfig` | `TopographyService` |
| Widoczne zasoby | `World` + `ResourceNode` | `ResourceRepository` + Unity `ResourceView` |
| Regrowth zasobów | `ResourceNode` + core systems | `ResourceRegrowthSystem` |
| Ekosystem abstrakcyjny | `EcosystemDirector` | `EcosystemSimulation` + `BiomeStateRepository` |
| Varnak profile | `EvolutionDirector` | `EvolutionSystem` |
| Player runtime | `Player` | `PlayerController` + `PlayerState` |
| Survival stats | `PlayerStats` | `SurvivalState` + `SurvivalSystem` |
| Inventory | `InventoryState` | `InventoryState` |
| Crafting | `GameBalance`, recipes, Player methods | `CraftingSystem` |
| Save/load | `SaveSystem` + collector/restorer | `SaveService` + serializable DTO |
| HUD/map/debug | UI nodes | ViewModels + Unity UI views |
| Rendering/culling | `World`, render controllers | `WorldRenderer`, `VisibilitySystem`, `SpatialIndex` |

---

## 7. Proponowana architektura Unity

### 7.1. Warstwy

```text
ApexShift.Core
├── Config
├── World
├── Resources
├── Inventory
├── Crafting
├── Survival
├── Creatures
├── Ecosystem
├── Evolution
├── Save
└── Common

ApexShift.UnityRuntime
├── Bootstrap
├── SceneAdapters
├── Views
├── PrefabFactories
├── Pools
├── Input
├── PhysicsAdapters
└── SaveStorage

ApexShift.Presentation
├── HUD
├── Map
├── Minimap
├── DebugPanel
└── ViewModels

ApexShift.Tests
├── EditMode
└── PlayMode
```

### 7.2. Zasada migracji

Core nie zna Unity.

Core nie powinien używać:

- `MonoBehaviour`,
- `GameObject`,
- `Transform`,
- `Time.deltaTime`,
- `UnityEngine.Random`,
- `Physics`,
- `SceneManager`,
- `Resources.Load`.

Core powinien używać własnych lub prostych typów:

```csharp
Vector2D / Vector3D
RectD
IRandom
IGameClock
IEventSink
IWorldQuery
ISpatialIndex
```

Unity runtime tylko adaptuje core do silnika.

### 7.3. Najważniejsze interfejsy

```csharp
public interface IGameEventBus
{
    void Publish<TEvent>(TEvent gameEvent);
    void Subscribe<TEvent>(Action<TEvent> handler);
}

public interface IWorldQuery
{
    string GetBiomeId(Vector2D position);
    TerrainZone GetTerrainZone(Vector2D position);
    bool IsWater(Vector2D position);
    bool IsInsideWorld(Vector2D position);
    IEnumerable<ResourceState> FindResourcesNear(Vector2D position, float radius);
    IEnumerable<CreatureState> FindCreaturesNear(Vector2D position, float radius);
}

public interface ISpatialIndex<T>
{
    void Add(T item, Vector2D position);
    void Remove(T item);
    void Update(T item, Vector2D position);
    IReadOnlyList<T> QueryCircle(Vector2D center, float radius);
    IReadOnlyList<T> QueryRect(RectD rect);
}

public interface ISaveStorage
{
    void SaveText(string key, string json);
    bool TryLoadText(string key, out string json);
}
```

---

## 8. System świata

### 8.1. Obecne założenia świata

Świat jest większy niż pierwotny prototyp. Aktualny kod `WorldConfig` używa:

- `BASELINE_WORLD_SCALE = 3.0`,
- `WORLD_SCALE = 6.0`,
- `BASE_WORLD_RECT = Rect2(-1680, -1040, 3360, 2080)`,
- `WORLD_RECT = BASE_WORLD_RECT * WORLD_SCALE`.

Uwaga: część starszej dokumentacji i `GameBalance.LIVING_WORLD` nadal wskazuje `world_scale = 2.2`. Przy migracji do Unity trzeba traktować `WorldConfig` jako aktualniejsze źródło prawdy albo jawnie zdecydować nową skalę świata. Nie wolno bezrefleksyjnie mieszać tych wartości.

### 8.2. Biomy

Obecnie istnieje pięć głównych biomów:

| Biome ID | Nazwa | Założenie projektowe | Dominująca roślinność |
|---|---|---|---|
| `westwood` | Westwood | Gęsty, ciemniejszy las | głównie iglaste drzewa |
| `stoneback_ridge` | Stoneback Ridge | Skalisty teren, wzgórza | pojedyncze iglaste drzewa, skały |
| `hearth_meadow` | Hearth Meadow | Bezpieczniejsza łąka startowa | pojedyncze liściaste drzewa, trawy |
| `south_thicket` | South Thicket | Gęste południowe zarośla | głównie liściaste drzewa i krzaki |
| `redfang_wilds` | Redfang Wilds | Niebezpieczny, suchszy biom | suche drzewa, suche krzewy, zagrożenie |

Ustalenia z rozmów, które trzeba utrzymać:

- Westwood ma być głównie gęstym lasem iglastym.
- South Thicket ma mieć przewagę lasów liściastych.
- Hearth Meadow ma mieć pojedyncze liściaste drzewa.
- Stoneback Ridge ma mieć pojedyncze iglaste drzewa.
- Redfang Wilds ma mieć pojedyncze suche drzewa i suche krzewy.
- Każdy biom powinien mieć procentowe profile spawnu roślinności, a nie sztywne listy ręczne.
- Profile biomów powinny być konfigurowalne danymi.

### 8.3. Terrain zones

WorldConfig wylicza wysokość terenu i dzieli świat na:

```text
deep_ocean
shallow_water
shore
land
highland
```

W Unity powinno to być częścią `TopographyService`.

Proponowana struktura:

```csharp
public enum TerrainZone
{
    DeepOcean,
    ShallowWater,
    Shore,
    Land,
    Highland
}

public sealed class TerrainSample
{
    public Vector2D Position;
    public float Height;
    public TerrainZone Zone;
    public string BiomeId;
    public float MovementMultiplier;
}
```

### 8.4. Ponds i hills

W kodzie istnieje starsza lista `LANDMARKS` z pozycjami wzgórz i stawów, ale aktualna logika oznacza, że ponds i hills są generowane przez `WorldTopography`, a nie przez klasyczne landmark POI. W migracji trzeba podjąć decyzję:

**Opcja A — zachować topografię proceduralną jako źródło prawdy**
- Ponds/hills wynikają z seedu i topography rules.
- Save zapisuje seed + layout.
- Mapa i minimapa odczytują topography layer.

**Opcja B — jawne POI jako data-driven landmarks**
- Ponds/hills są encjami świata.
- Mają ID, typ, pozycję, radius, biome_id.
- Łatwiej debugować i balansować.
- Mniej naturalne, jeśli świat ma być proceduralny.

Rekomendacja do Unity:

> Użyj modelu hybrydowego: `WorldLayout` zawiera wygenerowane ponds/hills jako jawne struktury danych. Są generowane proceduralnie, ale po wygenerowaniu stają się serializowalnymi POI.

Przykład:

```csharp
public sealed class WorldLayout
{
    public int Seed;
    public IReadOnlyList<BiomePolygon> Biomes;
    public IReadOnlyList<PondDescriptor> Ponds;
    public IReadOnlyList<HillDescriptor> Hills;
    public IReadOnlyList<ResourceSpawnPoint> InitialResources;
}
```

---

## 9. World boot sequence

Obecny boot świata w Godot robi wiele rzeczy sekwencyjnie i rozkłada je na klatki. To pomaga przy stutteringu startowym, ale jednocześnie boot stał się częścią kontraktu systemu.

Docelowy boot w Unity:

```text
GameBootstrapper
1. Load GameSession intent
2. Load or create WorldSeed
3. Generate or restore WorldLayout
4. Build WorldQueryService
5. Build SpatialIndex
6. Spawn Player at safe start
7. Spawn resource views from ResourceState
8. Spawn visible creature views from CreatureState / ecosystem demand
9. Bind HUD, minimap, map, debug view models
10. Emit GameStarted
```

### 9.1. Zasada

W Unity boot powinien być jawnie podzielony na etapy:

```csharp
public enum BootStage
{
    Session,
    WorldLayout,
    Topography,
    Resources,
    Ecosystem,
    Creatures,
    Player,
    UI,
    Ready
}
```

Każdy etap powinien mieć testy i logi.

---

## 10. Resource system

### 10.1. Obecne rodzaje zasobów

`ResourceNode` obsługuje obecnie:

| Kind | Item | Rola |
|---|---|---|
| `conifer_tree` | wood | drzewo iglaste |
| `leafy_tree` | wood | drzewo liściaste |
| `dry_tree` | wood | suche drzewo |
| `rock` | stone | kamień |
| `bush` | fiber | krzak |
| `dry_bush` | fiber | suchy krzak |
| `small_bush` | fiber | mały krzak |
| `berry_bush` | berries | krzak z jagodami / food resource |
| `grass_patch` | grass | trawa, głównie visual/food dla stworzeń |
| `dense_grass` | grass | gęsta trawa, głównie visual/food dla stworzeń |
| `meat_drop` | meat | drop po śmierci stworzeń |
| `bone_drop` | bone | drop po Varnaku |
| `item_drop` / `inventory_drop` | dynamiczne | przedmiot na ziemi |

### 10.2. Ważne rozróżnienie

W obecnym kodzie roślinność ma kilka ról:

1. **Zasób dla gracza**
   - drewno,
   - fiber,
   - stone,
   - meat,
   - bone.

2. **Jedzenie dla stworzeń**
   - edible vegetation,
   - trawy,
   - krzaki,
   - meat drop dla padlinożerców.

3. **Dekoracja**
   - visual-only grass,
   - dekoracyjne krzewy/trawy,
   - elementy biomów.

4. **Wskaźnik biomasy**
   - ilość i stan roślinności odzwierciedla abstract biomass w `EcosystemDirector`.

W Unity należy rozdzielić te role zamiast trzymać wszystko w jednym komponencie `ResourceView`.

### 10.3. Docelowy model danych

```csharp
public sealed class ResourceState
{
    public Guid Id;
    public string Kind;
    public string ItemId;
    public string BiomeId;
    public Vector2D Position;

    public int Amount;
    public int MatureAmount;

    public int GrowthStage;
    public int MaxGrowthStage;
    public float GrowthProgress;
    public float DaysToNextStage;
    public float DaysSinceHarvested;

    public bool IsHarvested;
    public bool CanBeHarvestedByPlayer;
    public bool IsEdibleByHerbivores;
    public bool IsRenderOnly;
    public bool IsPondVegetation;
    public bool IsInventoryDrop;

    public float FoodValue;
    public float BiomassImpact;
}
```

### 10.4. Docelowe usługi

```text
ResourceCatalog
ResourceRepository
ResourceSpawnPlanner
ResourceHarvestRules
ResourceRegrowthSystem
ResourceDropTable
ResourceViewFactory
ResourceVisibilitySystem
```

### 10.5. Regrowth

Obecny system regrowth ma etapy:

```text
depleted
sprout
young
mature
```

Czasy z balansu:

| Typ | Czas odrostu |
|---|---:|
| trawa | 1 dzień |
| krzak | 2 dni |
| dry_bush | 3 dni |
| drzewo | 15 dni |

W Unity regrowth powinien działać bez widoku.

```csharp
public sealed class ResourceRegrowthSystem
{
    public bool AdvanceDays(ResourceState state, float days);
    public void MarkHarvested(ResourceState state);
    public void ForceFullRegrowth(ResourceState state);
}
```

### 10.6. Harvest flow

Docelowy flow:

```text
PlayerInteraction
→ ResourceHarvestRules.TryHarvest(resource, inventory)
→ InventoryState mutation
→ ResourceState mutation
→ GameEvent: ResourceHarvested
→ EcosystemSimulation receives biomass impact
→ Presentation updates HUD/message
```

Nie wolno w Unity robić tak, aby `ResourceView` bezpośrednio znał `PlayerController`, `HUD`, `EcosystemDirector` i `SaveSystem`.

---

## 11. Inventory i crafting

### 11.1. Inventory

Obecny inventory jest już dobrze kandydatem do przeniesienia, ponieważ `scripts/player/inventory.gd` jest tylko adapterem do `scripts/core/inventory/inventory_state.gd`.

Aktualne zasady:

- domyślnie 9 slotów,
- dodawanie najpierw stackuje do istniejących slotów,
- potem używa pustych slotów,
- `add_item` zwraca leftover,
- `add_item_full_stack` wymaga miejsca na cały stack,
- save zapisuje tylko niepuste sloty,
- load obsługuje nowszy format `slots` i starszy legacy format.

Docelowy Unity model:

```csharp
public sealed class InventoryState
{
    public int SlotCount { get; }
    public IReadOnlyList<ItemStack> Slots { get; }

    public int AddItem(string itemId, int amount);
    public bool CanAddItem(string itemId, int amount);
    public bool AddItemFullStack(string itemId, int amount);
    public bool RemoveItem(string itemId, int amount);
    public int GetAmount(string itemId);
}
```

### 11.2. Item database

Obecny `data/items.json` jest bardzo prosty. Zawiera tylko display names dla kilku itemów. W Unity warto go rozbudować.

Docelowy `ItemDefinition`:

```csharp
public sealed class ItemDefinition
{
    public string Id;
    public string DisplayName;
    public int MaxStack;
    public ItemCategory Category;
    public bool IsConsumable;
    public float Nutrition;
    public string GroundVisualId;
    public string IconId;
}
```

### 11.3. Recipes

Obecne recipes:

| Recipe | Koszt |
|---|---|
| campfire | wood 3, stone 2 |
| spear | wood 2, stone 1, fiber 1 |
| torch | wood 1, fiber 1 |
| bow | wood 3, fiber 4, bone 1 |
| trap | wood 2, fiber 2 |
| wall | wood 3 |
| storage_box | wood 4 |
| tent | wood 4, fiber 3 |

Uwaga: `GameBalance.CRAFTING_COSTS` zawiera także `basic_trap` i `cooked_meat`, których nie ma w prostym `data/recipes.json`. Przy migracji trzeba ujednolicić jedno źródło prawdy.

Rekomendacja:

> W Unity przepisy powinny być data-driven, najlepiej jako JSON lub ScriptableObject generowane z jednego canonical źródła. Nie trzymać kosztów równolegle w kodzie i JSON.

---

## 12. Player i survival

### 12.1. Obecny model

Player łączy obecnie:

- ruch,
- interakcje,
- crafting,
- walkę,
- łuk,
- pochodnię,
- inventory,
- survival stats,
- skanowanie świata,
- komunikację z UI.

W Unity trzeba to rozdzielić.

### 12.2. Docelowy podział

```text
PlayerController
├── PlayerMovementController
├── PlayerInteractionController
├── PlayerCombatController
├── PlayerCraftingController
├── PlayerEquipmentController
└── PlayerState
    ├── SurvivalState
    ├── InventoryState
    ├── HotbarState
    └── EquipmentState
```

### 12.3. Survival stats

Obecne wartości:

| Stat | Max |
|---|---:|
| health | 100 |
| hunger | 100 |
| stamina | 100 |
| rest | 100 |

Ważne progi:

| Parametr | Wartość |
|---|---:|
| low hunger | 25 |
| exhausted rest | 20 |
| hunger decay | 0.75 |
| rest decay | 0.35 |
| running rest decay | 0.9 |
| running stamina decay | 14 |
| starvation damage / s | 1 |
| meat nutrition | 32 |
| sleep hunger cost | 8 |
| sleep health restore | 35 |

Docelowy Unity core:

```csharp
public sealed class SurvivalSystem
{
    public void Tick(SurvivalState state, float deltaTime, SurvivalInput input);
    public bool SpendStamina(SurvivalState state, float amount);
    public void ApplyDamage(SurvivalState state, float amount);
    public void ApplyFood(SurvivalState state, float nutrition);
    public void SleepRecover(SurvivalState state);
    public float GetSpeedMultiplier(SurvivalState state);
}
```

---

## 13. Combat, pochodnia i ognisko

### 13.1. Combat

Obecne systemy:

- attack arc,
- spear,
- bow,
- projectile,
- trap damage,
- Varnak attacks.

W Unity combat powinien używać:

```text
AttackCommand
DamageEvent
HitResolver
ProjectileSystem
CreatureDamageSystem
```

Nie powinien być rozproszony między `Player`, `Varnak`, `ArrowProjectile`, `Trap` i UI.

### 13.2. Pochodnia

Pochodnia ma wpływać na Varnaki:

- zmniejsza detection range,
- zmniejsza aggression,
- zmniejsza close chase range,
- zwiększa attack cooldown,
- wpływa na flee speed,
- daje światło.

Docelowo:

```csharp
public sealed class FearAura
{
    public float Radius;
    public float DetectionMultiplier;
    public float AggressionMultiplier;
    public float AttackCooldownMultiplier;
}
```

Varnak AI nie powinno znać bezpośrednio `TorchView`. Powinno pytać `ThreatModifierService` albo `WorldQuery`.

### 13.3. Ognisko

Ognisko pełni kilka ról:

- bezpieczna strefa,
- światło,
- regeneracja zdrowia/staminy,
- fuel burn,
- element bazy,
- wpływ na Varnaki.

W Unity ognisko powinno być strukturą świata z `CampfireState` oraz osobnym `CampfireView`.

---

## 14. Creature system

### 14.1. Wspólne założenia

Obecne stworzenia są `CharacterBody2D`, ale używają wspólnych systemów:

- `CreatureState`,
- `CreatureContext`,
- `GodotCreatureAdapter`,
- `CreatureSimulationLOD`,
- `MovementSpikeTracker`,
- `HungerDiet`.

To jest dobry kierunek. W Unity należy go dokończyć.

### 14.2. SmallPrey

Stany:

```text
IDLE
WANDER
SEEK_FOOD
EAT
FLEE
DEAD
```

Rola:

- podstawowe roślinożerne ofiary,
- jedzą roślinność,
- uciekają przed graczem i Varnakami,
- są pożywieniem dla Varnaków i głodnych Grazerów,
- wpływają na biomową presję konsumpcji.

Docelowo:

```csharp
public sealed class SmallPreyBrain : ICreatureBrain
{
    public CreatureDecision Decide(CreatureState state, CreatureContext context);
}
```

### 14.3. Grazer

Stany:

```text
IDLE
WANDER
EAT_PLANTS
SEEK_FOOD
FLEE
SCAVENGE
HUNT_SMALL_PREY
DEAD
```

Rola:

- startuje jako herbivore,
- je rośliny,
- może jeść padlinę,
- może polować na SmallPrey przy dużym głodzie i presji biomu,
- może przesuwać niszę z `HERBIVORE` w stronę `OMNIVORE`.

Kluczowe cechy:

```text
plant_diet
meat_diet
scavenger_diet
aggression
fear
reproduction_rate
current_niche
```

### 14.4. Varnak

Stany:

```text
IDLE
WANDER
STALK
CHASE
ATTACK
FLEE
HUNT_ECOSYSTEM
EAT_MEAT
```

Rola:

- główny drapieżnik,
- poluje na gracza i stworzenia,
- reaguje na ogień/pochodnię,
- ma profil adaptacyjny,
- presja Varnaków wpływa na populacje w biomie.

Kluczowe cechy:

```text
aggression
fire_fear
trap_awareness
pack_coordination
night_activity
base_curiosity
stalk_tendency
meat_diet
scavenger_diet
```

### 14.5. Simulation LOD

Obecny prototyp ma poziomy symulacji:

```text
near
medium
far / background
```

To trzeba utrzymać w Unity, ale jako system, nie jako logikę rozsianą po każdym MonoBehaviour.

Docelowo:

```csharp
public enum SimulationLevel
{
    Near,
    Medium,
    Far
}

public sealed class CreatureSimulationScheduler
{
    public void TickVisibleCreatures(float deltaTime);
    public void TickBackgroundCreatures(float deltaTime);
    public SimulationLevel GetLevel(Vector2D creaturePosition, Vector2D playerPosition);
}
```

---

## 15. HungerDiet

Obecny `HungerDiet` jest już prawie czystą logiką core.

Model:

```text
hunger
max_hunger
hunger_growth_rate
energy
plant_diet
meat_diet
scavenger_diet
hungry_threshold
starving_threshold
desperate_threshold
```

Stany głodu:

```text
comfortable
hungry
starving
desperate
```

Docelowo można przenieść niemal 1:1 do C#.

Ważne: w obecnym kodzie nazewnictwo może być mylące, bo dla gracza `hunger` oznacza potrzebę jedzenia w skali 0–100, a dla stworzeń `hunger` to ratio narastającego głodu w skali 0–1. W Unity warto rozdzielić nazwy:

```text
Player hunger: foodEnergy / hungerMeter
Creature hunger: hungerDrive
```

---

## 16. Ecosystem system

### 16.1. Cel

Ekosystem ma sprawić, że biom jest czymś więcej niż kolorem tła. Każdy biom ma stan, który zmienia się pod wpływem:

- naturalnego odrostu roślin,
- jedzenia roślin przez SmallPrey,
- jedzenia roślin przez Grazerów,
- gracza zbierającego rośliny,
- Varnaków polujących,
- śmierci stworzeń,
- debug controls.

### 16.2. Obecny biome state

Każdy biome state zawiera między innymi:

```text
plant_biomass
plant_biomass_percent
max_plant_biomass
plant_regrowth_rate
plant_consumption_pressure
overgrazing_pressure
overgrazing_level
small_prey_population
grazer_population
varnak_population / pressure
food_stress
starvation_pressure
average_hunger
average_energy
small_prey_generation
grazer_generation
varnak_generation
average traits
current_niche
status
```

### 16.3. Status biomu

Statusy:

```text
healthy
stressed
depleted
collapsing
```

Biomass affects:

- debug panel,
- HUD messages,
- biome visual color,
- visible plant resource count,
- SmallPrey population growth,
- Grazer food stress,
- Grazer niche shift.

### 16.4. Dual-state problem

Obecnie istnieją dwa równoległe światy:

```text
World
- visible resources
- visible creatures
- spawned nodes

EcosystemDirector
- abstract biomass
- abstract populations
- average traits
```

To jest projektowo sensowne, ale trudne technicznie. W Unity trzeba zachować rozdział, lecz ustandaryzować synchronizację.

Proponowany model:

```text
Visible World
    emits factual events
        ↓
EcosystemSimulation
    updates abstract biome state
        ↓
WorldProjectionSystem
    decides what should appear/disappear/change visibly
        ↓
Resource/Creature repositories and Unity views
```

### 16.5. Eventy ekosystemu

Warto zachować eventy:

```text
plant_resource_harvested
ecosystem_vegetation_changed
ecosystem_biome_stressed
ecosystem_biome_depleted
ecosystem_biome_collapsing
small_prey_population_declining
grazer_population_declining
grazer_niche_shifted
small_prey_consumed_plants
grazer_consumed_plants
grazer_scavenged
grazer_hunted_small_prey
meat_consumed_by_creature
```

### 16.6. Not in scope na start Unity

Nie przenosić od razu:

- pełnej genetyki osobników,
- migracji między biomami,
- wielu gatunków drapieżników,
- sezonów,
- chorób,
- indywidualnego rozmnażania każdego stworzenia,
- pełnego 3D zachowania stada.

Najpierw przenieść biome-level model.

---

## 17. EvolutionDirector i adaptacja Varnaków

### 17.1. Rola

Varnaki mają być głównym nośnikiem motywu Apex Shift. Ich profil powinien zmieniać się w odpowiedzi na zachowania gracza.

Obecne / planowane traitsy:

```text
aggression
fire_fear
trap_awareness
pack_coordination
night_activity
base_curiosity
stalk_tendency
meat_diet
scavenger_diet
generation
```

### 17.2. Kierunki adaptacji

Przykładowe reguły:

| Zachowanie gracza | Możliwa adaptacja Varnaków |
|---|---|
| Częste używanie pułapek | wzrost `trap_awareness` |
| Częste używanie ognia | zmiana `fire_fear` |
| Częste zabijanie Varnaków | wzrost ostrożności albo stalkingu |
| Ukrywanie się w bazie | wzrost ciekawości wobec ścian |
| Aktywność nocą | większa presja nocna |
| Polowanie na ofiary | Varnaki konkurują z graczem o food web |

### 17.3. Unity system

```csharp
public sealed class EvolutionSystem
{
    public VarnakProfile CurrentProfile { get; }

    public void ApplyPlayerAction(PlayerActionEvent action);
    public void AdvanceGeneration(EcosystemState ecosystem);
    public VarnakProfile GetSpawnProfile(string biomeId, int day);
}
```

---

## 18. Day/Night

### 18.1. Obecna rola

DayNight wpływa na:

- HUD clock,
- nocny overlay,
- Varnak danger,
- dzień gry,
- save/load,
- sleep,
- population pressure,
- Varnak day scaling.

Obecnie dzień trwa około 120 sekund w prototypie.

### 18.2. Unity model

```csharp
public sealed class DayNightState
{
    public int Day;
    public float TimeOfDay01;
    public float Hour;
    public bool IsNight;
    public float NightAmount;
}

public sealed class DayNightSystem
{
    public void Tick(DayNightState state, float deltaTime);
    public void StartNewDay(string reason);
}
```

---

## 19. Save/load

### 19.1. Obecny save path

Godot zapisuje do:

```text
user://savegame.json
```

Save system używa:

```text
SaveSerializer
SaveDataCollector
SaveDataRestorer
GameSaveData
PlayerSaveData
BuildingState
```

### 19.2. Obecny save zawiera

- version,
- world,
- world_generation,
- player,
- resources,
- varnaks,
- small_prey,
- grazers,
- buildings,
- storage_boxes,
- day_night,
- evolution,
- ecosystem,
- clock.

### 19.3. Bardzo ważna kolejność restore

Kolejność restore jest krytyczna:

1. Begin world restore.
2. Restore landmarks/world seed.
3. Restore bootstrap world state in GameSession.
4. Determine restore mode:
   - full layout,
   - seed fallback,
   - legacy.
5. Apply world layout if present.
6. Restore player.
7. Restore evolution.
8. Restore ecosystem.
9. Restore day/night.
10. Restore resources.
11. Restore buildings.
12. Restore storage boxes.
13. Restore Varnaks.
14. Restore SmallPrey.
15. Restore Grazers.
16. End world restore.
17. Rebuild runtime indexes.

W Unity trzeba zachować tę zasadę:

> Najpierw przestrzeń i layout, potem obiekty zależne od przestrzeni, potem runtime indeksy.

### 19.4. Unity save DTO

```csharp
public sealed class SaveGameData
{
    public int Version;
    public WorldSaveData World;
    public WorldGenerationSaveData WorldGeneration;
    public PlayerSaveData Player;
    public List<ResourceSaveData> Resources;
    public List<CreatureSaveData> Creatures;
    public List<BuildingSaveData> Buildings;
    public DayNightSaveData DayNight;
    public EvolutionSaveData Evolution;
    public EcosystemSaveData Ecosystem;
}
```

---

## 20. UI, mapy i debug

### 20.1. Obecny problem

HUD, minimapa, map screen i debug panel mają własne odczyty i cache. To jest jeden z głównych powodów, dla których dalszy rozwój może robić się coraz trudniejszy.

### 20.2. Unity rekomendacja

Wprowadzić `WorldSnapshotService`.

```text
World / Player / Ecosystem / DayNight
        ↓
WorldSnapshotService
        ↓
HUDViewModel
MapViewModel
MinimapViewModel
DebugViewModel
```

UI nie powinno skanować sceny ani pytać bezpośrednio obiektów świata.

### 20.3. Minimap

Minimap powinna konsumować uproszczone dane:

```csharp
public sealed class MapSnapshot
{
    public Vector2D PlayerPosition;
    public IReadOnlyList<MapMarker> ResourceMarkers;
    public IReadOnlyList<MapMarker> CreatureMarkers;
    public IReadOnlyList<BiomeRegionViewData> Biomes;
    public IReadOnlyList<TopographyMarker> PondsAndHills;
}
```

### 20.4. Debug panel

Debug panel ma pozostać narzędziem developerskim:

- biome state,
- population,
- biomass,
- creature counts,
- resource counts,
- culling stats,
- FPS,
- benchmark,
- spawn rejection summaries,
- save/load validation.

Nie powinien być częścią finalnego gameplay UI.

---

## 21. Rendering, culling i performance

### 21.1. Obecny stan

Aktualny `World` zajmuje się jednocześnie:

- generowaniem świata,
- spawnem zasobów,
- spawnem stworzeń,
- bindingiem query services,
- zarządzaniem chunk managerem,
- vegetation visual layer,
- render controllerem,
- terrain surface rendererem,
- visibility controllerem,
- performance governorem,
- debug/profilerem.

To trzeba rozbić.

### 21.2. Zasada Unity

```text
Simulation != Rendering != Presentation
```

Nie wolno mieszać:

- stanu zasobów,
- decyzji AI,
- renderowania trawy,
- minimapy,
- debug textu,
- spawn sync,
- save/load.

### 21.3. Unity systems

```text
VisibilitySystem
SpatialIndex
ChunkManager
ResourceViewPool
CreatureViewPool
TerrainRenderer
VegetationRenderer
MapRenderer
RenderBudgetGovernor
```

### 21.4. Dekoracyjna roślinność

Obecne problemy i ustalenia:

- Grass ResourceNode nodes nie powinny wrócić jako masowe nody.
- `grass_patch_node_count` i `dense_grass_node_count` powinny pozostać 0 dla dekoracyjnej trawy.
- Trawa dekoracyjna ma być data-driven.
- Renderować tylko widoczną/aktywną dekoracyjną roślinność.
- Dane należy bucketować w chunkach.
- Rysować tylko camera visible rect + margin albo active chunks.
- Benchmark powinien mierzyć:
  - `decorative_vegetation_total_instance_count`,
  - `decorative_vegetation_drawn_instance_count`,
  - `decorative_vegetation_visible_chunk_count`.

Unity odpowiednik:

```csharp
public sealed class DecorativeVegetationSystem
{
    public void Register(DecorativeVegetationInstance instance);
    public IReadOnlyList<DecorativeVegetationInstance> QueryVisible(RectD cameraRect, float margin);
}
```

Renderowanie:
- w 2D: `SpriteRenderer` batching, `Tilemap`, `Graphics.DrawMeshInstanced`;
- w 3D: GPU instancing / terrain details / custom indirect rendering.

---

## 22. Znane bugi, ryzyka i niespójności

Ta sekcja jest bardzo ważna: obecny develop nie jest idealnym wzorcem do przepisania. Część rzeczy powinna być potraktowana jako **bug do zamiany na test**, a nie jako zachowanie do kopiowania.

### 22.1. Bug: Westwood pusty / brak widocznych drzew

Objaw z testów ręcznych:
- w Westwood nie widać drzew,
- UI statystyki pokazują 0,
- biom, który powinien być gęstym lasem iglastym, wygląda pusto.

Możliwe obszary:
- `VegetationSpawnPlanner`,
- biome vegetation profiles,
- resource spawn rejection,
- topography blocking,
- visibility culling,
- decorative vegetation visual layer,
- chunk assignments,
- resource activation,
- błędny query biome ID,
- rozjazd między visual-only vegetation a ResourceNode counts.

Unity action:
- dodać test seed dla Westwood:
  - spawn count coniferów > minimum,
  - drawn count > 0,
  - resource count nie jest zerowy,
  - visible chunk count > 0 przy kamerze w Westwood.
- rozdzielić:
  - total generated resources,
  - active resources,
  - visible rendered resources,
  - interactable resources.

### 22.2. Bug: South Thicket diagonal line / drzewa po skosie

Objaw:
- w South Thicket drzewa/roślinność pojawiały się w jednej linii po skosie.

Możliwe obszary:
- deterministic slot fallback,
- precomputed biome spawn points,
- źle dobrany sampling punktów w poligonie,
- błąd w jitterze lub fallbacku,
- pomylenie osi / transformów,
- zbyt mało prób spawnu i fallback liniowy.

Unity action:
- test rozkładu punktów:
  - punkty nie mogą mieć bardzo wysokiej korelacji liniowej,
  - bounding box ma być wypełniony,
  - minimalne odległości zachowane,
  - spawn points muszą być wewnątrz poligonu biomu i poza wodą.

### 22.3. Bug/ryzyko: UI pokazuje 0 mimo istniejącej logiki

Objaw:
- statystyki UI pokazują 0 dla drzew/roślinności.

Możliwe obszary:
- UI czyta z innego źródła niż faktyczny system renderujący,
- visual-only vegetation nie jest liczona jako resource node,
- count mierzy tylko aktywne nody, a nie dane w visual layer,
- culling ukrywa nody,
- rejestr/chunk manager nie został odbudowany po boot/load.

Unity action:
- wprowadzić jawne liczniki:
  - `generated_count`,
  - `registered_count`,
  - `active_count`,
  - `visible_count`,
  - `drawn_count`,
  - `interactable_count`.
- HUD/debug nie może samodzielnie zgadywać liczników. Musi czytać z `WorldSnapshotService`.

### 22.4. Niespójność: WORLD_SCALE

Obecny `WorldConfig` ma `WORLD_SCALE = 6.0`, ale część starszej dokumentacji / `GameBalance.LIVING_WORLD` wskazuje `2.2`.

Unity action:
- wybrać jedno źródło prawdy,
- przenieść skalę do `WorldGenerationConfig`,
- dodać test sprawdzający, że dokumentowane world bounds zgadzają się z runtime bounds.

### 22.5. Ryzyko: `is_player_start_biome_position`

Obecna funkcja wygląda podejrzanie, ponieważ sprawdza, czy biom startowy istnieje, a nie czy dana pozycja leży w biomie startowym. To może sprawić, że safe start validation nie waliduje faktycznej pozycji tak dokładnie, jak nazwa sugeruje.

Unity action:
- poprawna funkcja powinna wyglądać logicznie tak:

```csharp
bool IsPlayerStartBiomePosition(Vector2D position)
{
    return worldQuery.GetBiomeId(position) == config.PlayerStartBiomeId;
}
```

### 22.6. Ryzyko: landmarks vs topography

W kodzie istnieją statyczne landmarki, ale aktualna selekcja zwraca pustą listę, bo ponds/hills są przeniesione do topografii.

To może być poprawne, ale dla migracji jest ryzykiem semantycznym.

Unity action:
- zdecydować jeden model:
  - `GeneratedTopographyFeatures` jako serializowalne POI,
  - albo legacy landmarks usunąć/oznaczyć jako deprecated.
- Mapa, minimapa, save/load i spawn blockers muszą czytać z tego samego źródła.

### 22.7. Ryzyko: dual-state ecosystem

Jeżeli `World` i `Ecosystem` rozjadą się, gracz zobaczy jedno, debug pokaże drugie, a save zapisze trzecie.

Unity action:
- event log + snapshot tests,
- po każdej zmianie biomasy sprawdzać:
  - biome state,
  - resource projection,
  - visible count,
  - map marker count.

### 22.8. Ryzyko: visibility culling psuje interakcje

Obecny `ResourceNode` dezaktywuje widoczność, kolizje i process w zależności od cullingu. Jeśli activation nie zadziała, zasób może istnieć w danych, ale nie być widoczny ani interaktywny.

Unity action:
- oddzielić culling renderingu od interakcji.
- Interakcja powinna używać spatial query, nie widoczności renderera.
- Test: zasób blisko gracza nie może pozostać culled.

### 22.9. Ryzyko: save/load i rebuild indeksów

Save/load jest już bardziej uporządkowany, ale wymaga poprawnej kolejności. Po restore trzeba odbudować runtime indexes.

Unity action:
- save/load roundtrip test:
  - world layout,
  - player,
  - resources,
  - creatures,
  - ecosystem,
  - day/night,
  - buildings,
  - storage,
  - spatial index,
  - map snapshot.

### 22.10. Prototype limitations

Znane ograniczenia prototypu:
- UI jest utilitarne i nie ma finalnego polishu.
- Debug overlaye są narzędziem testowym.
- Inventory i storage działają, ale UX/balans może się zmieniać.
- Save data obejmuje główne systemy prototypu, ale nowe stany wymagają walidacji.
- Creature behavior, drops i resource tuning są nadal iterowane.

---

## 23. Czego nie kopiować do Unity

Nie kopiować 1:1:

1. Wielkiego `World` jako jednego MonoBehaviour.
2. `Player` jako klasy odpowiedzialnej za wszystko.
3. UI skanującego świat samodzielnie.
4. Grup jako głównego mechanizmu query.
5. Globalnych singletonów bez Composition Root.
6. Renderowania powiązanego z symulacją.
7. `queue_redraw`-style aktualizacji wszędzie po każdej zmianie.
8. Braku jednego źródła prawdy dla parametrów.
9. Równoległych danych w `GameBalance`, JSON i docs.
10. Dublowania cache między minimapą i full mapą.

---

## 24. Co przenieść niemal bezpośrednio

Dobre kandydaty do portu:

| System | Status |
|---|---|
| `InventoryState` | bardzo dobry kandydat do portu na czysty C# |
| `HungerDiet` | bardzo dobry kandydat do portu |
| `SurvivalState/System` | dobry kandydat |
| `ResourceState` | dobry kandydat po oczyszczeniu |
| `ResourceHarvestRules` | dobry kandydat |
| `ResourceRegrowthSystem` | dobry kandydat |
| `Save DTO` | dobry kandydat po przepisaniu na C# |
| `EcosystemSimulation` | przenieść jako core, ale uprościć integrację |
| `CreatureState` | przenieść, ale AI rozdzielić na brain/services |
| `GameBalance` | przenieść do data/config, nie jako jedna ogromna klasa |

---

## 25. Proponowana kolejność migracji do Unity

### Etap 0 — zamrożenie wiedzy

- Zachować branch Godot jako referencję.
- Oznaczyć znane bugi jako test cases.
- Spisać docelowe zachowania biomów.
- Wybrać aktualne źródło prawdy dla world scale i balansu.

### Etap 1 — Core C# bez Unity

Stworzyć projekt:

```text
ApexShift.Core
ApexShift.Core.Tests
```

Przenieść:

1. `InventoryState`.
2. `ItemStack`.
3. `ItemDatabase`.
4. `RecipeDatabase`.
5. `CraftingSystem`.
6. `SurvivalState`.
7. `SurvivalSystem`.
8. `HungerDiet`.
9. `ResourceState`.
10. `ResourceHarvestRules`.
11. `ResourceRegrowthSystem`.

W tym etapie nie ma scen, prefabs, rendererów ani inputu.

### Etap 2 — World generation core

Przenieść:

- `WorldConfig`,
- biome definitions,
- terrain height,
- terrain zones,
- biome polygons,
- organic edge jitter,
- safe player start,
- spawn planner.

Dodać testy deterministic seed.

### Etap 3 — Ecosystem core

Przenieść:

- `BiomeState`,
- `EcosystemSimulation`,
- event ingestion,
- plant biomass,
- food stress,
- grazer niche shift,
- population changes.

Dodać testy:
- harvesting changes biomass,
- grazers under food stress shift diet,
- biomass status changes,
- save/load restores ecosystem.

### Etap 4 — Creature AI core

Przenieść stan i brainy:

```text
SmallPreyBrain
GrazerBrain
VarnakBrain
CreatureSimulationScheduler
CreatureLOD
```

AI powinno zwracać decyzje, a Unity view/controller powinien je wykonywać.

### Etap 5 — Unity Runtime

Dodać:

- `GameBootstrapper`,
- `WorldRuntime`,
- `PlayerController`,
- `ResourceView`,
- `CreatureView`,
- `PoolService`,
- `UnityWorldQueryAdapter`,
- `UnityInputAdapter`,
- `UnitySaveStorage`.

### Etap 6 — UI i debug

Dodać:

- HUD,
- minimap,
- map screen,
- debug panel,
- benchmark overlay.

Wszystko przez `WorldSnapshotService`.

### Etap 7 — walidacja feature parity

Porównać z Godot:

- basic survival loop,
- resource harvest,
- crafting,
- save/load,
- ecosystem tick,
- creature death/drop,
- Varnak day scaling,
- Westwood vegetation,
- South Thicket distribution,
- UI counters.

---

## 26. Test plan do Unity

### 26.1. Unit tests

Najważniejsze testy:

#### Inventory

- add item to empty slot,
- stack item,
- reject unknown item,
- full inventory returns leftover,
- remove item,
- save/load slots,
- legacy migration.

#### Crafting

- recipe exists,
- craft consumes resources,
- cannot craft without resources,
- crafted item/equipment state changes,
- recipe database equals expected canonical config.

#### Survival

- hunger/rest decay,
- stamina spend/recovery,
- starvation damage,
- food restores hunger,
- sleep restores health and costs hunger,
- speed multiplier under low hunger/rest.

#### Resources

- setup resource kind,
- harvest gives item,
- cannot harvest render-only vegetation,
- regrowth stages,
- creature consumption reduces food/growth,
- meat drop consumed once,
- resource save/load.

#### World generation

- deterministic seed,
- different seed produces different layout,
- terrain zones valid,
- player start on land/highland,
- player start in desired biome,
- resources not in deep water,
- creatures not in water,
- biome profile produces expected majority vegetation.

#### Ecosystem

- harvest reduces biomass,
- biomass status thresholds,
- small prey consumption changes pressure,
- grazer food stress,
- grazer niche shift,
- Varnak pressure,
- save/load state.

#### Creatures

- SmallPrey flees threat,
- SmallPrey seeks food when hungry,
- Grazer eats plants,
- Grazer scavenges/hunts when desperate,
- Varnak hunts prey/player,
- deaths emit events,
- drops spawn once.

### 26.2. Regression tests z bugów

- Westwood is not empty.
- Westwood has majority conifer visual + resource presence.
- South Thicket spawn distribution is not diagonal.
- UI counts match world snapshot.
- Culling does not hide interactable resources near player.
- Decorative vegetation total can grow, but drawn count stays budgeted.
- Save/load rebuilds indexes.
- Terrain/map does not show debug grid or diagonal artifacts in normal mode.

### 26.3. Golden seed tests

Stworzyć listę seedów:

```text
seed_westwood_dense_forest
seed_south_thicket_distribution
seed_hearth_meadow_safe_start
seed_redfang_danger
seed_large_world_performance
```

Dla każdego seedu snapshot:

- biome counts,
- resource counts,
- creature counts,
- ponds/hills,
- player start,
- culling/visible counts.

---

## 27. Data-driven config do Unity

### 27.1. Foldery danych

```text
Assets/ApexShift/Data/Items
Assets/ApexShift/Data/Recipes
Assets/ApexShift/Data/Biomes
Assets/ApexShift/Data/Resources
Assets/ApexShift/Data/Creatures
Assets/ApexShift/Data/Ecosystem
Assets/ApexShift/Data/WorldGeneration
Assets/ApexShift/Data/Difficulty
```

### 27.2. Biome vegetation profile

Przykład:

```json
{
  "id": "westwood",
  "displayName": "Westwood",
  "dangerous": false,
  "vegetation": {
    "conifer_tree": 0.62,
    "leafy_tree": 0.03,
    "dry_tree": 0.00,
    "bush": 0.10,
    "berry_bush": 0.08,
    "dry_bush": 0.02,
    "grass_patch": 0.10,
    "dense_grass": 0.05
  }
}
```

### 27.3. Redfang profile

```json
{
  "id": "redfang_wilds",
  "displayName": "Redfang Wilds",
  "dangerous": true,
  "vegetation": {
    "conifer_tree": 0.00,
    "leafy_tree": 0.00,
    "dry_tree": 0.50,
    "bush": 0.05,
    "berry_bush": 0.01,
    "dry_bush": 0.30,
    "grass_patch": 0.08,
    "dense_grass": 0.06
  }
}
```

To są wartości startowe do dopracowania, nie ostateczny balans.

---

## 28. Minimalny vertical slice Unity

Pierwszy sensowny vertical slice Unity powinien zawierać:

1. Jeden proceduralny świat z pięcioma biomami.
2. Topografia:
   - ląd,
   - woda,
   - shore,
   - highland.
3. Poprawny safe spawn gracza.
4. Resource spawn:
   - drzewa,
   - kamienie,
   - krzaki,
   - trawa dekoracyjna,
   - mięso/kości jako drop.
5. Inventory 9-slot.
6. Crafting:
   - spear,
   - campfire,
   - torch,
   - bow,
   - trap,
   - wall,
   - storage,
   - tent.
7. Player survival stats.
8. SmallPrey.
9. Grazer.
10. Varnak.
11. Ecosystem biome biomass.
12. Day/night.
13. Save/load.
14. HUD + debug panel.
15. Testy regression dla znanych bugów.

---

## 29. Decyzje projektowe do utrzymania

### 29.1. Gra nie ma być tylko survivalem z craftingiem

Crafting jest środkiem do przetrwania, ale nie głównym wyróżnikiem. Główne wyróżniki to:

- adaptujące się zagrożenie,
- lokalne konsekwencje działań,
- żywy food web,
- utrata pozycji apex predator.

### 29.2. Varnaki są centralne

Varnaki są najbardziej rozpoznawalnym zagrożeniem. Muszą mieć:

- czytelny profil zachowania,
- progresję presji,
- reakcję na ogień/pułapki,
- związek z nocą,
- związek z ekosystemem,
- potencjalny rozwój pokoleniowy.

### 29.3. Świat ma być naturalny, nie kafelkowy

Było ustalenie, że świat nie powinien wyglądać jak duże kafle. Unity powinno iść w stronę:

- organicznych poligonów biomów,
- noise/jitter na granicach,
- warstw renderingu,
- chunków tylko jako techniczna optymalizacja,
- naturalnych przejść wizualnych.

### 29.4. Debug jest częścią produkcji

Ponieważ projekt jest systemowy, debug tools są kluczowe:

- biome overlay,
- creature state,
- resource counts,
- spawn rejection,
- culling stats,
- save/load validation,
- benchmark.

---

## 30. GitHub issues / task backlog dla Unity

### Epic: Core migration

- `[CORE] Port InventoryState to Unity C#`
- `[CORE] Port CraftingSystem and recipe database`
- `[CORE] Port SurvivalState and SurvivalSystem`
- `[CORE] Port HungerDiet`
- `[CORE] Port ResourceState, HarvestRules and RegrowthSystem`
- `[CORE] Port CreatureState DTO`
- `[CORE] Port EcosystemSimulation`
- `[CORE] Create deterministic RNG abstraction`
- `[CORE] Create Vector2D/RectD math primitives`

### Epic: World generation

- `[WORLD] Port WorldConfig and biome definitions`
- `[WORLD] Implement terrain height and zone sampling`
- `[WORLD] Implement organic biome polygons`
- `[WORLD] Implement topography ponds and hills`
- `[WORLD] Implement safe player start`
- `[WORLD] Implement biome vegetation profiles`
- `[WORLD] Implement resource spawn planner`
- `[WORLD] Add golden seed tests`

### Epic: Unity runtime

- `[UNITY] Create GameBootstrapper`
- `[UNITY] Create WorldRuntime`
- `[UNITY] Create ResourceView and ResourceViewPool`
- `[UNITY] Create CreatureView and CreatureViewPool`
- `[UNITY] Create PlayerController modules`
- `[UNITY] Create SpatialIndex`
- `[UNITY] Create WorldSnapshotService`
- `[UNITY] Implement SaveStorage using persistentDataPath`

### Epic: Bugs carried from Godot

- `[BUG] Westwood vegetation count is zero / invisible`
- `[BUG] South Thicket diagonal vegetation distribution`
- `[BUG] UI resource stats show zero despite generated vegetation`
- `[BUG] Validate player start biome check`
- `[BUG] Resolve world scale config mismatch`
- `[BUG] Separate decorative vegetation counts from resource node counts`
- `[BUG] Culling must not break nearby resource interaction`
- `[BUG] Save/load must rebuild all runtime indexes`

### Epic: Tests

- `[TEST] Inventory and crafting unit tests`
- `[TEST] Resource harvest/regrowth unit tests`
- `[TEST] World generation deterministic seed tests`
- `[TEST] Biome vegetation majority tests`
- `[TEST] Creature AI behavior tests`
- `[TEST] Ecosystem biomass and niche shift tests`
- `[TEST] Save/load roundtrip tests`
- `[TEST] Debug snapshot consistency tests`

---

## 31. Appendix A — Słownik pojęć

| Pojęcie | Znaczenie |
|---|---|
| Apex Shift | Przesunięcie dominacji ze strony człowieka na adaptujące się drapieżniki/świat. |
| Varnak | Główny drapieżnik i symbol adaptacji zagrożenia. |
| SmallPrey | Mała ofiara, roślinożerca pierwszego poziomu. |
| Grazer | Większy roślinożerca/flex herbivore, może przesuwać dietę. |
| Biomass | Abstrakcyjny poziom roślinności w biomie. |
| Biome state | Stan konkretnego biomu: biomasa, populacje, stres, presja, traitsy. |
| Visible world | Faktyczne obiekty widoczne i interaktywne w świecie. |
| Abstract ecosystem | Symulacja populacji i biomasy bez pełnych osobników. |
| ResourceNode | Godotowy węzeł zasobu, do rozbicia na ResourceState + ResourceView. |
| Render-only vegetation | Roślinność dekoracyjna, nie jako pełny node/interactable resource. |
| WorldSnapshot | Uproszczone dane prezentacyjne dla HUD, mapy i debug panelu. |

---

## 32. Appendix B — Najważniejsze reguły architektoniczne dla Unity

1. Core nie zna Unity.
2. Unity views nie są źródłem prawdy.
3. Save zapisuje state, nie GameObjecty.
4. UI czyta snapshot, nie scenę.
5. AI pyta `WorldQuery`, nie skanuje sceny.
6. Rendering ma własne budżety.
7. Dekoracyjna trawa nie jest masą obiektów gameplayowych.
8. Culling renderingu nie może usuwać danych świata.
9. World generation musi być deterministyczny.
10. Każdy znany bug z Godota powinien zostać testem w Unity.

---

## 33. Appendix C — Minimalne kryteria, żeby uznać migrację za udaną

Migracja ma sens dopiero wtedy, gdy Unity build potrafi:

- wygenerować świat z pięcioma biomami,
- poprawnie rozmieścić zasoby zgodnie z profilem biomu,
- pokazać Westwood jako gęsty las iglasty,
- pokazać South Thicket bez diagonalnego artefaktu spawnu,
- utrzymać dekoracyjną roślinność w budżecie renderingu,
- pozwolić zebrać podstawowe zasoby,
- pozwolić craftować podstawowe itemy,
- obsługiwać inventory i storage,
- obsługiwać survival stats,
- mieć SmallPrey, Grazers i Varnaki,
- mieć podstawowy ekosystem biomów,
- mieć save/load,
- mieć debug panel,
- mieć testy regresji na znane bugi.

---

## 34. Podsumowanie

Największą wartością obecnego prototypu nie jest gotowy kod silnikowy, tylko projekt systemów:

- survival,
- zasoby,
- inventory,
- crafting,
- biome world,
- ekosystem,
- Varnaki,
- adaptacja,
- save/load,
- debug/test culture.

Do Unity należy przenieść **logikę, dane, balans, testy i intencje projektowe**. Nie należy przenosić struktury, w której `World` i `Player` robią zbyt wiele, a UI i debug czytają świat na własną rękę.

Najbezpieczniejsza ścieżka:

1. Najpierw czysty Core C#.
2. Potem testy.
3. Potem Unity adaptery.
4. Dopiero potem grafika, prefaby, UI i 3D.
5. Znane bugi z Godota zamienić w testy regresji, żeby nie przenieść ich do nowej wersji.
