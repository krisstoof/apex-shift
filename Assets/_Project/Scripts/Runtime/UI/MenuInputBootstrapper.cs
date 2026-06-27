using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ApexShift.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class MenuInputBootstrapper : MonoBehaviour
    {
        [SerializeField] private Canvas menuCanvas;
        [SerializeField] private bool forceCanvasOnTop = true;
        [SerializeField] private int sortingOrder = 5000;
        [SerializeField] private bool logDiagnostics = true;

        private void Awake()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsureGraphicRaycaster();
            LogDiagnostics();
        }

        private void OnEnable()
        {
            EnsureEventSystem();
            EnsureCanvas();
            EnsureGraphicRaycaster();
            LogDiagnostics();
        }

        private void EnsureEventSystem()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                GameObject go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputSystemModule == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
            }
#else
            StandaloneInputModule legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyModule == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
#endif
        }

        private void EnsureCanvas()
        {
            if (menuCanvas == null)
            {
                menuCanvas = GetComponentInParent<Canvas>();
            }

            if (menuCanvas == null)
            {
                menuCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (menuCanvas == null)
            {
                return;
            }

            if (forceCanvasOnTop)
            {
                menuCanvas.overrideSorting = true;
                menuCanvas.sortingOrder = sortingOrder;
            }
        }

        private void EnsureGraphicRaycaster()
        {
            if (menuCanvas == null)
            {
                return;
            }

            GraphicRaycaster raycaster = menuCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                menuCanvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void LogDiagnostics()
        {
            if (!logDiagnostics)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current;
            string eventSystemInfo = eventSystem != null
                ? $"{eventSystem.name}, module={eventSystem.currentInputModule?.GetType().Name ?? "none"}"
                : "missing";

            string canvasInfo = menuCanvas != null
                ? $"{menuCanvas.name}, active={menuCanvas.gameObject.activeInHierarchy}, enabled={menuCanvas.enabled}, renderMode={menuCanvas.renderMode}, sorting={menuCanvas.sortingOrder}, raycaster={menuCanvas.GetComponent<GraphicRaycaster>() != null}"
                : "missing";

            Debug.Log($"[MenuInputBootstrapper] EventSystem: {eventSystemInfo}");
            Debug.Log($"[MenuInputBootstrapper] Canvas: {canvasInfo}");
        }
    }
}
