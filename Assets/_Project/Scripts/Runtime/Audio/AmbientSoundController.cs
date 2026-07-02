using System.Collections.Generic;
using ApexShift.Runtime.DayNight;
using ApexShift.Runtime.World.Query;
using UnityEngine;

namespace ApexShift.Runtime.Audio
{
    /// <summary>
    /// Bridges the day/night cycle and biome system to <see cref="AmbientMusicRuntime"/>.
    ///
    /// Every <see cref="biomeCheckIntervalSeconds"/> the controller:
    ///   1. queries the player's current biome via <see cref="WorldQueryRuntime"/>,
    ///   2. evaluates the time-of-day phase (day / dusk-dawn / night) from <see cref="DayNightRuntime"/>,
    ///   3. checks a rain flag that can be set externally (future weather system),
    ///   4. looks up the matching <see cref="BiomeAmbientProfile"/> and swaps the clip playlist
    ///      on <see cref="AmbientMusicRuntime"/> if the context changed.
    ///
    /// Profiles are matched by biome id.  A <see cref="defaultProfile"/> is used whenever no
    /// biome-specific profile is registered.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AmbientSoundController : MonoBehaviour
    {
        [Header("Profiles")]
        [Tooltip("Fallback profile used when no biome-specific profile matches.")]
        [SerializeField] private BiomeAmbientProfile defaultProfile;

        [Tooltip("Biome-specific profiles. Priority over defaultProfile.")]
        [SerializeField] private List<BiomeAmbientProfile> biomeProfiles = new List<BiomeAmbientProfile>();

        [Header("Timing")]
        [Tooltip("How often (seconds) to re-evaluate which biome the player is in.")]
        [SerializeField, Range(0.5f, 30f)] private float biomeCheckIntervalSeconds = 4f;

        [Header("Time-of-day thresholds  (normalised 0..1)")]
        [SerializeField, Range(0f, 0.5f)] private float dawnStartT   = 0.20f;
        [SerializeField, Range(0f, 0.5f)] private float dayStartT    = 0.30f;
        [SerializeField, Range(0.5f, 1f)] private float duskStartT   = 0.68f;
        [SerializeField, Range(0.5f, 1f)] private float nightStartT  = 0.80f;

        // ── State ────────────────────────────────────────────────────────────────

        private AmbientMusicRuntime ambientMusic;
        private DayNightRuntime dayNight;
        private WorldQueryRuntime worldQuery;
        private Transform playerTransform;

        private AmbientContext lastContext;
        private float checkTimer;
        private bool raining;

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Call this from a future weather system to switch rain ambients.</summary>
        public void SetRaining(bool value)
        {
            raining = value;
        }

        public void RegisterProfile(BiomeAmbientProfile profile)
        {
            if (profile == null) return;
            if (!biomeProfiles.Contains(profile))
                biomeProfiles.Add(profile);
        }

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Start()
        {
            ambientMusic = AmbientMusicRuntime.Active;
            dayNight     = DayNightRuntime.Active;
            worldQuery   = WorldQueryRuntime.Active;

            // Force first evaluation immediately
            checkTimer = biomeCheckIntervalSeconds;
        }

        private void Update()
        {
            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = biomeCheckIntervalSeconds;

            Evaluate();
        }

        // ── Core logic ───────────────────────────────────────────────────────────

        private void Evaluate()
        {
            if (ambientMusic == null)
            {
                ambientMusic = AmbientMusicRuntime.Active;
                if (ambientMusic == null) return;
            }

            string biomeId = ResolvePlayerBiome();
            AmbientContext ctx = BuildContext(biomeId);

            if (ctx.Equals(lastContext)) return;
            lastContext = ctx;

            BiomeAmbientProfile profile = ResolveProfile(ctx.BiomeId);
            if (profile == null) return;

            AudioClip[] clips = profile.GetClips(ctx);
            if (clips == null || clips.Length == 0) return;

            ambientMusic.SetClips(clips);
            ambientMusic.SetVolume(profile.VolumeMultiplier);
            ambientMusic.Play();

            Debug.Log($"[AmbientSoundController] Switched to biome={ctx.BiomeId} night={ctx.IsNight} dawnDusk={ctx.IsDawnOrDusk} rain={ctx.IsRaining} → {clips.Length} clip(s) from {profile.name}");
        }

        private string ResolvePlayerBiome()
        {
            if (worldQuery == null)
                worldQuery = WorldQueryRuntime.Active;

            if (worldQuery == null) return "default";

            if (playerTransform == null)
            {
                GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo != null) playerTransform = playerGo.transform;
            }

            Vector3 pos = playerTransform != null ? playerTransform.position : Vector3.zero;
            return worldQuery.GetBiomeIdForPosition(pos);
        }

        private AmbientContext BuildContext(string biomeId)
        {
            float t = dayNight != null ? dayNight.TimeOfDay01 : 0.5f;

            bool isNight      = t < dawnStartT || t >= nightStartT;
            bool isDawnOrDusk = !isNight && (t < dayStartT || t >= duskStartT);

            return new AmbientContext(biomeId, isNight, isDawnOrDusk, raining);
        }

        private BiomeAmbientProfile ResolveProfile(string biomeId)
        {
            // Exact match first
            foreach (BiomeAmbientProfile p in biomeProfiles)
            {
                if (p != null && string.Equals(p.BiomeId, biomeId, System.StringComparison.OrdinalIgnoreCase))
                    return p;
            }

            // Prefix / partial match (e.g. "westwood_rain" → "westwood")
            string normalizedBiome = (biomeId ?? string.Empty).ToLowerInvariant();
            foreach (BiomeAmbientProfile p in biomeProfiles)
            {
                if (p != null && !string.IsNullOrWhiteSpace(p.BiomeId)
                    && normalizedBiome.StartsWith(p.BiomeId.ToLowerInvariant()))
                    return p;
            }

            return defaultProfile;
        }
    }
}
