using UnityEngine;

namespace ApexShift.Runtime.Audio
{
    [CreateAssetMenu(menuName = "ApexShift/Audio/Creature Audio Profile", fileName = "CreatureAudioProfile")]
    public sealed class CreatureAudioProfile : ScriptableObject
    {
        public AudioClip[] smallPreyAlarmClips;
        public AudioClip[] grazerAlertClips;
        public AudioClip[] varnakGrowlClips;
        public AudioClip[] varnakAttackClips;
        public AudioClip[] varnakHurtClips;
        public AudioClip[] deathVoiceClips;
        public AudioClip[] footstepClips;

#if UNITY_EDITOR
        private void OnValidate()
        {
            AutoAssign();
        }

        private void AutoAssign()
        {
            string basePath = "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/creatures/";
            smallPreyAlarmClips = LoadClips(new[]
            {
                basePath + "small_prey/small_prey_alarm_real_01.wav",
                basePath + "small_prey/small_prey_alarm_real_02.wav",
                basePath + "small_prey/small_prey_alarm_real_03.wav",
            }, smallPreyAlarmClips);

            grazerAlertClips = LoadClips(new[]
            {
                basePath + "grazer/grazer_alert_real_01.wav",
                basePath + "grazer/grazer_alert_real_02.wav",
                basePath + "grazer/grazer_alert_real_03.wav",
            }, grazerAlertClips);

            varnakGrowlClips = LoadClips(new[]
            {
                basePath + "varnak/varnak_growl_real_01.wav",
                basePath + "varnak/varnak_growl_real_02.wav",
                basePath + "varnak/varnak_growl_real_03.wav",
            }, varnakGrowlClips);

            varnakAttackClips = LoadClips(new[]
            {
                basePath + "varnak/varnak_attack_real_01.wav",
                basePath + "varnak/varnak_attack_real_02.wav",
                basePath + "varnak/varnak_attack_real_03.wav",
            }, varnakAttackClips);

            varnakHurtClips = LoadClips(new[]
            {
                basePath + "varnak/varnak_hurt_real_01.wav",
                basePath + "varnak/varnak_hurt_real_02.wav",
                basePath + "varnak/varnak_hurt_real_03.wav",
            }, varnakHurtClips);

            deathVoiceClips = LoadClips(new[]
            {
                basePath + "grazer/grazer_death_real_01.wav",
                basePath + "varnak/varnak_death_real_01.wav"
            }, deathVoiceClips);

            footstepClips = LoadClips(new[]
            {
                "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/footsteps/footstep_grass_real_01.wav",
                "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/footsteps/footstep_grass_real_02.wav",
                "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/footsteps/footstep_grass_real_03.wav",
                "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/footsteps/footstep_dirt_real_01.wav",
                "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/footsteps/footstep_dirt_real_02.wav",
                "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/footsteps/footstep_dirt_real_03.wav",
                "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/footsteps/footstep_stone_real_01.wav",
                "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/footsteps/footstep_stone_real_02.wav",
                "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/footsteps/footstep_stone_real_03.wav"
            }, footstepClips);
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
