using UnityEngine;

namespace ApexShift.Runtime.Creatures
{
    [RequireComponent(typeof(CreatureAgentView))]
    public sealed class CreaturePlayerAwarenessBehavior : MonoBehaviour
    {
        [SerializeField] private float decisionInterval = 0.20f;
        [SerializeField] private float preyFleeRange = 7f;
        [SerializeField] private float grazerFleeRange = 6f;
        [SerializeField] private float fleeDistance = 10f;
        [SerializeField] private float varnakChaseRange = 12f;
        [SerializeField] private float varnakStopDistance = 2.5f;

        private CreatureAgentView view;
        private CreatureBehaviorBrain behavior;
        private Transform player;
        private float decisionTimer;

        private void Awake()
        {
            if (view == null) view = GetComponent<CreatureAgentView>();
            if (behavior == null) behavior = GetComponent<CreatureBehaviorBrain>();
            ResolvePlayer();
        }

        private void Update()
        {
            if (view == null) view = GetComponent<CreatureAgentView>();
            if (behavior == null) behavior = GetComponent<CreatureBehaviorBrain>();
            if (view == null) return;

            decisionTimer -= Time.deltaTime;
if (decisionTimer > 0f)
            {
                return;
            }

            decisionTimer = Mathf.Max(0.05f, decisionInterval);

            if (player == null)
            {
                ResolvePlayer();
                if (player == null)
                {
                    return;
                }
            }

            string id = (view != null ? view.CreatureId : string.Empty) ?? string.Empty;
            float distance = Vector3.Distance(transform.position, player.position);
            CreatureBehaviorState currentState = behavior != null ? behavior.State : CreatureBehaviorState.Idle;

            if (id == "small_prey" && distance <= preyFleeRange)
            {
                FleeFromPlayer();
                return;
            }

            if (id == "grazer" && distance <= grazerFleeRange)
            {
                FleeFromPlayer();
                return;
            }

            if (id == "varnak" && IsPassiveState(currentState) && distance <= varnakChaseRange && distance > varnakStopDistance)
            {
                view.MoveTo(player.position);
            }
        }

        private static bool IsPassiveState(CreatureBehaviorState state)
        {
            return state == CreatureBehaviorState.Idle || state == CreatureBehaviorState.Wander;
        }

        private void FleeFromPlayer()
        {
            Vector3 direction = transform.position - player.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Random.insideUnitSphere;
                direction.y = 0f;
            }

            Vector3 target = transform.position + direction.normalized * fleeDistance;
            view.MoveTo(target);
        }

        private void ResolvePlayer()
        {
            GameObject playerObject = GameObject.Find("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        private void OnDrawGizmosSelected()
        {
            string id = view != null ? view.CreatureId : string.Empty;
            Gizmos.color = id == "varnak" ? Color.red : Color.cyan;
            float radius = id == "varnak" ? varnakChaseRange : Mathf.Max(preyFleeRange, grazerFleeRange);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
