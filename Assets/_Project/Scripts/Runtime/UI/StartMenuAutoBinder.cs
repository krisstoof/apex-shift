using ApexShift.Runtime.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class StartMenuAutoBinder : MonoBehaviour
    {
        [SerializeField] private GameStartupController startupController;
        [SerializeField] private Transform menuRoot;
        [SerializeField] private bool bindOnlyWhenNoPersistentListeners = true;

        private void Awake()
        {
            if (menuRoot == null)
            {
                menuRoot = transform;
            }

            if (startupController == null)
            {
                startupController = Object.FindAnyObjectByType<GameStartupController>();
            }

            BindButtons();
        }

        [ContextMenu("Bind Buttons")]
        public void BindButtons()
        {
            if (startupController == null)
            {
                Debug.LogWarning("[StartMenuAutoBinder] Missing GameStartupController. Buttons cannot be bound.");
                return;
            }

            Button[] buttons = menuRoot.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                string normalized = Normalize(button.name);

                if (bindOnlyWhenNoPersistentListeners && button.onClick.GetPersistentEventCount() > 0)
                {
                    Debug.Log($"[StartMenuAutoBinder] Skip '{button.name}' because it already has persistent listeners.");
                    continue;
                }

                if (normalized.Contains("new") || normalized.Contains("start"))
                {
                    button.onClick.AddListener(startupController.StartNewGame);
                    Debug.Log($"[StartMenuAutoBinder] Bound '{button.name}' -> StartNewGame");
                }
                else if (normalized.Contains("load") || normalized.Contains("continue"))
                {
                    button.onClick.AddListener(startupController.ContinueOrLoadGame);
                    Debug.Log($"[StartMenuAutoBinder] Bound '{button.name}' -> ContinueOrLoadGame");
                }
                else if (normalized.Contains("resume"))
                {
                    button.onClick.AddListener(startupController.ResumeGame);
                    Debug.Log($"[StartMenuAutoBinder] Bound '{button.name}' -> ResumeGame");
                }
                else if (normalized.Contains("quit") || normalized.Contains("exit"))
                {
                    button.onClick.AddListener(startupController.QuitGame);
                    Debug.Log($"[StartMenuAutoBinder] Bound '{button.name}' -> QuitGame");
                }
                else if (normalized.Contains("back") || normalized.Contains("menu"))
                {
                    button.onClick.AddListener(startupController.ShowMainMenu);
                    Debug.Log($"[StartMenuAutoBinder] Bound '{button.name}' -> ShowMainMenu");
                }
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
        }
    }
}
