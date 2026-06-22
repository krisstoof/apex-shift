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
        private Vector3 offset = new Vector3(0f, 18f, -18f);

        [SerializeField]
        private float smoothing = 10f;

        [SerializeField]
        private float orthographicSize = 14f;

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

            Vector3 desiredPosition = target.position + offset;
            if (smoothing <= 0f)
            {
                transform.position = desiredPosition;
                return;
            }

            float t = Mathf.Clamp01(smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desiredPosition, t);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
