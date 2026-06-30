using System.Collections.Generic;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Audio;
using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    [DisallowMultipleComponent]
    public sealed class TrapDamageRuntime : MonoBehaviour
    {
        [SerializeField] private string trapId = string.Empty;
        [SerializeField] private Vector3 footprintSize = new Vector3(2f, 0.5f, 2f);
        [SerializeField] private float damage = 24f;
        [SerializeField] private float triggerCooldownSeconds = 1.25f;
        [SerializeField] private float scanIntervalSeconds = 0.15f;
        [SerializeField] private LayerMask creatureMask = Physics.DefaultRaycastLayers;
        [SerializeField] private TrapAudioProfile trapAudioProfile;
        [SerializeField] private AudioClip[] triggerVoiceClips;
        [SerializeField] private float audioVolume = 0.85f;

        private readonly Dictionary<int, float> lastTriggerTimes = new Dictionary<int, float>();
        private float scanTimer;

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyTrapAudioProfile();
            AutoAssignTrapAudio();
        }
#endif

        public void Configure(string trapId, Vector3 footprint)
        {
            this.trapId = string.IsNullOrWhiteSpace(trapId) ? gameObject.name : trapId.Trim();
            footprintSize = new Vector3(Mathf.Max(0.25f, footprint.x), Mathf.Max(0.25f, footprint.y), Mathf.Max(0.25f, footprint.z));
        }

        public void SetTrapAudioProfile(TrapAudioProfile profile) => trapAudioProfile = profile;

        private void Update()
        {
            scanTimer -= Time.deltaTime;
            if (scanTimer > 0f)
            {
                return;
            }

            scanTimer = Mathf.Max(0.05f, scanIntervalSeconds);
            ScanForCreatures();
        }

        private void ScanForCreatures()
        {
            Vector3 halfExtents = new Vector3(Mathf.Max(0.25f, footprintSize.x * 0.5f), Mathf.Max(0.25f, footprintSize.y * 0.5f), Mathf.Max(0.25f, footprintSize.z * 0.5f));
            Vector3 center = transform.position + Vector3.up * Mathf.Max(0.20f, halfExtents.y);
            Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, creatureMask, QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                CreatureHealthRuntime health = hit.GetComponentInParent<CreatureHealthRuntime>();
                if (health == null || health.IsDead)
                {
                    continue;
                }

                int key = health.gameObject != null ? health.gameObject.GetHashCode() : health.GetHashCode();
                if (lastTriggerTimes.TryGetValue(key, out float lastTime) && Time.time - lastTime < Mathf.Max(0.05f, triggerCooldownSeconds))
                {
                    continue;
                }

                lastTriggerTimes[key] = Time.time;
                health.TakeDamage(damage);
                PlayRandomClip(triggerVoiceClips, health.transform.position);
                CreatureAgentView view = health.GetComponent<CreatureAgentView>();
                string target = view != null ? view.CreatureId : "creature";
                GameEventBus.PublishCreatureEvent(GameplayEventKind.TrapTriggered, health.transform.position, "default", "trap", target, amount: damage, message: $"trap_triggered:{trapId}");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 halfExtents = new Vector3(Mathf.Max(0.25f, footprintSize.x * 0.5f), Mathf.Max(0.25f, footprintSize.y * 0.5f), Mathf.Max(0.25f, footprintSize.z * 0.5f));
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position + Vector3.up * Mathf.Max(0.20f, halfExtents.y), transform.rotation, Vector3.one);
            Gizmos.color = new Color(1f, 0.1f, 0.05f, 0.28f);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2f);
            Gizmos.color = new Color(1f, 0.1f, 0.05f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
            Gizmos.matrix = oldMatrix;
        }
#endif

        private void PlayRandomClip(AudioClip[] clips, Vector3 position)
        {
            if (clips != null && clips.Length > 0)
            {
                int start = Random.Range(0, clips.Length);
                for (int i = 0; i < clips.Length; i++)
                {
                    AudioClip clip = clips[(start + i) % clips.Length];
                    if (clip == null)
                    {
                        continue;
                    }

                    PlaySpatialAudio(clip, position, Mathf.Clamp01(audioVolume));
                    return;
                }
            }

            // Fallback to AudioLibrary
            AudioClip fallbackClip = AudioLibrary.GetRandomTrapSnap();
            if (fallbackClip != null)
            {
                PlaySpatialAudio(fallbackClip, position, Mathf.Clamp01(audioVolume));
            }
        }

        private static void PlaySpatialAudio(AudioClip clip, Vector3 position, float volume)
        {
            GameObject emitter = new GameObject($"TrapAudio_{clip.name}");
            emitter.transform.position = position;
            AudioSource source = emitter.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2f;
            source.maxDistance = 30f;
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.Play();
            Object.Destroy(emitter, clip.length + 0.25f);
        }

        private void ApplyTrapAudioProfile()
        {
            if (trapAudioProfile == null)
            {
                return;
            }

            triggerVoiceClips = trapAudioProfile.trapTriggerVoiceClips;
        }

#if UNITY_EDITOR
        private void AutoAssignTrapAudio()
        {
            string basePath = "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/combat/";
            triggerVoiceClips = LoadClips(new[]
            {
                basePath + "traps/trap_snap_real_01.wav",
                basePath + "traps/trap_snap_real_02.wav",
                basePath + "traps/trap_snap_real_03.wav",
                basePath + "traps/trap_snap_real_04.wav"
            }, triggerVoiceClips);
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
