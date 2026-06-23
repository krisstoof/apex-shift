using ApexShift.Runtime.PlayerInput;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public sealed class PlayerMotionVisualFeedback : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private Transform visualRoot;

        [SerializeField]
        private float bobHeight = 0.05f;

        [SerializeField]
        private float walkBobSpeed = 8f;

        [SerializeField]
        private float sprintBobSpeed = 13f;

        [SerializeField]
        private float returnSpeed = 12f;

        [SerializeField]
        private bool enableBobbing = true;

        private Vector3 defaultLocalPosition;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (visualRoot == null)
            {
                visualRoot = ResolveVisualRoot();
            }

            defaultLocalPosition = visualRoot.localPosition;
        }

        private void Update()
        {
            if (inputReader == null || visualRoot == null)
            {
                return;
            }

            if (!enableBobbing)
            {
                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, defaultLocalPosition, returnSpeed * Time.deltaTime);
                return;
            }

            bool moving = inputReader.Move.sqrMagnitude > 0.01f;
            if (!moving)
            {
                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, defaultLocalPosition, returnSpeed * Time.deltaTime);
                return;
            }

            float bobSpeed = inputReader.SprintHeld ? sprintBobSpeed : walkBobSpeed;
            float offset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            visualRoot.localPosition = defaultLocalPosition + Vector3.up * Mathf.Abs(offset);
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            inputReader = reader;
        }

        public void SetVisualRoot(Transform root)
        {
            visualRoot = root;
            if (visualRoot != null)
            {
                defaultLocalPosition = visualRoot.localPosition;
            }
        }

        public void SetBobbingEnabled(bool enabled)
        {
            enableBobbing = enabled;
        }

        private Transform ResolveVisualRoot()
        {
            SkinnedMeshRenderer skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skinnedMeshRenderer != null)
            {
                return skinnedMeshRenderer.transform;
            }

            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator != null && animator.transform != transform)
            {
                return animator.transform;
            }

            if (transform.childCount > 0)
            {
                return transform.GetChild(0);
            }

            return transform;
        }
    }
}
