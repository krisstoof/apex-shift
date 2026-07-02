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
        private float swimSpeed = 3f;

        [SerializeField]
        [Tooltip("Y position of the water surface. The player floats at this height while in water.")]
        private float waterSurfaceY = -0.3f;

        private CharacterController characterController;
        private Rigidbody rigidbodyComponent;
        private bool isInWater;
        private float _verticalVelocity;
        private const float Gravity = -22f;
        private float _waterCheckTimer;   // for periodic topography-based swim detection

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
            if (input.sqrMagnitude > 0.01f)
            {
                Debug.Log($"[Controller] Movement Input: {input}");
            }
            Vector3 movement = CalculateCameraRelativeMovement(input);
if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            MoveWithWorldBounds(movement);
            ApplyGravity();
            CheckWaterStateFromTopography();
            RotateTowardLookPosition(inputReader.LookScreenPosition);
        }

        /// <summary>
        /// Periodically queries IslandTopographyRuntime to sync swim state.
        /// Acts as a reliable fallback if trigger-based detection misses transitions.
        /// </summary>
        private void CheckWaterStateFromTopography()
        {
            _waterCheckTimer -= Time.deltaTime;
            if (_waterCheckTimer > 0f) return;
            _waterCheckTimer = 0.4f;

            IslandTopographyRuntime topo = IslandTopographyRuntime.Active;
            if (topo == null || !topo.IsBuilt) return;

            bool onWater = topo.IsWaterAt(transform.position.x, transform.position.z);
            if (onWater && !isInWater)
            {
                EnterWater();
                GetComponent<PlayerAnimationDriver>()?.SetSwimming(true);
            }
            else if (!onWater && isInWater)
            {
                ExitWater();
                GetComponent<PlayerAnimationDriver>()?.SetSwimming(false);
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

            // While swimming, keep player floating at the water surface
            if (isInWater)
                desiredPosition.y = Mathf.Lerp(currentPosition.y, waterSurfaceY, 8f * Time.deltaTime);

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
            clamped.y = currentPosition.y;
            ApplyMovement(clamped);
        }

        private void RotateTowardLookPosition(Vector2 screenPosition)
        {
            CameraComponent mainCamera = CameraComponent.main;
            if (mainCamera == null)
            {
                if (Time.frameCount % 120 == 0) Debug.LogWarning("[Controller] Main Camera not found for rotation!");
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

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[Controller] Rotating toward direction: {direction.normalized}");
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
            isInWater = true;
        }

        /// <summary>Called by PlayerWaterDetector when the player leaves all water trigger volumes.</summary>
        public void ExitWater()
        {
            isInWater = false;
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
