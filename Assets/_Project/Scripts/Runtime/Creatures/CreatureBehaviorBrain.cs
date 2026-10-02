using UnityEngine;
using UnityEngine.Serialization;
using ApexShift.Runtime.World.Environment;
using ApexShift.Core.Ecosystem;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.World.Query;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.Audio;
using ApexShift.Runtime.Fire;

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
        [FormerlySerializedAs("fleeRangeGrazer")]
        [SerializeField] private float fleeRangeHerbivore = 28f;
        [SerializeField] private float fleeDistanceSmallPrey = 34f;
        [FormerlySerializedAs("fleeDistanceGrazer")]
        [SerializeField] private float fleeDistanceHerbivore = 30f;
        [SerializeField] private float threatFallbackScanRadius = 45f;
        [SerializeField] private float eatCooldownSeconds = 1.25f;
        [SerializeField] private float smallPreyPanicDuration = 3.4f;
        [SerializeField] private float eatDistance = 2.2f;
        [SerializeField] private float navSampleDistance = 7f;
        [Header("Predator player AI")]
        [FormerlySerializedAs("varnakPlayerDetectRange")]
        [SerializeField] private float predatorPlayerDetectRange = 28f;
        [FormerlySerializedAs("varnakPlayerCloseChaseRange")]
        [SerializeField] private float predatorPlayerCloseChaseRange = 12f;
        [FormerlySerializedAs("varnakPlayerAttackRange")]
        [SerializeField] private float predatorPlayerAttackRange = 3.5f;
        [FormerlySerializedAs("varnakPlayerDamage")]
        [SerializeField] private float predatorPlayerDamage = 12f;
        [FormerlySerializedAs("varnakFireFearRangeMultiplier")]
        [SerializeField] private float predatorFireFearRangeMultiplier = 1.45f;
        [FormerlySerializedAs("varnakFireFearMinimumRange")]
        [SerializeField] private float predatorFireFearMinimumRange = 12f;
        [Header("Grazer emergency AI")]
        [FormerlySerializedAs("grazerScavengeRange")]
        [SerializeField] private float foragerScavengeRange = 72f;
        [FormerlySerializedAs("grazerSmallPreyDetectRange")]
        [SerializeField] private float foragerSmallPreyDetectRange = 16f;
        [FormerlySerializedAs("grazerSmallPreyAttackRange")]
        [SerializeField] private float foragerSmallPreyAttackRange = 2.2f;
        [Header("Grazer parity AI")]
        [FormerlySerializedAs("grazerPlantSearchRange")]
        [SerializeField] private float foragerPlantSearchRange = 120f;
        [FormerlySerializedAs("grazerPlantBiomassImpact")]
        [SerializeField] private float foragerPlantBiomassImpact = 1.2f;
        [FormerlySerializedAs("grazerMeatBiomassRequest")]
        [SerializeField] private float foragerMeatBiomassRequest = 1f;
        [FormerlySerializedAs("grazerMeatMinimumNutritionRatio")]
        [SerializeField] private float foragerMeatMinimumNutritionRatio = 0.38f;
        [FormerlySerializedAs("grazerLowBiomassPercent")]
        [SerializeField] private float foragerLowBiomassPercent = 35f;
        [FormerlySerializedAs("grazerPredationRiskThreshold")]
        [SerializeField] private float foragerPredationRiskThreshold = 0.82f;
        [FormerlySerializedAs("grazerAggression")]
        [SerializeField] private float foragerAggression = 0.15f;
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
        private float _predatorCombatCooldownTimer;
        private float _timer;

        public string DecisionReason { get; private set; } = "spawn";
        public string LastFoodSource { get; private set; } = "none";
        public int DecisionCount { get; private set; }
        public string CurrentHabitatId { get; private set; } = HabitatIds.JungleInterior;
        public string HomeHabitatId { get; private set; } = HabitatIds.JungleInterior;
        public string PopulationHabitatId { get; private set; } = HabitatIds.JungleInterior;
        private bool _habitatMemoryInitialized;
        public string CurrentNiche { get; private set; } = "HERBIVORE";
        public float HuntDrive { get; private set; }
        public float AttackCooldown => _predatorCombatCooldownTimer;
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
            UpdateHabitatMemory(creatureId);
            UpdateTargetMemory(creatureId);
            if ((_agentView.Role == CreatureRole.SmallPrey || _agentView.Role == CreatureRole.HerbivoreOmnivore) && TryFleePredator(_worldQuery, creatureId)) return;
            if ((_agentView.Role == CreatureRole.SmallPrey || _agentView.Role == CreatureRole.HerbivoreOmnivore) && TryFleePlayer(creatureId)) return;
            float effectiveDelta = _simulationLod != null ? _simulationLod.GetEffectiveAiInterval(updateInterval) : updateInterval;
            if (_eatCooldownTimer > 0f) _eatCooldownTimer -= effectiveDelta;
            if (_predatorCombatCooldownTimer > 0f) _predatorCombatCooldownTimer -= effectiveDelta;
            if (_panicTimer > 0f) _panicTimer -= effectiveDelta;
            if (_agentView.Role == CreatureRole.Predator) { HandlePredatorBrain(_worldQuery); return; }
            if (_agentView.Role == CreatureRole.HerbivoreOmnivore) { HandleForagerBrain(_worldQuery); return; }
            if (_agentView.Role == CreatureRole.Scavenger)
            {
                if (_worldQuery != null && _worldQuery.TryFindNearestMeatFood(transform.position, _agentView.Definition.FoodSearchRadius, out var meat))
                {
                    if (HorizontalDistance(transform.position, meat.transform.position) <= eatDistance)
                    {
                        _needs.Eat(FoodKind.Meat, meat.Consume(1f));
                        SetState(CreatureBehaviorState.Scavenge, "scavenger_meat");
                    }
                    else { SetState(CreatureBehaviorState.SeekFood); MoveToTarget(meat.transform.position); }
                }
                else SetState(CreatureBehaviorState.Wander);
                return;
            }
            HandleSmallPreyBrain(_worldQuery);
        }

        private void HandlePredatorBrain(WorldQueryRuntime query)
        {
            if (TryFleeFromFire())
            {
                return;
            }

            if (_player != null)
            {
                float playerDistance = HorizontalDistance(transform.position, _player.position);
                if (playerDistance <= predatorPlayerDetectRange)
                {
                    Debug.Log($"[Predator] Player detected at distance: {playerDistance:F2}, DetectRange: {predatorPlayerDetectRange}, AttackRange: {predatorPlayerAttackRange}, Cooldown: {_predatorCombatCooldownTimer:F2}");
                    bool shouldAttackPlayer = playerDistance <= predatorPlayerAttackRange;
                    if (shouldAttackPlayer)
                    {
                        Debug.Log($"[Predator] ATTACK STATE - Player in range! Distance: {playerDistance:0.00}, AttackRange: {predatorPlayerAttackRange}");
                    }

                    SetState(shouldAttackPlayer ? CreatureBehaviorState.Attack : CreatureBehaviorState.Chase, $"player d:{playerDistance:0.0}");
                    if (playerDistance > predatorPlayerAttackRange)
                    {
                        MoveToTarget(_player.position);
                    }
                    else
                    {
                        _agentView.Stop();
                        Debug.Log($"[Predator] About to call TryPublishPredatorCombatEvent - Cooldown: {_predatorCombatCooldownTimer}");
                        TryPublishPredatorCombatEvent(GameplayEventKind.VarnakAttackedPlayer, "player", "varnak_attacked_player");
                    }

                    return;
                }
            }

            // Meat is preferred over hunting when hungry; player/fire reactions retain their priority.
            if (_needs.IsHungry && query.TryFindNearestMeatFood(transform.position, _agentView.Definition.FoodSearchRadius, out var meat))
            {
                _currentPrey = null;
                _currentFood = meat;
                float distance = HorizontalDistance(transform.position, meat.transform.position);
                if (distance > eatDistance) { SetState(CreatureBehaviorState.Scavenge, "predator_seek_meat"); MoveToTarget(meat.transform.position); }
                else
                {
                    SetState(CreatureBehaviorState.EatMeat, "predator_eat_meat");
                    if (_eatCooldownTimer <= 0f)
                    {
                        _eatCooldownTimer = eatCooldownSeconds;
                        _needs.Eat(FoodKind.Meat, meat.Consume(1f));
                        LastFoodSource = string.IsNullOrWhiteSpace(meat.SourceId) ? "meat_drop" : meat.SourceId;
                    }
                    _agentView.Stop();
                }
                return;
            }

            CreatureAgentView prey = _currentPrey;
            if (prey == null || !prey.isActiveAndEnabled)
            {
                prey = FindNearestSceneCreatureByRole(CreatureRole.SmallPrey, preySightRange) ?? FindNearestSceneCreatureByRole(CreatureRole.HerbivoreOmnivore, preySightRange * 0.65f);
                _currentPrey = prey;
            }
            if (prey != null)
            {
                float distance = HorizontalDistance(transform.position, prey.transform.position);
                SetState(distance <= predatorPlayerAttackRange ? CreatureBehaviorState.Attack : distance <= predatorPlayerCloseChaseRange ? CreatureBehaviorState.Chase : CreatureBehaviorState.Stalk, $"prey d:{distance:0.0}");
                if (distance > predatorPlayerAttackRange)
                {
                    MoveToTarget(prey.transform.position);
                }
                else
                {
                    _agentView.Stop();
                    TryPublishPredatorCombatEvent(GetPredatorHuntEventKind(prey.Role), prey.CreatureId, $"varnak_hunted_{prey.CreatureId}");
                }
                return;
            }
            SetState(CreatureBehaviorState.Wander, "seek_food");
        }

        private bool TryFleeFromFire()
        {
            if (!FireSourceRegistry.TryGetStrongestSource(transform.position, predatorFireFearRangeMultiplier, out FireSourceRuntime source))
            {
                return false;
            }

            Vector3 away = transform.position - source.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f)
            {
                away = Random.insideUnitSphere;
                away.y = 0f;
            }

            float radius = Mathf.Max(1f, source.ProtectionRadius);
            float fleeRange = Mathf.Max(predatorFireFearMinimumRange, radius * Mathf.Max(1f, predatorFireFearRangeMultiplier));
            Vector3 target = transform.position + away.normalized * fleeRange;
            CreatureNavigationAdapter adapter = _agentView != null ? _agentView.GetNavigationAdapter() : null;
            if (adapter != null && adapter.TrySamplePosition(target, out Vector3 sampled, navSampleDistance))
            {
                target = sampled;
            }

            if (_wander != null && _wander.enabled)
            {
                _wander.enabled = false;
            }

            SetState(CreatureBehaviorState.Flee, $"afraid_of_fire:{source.SourceId}");
            _agentView.Stop();
            MoveToTarget(target);
            TryPublishPredatorCombatEvent(GameplayEventKind.VarnakScaredByFire, source.SourceId, "varnak_scared_by_fire");
            return true;
        }

        private void HandleForagerBrain(WorldQueryRuntime query)
        {
            if (_panicTimer > 0f) { SetState(CreatureBehaviorState.Flee, "panic"); return; }

            UpdateGrazerNiche();

            HungerStage hungerStage = _needs.State.Stage;
            bool isHungry = _needs.IsHungry;
            float riskDrive = _needs.State.RiskDrive;
            float biomassPercent = GetCurrentBiomePlantBiomassPercent();

            FoodSourceView plantFood = null;
            query.TryFindNearestPlantFood(transform.position, foragerPlantSearchRange, out plantFood);

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
                SetState(distance <= foragerSmallPreyAttackRange ? CreatureBehaviorState.Attack : CreatureBehaviorState.HuntSmallPrey, $"grazer_predation d:{distance:0.0}");
                if (distance <= foragerSmallPreyAttackRange)
                {
                    _agentView.Stop();
                    PublishCreatureGameplayEvent(GameplayEventKind.GrazerHuntedSmallPrey, "small_prey", 1f, 0f, 0f, "grazer_hunted_small_prey");
                }
                else
                {
                    MoveToTarget(prey.transform.position);
                }
                return;
            }

            FoodSourceView meatFood = null;
            bool canScavenge = CanGrazerScavenge(plantFood, hungerStage, biomassPercent);
            if (canScavenge && query.TryFindNearestMeatFood(transform.position, foragerScavengeRange, out meatFood))
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
            float requestedBiomass = Mathf.Max(0.01f, foragerPlantBiomassImpact);
            float nutrition = food.Consume(requestedBiomass);
            if (nutrition <= 0f)
            {
                return;
            }

            _needs.Eat(food.Kind, Mathf.Max(8f, nutrition));
            LastFoodSource = string.IsNullOrWhiteSpace(food.SourceId) ? "plants" : food.SourceId;
            EcosystemDirectorRuntime.Active?.DebugReducePlantBiomass(transform.position, requestedBiomass);
            PublishCreatureGameplayEvent(GameplayEventKind.GrazerConsumedPlants, "plants", requestedBiomass, nutrition, requestedBiomass, "grazer_consumed_plants");
        }

        private void ConsumeGrazerMeat(FoodSourceView food)
        {
            float requestedBiomass = Mathf.Max(0.01f, foragerMeatBiomassRequest);
            float nutrition = food.Consume(requestedBiomass);
            if (nutrition <= 0f)
            {
                return;
            }

            float before = _needs.State.Hunger;
            float requestedNutrition = Mathf.Max(8f, nutrition);
            float weightedReduction = _needs.Eat(food.Kind, requestedNutrition);
            float minimumReduction = requestedNutrition * Mathf.Clamp01(foragerMeatMinimumNutritionRatio);
            if (weightedReduction < minimumReduction)
            {
                _needs.Eat(Mathf.Max(0f, minimumReduction - weightedReduction));
            }

            LastFoodSource = string.IsNullOrWhiteSpace(food.SourceId) ? "meat_drop" : food.SourceId;
            PublishCreatureGameplayEvent(GameplayEventKind.GrazerScavengedMeat, "meat_drop", requestedBiomass, requestedNutrition, 0f, "grazer_scavenged_meat");
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
            PublishCreatureGameplayEvent(GameplayEventKind.SmallPreyConsumedPlants, "plants", requestedBiomass, nutrition, requestedBiomass, "small_prey_consumed_plants");
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
            if (_predatorCombatCooldownTimer > 0f) _predatorCombatCooldownTimer = Mathf.Max(0f, _predatorCombatCooldownTimer - elapsed);
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

        private CreatureAgentView FindNearestSceneCreatureByRole(CreatureRole role, float maxDistance)
        {
            _worldQuery = WorldQueryRuntime.GetOrCreate(EcosystemRuntime.Instance) ?? _worldQuery;
            if (_worldQuery == null)
            {
                return null;
            }

            return _worldQuery.TryFindNearestCreatureByRole(transform.position, role, maxDistance, out CreatureAgentView found) ? found : null;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b) => Mathf.Sqrt(HorizontalSqrDistance(a, b));
        private static float HorizontalSqrDistance(Vector3 a, Vector3 b) { float dx = a.x - b.x; float dz = a.z - b.z; return dx * dx + dz * dz; }
        private void UpdateHabitatMemory(string creatureId)
        {
            string biomeId = _worldQuery != null ? _worldQuery.GetHabitatIdForPosition(transform.position) : HabitatIds.JungleInterior;
            if (string.IsNullOrWhiteSpace(biomeId))
            {
                biomeId = HabitatIds.JungleInterior;
            }

            CurrentHabitatId = biomeId;
            if (!_habitatMemoryInitialized)
            {
                HomeHabitatId = biomeId;
                PopulationHabitatId = biomeId;
                _habitatMemoryInitialized = true;
            }
        }
        private void UpdateGrazerNiche()
        {
            float biomassPercent = GetCurrentBiomePlantBiomassPercent();
            HungerStage stage = _needs.State.Stage;
            bool highRisk = stage == HungerStage.Desperate || biomassPercent <= foragerLowBiomassPercent;
            CurrentNiche = highRisk ? "OMNIVORE" : "HERBIVORE";
        }

        private float GetCurrentBiomePlantBiomassPercent()
        {
            EcosystemDirectorRuntime director = EcosystemDirectorRuntime.Active;
            if (director == null)
            {
                return 100f;
            }

            BiomeEcosystemState state = director.GetBiomeState(LegacyBiomeCompatibility.ToLegacyBiomeId(CurrentHabitatId));
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
                   || biomassPercent <= foragerLowBiomassPercent;
        }

        private bool CanGrazerHuntSmallPrey(FoodSourceView plantFood, HungerStage hungerStage, float riskDrive, float biomassPercent, WorldQueryRuntime query, out CreatureAgentView prey)
        {
            prey = null;
            if (plantFood != null || query == null)
            {
                return false;
            }

            if (!query.TryFindNearestCreatureByRole(transform.position, CreatureRole.SmallPrey, foragerSmallPreyDetectRange, out prey) || prey == null)
            {
                return false;
            }

            if (hungerStage == HungerStage.Desperate)
            {
                return true;
            }

            return hungerStage == HungerStage.Starving
                   && (biomassPercent <= foragerLowBiomassPercent || (CurrentNiche == "OMNIVORE" && riskDrive >= foragerPredationRiskThreshold && foragerAggression + _needs.Diet.MeatPreference >= 0.18f));
        }
        private void UpdateTargetMemory(string creatureId) { if (_currentPrey != null && (!_currentPrey.isActiveAndEnabled || HorizontalDistance(transform.position, _currentPrey.transform.position) > preySightRange * 1.25f)) _currentPrey = null; if (_currentFood != null && (!_currentFood.isActiveAndEnabled || _currentFood.IsEmpty)) _currentFood = null; }
        private bool TryFleePredator(WorldQueryRuntime query, string creatureId) { float fleeRange = _agentView.Role == CreatureRole.SmallPrey ? fleeRangeSmallPrey * 1.5f : fleeRangeHerbivore * 1.25f; float fleeDistance = _agentView.Role == CreatureRole.SmallPrey ? fleeDistanceSmallPrey * 1.25f : fleeDistanceHerbivore * 1.15f; float scanRange = Mathf.Max(fleeRange, threatFallbackScanRadius); CreatureAgentView predator = query != null && query.TryFindNearestCreatureByRole(transform.position, CreatureRole.Predator, scanRange, out CreatureAgentView foundPredator) ? foundPredator : null; if (predator == null) return false; float distance = HorizontalDistance(transform.position, predator.transform.position); if (distance > scanRange) return false; Vector3 away = transform.position - predator.transform.position; away.y = 0f; if (away.sqrMagnitude < 0.001f) { away = Random.insideUnitSphere; away.y = 0f; } _currentPrey = null; _currentFood = null; _player = null; SetState(CreatureBehaviorState.Flee, $"flee_predator d:{distance:0.0}"); SetWanderEnabled(false); MoveToTarget(transform.position + away.normalized * Mathf.Max(4f, fleeDistance)); return true; }
        private bool TryFleePlayer(string creatureId) { ResolvePlayer(); if (_player == null) return false; float fleeRange = _agentView.Role == CreatureRole.SmallPrey ? fleeRangeSmallPrey : fleeRangeHerbivore; float fleeDistance = _agentView.Role == CreatureRole.SmallPrey ? fleeDistanceSmallPrey : fleeDistanceHerbivore; float distance = HorizontalDistance(transform.position, _player.position); if (distance > fleeRange) return false; Vector3 away = transform.position - _player.position; away.y = 0f; if (away.sqrMagnitude < 0.001f) { away = Random.insideUnitSphere; away.y = 0f; } _currentPrey = null; _currentFood = null; SetState(CreatureBehaviorState.Flee, $"flee_player d:{distance:0.0}"); SetWanderEnabled(false); MoveToTarget(transform.position + away.normalized * Mathf.Max(4f, fleeDistance)); if (_agentView.Role == CreatureRole.SmallPrey) _panicTimer = smallPreyPanicDuration; return true; }
        private void MoveToTarget(Vector3 targetPosition) { CreatureNavigationAdapter adapter = _agentView != null ? _agentView.GetNavigationAdapter() : null; if (adapter != null && adapter.TrySamplePosition(targetPosition, out Vector3 navTarget, navSampleDistance)) { _agentView.MoveTo(navTarget); return; } _agentView.MoveTo(targetPosition); }
        private void ResolvePlayer() { if (_player != null && _player.gameObject.activeInHierarchy) return; _player = PlayerPresenceRuntime.ActiveTransform; }
        private void SetState(CreatureBehaviorState next) => SetState(next, DecisionReason);
        private void SetState(CreatureBehaviorState next, string reason) { _state = next; DecisionReason = string.IsNullOrWhiteSpace(reason) ? next.ToString() : reason; if (_debugOverlay != null) _debugOverlay.SetBehaviorState(next); }
        private GameplayEventKind GetPredatorHuntEventKind(CreatureRole targetRole)
        {
            return targetRole == CreatureRole.HerbivoreOmnivore ? GameplayEventKind.VarnakHuntedGrazer : GameplayEventKind.VarnakHuntedSmallPrey;
        }

        private void PublishCreatureGameplayEvent(GameplayEventKind kind, string targetKind, float amount, float nutrition, float biomassImpact, string message)
        {
            string actor = _agentView != null && !string.IsNullOrWhiteSpace(_agentView.CreatureId) ? _agentView.CreatureId : gameObject.name;
            GameEventBus.PublishCreatureEvent(kind, transform.position, ResolveEventBiomeId(), actor, targetKind, amount, nutrition, biomassImpact, message);
        }

        private void TryPublishPredatorCombatEvent(GameplayEventKind kind, string targetKind, string message)
        {
            if (_predatorCombatCooldownTimer > 0f)
            {
                return;
            }

            PublishCreatureGameplayEvent(kind, targetKind, 1f, 0f, 0f, message);
            if (kind == GameplayEventKind.VarnakAttackedPlayer && _player != null)
            {
                PlayerSurvivalRuntime survival = _player.GetComponent<PlayerSurvivalRuntime>() ?? _player.GetComponentInParent<PlayerSurvivalRuntime>();
                if (survival != null)
                {
                    float damageAmount = Mathf.Max(0f, predatorPlayerDamage);
                    Debug.Log($"[Predator] Applying {damageAmount} damage to player at {_player.position}");
                    survival.Damage(damageAmount);
                    ProceduralCombatAudio.PlayMeleeHit(_player.position + Vector3.up * 0.9f, 0.60f);
                }
                else
                {
                    Debug.LogWarning($"[Predator] Could not find PlayerSurvivalRuntime on player object or its parent!");
                }
            }

            _predatorCombatCooldownTimer = 1.25f;
        }

        private string ResolveEventBiomeId()
        {
            if (!string.IsNullOrWhiteSpace(CurrentHabitatId) && CurrentHabitatId != "default") return LegacyBiomeCompatibility.ToLegacyBiomeId(CurrentHabitatId);
            return _worldQuery != null ? _worldQuery.GetBiomeIdForPosition(transform.position) : "default";
        }
        private void SetWanderEnabled(bool enabled) { if (_wander != null) _wander.enabled = enabled; }
        public void SetBehaviorStateForTests(CreatureBehaviorState state, string reason = "test") => SetState(state, reason);
        private static string NormalizeSavedHabitat(string value) => HabitatIds.Normalize(value) == value
            ? value : LegacyBiomeCompatibility.FromLegacyBiomeId(value);

        // Compatibility readers for legacy diagnostic consumers; new saves use habitat fields.
        public string CurrentBiomeId => LegacyBiomeCompatibility.ToLegacyBiomeId(CurrentHabitatId);
        public string HomeBiomeId => LegacyBiomeCompatibility.ToLegacyBiomeId(HomeHabitatId);
        public string PopulationBiomeId => LegacyBiomeCompatibility.ToLegacyBiomeId(PopulationHabitatId);

        public void RestoreSaveState(string behaviorState, string decisionReason, string lastFoodSource, string currentBiomeId, string homeBiomeId, string populationBiomeId, float attackCooldown, string currentNiche, float huntDrive)
        {
            if (!System.Enum.TryParse(behaviorState, true, out CreatureBehaviorState parsedState))
            {
                parsedState = CreatureBehaviorState.Wander;
            }

            SetState(parsedState, string.IsNullOrWhiteSpace(decisionReason) ? "save_load" : decisionReason);
            LastFoodSource = string.IsNullOrWhiteSpace(lastFoodSource) ? "none" : lastFoodSource;
            CurrentHabitatId = string.IsNullOrWhiteSpace(currentBiomeId) ? HabitatIds.JungleInterior : NormalizeSavedHabitat(currentBiomeId);
            HomeHabitatId = string.IsNullOrWhiteSpace(homeBiomeId) ? CurrentHabitatId : NormalizeSavedHabitat(homeBiomeId);
            PopulationHabitatId = string.IsNullOrWhiteSpace(populationBiomeId) ? CurrentHabitatId : NormalizeSavedHabitat(populationBiomeId);
            _habitatMemoryInitialized = true;
            _predatorCombatCooldownTimer = Mathf.Max(0f, attackCooldown);
            CurrentNiche = string.IsNullOrWhiteSpace(currentNiche) ? CurrentNiche : currentNiche.Trim();
            HuntDrive = Mathf.Clamp01(huntDrive);

            if (parsedState == CreatureBehaviorState.Dead)
            {
                _currentFood = null;
                _currentPrey = null;
                _panicTimer = 0f;
                _eatCooldownTimer = 0f;
            }
        }
        public void OnCreatureDied() { SetState(CreatureBehaviorState.Dead); SetWanderEnabled(false); _currentPrey = null; _currentFood = null; _eatCooldownTimer = 0f; _panicTimer = 0f; _predatorCombatCooldownTimer = 0f; }
    }
}
