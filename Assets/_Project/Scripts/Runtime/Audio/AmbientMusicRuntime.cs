using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ApexShift.Runtime.Audio
{
    /// <summary>
    /// Lightweight ambient music player for imported nature ambience packs.
    ///
    /// It intentionally avoids mixer/profile complexity for the prototype:
    /// - finds Free_Nature_Ambient clips in the editor,
    /// - can also use manually assigned clips,
    /// - can load clips from Resources/Free_Nature_Ambient in builds,
    /// - crossfades between tracks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbientMusicRuntime : MonoBehaviour
    {
        public static AmbientMusicRuntime Active { get; private set; }

        [Header("Playback")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool shuffle = true;
        [SerializeField] private bool loopSingleClip = true;
        [SerializeField] private float targetVolume = 0.22f;
        [SerializeField] private float fadeInSeconds = 4.0f;
        [SerializeField] private float fadeOutSeconds = 4.0f;
        [SerializeField] private float minDelayBetweenTracks = 3.0f;
        [SerializeField] private float maxDelayBetweenTracks = 9.0f;

        [Header("Clip Sources")]
        [SerializeField] private AudioClip[] ambientClips;
        [SerializeField] private string resourcesFolder = "Free_Nature_Ambient";
        [SerializeField] private string editorPathToken = "Free_Nature_Ambient";

        private readonly List<AudioClip> runtimeClips = new List<AudioClip>();
        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private AudioSource inactiveSource;
        private Coroutine playlistRoutine;
        private int lastClipIndex = -1;

        private void Awake()
        {
            if (Active != null && Active != this)
            {
                Destroy(gameObject);
                return;
            }

            Active = this;
            EnsureSources();
            RefreshClipList();
        }

        private void OnEnable()
        {
            Active = this;
            if (playOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public void SetClips(IEnumerable<AudioClip> clips)
        {
            List<AudioClip> unique = new List<AudioClip>();
            if (clips != null)
            {
                foreach (AudioClip clip in clips)
                {
                    if (clip != null && !unique.Contains(clip))
                    {
                        unique.Add(clip);
                    }
                }
            }

            ambientClips = unique.ToArray();
            RefreshClipList();
        }

        public void SetVolume(float volume)
        {
            targetVolume = Mathf.Clamp01(volume);
            if (activeSource != null && activeSource.isPlaying)
            {
                activeSource.volume = targetVolume;
            }
        }

        public void Play()
        {
            EnsureSources();
            RefreshClipList();

            if (runtimeClips.Count == 0)
            {
                Debug.LogWarning("[AmbientMusic] No Free_Nature_Ambient clips found. Import the pack under Assets/_Project/Audio/Free_Nature_Ambient or Resources/Free_Nature_Ambient, or assign clips manually.", this);
                return;
            }

            if (playlistRoutine != null)
            {
                StopCoroutine(playlistRoutine);
            }

            playlistRoutine = StartCoroutine(PlaylistRoutine());
        }

        public void Stop()
        {
            if (playlistRoutine != null)
            {
                StopCoroutine(playlistRoutine);
                playlistRoutine = null;
            }

            if (sourceA != null) sourceA.Stop();
            if (sourceB != null) sourceB.Stop();
        }

        private IEnumerator PlaylistRoutine()
        {
            while (enabled)
            {
                AudioClip clip = PickNextClip();
                if (clip == null)
                {
                    yield break;
                }

                yield return CrossfadeTo(clip);

                if (clip.length > 0.01f)
                {
                    float holdSeconds = Mathf.Max(0.2f, clip.length - Mathf.Max(0.1f, fadeOutSeconds));
                    yield return new WaitForSeconds(holdSeconds);
                }
                else
                {
                    yield return new WaitForSeconds(10f);
                }

                if (runtimeClips.Count <= 1 && loopSingleClip)
                {
                    continue;
                }

                float delay = Random.Range(Mathf.Max(0f, minDelayBetweenTracks), Mathf.Max(minDelayBetweenTracks, maxDelayBetweenTracks));
                if (delay > 0f)
                {
                    yield return FadeOutActive();
                    yield return new WaitForSeconds(delay);
                }
            }
        }

        private IEnumerator CrossfadeTo(AudioClip clip)
        {
            if (clip == null)
            {
                yield break;
            }

            AudioSource next = inactiveSource;
            AudioSource previous = activeSource;

            next.clip = clip;
            next.loop = runtimeClips.Count <= 1 && loopSingleClip;
            next.volume = 0f;
            next.Play();

            float duration = Mathf.Max(0.01f, fadeInSeconds);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float normalized = Mathf.Clamp01(t / duration);
                next.volume = Mathf.Lerp(0f, targetVolume, normalized);
                if (previous != null && previous.isPlaying)
                {
                    previous.volume = Mathf.Lerp(previous.volume, 0f, normalized);
                }

                yield return null;
            }

            next.volume = targetVolume;
            if (previous != null)
            {
                previous.Stop();
                previous.clip = null;
                previous.volume = 0f;
            }

            activeSource = next;
            inactiveSource = previous == sourceA ? sourceA : sourceB;
        }

        private IEnumerator FadeOutActive()
        {
            if (activeSource == null || !activeSource.isPlaying)
            {
                yield break;
            }

            float startVolume = activeSource.volume;
            float duration = Mathf.Max(0.01f, fadeOutSeconds);
            for (float t = 0f; t < duration; t += Time.deltaTime)
            {
                float normalized = Mathf.Clamp01(t / duration);
                activeSource.volume = Mathf.Lerp(startVolume, 0f, normalized);
                yield return null;
            }

            activeSource.Stop();
            activeSource.clip = null;
            activeSource.volume = 0f;
        }

        private AudioClip PickNextClip()
        {
            if (runtimeClips.Count == 0)
            {
                return null;
            }

            if (runtimeClips.Count == 1 || !shuffle)
            {
                lastClipIndex = (lastClipIndex + 1) % runtimeClips.Count;
                return runtimeClips[lastClipIndex];
            }

            int index;
            do
            {
                index = Random.Range(0, runtimeClips.Count);
            }
            while (runtimeClips.Count > 1 && index == lastClipIndex);

            lastClipIndex = index;
            return runtimeClips[index];
        }

        private void EnsureSources()
        {
            if (sourceA == null)
            {
                sourceA = CreateSource("AmbientMusicSourceA");
            }

            if (sourceB == null)
            {
                sourceB = CreateSource("AmbientMusicSourceB");
            }

            if (activeSource == null) activeSource = sourceA;
            if (inactiveSource == null) inactiveSource = sourceB;
        }

        private AudioSource CreateSource(string sourceName)
        {
            GameObject sourceGo = new GameObject(sourceName);
            sourceGo.transform.SetParent(transform, false);
            AudioSource source = sourceGo.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = 0f;
            return source;
        }

        private void RefreshClipList()
        {
            runtimeClips.Clear();

            if (ambientClips != null)
            {
                foreach (AudioClip clip in ambientClips)
                {
                    AddClip(clip);
                }
            }

            foreach (AudioClip clip in UnityEngine.Resources.LoadAll<AudioClip>(resourcesFolder))
            {
                AddClip(clip);
            }

            foreach (AudioClip clip in UnityEngine.Resources.LoadAll<AudioClip>("Audio/Free_Nature_Ambient"))
            {
                AddClip(clip);
            }

#if UNITY_EDITOR
            foreach (AudioClip clip in FindEditorClips())
            {
                AddClip(clip);
            }
#endif
        }

        private void AddClip(AudioClip clip)
        {
            if (clip != null && !runtimeClips.Contains(clip))
            {
                runtimeClips.Add(clip);
            }
        }

#if UNITY_EDITOR
        private IEnumerable<AudioClip> FindEditorClips()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" });
            string normalizedToken = NormalizePathToken(editorPathToken);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string normalizedPath = NormalizePathToken(path);

                if (!normalizedPath.Contains(normalizedToken) &&
                    !normalizedPath.Contains("freenatureambient") &&
                    !normalizedPath.Contains("natureambient"))
                {
                    continue;
                }

                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    yield return clip;
                }
            }
        }

        private static string NormalizePathToken(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace("_", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty).Replace("/", string.Empty).ToLowerInvariant();
        }
#endif
    }
}
