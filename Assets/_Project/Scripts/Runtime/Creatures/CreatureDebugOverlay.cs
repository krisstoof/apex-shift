using ApexShift.Runtime.Ecosystem;
using UnityEngine;
using UnityEngine.AI;

namespace ApexShift.Runtime.Creatures
{
    [DisallowMultipleComponent]
    public sealed class CreatureDebugOverlay : MonoBehaviour
    {
        public static bool HideAllDebugFrames { get; set; } = false;

        [SerializeField] private bool showDebugFrame = true;
        [SerializeField] private float maxDrawDistance = 90f;
        [SerializeField] private float verticalOffset = 2.25f;
        [SerializeField] private int width = 220;
        [SerializeField] private int height = 118;

        private CreatureAgentView agentView;
        private CreatureNeedsRuntime needs;
        private CreatureFoodSeekingBehavior foodSeeking;
        private NavMeshAgent navAgent;

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
            navAgent = GetComponent<NavMeshAgent>();
        }

        private void OnGUI()
        {
            if (HideAllDebugFrames || !showDebugFrame)
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

            Rect rect = new Rect(
                screenPosition.x - width * 0.5f,
                Screen.height - screenPosition.y,
                width,
                height);

            GUI.Box(rect, BuildDebugText());
        }

        private string BuildDebugText()
        {
            string creatureId = agentView != null ? agentView.CreatureId : gameObject.name;
            string behavior = GetBehaviorLabel();
            string nav = GetNavigationLabel();
            string target = GetTargetLabel();

            string hunger = "n/a";
            string energy = "n/a";
            string diet = "n/a";

            if (needs != null)
            {
                hunger = $"{needs.State.Stage} {needs.State.Hunger:0}/{needs.State.MaxHunger:0}";
                energy = $"{needs.State.Energy:0}";
                diet = $"P:{needs.Diet.PlantPreference:0.00} M:{needs.Diet.MeatPreference:0.00} S:{needs.Diet.ScavengerPreference:0.00}";
            }

            return
                $"{creatureId}\n" +
                $"behavior: {behavior}\n" +
                $"hunger: {hunger}  energy: {energy}\n" +
                $"diet: {diet}\n" +
                $"nav: {nav}\n" +
                $"target: {target}";
        }

        private string GetBehaviorLabel()
        {
            if (navAgent == null)
            {
                return "no_nav_agent";
            }

            if (!navAgent.isOnNavMesh)
            {
                return "off_navmesh";
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
            if (foodSeeking == null || foodSeeking.CurrentTarget == null)
            {
                return "none";
            }

            FoodSourceView target = foodSeeking.CurrentTarget;
            float distance = Vector3.Distance(transform.position, target.transform.position);
            return $"{target.Kind} d:{distance:0.0} biomass:{target.BiomassRatio:0.00}";
        }
    }
}
