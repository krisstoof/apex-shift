using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.World.Query;
using UnityEngine;

namespace ApexShift.Runtime.Ecosystem
{
    public class CreatureNeedsRuntime : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private string creatureId;
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float hungerGrowthRate = 20f;
        [SerializeField] private float hungryThreshold = 35f;
        [SerializeField] private float starvingThreshold = 60f;
        [SerializeField] private float desperateThreshold = 82f;
        [SerializeField] private float foodSearchRadius = 50f;
        [SerializeField] private float desperateFoodSearchRadius = 80f;
        [SerializeField] private float preySeekHungerThreshold = 42f;
        [SerializeField] private float fleeHungerThreshold = 28f;
        [SerializeField] private bool useSpeciesDefaults = true;
        [SerializeField] private float initialHungerMin = 15f;
        [SerializeField] private float initialHungerMax = 40f;

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
        public float PreySeekHungerThreshold => preySeekHungerThreshold;
        public float FleeHungerThreshold => fleeHungerThreshold;

        private void Awake()
        {
            EnsureInitialized();
        }

        public void Configure(string id)
        {
            creatureId = string.IsNullOrWhiteSpace(id) ? "small_prey" : id.Trim().ToLowerInvariant();

            if (useSpeciesDefaults)
            {
                ApplySpeciesDefaults(creatureId);
            }

            _diet = CreatureDietProfile.GetDefault(creatureId);
            _state = new CreatureNeedsState(maxHunger, hungerGrowthRate, hungryThreshold, starvingThreshold, desperateThreshold);

            // Seed the migration slice so behavior is visible immediately in RuntimeWorld.
            // Runtime keeps the existing Unity 0-100 hunger scale, but rates/thresholds mirror Godot ratios.
            float seededHunger = Random.Range(initialHungerMin, initialHungerMax);
            seededHunger = Mathf.Max(seededHunger, hungryThreshold + maxHunger * 0.04f);
            _state.SetHunger(seededHunger);
        }

        private void Update()
        {
            EnsureInitialized();

            float movementIntensity = 0f;
            var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null && navAgent.speed > 0.01f)
            {
                movementIntensity = Mathf.Clamp01(navAgent.velocity.magnitude / navAgent.speed);
            }

            _state.Tick(Time.deltaTime, movementIntensity);
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

            WorldQueryRuntime query = WorldQueryRuntime.GetOrCreate(ecosystem);
            if (query == null)
            {
                return false;
            }

            float range = State.Stage == HungerStage.Desperate
                ? Mathf.Max(foodSearchRadius, desperateFoodSearchRadius)
                : foodSearchRadius;

            if (Diet.PlantPreference >= Diet.MeatPreference && Diet.PlantDiet)
            {
                if (query.TryFindNearestPlantFood(transform.position, range, out source))
                {
                    return true;
                }
            }

            if ((Diet.MeatDiet || Diet.ScavengerDiet) && query.TryFindNearestMeatFood(transform.position, range, out source))
            {
                return true;
            }

            if (Diet.PlantDiet)
            {
                return query.TryFindNearestPlantFood(transform.position, range, out source);
            }

            return false;
        }

        private void ApplySpeciesDefaults(string id)
        {
            maxHunger = 100f;

            switch (id)
            {
                case "small_prey":
                    hungerGrowthRate = 20f;
                    hungryThreshold = 35f;
                    starvingThreshold = 60f;
                    desperateThreshold = 82f;
                    preySeekHungerThreshold = 50f;
                    fleeHungerThreshold = 24f;
                    foodSearchRadius = 110f;
                    desperateFoodSearchRadius = 160f;
                    initialHungerMin = 36f;
                    initialHungerMax = 48f;
                    break;

                case "grazer":
                    hungerGrowthRate = 30f;
                    hungryThreshold = 35f;
                    starvingThreshold = 60f;
                    desperateThreshold = 82f;
                    preySeekHungerThreshold = 46f;
                    fleeHungerThreshold = 26f;
                    foodSearchRadius = 120f;
                    desperateFoodSearchRadius = 170f;
                    initialHungerMin = 36f;
                    initialHungerMax = 52f;
                    break;

                case "varnak":
                    hungerGrowthRate = 18f;
                    hungryThreshold = 32f;
                    starvingThreshold = 58f;
                    desperateThreshold = 80f;
                    preySeekHungerThreshold = 38f;
                    fleeHungerThreshold = 22f;
                    foodSearchRadius = 140f;
                    desperateFoodSearchRadius = 200f;
                    initialHungerMin = 36f;
                    initialHungerMax = 58f;
                    break;

                default:
                    hungerGrowthRate = 20f;
                    hungryThreshold = 35f;
                    starvingThreshold = 60f;
                    desperateThreshold = 82f;
                    preySeekHungerThreshold = 42f;
                    fleeHungerThreshold = 28f;
                    foodSearchRadius = 50f;
                    desperateFoodSearchRadius = 80f;
                    initialHungerMin = 15f;
                    initialHungerMax = 35f;
                    break;
            }
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
