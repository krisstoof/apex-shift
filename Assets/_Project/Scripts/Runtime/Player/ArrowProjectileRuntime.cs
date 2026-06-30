using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Audio;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class ArrowProjectileRuntime : MonoBehaviour
    {
        [SerializeField] private float damage = 15f;
        [SerializeField] private float speed = 18f;
        [SerializeField] private float lifetimeSeconds = 2.5f;
        [SerializeField] private float maxRange = 24f;
        [SerializeField] private float hitRadius = 0.38f;
        [SerializeField] private LayerMask creatureMask = Physics.DefaultRaycastLayers;

        private GameObject owner;
        private AudioClip[] impactVoiceClips;
        private float impactAudioVolume = 0.9f;
        private Vector3 direction = Vector3.forward;
        private Vector3 startPosition;
        private float age;
        private bool configured;

        public void Configure(GameObject owner, Vector3 direction, float damage, float speed, float lifetimeSeconds, float maxRange, float hitRadius, LayerMask creatureMask, AudioClip[] impactVoiceClips, float impactAudioVolume)
        {
            this.owner = owner;
            this.direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            this.damage = Mathf.Max(0f, damage);
            this.speed = Mathf.Max(0.1f, speed);
            this.lifetimeSeconds = Mathf.Max(0.05f, lifetimeSeconds);
            this.maxRange = Mathf.Max(0.5f, maxRange);
            this.hitRadius = Mathf.Max(0.05f, hitRadius);
            this.creatureMask = creatureMask;
            this.impactVoiceClips = impactVoiceClips;
            this.impactAudioVolume = impactAudioVolume;
            startPosition = transform.position;
            transform.rotation = Quaternion.LookRotation(this.direction, Vector3.up);
            configured = true;
            EnsureVisual();
        }

        private void Awake()
        {
            if (!configured)
            {
                startPosition = transform.position;
                EnsureVisual();
            }
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetimeSeconds || Vector3.Distance(startPosition, transform.position) >= maxRange)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 nextPosition = transform.position + direction * (speed * Time.deltaTime);
            if (TryHitCreature(nextPosition))
            {
                Destroy(gameObject);
                return;
            }

            transform.position = nextPosition;
        }

        private bool TryHitCreature(Vector3 nextPosition)
        {
            Collider[] hits = Physics.OverlapSphere(nextPosition, hitRadius, creatureMask, QueryTriggerInteraction.Collide);
            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                if (owner != null && hit.transform.IsChildOf(owner.transform))
                {
                    continue;
                }

                CreatureHealthRuntime health = hit.GetComponentInParent<CreatureHealthRuntime>();
                if (health == null || health.IsDead)
                {
                    continue;
                }

                health.TakeDamage(damage);
                CombatFxSpawner.SpawnHitBurst(health.transform.position + Vector3.up * 0.5f, new Color(0.85f, 0.85f, 0.95f), 0.14f, 0.18f);
                PlayRandomClip(impactVoiceClips, health.transform.position);
                CreatureAgentView view = health.GetComponent<CreatureAgentView>();
                string target = view != null ? view.CreatureId : "creature";
                GameEventBus.PublishCreatureEvent(GameplayEventKind.PlayerProjectileHit, health.transform.position, "default", "player", target, amount: damage, message: "player_arrow_hit");
                return true;
            }

            return false;
        }

        private void EnsureVisual()
        {
            if (transform.childCount > 0)
            {
                return;
            }

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(transform, false);
            shaft.transform.localPosition = Vector3.zero;
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localScale = new Vector3(0.035f, 0.42f, 0.035f);
            RemoveCollider(shaft);
            ApplyMaterial(shaft, new Color(0.42f, 0.25f, 0.10f));

            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "ArrowTip";
            tip.transform.SetParent(transform, false);
            tip.transform.localPosition = Vector3.forward * 0.46f;
            tip.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            tip.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            RemoveCollider(tip);
            ApplyMaterial(tip, new Color(0.45f, 0.45f, 0.42f));

            GameObject featherA = GameObject.CreatePrimitive(PrimitiveType.Cube);
            featherA.name = "ArrowFeatherA";
            featherA.transform.SetParent(transform, false);
            featherA.transform.localPosition = Vector3.back * 0.42f + Vector3.right * 0.04f;
            featherA.transform.localScale = new Vector3(0.03f, 0.12f, 0.16f);
            RemoveCollider(featherA);
            ApplyMaterial(featherA, new Color(0.72f, 0.72f, 0.65f));

            GameObject featherB = GameObject.CreatePrimitive(PrimitiveType.Cube);
            featherB.name = "ArrowFeatherB";
            featherB.transform.SetParent(transform, false);
            featherB.transform.localPosition = Vector3.back * 0.42f + Vector3.left * 0.04f;
            featherB.transform.localScale = new Vector3(0.03f, 0.12f, 0.16f);
            RemoveCollider(featherB);
            ApplyMaterial(featherB, new Color(0.72f, 0.72f, 0.65f));
        }

        private static void RemoveCollider(GameObject go)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(collider);
            }
            else
            {
                DestroyImmediate(collider);
            }
        }

        private static void ApplyMaterial(GameObject go, Color color)
        {
            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (material.shader == null)
            {
                material.shader = Shader.Find("Standard");
            }

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;
            renderer.sharedMaterial = material;
        }

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

                    PlaySpatialAudio(clip, position, Mathf.Clamp01(impactAudioVolume));
                    return;
                }
            }

            // Fallback to AudioLibrary
            AudioClip fallbackClip = AudioLibrary.GetRandomArrowHitFlesh();
            if (fallbackClip != null)
            {
                PlaySpatialAudio(fallbackClip, position, Mathf.Clamp01(impactAudioVolume));
            }
        }

        private void PlaySpatialAudio(AudioClip clip, Vector3 position, float volume)
        {
            GameObject emitter = new GameObject($"ArrowAudio_{clip.name}");
            emitter.transform.position = position;
            AudioSource source = emitter.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1f;
            source.maxDistance = 25f;
            source.playOnAwake = false;
            source.dopplerLevel = 0f;
            source.Play();
            Object.Destroy(emitter, clip.length + 0.25f);
        }
    }
}
