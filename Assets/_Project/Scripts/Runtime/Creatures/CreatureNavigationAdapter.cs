using UnityEngine;
using UnityEngine.AI;

namespace ApexShift.Runtime.Creatures
{
    public class CreatureNavigationAdapter : MonoBehaviour
    {
        private NavMeshAgent _agent;
        private bool hasValidNavMesh;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            hasValidNavMesh = WarpToNearestNavMesh();
        }

        public bool IsOnNavMesh => _agent != null && _agent.isOnNavMesh;

        public bool TryMoveTo(Vector3 destination)
{
            if (!hasValidNavMesh || _agent == null || !_agent.isOnNavMesh) return false;
            _agent.isStopped = false;
            return _agent.SetDestination(destination);
        }

        public bool TrySamplePosition(Vector3 sourcePosition, out Vector3 hitPosition, float maxDistance = 5f)
        {
            if (NavMesh.SamplePosition(sourcePosition, out NavMeshHit hit, maxDistance, NavMesh.AllAreas))
            {
                hitPosition = hit.position;
                return true;
            }

            hitPosition = sourcePosition;
            return false;
        }

        public bool WarpToNearestNavMesh()
        {
            if (_agent == null)
            {
                return false;
            }

            if (TrySamplePosition(transform.position, out Vector3 hitPosition, 10f))
            {
                _agent.Warp(hitPosition);
                hasValidNavMesh = true;
                return true;
            }

            hasValidNavMesh = false;
            return false;
        }

        public bool HasReachedDestination(float stoppingDistance = 0.5f)
        {
            if (!hasValidNavMesh || _agent == null || _agent.pathPending) return false;
            return _agent.remainingDistance <= (_agent.stoppingDistance > 0 ? _agent.stoppingDistance : stoppingDistance);
        }

        public void Stop()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        public void ConfigureMovement(float speed, float acceleration, float stoppingDistance = 0.5f)
        {
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            _agent.speed = speed;
            _agent.acceleration = acceleration;
            _agent.stoppingDistance = stoppingDistance;
        }
    }
}
