using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class GameMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject startMenuRoot;
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private GameObject optionsMenuRoot;
        [SerializeField] private CanvasGroup startMenuGroup;
        [SerializeField] private CanvasGroup pauseMenuGroup;
        [SerializeField] private CanvasGroup optionsMenuGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;

        private float targetStartAlpha;
        private float targetPauseAlpha;
        private float targetOptionsAlpha;

        public void Configure(
            GameObject startMenuRoot,
            GameObject pauseMenuRoot,
            GameObject optionsMenuRoot,
            CanvasGroup startMenuGroup,
            CanvasGroup pauseMenuGroup,
            CanvasGroup optionsMenuGroup,
            Text titleText,
            Text statusText)
        {
            this.startMenuRoot = startMenuRoot;
            this.pauseMenuRoot = pauseMenuRoot;
            this.optionsMenuRoot = optionsMenuRoot;
            this.startMenuGroup = startMenuGroup;
            this.pauseMenuGroup = pauseMenuGroup;
            this.optionsMenuGroup = optionsMenuGroup;
            this.titleText = titleText;
            this.statusText = statusText;
        }

        private void Update()
        {
            UpdateFade(startMenuGroup, ref targetStartAlpha);
            UpdateFade(pauseMenuGroup, ref targetPauseAlpha);
            UpdateFade(optionsMenuGroup, ref targetOptionsAlpha);
        }

        public void SetMainMenuVisible(bool visible)
        {
            targetStartAlpha = visible ? 1f : 0f;
            SetMenuVisible(startMenuRoot, visible);
            SetMenuAlpha(startMenuGroup, targetStartAlpha);
            if (visible)
            {
                SetStatus("Press Start to play");
            }
        }

        public void SetPauseMenuVisible(bool visible)
        {
            targetPauseAlpha = visible ? 1f : 0f;
            SetMenuVisible(pauseMenuRoot, visible);
            SetMenuAlpha(pauseMenuGroup, targetPauseAlpha);
        }

        public void SetOptionsMenuVisible(bool visible)
        {
            targetOptionsAlpha = visible ? 1f : 0f;
            SetMenuVisible(optionsMenuRoot, visible);
            SetMenuAlpha(optionsMenuGroup, targetOptionsAlpha);
        }

        public void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }

            if (titleText != null)
            {
                titleText.text = "Apex Shift";
            }
        }

        private static void SetMenuVisible(GameObject root, bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        private static void SetMenuAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
                group.interactable = alpha > 0.99f;
                group.blocksRaycasts = alpha > 0.99f;
            }
        }

        private static void UpdateFade(CanvasGroup group, ref float targetAlpha)
        {
            if (group == null)
            {
                return;
            }

            float next = Mathf.MoveTowards(group.alpha, targetAlpha, Time.unscaledDeltaTime * 4.5f);
            group.alpha = next;
            bool enabled = next > 0.99f;
            group.interactable = enabled;
            group.blocksRaycasts = enabled;
        }
    }
}
