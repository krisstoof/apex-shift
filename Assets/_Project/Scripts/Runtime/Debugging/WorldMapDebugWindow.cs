using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Resources;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.Debugging
{
    [DisallowMultipleComponent]
    public sealed class WorldMapDebugWindow : MonoBehaviour
    {
        [SerializeField] private bool visible = false;
        [SerializeField] private KeyCode toggleKey = KeyCode.F4;
        [SerializeField] private float refreshIntervalSeconds = 0.5f;

        private Rect windowRect = new Rect(720f, 140f, 520f, 520f);
        private Vector2 scroll;
        private float refreshTimer;
        private string cachedText = "World debug loading...";
        private bool showDebugControls = false;
        private bool isResizing = false;

        private void Update()
        {
            bool togglePressed = false;
            if (Keyboard.current != null)
            {
                if (toggleKey == KeyCode.F4 && Keyboard.current[Key.F4].wasPressedThisFrame) togglePressed = true;
                else if (toggleKey == KeyCode.F1 && Keyboard.current[Key.F1].wasPressedThisFrame) togglePressed = true;
                else if (toggleKey == KeyCode.F2 && Keyboard.current[Key.F2].wasPressedThisFrame) togglePressed = true;
                else if (toggleKey == KeyCode.F3 && Keyboard.current[Key.F3].wasPressedThisFrame) togglePressed = true;
                else if (toggleKey == KeyCode.Escape && Keyboard.current[Key.Escape].wasPressedThisFrame) togglePressed = true;
            }

            if (togglePressed)
            {
                visible = !visible;
            }

            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                refreshTimer = Mathf.Max(0.1f, refreshIntervalSeconds);
                cachedText = BuildDebugText();
            }
        }

        private void OnGUI()
        {
            if (!visible)
            {
                DebugUIBounds.WorldMapWindowVisible = false;
                return;
            }

            DebugUIBounds.WorldMapWindowVisible = true;
            DebugUIBounds.WorldMapWindowRect = windowRect;

            windowRect = GUI.Window(6789, windowRect, DrawWindow, "World / Map Debug  [F4]");
        }

        private void DrawWindow(int id)
        {
            showDebugControls = GUILayout.Toggle(showDebugControls, "Show Debug Controls / Opcje Debugowania", "button");
            if (showDebugControls)
            {
                GUILayout.BeginVertical("box");
                bool hideFrames = CreatureDebugOverlay.HideAllDebugFrames;
                bool newHideFrames = GUILayout.Toggle(hideFrames, "Hide All Creature Overlays / Ukryj Ramki Zwierząt");
                if (newHideFrames != hideFrames)
                {
                    CreatureDebugOverlay.HideAllDebugFrames = newHideFrames;
                }

                if (GUILayout.Button("Feed All Creatures / Nakarm wszystkie zwierzęta"))
                {
                    CreatureNeedsRuntime[] needs = Object.FindObjectsByType<CreatureNeedsRuntime>(FindObjectsInactive.Exclude);
                    foreach (var need in needs)
                    {
                        if (need != null) need.Eat(-need.State.Hunger); // reset hunger to 0
                    }
                }

                if (GUILayout.Button("Warp Creatures to Player / Przywołaj zwierzęta"))
                {
                    GameObject playerObj = GameObject.Find("Player");
                    if (playerObj != null)
                    {
                        CreatureNavigationAdapter[] adapters = Object.FindObjectsByType<CreatureNavigationAdapter>(FindObjectsInactive.Exclude);
                        foreach (var adapter in adapters)
                        {
                            if (adapter != null)
                            {
                                adapter.transform.position = playerObj.transform.position + Random.insideUnitSphere * 3f;
                                adapter.WarpToNearestNavMesh();
                            }
                        }
                    }
                }
                GUILayout.EndVertical();
            }

            float scrollHeight = windowRect.height - (showDebugControls ? 140f : 44f);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Width(windowRect.width - 16f), GUILayout.Height(Mathf.Max(50f, scrollHeight)));
            GUILayout.Label(cachedText);
            GUILayout.EndScrollView();

            // Resize handle
            Rect resizeHandleRect = new Rect(windowRect.width - 16f, windowRect.height - 16f, 16f, 16f);
            GUI.Box(resizeHandleRect, "", "label");

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && resizeHandleRect.Contains(currentEvent.mousePosition))
            {
                isResizing = true;
                currentEvent.Use();
            }

            if (isResizing)
            {
                if (currentEvent.type == EventType.MouseDrag)
                {
                    windowRect.width = Mathf.Max(230f, currentEvent.mousePosition.x + 10f);
                    windowRect.height = Mathf.Max(230f, currentEvent.mousePosition.y + 10f);
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.MouseUp)
                {
                    isResizing = false;
                    currentEvent.Use();
                }
            }

            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 22f));
        }

        private string BuildDebugText()
        {
            StringBuilder builder = new StringBuilder(4096);

            CreatureAgentView[] creatures = Object.FindObjectsByType<CreatureAgentView>(FindObjectsInactive.Exclude);
            CreatureNeedsRuntime[] needs = Object.FindObjectsByType<CreatureNeedsRuntime>(FindObjectsInactive.Exclude);
            FoodSourceView[] foods = Object.FindObjectsByType<FoodSourceView>(FindObjectsInactive.Exclude);
            ResourceNodeView[] resources = Object.FindObjectsByType<ResourceNodeView>(FindObjectsInactive.Exclude);
            NavMeshAgent[] agents = Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Exclude);
            Transform player = FindPlayer();
            EcosystemRuntime ecosystem = EcosystemRuntime.Instance;

            builder.AppendLine("=== WORLD ===");
            builder.AppendLine($"t: {Time.time:0.0}s");
            builder.AppendLine($"ply: {(player != null ? player.position.ToString("F1") : "miss")}");
            builder.AppendLine($"res: {resources.Length}");
            builder.AppendLine($"food: {foods.Length}");
            builder.AppendLine();

            AppendEcosystem(builder, foods);
            AppendCreatureSummary(builder, creatures, needs, agents);
            AppendSpeciesSummary(builder, creatures, player);
            AppendCreatureRows(builder, creatures, player);

            return builder.ToString();
        }

        private static void AppendEcosystem(StringBuilder builder, FoodSourceView[] foods)
        {
            int plants = foods.Count(f => f != null && f.Kind == FoodKind.Plants);
            int meat = foods.Count(f => f != null && f.Kind == FoodKind.Meat);
            int scavenger = foods.Count(f => f != null && f.Kind == FoodKind.Scavenger);
            int empty = foods.Count(f => f == null || f.IsEmpty);

            float avgPlants = AverageBiomass(foods, FoodKind.Plants);
            float avgMeat = AverageBiomass(foods, FoodKind.Meat);

            EcosystemRuntime ecosystem = EcosystemRuntime.Instance;

            builder.AppendLine("=== ECOSYS ===");
            builder.AppendLine($"reg: {(ecosystem != null ? "ok" : "miss")}");
            if (ecosystem != null)
            {
                builder.AppendLine($"reg food all/pl/me: {ecosystem.FoodSourceCount}/{ecosystem.PlantFoodSourceCount}/{ecosystem.MeatFoodSourceCount}");
                builder.AppendLine($"reg creatures: {ecosystem.CreatureCount}");
            }

            builder.AppendLine($"scene food a/pl/me/sc/em: {foods.Length}/{plants}/{meat}/{scavenger}/{empty}");
            builder.AppendLine($"avg bio pl/me: {avgPlants:0.00}/{avgMeat:0.00}");
            builder.AppendLine();
        }

        private static void AppendCreatureSummary(StringBuilder builder, CreatureAgentView[] creatures, CreatureNeedsRuntime[] needs, NavMeshAgent[] agents)
        {
            int onNavMesh = agents.Count(a => a != null && a.isOnNavMesh);
            int offNavMesh = agents.Length - onNavMesh;
            int moving = agents.Count(a => a != null && a.isOnNavMesh && a.velocity.sqrMagnitude > 0.01f);
            int withPath = agents.Count(a => a != null && a.isOnNavMesh && a.hasPath);
            int hungry = needs.Count(n => n != null && n.State.IsHungry);
            int starving = needs.Count(n => n != null && n.State.Stage == HungerStage.Starving);
            int desperate = needs.Count(n => n != null && n.State.Stage == HungerStage.Desperate);
            float avgHunger = needs.Length == 0 ? 0f : needs.Average(n => n.State.Hunger / Mathf.Max(0.01f, n.State.MaxHunger));
            float avgEnergy = needs.Length == 0 ? 0f : needs.Average(n => n.State.Energy);

            builder.AppendLine("=== CREATURES ===");
            builder.AppendLine($"count: {creatures.Length}");
            builder.AppendLine($"nav on/off: {onNavMesh}/{offNavMesh}");
            builder.AppendLine($"move/path: {moving}/{withPath}");
            builder.AppendLine($"hun/star/des: {hungry}/{starving}/{desperate}");
            builder.AppendLine($"avg hunger: {avgHunger:0.00}");
            builder.AppendLine($"avg energy: {avgEnergy:0.0}");
            builder.AppendLine();
        }

        private static void AppendSpeciesSummary(StringBuilder builder, CreatureAgentView[] creatures, Transform player)
        {
            builder.AppendLine("=== SPECIES ===");

            foreach (IGrouping<string, CreatureAgentView> group in creatures
                         .Where(c => c != null)
                         .GroupBy(c => string.IsNullOrWhiteSpace(c.CreatureId) ? "unknown" : c.CreatureId)
                         .OrderBy(g => g.Key))
            {
                int count = group.Count();
                int moving = group.Count(c =>
                {
                    NavMeshAgent agent = c.GetComponent<NavMeshAgent>();
                    return agent != null && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.01f;
                });
                int hungry = group.Count(c =>
                {
                    CreatureNeedsRuntime needs = c.GetComponent<CreatureNeedsRuntime>();
                    return needs != null && needs.State.IsHungry;
                });
                float nearestPlayer = player == null
                    ? -1f
                    : group.Min(c => Vector3.Distance(c.transform.position, player.position));

                builder.AppendLine($"{group.Key}: c={count} m={moving} h={hungry} ply={(nearestPlayer >= 0f ? nearestPlayer.ToString("0.0") : "n/a")}");
            }

            builder.AppendLine();
        }

        private static void AppendCreatureRows(StringBuilder builder, CreatureAgentView[] creatures, Transform player)
        {
            builder.AppendLine("=== CREATURES ===");

            foreach (CreatureAgentView creature in creatures.Where(c => c != null).OrderBy(c => c.CreatureId).Take(32))
            {
                CreatureNeedsRuntime needs = creature.GetComponent<CreatureNeedsRuntime>();
                CreatureFoodSeekingBehavior food = creature.GetComponent<CreatureFoodSeekingBehavior>();
                NavMeshAgent agent = creature.GetComponent<NavMeshAgent>();

                string id = string.IsNullOrWhiteSpace(creature.CreatureId) ? "unk" : creature.CreatureId;
                string stage = needs != null ? needs.State.Stage.ToString().Substring(0, Mathf.Min(3, needs.State.Stage.ToString().Length)).ToLowerInvariant() : "non";
                string hunger = needs != null ? $"{needs.State.Hunger:0}/{needs.State.MaxHunger:0}" : "n/a";
                string nav = agent == null ? "noa" : agent.isOnNavMesh ? $"on r:{agent.remainingDistance:0.0} v:{agent.velocity.magnitude:0.0}" : "off";
                string target = food != null && food.CurrentTarget != null ? $"{food.CurrentTarget.Kind} {Vector3.Distance(creature.transform.position, food.CurrentTarget.transform.position):0.0}m" : "none";
                string playerDistance = player != null ? $"{Vector3.Distance(creature.transform.position, player.position):0.0}m" : "n/a";
                CreatureBehaviorRuntime behavior = creature.GetComponent<CreatureBehaviorRuntime>();

                builder.AppendLine($"{id,-10} st={stage,-3} bh={(behavior != null ? behavior.State.ToString().ToLowerInvariant() : "leg"),-8} hu={hunger,-8} nv={nav,-14} tgt={target,-12} ply={playerDistance}");
            }

            if (creatures.Length > 32)
            {
                builder.AppendLine($"... {creatures.Length - 32} more creatures not shown");
            }
        }

        private static float AverageBiomass(FoodSourceView[] foods, FoodKind kind)
        {
            FoodSourceView[] matching = foods.Where(f => f != null && f.Kind == kind).ToArray();
            return matching.Length == 0 ? 0f : matching.Average(f => f.BiomassRatio);
        }

        private static Transform FindPlayer()
        {
            GameObject player = GameObject.Find("Player");
            return player != null ? player.transform : null;
        }
    }
}
