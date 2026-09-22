using UnityEngine;

namespace ApexShift.Runtime.World.Biomes
{
    public sealed class BiomeClassifier
    {
        private readonly int seed;
        private readonly BiomeFieldSettings settings;

        public BiomeClassifier(int seed, BiomeFieldSettings settings)
        {
            this.seed = seed;
            this.settings = settings ?? new BiomeFieldSettings();
        }

        public string Classify(Vector3 position, BiomeEnvironmentSample sample)
        {
            float distance = new Vector2(position.x, position.z).magnitude;
            float starter = SmoothStep(settings.StarterOuterRadius, settings.StarterInnerRadius, distance);
            float slope01 = Mathf.Clamp01(sample.SlopeDegrees / 45f);
            float ridge = Mathf.Clamp01((sample.Elevation01 - settings.RidgeElevationThreshold) / Mathf.Max(0.01f, 1f - settings.RidgeElevationThreshold));
            ridge = Mathf.Max(ridge, Mathf.Clamp01((sample.SlopeDegrees - settings.RidgeSlopeThreshold) / 22f));

            float noise = (Mathf.PerlinNoise((position.x + seed * 0.13f) * settings.DecisionNoiseFrequency,
                                              (position.z - seed * 0.17f) * settings.DecisionNoiseFrequency) - 0.5f)
                          * settings.DecisionNoiseStrength;
            float starterScore = starter * 1.65f * (1f - slope01) * (1f - sample.Elevation01 * 0.85f) + 0.08f;
            float stonebackScore = ridge * 1.35f + sample.Elevation01 * 0.18f - sample.Moisture01 * 0.18f + noise;
            float southScore = sample.Moisture01 * 1.30f + (1f - sample.Elevation01) * 0.20f - slope01 * 0.28f
                              + (sample.Moisture01 >= settings.WetMoistureThreshold + 0.15f ? 0.20f : 0f) - noise;
            float westScore = sample.Moisture01 * 0.62f + (1f - Mathf.Abs(sample.Temperature01 - 0.52f) * 1.7f) * 0.42f
                              + (1f - Mathf.Abs(sample.Elevation01 - 0.42f) * 1.5f) * 0.30f - slope01 * 0.18f + noise * 0.5f;
            float redfangScore = sample.Dryness01 * 0.95f + sample.Temperature01 * 0.75f
                                 + (1f - Mathf.Abs(sample.Elevation01 - 0.48f) * 1.6f) * 0.25f
                                 + (sample.Dryness01 >= settings.DryDrynessThreshold && sample.Temperature01 >= settings.WarmTemperatureThreshold ? 0.18f : 0f)
                                 - slope01 * 0.45f - noise;

            string best = "south_thicket";
            float bestScore = southScore;
            if (starterScore > bestScore) { best = "hearth_meadow"; bestScore = starterScore; }
            if (westScore > bestScore) { best = "westwood"; bestScore = westScore; }
            if (redfangScore > bestScore) { best = "redfang_wilds"; bestScore = redfangScore; }
            if (stonebackScore > bestScore) best = "stoneback_ridge";
            return best;
        }

        private static float SmoothStep(float outer, float inner, float value)
        {
            if (outer <= inner) return value <= inner ? 1f : 0f;
            float t = Mathf.Clamp01((value - inner) / (outer - inner));
            t = t * t * (3f - 2f * t);
            return 1f - t;
        }
    }
}
