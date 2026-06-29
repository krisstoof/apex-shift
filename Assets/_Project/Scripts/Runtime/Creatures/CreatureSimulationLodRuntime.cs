using UnityEngine;
using ApexShift.Runtime.Config;

namespace ApexShift.Runtime.Creatures
{
    public enum CreatureSimulationLodLevel
    {
        Near,
        Medium,
        Far
    }

    [DisallowMultipleComponent]
    public sealed class CreatureSimulationLodRuntime : MonoBehaviour
    {
        // Ported from apex-shift-2d/scripts/creatures/creature_simulation_lod.gd
        // and GameBalance.CREATURE_SIMULATION_LOD.
        // Godot used pixel distances 900/1800. Unity runtime uses world units, so
        // defaults are scaled down by 10 while preserving the same near/medium/far ratio.
        [SerializeField] private float nearDistance = 90f;
        [SerializeField] private float mediumDistance = 180f;
        [SerializeField] private float forceVarnakNearDistance = 70f;
        [SerializeField] private float mediumAiIntervalMultiplier = 3f;
        [SerializeField] private float mediumSpatialUpdateMultiplier = 2f;
        [SerializeField] private float farUpdateIntervalSeconds = 3f;
        [SerializeField] private float backgroundSimulationIntervalSeconds = 3f;
        [SerializeField] private bool debugEnabled = true;
        [SerializeField] private CreatureSimulationLodConfig lodConfig;

        private CreatureAgentView _agentView;
        private Transform _player;
        private float _farSimulationTimer;
        private float _backgroundSimulationTimer;

        public CreatureSimulationLodLevel Level { get; private set; } = CreatureSimulationLodLevel.Near;
        public CreatureSimulationLodLevel LastLevel { get; private set; } = CreatureSimulationLodLevel.Near;
        public string LevelName => GetLevelName(Level);
        public float DistanceToPlayer { get; private set; }
        public int LodChangeCount { get; private set; }
        public int ActiveSimulationTickCount { get; private set; }
        public int FarSimulationTickCount { get; private set; }
        public int BackgroundSimulationTickCount { get; private set; }
        public bool IsVisibilityCulled { get; private set; }
        public bool IsBackgroundSimulated { get; private set; }
        public float FarUpdateIntervalSeconds => farUpdateIntervalSeconds;
        public float MediumSpatialUpdateMultiplier => mediumSpatialUpdateMultiplier;
        public bool DebugEnabled => debugEnabled;
        public bool IsNear => Level == CreatureSimulationLodLevel.Near;
        public bool IsMedium => Level == CreatureSimulationLodLevel.Medium;
        public bool IsFar => Level == CreatureSimulationLodLevel.Far;
        public bool IsBackgroundSimulationMode => IsVisibilityCulled && !IsFar;
        public bool ShouldRunFullAi => !IsFar && !IsBackgroundSimulationMode;

        private void Awake()
        {
            Cache();
            ApplyConfig(lodConfig);
        }

        private void OnEnable()
        {
            Cache();
            ApplyConfig(lodConfig);
        }

        public void Tick(float deltaTime, string creatureId = null)
        {
            Cache();
            ResolvePlayer();
            ResolveCurrentLevel(creatureId);

            if (IsFar)
            {
                IsBackgroundSimulated = false;
                return;
            }

            if (IsBackgroundSimulationMode)
            {
                return;
            }

            _backgroundSimulationTimer = 0f;
            IsBackgroundSimulated = false;
            ActiveSimulationTickCount++;
        }

        public bool TryConsumeFarTick(float deltaTime, out float elapsedSeconds)
        {
            elapsedSeconds = 0f;
            if (!IsFar)
            {
                _farSimulationTimer = 0f;
                return false;
            }

            _farSimulationTimer += Mathf.Max(0f, deltaTime);
            if (_farSimulationTimer < farUpdateIntervalSeconds)
            {
                return false;
            }

            elapsedSeconds = _farSimulationTimer;
            _farSimulationTimer = 0f;
            FarSimulationTickCount++;
            return true;
        }

        public bool TryConsumeBackgroundTick(float deltaTime, out float elapsedSeconds)
        {
            elapsedSeconds = 0f;
            if (!IsBackgroundSimulationMode)
            {
                _backgroundSimulationTimer = 0f;
                return false;
            }

            _backgroundSimulationTimer += Mathf.Max(0f, deltaTime);
            if (_backgroundSimulationTimer < backgroundSimulationIntervalSeconds)
            {
                return false;
            }

            elapsedSeconds = _backgroundSimulationTimer;
            _backgroundSimulationTimer = 0f;
            BackgroundSimulationTickCount++;
            IsBackgroundSimulated = true;
            return true;
        }

        public float GetEffectiveAiInterval(float baseInterval)
        {
            float safeBase = Mathf.Max(0.01f, baseInterval);
            return IsMedium ? safeBase * Mathf.Max(1f, mediumAiIntervalMultiplier) : safeBase;
        }

        public void SetVisibilityCulled(bool isCulled)
        {
            if (IsVisibilityCulled == isCulled)
            {
                return;
            }

            IsVisibilityCulled = isCulled;
            _backgroundSimulationTimer = 0f;
            IsBackgroundSimulated = isCulled && !IsFar;
        }

        public void SetPlayerForTests(Transform player)
        {
            _player = player;
        }

        public void ApplyConfig(CreatureSimulationLodConfig config)
        {
            if (config == null)
            {
                return;
            }

            nearDistance = config.NearDistance;
            mediumDistance = config.MediumDistance;
            forceVarnakNearDistance = config.ForceVarnakNearDistance;
            mediumAiIntervalMultiplier = config.MediumAiIntervalMultiplier;
            mediumSpatialUpdateMultiplier = config.MediumSpatialUpdateMultiplier;
            farUpdateIntervalSeconds = config.FarUpdateIntervalSeconds;
            backgroundSimulationIntervalSeconds = config.BackgroundSimulationIntervalSeconds;
            debugEnabled = config.DebugEnabled;
        }

        public void ForceDistancesForTests(float near, float medium, float forceVarnakNear)
        {
            nearDistance = Mathf.Max(0.01f, near);
            mediumDistance = Mathf.Max(nearDistance, medium);
            forceVarnakNearDistance = Mathf.Max(0f, forceVarnakNear);
        }

        public static CreatureSimulationLodLevel ResolveLevel(
            float distanceToPlayer,
            float nearDistance,
            float mediumDistance,
            string creatureType = null,
            float forceVarnakNearDistance = 0f)
        {
            float distance = Mathf.Max(0f, distanceToPlayer);
            float near = Mathf.Max(0.01f, nearDistance);
            float medium = Mathf.Max(near, mediumDistance);
            string normalizedType = (creatureType ?? string.Empty).Trim().ToLowerInvariant();

            if (normalizedType == "varnak" && forceVarnakNearDistance > 0f && distance <= forceVarnakNearDistance)
            {
                return CreatureSimulationLodLevel.Near;
            }

            if (distance <= near)
            {
                return CreatureSimulationLodLevel.Near;
            }

            return distance <= medium ? CreatureSimulationLodLevel.Medium : CreatureSimulationLodLevel.Far;
        }

        public static string GetLevelName(CreatureSimulationLodLevel level)
        {
            return level switch
            {
                CreatureSimulationLodLevel.Near => "near",
                CreatureSimulationLodLevel.Medium => "medium",
                CreatureSimulationLodLevel.Far => "far",
                _ => "unknown"
            };
        }

        private void Cache()
        {
            if (_agentView == null)
            {
                _agentView = GetComponent<CreatureAgentView>();
            }
        }

        private void ResolveCurrentLevel(string creatureId)
        {
            if (_player == null)
            {
                DistanceToPlayer = 0f;
                SetLevel(CreatureSimulationLodLevel.Near);
                return;
            }

            DistanceToPlayer = HorizontalDistance(transform.position, _player.position);
            string resolvedCreatureId = string.IsNullOrWhiteSpace(creatureId)
                ? _agentView != null ? _agentView.CreatureId : string.Empty
                : creatureId;

            SetLevel(ResolveLevel(DistanceToPlayer, nearDistance, mediumDistance, resolvedCreatureId, forceVarnakNearDistance));
        }

        private void SetLevel(CreatureSimulationLodLevel nextLevel)
        {
            if (Level == nextLevel)
            {
                return;
            }

            LastLevel = Level;
            Level = nextLevel;
            LodChangeCount++;
            _farSimulationTimer = 0f;
            _backgroundSimulationTimer = 0f;
        }

        private void ResolvePlayer()
        {
            if (_player != null && _player.gameObject.activeInHierarchy)
            {
                return;
            }

            GameObject playerObject = null;
            try
            {
                playerObject = GameObject.FindWithTag("Player");
            }
            catch (UnityException)
            {
            }

            if (playerObject == null)
            {
                playerObject = GameObject.Find("Player");
            }

            if (playerObject == null)
            {
                var controller = Object.FindAnyObjectByType<ApexShift.Runtime.Player.IsometricPlayerController>();
                if (controller != null)
                {
                    playerObject = controller.gameObject;
                }
            }

            _player = playerObject != null ? playerObject.transform : null;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
