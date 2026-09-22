using System;
using UnityEngine;

namespace ApexShift.Runtime.World.Topography
{
    /// <summary>Configuration for the deterministic layered island heightfield.</summary>
    [Serializable]
    public sealed class TerrainHeightfieldSettings
    {
        [Header("Macro elevation")]
        [SerializeField] private float macroFrequency = 0.012f;
        [SerializeField] private float macroAmplitude = 0.34f;

        [Header("Rolling hills")]
        [SerializeField] private float hillsFrequency = 0.028f;
        [SerializeField] private float hillsAmplitude = 0.16f;

        [Header("Ridges and valleys")]
        [SerializeField] private float ridgeFrequency = 0.052f;
        [SerializeField] private float ridgeAmplitude = 0.20f;
        [SerializeField] private float valleyFrequency = 0.020f;
        [SerializeField] private float valleyStrength = 0.16f;

        [Header("Surface detail")]
        [SerializeField] private float detailFrequency = 0.11f;
        [SerializeField] private float detailAmplitude = 0.035f;

        [Header("Domain warp")]
        [SerializeField] private float warpFrequency = 0.018f;
        [SerializeField] private float warpStrength = 8f;

        [Header("Elevation")]
        [SerializeField] private float minimumHeight = -0.20f;
        [SerializeField] private float maximumHeight = 0.95f;

        [Header("Safe start")]
        [SerializeField] private float spawnFlattenInnerRadius = 10f;
        [SerializeField] private float spawnFlattenOuterRadius = 26f;
        [SerializeField] private float spawnTargetElevation = 0.03f;

        [Header("Spawn slope thresholds")]
        [SerializeField] private float playerSafeSlopeDegrees = 14f;
        [SerializeField] private float creatureSafeSlopeDegrees = 24f;
        [SerializeField] private float resourceSafeSlopeDegrees = 20f;

        public float MacroFrequency => macroFrequency;
        public float MacroAmplitude => macroAmplitude;
        public float HillsFrequency => hillsFrequency;
        public float HillsAmplitude => hillsAmplitude;
        public float RidgeFrequency => ridgeFrequency;
        public float RidgeAmplitude => ridgeAmplitude;
        public float ValleyFrequency => valleyFrequency;
        public float ValleyStrength => valleyStrength;
        public float DetailFrequency => detailFrequency;
        public float DetailAmplitude => detailAmplitude;
        public float WarpFrequency => warpFrequency;
        public float WarpStrength => warpStrength;
        public float MinimumHeight => minimumHeight;
        public float MaximumHeight => maximumHeight;
        public float SpawnFlattenInnerRadius => spawnFlattenInnerRadius;
        public float SpawnFlattenOuterRadius => spawnFlattenOuterRadius;
        public float SpawnTargetElevation => spawnTargetElevation;
        public float PlayerSafeSlopeDegrees => playerSafeSlopeDegrees;
        public float CreatureSafeSlopeDegrees => creatureSafeSlopeDegrees;
        public float ResourceSafeSlopeDegrees => resourceSafeSlopeDegrees;
    }
}
