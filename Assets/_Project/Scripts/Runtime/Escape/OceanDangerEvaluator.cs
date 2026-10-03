using System;
using UnityEngine;

namespace ApexShift.Runtime.Escape
{
    [Serializable]
    public sealed class OceanDangerProfile
    {
        public float dangerStartDistance = 12f;
        public float hardFailureDistance = 38f;
        public float damageStartDanger = 0.25f;
        public float maxDamagePerSecond = 0.18f;
    }

    public readonly struct OceanDangerSample
    {
        public readonly float Danger01;
        public readonly float Durability01;
        public readonly bool ShouldFail;
        public OceanDangerSample(float danger, float durability, bool shouldFail)
        { Danger01 = danger; Durability01 = durability; ShouldFail = shouldFail; }
    }

    /// <summary>Deterministic physical distance model; visual wobble never influences failure.</summary>
    public static class OceanDangerEvaluator
    {
        public static OceanDangerSample Evaluate(OceanDangerProfile profile, float distanceToCoast,
            float durability, float deltaTime)
        {
            profile = profile ?? new OceanDangerProfile();
            float start = Mathf.Max(0f, profile.dangerStartDistance);
            float end = Mathf.Max(start + 0.01f, profile.hardFailureDistance);
            float danger = Mathf.InverseLerp(start, end, Mathf.Max(0f, distanceToCoast));
            float damage = Mathf.InverseLerp(Mathf.Clamp01(profile.damageStartDanger), 1f, danger);
            float remaining = Mathf.Clamp01(durability - damage * damage
                * Mathf.Max(0f, profile.maxDamagePerSecond) * Mathf.Max(0f, deltaTime));
            return new OceanDangerSample(danger, remaining, remaining <= 0f || distanceToCoast >= end);
        }
    }
}
