using UnityEngine;

namespace ApexShift.Runtime.Audio
{
    [CreateAssetMenu(menuName = "ApexShift/Audio/Trap Audio Profile", fileName = "TrapAudioProfile")]
    public sealed class TrapAudioProfile : ScriptableObject
    {
        [Header("Trap")]
        public AudioClip[] trapTriggerVoiceClips;

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoAssign();
        }

        private void AutoAssign()
        {
            string basePath = "Assets/_Project/Audio/SFX/combat/";
            string packagedBasePath = "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/combat/";
            trapTriggerVoiceClips = LoadClips(new[]
            {
                basePath + "traps/trap_snap_real_01.wav",
                basePath + "traps/trap_snap_real_02.wav",
                basePath + "traps/trap_snap_real_03.wav",
                basePath + "traps/trap_snap_real_04.wav",
                packagedBasePath + "traps/trap_snap_real_01.wav",
                packagedBasePath + "traps/trap_snap_real_02.wav",
                packagedBasePath + "traps/trap_snap_real_03.wav",
                packagedBasePath + "traps/trap_snap_real_04.wav"
            }, trapTriggerVoiceClips);
        }

        private static AudioClip[] LoadClips(string[] paths, AudioClip[] fallback)
        {
            if (paths == null || paths.Length == 0)
            {
                return fallback;
            }

            AudioClip[] clips = new AudioClip[paths.Length];
            bool anyLoaded = false;
            for (int i = 0; i < paths.Length; i++)
            {
                clips[i] = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
                anyLoaded |= clips[i] != null;
            }

            return anyLoaded ? clips : fallback;
        }
#endif
    }
}
