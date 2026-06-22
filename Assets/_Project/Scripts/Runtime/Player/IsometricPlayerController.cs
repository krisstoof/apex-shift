using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public sealed class IsometricPlayerController : MonoBehaviour
    {
        [SerializeField]
        private float moveSpeed = 5f;

        private void Update()
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

            Vector3 movement = new Vector3(horizontal, 0f, vertical);
            if (movement.sqrMagnitude > 1f)
            {
                movement.Normalize();
            }

            transform.position += movement * (moveSpeed * Time.deltaTime);
        }
    }
}
