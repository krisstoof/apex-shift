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

        [SerializeField]
        private bool logAnimationSetup = true;

        private bool hasSpeed;
        private bool hasMoving;
        private bool hasSprinting;
        private bool hasAttack;
        private bool hasInteract;
        private bool hasStateFallback;
        private bool loggedMissingStateFallback;
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

            if (logAnimationSetup)
            {
                Debug.Log(
                    $"[PlayerAnimationDriver] Animator={(animator != null ? animator.name : "missing")}, " +
                    $"hasSpeed={hasSpeed}, hasMoving={hasMoving}, hasSprinting={hasSprinting}, " +
                    $"hasAttack={hasAttack}, hasInteract={hasInteract}, hasStateFallback={hasStateFallback}",
                    this);
            }
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
            if (!CanUseStateFallback())
            {
                if (!loggedMissingStateFallback)
                {
                    loggedMissingStateFallback = true;
                    Debug.LogWarning("[PlayerAnimationDriver] State fallback skipped because Idle/Walking/Running clips were not found.", this);
                }

                return;
            }

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
            hasStateFallback = false;

            if (animator == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
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

            hasStateFallback = !hasSpeed && !hasMoving && CanUseStateFallback();
        }

        private bool CanUseStateFallback()
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            return ContainsClip(clips, idleStateName)
                && ContainsClip(clips, walkingStateName)
                && ContainsClip(clips, runningStateName);
        }

        private static bool ContainsClip(AnimationClip[] clips, string clipName)
        {
            if (clips == null || string.IsNullOrWhiteSpace(clipName))
            {
                return false;
            }

            foreach (AnimationClip clip in clips)
            {
                if (clip != null && clip.name == clipName)
                {
                    return true;
                }
            }

            return false;
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            inputReader = reader;
        }

        public void SetAnimator(Animator targetAnimator)
        {
            animator = targetAnimator;
            currentState = null;
            loggedMissingStateFallback = false;
            CacheParameters();
        }
    }
}
