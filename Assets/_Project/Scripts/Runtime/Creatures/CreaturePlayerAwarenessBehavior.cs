using UnityEngine;
using ApexShift.Runtime.Player;

namespace ApexShift.Runtime.Creatures
{
    [RequireComponent(typeof(CreatureAgentView))]
    public sealed class CreaturePlayerAwarenessBehavior : MonoBehaviour
    {
        [SerializeField] private float decisionInterval = 0.20f;
        [SerializeField] private float preyFleeRange = 4.5f;
        [SerializeField] private float grazerFleeRange = 6f;
        [SerializeField] private float fleeDistance = 8f;
        [SerializeField] private float varnakChaseRange = 28f;
        [SerializeField] private float varnakStopDistance = 2.5f;
        [SerializeField] private float forcedThreatDuration = 1.25f;
        [SerializeField] private float navSampleDistance = 4f;

        private CreatureAgentView view;
        private CreatureBehaviorBrain behavior;
        private CreatureWanderBehavior wander;
        private Transform player;
        private float decisionTimer;
        private float forcedThreatTimer;
        private string creatureId;

        private void Awake()
        {
            Cache();
            ResolvePlayer();
        }

        public void Configure(string id)
        {
            creatureId = string.IsNullOrWhiteSpace(id) ? creatureId : id.Trim().ToLowerInvariant();
        }

        public static void NotifyNearby(Vector3 position, Transform player, float radius, float intensity, string reason)
        {
            if (radius <= 0f)
            {
                return;
            }

            CreaturePlayerAwarenessBehavior[] awareness = Object.FindObjectsByType<CreaturePlayerAwarenessBehavior>(FindObjectsInactive.Exclude);
            foreach (CreaturePlayerAwarenessBehavior item in awareness)
            {
                if (item == null || Vector3.Distance(position, item.transform.position) > radius)
                {
                    continue;
                }

                item.NotifyPlayerThreat(player, intensity, reason);
            }
        }

        public static void NotifyCreatureHit(CreatureHealthRuntime health, Transform player, float intensity, string reason)
        {
            if (health == null)
            {
                return;
            }

            health.GetComponent<CreaturePlayerAwarenessBehavior>()?.NotifyPlayerThreat(player, intensity, reason);
        }

        private void Update()
        {
            Cache();
            if (view == null) return;

            decisionTimer -= Time.deltaTime;
            if (decisionTimer > 0f)
            {
                return;
            }

            decisionTimer = Mathf.Max(0.05f, decisionInterval);
            if (forcedThreatTimer > 0f)
            {
                forcedThreatTimer = Mathf.Max(0f, forcedThreatTimer - decisionInterval);
            }

            if (player == null)
            {
                ResolvePlayer();
                if (player == null)
                {
                    return;
                }
            }

            string id = ResolveCreatureId();
            float distance = HorizontalDistance(transform.position, player.position);
            CreatureBehaviorState currentState = behavior != null ? behavior.State : CreatureBehaviorState.Idle;
            bool forced = forcedThreatTimer > 0f;

            if ((id == "small_prey" || id == "prey") && (forced || distance <= preyFleeRange))
            {
                FleeFromPlayer(forced ? "combat_threat" : $"player_near d:{distance:0.0}");
                return;
            }

            if ((id == "grazer" || id == "deer") && (forced || distance <= grazerFleeRange))
            {
                FleeFromPlayer(forced ? "combat_threat" : $"player_near d:{distance:0.0}");
                return;
            }

            if (id == "varnak" && (forced || IsPassiveState(currentState)) && distance <= varnakChaseRange)
            {
                if (distance > varnakStopDistance)
                {
                    behavior?.SetBehaviorStateForTests(CreatureBehaviorState.Chase, forced ? "player_combat_noise" : $"player_detected d:{distance:0.0}");
                    view.MoveTo(player.position);
                }
                else
                {
                    behavior?.SetBehaviorStateForTests(CreatureBehaviorState.Attack, "player_in_attack_range");
                    view.Stop();
                }
            }

            if (!forced && (id == "small_prey" || id == "prey" || id == "grazer" || id == "deer"))
            {
                RestoreWanderIfSafe(distance, id);
            }
        }

        private static bool IsPassiveState(CreatureBehaviorState state)
        {
            return state == CreatureBehaviorState.Idle || state == CreatureBehaviorState.Wander;
        }

        public void NotifyPlayerThreat(Transform sourcePlayer, float intensity, string reason)
        {
            if (sourcePlayer != null)
            {
                player = sourcePlayer;
            }
            else
            {
                ResolvePlayer();
            }

            forcedThreatTimer = Mathf.Max(forcedThreatTimer, forcedThreatDuration * Mathf.Clamp01(Mathf.Max(0.1f, intensity)));
        }

        private void Cache()
        {
            if (view == null) view = GetComponent<CreatureAgentView>();
            if (behavior == null) behavior = GetComponent<CreatureBehaviorBrain>();
            if (wander == null) wander = GetComponent<CreatureWanderBehavior>();
            if (string.IsNullOrWhiteSpace(creatureId) && view != null)
            {
                creatureId = view.CreatureId;
            }
        }

        private void FleeFromPlayer(string reason)
        {
            if (player == null || view == null) return;
            Vector3 direction = transform.position - player.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Random.insideUnitSphere;
                direction.y = 0f;
            }

            Vector3 target = transform.position + direction.normalized * fleeDistance;
            CreatureNavigationAdapter adapter = view.GetNavigationAdapter();
            if (adapter != null && adapter.TrySamplePosition(target, out Vector3 sampled, navSampleDistance))
            {
                target = sampled;
            }

            if (wander != null && wander.enabled)
            {
                wander.enabled = false;
            }

            behavior?.SetBehaviorStateForTests(CreatureBehaviorState.Flee, reason);
            view.MoveTo(target);
        }

        private void RestoreWanderIfSafe(float distance, string id)
        {
            if (wander == null || wander.enabled)
            {
                return;
            }

            float safeDistance = id == "grazer" || id == "deer"
                ? grazerFleeRange * 1.10f
                : preyFleeRange * 1.10f;

            if (distance > safeDistance)
            {
                wander.enabled = true;
            }
        }

        private void ResolvePlayer()
        {
            player = PlayerPresenceRuntime.ResolveFallback();
        }

        private string ResolveCreatureId()
        {
            if (!string.IsNullOrWhiteSpace(creatureId))
            {
                return creatureId.Trim().ToLowerInvariant();
            }

            creatureId = (view != null ? view.CreatureId : string.Empty) ?? string.Empty;
            return creatureId.Trim().ToLowerInvariant();
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void OnDrawGizmosSelected()
        {
            string id = ResolveCreatureId();
            Gizmos.color = id == "varnak" ? Color.red : Color.cyan;
            float radius = id == "varnak" ? varnakChaseRange : Mathf.Max(preyFleeRange, grazerFleeRange);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
