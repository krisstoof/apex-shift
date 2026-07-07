using System;
using UnityEngine;
#if UNITY_RENDER_PIPELINE_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

namespace ApexShift.Runtime.Settings
{
    public sealed class GameSettingsService : MonoBehaviour
    {
        private const string PlayerPrefsKey = "apex_shift.game_settings.v1";
        private static GameSettingsService instance;

        [SerializeField]
        private bool applyOnAwake = true;

        [SerializeField]
        private GameSettingsData current;

        public static GameSettingsService Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindAnyObjectByType<GameSettingsService>();
                if (instance != null)
                {
                    return instance;
                }

                GameObject go = new GameObject("GameSettingsService");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<GameSettingsService>();
                instance.Load();
                instance.Apply();
                return instance;
            }
        }

        public event Action<GameSettingsData> SettingsApplied;

        public GameSettingsData Current
        {
            get
            {
                EnsureLoaded();
                return current;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
            if (applyOnAwake)
            {
                Apply();
            }
        }

        public GameSettingsData GetEditableCopy()
        {
            EnsureLoaded();
            return current.Clone();
        }

        public void ApplyAndSave(GameSettingsData settings)
        {
            current = settings?.Clone() ?? GameSettingsData.CreateDefaults();
            current.Sanitize();
            Apply();
            Save();
        }

        public void ResetDefaults()
        {
            current = GameSettingsData.CreateDefaults();
            current.Sanitize();
            Apply();
            Save();
        }

        public void Load()
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                current = GameSettingsData.CreateDefaults();
            }
            else
            {
                try
                {
                    current = JsonUtility.FromJson<GameSettingsData>(json) ?? GameSettingsData.CreateDefaults();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Settings] Could not load settings JSON, using defaults. {ex.Message}", this);
                    current = GameSettingsData.CreateDefaults();
                }
            }

            current.Sanitize();
        }

        public void Save()
        {
            EnsureLoaded();
            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(current));
            PlayerPrefs.Save();
        }

        public void Apply()
        {
            EnsureLoaded();
            ApplyAudio(current);
            ApplyGraphics(current);
            SettingsApplied?.Invoke(current.Clone());
        }

        private void EnsureLoaded()
        {
            if (current == null)
            {
                Load();
            }
        }

        private static void ApplyAudio(GameSettingsData settings)
        {
            float master = settings.muteAudio ? 0f : Mathf.Clamp01(settings.masterVolume);
            AudioListener.volume = master;
            AudioSettingsRuntime.ApplyGlobalSettings(settings);
        }

        private static void ApplyGraphics(GameSettingsData settings)
        {
            int qualityCount = QualitySettings.names != null ? QualitySettings.names.Length : 0;
            if (qualityCount > 0)
            {
                int quality = Mathf.Clamp(settings.qualityIndex, 0, qualityCount - 1);
                if (QualitySettings.GetQualityLevel() != quality)
                {
                    QualitySettings.SetQualityLevel(quality, true);
                }
            }

            QualitySettings.vSyncCount = settings.vSync ? 1 : 0;
            Application.targetFrameRate = settings.vSync ? -1 : Mathf.Clamp(settings.targetFps, 30, 240);
            ShadowQuality shadowQuality = settings.shadowQuality <= 0
                ? ShadowQuality.Disable
                : settings.shadowQuality == 1
                    ? ShadowQuality.HardOnly
                    : ShadowQuality.All;
            QualitySettings.shadows = shadowQuality;

            if (settings.resolutionWidth > 0 && settings.resolutionHeight > 0)
            {
                Screen.SetResolution(settings.resolutionWidth, settings.resolutionHeight, settings.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            }
            else
            {
                Screen.fullScreen = settings.fullscreen;
            }

            ApplyRenderScale(settings.renderScale);
        }

        private static void ApplyRenderScale(float renderScale)
        {
#if UNITY_RENDER_PIPELINE_UNIVERSAL
            UniversalRenderPipelineAsset urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.renderScale = Mathf.Clamp(renderScale, 0.5f, 1.5f);
            }
#endif
        }
    }
}
