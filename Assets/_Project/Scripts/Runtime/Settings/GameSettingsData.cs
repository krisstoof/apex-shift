using System;
using UnityEngine;

namespace ApexShift.Runtime.Settings
{
    [Serializable]
    public sealed class GameSettingsData
    {
        public float masterVolume = 1f;
        public float musicVolume = 1f;
        public float ambientVolume = 1f;
        public float sfxVolume = 1f;
        public float uiVolume = 1f;
        public bool muteAudio;

        public bool fullscreen = true;
        public int resolutionWidth;
        public int resolutionHeight;
        public int qualityIndex = -1;
        public bool vSync = true;
        public int targetFps = 60;
        public float renderScale = 1f;
        public int shadowQuality = 2;

        public static GameSettingsData CreateDefaults()
        {
            Resolution current = Screen.currentResolution;
            return new GameSettingsData
            {
                masterVolume = 1f,
                musicVolume = 1f,
                ambientVolume = 1f,
                sfxVolume = 1f,
                uiVolume = 1f,
                muteAudio = false,
                fullscreen = Screen.fullScreen,
                resolutionWidth = Mathf.Max(640, current.width),
                resolutionHeight = Mathf.Max(360, current.height),
                qualityIndex = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, Mathf.Max(0, QualitySettings.names.Length - 1)),
                vSync = QualitySettings.vSyncCount > 0,
                targetFps = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60,
                renderScale = 1f,
                shadowQuality = 2
            };
        }

        public GameSettingsData Clone()
        {
            return new GameSettingsData
            {
                masterVolume = masterVolume,
                musicVolume = musicVolume,
                ambientVolume = ambientVolume,
                sfxVolume = sfxVolume,
                uiVolume = uiVolume,
                muteAudio = muteAudio,
                fullscreen = fullscreen,
                resolutionWidth = resolutionWidth,
                resolutionHeight = resolutionHeight,
                qualityIndex = qualityIndex,
                vSync = vSync,
                targetFps = targetFps,
                renderScale = renderScale,
                shadowQuality = shadowQuality
            };
        }

        public void Sanitize()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            ambientVolume = Mathf.Clamp01(ambientVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            uiVolume = Mathf.Clamp01(uiVolume);
            resolutionWidth = Mathf.Max(640, resolutionWidth);
            resolutionHeight = Mathf.Max(360, resolutionHeight);
            qualityIndex = Mathf.Clamp(qualityIndex < 0 ? QualitySettings.GetQualityLevel() : qualityIndex, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            targetFps = Mathf.Clamp(targetFps, 30, 240);
            renderScale = Mathf.Clamp(renderScale, 0.5f, 1.5f);
            shadowQuality = Mathf.Clamp(shadowQuality, 0, 2);
        }
    }
}
