using ApexShift.Runtime.Debugging;
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
        [SerializeField] private float refreshIntervalSeconds = 0.35f;

        private CreatureAgentView agentView;
        private CreatureNeedsRuntime needs;
        private CreatureFoodSeekingBehavior foodSeeking;
        private CreatureBehaviorBrain behaviorRuntime;
        private NavMeshAgent navAgent;
        private CreatureSimulationLodRuntime simulationLod;
        private CreatureBehaviorState behaviorState = CreatureBehaviorState.Idle;
        private string cachedDebugText = string.Empty;
        private int cachedLineCount = 1;
        private float nextDebugTextRefreshTime;

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

        public void SetBehaviorState(CreatureBehaviorState state)
        {
            behaviorState = state;
        }

        private void OnGUI()
        {
            if (!GameSessionState.IsGameplayActive || !RuntimeDebugSettings.DebugEnabled || !RuntimeDebugSettings.CreatureFramesEnabled || HideAllDebugFrames || !showDebugFrame)
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

            string text = GetCachedDebugText();
            int lineCount = cachedLineCount;
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

        public void ForceRefreshForTests()
        {
            cachedDebugText = string.Empty;
            nextDebugTextRefreshTime = 0f;
        }

        private string GetCachedDebugText()
        {
            float interval = Mathf.Max(0.05f, refreshIntervalSeconds > 0f ? refreshIntervalSeconds : RuntimeDebugSettings.RefreshIntervalSeconds);
            if (string.IsNullOrEmpty(cachedDebugText) || Time.unscaledTime >= nextDebugTextRefreshTime)
            {
                CreatureDebugData data = CreatureDebugData.Capture(gameObject);
                behaviorState = data.state;
                cachedDebugText = data.ToOverlayText();
                cachedLineCount = Mathf.Max(1, cachedDebugText.Split('\n').Length);
                nextDebugTextRefreshTime = Time.unscaledTime + interval;
            }

            return cachedDebugText;
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
