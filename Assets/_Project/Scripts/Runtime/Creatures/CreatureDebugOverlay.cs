using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Flow;
using UnityEngine;
using UnityEngine.AI;

namespace ApexShift.Runtime.Creatures
{
    [DisallowMultipleComponent]
    public sealed class CreatureDebugOverlay : MonoBehaviour
    {
        public static bool HideAllDebugFrames { get; set; } = false;

        [SerializeField] private bool showDebugFrame = true;
        [SerializeField] private float maxDrawDistance = 50f;
        [SerializeField] private float verticalOffset = 1.8f;
        [SerializeField] private int width = 160;
        [SerializeField] private int height = 80;

        private CreatureAgentView agentView;
        private CreatureNeedsRuntime needs;
        private CreatureFoodSeekingBehavior foodSeeking;
        private CreatureBehaviorBrain behaviorRuntime;
        private NavMeshAgent navAgent;
        private CreatureSimulationLodRuntime simulationLod;
        private CreatureBehaviorState behaviorState = CreatureBehaviorState.Idle;

        private void Awake()
        {
            Cache();
        }

        private void OnEnable()
        {
            Cache();
        }

        private void Cache()
        {
            agentView = GetComponent<CreatureAgentView>();
            needs = GetComponent<CreatureNeedsRuntime>();
            foodSeeking = GetComponent<CreatureFoodSeekingBehavior>();
            behaviorRuntime = GetComponent<CreatureBehaviorBrain>();
            navAgent = GetComponent<NavMeshAgent>();
            simulationLod = GetComponent<CreatureSimulationLodRuntime>();
        }

        private void EnsureRuntimeReferences()
        {
            if (agentView == null) agentView = GetComponent<CreatureAgentView>();
            if (needs == null) needs = GetComponent<CreatureNeedsRuntime>();
            if (behaviorRuntime == null) behaviorRuntime = GetComponent<CreatureBehaviorBrain>();
            if (navAgent == null) navAgent = GetComponent<NavMeshAgent>();
            if (simulationLod == null) simulationLod = GetComponent<CreatureSimulationLodRuntime>();
        }

        public void SetBehaviorState(CreatureBehaviorState state)
        {
            behaviorState = state;
        }

        private void OnGUI()
        {
            if (!GameSessionState.IsGameplayActive || HideAllDebugFrames || !showDebugFrame)
            {
                return;
            }

            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (camera == null)
            {
                return;
            }

            Vector3 worldPosition = transform.position + Vector3.up * verticalOffset;
            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
            {
                return;
            }

            float distance = Vector3.Distance(camera.transform.position, transform.position);
            if (distance > maxDrawDistance)
            {
                return;
            }

            string text = BuildDebugText();
            int lineCount = text.Split('\n').Length;
            int dynamicHeight = Mathf.Max(height, 18 + lineCount * 16);

            Rect rect = new Rect(
                screenPosition.x - width * 0.5f,
                Screen.height - screenPosition.y,
                width,
                dynamicHeight);

            Color oldColor = GUI.color;
            GUI.color = GetFrameColor();
            GUI.Box(rect, text);
            GUI.color = oldColor;
        }

        private string BuildDebugText()
        {
            EnsureRuntimeReferences();

            string creatureId = agentView != null ? agentView.CreatureId : gameObject.name;
            string behavior = GetBehaviorLabel();
            string nav = GetNavigationLabel();
            string target = GetTargetLabel();

            string hunger = "n/a";
            string energy = "n/a";
            string diet = "n/a";
            string hungry = "n/a";
            string lastFood = "n/a";

            if (needs != null)
            {
                hunger = $"{needs.State.Stage} {needs.State.Hunger:0}/{needs.State.MaxHunger:0}";
                energy = $"{needs.State.Energy:0}";
                diet = $"P:{needs.Diet.PlantPreference:0.00} M:{needs.Diet.MeatPreference:0.00} S:{needs.Diet.ScavengerPreference:0.00}";
                hungry = needs.IsHungry ? "true" : "false";
            }

            string extra = "";
            if (behaviorRuntime != null)
            {
                string lod = "";
                if (simulationLod != null && simulationLod.DebugEnabled)
                {
                    lod =
                        $"\nlod: {simulationLod.LevelName} d:{simulationLod.DistanceToPlayer:0.0} c:{simulationLod.LodChangeCount}" +
                        $"\ntick: a:{simulationLod.ActiveSimulationTickCount}" +
                        $" f:{simulationLod.FarSimulationTickCount}" +
                        $" b:{simulationLod.BackgroundSimulationTickCount}";
                    if (simulationLod.IsVisibilityCulled)
                    {
                        lod += " bg";
                    }
                }

                lastFood = ShortenLastFoodSource(behaviorRuntime.LastFoodSource);
                extra =
                    $"\nwhy: {ShortenDecisionReason(behaviorRuntime.DecisionReason)}" +
                    $"\nlast: {lastFood}" +
                    $"\nniche: {behaviorRuntime.CurrentNiche}" +
                    $"\ndec: {behaviorRuntime.DecisionCount} atk:{behaviorRuntime.AttackCooldown:0.0}";
                extra += lod;
            }

            return
                $"{creatureId}\n" +
                $"beh: {behavior}\n" +
                $"hun: {hunger} hungry:{hungry} en:{energy}\n" +
                $"diet: {diet}\n" +
                $"nav: {nav}\n" +
                $"tgt: {target}" +
                extra;
        }

        private string GetBehaviorLabel()
        {
            EnsureRuntimeReferences();

            if (navAgent == null)
            {
                return "no_nav_agent";
            }

            if (!navAgent.isOnNavMesh)
            {
                return "off_navmesh";
            }

            if (behaviorRuntime != null)
            {
                behaviorState = behaviorRuntime.State;
                return behaviorRuntime.State.ToString().ToLowerInvariant();
            }

            if (foodSeeking != null && foodSeeking.HasTarget)
            {
                return foodSeeking.IsEating ? "eat_food" : "seek_food";
            }

            if (navAgent.hasPath && navAgent.remainingDistance > navAgent.stoppingDistance + 0.1f)
            {
                return "move/wander";
            }

            if (needs != null && needs.IsHungry)
            {
                return "hungry_idle";
            }

            return "idle";
        }

        private string GetNavigationLabel()
        {
            if (navAgent == null)
            {
                return "missing";
            }

            return navAgent.isOnNavMesh
                ? $"on path:{navAgent.hasPath} rem:{navAgent.remainingDistance:0.0} vel:{navAgent.velocity.magnitude:0.0}"
                : "off navmesh";
        }

        private string GetTargetLabel()
        {
            EnsureRuntimeReferences();

            if (behaviorRuntime != null)
            {
                string label = behaviorRuntime.CurrentTargetLabel;
                if (behaviorRuntime.CurrentTargetTransform != null)
                {
                    float targetDistance = Vector3.Distance(transform.position, behaviorRuntime.CurrentTargetTransform.position);
                    return $"{label} {targetDistance:0.0}m";
                }

                return label;
            }

            if (foodSeeking == null || foodSeeking.CurrentTarget == null)
            {
                return "none";
            }

            FoodSourceView target = foodSeeking.CurrentTarget;
            float distance = Vector3.Distance(transform.position, target.transform.position);
            return $"{target.Kind} {distance:0.0}m {target.SourceId}";
        }

        private Color GetFrameColor()
        {
            if (behaviorRuntime != null)
            {
                Color feedColor = GetLastFoodColor();
                if (feedColor.a > 0f)
                {
                    return feedColor;
                }
            }

            switch (behaviorState)
            {
                case CreatureBehaviorState.HuntPrey:
                    return new Color(0.95f, 0.38f, 0.28f, 0.92f);
                case CreatureBehaviorState.Flee:
                    return new Color(0.98f, 0.82f, 0.22f, 0.92f);
                case CreatureBehaviorState.SeekFood:
                    return new Color(0.38f, 0.82f, 0.42f, 0.92f);
                case CreatureBehaviorState.Eat:
                    return new Color(0.45f, 0.92f, 0.70f, 0.92f);
                case CreatureBehaviorState.Dead:
                    return new Color(0.60f, 0.60f, 0.60f, 0.85f);
                case CreatureBehaviorState.Wander:
                    return new Color(0.52f, 0.76f, 0.98f, 0.88f);
                default:
                    return new Color(0.82f, 0.82f, 0.82f, 0.88f);
            }
        }

        private Color GetLastFoodColor()
        {
            if (behaviorRuntime == null)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            string lastFood = (behaviorRuntime.LastFoodSource ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(lastFood) || lastFood == "none")
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            if (lastFood.Contains("meat") || lastFood.Contains("hunted") || lastFood.Contains("scaven"))
            {
                return new Color(0.92f, 0.36f, 0.24f, 0.92f);
            }

            if (lastFood.Contains("plant") || lastFood.Contains("berry") || lastFood.Contains("bush") || lastFood.Contains("grass"))
            {
                return new Color(0.34f, 0.76f, 0.36f, 0.92f);
            }

            return new Color(0.48f, 0.68f, 0.96f, 0.88f);
        }

        private static string ShortenLastFoodSource(string source)
        {
            string value = (source ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value) || value == "none")
            {
                return "none";
            }

            if (value.Contains("hunted") || value.Contains("meat") || value.Contains("scaven"))
            {
                return "meat";
            }

            if (value.Contains("plant") || value.Contains("berry") || value.Contains("bush") || value.Contains("grass") || value.Contains("leaf"))
            {
                return "plant";
            }

            if (value.Contains("hunt"))
            {
                return "hunt";
            }

            if (value.Contains("scaven"))
            {
                return "scavenge";
            }

            return value.Length > 12 ? value.Substring(0, 12) : value;
        }

        private static string ShortenDecisionReason(string reason)
        {
            string value = (reason ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value))
            {
                return "none";
            }

            if (value.Contains("hungry_no_meat_or_prey"))
            {
                return "need_food";
            }

            if (value.Contains("varnak_not_hungry"))
            {
                return "idle";
            }

            if (value.Contains("attack_cooldown"))
            {
                return "atk_cd";
            }

            if (value.Contains("eat_cooldown"))
            {
                return "eat_cd";
            }

            if (value.Contains("flee_varnak"))
            {
                return "flee_v";
            }

            if (value.Contains("flee_player"))
            {
                return "flee_p";
            }

            if (value.Contains("hunt_prey"))
            {
                return "hunt";
            }

            if (value.Contains("seek_food"))
            {
                return "seek";
            }

            if (value.Contains("eat_food"))
            {
                return "eat";
            }

            if (value.Length > 12)
            {
                return value.Substring(0, 12);
            }

            return value;
        }
    }
}
