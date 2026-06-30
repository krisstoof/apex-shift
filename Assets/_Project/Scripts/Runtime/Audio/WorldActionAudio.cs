using UnityEngine;

namespace ApexShift.Runtime.Audio
{
    /// <summary>
    /// World action audio system. Plays UI and environmental sounds.
    /// Uses procedural fallbacks when specific audio files are not available.
    /// </summary>
    public static class WorldActionAudio
    {
        private const float MinDistance = 2.5f;
        private const float MaxDistance = 18f;

        public static void PlayBuild(Vector3 position)
        {
            // Use wood_chop sound as a placeholder for building
            AudioClip clip = AudioLibrary.GetRandomWoodChop();
            if (clip != null)
            {
                PlaySpatialClip(clip, position, 0.55f);
            }
        }

        public static void PlayPickup(Vector3 position)
        {
            // Create a soft pickup sound effect
            AudioClip clip = CreatePickupSound();
            if (clip != null)
            {
                PlaySpatialClip(clip, position, 0.5f);
            }
        }

        public static void PlayUIClick(Vector3 position)
        {
            // Create UI click sound
            AudioClip clip = CreateClickSound();
            if (clip != null)
            {
                PlaySpatialClip(clip, position, 0.35f);
            }
        }

        public static void PlayUIInvalid(Vector3 position)
        {
            // Create UI invalid sound
            AudioClip clip = CreateInvalidSound();
            if (clip != null)
            {
                PlaySpatialClip(clip, position, 0.4f);
            }
        }

        private static void PlaySpatialClip(AudioClip clip, Vector3 position, float volume)
        {
            GameObject emitter = new GameObject($"WorldAudio_{clip.name}");
            emitter.transform.position = position;
            AudioSource source = emitter.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = MinDistance;
            source.maxDistance = MaxDistance;
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.Play();
            Object.Destroy(emitter, clip.length + 0.25f);
        }

        private static AudioClip CreatePickupSound()
        {
            const int sampleRate = 44100;
            float duration = 0.18f;
            float[] data = new float[Mathf.CeilToInt(duration * sampleRate)];
            
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)sampleRate;
                
                // Quick, happy pickup sound
                float env = Mathf.Min(1f, t / 0.02f) * Mathf.Exp(-t * 11f);
                
                // Rising pitch sweep (ascending arpeggio effect)
                float pitch1 = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.22f;
                float pitch2 = Mathf.Sin(2f * Mathf.PI * 680f * t) * 0.18f;
                float pitch3 = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.14f;
                float bell = Mathf.PerlinNoise(i * 0.017f, 4.2f) * 2f - 1f;
                bell *= Mathf.Exp(-t * 8f);
                
                data[i] = (pitch1 * (1f - t / duration) + pitch2 * (t / duration * 0.5f) + pitch3 * (t / duration * 0.3f) + bell * 0.08f) * env;
            }
            
            FadeAudio(data, 0.003f, 0.03f, sampleRate);
            AudioClip clip = AudioClip.Create("proc_pickup_fallback", data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateClickSound()
        {
            const int sampleRate = 44100;
            float duration = 0.12f;
            float[] data = new float[Mathf.CeilToInt(duration * sampleRate)];
            
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)sampleRate;
                
                // Sharp, clean click
                float env = Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t * 18f);
                
                float highClick = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(960f, 240f, t / duration) * t) * 0.24f;
                float lowClick = Mathf.Sin(2f * Mathf.PI * 380f * t) * 0.12f;
                
                data[i] = (highClick + lowClick) * env;
            }
            
            FadeAudio(data, 0.002f, 0.02f, sampleRate);
            AudioClip clip = AudioClip.Create("proc_click_fallback", data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateInvalidSound()
        {
            const int sampleRate = 44100;
            float duration = 0.20f;
            float[] data = new float[Mathf.CeilToInt(duration * sampleRate)];
            
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)sampleRate;
                
                // Descending sad tone
                float env = Mathf.Min(1f, t / 0.04f) * Mathf.Exp(-t * 7f);
                
                float descend1 = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(480f, 240f, t / duration) * t) * 0.25f;
                float descend2 = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(360f, 180f, t / duration) * t) * 0.15f;
                
                float noise = Mathf.PerlinNoise(i * 0.016f, 7.8f) * 2f - 1f;
                
                data[i] = (descend1 + descend2 + noise * 0.10f) * env;
            }
            
            FadeAudio(data, 0.003f, 0.035f, sampleRate);
            AudioClip clip = AudioClip.Create("proc_invalid_fallback", data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static void FadeAudio(float[] data, float fadeInSeconds, float fadeOutSeconds, int sampleRate)
        {
            int fadeIn = Mathf.Min(data.Length, Mathf.CeilToInt(fadeInSeconds * sampleRate));
            int fadeOut = Mathf.Min(data.Length, Mathf.CeilToInt(fadeOutSeconds * sampleRate));

            for (int i = 0; i < fadeIn; i++)
            {
                data[i] *= i / Mathf.Max(1f, fadeIn);
            }

            for (int i = 0; i < fadeOut; i++)
            {
                int index = data.Length - 1 - i;
                data[index] *= i / Mathf.Max(1f, fadeOut);
            }
        }
    }
}
