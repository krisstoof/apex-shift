using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.World;
using UnityEngine;
using CameraComponent = UnityEngine.Camera;

namespace ApexShift.Runtime.Player
{
    public sealed class IsometricPlayerController : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

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

        private CharacterController characterController;
        private Rigidbody rigidbodyComponent;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
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

            MoveWithWorldBounds(movement);
            RotateTowardLookPosition(inputReader.LookScreenPosition);
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
            float speed = inputReader != null && inputReader.SprintHeld ? sprintSpeed : walkSpeed;
            Vector3 desiredPosition = currentPosition + movement * (speed * Time.deltaTime);

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
    }
}
