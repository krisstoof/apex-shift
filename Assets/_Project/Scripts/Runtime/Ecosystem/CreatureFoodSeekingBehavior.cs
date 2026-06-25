using ApexShift.Runtime.Creatures;
using UnityEngine;

namespace ApexShift.Runtime.Ecosystem
{
    [RequireComponent(typeof(CreatureNeedsRuntime))]
    [RequireComponent(typeof(CreatureAgentView))]
    public class CreatureFoodSeekingBehavior : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 2.2f;
        [SerializeField] private float consumptionRate = 3f;
        [SerializeField] private float targetRefreshInterval = 0.50f;
        [SerializeField] private float navSampleDistance = 5f;

        private CreatureNeedsRuntime _needs;
        private CreatureAgentView _view;
        private CreatureWanderBehavior _wander;
        private FoodSourceView _currentTarget;
        private bool _isEating;
        private float _targetRefreshTimer;

        public FoodSourceView CurrentTarget => _currentTarget;
        public bool HasTarget => _currentTarget != null && !_currentTarget.IsEmpty;
        public bool IsEating => _isEating;

        private void Awake()
        {
            _needs = GetComponent<CreatureNeedsRuntime>();
            _view = GetComponent<CreatureAgentView>();
            _wander = GetComponent<CreatureWanderBehavior>();
        }

        private void Update()
        {
            _isEating = false;

            if (_needs == null || _view == null)
            {
                return;
            }

            if (!_needs.State.IsHungry)
            {
                _currentTarget = null;
                SetWanderEnabled(true);
                return;
            }

            SetWanderEnabled(false);

            _targetRefreshTimer -= Time.deltaTime;
            if (_currentTarget == null || _currentTarget.IsEmpty || _targetRefreshTimer <= 0f)
            {
                _currentTarget = FindFood();
                _targetRefreshTimer = Mathf.Max(0.1f, targetRefreshInterval);
            }

            if (_currentTarget == null)
            {
                return;
            }

            float dist = HorizontalDistance(transform.position, _currentTarget.transform.position);
            if (dist <= interactionRange)
            {
                _view.Stop();
                _isEating = true;
                float nutrition = _currentTarget.Consume(consumptionRate * Time.deltaTime);
                _needs.Eat(_currentTarget.Kind, nutrition);
                return;
            }

            CreatureNavigationAdapter adapter = _view.GetNavigationAdapter();
            if (adapter != null && adapter.TrySamplePosition(_currentTarget.transform.position, out Vector3 navTarget, navSampleDistance))
            {
                _view.MoveTo(navTarget);
            }
            else
            {
                _currentTarget = null;
            }
        }

        private FoodSourceView FindFood()
        {
            return _needs != null && _needs.TryFindPreferredFood(EcosystemRuntime.Instance, out FoodSourceView food)
                ? food
                : null;
        }

        private void SetWanderEnabled(bool enabled)
        {
            if (_wander != null && _wander.enabled != enabled)
            {
                _wander.enabled = enabled;
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            Vector2 aa = new Vector2(a.x, a.z);
            Vector2 bb = new Vector2(b.x, b.z);
            return Vector2.Distance(aa, bb);
        }
    }
}
