using System;
using UnityEngine;

namespace ApexShift.Runtime.World.Biomes
{
    [Serializable]
    public sealed class BiomeFieldSettings
    {
        [SerializeField] private float moistureFrequency = 0.018f;
        [SerializeField] private float moistureWarpFrequency = 0.012f;
        [SerializeField] private float moistureWarpStrength = 10f;
        [SerializeField] private float temperatureFrequency = 0.016f;
        [SerializeField] private float temperatureNoiseInfluence = 0.65f;
        [SerializeField] private float elevationCooling = 0.35f;
        [SerializeField] private float starterInnerRadius = 12f;
        [SerializeField] private float starterOuterRadius = 28f;
        [SerializeField] private float ridgeElevationThreshold = 0.58f;
        [SerializeField] private float ridgeSlopeThreshold = 28f;
        [SerializeField] private float wetMoistureThreshold = 0.60f;
        [SerializeField] private float dryDrynessThreshold = 0.62f;
        [SerializeField] private float warmTemperatureThreshold = 0.56f;
        [SerializeField] private float decisionNoiseFrequency = 0.035f;
        [SerializeField] private float decisionNoiseStrength = 0.08f;

        public float MoistureFrequency => moistureFrequency;
        public float MoistureWarpFrequency => moistureWarpFrequency;
        public float MoistureWarpStrength => moistureWarpStrength;
        public float TemperatureFrequency => temperatureFrequency;
        public float TemperatureNoiseInfluence => temperatureNoiseInfluence;
        public float ElevationCooling => elevationCooling;
        public float StarterInnerRadius => starterInnerRadius;
        public float StarterOuterRadius => starterOuterRadius;
        public float RidgeElevationThreshold => ridgeElevationThreshold;
        public float RidgeSlopeThreshold => ridgeSlopeThreshold;
        public float WetMoistureThreshold => wetMoistureThreshold;
        public float DryDrynessThreshold => dryDrynessThreshold;
        public float WarmTemperatureThreshold => warmTemperatureThreshold;
        public float DecisionNoiseFrequency => decisionNoiseFrequency;
        public float DecisionNoiseStrength => decisionNoiseStrength;
    }
}
