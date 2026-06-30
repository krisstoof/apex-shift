using UnityEngine;
using UnityEngine.AI;

namespace ApexShift.Runtime.Creatures
{
    [RequireComponent(typeof(Animator))]
    public sealed class CreatureAnimationDriver : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private string verticalParamName = "Vert";
        [SerializeField] private string stateParamName = "State";
        [SerializeField] private string eatTriggerName = "Eat";
        [SerializeField] private string attackTriggerName = "Attack";
        [SerializeField] private string deadBoolName = "Dead";
        [SerializeField] private float transitionSpeed = 4.5f;
        [SerializeField] private float runSpeedThreshold = 2.0f;

        private Animator _animator;
        private NavMeshAgent _agent;
        private CreatureNavigationAdapter _navigationAdapter;

        private float _currentVert;
        private float _currentState;
        private CreatureBehaviorBrain _behavior;
        private CreatureBehaviorState _lastState;

        public float CurrentState => _currentState;

        private void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            _agent = GetComponent<NavMeshAgent>();
            _navigationAdapter = GetComponent<CreatureNavigationAdapter>();
            _behavior = GetComponent<CreatureBehaviorBrain>();
        }

        public void Configure(float runThreshold, float speedMultiplier = 1f)
        {
            runSpeedThreshold = runThreshold;
        }

        private void Update()
        {
            if (_animator == null)
            {
                return;
            }

            if (_behavior == null)
            {
                _behavior = GetComponent<CreatureBehaviorBrain>();
            }

            float currentSpeed = _agent != null ? _agent.velocity.magnitude : 0f;
            float maxSpeed = _agent != null ? _agent.speed : 0f;
            if (_navigationAdapter != null && _agent == null)
            {
                currentSpeed = 0f;
                maxSpeed = 0f;
            }
            float targetVert = 0f;
            float targetState = 0f;
            CreatureBehaviorState state = _behavior != null ? _behavior.State : CreatureBehaviorState.Idle;

            if (_behavior != null)
            {
                switch (state)
                {
                    case CreatureBehaviorState.HuntPrey:
                    case CreatureBehaviorState.HuntSmallPrey:
                    case CreatureBehaviorState.SeekFood:
                    case CreatureBehaviorState.Scavenge:
                    case CreatureBehaviorState.Stalk:
                    case CreatureBehaviorState.Chase:
                    case CreatureBehaviorState.Wander:
                        targetVert = Mathf.Clamp01(currentSpeed / Mathf.Max(0.01f, maxSpeed));
                        break;
                    case CreatureBehaviorState.Flee:
                        targetVert = Mathf.Clamp01((currentSpeed / Mathf.Max(0.01f, maxSpeed)) + 0.25f);
                        break;
                    case CreatureBehaviorState.Eat:
                    case CreatureBehaviorState.EatPlants:
                    case CreatureBehaviorState.EatMeat:
                        targetVert = 0.05f;
                        break;
                }

                switch (state)
                {
                    case CreatureBehaviorState.Idle:
                        targetState = 0f;
                        break;
                    case CreatureBehaviorState.Wander:
                        targetState = 0.15f;
                        break;
                    case CreatureBehaviorState.SeekFood:
                    case CreatureBehaviorState.Scavenge:
                    case CreatureBehaviorState.Stalk:
                        targetState = 0.35f;
                        break;
                    case CreatureBehaviorState.HuntPrey:
                    case CreatureBehaviorState.HuntSmallPrey:
                    case CreatureBehaviorState.Chase:
                        targetState = 0.6f;
                        break;
                    case CreatureBehaviorState.Attack:
                        targetState = 0.75f;
                        break;
                    case CreatureBehaviorState.Flee:
                        targetState = 0.85f;
                        break;
                    case CreatureBehaviorState.Eat:
                    case CreatureBehaviorState.EatPlants:
                    case CreatureBehaviorState.EatMeat:
                        targetState = 1f;
                        break;
                    case CreatureBehaviorState.Dead:
                        targetState = 0f;
                        targetVert = 0f;
                        break;
                }

                if (state != _lastState)
                {
                    if ((state == CreatureBehaviorState.Eat || state == CreatureBehaviorState.EatPlants || state == CreatureBehaviorState.EatMeat) &&
                        HasParameter(eatTriggerName, AnimatorControllerParameterType.Trigger))
                    {
                        _animator.SetTrigger(eatTriggerName);
                    }

                    if ((state == CreatureBehaviorState.HuntPrey || state == CreatureBehaviorState.HuntSmallPrey || state == CreatureBehaviorState.Attack) &&
                        HasParameter(attackTriggerName, AnimatorControllerParameterType.Trigger))
                    {
                        _animator.SetTrigger(attackTriggerName);
                    }

                    if (HasParameter(deadBoolName, AnimatorControllerParameterType.Bool))
                    {
                        _animator.SetBool(deadBoolName, state == CreatureBehaviorState.Dead);
                    }

                    _lastState = state;
                }
            }
            else if (currentSpeed > 0.05f && maxSpeed > 0.01f)
            {
                targetVert = Mathf.Clamp01(currentSpeed / maxSpeed);
                targetState = currentSpeed > runSpeedThreshold ? 1f : 0f;
            }

            _currentVert = Mathf.MoveTowards(_currentVert, targetVert, transitionSpeed * Time.deltaTime);
            _currentState = Mathf.MoveTowards(_currentState, targetState, transitionSpeed * Time.deltaTime);

            if (HasParameter(verticalParamName, AnimatorControllerParameterType.Float))
            {
                _animator.SetFloat(verticalParamName, _currentVert);
            }

            if (HasParameter(stateParamName, AnimatorControllerParameterType.Float))
            {
                _animator.SetFloat(stateParamName, _currentState);
            }
        }

        private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null || string.IsNullOrWhiteSpace(parameterName))
            {
                return false;
            }

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.type == type && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
