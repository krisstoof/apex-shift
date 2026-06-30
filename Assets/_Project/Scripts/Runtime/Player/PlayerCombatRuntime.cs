using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Events;
using ApexShift.Runtime.Audio;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatRuntime : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerInventoryRuntime inventoryRuntime;
        [SerializeField] private PlayerSurvivalRuntime survivalRuntime;
        [SerializeField] private Transform attackOrigin;

        [Header("Prototype Rules")]
        [SerializeField] private bool allowPrototypeUnarmedAttack = true;

        [Header("Melee")]
        [SerializeField] private float unarmedDamage = 5f;
        [SerializeField] private float spearDamage = 18f;
        [SerializeField] private float spearRange = 2.25f;
        [SerializeField] private float unarmedRange = 1.35f;
        [SerializeField] private float attackArcDegrees = 145f;
        [SerializeField] private float meleeForgivenessRange = 0.85f;
        [SerializeField] private float prototypeGuaranteedMeleeHitRange = 4.75f;
        [SerializeField] private float meleeCooldownSeconds = 0.55f;
        [SerializeField] private float meleeStaminaCost = 8f;

        [Header("Bow")]
        [SerializeField] private float bowDamage = 15f;
        [SerializeField] private float bowCooldownSeconds = 1.05f;
        [SerializeField] private float bowStaminaCost = 12f;
        [SerializeField] private float arrowSpeed = 18f;
        [SerializeField] private float arrowLifetimeSeconds = 2.5f;
        [SerializeField] private float arrowMaxRange = 24f;
        [SerializeField] private float arrowHitRadius = 0.38f;

        [Header("Collision")]
        [SerializeField] private LayerMask creatureMask = Physics.DefaultRaycastLayers;
        [SerializeField] private float attackHeightOffset = 0.9f;

        [Header("Audio")]
        [SerializeField] private CombatAudioProfile combatAudioProfile;
        [SerializeField] private AudioClip[] attackVoiceClips;
        [SerializeField] private AudioClip[] meleeHitVoiceClips;
        [SerializeField] private AudioClip[] bowVoiceClips;
        [SerializeField] private AudioClip[] bowHitVoiceClips;
        [SerializeField] private float audioVolume = 0.9f;

        private float cooldownRemaining;

        public bool IsOnCooldown => cooldownRemaining > 0f;

        private void Awake()
        {
            ResolveReferences();
            ApplyCombatAudioProfile();
#if UNITY_EDITOR
            AutoAssignCombatAudio();
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ApplyCombatAudioProfile();
            AutoAssignCombatAudio();
        }
#endif

        private void OnEnable()
        {
            ResolveReferences();
            if (inputReader != null)
            {
                inputReader.AttackPressed += HandleAttackPressed;
            }
        }

        private void OnDisable()
        {
            if (inputReader != null)
            {
                inputReader.AttackPressed -= HandleAttackPressed;
            }
        }

        private void Update()
        {
            if (cooldownRemaining > 0f)
            {
                cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
            }
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            if (inputReader == reader)
            {
                return;
            }

            if (isActiveAndEnabled && inputReader != null)
            {
                inputReader.AttackPressed -= HandleAttackPressed;
            }

            inputReader = reader;

            if (isActiveAndEnabled && inputReader != null)
            {
                inputReader.AttackPressed += HandleAttackPressed;
            }
        }

        public void SetInventoryRuntime(PlayerInventoryRuntime runtime) => inventoryRuntime = runtime;
        public void SetSurvivalRuntime(PlayerSurvivalRuntime runtime) => survivalRuntime = runtime;
        public void SetAttackOrigin(Transform origin) => attackOrigin = origin != null ? origin : transform;

        public bool TriggerPrimaryAttack()
        {
            ResolveReferences();
            if (cooldownRemaining > 0f)
            {
                return false;
            }

            Vector3 direction = ResolveAimDirection();
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = transform.forward;
                direction.y = 0f;
                direction.Normalize();
            }

            // In the current prototype there is no explicit "equipped weapon" state yet.
            // Prefer melee if the player owns a spear or unarmed prototype attacks are allowed.
            // Otherwise owning a bow makes every LMB fire a projectile, which feels like
            // melee is broken and makes close combat unreliable.
            if (HasItem("spear") || allowPrototypeUnarmedAttack)
            {
                return TryMeleeAttack(direction, HasItem("spear"));
            }

            if (HasItem("bow"))
            {
                return TryFireBow(direction);
            }

            PublishCombatEvent(GameplayEventKind.PlayerMeleeHit, "player_attack_no_weapon", transform.position, "none", "no_weapon", 0f);
            return false;
        }

        private void HandleAttackPressed() => TriggerPrimaryAttack();

        private bool TryMeleeAttack(Vector3 direction, bool hasSpear)
        {
            float damage = hasSpear ? spearDamage : unarmedDamage;
            float range = hasSpear ? spearRange : unarmedRange;
            if (!TrySpendStamina(meleeStaminaCost))
            {
                PublishCombatEvent(GameplayEventKind.PlayerMeleeHit, "player_melee_no_stamina", transform.position, hasSpear ? "spear" : "unarmed", "stamina", 0f);
                return false;
            }

            cooldownRemaining = Mathf.Max(0.05f, meleeCooldownSeconds);
            Vector3 origin = GetAttackOrigin();
            CombatFxSpawner.SpawnSlashArc(origin, direction, range, hasSpear, 0.16f);
            PlayRandomClip(attackVoiceClips, origin);
            ProceduralCombatAudio.PlayMeleeSwing(origin, audioVolume * 0.55f);
            CreaturePlayerAwarenessBehavior.NotifyNearby(transform.position, transform, Mathf.Max(8f, range + 8f), 0.85f, "player_attack_noise");

            CreatureHealthRuntime target = FindMeleeTarget(origin, direction, range, attackArcDegrees);
            if (target == null)
            {
                Debug.DrawRay(origin, direction * range, Color.yellow, 0.25f);
                PublishCombatEvent(GameplayEventKind.PlayerMeleeHit, "player_melee_miss", origin, hasSpear ? "spear" : "unarmed", "miss", 0f);
                return false;
            }

            target.TakeDamage(damage);
            CreaturePlayerAwarenessBehavior.NotifyCreatureHit(target, transform, 1f, "player_melee_hit");
            CreaturePlayerAwarenessBehavior.NotifyNearby(target.transform.position, transform, Mathf.Max(10f, range + 10f), 1f, "player_melee_hit_nearby");
            Debug.DrawLine(origin, target.transform.position + Vector3.up * 0.5f, Color.red, 0.35f);
            CombatFxSpawner.SpawnHitBurst(target.transform.position + Vector3.up * 0.5f, hasSpear ? new Color(0.92f, 0.36f, 0.24f) : new Color(0.90f, 0.82f, 0.25f));
            PlayRandomClip(meleeHitVoiceClips, target.transform.position);
            ProceduralCombatAudio.PlayMeleeHit(target.transform.position + Vector3.up * 0.5f, audioVolume * 0.65f);
            PublishCombatEvent(GameplayEventKind.PlayerMeleeHit, "player_melee_hit", target.transform.position, hasSpear ? "spear" : "unarmed", ResolveCreatureId(target), damage);
            return true;
        }

        private bool TryFireBow(Vector3 direction)
        {
            if (!TrySpendStamina(bowStaminaCost))
            {
                PublishCombatEvent(GameplayEventKind.PlayerBowFired, "player_bow_no_stamina", transform.position, "bow", "stamina", 0f);
                return false;
            }

            cooldownRemaining = Mathf.Max(0.05f, bowCooldownSeconds);
            Vector3 spawnPosition = GetAttackOrigin() + direction.normalized * 0.8f;
            GameObject projectile = new GameObject("ArrowProjectile");
            projectile.transform.position = spawnPosition;
            projectile.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            ArrowProjectileRuntime arrow = projectile.AddComponent<ArrowProjectileRuntime>();
            arrow.Configure(gameObject, direction, bowDamage, arrowSpeed, arrowLifetimeSeconds, arrowMaxRange, arrowHitRadius, creatureMask, bowHitVoiceClips, audioVolume);
            CombatFxSpawner.SpawnMuzzleFlash(spawnPosition, direction);
            PlayRandomClip(bowVoiceClips, spawnPosition);
            ProceduralCombatAudio.PlayBowRelease(spawnPosition, audioVolume * 0.60f);
            CreaturePlayerAwarenessBehavior.NotifyNearby(transform.position, transform, Mathf.Max(14f, arrowMaxRange * 0.45f), 0.90f, "player_bow_noise");

            PublishCombatEvent(GameplayEventKind.PlayerBowFired, "player_bow_fired", spawnPosition, "bow", "projectile", bowDamage);
            return true;
        }

        private CreatureHealthRuntime FindMeleeTarget(Vector3 origin, Vector3 direction, float range, float arcDegrees)
        {
            float effectiveRange = Mathf.Max(0.5f, range + meleeForgivenessRange);
            Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up * 0.75f, effectiveRange, creatureMask, QueryTriggerInteraction.Collide);
            CreatureHealthRuntime best = null;
            CreatureHealthRuntime nearestFallback = null;
            float bestScore = float.PositiveInfinity;
            float nearestScore = float.PositiveInfinity;
            float halfArc = Mathf.Max(1f, arcDegrees * 0.5f);

            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                CreatureHealthRuntime health = hit.GetComponentInParent<CreatureHealthRuntime>();
                ConsiderMeleeTarget(health, origin, direction, effectiveRange, halfArc, ref best, ref bestScore, ref nearestFallback, ref nearestScore);
            }

            if (best == null && nearestFallback == null)
            {
                CreatureHealthRuntime[] allCreatures = Object.FindObjectsByType<CreatureHealthRuntime>(FindObjectsInactive.Exclude);
                foreach (CreatureHealthRuntime health in allCreatures)
                {
                    ConsiderMeleeTarget(health, origin, direction, effectiveRange, halfArc, ref best, ref bestScore, ref nearestFallback, ref nearestScore);
                }
            }

            if (best != null) return best;
            if (nearestFallback != null) return nearestFallback;

            // Last-resort prototype fallback: if the player is physically close to a creature,
            // deal damage even when collider/layer/aim arc checks missed. This prevents the
            // most frustrating failure mode: slash effect appears but the animal cannot be harmed.
            return FindNearestCreatureByDistance(Mathf.Max(range, prototypeGuaranteedMeleeHitRange));
        }

        private CreatureHealthRuntime FindNearestCreatureByDistance(float range)
        {
            CreatureHealthRuntime[] allCreatures = Object.FindObjectsByType<CreatureHealthRuntime>(FindObjectsInactive.Exclude);
            CreatureHealthRuntime best = null;
            float bestDistance = float.PositiveInfinity;
            Vector3 playerPos = transform.position;

            foreach (CreatureHealthRuntime health in allCreatures)
            {
                if (health == null || health.IsDead || health.transform == transform || health.transform.IsChildOf(transform))
                {
                    continue;
                }

                Vector3 delta = health.transform.position - playerPos;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance <= range && distance < bestDistance)
                {
                    best = health;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void ConsiderMeleeTarget(CreatureHealthRuntime health, Vector3 origin, Vector3 direction, float range, float halfArc, ref CreatureHealthRuntime best, ref float bestScore, ref CreatureHealthRuntime nearestFallback, ref float nearestScore)
        {
            if (health == null || health.IsDead)
            {
                return;
            }

            if (health.transform == transform || health.transform.IsChildOf(transform))
            {
                return;
            }

            Vector3 targetPoint = health.transform.position + Vector3.up * 0.55f;
            Vector3 toTarget = targetPoint - origin;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance > range)
            {
                return;
            }

            if (distance < nearestScore)
            {
                nearestFallback = health;
                nearestScore = distance;
            }

            if (distance <= Mathf.Max(0.45f, meleeForgivenessRange))
            {
                float closeScore = distance * 0.2f;
                if (closeScore < bestScore)
                {
                    best = health;
                    bestScore = closeScore;
                }
                return;
            }

            if (toTarget.sqrMagnitude <= 0.001f)
            {
                return;
            }

            float angle = Vector3.Angle(direction, toTarget.normalized);
            if (angle <= halfArc)
            {
                float score = angle * 0.7f + distance * 0.3f;
                if (score < bestScore)
                {
                    best = health;
                    bestScore = score;
                }
            }
        }

        private Vector3 ResolveAimDirection()
        {
            Vector3 origin = GetAttackOrigin();
            UnityEngine.Camera camera = UnityEngine.Camera.main != null ? UnityEngine.Camera.main : Object.FindAnyObjectByType<UnityEngine.Camera>();
            Vector2 screenPosition = Vector2.zero;
            bool hasScreenPosition = false;

            if (Mouse.current != null)
            {
                screenPosition = Mouse.current.position.ReadValue();
                hasScreenPosition = true;
            }
            else if (inputReader != null && inputReader.LookScreenPosition.sqrMagnitude > 0.001f)
            {
                screenPosition = inputReader.LookScreenPosition;
                hasScreenPosition = true;
            }

            if (camera != null && hasScreenPosition)
            {
                Ray ray = camera.ScreenPointToRay(screenPosition);
                Plane plane = new Plane(Vector3.up, new Vector3(0f, origin.y, 0f));
                if (plane.Raycast(ray, out float enter))
                {
                    Vector3 target = ray.GetPoint(enter);
                    Vector3 direction = target - origin;
                    direction.y = 0f;
                    if (direction.sqrMagnitude > 0.001f)
                    {
                        return direction.normalized;
                    }
                }
            }

            Vector3 fallback = transform.forward;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.001f ? fallback.normalized : Vector3.forward;
        }

        private Vector3 GetAttackOrigin()
        {
            Transform origin = attackOrigin != null ? attackOrigin : transform;
            return origin.position + Vector3.up * Mathf.Max(0f, attackHeightOffset);
        }

        private bool TrySpendStamina(float amount)
        {
            if (amount <= 0f || survivalRuntime == null || survivalRuntime.Stats == null)
            {
                return true;
            }

            return survivalRuntime.Stats.SpendStamina(amount);
        }

        private bool HasItem(string itemId)
        {
            return inventoryRuntime != null && inventoryRuntime.Inventory != null && inventoryRuntime.Inventory.HasItem(itemId, 1);
        }

        private void ResolveReferences()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (inventoryRuntime == null) inventoryRuntime = GetComponent<PlayerInventoryRuntime>();
            if (survivalRuntime == null) survivalRuntime = GetComponent<PlayerSurvivalRuntime>();
            if (attackOrigin == null) attackOrigin = transform;
        }

        private void ApplyCombatAudioProfile()
        {
            if (combatAudioProfile == null)
            {
                return;
            }

            attackVoiceClips = combatAudioProfile.attackVoiceClips;
#pragma warning disable CS0618
            meleeHitVoiceClips = combatAudioProfile.meleeHitVoiceClips != null && combatAudioProfile.meleeHitVoiceClips.Length > 0 ? combatAudioProfile.meleeHitVoiceClips : combatAudioProfile.hitVoiceClips;
#pragma warning restore CS0618
            bowVoiceClips = combatAudioProfile.bowVoiceClips;
            bowHitVoiceClips = combatAudioProfile.bowHitVoiceClips;
        }

        private static string ResolveCreatureId(CreatureHealthRuntime health)
        {
            CreatureAgentView view = health != null ? health.GetComponent<CreatureAgentView>() : null;
            return view != null ? view.CreatureId : "creature";
        }

        private static void PublishCombatEvent(GameplayEventKind kind, string message, Vector3 position, string weaponId, string targetKind, float amount)
        {
            GameEventBus.PublishCreatureEvent(kind, position, "default", "player", targetKind, amount: amount, message: $"{message}:{weaponId}");
        }

        private void PlayRandomClip(AudioClip[] clips, Vector3 position)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

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

        private void PlaySpatialAudio(AudioClip clip, Vector3 position, float volume)
        {
            GameObject emitter = new GameObject($"PlayerCombatAudio_{clip.name}");
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

#if UNITY_EDITOR
        private void AutoAssignCombatAudio()
        {
            string basePath = "Assets/_Project/Audio/SFX/combat/";
            string packagedBasePath = "Assets/apex_shift_audio_assets_v3_realistic/apex_shift_audio_assets_v3_realistic/Assets/_Project/Audio/SFX/combat/";
            attackVoiceClips = LoadClips(new[]
            {
                basePath + "player/spear_swing_real_01.wav",
                basePath + "player/spear_swing_real_02.wav",
                basePath + "player/spear_swing_real_03.wav",
                basePath + "player/spear_swing_real_04.wav",
                packagedBasePath + "player/spear_swing_real_01.wav",
                packagedBasePath + "player/spear_swing_real_02.wav",
                packagedBasePath + "player/spear_swing_real_03.wav",
                packagedBasePath + "player/spear_swing_real_04.wav",
                basePath + "player/bow_release_real_01.wav",
                basePath + "player/bow_release_real_02.wav",
                basePath + "player/bow_release_real_03.wav",
                basePath + "player/bow_release_real_04.wav"
            }, attackVoiceClips);

            meleeHitVoiceClips = LoadClips(new[]
            {
                basePath + "player/spear_hit_flesh_real_01.wav",
                basePath + "player/spear_hit_flesh_real_02.wav",
                basePath + "player/spear_hit_flesh_real_03.wav",
                basePath + "player/spear_hit_flesh_real_04.wav",
                packagedBasePath + "player/spear_hit_flesh_real_01.wav",
                packagedBasePath + "player/spear_hit_flesh_real_02.wav",
                packagedBasePath + "player/spear_hit_flesh_real_03.wav",
                packagedBasePath + "player/spear_hit_flesh_real_04.wav",
                basePath + "player/arrow_hit_flesh_real_01.wav"
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
                basePath + "player/arrow_fly_real_04.wav",
                packagedBasePath + "player/bow_release_real_01.wav",
                packagedBasePath + "player/bow_release_real_02.wav",
                packagedBasePath + "player/bow_release_real_03.wav",
                packagedBasePath + "player/bow_release_real_04.wav",
                packagedBasePath + "player/arrow_fly_real_01.wav",
                packagedBasePath + "player/arrow_fly_real_02.wav"
            }, bowVoiceClips);

            bowHitVoiceClips = LoadClips(new[]
            {
                basePath + "player/arrow_hit_flesh_real_01.wav",
                basePath + "player/arrow_hit_wood_real_01.wav",
                basePath + "player/arrow_hit_stone_real_01.wav",
                packagedBasePath + "player/arrow_hit_flesh_real_01.wav",
                packagedBasePath + "player/arrow_hit_wood_real_01.wav",
                packagedBasePath + "player/arrow_hit_stone_real_01.wav"
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
