using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Config;
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
        [SerializeField] private SpeciesDefinition speciesDefinition;
        [SerializeField] private GameBalanceConfig gameBalanceConfig;

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
            Configure(id, null);
        }

        public void Configure(string id, SpeciesDefinition overrideDefinition)
        {
            creatureId = string.IsNullOrWhiteSpace(id) ? "small_prey" : id.Trim().ToLowerInvariant();

            SpeciesDefinition resolved = GameBalanceConfigProvider.ResolveSpeciesDefinition(gameBalanceConfig, overrideDefinition != null ? overrideDefinition : speciesDefinition, creatureId, this);
            if (useSpeciesDefaults)
            {
                ApplySpeciesDefinition(resolved);
            }

            _diet = useSpeciesDefaults ? resolved.CreateDietProfile() : CreatureDietProfile.GetDefault(creatureId);
            _state = new CreatureNeedsState(maxHunger, hungerGrowthRate, hungryThreshold, starvingThreshold, desperateThreshold);

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

        public void RestoreNeeds(float hunger, float energy)
        {
            EnsureInitialized();
            State.SetHunger(hunger);
            State.SetEnergy(energy);
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

        public void SetSpeciesDefinitionForTests(SpeciesDefinition definition)
        {
            speciesDefinition = definition;
            _state = null;
            _diet = null;
        }

        public void SetGameBalanceConfigForTests(GameBalanceConfig config)
        {
            gameBalanceConfig = config;
            _state = null;
            _diet = null;
        }

        private void ApplySpeciesDefinition(SpeciesDefinition definition)
        {
            if (definition == null) return;
            maxHunger = definition.MaxHunger;
            hungerGrowthRate = definition.HungerGrowthRate;
            hungryThreshold = definition.HungryThreshold;
            starvingThreshold = definition.StarvingThreshold;
            desperateThreshold = definition.DesperateThreshold;
            foodSearchRadius = definition.FoodSearchRadius;
            desperateFoodSearchRadius = definition.DesperateFoodSearchRadius;
            preySeekHungerThreshold = definition.PreySeekHungerThreshold;
            fleeHungerThreshold = definition.FleeHungerThreshold;
            initialHungerMin = definition.InitialHungerMin;
            initialHungerMax = definition.InitialHungerMax;
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
