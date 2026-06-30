using UnityEngine;

namespace ApexShift.Runtime.Audio
{
    [CreateAssetMenu(menuName = "ApexShift/Audio/Combat Audio Profile", fileName = "CombatAudioProfile")]
    public sealed class CombatAudioProfile : ScriptableObject
    {
        [Header("Player Combat")]
        public AudioClip[] attackVoiceClips;
        public AudioClip[] meleeHitVoiceClips;
        public AudioClip[] bowHitVoiceClips;
        public AudioClip[] bowVoiceClips;

        [System.Obsolete("Use meleeHitVoiceClips instead.")]
        public AudioClip[] hitVoiceClips;

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoAssign();
        }

        private void AutoAssign()
        {
            string basePath = "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/combat/";
            attackVoiceClips = LoadClips(new[]
            {
                basePath + "player/spear_swing_real_01.wav",
                basePath + "player/spear_swing_real_02.wav",
                basePath + "player/spear_swing_real_03.wav",
                basePath + "player/spear_swing_real_04.wav"
            }, attackVoiceClips);

            meleeHitVoiceClips = LoadClips(new[]
            {
                basePath + "player/spear_hit_flesh_real_01.wav",
                basePath + "player/spear_hit_flesh_real_02.wav",
                basePath + "player/spear_hit_flesh_real_03.wav",
                basePath + "player/spear_hit_flesh_real_04.wav"
            }, meleeHitVoiceClips);

            bowVoiceClips = LoadClips(new[]
            {
                basePath + "player/bow_release_real_01.wav",
                basePath + "player/bow_release_real_02.wav",
                basePath + "player/bow_release_real_03.wav",
                basePath + "player/bow_release_real_04.wav",
                basePath + "player/arrow_fly_real_01.wav",
                basePath + "player/arrow_fly_real_02.wav",
                basePath + "player/arrow_fly_real_03.wav",
                basePath + "player/arrow_fly_real_04.wav"
            }, bowVoiceClips);

            bowHitVoiceClips = LoadClips(new[]
            {
                basePath + "player/arrow_hit_flesh_real_01.wav",
                basePath + "player/arrow_hit_wood_real_01.wav",
                basePath + "player/arrow_hit_stone_real_01.wav"
            }, bowHitVoiceClips);
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
