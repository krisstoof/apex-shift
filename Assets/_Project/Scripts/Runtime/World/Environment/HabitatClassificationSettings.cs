using System;
using UnityEngine;

namespace ApexShift.Runtime.World.Environment
{
    [Serializable]
    public sealed class HabitatClassificationSettings
    {
        [SerializeField, Min(0f)] private float coastBandDistance = 16f;
        [SerializeField, Range(0f, 1f)] private float rockyElevationThreshold = 0.67f;
        [SerializeField, Range(0f, 90f)] private float rockySlopeThreshold = 28f;
        [SerializeField, Range(0f, 1f)] private float wetMoistureThreshold = 0.67f;
        [SerializeField, Range(0f, 1f)] private float lowlandElevationMax = 0.43f;

        public float CoastBandDistance => Mathf.Max(0f, coastBandDistance);
        public float RockyElevationThreshold => Mathf.Clamp01(rockyElevationThreshold);
        public float RockySlopeThreshold => Mathf.Clamp(rockySlopeThreshold, 0f, 90f);
        public float WetMoistureThreshold => Mathf.Clamp01(wetMoistureThreshold);
        public float LowlandElevationMax => Mathf.Clamp01(lowlandElevationMax);

        public void Configure(float coastDistance, float rockyElevation, float rockySlope, float wetMoisture, float lowlandElevation)
        {
            coastBandDistance = Mathf.Max(0f, coastDistance);
            rockyElevationThreshold = Mathf.Clamp01(rockyElevation);
            rockySlopeThreshold = Mathf.Clamp(rockySlope, 0f, 90f);
            wetMoistureThreshold = Mathf.Clamp01(wetMoisture);
            lowlandElevationMax = Mathf.Clamp01(lowlandElevation);
        }
    }
}
