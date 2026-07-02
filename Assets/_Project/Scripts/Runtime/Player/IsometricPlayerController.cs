using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.World;
using ApexShift.Runtime.World.Topography;
using UnityEngine;
using CameraComponent = UnityEngine.Camera;

namespace ApexShift.Runtime.Player
{
    public sealed class IsometricPlayerController : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private PlayerSurvivalRuntime survivalRuntime;

        [SerializeField]
        private float walkSpeed = 5f;

        [SerializeField]
        private float sprintSpeed = 8f;

        [SerializeField]
        private float turnSpeed = 18f;

        [SerializeField]
        private bool usePhysicsMovement = true;

        [SerializeField]
        private bool movementEnabled = true;

        [Header("Water / Swimming")]
        [SerializeField]
        [Tooltip("Movement speed while swimming.")]
        private float swimSpeed = 2.35f;

        [SerializeField]
        [Tooltip("Player root Y while swimming. This is below water surface so the character looks submerged.")]
        private float waterSurfaceY = -0.85f;

        [SerializeField]
        [Tooltip("How quickly the player root is pulled down/up when entering or leaving water.")]
        private float waterVerticalBlendSpeed = 10f;

        [Header("Swimming Visuals")]
        [SerializeField]
        [Tooltip("Child transform used as visual root. If empty, the first child with a Renderer is used.")]
        private Transform visualRoot;

        [SerializeField]
        [Tooltip("Additional local Y offset applied to the visual only while swimming.")]
        private float swimVisualYOffset = -0.62f;

        [SerializeField]
        [Tooltip("Forward pitch applied to the visual while swimming. Makes the character look like swimming instead of standing.")]
        private float swimVisualPitchDegrees = 58f;

        [SerializeField]
        private float swimVisualBlendSpeed = 8f;

        [SerializeField]
        [Tooltip("Confirm water state from topography every frame instead of relying only on trigger callbacks.")]
        private bool useTopographyWaterState = true;

        [SerializeField]
        private bool logWaterStateChanges;

        private CharacterController characterController;
        private Rigidbody rigidbodyComponent;
        private PlayerAnimationDriver animationDriver;
        private bool isInWater;
        private Vector3 visualDefaultLocalPosition;
        private Quaternion visualDefaultLocalRotation = Quaternion.identity;
        private float _verticalVelocity;
        private const float Gravity = -22f;
        private bool hadTopographyState;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (survivalRuntime == null)
            {
                survivalRuntime = GetComponent<PlayerSurvivalRuntime>();
            }

            characterController = GetComponent<CharacterController>();
            rigidbodyComponent = GetComponent<Rigidbody>();
            animationDriver = GetComponent<PlayerAnimationDriver>();
            ResolveVisualRoot();
        }

        private void Update()
        {
            if (inputReader == null)
            {
                return;
            }

            if (Time.timeScale < 0.01f)
            {
                return;
            }

            if (!movementEnabled)
            {
                RotateTowardLookPosition(inputReader.LookScreenPosition);
                return;
            }

            Vector2 input = inputReader.Move;
            Vector3 movement = CalculateCameraRelativeMovement(input);
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            SyncWaterStateFromTopography();
            MoveWithWorldBounds(movement);
            ApplyGravity();
            SyncWaterStateFromTopography();
            UpdateSwimmingVisuals();
            RotateTowardLookPosition(inputReader.LookScreenPosition);
        }

        /// <summary>
        /// Queries IslandTopographyRuntime to sync swim state.
        /// This is the authoritative water check; trigger callbacks are only a fast hint.
        /// </summary>
        private void SyncWaterStateFromTopography()
        {
            if (!useTopographyWaterState)
            {
                return;
            }

            IslandTopographyRuntime topo = IslandTopographyRuntime.Active;
            if (topo == null || !topo.IsBuilt) return;

            bool onWater = topo.IsWaterAt(transform.position.x, transform.position.z);
            hadTopographyState = true;
            SetWaterState(onWater, "topography");
        }

        private bool IsWaterAtPosition(Vector3 position)
        {
            IslandTopographyRuntime topo = IslandTopographyRuntime.Active;
            if (topo != null && topo.IsBuilt)
            {
                return topo.IsWaterAt(position.x, position.z);
            }

            return isInWater;
        }

        private void SetWaterState(bool inWater, string reason)
        {
            if (isInWater == inWater)
            {
                return;
            }

            if (inWater)
            {
                isInWater = true;
                _verticalVelocity = 0f;
                if (animationDriver == null) animationDriver = GetComponent<PlayerAnimationDriver>();
                ResolveVisualRoot();
                animationDriver?.SetSwimming(true);
                if (logWaterStateChanges) Debug.Log($"[Controller] Entered water via {reason}", this);
            }
            else
            {
                isInWater = false;
                if (animationDriver == null) animationDriver = GetComponent<PlayerAnimationDriver>();
                ResolveVisualRoot();
                animationDriver?.SetSwimming(false);
                if (logWaterStateChanges) Debug.Log($"[Controller] Exited water via {reason}", this);
            }
        }

        private void ApplyGravity()
        {
            if (characterController == null || !characterController.enabled || !usePhysicsMovement)
                return;
            if (isInWater)
            {
                _verticalVelocity = 0f;
                return;
            }
            if (characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;      // small constant keeps CharacterController grounded
            else
                _verticalVelocity += Gravity * Time.deltaTime;
            characterController.Move(new Vector3(0f, _verticalVelocity * Time.deltaTime, 0f));
        }

        private Vector3 CalculateCameraRelativeMovement(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }

            CameraComponent mainCamera = CameraComponent.main;
            if (mainCamera == null)
            {
                return new Vector3(input.x, 0f, input.y);
            }

            Vector3 cameraForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;
            Vector3 movement = cameraRight * input.x + cameraForward * input.y;
            movement.y = 0f;
            return movement;
        }

        private void MoveWithWorldBounds(Vector3 movement)
        {
            if (movement.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector3 currentPosition = transform.position;
            float speed = GetCurrentMovementSpeed();
            Vector3 desiredPosition = currentPosition + movement * (speed * Time.deltaTime);

            bool desiredWaterState = IsWaterAtPosition(desiredPosition);
            if (desiredWaterState != isInWater)
            {
                SetWaterState(desiredWaterState, "desired_position");
            }

            // While swimming, pull the player below the water surface. This makes the
            // state visually distinct from simply walking on a blue floor.
            if (isInWater)
            {
                desiredPosition.y = Mathf.Lerp(currentPosition.y, waterSurfaceY, Mathf.Clamp01(waterVerticalBlendSpeed * Time.deltaTime));
            }

            WorldBounds worldBounds = WorldBounds.Active;
            if (worldBounds == null)
            {
                transform.position = desiredPosition;
                return;
            }

            if (worldBounds.Contains(desiredPosition))
            {
                ApplyMovement(desiredPosition);
                return;
            }

            Vector3 xOnly = new Vector3(desiredPosition.x, currentPosition.y, currentPosition.z);
            if (worldBounds.Contains(xOnly))
            {
                ApplyMovement(xOnly);
                return;
            }

            Vector3 zOnly = new Vector3(currentPosition.x, currentPosition.y, desiredPosition.z);
            if (worldBounds.Contains(zOnly))
            {
                ApplyMovement(zOnly);
                return;
            }

            Vector3 clamped = worldBounds.ClampToNearestAllowed(desiredPosition);
            clamped.y = isInWater ? Mathf.Lerp(currentPosition.y, waterSurfaceY, Mathf.Clamp01(waterVerticalBlendSpeed * Time.deltaTime)) : currentPosition.y;
            ApplyMovement(clamped);
        }

        private void RotateTowardLookPosition(Vector2 screenPosition)
        {
            CameraComponent mainCamera = CameraComponent.main;
            if (mainCamera == null)
            {
                return;
            }

            if (screenPosition.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(screenPosition);
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (!groundPlane.Raycast(ray, out float enter))
            {
                return;
            }

            Vector3 hitPoint = ray.GetPoint(enter);
            Vector3 direction = hitPoint - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Mathf.Clamp01(turnSpeed * Time.deltaTime));
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            inputReader = reader;
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
        }

        /// <summary>Called by PlayerWaterDetector when the player enters a water trigger volume.</summary>
        public void EnterWater()
        {
            if (!hadTopographyState || IsWaterAtPosition(transform.position)) SetWaterState(true, "trigger");
        }

        /// <summary>Called by PlayerWaterDetector when the player leaves all water trigger volumes.</summary>
        public void ExitWater()
        {
            if (!hadTopographyState || !IsWaterAtPosition(transform.position)) SetWaterState(false, "trigger");
        }

        public bool IsInWater => isInWater;

        public void SetSurvivalRuntime(PlayerSurvivalRuntime runtime)
        {
            survivalRuntime = runtime;
        }

        private void ApplyMovement(Vector3 position)
        {
            if (!usePhysicsMovement)
            {
                transform.position = position;
                return;
            }

            if (characterController != null && characterController.enabled)
            {
                Vector3 delta = position - transform.position;
                if (delta.sqrMagnitude > 0.000001f)
                {
                    characterController.Move(delta);
                }

                return;
            }

            if (rigidbodyComponent != null && !rigidbodyComponent.isKinematic)
            {
                rigidbodyComponent.MovePosition(position);
                return;
            }

            transform.position = position;
        }

        private void ResolveVisualRoot()
        {
            if (visualRoot != null)
            {
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.transform == transform)
                {
                    continue;
                }

                visualRoot = renderer.transform;
                break;
            }

            if (visualRoot == null && transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0);
            }

            if (visualRoot != null)
            {
                visualDefaultLocalPosition = visualRoot.localPosition;
                visualDefaultLocalRotation = visualRoot.localRotation;
            }
        }

        private void UpdateSwimmingVisuals()
        {
            ResolveVisualRoot();
            if (visualRoot == null)
            {
                return;
            }

            float blend = Mathf.Clamp01(swimVisualBlendSpeed * Time.deltaTime);
            if (isInWater)
            {
                Vector3 targetLocalPosition = visualDefaultLocalPosition + Vector3.up * swimVisualYOffset;
                Quaternion targetLocalRotation = visualDefaultLocalRotation * Quaternion.Euler(swimVisualPitchDegrees, 0f, 0f);

                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetLocalPosition, blend);
                visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetLocalRotation, blend);
            }
            else
            {
                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualDefaultLocalPosition, blend);
                visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, visualDefaultLocalRotation, blend);
            }
        }

        private float GetCurrentMovementSpeed()
        {
            if (isInWater)
            {
                float sm = survivalRuntime != null ? Mathf.Max(0f, survivalRuntime.SpeedMultiplier) : 1f;
                return swimSpeed * sm;
            }

            bool shouldSprint;
            float speedMultiplier = 1f;
            if (survivalRuntime != null)
            {
                shouldSprint = inputReader != null && inputReader.SprintHeld && survivalRuntime.CanSprint;
                speedMultiplier = survivalRuntime.SpeedMultiplier;
            }
            else
            {
                shouldSprint = inputReader != null && inputReader.SprintHeld;
            }

            return (shouldSprint ? sprintSpeed : walkSpeed) * Mathf.Max(0f, speedMultiplier);
        }
    }
}
