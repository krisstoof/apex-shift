using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ApexShift.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class StartMenuButtonDiagnostics : MonoBehaviour
    {
        [SerializeField] private Transform menuRoot;
        [SerializeField] private bool autoAttachClickLoggers = true;
        [SerializeField] private bool logOnEnable = true;

        private void Awake()
        {
            if (menuRoot == null)
            {
                menuRoot = transform;
            }
        }

        private void OnEnable()
        {
            if (menuRoot == null)
            {
                menuRoot = transform;
            }

            if (logOnEnable)
            {
                LogMenuState();
            }

            if (autoAttachClickLoggers)
            {
                AttachClickLoggers();
            }
        }

        [ContextMenu("Log Menu State")]
        public void LogMenuState()
        {
            Debug.Log("[StartMenuButtonDiagnostics] ---- MENU INPUT DIAGNOSTICS ----");

            EventSystem eventSystem = EventSystem.current;
            Debug.Log(eventSystem != null
                ? $"[StartMenuButtonDiagnostics] EventSystem: {eventSystem.name}, module={eventSystem.currentInputModule?.GetType().Name ?? "none"}"
                : "[StartMenuButtonDiagnostics] EventSystem: MISSING");

            Canvas[] canvases = menuRoot.GetComponentsInChildren<Canvas>(true);
            Debug.Log($"[StartMenuButtonDiagnostics] Canvases under root: {canvases.Length}");
            foreach (Canvas canvas in canvases)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                Debug.Log($"[StartMenuButtonDiagnostics] Canvas '{GetPath(canvas.transform)}': active={canvas.gameObject.activeInHierarchy}, enabled={canvas.enabled}, renderMode={canvas.renderMode}, raycaster={(raycaster != null)}, sorting={canvas.sortingOrder}");
            }

            CanvasGroup[] canvasGroups = menuRoot.GetComponentsInChildren<CanvasGroup>(true);
            foreach (CanvasGroup group in canvasGroups)
            {
                Debug.Log($"[StartMenuButtonDiagnostics] CanvasGroup '{GetPath(group.transform)}': active={group.gameObject.activeInHierarchy}, alpha={group.alpha}, interactable={group.interactable}, blocksRaycasts={group.blocksRaycasts}");
            }

            Button[] buttons = menuRoot.GetComponentsInChildren<Button>(true);
            Debug.Log($"[StartMenuButtonDiagnostics] Buttons under root: {buttons.Length}");
            foreach (Button button in buttons)
            {
                int persistentListeners = button.onClick.GetPersistentEventCount();
                Graphic graphic = button.targetGraphic;
                Debug.Log($"[StartMenuButtonDiagnostics] Button '{GetPath(button.transform)}': active={button.gameObject.activeInHierarchy}, interactable={button.interactable}, enabled={button.enabled}, targetGraphic={(graphic != null ? graphic.name : "null")}, targetRaycast={(graphic != null && graphic.raycastTarget)}, persistentListeners={persistentListeners}");

                for (int i = 0; i < persistentListeners; i++)
                {
                    Object target = button.onClick.GetPersistentTarget(i);
                    string method = button.onClick.GetPersistentMethodName(i);
                    Debug.Log($"[StartMenuButtonDiagnostics]   listener[{i}]: target={(target != null ? target.name : "MISSING")}, method={method}");
                }
            }

            Debug.Log("[StartMenuButtonDiagnostics] ---- END ----");
        }

        private void AttachClickLoggers()
        {
            Button[] buttons = menuRoot.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                MenuButtonClickProbe probe = button.GetComponent<MenuButtonClickProbe>();
                if (probe == null)
                {
                    probe = button.gameObject.AddComponent<MenuButtonClickProbe>();
                }

                probe.Configure(button);
            }
        }

        private static string GetPath(Transform transform)
        {
            if (transform == null)
            {
                return "null";
            }

            string path = transform.name;
            Transform current = transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }
    }
}
