using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ApexShift.Runtime.Player
{
    public sealed class PlayerDeathRuntime : MonoBehaviour
    {
        [SerializeField]
        private PlayerSurvivalRuntime survivalRuntime;

        [SerializeField]
        private bool showRuntimeOverlay = true;

        [SerializeField]
        private bool freezeTimeOnDeath;

        private readonly List<Behaviour> disabledBehaviours = new List<Behaviour>();
        private Canvas overlayCanvas;
        private Text messageText;
        private bool handledDeath;
        private float previousTimeScale = 1f;

        private void Awake()
        {
            if (survivalRuntime == null)
            {
                survivalRuntime = GetComponent<PlayerSurvivalRuntime>();
            }
        }

        private void OnEnable()
        {
            if (survivalRuntime != null)
            {
                survivalRuntime.PlayerDied += OnPlayerDied;
            }
        }

        private void Start()
        {
            if (survivalRuntime != null && survivalRuntime.IsDead)
            {
                HandleDeath(survivalRuntime.DeathReason);
            }
        }

        private void OnDisable()
        {
            if (survivalRuntime != null)
            {
                survivalRuntime.PlayerDied -= OnPlayerDied;
            }
        }

        private void OnPlayerDied(PlayerSurvivalRuntime source, string reason)
        {
            HandleDeath(reason);
        }

        private void HandleDeath(string reason)
        {
            if (handledDeath)
            {
                return;
            }

            handledDeath = true;
            DisableGameplayBehaviours();
            if (freezeTimeOnDeath)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            if (showRuntimeOverlay)
            {
                BuildOverlayIfNeeded();
                if (overlayCanvas != null)
                {
                    overlayCanvas.enabled = true;
                }

                if (messageText != null)
                {
                    messageText.text = string.IsNullOrWhiteSpace(reason)
                        ? "You died"
                        : "You died\n" + reason;
                }
            }
        }

        private void DisableGameplayBehaviours()
        {
            disabledBehaviours.Clear();
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null || behaviour == this || behaviour == survivalRuntime || !behaviour.enabled)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (!ShouldDisableOnDeath(typeName))
                {
                    continue;
                }

                behaviour.enabled = false;
                disabledBehaviours.Add(behaviour);
            }
        }

        private static bool ShouldDisableOnDeath(string typeName)
        {
            return typeName == "PlayerInputReader"
                   || typeName == "IsometricPlayerController"
                   || typeName == "PlayerCombatRuntime"
                   || typeName == "PlayerCraftingRuntime"
                   || typeName == "PlayerHeldItemRuntime"
                   || typeName == "BuildingPlacementRuntime"
                   || typeName == "ActionBarRuntime";
        }

        private void BuildOverlayIfNeeded()
        {
            if (overlayCanvas != null)
            {
                return;
            }

            GameObject overlay = new GameObject("GameOverOverlay", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            overlay.transform.SetParent(transform, false);
            overlayCanvas = overlay.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 9000;

            CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(overlay.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            messageText = CreateText(panel.transform, "DeathMessage", "You died", 42, TextAnchor.MiddleCenter, new Vector2(0f, 120f), new Vector2(900f, 160f), true);
            CreateButton(panel.transform, "RestartRunButton", "Restart Run", new Vector2(260f, 56f), new Vector2(0f, 10f), RestartRun);
            CreateButton(panel.transform, "LoadSaveButton", "Load Last Save", new Vector2(260f, 56f), new Vector2(0f, -64f), LoadLastSavePlaceholder);
            CreateButton(panel.transform, "MainMenuButton", "Back To Main Menu", new Vector2(260f, 56f), new Vector2(0f, -138f), BackToMainMenu);
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size, bool bold)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = text;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            return label;
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 size, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.24f, 0.32f, 0.24f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            CreateText(go.transform, "Label", text, 18, TextAnchor.MiddleCenter, Vector2.zero, size, false);
            return button;
        }

        private void RestartRun()
        {
            RestoreTimeScale();
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }
        }

        private void LoadLastSavePlaceholder()
        {
            Debug.Log("[PlayerDeath] Load Last Save requested. Hook this button to GameSaveService load flow when save selection is available.", this);
        }

        private void BackToMainMenu()
        {
            RestoreTimeScale();
            SceneManager.LoadScene(0);
        }

        private void RestoreTimeScale()
        {
            if (freezeTimeOnDeath)
            {
                Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
            }
        }
    }
}
