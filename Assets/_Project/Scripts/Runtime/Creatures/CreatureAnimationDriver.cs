using UnityEngine;
using UnityEngine.AI;

namespace ApexShift.Runtime.Creatures
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class CreatureAnimationDriver : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private string verticalParamName = "Vert";
        [SerializeField] private string stateParamName = "State";
        [SerializeField] private float transitionSpeed = 4.5f;
        [SerializeField] private float runSpeedThreshold = 2.0f;

        private Animator _animator;
        private NavMeshAgent _agent;

        private float _currentVert;
        private float _currentState;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();
        }

        public void Configure(float runThreshold, float speedMultiplier = 1f)
        {
            runSpeedThreshold = runThreshold;
        }

        private void Update()
        {
            if (_animator == null || _agent == null) return;

            // Get current agent speed
            float currentSpeed = _agent.velocity.magnitude;
            float maxSpeed = _agent.speed;

            // Calculate target animation parameters
            float targetVert = 0f;
            float targetState = 0f;

            if (currentSpeed > 0.05f && maxSpeed > 0.01f)
            {
                // Set Vert to represent movement intensity
                targetVert = Mathf.Clamp01(currentSpeed / maxSpeed);

                // If moving faster than threshold, transition to running state (1), otherwise walking state (0)
                if (currentSpeed > runSpeedThreshold)
                {
                    targetState = 1f;
                }
                else
                {
                    targetState = 0f;
                }
            }

            // Smoothly interpolate current parameter values
            _currentVert = Mathf.MoveTowards(_currentVert, targetVert, transitionSpeed * Time.deltaTime);
            _currentState = Mathf.MoveTowards(_currentState, targetState, transitionSpeed * Time.deltaTime);

            // Apply to Animator
            _animator.SetFloat(verticalParamName, _currentVert);
            _animator.SetFloat(stateParamName, _currentState);
        }
    }
}