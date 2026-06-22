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
        private Vector3 offset = new Vector3(0f, 8f, -8f);

        [SerializeField]
        private float smoothing = 18f;

        [SerializeField]
        private float orthographicSize = 6f;

        [SerializeField]
        private float zoomSpeed = 4f;

        [SerializeField]
        private float minOrthographicSize = 8f;

        [SerializeField]
        private float maxOrthographicSize = 18f;

        private bool firstFrame = true;

        private CameraComponent cachedCamera;

        private void Reset()
        {
            cachedCamera = GetComponent<CameraComponent>();
            if (cachedCamera != null)
            {
                cachedCamera.orthographic = true;
            }
        }

        private void Awake()
        {
            cachedCamera = GetComponent<CameraComponent>();
            if (cachedCamera != null)
            {
                cachedCamera.orthographic = true;
                cachedCamera.orthographicSize = orthographicSize;
            }
            transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            HandleZoom();
            Vector3 desiredPosition = target.position + offset;
            if (firstFrame)
            {
                transform.position = desiredPosition;
                firstFrame = false;
                return;
            }

            if (smoothing <= 0f)
            {
                transform.position = desiredPosition;
                return;
            }

            float t = Mathf.Clamp01(smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
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
    }
}
