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
                Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
                randomDirection += transform.position;
                
                if (_view.GetNavigationAdapter().TrySamplePosition(randomDirection, out Vector3 targetPos, wanderRadius))
                {
                    _view.MoveTo(targetPos);

                    // Wait until reached or path invalid
                    while (!_view.GetNavigationAdapter().HasReachedDestination())
                    {
                        yield return new WaitForSeconds(0.5f);
                    }
                }

                float waitTime = Random.Range(waitTimeMin, waitTimeMax);
                yield return new WaitForSeconds(waitTime);
            }
        }
    }
}
