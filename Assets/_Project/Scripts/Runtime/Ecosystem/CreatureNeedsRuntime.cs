using ApexShift.Core.Ecosystem;
using UnityEngine;

namespace ApexShift.Runtime.Ecosystem
{
    public class CreatureNeedsRuntime : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string creatureId;
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float hungerGrowthRate = 1f;
        [SerializeField] private float hungryThreshold = 30f;
        [SerializeField] private float starvingThreshold = 60f;
        [SerializeField] private float desperateThreshold = 85f;
        [SerializeField] private float foodSearchRadius = 50f;
        [SerializeField] private float desperateFoodSearchRadius = 80f;

        private CreatureNeedsState _state;
        private CreatureDietProfile _diet;

        public CreatureNeedsState State
        {
            get
            {
                EnsureInitialized();
                return _state;
            }
        }

        public CreatureDietProfile Diet
        {
            get
            {
                EnsureInitialized();
                return _diet;
            }
        }

        public string CreatureId => creatureId;
        public bool IsHungry => State.IsHungry;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Configure(string id)
        {
            creatureId = string.IsNullOrWhiteSpace(id) ? "small_prey" : id.Trim().ToLowerInvariant();
            _diet = CreatureDietProfile.GetDefault(creatureId);
            _state = new CreatureNeedsState(maxHunger, hungerGrowthRate, hungryThreshold, starvingThreshold, desperateThreshold);
        }

        private void Update()
        {
            EnsureInitialized();
            _state.Tick(Time.deltaTime);
        }

        public void Eat(float nutrition)
        {
            State.Eat(nutrition);
        }

        public float Eat(FoodKind kind, float nutrition)
        {
            float preference = Mathf.Max(Diet.GetPreference(kind), 0.05f);
            float weightedNutrition = nutrition * preference;
            State.Eat(weightedNutrition);
            return weightedNutrition;
        }

        public bool TryFindPreferredFood(EcosystemRuntime ecosystem, out FoodSourceView source)
        {
            EnsureInitialized();

            source = null;
            ecosystem ??= EcosystemRuntime.Instance;
            if (ecosystem == null)
            {
                return false;
            }

            float range = State.Stage == HungerStage.Desperate
                ? Mathf.Max(foodSearchRadius, desperateFoodSearchRadius)
                : foodSearchRadius;

            if (Diet.PlantPreference >= Diet.MeatPreference && Diet.PlantDiet)
            {
                if (ecosystem.TryFindNearestPlantFood(transform.position, range, out source))
                {
                    return true;
                }
            }

            if ((Diet.MeatDiet || Diet.ScavengerDiet) && ecosystem.TryFindNearestMeatFood(transform.position, range, out source))
            {
                return true;
            }

            if (Diet.PlantDiet)
            {
                return ecosystem.TryFindNearestPlantFood(transform.position, range, out source);
            }

            return false;
        }

        private void EnsureInitialized()
        {
            if (_state != null && _diet != null)
            {
                return;
            }

            Configure(creatureId);
        }
    }
}
