using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField]
        private string idleStateName = "Idle";

        [SerializeField]
        private string walkingStateName = "Walking";

        [SerializeField]
        private string runningStateName = "Running";

        [SerializeField]
        private float runningThreshold = 0.95f;

        private Animator animator;
        private string currentState;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (animator == null)
            {
                return;
            }

            Vector2 movementInput = ReadMovementInput();
            string targetState = idleStateName;

            if (movementInput.sqrMagnitude > 0.01f)
            {
                targetState = movementInput.sqrMagnitude >= runningThreshold ? runningStateName : walkingStateName;
            }

            if (string.Equals(currentState, targetState))
            {
                return;
            }

            currentState = targetState;
            animator.CrossFadeInFixedTime(currentState, 0.1f);
        }

        private static Vector2 ReadMovementInput()
        {
#if ENABLE_INPUT_SYSTEM
            Vector2 moveInput = Vector2.zero;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard == null)
            {
                return moveInput;
            }

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

            return moveInput;
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }
    }
}

