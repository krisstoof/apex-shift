using System.Collections.Generic;
using ApexShift.Runtime.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class OptionsMenuController : MonoBehaviour
    {
        [SerializeField] private Font font;
        [SerializeField] private Text statusText;

        private GameSettingsData draft;
        private bool built;
        private readonly List<Selectable> controls = new List<Selectable>();

        private Slider masterSlider;
        private Slider musicSlider;
        private Slider ambientSlider;
        private Slider sfxSlider;
        private Slider uiSlider;
        private Toggle muteToggle;
        private Toggle fullscreenToggle;
        private Dropdown resolutionDropdown;
        private Dropdown qualityDropdown;
        private Toggle vSyncToggle;
        private Dropdown targetFpsDropdown;
        private Dropdown shadowDropdown;
        private Slider renderScaleSlider;
        private Text renderScaleValueText;
        private readonly List<Resolution> availableResolutions = new List<Resolution>();

        public void BuildIfNeeded(Font uiFont)
        {
            if (built)
            {
                return;
            }

            font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            draft = GameSettingsService.Instance.GetEditableCopy();
            BuildLayout();
            ReadDraftIntoControls();
            built = true;
        }

        private void OnEnable()
        {
            if (!built)
            {
                BuildIfNeeded(font);
            }
            else
            {
                draft = GameSettingsService.Instance.GetEditableCopy();
                ReadDraftIntoControls();
            }
        }

        private void BuildLayout()
        {
            Transform existing = transform.Find("SettingsControls");
            if (existing != null)
            {
                Destroy(existing.gameObject);
            }

            GameObject root = new GameObject("SettingsControls", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            RectTransform rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.offsetMin = new Vector2(44f, 72f);
            rootRt.offsetMax = new Vector2(-44f, -200f);

            CreateSectionTitle(root.transform, "Audio", new Vector2(0f, -6f));
            masterSlider = CreateSliderRow(root.transform, "Master", new Vector2(0f, -38f), value => draft.masterVolume = value);
            musicSlider = CreateSliderRow(root.transform, "Music", new Vector2(0f, -72f), value => draft.musicVolume = value);
            ambientSlider = CreateSliderRow(root.transform, "Ambient", new Vector2(0f, -106f), value => draft.ambientVolume = value);
            sfxSlider = CreateSliderRow(root.transform, "SFX", new Vector2(0f, -140f), value => draft.sfxVolume = value);
            uiSlider = CreateSliderRow(root.transform, "UI", new Vector2(0f, -174f), value => draft.uiVolume = value);
            muteToggle = CreateToggleRow(root.transform, "Mute", new Vector2(0f, -208f), value => draft.muteAudio = value);

            CreateSectionTitle(root.transform, "Graphics", new Vector2(390f, -6f));
            fullscreenToggle = CreateToggleRow(root.transform, "Fullscreen", new Vector2(390f, -38f), value => draft.fullscreen = value);
            resolutionDropdown = CreateDropdownRow(root.transform, "Resolution", new Vector2(390f, -72f), BuildResolutionOptions(), index => SetResolutionFromDropdown(index));
            qualityDropdown = CreateDropdownRow(root.transform, "Quality", new Vector2(390f, -116f), BuildQualityOptions(), index => draft.qualityIndex = index);
            vSyncToggle = CreateToggleRow(root.transform, "VSync", new Vector2(390f, -160f), value => draft.vSync = value);
            targetFpsDropdown = CreateDropdownRow(root.transform, "Target FPS", new Vector2(390f, -194f), new List<string> { "30", "60", "90", "120", "144", "240" }, index => draft.targetFps = ResolveFps(index));
            shadowDropdown = CreateDropdownRow(root.transform, "Shadows", new Vector2(390f, -238f), new List<string> { "Off", "Low", "High" }, index => draft.shadowQuality = index);
            renderScaleSlider = CreateSliderRow(root.transform, "Render Scale", new Vector2(390f, -282f), value =>
            {
                draft.renderScale = Mathf.Lerp(0.5f, 1.5f, value);
                if (renderScaleValueText != null) renderScaleValueText.text = draft.renderScale.ToString("0.00x");
            });
            renderScaleValueText = CreateText(root.transform, "RenderScaleValue", "1.00x", 13, TextAnchor.MiddleLeft, new Vector2(735f, -282f), new Vector2(70f, 24f));

            statusText = CreateText(transform, "SettingsStatus", "Settings are saved with PlayerPrefs.", 14, TextAnchor.MiddleLeft, new Vector2(46f, -382f), new Vector2(560f, 28f));
            statusText.color = new Color(0.85f, 0.95f, 0.75f, 1f);

            Button apply = CreateButton(transform, "ApplyButton", "Apply", new Vector2(150f, 42f), new Vector2(46f, -438f), Apply);
            Button reset = CreateButton(transform, "ResetDefaultsButton", "Reset Defaults", new Vector2(190f, 42f), new Vector2(210f, -438f), ResetDefaults);
            controls.Add(apply);
            controls.Add(reset);
        }

        private void ReadDraftIntoControls()
        {
            if (draft == null)
            {
                draft = GameSettingsService.Instance.GetEditableCopy();
            }

            SetSlider(masterSlider, draft.masterVolume);
            SetSlider(musicSlider, draft.musicVolume);
            SetSlider(ambientSlider, draft.ambientVolume);
            SetSlider(sfxSlider, draft.sfxVolume);
            SetSlider(uiSlider, draft.uiVolume);
            SetToggle(muteToggle, draft.muteAudio);
            SetToggle(fullscreenToggle, draft.fullscreen);
            SetDropdown(resolutionDropdown, FindResolutionIndex(draft.resolutionWidth, draft.resolutionHeight));
            SetDropdown(qualityDropdown, Mathf.Clamp(draft.qualityIndex, 0, Mathf.Max(0, qualityDropdown.options.Count - 1)));
            SetToggle(vSyncToggle, draft.vSync);
            SetDropdown(targetFpsDropdown, FindFpsIndex(draft.targetFps));
            SetDropdown(shadowDropdown, Mathf.Clamp(draft.shadowQuality, 0, 2));
            SetSlider(renderScaleSlider, Mathf.InverseLerp(0.5f, 1.5f, draft.renderScale));
            if (renderScaleValueText != null) renderScaleValueText.text = draft.renderScale.ToString("0.00x");
        }

        private void Apply()
        {
            draft.Sanitize();
            GameSettingsService.Instance.ApplyAndSave(draft);
            draft = GameSettingsService.Instance.GetEditableCopy();
            ReadDraftIntoControls();
            SetStatus("Settings applied and saved.", true);
        }

        private void ResetDefaults()
        {
            GameSettingsService.Instance.ResetDefaults();
            draft = GameSettingsService.Instance.GetEditableCopy();
            ReadDraftIntoControls();
            SetStatus("Default settings restored.", true);
        }

        private void SetStatus(string message, bool success)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusText.color = success ? new Color(0.85f, 0.95f, 0.75f, 1f) : new Color(1f, 0.62f, 0.50f, 1f);
        }

        private Slider CreateSliderRow(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction<float> onChanged)
        {
            CreateText(parent, label + "Label", label, 14, TextAnchor.MiddleLeft, position, new Vector2(110f, 24f));
            GameObject go = new GameObject(label + "Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position + new Vector2(120f, -3f);
            rt.sizeDelta = new Vector2(210f, 20f);
            Slider slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.onValueChanged.AddListener(onChanged);
            BuildSliderVisuals(go.transform, slider);
            controls.Add(slider);
            return slider;
        }

        private Toggle CreateToggleRow(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction<bool> onChanged)
        {
            CreateText(parent, label + "Label", label, 14, TextAnchor.MiddleLeft, position, new Vector2(150f, 24f));
            GameObject go = new GameObject(label + "Toggle", typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position + new Vector2(170f, -1f);
            rt.sizeDelta = new Vector2(24f, 24f);
            Toggle toggle = go.GetComponent<Toggle>();
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.17f, 0.12f, 1f);
            GameObject check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(go.transform, false);
            RectTransform checkRt = check.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.2f, 0.2f);
            checkRt.anchorMax = new Vector2(0.8f, 0.8f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;
            check.GetComponent<Image>().color = new Color(0.74f, 0.9f, 0.74f, 1f);
            toggle.targetGraphic = bg;
            toggle.graphic = check.GetComponent<Image>();
            toggle.onValueChanged.AddListener(onChanged);
            controls.Add(toggle);
            return toggle;
        }

        private Dropdown CreateDropdownRow(Transform parent, string label, Vector2 position, List<string> options, UnityEngine.Events.UnityAction<int> onChanged)
        {
            CreateText(parent, label + "Label", label, 14, TextAnchor.MiddleLeft, position, new Vector2(120f, 24f));
            GameObject go = new GameObject(label + "Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = position + new Vector2(140f, -2f);
            rt.sizeDelta = new Vector2(190f, 28f);
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.12f, 0.17f, 0.12f, 1f);
            Dropdown dropdown = go.GetComponent<Dropdown>();
            dropdown.options.Clear();
            foreach (string option in options)
            {
                dropdown.options.Add(new Dropdown.OptionData(option));
            }

            Text caption = CreateText(go.transform, "Label", string.Empty, 13, TextAnchor.MiddleLeft, new Vector2(8f, -2f), new Vector2(150f, 24f));
            caption.raycastTarget = false;
            dropdown.captionText = caption;
            dropdown.targetGraphic = image;
            dropdown.onValueChanged.AddListener(onChanged);
            controls.Add(dropdown);
            return dropdown;
        }

        private void BuildSliderVisuals(Transform root, Slider slider)
        {
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root, false);
            RectTransform bgRt = background.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(0f, 6f);
            bgRt.offsetMax = new Vector2(0f, -6f);
            background.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.10f, 1f);

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root, false);
            RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(4f, 6f);
            fillAreaRt.offsetMax = new Vector2(-4f, -6f);

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = new Color(0.62f, 0.82f, 0.48f, 1f);
            slider.fillRect = fill.GetComponent<RectTransform>();

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(root, false);
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(18f, 24f);
            handle.GetComponent<Image>().color = new Color(0.94f, 0.88f, 0.54f, 1f);
            slider.handleRect = handleRt;
            slider.targetGraphic = handle.GetComponent<Image>();
        }

        private Button CreateButton(Transform parent, string name, string text, Vector2 size, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.24f, 0.32f, 0.24f, 1f);
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            CreateText(go.transform, "Label", text, 14, TextAnchor.MiddleCenter, Vector2.zero, size, false);
            return button;
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size, bool bold = false)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text label = go.GetComponent<Text>();
            label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.alignment = alignment;
            label.color = Color.white;
            label.text = text;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            return label;
        }

        private void CreateSectionTitle(Transform parent, string text, Vector2 position)
        {
            Text title = CreateText(parent, text + "Title", text, 18, TextAnchor.MiddleLeft, position, new Vector2(250f, 28f), true);
            title.color = new Color(0.98f, 0.88f, 0.54f, 1f);
        }

        private List<string> BuildResolutionOptions()
        {
            availableResolutions.Clear();
            HashSet<string> seen = new HashSet<string>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                string key = resolution.width + "x" + resolution.height;
                if (!seen.Add(key))
                {
                    continue;
                }

                availableResolutions.Add(resolution);
            }

            if (availableResolutions.Count == 0)
            {
                Resolution current = Screen.currentResolution;
                availableResolutions.Add(current);
            }

            List<string> labels = new List<string>();
            foreach (Resolution resolution in availableResolutions)
            {
                labels.Add(resolution.width + " x " + resolution.height);
            }

            return labels;
        }

        private List<string> BuildQualityOptions()
        {
            List<string> labels = new List<string>();
            string[] names = QualitySettings.names;
            if (names == null || names.Length == 0)
            {
                labels.Add("Default");
                return labels;
            }

            labels.AddRange(names);
            return labels;
        }

        private void SetResolutionFromDropdown(int index)
        {
            if (availableResolutions.Count == 0)
            {
                return;
            }

            int safeIndex = Mathf.Clamp(index, 0, availableResolutions.Count - 1);
            Resolution resolution = availableResolutions[safeIndex];
            draft.resolutionWidth = resolution.width;
            draft.resolutionHeight = resolution.height;
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < availableResolutions.Count; i++)
            {
                Resolution resolution = availableResolutions[i];
                if (resolution.width == width && resolution.height == height)
                {
                    return i;
                }
            }

            return Mathf.Clamp(availableResolutions.Count - 1, 0, Mathf.Max(0, availableResolutions.Count - 1));
        }

        private static int ResolveFps(int index)
        {
            int[] values = { 30, 60, 90, 120, 144, 240 };
            return values[Mathf.Clamp(index, 0, values.Length - 1)];
        }

        private static int FindFpsIndex(int fps)
        {
            int[] values = { 30, 60, 90, 120, 144, 240 };
            int closest = 0;
            int bestDelta = int.MaxValue;
            for (int i = 0; i < values.Length; i++)
            {
                int delta = Mathf.Abs(values[i] - fps);
                if (delta < bestDelta)
                {
                    closest = i;
                    bestDelta = delta;
                }
            }

            return closest;
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider != null) slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        }

        private static void SetToggle(Toggle toggle, bool value)
        {
            if (toggle != null) toggle.SetIsOnWithoutNotify(value);
        }

        private static void SetDropdown(Dropdown dropdown, int value)
        {
            if (dropdown == null || dropdown.options.Count == 0) return;
            dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, dropdown.options.Count - 1));
            dropdown.RefreshShownValue();
        }
    }
}
