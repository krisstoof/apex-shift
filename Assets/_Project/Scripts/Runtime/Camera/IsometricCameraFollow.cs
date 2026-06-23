using UnityEngine;
using CameraComponent = UnityEngine.Camera;

namespace ApexShift.Runtime.Camera
{
    [RequireComponent(typeof(CameraComponent))]
    public sealed class IsometricCameraFollow : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private Vector3 focusOffset = new Vector3(0f, 1.25f, 0f);

        [SerializeField]
        private float followDistance = 20f;

        [SerializeField]
        private float smoothing = 12f;

        [SerializeField]
        private float orthographicSize = 14f;

        [SerializeField]
        private float zoomSpeed = 4f;

        [SerializeField]
        private float minOrthographicSize = 10f;

        [SerializeField]
        private float maxOrthographicSize = 22f;

        [SerializeField]
        private bool enableSmoothing = true;

        [SerializeField]
        private bool disableWhenCinemachineBrainExists = true;

        private bool firstFrame = true;
        private Quaternion initialRotation = Quaternion.Euler(35.264f, 45f, 0f);

        private CameraComponent cachedCamera;

        private void Reset()
        {
            cachedCamera = GetComponent<CameraComponent>();
            if (cachedCamera != null)
            {
                cachedCamera.orthographic = true;
                cachedCamera.orthographicSize = orthographicSize;
            }
        }

        private void Awake()
        {
            if (disableWhenCinemachineBrainExists && HasCinemachineBrain())
            {
                enabled = false;
                return;
            }

            cachedCamera = GetComponent<CameraComponent>();
            if (cachedCamera != null)
            {
                cachedCamera.orthographic = true;
                cachedCamera.orthographicSize = orthographicSize;
            }
            transform.rotation = initialRotation;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleZoom();

            Vector3 desiredPosition = CalculateDesiredPosition();
            if (firstFrame)
            {
                transform.position = desiredPosition;
                firstFrame = false;
                return;
            }

            if (!enableSmoothing || smoothing <= 0f)
            {
                transform.position = desiredPosition;
                return;
            }

            float t = Mathf.Clamp01(smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
        }

        public void SnapToTarget()
        {
            if (target == null)
            {
                return;
            }

            transform.position = CalculateDesiredPosition();
            firstFrame = false;
        }

        public void SetInitialRotation(Quaternion rotation)
        {
            initialRotation = rotation;
            transform.rotation = rotation;
        }

        public void SetOrthographicSize(float size)
        {
            orthographicSize = size;
            if (cachedCamera != null)
            {
                cachedCamera.orthographicSize = size;
            }
        }

        public void SetFollowDistance(float distance)
        {
            followDistance = distance;
        }

        private Vector3 CalculateDesiredPosition()
        {
            Vector3 focusPoint = target.position + focusOffset;
            Vector3 backwardOffset = -transform.forward * followDistance;
            return focusPoint + backwardOffset;
        }

        private void HandleZoom()
        {
            float scrollDelta = 0f;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                scrollDelta = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
            }
#else
            scrollDelta = Input.mouseScrollDelta.y;
#endif

            if (Mathf.Abs(scrollDelta) < 0.001f || cachedCamera == null)
            {
                return;
            }

            orthographicSize = Mathf.Clamp(orthographicSize - scrollDelta * zoomSpeed, minOrthographicSize, maxOrthographicSize);
            cachedCamera.orthographicSize = orthographicSize;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetSmoothingEnabled(bool enabled)
        {
            enableSmoothing = enabled;
        }

        private bool HasCinemachineBrain()
        {
            Component[] components = GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component != null && component.GetType().Name.Contains("CinemachineBrain"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
