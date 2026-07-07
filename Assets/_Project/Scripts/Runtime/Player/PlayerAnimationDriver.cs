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
        private string swimmingStateName = "Swimming";

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
        private string swimmingParameter = "IsSwimming";

        [SerializeField]
        private string gatherTrigger = "Gather";

        [SerializeField]
        private string spearAttackTrigger = "SpearAttack";

        [SerializeField]
        private string bowAttackTrigger = "BowAttack";

        [SerializeField]
        private string axeUseTrigger = "AxeUse";

        [SerializeField]
        private string pickaxeUseTrigger = "PickaxeUse";

        [SerializeField]
        private string torchUseTrigger = "TorchUse";

        [SerializeField]
        private string chopTrigger = "Chop";

        [SerializeField]
        private string mineTrigger = "Mine";

        [SerializeField]
        private string hurtTrigger = "Hurt";

        [SerializeField]
        private string deathTrigger = "Death";

        [SerializeField]
        private float crossFadeDuration = 0.12f;

        [SerializeField]
        private bool logAnimationSetup = false;

        private bool hasSpeed;
        private bool hasMoving;
        private bool hasSprinting;
        private bool hasAttack;
        private bool hasInteract;
        private bool hasSwimming;
        private bool hasGather;
        private bool hasSpearAttack;
        private bool hasBowAttack;
        private bool hasAxeUse;
        private bool hasPickaxeUse;
        private bool hasTorchUse;
        private bool hasChop;
        private bool hasMine;
        private bool hasHurt;
        private bool hasDeath;
        private bool hasStateFallback;
        private bool hasSwimmingStateFallback;
        private bool loggedMissingStateFallback;
        private string currentState;
        private bool isSwimming;

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

            if (logAnimationSetup || animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.Log(
                    $"[PlayerAnimationDriver] Animator={(animator != null ? animator.name : "missing")}, " +
                    $"controller={(animator != null && animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "missing")}, " +
                    $"hasSpeed={hasSpeed}, hasMoving={hasMoving}, hasSprinting={hasSprinting}, " +
                    $"hasAttack={hasAttack}, hasInteract={hasInteract}, hasSwimming={hasSwimming}, " +
                    $"hasGather={hasGather}, hasSpearAttack={hasSpearAttack}, hasBowAttack={hasBowAttack}, " +
                    $"hasAxeUse={hasAxeUse}, hasPickaxeUse={hasPickaxeUse}, hasTorchUse={hasTorchUse}, " +
                    $"hasChop={hasChop}, hasMine={hasMine}, hasHurt={hasHurt}, hasDeath={hasDeath}, " +
                    $"hasStateFallback={hasStateFallback}, hasSwimmingStateFallback={hasSwimmingStateFallback}",
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

            if (hasSwimming)
            {
                animator.SetBool(swimmingParameter, isSwimming);
            }

            if (hasStateFallback)
            {
                UpdateStateFallback(isMoving, isSprinting, isSwimming);
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

        public void TriggerGather()
        {
            if (animator != null && hasGather)
            {
                animator.SetTrigger(gatherTrigger);
            }
            else
            {
                OnInteractPressed();
            }
        }

        public void TriggerItemUse(string itemId)
        {
            string normalized = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "spear":
                    TriggerSpearAttack();
                    break;
                case "bow":
                    TriggerBowAttack();
                    break;
                case "axe":
                    TriggerAxeUse();
                    break;
                case "pickaxe":
                    TriggerPickaxeUse();
                    break;
                case "torch":
                    TriggerTorchUse();
                    break;
                default:
                    TriggerGather();
                    break;
            }
        }

        public void TriggerSpearAttack()
        {
            if (TrySetTrigger(spearAttackTrigger, hasSpearAttack)) return;
            OnAttackPressed();
        }

        public void TriggerBowAttack()
        {
            if (TrySetTrigger(bowAttackTrigger, hasBowAttack)) return;
            OnAttackPressed();
        }

        public void TriggerAxeUse()
        {
            if (TrySetTrigger(axeUseTrigger, hasAxeUse)) return;
            TriggerGather();
        }

        public void TriggerPickaxeUse()
        {
            if (TrySetTrigger(pickaxeUseTrigger, hasPickaxeUse)) return;
            TriggerAxeUse();
        }

        public void TriggerTorchUse()
        {
            if (TrySetTrigger(torchUseTrigger, hasTorchUse)) return;
            TriggerGather();
        }

        public void TriggerChop()
        {
            if (animator != null && hasChop) animator.SetTrigger(chopTrigger);
            else TriggerGather();
        }

        public void TriggerMine()
        {
            if (animator != null && hasMine) animator.SetTrigger(mineTrigger);
            else TriggerGather();
        }

        public void TriggerHurt()
        {
            TrySetTrigger(hurtTrigger, hasHurt);
        }

        public void TriggerDeath()
        {
            TrySetTrigger(deathTrigger, hasDeath);
        }

        /// <summary>
        /// Called by PlayerWaterDetector to enable or disable the swimming animation state.
        /// Has no effect if the Animator controller does not contain an "IsSwimming" bool parameter.
        /// </summary>
        public void SetSwimming(bool swimming)
        {
            isSwimming = swimming;
            if (animator != null && hasSwimming)
            {
                animator.SetBool(swimmingParameter, swimming);
            }
        }

        private void UpdateStateFallback(bool isMoving, bool isSprinting, bool swimming)
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

            if (swimming && ContainsClip(animator.runtimeAnimatorController.animationClips, swimmingStateName))
            {
                if (!string.Equals(currentState, swimmingStateName))
                {
                    currentState = swimmingStateName;
                    animator.CrossFadeInFixedTime(currentState, crossFadeDuration);
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
            hasGather = false;
            hasSpearAttack = false;
            hasBowAttack = false;
            hasAxeUse = false;
            hasPickaxeUse = false;
            hasTorchUse = false;
            hasChop = false;
            hasMine = false;
            hasHurt = false;
            hasDeath = false;
            hasStateFallback = false;
            hasSwimmingStateFallback = false;

            if (animator == null || animator.runtimeAnimatorController == null)
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
                else if (parameter.name == swimmingParameter)
                {
                    hasSwimming = parameter.type == AnimatorControllerParameterType.Bool;
                }
                else if (parameter.name == gatherTrigger)
                {
                    hasGather = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == spearAttackTrigger)
                {
                    hasSpearAttack = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == bowAttackTrigger)
                {
                    hasBowAttack = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == axeUseTrigger)
                {
                    hasAxeUse = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == pickaxeUseTrigger)
                {
                    hasPickaxeUse = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == torchUseTrigger)
                {
                    hasTorchUse = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == chopTrigger)
                {
                    hasChop = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == mineTrigger)
                {
                    hasMine = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == hurtTrigger)
                {
                    hasHurt = parameter.type == AnimatorControllerParameterType.Trigger;
                }
                else if (parameter.name == deathTrigger)
                {
                    hasDeath = parameter.type == AnimatorControllerParameterType.Trigger;
                }
            }

            hasSwimmingStateFallback = ContainsClip(animator.runtimeAnimatorController.animationClips, swimmingStateName);
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

        private bool TrySetTrigger(string triggerName, bool hasTrigger)
        {
            if (animator == null || !hasTrigger)
            {
                return false;
            }

            animator.SetTrigger(triggerName);
            return true;
        }
    }
}
