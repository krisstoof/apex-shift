using ApexShift.Runtime.World;
using UnityEngine;
using CameraComponent = UnityEngine.Camera;

namespace ApexShift.Runtime.Player
{
    public sealed class IsometricPlayerController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField]
        private float turnSpeed = 18f;

        private void Update()
        {
            Vector2 input = ReadMoveInput();
            Vector3 movement = CalculateCameraRelativeMovement(input);
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            MoveWithWorldBounds(movement);
            RotateTowardMouse();
        }

        private Vector2 ReadMoveInput()
        {
            float horizontal;
            float vertical;

#if ENABLE_INPUT_SYSTEM
            Vector2 moveInput = Vector2.zero;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    moveInput.x -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    moveInput.x += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    moveInput.y -= 1f;
                }

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    moveInput.y += 1f;
                }
            }

            horizontal = moveInput.x;
            vertical = moveInput.y;
#else
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
#endif

            return new Vector2(horizontal, vertical);
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
            Vector3 desiredPosition = currentPosition + movement * (moveSpeed * Time.deltaTime);

            WorldBounds worldBounds = WorldBounds.Active;
            if (worldBounds == null)
            {
                transform.position = desiredPosition;
                return;
            }

            if (worldBounds.Contains(desiredPosition))
            {
                transform.position = desiredPosition;
                return;
            }

            Vector3 xOnly = new Vector3(desiredPosition.x, currentPosition.y, currentPosition.z);
            if (worldBounds.Contains(xOnly))
            {
                transform.position = xOnly;
                return;
            }

            Vector3 zOnly = new Vector3(currentPosition.x, currentPosition.y, desiredPosition.z);
            if (worldBounds.Contains(zOnly))
            {
                transform.position = zOnly;
                return;
            }

            Vector3 clamped = worldBounds.ClampToNearestAllowed(desiredPosition);
            clamped.y = currentPosition.y;
            transform.position = clamped;
        }

        private void RotateTowardMouse()
        {
            CameraComponent mainCamera = CameraComponent.main;
            if (mainCamera == null)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current == null)
            {
                return;
            }

            Vector3 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
            Vector3 mousePosition = Input.mousePosition;
#endif

            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
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
    }
}
