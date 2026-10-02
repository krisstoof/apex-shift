using UnityEngine;
using UnityEngine.Serialization;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Fire;
using ApexShift.Runtime.World.Query;
using System.Collections.Generic;

namespace ApexShift.Runtime.Creatures
{
    [RequireComponent(typeof(CreatureAgentView))]
    public sealed class CreaturePlayerAwarenessBehavior : MonoBehaviour
    {
        [SerializeField] private float decisionInterval = 0.20f;
        [SerializeField] private float preyFleeRange = 4.5f;
        [FormerlySerializedAs("grazerFleeRange")]
        [SerializeField] private float herbivoreFleeRange = 6f;
        [SerializeField] private float fleeDistance = 8f;
        [FormerlySerializedAs("varnakChaseRange")]
        [SerializeField] private float predatorChaseRange = 28f;
        [FormerlySerializedAs("varnakStopDistance")]
        [SerializeField] private float predatorStopDistance = 2.5f;
        [FormerlySerializedAs("varnakFireFearRangeMultiplier")]
        [SerializeField] private float fireFearRangeMultiplier = 1.45f;
        [FormerlySerializedAs("varnakFireFearMinimumRange")]
        [SerializeField] private float fireFearMinimumRange = 12f;
        [SerializeField] private float forcedThreatDuration = 1.25f;
        [SerializeField] private float navSampleDistance = 4f;

        private CreatureAgentView view;
        private CreatureBehaviorBrain behavior;
        private CreatureWanderBehavior wander;
        private Transform player;
        private float decisionTimer;
        private float forcedThreatTimer;
        private string creatureId;
        private static readonly List<CreatureAgentView> nearbyBuffer = new List<CreatureAgentView>(32);

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

            WorldQueryRuntime query = WorldQueryRuntime.Active;
            if (query == null || query.GetCreaturesInRadius(position, radius, nearbyBuffer) == 0)
            {
                return;
            }

            for (int i = 0; i < nearbyBuffer.Count; i++)
            {
                CreaturePlayerAwarenessBehavior item = nearbyBuffer[i] != null
                    ? nearbyBuffer[i].CachedAwareness
                    : null;
                if (item == null)
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

            CreatureRole role = view != null ? view.Role : GetComponent<CreatureAgentView>().Role;
            float distance = HorizontalDistance(transform.position, player.position);
            CreatureBehaviorState currentState = behavior != null ? behavior.State : CreatureBehaviorState.Idle;
            bool forced = forcedThreatTimer > 0f;

            if ((role == CreatureRole.SmallPrey) && (forced || distance <= preyFleeRange))
            {
                FleeFromPlayer(forced ? "combat_threat" : $"player_near d:{distance:0.0}");
                return;
            }

            if ((role == CreatureRole.HerbivoreOmnivore) && (forced || distance <= herbivoreFleeRange))
            {
                FleeFromPlayer(forced ? "combat_threat" : $"player_near d:{distance:0.0}");
                return;
            }

            if (role == CreatureRole.Predator && TryAvoidFire(distance))
            {
                return;
            }

            if (role == CreatureRole.Predator && (forced || IsPassiveState(currentState)) && distance <= predatorChaseRange)
            {
                if (distance > predatorStopDistance)
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

            if (!forced && (role == CreatureRole.SmallPrey || role == CreatureRole.HerbivoreOmnivore))
            {
                RestoreWanderIfSafe(distance, role);
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

        private bool TryAvoidFire(float playerDistance)
        {
            if (!FireSourceRegistry.TryGetStrongestSource(transform.position, fireFearRangeMultiplier, out FireSourceRuntime source))
            {
                return false;
            }

            Vector3 direction = transform.position - source.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Random.insideUnitSphere;
                direction.y = 0f;
            }

            float radius = Mathf.Max(1f, source.ProtectionRadius);
            float fleeRange = Mathf.Max(fireFearMinimumRange, radius * Mathf.Max(1f, fireFearRangeMultiplier));
            Vector3 target = transform.position + direction.normalized * fleeRange;
            CreatureNavigationAdapter adapter = view.GetNavigationAdapter();
            if (adapter != null && adapter.TrySamplePosition(target, out Vector3 sampled, navSampleDistance))
            {
                target = sampled;
            }

            behavior?.SetBehaviorStateForTests(CreatureBehaviorState.Flee, $"fire_source:{source.SourceId}");
            view.MoveTo(target);
            GameEventBus.PublishCreatureEvent(
                GameplayEventKind.VarnakScaredByFire,
                transform.position,
                "global",
                view.SpeciesId,
                source.SourceId,
                amount: Mathf.Max(0f, playerDistance),
                message: "varnak_scared_by_fire");
            return true;
        }

        private void RestoreWanderIfSafe(float distance, CreatureRole role)
        {
            if (wander == null || wander.enabled)
            {
                return;
            }

            float safeDistance = role == CreatureRole.HerbivoreOmnivore
                ? herbivoreFleeRange * 1.10f
                : preyFleeRange * 1.10f;

            if (distance > safeDistance)
            {
                wander.enabled = true;
            }
        }

        private void ResolvePlayer()
        {
            player = PlayerPresenceRuntime.ActiveTransform;
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
            CreatureRole role = view != null ? view.Role : GetComponent<CreatureAgentView>().Role;
            Gizmos.color = role == CreatureRole.Predator ? Color.red : Color.cyan;
            float radius = role == CreatureRole.Predator ? predatorChaseRange : Mathf.Max(preyFleeRange, herbivoreFleeRange);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
