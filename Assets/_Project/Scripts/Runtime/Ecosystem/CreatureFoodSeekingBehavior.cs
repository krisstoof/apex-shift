using ApexShift.Runtime.Creatures;
using UnityEngine;

namespace ApexShift.Runtime.Ecosystem
{
    [RequireComponent(typeof(CreatureNeedsRuntime))]
    [RequireComponent(typeof(CreatureAgentView))]
    public class CreatureFoodSeekingBehavior : MonoBehaviour
    {
        [SerializeField] private float interactionRange = 1.5f;
        [SerializeField] private float consumptionRate = 2f;

        private CreatureNeedsRuntime _needs;
        private CreatureAgentView _view;
        private FoodSourceView _currentTarget;

        private void Awake()
        {
            _needs = GetComponent<CreatureNeedsRuntime>();
            _view = GetComponent<CreatureAgentView>();
        }

        private void Update()
        {
            if (!_needs.State.IsHungry)
            {
                _currentTarget = null;
                return;
            }

            if (_currentTarget == null || _currentTarget.IsEmpty)
            {
                _currentTarget = FindFood();
            }

            if (_currentTarget != null)
            {
                float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);
                if (dist <= interactionRange)
                {
                    _view.Stop();
                    float nutrition = _currentTarget.Consume(consumptionRate * Time.deltaTime);
                    _needs.Eat(nutrition);
                }
                else
                {
                    _view.MoveTo(_currentTarget.transform.position);
                }
            }
        }

        private FoodSourceView FindFood()
        {
            if (EcosystemRuntime.Instance == null) return null;

            if (_needs.Diet != null && _needs.Diet.PlantDiet)
            {
                return EcosystemRuntime.Instance.TryFindNearestFood(transform.position, Core.Ecosystem.FoodKind.Plants);
            }
            
            return null;
        }
    }
}
