using System;
using UnityEngine;

namespace ApexShift.Runtime.Audio
{
    /// <summary>
    /// Assigns ambient audio clips to a specific biome + time-of-day combination.
    /// Assign one profile per biome via BiomeDefinitionAsset or directly to AmbientSoundController.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BiomeAmbientProfile",
        menuName = "Apex Shift/Audio/Biome Ambient Profile",
        order = 20)]
    public sealed class BiomeAmbientProfile : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Biome id this profile belongs to (e.g. westwood, stoneback_ridge). Leave empty to use as default / fallback.")]
        [SerializeField] private string biomeId = string.Empty;

        [Header("Day ambients")]
        [Tooltip("Played during daytime (t=0.30..0.70).")]
        [SerializeField] private AudioClip[] dayClips = Array.Empty<AudioClip>();

        [Header("Night ambients")]
        [Tooltip("Played during night (t>0.80 or t<0.20).")]
        [SerializeField] private AudioClip[] nightClips = Array.Empty<AudioClip>();

        [Header("Dawn / Dusk ambients  (optional)")]
        [Tooltip("Played during golden hour transitions. Falls back to day clips when empty.")]
        [SerializeField] private AudioClip[] dawnDuskClips = Array.Empty<AudioClip>();

        [Header("Rain ambients  (optional)")]
        [Tooltip("Played when weather layer reports rain. Falls back to day/night clips when empty.")]
        [SerializeField] private AudioClip[] rainClips = Array.Empty<AudioClip>();

        [Header("Volume")]
        [SerializeField, Range(0f, 1f)] private float volumeMultiplier = 1f;

        // ── Properties ──────────────────────────────────────────────────────────

        public string BiomeId => biomeId;
        public float VolumeMultiplier => Mathf.Clamp01(volumeMultiplier);

        /// <summary>Returns the appropriate clip list for the given ambient context.</summary>
        public AudioClip[] GetClips(AmbientContext context)
        {
            if (context.IsRaining && rainClips != null && rainClips.Length > 0)
                return rainClips;

            if (context.IsDawnOrDusk && dawnDuskClips != null && dawnDuskClips.Length > 0)
                return dawnDuskClips;

            if (context.IsNight && nightClips != null && nightClips.Length > 0)
                return nightClips;

            return dayClips != null && dayClips.Length > 0 ? dayClips : Array.Empty<AudioClip>();
        }

        public bool HasAnyClips()
        {
            return (dayClips != null && dayClips.Length > 0)
                || (nightClips != null && nightClips.Length > 0)
                || (dawnDuskClips != null && dawnDuskClips.Length > 0)
                || (rainClips != null && rainClips.Length > 0);
        }
    }

    /// <summary>Snapshot of the current ambient context used to select the right clip set.</summary>
    public readonly struct AmbientContext : IEquatable<AmbientContext>
    {
        public readonly string BiomeId;
        public readonly bool IsNight;
        public readonly bool IsDawnOrDusk;
        public readonly bool IsRaining;

        public AmbientContext(string biomeId, bool isNight, bool isDawnOrDusk, bool isRaining)
        {
            BiomeId = biomeId ?? string.Empty;
            IsNight = isNight;
            IsDawnOrDusk = isDawnOrDusk;
            IsRaining = isRaining;
        }

        public bool Equals(AmbientContext other) =>
            string.Equals(BiomeId, other.BiomeId, StringComparison.OrdinalIgnoreCase)
            && IsNight == other.IsNight
            && IsDawnOrDusk == other.IsDawnOrDusk
            && IsRaining == other.IsRaining;

        public override bool Equals(object obj) => obj is AmbientContext other && Equals(other);
        public override int GetHashCode() =>
            HashCode.Combine(BiomeId?.ToLowerInvariant(), IsNight, IsDawnOrDusk, IsRaining);
    }
}
