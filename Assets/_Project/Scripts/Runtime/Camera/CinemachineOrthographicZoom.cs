using Unity.Cinemachine;
using UnityEngine;

namespace ApexShift.Runtime.Camera
{
    [DisallowMultipleComponent]
    public sealed class CinemachineOrthographicZoom : MonoBehaviour
    {
        [SerializeField]
        private float zoomSpeed = 4f;

        [SerializeField]
        private float minOrthographicSize = 10f;

        [SerializeField]
        private float maxOrthographicSize = 22f;

        private CinemachineCamera cinemachineCamera;

        private void Awake()
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }

        private void LateUpdate()
        {
            if (cinemachineCamera == null)
            {
                return;
            }

            if (ApexShift.Runtime.Debugging.DebugUIBounds.IsMouseOverAnyWindow())
            {
                return;
            }

            float scrollDelta = 0f;
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                scrollDelta = UnityEngine.InputSystem.Mouse.current.scroll.ReadValue().y;
            }
#else
            scrollDelta = Input.mouseScrollDelta.y;
#endif

            if (Mathf.Abs(scrollDelta) < 0.001f)
            {
                return;
            }

            LensSettings lens = cinemachineCamera.Lens;
            float nextSize = Mathf.Clamp(
                lens.OrthographicSize - scrollDelta * zoomSpeed,
                minOrthographicSize,
                maxOrthographicSize);

            if (Mathf.Approximately(nextSize, lens.OrthographicSize))
            {
                return;
            }

            lens.OrthographicSize = nextSize;
            cinemachineCamera.Lens = lens;
        }
    }
}
