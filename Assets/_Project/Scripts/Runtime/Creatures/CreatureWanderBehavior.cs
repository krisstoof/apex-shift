using UnityEngine;
using System.Collections;

namespace ApexShift.Runtime.Creatures
{
    [RequireComponent(typeof(CreatureAgentView))]
    public class CreatureWanderBehavior : MonoBehaviour
    {
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float waitTimeMin = 2f;
        [SerializeField] private float waitTimeMax = 5f;

        private CreatureAgentView _view;
        private bool _isWandering = false;

        private void Awake()
        {
            _view = GetComponent<CreatureAgentView>();
        }

        private void OnEnable()
        {
            StartCoroutine(WanderRoutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _isWandering = false;
        }

        public void Configure(float radius, float minWait, float maxWait)
        {
            wanderRadius = radius;
            waitTimeMin = minWait;
            waitTimeMax = maxWait;
        }

        private IEnumerator WanderRoutine()
        {
            _isWandering = true;
            
            while (_isWandering)
            {
                if (_view == null) _view = GetComponent<CreatureAgentView>();
                var adapter = _view != null ? _view.GetNavigationAdapter() : null;

                if (adapter != null && adapter.IsOnNavMesh)
                {
                    Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                    randomDirection += transform.position;
                    
                    if (adapter.TrySamplePosition(randomDirection, out Vector3 targetPos, wanderRadius))
                    {
                        Debug.Log($"[Wander] {gameObject.name} moving to {targetPos}");
                        _view.MoveTo(targetPos);

                        // Wait until reached or path invalid
                        float timeout = 10f;
                        while (!adapter.HasReachedDestination() && timeout > 0f)
                        {
                            timeout -= 0.5f;
                            yield return new WaitForSeconds(0.5f);
                        }
                    }
                }

                float waitTime = Random.Range(waitTimeMin, waitTimeMax);
                yield return new WaitForSeconds(waitTime);
            }
        }
}
}
