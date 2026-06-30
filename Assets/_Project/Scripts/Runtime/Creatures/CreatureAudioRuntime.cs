using ApexShift.Runtime.Audio;
using UnityEngine;

namespace ApexShift.Runtime.Creatures
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class CreatureAudioRuntime : MonoBehaviour
    {
        [SerializeField] private CreatureAudioProfile creatureAudioProfile;
        [SerializeField] private CreatureAnimationDriver animationDriver;
        [SerializeField] private CreatureBehaviorBrain behaviorBrain;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private string creatureId;
        [SerializeField] private float walkFootstepInterval = 0.40f;
        [SerializeField] private float runFootstepInterval = 0.22f;
        [SerializeField] private float creatureVolumeMultiplier = 0.48f;
        [SerializeField] private float creatureMinDistance = 0.85f;
        [SerializeField] private float creatureMaxDistance = 6.5f;
        [SerializeField] private float voiceCooldownSeconds = 3.25f;
        [SerializeField] private bool muteWhenOutsideCameraView = true;
        [SerializeField] private float cameraViewMargin = 0.03f;

        private float voiceCooldownTimer;

        private float footstepTimer;
        private CreatureBehaviorState lastState;

        private void Awake() => Resolve();

        private void Update()
        {
            Resolve();
            if (audioSource == null || creatureAudioProfile == null)
            {
                return;
            }

            string id = ResolveCreatureId();
            CreatureBehaviorState state = behaviorBrain != null ? behaviorBrain.State : CreatureBehaviorState.Idle;
            if (voiceCooldownTimer > 0f)
            {
                voiceCooldownTimer = Mathf.Max(0f, voiceCooldownTimer - Time.deltaTime);
            }
            if (state != lastState)
            {
                if (voiceCooldownTimer <= 0f) PlayStateVoice(id, state);
                lastState = state;
            }

            float locomotion = animationDriver != null ? animationDriver.CurrentState : 0f;
            if (locomotion <= 0.12f)
            {
                footstepTimer = 0f;
                return;
            }

            float cadence = locomotion >= 0.6f ? runFootstepInterval : walkFootstepInterval;
            footstepTimer -= Time.deltaTime;
            if (footstepTimer > 0f)
            {
                return;
            }

            footstepTimer = cadence;
            PlayRandom(creatureAudioProfile.footstepClips, locomotion >= 0.6f ? 0.22f : 0.14f);
        }

        public void SetCreatureAudioProfile(CreatureAudioProfile profile) => creatureAudioProfile = profile;
        public void Configure(string id) => creatureId = string.IsNullOrWhiteSpace(id) ? creatureId : id.Trim().ToLowerInvariant();

        private void Resolve()
        {
            if (audioSource == null)
            {
                if (!TryGetComponent(out audioSource))
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            if (audioSource != null)
            {
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.minDistance = Mathf.Max(0.25f, creatureMinDistance);
                audioSource.maxDistance = Mathf.Max(audioSource.minDistance + 1f, creatureMaxDistance);
                audioSource.dopplerLevel = 0f;
            }

            if (animationDriver == null) animationDriver = GetComponent<CreatureAnimationDriver>();
            if (behaviorBrain == null) behaviorBrain = GetComponent<CreatureBehaviorBrain>();
        }

        private string ResolveCreatureId()
        {
            if (!string.IsNullOrWhiteSpace(creatureId))
            {
                return creatureId;
            }

            CreatureAgentView view = GetComponent<CreatureAgentView>();
            return view != null ? view.CreatureId : string.Empty;
        }

        private void PlayStateVoice(string id, CreatureBehaviorState state)
        {
            if (state == CreatureBehaviorState.Dead)
            {
                PlayRandom(creatureAudioProfile?.deathVoiceClips, 0.85f);
                return;
            }

            if (id == "varnak")
            {
                if (state == CreatureBehaviorState.Attack || state == CreatureBehaviorState.Chase || state == CreatureBehaviorState.Stalk)
                {
                    if (PlayRandom(creatureAudioProfile?.varnakAttackClips, 0.42f))
                        return;
                    PlayVarnakAudioFromLibrary(true);
                    return;
                }

                if (PlayRandom(creatureAudioProfile?.varnakGrowlClips, 0.32f))
                    return;
                PlayVarnakAudioFromLibrary(false);
                return;
            }

            if (id == "grazer")
            {
                if (PlayRandom(creatureAudioProfile?.grazerAlertClips, 0.24f))
                    return;
                PlayGrazerAudioFromLibrary();
                return;
            }

            if (id == "small_prey")
            {
                if (PlayRandom(creatureAudioProfile?.smallPreyAlarmClips, 0.20f))
                    return;
                PlaySmallPreyAudioFromLibrary();
            }
        }

        private void PlayVarnakAudioFromLibrary(bool isAttack)
        {
            AudioClip clip = isAttack ? AudioLibrary.GetRandomVarnakAttack() : AudioLibrary.GetRandomVarnakGrowl();
            if (clip != null)
                PlayOneShot(clip, isAttack ? 0.42f : 0.32f);
        }

        private void PlayGrazerAudioFromLibrary()
        {
            AudioClip clip = AudioLibrary.GetRandomGrazerAlert();
            if (clip != null)
                PlayOneShot(clip, 0.24f);
        }

        private void PlaySmallPreyAudioFromLibrary()
        {
            AudioClip clip = AudioLibrary.GetRandomSmallPreyAlarm();
            if (clip != null)
                PlayOneShot(clip, 0.20f);
        }

        private bool PlayRandom(AudioClip[] clips, float volume)
        {
            if (clips == null || clips.Length == 0)
            {
                return false;
            }

            int start = Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[(start + i) % clips.Length];
                if (clip == null)
                {
                    continue;
                }

                PlayOneShot(clip, volume);
                return true;
            }
            return false;
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || audioSource == null) return;
            if (!IsAudibleFromCurrentCamera())
            {
                return;
            }

            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume * creatureVolumeMultiplier));
            voiceCooldownTimer = Mathf.Max(voiceCooldownTimer, voiceCooldownSeconds);
        }

        private bool IsAudibleFromCurrentCamera()
        {
            if (!muteWhenOutsideCameraView)
            {
                return true;
            }

            UnityEngine.Camera camera = UnityEngine.Camera.main != null ? UnityEngine.Camera.main : Object.FindAnyObjectByType<UnityEngine.Camera>();
            if (camera == null)
            {
                return true;
            }

            Vector3 viewport = camera.WorldToViewportPoint(transform.position + Vector3.up * 0.6f);
            if (viewport.z <= 0f)
            {
                return false;
            }

            float margin = Mathf.Clamp(cameraViewMargin, 0f, 0.25f);
            return viewport.x >= -margin && viewport.x <= 1f + margin &&
                   viewport.y >= -margin && viewport.y <= 1f + margin;
        }
    }
}
