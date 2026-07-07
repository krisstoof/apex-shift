using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ApexShift.Runtime.Settings
{
    public enum AudioChannel
    {
        Master,
        Music,
        Ambient,
        Sfx,
        UI
    }

    [DisallowMultipleComponent]
    public sealed class AudioSettingsRuntime : MonoBehaviour
    {
        private static readonly List<AudioSettingsRuntime> Instances = new List<AudioSettingsRuntime>();
        private static GameSettingsData lastSettings;

        [SerializeField]
        private AudioChannel channel = AudioChannel.Sfx;

        [SerializeField]
        private AudioSource audioSource;

        [SerializeField]
        private float baseVolume = 1f;

        [Header("Optional Mixer")]
        [SerializeField]
        private AudioMixer mixer;

        [SerializeField]
        private string masterVolumeParameter = "MasterVolume";

        [SerializeField]
        private string musicVolumeParameter = "MusicVolume";

        [SerializeField]
        private string ambientVolumeParameter = "AmbientVolume";

        [SerializeField]
        private string sfxVolumeParameter = "SfxVolume";

        [SerializeField]
        private string uiVolumeParameter = "UiVolume";

        public AudioChannel Channel
        {
            get => channel;
            set
            {
                channel = value;
                ApplySettings(lastSettings);
            }
        }

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource != null)
            {
                baseVolume = Mathf.Clamp01(audioSource.volume);
            }
        }

        private void OnEnable()
        {
            if (!Instances.Contains(this))
            {
                Instances.Add(this);
            }

            ApplySettings(lastSettings ?? GameSettingsService.Instance.Current);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        public void SetChannel(AudioChannel nextChannel)
        {
            Channel = nextChannel;
        }

        public void ApplySettings(GameSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            if (audioSource != null)
            {
                audioSource.volume = baseVolume * ResolveChannelVolume(settings, channel);
            }

            if (mixer != null)
            {
                TrySetMixerVolume(mixer, masterVolumeParameter, settings.muteAudio ? 0f : settings.masterVolume);
                TrySetMixerVolume(mixer, musicVolumeParameter, settings.musicVolume);
                TrySetMixerVolume(mixer, ambientVolumeParameter, settings.ambientVolume);
                TrySetMixerVolume(mixer, sfxVolumeParameter, settings.sfxVolume);
                TrySetMixerVolume(mixer, uiVolumeParameter, settings.uiVolume);
            }
        }

        public static void ApplyGlobalSettings(GameSettingsData settings)
        {
            if (settings == null)
            {
                return;
            }

            lastSettings = settings.Clone();
            for (int i = Instances.Count - 1; i >= 0; i--)
            {
                AudioSettingsRuntime runtime = Instances[i];
                if (runtime == null)
                {
                    Instances.RemoveAt(i);
                    continue;
                }

                runtime.ApplySettings(settings);
            }
        }

        public static float ResolveChannelVolume(GameSettingsData settings, AudioChannel channel)
        {
            if (settings == null || settings.muteAudio)
            {
                return 0f;
            }

            float master = Mathf.Clamp01(settings.masterVolume);
            float channelVolume = channel switch
            {
                AudioChannel.Music => settings.musicVolume,
                AudioChannel.Ambient => settings.ambientVolume,
                AudioChannel.Sfx => settings.sfxVolume,
                AudioChannel.UI => settings.uiVolume,
                _ => 1f,
            };

            return master * Mathf.Clamp01(channelVolume);
        }

        private static void TrySetMixerVolume(AudioMixer mixer, string parameter, float linearVolume)
        {
            if (mixer == null || string.IsNullOrWhiteSpace(parameter))
            {
                return;
            }

            float clamped = Mathf.Clamp01(linearVolume);
            float decibels = clamped <= 0.0001f ? -80f : Mathf.Log10(clamped) * 20f;
            try
            {
                mixer.SetFloat(parameter, decibels);
            }
            catch (Exception)
            {
                // Mixer parameter is optional. Missing parameters must not break runtime options.
            }
        }
    }
}
