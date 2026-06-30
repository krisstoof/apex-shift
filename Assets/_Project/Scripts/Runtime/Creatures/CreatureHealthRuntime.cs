using UnityEngine;
using ApexShift.Runtime.Config;
using ApexShift.Runtime.Audio;

namespace ApexShift.Runtime.Creatures
{
    [DisallowMultipleComponent]
    public sealed class CreatureHealthRuntime : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 30f;
        [SerializeField] private float currentHealth = 30f;
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private SpeciesDefinition speciesDefinition;
        [SerializeField] private GameBalanceConfig gameBalanceConfig;
        [SerializeField] private CreatureAudioProfile creatureAudioProfile;

#if UNITY_EDITOR
        private void OnValidate()
        {
        }
#endif

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;

        public void Configure(string creatureId)
        {
            Configure(creatureId, null);
        }

        public void Configure(string creatureId, SpeciesDefinition overrideDefinition)
        {
            SpeciesDefinition resolved = GameBalanceConfigProvider.ResolveSpeciesDefinition(gameBalanceConfig, overrideDefinition != null ? overrideDefinition : speciesDefinition, creatureId, this);
            maxHealth = resolved != null ? resolved.MaxHealth : 30f;

            currentHealth = maxHealth;
        }

        public void SetSpeciesDefinitionForTests(SpeciesDefinition definition) => speciesDefinition = definition;

        public void SetGameBalanceConfigForTests(GameBalanceConfig config) => gameBalanceConfig = config;
        public void SetCreatureAudioProfileForTests(CreatureAudioProfile profile) => creatureAudioProfile = profile;

        public void RestoreHealth(float restoredMaxHealth, float restoredCurrentHealth, bool dead)
        {
            maxHealth = Mathf.Max(0.01f, restoredMaxHealth);
            currentHealth = dead ? 0f : Mathf.Clamp(restoredCurrentHealth, 0f, maxHealth);
            enabled = !dead;

            if (dead)
            {
                CreatureBehaviorRuntime behavior = GetComponent<CreatureBehaviorRuntime>();
                if (behavior != null)
                {
                    behavior.OnCreatureDied();
                }
                else
                {
                    GetComponent<CreatureBehaviorBrain>()?.OnCreatureDied();
                }
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead)
            {
                return;
            }

            PlayHitAudio();
            currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
            if (currentHealth <= 0f)
            {
                PlayDeathAudio();
                var behavior = GetComponent<CreatureBehaviorRuntime>();
                if (behavior != null)
                {
                    behavior.OnCreatureDied();
                }

                CreatureMeatDropFactory.TrySpawnMeatDrop(transform.position, GetComponent<CreatureAgentView>());
                CreatureMeatDropFactory.TrySpawnBoneDrop(transform.position, GetComponent<CreatureAgentView>());

                if (destroyOnDeath)
                {
                    Destroy(gameObject, 0.1f);
                }
                else
                {
                    enabled = false;
                }
            }
        }

        public void Heal(float amount)
        {
            if (IsDead)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0f, amount));
        }

        private void PlayHitAudio()
        {
            AudioClip[] clips = creatureAudioProfile != null
                ? (creatureAudioProfile.varnakHurtClips != null && creatureAudioProfile.varnakHurtClips.Length > 0
                    ? creatureAudioProfile.varnakHurtClips
                    : creatureAudioProfile.grazerAlertClips != null && creatureAudioProfile.grazerAlertClips.Length > 0
                        ? creatureAudioProfile.grazerAlertClips
                        : creatureAudioProfile.smallPreyAlarmClips)
                : null;

            if (clips == null || clips.Length == 0)
            {
                // Fallback to AudioLibrary
                AudioClip clip = AudioLibrary.GetRandomVarnakHurt() ?? AudioLibrary.GetRandomGrazerAlert();
                if (clip != null)
                {
                PlaySpatialAudio(clip, transform.position, 0.26f);
                }
                return;
            }

            PlayRandomClip(clips);
        }

        private void PlayDeathAudio()
        {
            if (creatureAudioProfile == null || creatureAudioProfile.deathVoiceClips == null || creatureAudioProfile.deathVoiceClips.Length == 0)
            {
                // Fallback to AudioLibrary - try to find death audio based on creature type
                AudioClip clip = AudioLibrary.GetRandomVarnakDeath() ?? AudioLibrary.GetRandomGrazerDeath();
                if (clip != null)
                {
                    PlaySpatialAudio(clip, transform.position, 0.9f);
                }
                return;
            }

            PlayRandomClip(creatureAudioProfile.deathVoiceClips);
        }

        private void PlayRandomClip(AudioClip[] clips)
        {
            int start = Random.Range(0, clips.Length);
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClip clip = clips[(start + i) % clips.Length];
                if (clip == null)
                {
                    continue;
                }

                PlaySpatialAudio(clip, transform.position, 0.9f);
                return;
            }
        }

        private void PlaySpatialAudio(AudioClip clip, Vector3 position, float volume)
        {
            GameObject emitter = new GameObject($"CreatureAudio_{clip.name}");
            emitter.transform.position = position;
            AudioSource source = emitter.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 0.85f;
            source.maxDistance = 6.5f;
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.Play();
            Object.Destroy(emitter, clip.length + 0.25f);
        }
    }
}
