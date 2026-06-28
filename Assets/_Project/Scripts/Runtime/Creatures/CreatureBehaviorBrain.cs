using UnityEngine;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.World.Query;

namespace ApexShift.Runtime.Creatures
{
    /// <summary>
    /// Source-of-truth creature AI component.
    /// Keeps decision state, target selection, flee/chase behavior and debug/animation state in one place.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CreatureAgentView))]
    [RequireComponent(typeof(CreatureNeedsRuntime))]
    [RequireComponent(typeof(CreatureHealthRuntime))]
    [RequireComponent(typeof(CreatureSimulationLodRuntime))]
    public sealed class CreatureBehaviorBrain : MonoBehaviour
    {
        [SerializeField] private float updateInterval = 0.25f;
        [SerializeField] private float preySightRange = 48f;
        [SerializeField] private float fleeRangeSmallPrey = 36f;
        [SerializeField] private float fleeRangeGrazer = 28f;
        [SerializeField] private float fleeDistanceSmallPrey = 34f;
        [SerializeField] private float fleeDistanceGrazer = 30f;
        [SerializeField] private float threatFallbackScanRadius = 45f;
        [SerializeField] private float eatCooldownSeconds = 1.25f;
        [SerializeField] private float smallPreyPanicDuration = 3.4f;
        [SerializeField] private float eatDistance = 2.2f;
        [SerializeField] private float navSampleDistance = 7f;
        [Header("Varnak player AI")]
        [SerializeField] private float varnakPlayerDetectRange = 28f;
        [SerializeField] private float varnakPlayerCloseChaseRange = 12f;
        [SerializeField] private float varnakPlayerAttackRange = 2.5f;
        [Header("Grazer emergency AI")]
        [SerializeField] private float grazerScavengeRange = 72f;
        [SerializeField] private float grazerSmallPreyDetectRange = 16f;
        [SerializeField] private float grazerSmallPreyAttackRange = 2.2f;
        [Header("Grazer parity AI")]
        [SerializeField] private float grazerPlantSearchRange = 120f;
        [SerializeField] private float grazerPlantBiomassImpact = 1.2f;
        [SerializeField] private float grazerMeatBiomassRequest = 1f;
        [SerializeField] private float grazerMeatMinimumNutritionRatio = 0.38f;
        [SerializeField] private float grazerLowBiomassPercent = 35f;
        [SerializeField] private float grazerPredationRiskThreshold = 0.82f;
        [SerializeField] private float grazerAggression = 0.15f;
        [Header("Small prey parity AI")]
        [SerializeField] private float smallPreyFoodSearchRange = 110f;
        [SerializeField] private float smallPreyPlantBiomassImpact = 0.4f;
        [SerializeField] private bool smallPreyRequiresHungerForFoodSearch = true;

        private CreatureAgentView _agentView;
        private CreatureNeedsRuntime _needs;
        private CreatureHealthRuntime _health;
        private CreatureWanderBehavior _wander;
        private CreatureDebugOverlay _debugOverlay;
        private CreatureSimulationLodRuntime _simulationLod;
        private WorldQueryRuntime _worldQuery;
        private CreatureBehaviorState _state;
        private Transform _player;
        private CreatureAgentView _currentPrey;
        private FoodSourceView _currentFood;
        private float _targetRefreshTimer;
        private float _eatCooldownTimer;
        private float _panicTimer;
        private float _varnakCombatCooldownTimer;
        private float _timer;

        public string DecisionReason { get; private set; } = "spawn";
        public string LastFoodSource { get; private set; } = "none";
        public int DecisionCount { get; private set; }
        public string CurrentBiomeId { get; private set; } = "default";
        public string HomeBiomeId { get; private set; } = "default";
        public string PopulationBiomeId { get; private set; } = "default";
        public string CurrentNiche { get; private set; } = "HERBIVORE";
        public float AttackCooldown => _varnakCombatCooldownTimer;
        public CreatureBehaviorState State => _state;
        public Transform CurrentTargetTransform => _currentPrey != null ? _currentPrey.transform : _currentFood != null ? _currentFood.transform : _player;
        public string CurrentTargetLabel => _currentPrey != null ? $"prey:{_currentPrey.CreatureId}" : _currentFood != null ? $"food:{_currentFood.SourceId}" : _player != null ? "player" : "none";

        private void Awake() => Cache();
        private void OnEnable() { Cache(); EcosystemRuntime.Instance?.RegisterCreature(_agentView ?? GetComponent<CreatureAgentView>()); SetState(CreatureBehaviorState.Idle, "enabled"); }
        private void OnDisable() => EcosystemRuntime.Instance?.UnregisterCreature(_agentView);

        private void Cache()
        {
            _agentView = GetComponent<CreatureAgentView>();
            _needs = GetComponent<CreatureNeedsRuntime>();
            _health = GetComponent<CreatureHealthRuntime>();
            _wander = GetComponent<CreatureWanderBehavior>();
            _debugOverlay = GetComponent<CreatureDebugOverlay>();
            _simulationLod = GetComponent<CreatureSimulationLodRuntime>();
            _worldQuery = WorldQueryRuntime.Active;
        }

        private void Update()
        {
            if (_agentView == null || _needs == null || _health == null || _health.IsDead) return;
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            _simulationLod ??= GetComponent<CreatureSimulationLodRuntime>();
            string creatureId = (_agentView.CreatureId ?? string.Empty).Trim().ToLowerInvariant();
            if (_simulationLod != null)
            {
                _simulationLod.Tick(Time.deltaTime, creatureId);
                if (!_simulationLod.ShouldRunFullAi)
                {
                    SetWanderEnabled(false);
                    _agentView.Stop();
                    if (_simulationLod.IsFar && _simulationLod.TryConsumeFarTick(Time.deltaTime, out float farElapsed))
                    {
                        TickReducedSimulation(farElapsed, "far");
                    }
                    else if (_simulationLod.IsBackgroundSimulationMode && _simulationLod.TryConsumeBackgroundTick(Time.deltaTime, out float backgroundElapsed))
                    {
                        TickReducedSimulation(backgroundElapsed, "background");
                    }
                    return;
                }
            }

            _timer = Mathf.Max(0.05f, _simulationLod != null ? _simulationLod.GetEffectiveAiInterval(updateInterval) : updateInterval);
            DecisionCount++;
            TickBrain();
        }

        private void TickBrain()
        {
            EcosystemRuntime ecosystem = EcosystemRuntime.Instance;
            _worldQuery = WorldQueryRuntime.GetOrCreate(ecosystem) ?? _worldQuery;
            ResolvePlayer();
            if (_worldQuery == null) { SetState(CreatureBehaviorState.Wander); return; }
            string creatureId = (_agentView.CreatureId ?? string.Empty).Trim().ToLowerInvariant();
            UpdateBiomeMemory(creatureId);
            UpdateTargetMemory(creatureId);
            if ((creatureId == "small_prey" || creatureId == "grazer") && TryFleePredator(_worldQuery, creatureId)) return;
            if ((creatureId == "small_prey" || creatureId == "grazer") && TryFleePlayer(creatureId)) return;
            float effectiveDelta = _simulationLod != null ? _simulationLod.GetEffectiveAiInterval(updateInterval) : updateInterval;
            if (_eatCooldownTimer > 0f) _eatCooldownTimer -= effectiveDelta;
            if (_varnakCombatCooldownTimer > 0f) _varnakCombatCooldownTimer -= effectiveDelta;
            if (_panicTimer > 0f) _panicTimer -= effectiveDelta;
            if (creatureId == "varnak") { HandleVarnakBrain(_worldQuery); return; }
            if (creatureId == "grazer") { HandleGrazerBrain(_worldQuery); return; }
            HandleSmallPreyBrain(_worldQuery);
        }

        private void HandleVarnakBrain(WorldQueryRuntime query)
        {
            CreatureAgentView prey = _currentPrey;
            if (prey == null || !prey.isActiveAndEnabled)
            {
                prey = FindNearestSceneCreatureById("small_prey", preySightRange) ?? FindNearestSceneCreatureById("grazer", preySightRange * 0.65f);
                _currentPrey = prey;
            }
            if (prey != null)
            {
                float distance = HorizontalDistance(transform.position, prey.transform.position);
                SetState(distance <= varnakPlayerAttackRange ? CreatureBehaviorState.Attack : distance <= varnakPlayerCloseChaseRange ? CreatureBehaviorState.Chase : CreatureBehaviorState.Stalk, $"prey d:{distance:0.0}");
                if (distance > varnakPlayerAttackRange) MoveToTarget(prey.transform.position); else _agentView.Stop();
                return;
            }
            if (_player != null)
            {
                float distance = HorizontalDistance(transform.position, _player.position);
                if (distance <= varnakPlayerDetectRange)
                {
                    SetState(CreatureBehaviorState.Chase, $"player d:{distance:0.0}");
                    if (distance > varnakPlayerAttackRange) MoveToTarget(_player.position); else _agentView.Stop();
                    return;
                }
            }
            SetState(CreatureBehaviorState.Wander, "seek_food");
        }

        private void HandleGrazerBrain(WorldQueryRuntime query)
        {
            if (_panicTimer > 0f) { SetState(CreatureBehaviorState.Flee, "panic"); return; }

            UpdateGrazerNiche();

            HungerStage hungerStage = _needs.State.Stage;
            bool isHungry = _needs.IsHungry;
            float riskDrive = _needs.State.RiskDrive;
            float biomassPercent = GetCurrentBiomePlantBiomassPercent();

            FoodSourceView plantFood = null;
            query.TryFindNearestPlantFood(transform.position, grazerPlantSearchRange, out plantFood);

            // Godot grazer.gd strongly prefers plants whenever a valid plant target exists,
            // even while starving. Meat and predation are fallback risk choices.
            if (isHungry && plantFood != null)
            {
                _currentPrey = null;
                HandleGrazerPlantTarget(plantFood, hungerStage == HungerStage.Starving || hungerStage == HungerStage.Desperate ? "starving_still_prefers_plant" : "hungry_prefer_plants");
                return;
            }

            CreatureAgentView prey = null;
            bool canHuntSmallPrey = CanGrazerHuntSmallPrey(plantFood, hungerStage, riskDrive, biomassPercent, query, out prey);
            if (canHuntSmallPrey && prey != null)
            {
                _currentFood = null;
                _currentPrey = prey;
                float distance = HorizontalDistance(transform.position, prey.transform.position);
                SetState(distance <= grazerSmallPreyAttackRange ? CreatureBehaviorState.Attack : CreatureBehaviorState.HuntSmallPrey, $"grazer_predation d:{distance:0.0}");
                if (distance <= grazerSmallPreyAttackRange)
                {
                    _agentView.Stop();
                }
                else
                {
                    MoveToTarget(prey.transform.position);
                }
                return;
            }

            FoodSourceView meatFood = null;
            bool canScavenge = CanGrazerScavenge(plantFood, hungerStage, biomassPercent);
            if (canScavenge && query.TryFindNearestMeatFood(transform.position, grazerScavengeRange, out meatFood))
            {
                _currentPrey = null;
                HandleGrazerMeatTarget(meatFood, hungerStage == HungerStage.Starving || hungerStage == HungerStage.Desperate ? "starving_scavenge" : "hungry_scavenge_no_plants");
                return;
            }

            if (isHungry)
            {
                SetState(CreatureBehaviorState.Wander, hungerStage == HungerStage.Starving || hungerStage == HungerStage.Desperate ? "starving_no_food" : "hungry_no_food_wander");
                return;
            }

            SetState(CreatureBehaviorState.Wander, "graze");
        }

        private void HandleGrazerPlantTarget(FoodSourceView food, string reason)
        {
            if (food == null || food.IsEmpty)
            {
                _currentFood = null;
                SetState(CreatureBehaviorState.Wander, "plant_target_lost");
                return;
            }

            _currentFood = food;
            float distance = HorizontalDistance(transform.position, food.transform.position);
            if (distance <= eatDistance)
            {
                SetState(CreatureBehaviorState.EatPlants, $"plant d:{distance:0.0}");
                if (_eatCooldownTimer <= 0f)
                {
                    _eatCooldownTimer = eatCooldownSeconds;
                    ConsumeGrazerPlant(food);
                }

                _agentView.Stop();
                _currentFood = food != null && !food.IsEmpty ? food : null;
                return;
            }

            SetState(CreatureBehaviorState.SeekFood, $"{reason} d:{distance:0.0}");
            MoveToTarget(food.transform.position);
        }

        private void HandleGrazerMeatTarget(FoodSourceView food, string reason)
        {
            if (food == null || food.IsEmpty)
            {
                _currentFood = null;
                SetState(CreatureBehaviorState.Wander, "meat_target_lost");
                return;
            }

            _currentFood = food;
            float distance = HorizontalDistance(transform.position, food.transform.position);
            if (distance <= eatDistance)
            {
                SetState(CreatureBehaviorState.EatMeat, $"meat d:{distance:0.0}");
                if (_eatCooldownTimer <= 0f)
                {
                    _eatCooldownTimer = eatCooldownSeconds;
                    ConsumeGrazerMeat(food);
                }

                _agentView.Stop();
                _currentFood = food != null && !food.IsEmpty ? food : null;
                return;
            }

            SetState(CreatureBehaviorState.Scavenge, $"{reason} d:{distance:0.0}");
            MoveToTarget(food.transform.position);
        }

        private void ConsumeGrazerPlant(FoodSourceView food)
        {
            float requestedBiomass = Mathf.Max(0.01f, grazerPlantBiomassImpact);
            float nutrition = food.Consume(requestedBiomass);
            if (nutrition <= 0f)
            {
                return;
            }

            _needs.Eat(food.Kind, Mathf.Max(8f, nutrition));
            LastFoodSource = string.IsNullOrWhiteSpace(food.SourceId) ? "plants" : food.SourceId;
            EcosystemDirectorRuntime.Active?.DebugReducePlantBiomass(transform.position, requestedBiomass);
        }

        private void ConsumeGrazerMeat(FoodSourceView food)
        {
            float requestedBiomass = Mathf.Max(0.01f, grazerMeatBiomassRequest);
            float nutrition = food.Consume(requestedBiomass);
            if (nutrition <= 0f)
            {
                return;
            }

            float before = _needs.State.Hunger;
            float requestedNutrition = Mathf.Max(8f, nutrition);
            float weightedReduction = _needs.Eat(food.Kind, requestedNutrition);
            float minimumReduction = requestedNutrition * Mathf.Clamp01(grazerMeatMinimumNutritionRatio);
            if (weightedReduction < minimumReduction)
            {
                _needs.Eat(Mathf.Max(0f, minimumReduction - weightedReduction));
            }

            LastFoodSource = string.IsNullOrWhiteSpace(food.SourceId) ? "meat_drop" : food.SourceId;
            if (_needs.State.Hunger >= before)
            {
                _needs.Eat(minimumReduction);
            }
        }

        private void HandleSmallPreyBrain(WorldQueryRuntime query)
        {
            if (_panicTimer > 0f)
            {
                SetState(CreatureBehaviorState.Flee, "panic");
                return;
            }

            bool shouldSeekFood = !smallPreyRequiresHungerForFoodSearch || _needs.IsHungry;
            if (!shouldSeekFood)
            {
                _currentFood = null;
                SetState(CreatureBehaviorState.Wander, "not_hungry_wander");
                return;
            }

            FoodSourceView food = _currentFood;
            if (food == null || !food.isActiveAndEnabled || food.IsEmpty || HorizontalDistance(transform.position, food.transform.position) > smallPreyFoodSearchRange * 1.25f)
            {
                query.TryFindNearestPlantFood(transform.position, smallPreyFoodSearchRange, out food);
                _currentFood = food;
            }

            if (food != null)
            {
                float d = HorizontalDistance(transform.position, food.transform.position);
                if (d <= eatDistance)
                {
                    SetState(CreatureBehaviorState.EatPlants, $"food d:{d:0.0}");
                    if (_eatCooldownTimer <= 0f)
                    {
                        _eatCooldownTimer = eatCooldownSeconds;
                        ConsumeSmallPreyPlant(food);
                    }

                    _agentView.Stop();
                    _currentFood = food != null && !food.IsEmpty ? food : null;
                    return;
                }

                SetState(CreatureBehaviorState.SeekFood, $"hungry_seek_plant d:{d:0.0}");
                MoveToTarget(food.transform.position);
                return;
            }
            if (_player != null)
            {
                float d = HorizontalDistance(transform.position, _player.position);
                if (d <= fleeRangeSmallPrey) { Vector3 away = transform.position - _player.position; away.y = 0f; if (away.sqrMagnitude < 0.001f) { away = Random.insideUnitSphere; away.y = 0f; } SetState(CreatureBehaviorState.Flee, $"player d:{d:0.0}"); MoveToTarget(transform.position + away.normalized * fleeDistanceSmallPrey); _panicTimer = smallPreyPanicDuration; return; }
            }
            SetState(CreatureBehaviorState.Wander, "search");
        }

        private void ConsumeSmallPreyPlant(FoodSourceView food)
        {
            if (food == null || food.IsEmpty)
            {
                return;
            }

            float requestedBiomass = Mathf.Max(0.01f, smallPreyPlantBiomassImpact);
            float nutrition = food.Consume(requestedBiomass);
            if (nutrition <= 0f)
            {
                return;
            }

            _needs.Eat(food.Kind, Mathf.Max(0.5f, nutrition));
            LastFoodSource = string.IsNullOrWhiteSpace(food.SourceId) ? "plants" : food.SourceId;
            EcosystemDirectorRuntime.Active?.DebugReducePlantBiomass(transform.position, requestedBiomass);
        }

        private void TickReducedSimulation(float elapsedSeconds, string mode)
        {
            float elapsed = Mathf.Max(0f, elapsedSeconds);
            if (elapsed <= 0f)
            {
                return;
            }

            string creatureId = (_agentView != null ? _agentView.CreatureId : string.Empty) ?? string.Empty;
            UpdateTargetMemory(creatureId.Trim().ToLowerInvariant());
            if (_eatCooldownTimer > 0f) _eatCooldownTimer = Mathf.Max(0f, _eatCooldownTimer - elapsed);
            if (_varnakCombatCooldownTimer > 0f) _varnakCombatCooldownTimer = Mathf.Max(0f, _varnakCombatCooldownTimer - elapsed);
            if (_panicTimer > 0f) _panicTimer = Mathf.Max(0f, _panicTimer - elapsed);
            if (mode == "background" && _state == CreatureBehaviorState.Flee)
            {
                SetState(CreatureBehaviorState.Wander, "background_reset_flee");
            }
            else
            {
                DecisionReason = mode == "far" ? "far_simulation" : "background_simulation";
            }
        }

        private CreatureAgentView FindNearestSceneCreatureById(string creatureId, float maxDistance)
        {
            _worldQuery = WorldQueryRuntime.GetOrCreate(EcosystemRuntime.Instance) ?? _worldQuery;
            if (_worldQuery == null)
            {
                return null;
            }

            return _worldQuery.TryFindNearestCreatureById(transform.position, creatureId, maxDistance, out CreatureAgentView found) ? found : null;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) => Mathf.Sqrt(HorizontalSqrDistance(a, b));
        private static float HorizontalSqrDistance(Vector3 a, Vector3 b) { float dx = a.x - b.x; float dz = a.z - b.z; return dx * dx + dz * dz; }
        private void UpdateBiomeMemory(string creatureId)
        {
            if (creatureId != "small_prey" && creatureId != "grazer")
            {
                return;
            }

            string biomeId = _worldQuery != null ? _worldQuery.GetBiomeIdForPosition(transform.position) : "default";
            if (string.IsNullOrWhiteSpace(biomeId))
            {
                biomeId = "default";
            }

            CurrentBiomeId = biomeId;
            if (HomeBiomeId == "default") HomeBiomeId = biomeId;
            if (PopulationBiomeId == "default") PopulationBiomeId = biomeId;
        }
        private void UpdateGrazerNiche()
        {
            float biomassPercent = GetCurrentBiomePlantBiomassPercent();
            HungerStage stage = _needs.State.Stage;
            bool highRisk = stage == HungerStage.Desperate || biomassPercent <= grazerLowBiomassPercent;
            CurrentNiche = highRisk ? "OMNIVORE" : "HERBIVORE";
        }

        private float GetCurrentBiomePlantBiomassPercent()
        {
            EcosystemDirectorRuntime director = EcosystemDirectorRuntime.Active;
            if (director == null)
            {
                return 100f;
            }

            BiomeEcosystemState state = director.GetBiomeState(CurrentBiomeId);
            return state != null ? state.PlantBiomassPercent : 100f;
        }

        private bool CanGrazerScavenge(FoodSourceView plantFood, HungerStage hungerStage, float biomassPercent)
        {
            if (plantFood != null)
            {
                return false;
            }

            if (!_needs.IsHungry)
            {
                return false;
            }

            return hungerStage == HungerStage.Hungry
                   || hungerStage == HungerStage.Starving
                   || hungerStage == HungerStage.Desperate
                   || biomassPercent <= grazerLowBiomassPercent;
        }

        private bool CanGrazerHuntSmallPrey(FoodSourceView plantFood, HungerStage hungerStage, float riskDrive, float biomassPercent, WorldQueryRuntime query, out CreatureAgentView prey)
        {
            prey = null;
            if (plantFood != null || query == null)
            {
                return false;
            }

            if (!query.TryFindNearestCreatureById(transform.position, "small_prey", grazerSmallPreyDetectRange, out prey) || prey == null)
            {
                return false;
            }

            if (hungerStage == HungerStage.Desperate)
            {
                return true;
            }

            return hungerStage == HungerStage.Starving
                   && (biomassPercent <= grazerLowBiomassPercent || (CurrentNiche == "OMNIVORE" && riskDrive >= grazerPredationRiskThreshold && grazerAggression + _needs.Diet.MeatPreference >= 0.18f));
        }
        private void UpdateTargetMemory(string creatureId) { if (_currentPrey != null && (!_currentPrey.isActiveAndEnabled || HorizontalDistance(transform.position, _currentPrey.transform.position) > preySightRange * 1.25f)) _currentPrey = null; if (_currentFood != null && (!_currentFood.isActiveAndEnabled || _currentFood.IsEmpty)) _currentFood = null; }
        private bool TryFleePredator(WorldQueryRuntime query, string creatureId) { float fleeRange = creatureId == "small_prey" ? fleeRangeSmallPrey * 1.5f : fleeRangeGrazer * 1.25f; float fleeDistance = creatureId == "small_prey" ? fleeDistanceSmallPrey * 1.25f : fleeDistanceGrazer * 1.15f; float scanRange = Mathf.Max(fleeRange, threatFallbackScanRadius); CreatureAgentView predator = query != null && query.TryFindNearestCreatureById(transform.position, "varnak", scanRange, out CreatureAgentView foundPredator) ? foundPredator : null; if (predator == null) return false; float distance = HorizontalDistance(transform.position, predator.transform.position); if (distance > scanRange) return false; Vector3 away = transform.position - predator.transform.position; away.y = 0f; if (away.sqrMagnitude < 0.001f) { away = Random.insideUnitSphere; away.y = 0f; } _currentPrey = null; _currentFood = null; _player = null; SetState(CreatureBehaviorState.Flee, $"flee_varnak d:{distance:0.0}"); SetWanderEnabled(false); MoveToTarget(transform.position + away.normalized * Mathf.Max(4f, fleeDistance)); return true; }
        private bool TryFleePlayer(string creatureId) { ResolvePlayer(); if (_player == null) return false; float fleeRange = creatureId == "small_prey" ? fleeRangeSmallPrey : fleeRangeGrazer; float fleeDistance = creatureId == "small_prey" ? fleeDistanceSmallPrey : fleeDistanceGrazer; float distance = HorizontalDistance(transform.position, _player.position); if (distance > fleeRange) return false; Vector3 away = transform.position - _player.position; away.y = 0f; if (away.sqrMagnitude < 0.001f) { away = Random.insideUnitSphere; away.y = 0f; } _currentPrey = null; _currentFood = null; SetState(CreatureBehaviorState.Flee, $"flee_player d:{distance:0.0}"); SetWanderEnabled(false); MoveToTarget(transform.position + away.normalized * Mathf.Max(4f, fleeDistance)); if (creatureId == "small_prey") _panicTimer = smallPreyPanicDuration; return true; }
        private void MoveToTarget(Vector3 targetPosition) { CreatureNavigationAdapter adapter = _agentView != null ? _agentView.GetNavigationAdapter() : null; if (adapter != null && adapter.TrySamplePosition(targetPosition, out Vector3 navTarget, navSampleDistance)) { _agentView.MoveTo(navTarget); return; } _agentView.MoveTo(targetPosition); }
        private void ResolvePlayer() { if (_player != null && _player.gameObject.activeInHierarchy) return; GameObject playerObject = null; try { playerObject = GameObject.FindWithTag("Player"); } catch (UnityException) { } if (playerObject == null) playerObject = GameObject.Find("Player"); if (playerObject == null) { var controller = Object.FindAnyObjectByType<ApexShift.Runtime.Player.IsometricPlayerController>(); if (controller != null) playerObject = controller.gameObject; } _player = playerObject != null ? playerObject.transform : null; }
        private void SetState(CreatureBehaviorState next) => SetState(next, DecisionReason);
        private void SetState(CreatureBehaviorState next, string reason) { _state = next; DecisionReason = string.IsNullOrWhiteSpace(reason) ? next.ToString() : reason; if (_debugOverlay != null) _debugOverlay.SetBehaviorState(next); }
        private void SetWanderEnabled(bool enabled) { if (_wander != null) _wander.enabled = enabled; }
        public void SetBehaviorStateForTests(CreatureBehaviorState state, string reason = "test") => SetState(state, reason);
        public void OnCreatureDied() { SetState(CreatureBehaviorState.Dead); SetWanderEnabled(false); _currentPrey = null; _currentFood = null; _eatCooldownTimer = 0f; _panicTimer = 0f; _varnakCombatCooldownTimer = 0f; }
    }
}
