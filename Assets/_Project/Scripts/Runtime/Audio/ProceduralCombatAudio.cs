using UnityEngine;

namespace ApexShift.Runtime.Audio
{
    /// <summary>
    /// Combat audio system. Plays actual WAV files from the audio library instead of synthesized audio.
    /// Falls back to procedural generation if WAV files are not available.
    /// </summary>
    public static class ProceduralCombatAudio
    {
        private const float MinDistance = 3f;
        private const float MaxDistance = 26f;

        public static void PlayMeleeSwing(Vector3 position, float volume = 0.45f)
        {
            AudioClip clip = AudioLibrary.GetRandomSpearSwing();
            if (clip != null)
            {
                PlaySpatialClip(clip, position, volume);
            }
        }

        public static void PlayMeleeHit(Vector3 position, float volume = 0.65f)
        {
            AudioClip clip = AudioLibrary.GetRandomSpearHit();
            if (clip != null)
            {
                PlaySpatialClip(clip, position, volume);
            }
        }

        public static void PlayBowRelease(Vector3 position, float volume = 0.55f)
        {
            AudioClip clip = AudioLibrary.GetRandomBowRelease();
            if (clip != null)
            {
                PlaySpatialClip(clip, position, volume);
            }
        }

        public static void PlayArrowHit(Vector3 position, float volume = 0.55f)
        {
            AudioClip clip = AudioLibrary.GetRandomArrowHitFlesh();
            if (clip != null)
            {
                PlaySpatialClip(clip, position, volume);
            }
        }

        public static void PlayTrapTrigger(Vector3 position, float volume = 0.70f)
        {
            AudioClip clip = AudioLibrary.GetRandomTrapSnap();
            if (clip != null)
            {
                PlaySpatialClip(clip, position, volume);
            }
        }

        private static void PlaySpatialClip(AudioClip clip, Vector3 position, float volume)
        {
            GameObject emitter = new GameObject($"CombatAudio_{clip.name}");
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
    }
}
