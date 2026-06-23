using ApexShift.Runtime.PlayerInput;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private string idleStateName = "Idle";

        [SerializeField]
        private string walkingStateName = "Walking";

        [SerializeField]
        private string runningStateName = "Running";

        [SerializeField]
        private string speedParameter = "Speed";

        [SerializeField]
        private string movingParameter = "IsMoving";

        [SerializeField]
        private string sprintingParameter = "IsSprinting";

        [SerializeField]
        private string attackTrigger = "Attack";

        [SerializeField]
        private string interactTrigger = "Interact";

        [SerializeField]
        private float crossFadeDuration = 0.12f;

        private bool hasSpeed;
        private bool hasMoving;
        private bool hasSprinting;
        private bool hasAttack;
        private bool hasInteract;
        private bool hasStateFallback;
        private string currentState;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            CacheParameters();
        }

        private void OnEnable()
        {
            if (inputReader == null)
            {
                return;
            }

            inputReader.AttackPressed += OnAttackPressed;
            inputReader.InteractPressed += OnInteractPressed;
        }

        private void OnDisable()
        {
            if (inputReader == null)
            {
                return;
            }

            inputReader.AttackPressed -= OnAttackPressed;
            inputReader.InteractPressed -= OnInteractPressed;
        }

        private void Update()
        {
            if (inputReader == null || animator == null)
            {
                return;
            }

            float moveAmount = Mathf.Clamp01(inputReader.Move.magnitude);
            bool isMoving = moveAmount > 0.05f;
            bool isSprinting = inputReader.SprintHeld && isMoving;

            if (hasSpeed)
            {
                animator.SetFloat(speedParameter, isSprinting ? moveAmount * 2f : moveAmount);
            }

            if (hasMoving)
            {
                animator.SetBool(movingParameter, isMoving);
            }

            if (hasSprinting)
            {
                animator.SetBool(sprintingParameter, isSprinting);
            }

            if (hasStateFallback)
            {
                UpdateStateFallback(isMoving, isSprinting);
            }
        }

        private void OnAttackPressed()
        {
            if (animator != null && hasAttack)
            {
                animator.SetTrigger(attackTrigger);
            }
        }

        private void OnInteractPressed()
        {
            if (animator != null && hasInteract)
            {
                animator.SetTrigger(interactTrigger);
            }
        }

        private void UpdateStateFallback(bool isMoving, bool isSprinting)
        {
            string targetState = idleStateName;
            if (isMoving)
            {
                targetState = isSprinting ? runningStateName : walkingStateName;
            }

            if (string.Equals(currentState, targetState))
            {
                return;
            }

            currentState = targetState;
            animator.CrossFadeInFixedTime(currentState, crossFadeDuration);
        }

        private void CacheParameters()
        {
            hasSpeed = false;
            hasMoving = false;
            hasSprinting = false;
            hasAttack = false;
            hasInteract = false;
            hasStateFallback = true;

            if (animator == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                hasStateFallback = false;

                if (parameter.name == speedParameter)
                {
                    hasSpeed = parameter.type == AnimatorControllerParameterType.Float;
                }
                else if (parameter.name == movingParameter)
                {
                    hasMoving = parameter.type == AnimatorControllerParameterType.Bool;
                }
                else if (parameter.name == sprintingParameter)
                {
                    hasSprinting = parameter.type == AnimatorControllerParameterType.Bool;
                }
                else if (parameter.name == attackTrigger)
                {
                    hasAttack = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == interactTrigger)
                {
                    hasInteract = parameter.type == AnimatorControllerParameterType.Trigger;
                }
            }
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            inputReader = reader;
        }

        public void SetAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
            CacheParameters();
        }
    }
}
