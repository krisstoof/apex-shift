using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Camera;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.Story;
using ApexShift.Runtime.World;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Topography;
using UnityEngine;

namespace ApexShift.Runtime.Escape
{
    public enum RaftState { Placed, Launched, Failed }

    [DisallowMultipleComponent]
    public sealed class RaftRuntime : MonoBehaviour
    {
        [SerializeField] private float movementSpeed = 3.5f;
        [SerializeField] private float minDistanceToCoast;
        [SerializeField] private float maxDistanceToCoast = 16f;
        [SerializeField] private OceanDangerProfile dangerProfile = new OceanDangerProfile();
        private IslandTopographyRuntime topography;
        private GameObject player;
        private IsometricPlayerController controller;
        private PlayerInputReader input;
        private CharacterController characterController;
        private bool originalCharacterControllerEnabled;
        private bool originalMovementEnabled;
        private Transform visual;
        private float visualTime;

        public RaftState State { get; private set; } = RaftState.Placed;
        public bool IsAttemptActive => State == RaftState.Launched && player != null;
        public float Danger01 { get; private set; }
        public float Durability01 { get; private set; } = 1f;
        public Vector3 SafeReturnPoint { get; private set; }
        public Vector3 LaunchPosition { get; private set; }
        public GameObject MountedPlayer => player;
        public float MovementSpeed => movementSpeed;
        public string Prompt => "Board raft";

        public void Configure(float minDistance = 0f, float maxDistance = 16f)
        {
            minDistanceToCoast = Mathf.Max(0f, minDistance);
            maxDistanceToCoast = Mathf.Max(minDistanceToCoast, maxDistance);
        }

        private void ResolveTopography()
        {
            WorldGeneratorRuntime generator = GetComponentInParent<WorldGeneratorRuntime>();
            topography = generator != null ? generator.CurrentGeneration?.IslandTopography : IslandTopographyRuntime.Active;
        }

        public bool CanInteract(GameObject actor)
        {
            if (!isActiveAndEnabled || State != RaftState.Placed || actor == null
                || actor.GetComponent<IsometricPlayerController>() == null) return false;
            ResolveTopography();
            return PlaceableSurfaceValidation.ValidateWater(transform.position, topography, WorldBounds.Active,
                minDistanceToCoast, maxDistanceToCoast).isValid && TryResolveSafeReturn(out _);
        }

        public bool TryLaunch(GameObject actor)
        {
            if (!CanInteract(actor) || !TryResolveSafeReturn(out Vector3 safe)) return false;
            // Never move the player under BuildingRoot: generation ownership stays intact,
            // and load/clear can safely destroy both siblings without reparenting during teardown.
            player = actor;
            controller = actor.GetComponent<IsometricPlayerController>();
            input = actor.GetComponent<PlayerInputReader>();
            characterController = actor.GetComponent<CharacterController>();
            originalCharacterControllerEnabled = characterController != null && characterController.enabled;
            originalMovementEnabled = controller.MovementEnabled;
            SafeReturnPoint = safe;
            LaunchPosition = transform.position;
            controller.SetMovementEnabled(false);
            if (characterController != null) characterController.enabled = false;
            State = RaftState.Launched;
            visual = transform.Find("RaftVisual");
            SynchronizePlayer();
            return true;
        }

        private bool TryResolveSafeReturn(out Vector3 safe)
        {
            safe = default;
            if (topography == null) return false;
            float best = float.PositiveInfinity;
            foreach (Vector3 candidate in topography.GetAllLandCenters())
            {
                if (!IsSafeReturn(candidate)) continue;
                float distance = new Vector2(candidate.x - transform.position.x, candidate.z - transform.position.z).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                safe = candidate + Vector3.up * 0.15f;
            }
            if (!float.IsPositiveInfinity(best)) return true;
            Vector3 fallback = topography.GetSafePlayerSpawnPoint();
            if (!IsSafeReturn(fallback)) return false;
            safe = fallback + Vector3.up * 0.15f;
            return true;
        }

        private bool IsSafeReturn(Vector3 candidate)
            => topography.TryGetEnvironmentAt(candidate, out EnvironmentSample sample)
                && sample.IsLand && !sample.IsWater && !sample.IsShoreline && sample.SlopeDegrees <= 16f
                && WorldBounds.Active != null && WorldBounds.Active.Contains(candidate);

        private void Update()
        {
            if (!IsAttemptActive || Time.deltaTime <= 0f) return;
            Tick(input != null ? input.Move : Vector2.zero, Time.deltaTime);
            if (!IsAttemptActive || visual == null) return;
            visualTime += Time.deltaTime;
            float frequency = 1.5f + Danger01 * 3f;
            visual.localPosition = Vector3.up * Mathf.Sin(visualTime * frequency) * (0.03f + 0.15f * Danger01);
            visual.localRotation = Quaternion.Euler(Mathf.Sin(visualTime * frequency * 0.8f) * (1f + 7f * Danger01),
                0f, Mathf.Sin(visualTime * frequency * 1.1f) * (1f + 9f * Danger01));
        }

        public void Tick(Vector2 move, float deltaTime)
        {
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            Vector3 forward = camera != null ? Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = camera != null ? Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized : Vector3.right;
            TickWorldDirection(forward * move.y + right * move.x, deltaTime);
        }

        /// <summary>Shared movement integration used by input and deterministic simulation tests.</summary>
        public void TickWorldDirection(Vector3 direction, float deltaTime)
        {
            if (!IsAttemptActive || deltaTime <= 0f) return;
            direction.y = 0f;
            direction = Vector3.ClampMagnitude(direction, 1f);
            Vector3 next = transform.position + direction * movementSpeed * deltaTime;
            next.y = WorldWaterLevel.SurfaceY;
            Bounds grid = topography.WorldBounds;
            if (next.x <= grid.min.x + 1f || next.x >= grid.max.x - 1f
                || next.z <= grid.min.z + 1f || next.z >= grid.max.z - 1f
                || !topography.TryGetEnvironmentAt(next, out EnvironmentSample environment))
            {
                Danger01 = 1f;
                FailEscapeAttempt();
                return;
            }
            // Land is not navigable. Turning around is allowed; do not drive through the island.
            if (!environment.IsWater || environment.IsLand) return;
            transform.position = next;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            SynchronizePlayer();
            OceanDangerSample danger = OceanDangerEvaluator.Evaluate(dangerProfile, environment.DistanceToCoast, Durability01, deltaTime);
            Danger01 = danger.Danger01;
            Durability01 = danger.Durability01;
            if (danger.ShouldFail) FailEscapeAttempt();
        }

        private void SynchronizePlayer()
        {
            if (player != null) player.transform.SetPositionAndRotation(transform.position + Vector3.up * 0.65f, transform.rotation);
        }

        public void FailEscapeAttempt()
        {
            if (State != RaftState.Launched) return;
            State = RaftState.Failed;
            ReturnPlayer();
            PlaceableStructureRuntime structure = GetComponent<PlaceableStructureRuntime>();
            string instanceId = structure != null ? structure.InstanceId : string.Empty;
            BuildingRegistry.Active?.Unregister(structure);
            WorldGeneratorRuntime generator = GetComponentInParent<WorldGeneratorRuntime>();
            StoryProgressionRuntime story = generator != null ? generator.CurrentGeneration?.StoryProgression
                : transform.root.GetComponentInChildren<StoryProgressionRuntime>(true);
            if (story == null || !story.HasMilestone(StorySignalIds.RaftEscapeFailed))
                GameEventBus.PublishStorySignal(StorySignalIds.RaftEscapeFailed, instanceId);
            gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }

        private void ReturnPlayer()
        {
            if (player == null) return;
            player.transform.position = SafeReturnPoint;
            Physics.SyncTransforms();
            if (characterController != null) characterController.enabled = originalCharacterControllerEnabled;
            if (controller != null)
            {
                controller.SetMovementEnabled(originalMovementEnabled);
                controller.RefreshWaterState();
            }
            UnityEngine.Camera camera = UnityEngine.Camera.main;
            IsometricCameraFollow follow = camera != null ? camera.GetComponent<IsometricCameraFollow>() : null;
            if (follow != null) { follow.SetTarget(player.transform); follow.SnapToTarget(); }
            player = null;
        }

        private void OnDisable()
        {
            // Abort on load/clear/removal without claiming an ocean failure.
            if (State == RaftState.Launched) { State = RaftState.Failed; ReturnPlayer(); }
        }
    }
}
