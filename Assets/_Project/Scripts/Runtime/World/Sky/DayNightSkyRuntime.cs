using ApexShift.Runtime.DayNight;
using UnityEngine;

namespace ApexShift.Runtime.World.Sky
{
    /// <summary>
    /// Drives a procedural sky, directional sun/moon light, ambient light and fog
    /// based on the current time-of-day provided by DayNightRuntime.
    ///
    /// Attach to any persistent GameObject in the scene (WorldGeneratorRuntime creates
    /// one automatically).  Requires Unity's built-in Procedural sky material in
    /// RenderSettings.skyboxMaterial (assigned via the inspector or auto-created at runtime).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DayNightSkyRuntime : MonoBehaviour
    {
        // ─── Sun / Moon ──────────────────────────────────────────────────────────

        [Header("Sun")]
        [SerializeField] private Light sunLight;
        [SerializeField, Range(0f, 8f)] private float sunIntensityDay = 1.15f;
        [SerializeField, Range(0f, 8f)] private float sunIntensityGoldenHour = 0.7f;
        [SerializeField, Range(0f, 8f)] private float sunIntensityNight = 0f;
        [SerializeField] private Gradient sunColorGradient;

        [Header("Moon")]
        [SerializeField] private Light moonLight;
        [SerializeField, Range(0f, 2f)] private float moonIntensityNight = 0.18f;

        // ─── Sky colours ─────────────────────────────────────────────────────────

        [Header("Sky colours")]
        [SerializeField] private Gradient skyTintGradient;
        [SerializeField] private Gradient groundColorGradient;
        [SerializeField] private Gradient equatorColorGradient;

        // ─── Fog ─────────────────────────────────────────────────────────────────

        [Header("Fog")]
        [SerializeField] private bool controlFog = true;
        [SerializeField] private Gradient fogColorGradient;
        [SerializeField] private AnimationCurve fogDensityCurve;
        [SerializeField, Range(0f, 0.1f)] private float fogDensityDay = 0.008f;
        [SerializeField, Range(0f, 0.1f)] private float fogDensityNight = 0.018f;

        // ─── Skybox material ─────────────────────────────────────────────────────

        [Header("Skybox")]
        [SerializeField] private Material skyboxMaterial;

        // ─── Internal ────────────────────────────────────────────────────────────

        private DayNightRuntime dayNight;
        private static readonly int AtmosphereThicknessId = Shader.PropertyToID("_AtmosphereThickness");
        private static readonly int SkyTintId              = Shader.PropertyToID("_SkyTint");
        private static readonly int GroundColorId          = Shader.PropertyToID("_GroundColor");
        private static readonly int SunSizeId              = Shader.PropertyToID("_SunSize");
        private static readonly int SunSizeConvergenceId   = Shader.PropertyToID("_SunSizeConvergence");
        private static readonly int ExposureId             = Shader.PropertyToID("_Exposure");

        // ─── Lifecycle ───────────────────────────────────────────────────────────

        private void Awake()
        {
            EnsureDefaultGradients();
        }

        private void Start()
        {
            dayNight = DayNightRuntime.Active;

            EnsureSkyboxMaterial();
            EnsureSunLight();
            EnsureMoonLight();

            if (dayNight != null)
            {
                Apply(dayNight.TimeOfDay01);
            }
        }

        private void Update()
        {
            if (dayNight == null)
            {
                dayNight = DayNightRuntime.Active;
                if (dayNight == null) return;
            }

            Apply(dayNight.TimeOfDay01);
        }

        // ─── Public API ──────────────────────────────────────────────────────────

        public void Configure(Light sun, Light moon, Material skybox)
        {
            sunLight = sun;
            moonLight = moon;
            skyboxMaterial = skybox;
        }

        // ─── Core logic ──────────────────────────────────────────────────────────

        /// <param name="t">Normalised time of day 0..1 (midnight=0, noon≈0.5)</param>
        private void Apply(float t)
        {
            float sunAngle = (t - 0.25f) * 360f; // 0 = sunrise, 180 = sunset
            float moonAngle = sunAngle + 180f;

            // ── Sun rotation & intensity ──────────────────────────────────────
            if (sunLight != null)
            {
                sunLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);
                sunLight.color = sunColorGradient.Evaluate(t);
                sunLight.intensity = EvaluateSunIntensity(t);
                sunLight.enabled = true;
            }

            // ── Moon rotation & intensity ─────────────────────────────────────
            if (moonLight != null)
            {
                moonLight.transform.rotation = Quaternion.Euler(moonAngle, -30f, 0f);
                float nightT = Mathf.Clamp01(1f - Mathf.Abs(t - 0.0f) * 4f); // brightest at midnight
                moonLight.intensity = moonIntensityNight * nightT;
                moonLight.enabled = moonLight.intensity > 0.001f;
            }

            // ── Ambient light ─────────────────────────────────────────────────
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor     = skyTintGradient.Evaluate(t);
            RenderSettings.ambientEquatorColor = equatorColorGradient.Evaluate(t);
            RenderSettings.ambientGroundColor  = groundColorGradient.Evaluate(t);

            // ── Fog ───────────────────────────────────────────────────────────
            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = fogColorGradient.Evaluate(t);
                float fogT = fogDensityCurve != null ? fogDensityCurve.Evaluate(t) : t;
                RenderSettings.fogDensity = Mathf.Lerp(fogDensityDay, fogDensityNight, fogT);
            }

            // ── Skybox material properties ────────────────────────────────────
            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetColor(SkyTintId, skyTintGradient.Evaluate(t));
                skyboxMaterial.SetColor(GroundColorId, groundColorGradient.Evaluate(t));
                skyboxMaterial.SetFloat(AtmosphereThicknessId, Mathf.Lerp(0.85f, 1.1f, t));
                // Sun disc: small and bright at noon, large & dim at horizon
                float horizonBlend = Mathf.Abs(Mathf.Sin(sunAngle * Mathf.Deg2Rad));
                skyboxMaterial.SetFloat(SunSizeId, Mathf.Lerp(0.045f, 0.02f, horizonBlend));
                skyboxMaterial.SetFloat(SunSizeConvergenceId, Mathf.Lerp(5f, 10f, horizonBlend));
                skyboxMaterial.SetFloat(ExposureId, Mathf.Lerp(0.4f, 1.3f, horizonBlend));
                RenderSettings.skybox = skyboxMaterial;
                DynamicGI.UpdateEnvironment();
            }
        }

        private float EvaluateSunIntensity(float t)
        {
            // Night: 0..0.2 and 0.8..1.0
            // Golden hour: 0.2..0.3 and 0.7..0.8
            // Day: 0.3..0.7
            if (t < 0.2f || t > 0.8f)
                return sunIntensityNight;
            if (t < 0.3f)
                return Mathf.Lerp(sunIntensityNight, sunIntensityGoldenHour, (t - 0.2f) / 0.1f);
            if (t > 0.7f)
                return Mathf.Lerp(sunIntensityGoldenHour, sunIntensityNight, (t - 0.7f) / 0.1f);
            if (t < 0.35f)
                return Mathf.Lerp(sunIntensityGoldenHour, sunIntensityDay, (t - 0.3f) / 0.05f);
            if (t > 0.65f)
                return Mathf.Lerp(sunIntensityDay, sunIntensityGoldenHour, (t - 0.65f) / 0.05f);
            return sunIntensityDay;
        }

        // ─── Setup helpers ───────────────────────────────────────────────────────

        private void EnsureSkyboxMaterial()
        {
            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
                return;
            }

            // Try to find Unity's built-in Procedural skybox shader
            Shader proceduralShader = Shader.Find("Skybox/Procedural");
            if (proceduralShader != null)
            {
                skyboxMaterial = new Material(proceduralShader);
                skyboxMaterial.name = "DayNightProceduralSky";
                skyboxMaterial.SetFloat(SunSizeId, 0.04f);
                skyboxMaterial.SetFloat(SunSizeConvergenceId, 8f);
                skyboxMaterial.SetFloat(AtmosphereThicknessId, 1.0f);
                skyboxMaterial.SetFloat(ExposureId, 1.0f);
                RenderSettings.skybox = skyboxMaterial;
            }
            else
            {
                // Fallback: just control ambient/fog without a skybox material
                Debug.LogWarning("[DayNightSky] Skybox/Procedural shader not found. Sky colour will be applied via ambient only.");
            }
        }

        private void EnsureSunLight()
        {
            if (sunLight != null) return;

            // Reuse any existing directional light in the scene
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
            foreach (Light lt in lights)
            {
                if (lt.type == LightType.Directional)
                {
                    sunLight = lt;
                    sunLight.gameObject.name = "Sun";
                    return;
                }
            }

            // Create one
            GameObject go = new GameObject("Sun");
            go.transform.SetParent(transform);
            sunLight = go.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.shadows = LightShadows.Soft;
        }

        private void EnsureMoonLight()
        {
            if (moonLight != null) return;

            GameObject go = new GameObject("Moon");
            go.transform.SetParent(transform);
            moonLight = go.AddComponent<Light>();
            moonLight.type = LightType.Directional;
            moonLight.color = new Color(0.6f, 0.65f, 0.85f);
            moonLight.shadows = LightShadows.None;
            moonLight.intensity = 0f;
        }

        // ─── Default gradient factory ────────────────────────────────────────────

        private void EnsureDefaultGradients()
        {
            if (sunColorGradient == null || sunColorGradient.colorKeys.Length == 0)
                sunColorGradient = BuildSunColorGradient();

            if (skyTintGradient == null || skyTintGradient.colorKeys.Length == 0)
                skyTintGradient = BuildSkyTintGradient();

            if (groundColorGradient == null || groundColorGradient.colorKeys.Length == 0)
                groundColorGradient = BuildGroundColorGradient();

            if (equatorColorGradient == null || equatorColorGradient.colorKeys.Length == 0)
                equatorColorGradient = BuildEquatorColorGradient();

            if (fogColorGradient == null || fogColorGradient.colorKeys.Length == 0)
                fogColorGradient = BuildFogColorGradient();

            if (fogDensityCurve == null || fogDensityCurve.length == 0)
                fogDensityCurve = BuildFogDensityCurve();
        }

        // t=0/1 midnight, t=0.25 sunrise, t=0.5 noon, t=0.75 sunset

        private static Gradient BuildSunColorGradient()
        {
            Gradient g = new Gradient();
            g.mode = GradientMode.Blend;
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0.00f), // midnight
                    new GradientColorKey(new Color(1.0f,  0.55f, 0.15f), 0.25f), // sunrise
                    new GradientColorKey(new Color(1.0f,  0.92f, 0.70f), 0.32f), // early morning
                    new GradientColorKey(new Color(1.0f,  0.97f, 0.90f), 0.50f), // noon
                    new GradientColorKey(new Color(1.0f,  0.92f, 0.70f), 0.68f), // afternoon
                    new GradientColorKey(new Color(1.0f,  0.50f, 0.10f), 0.75f), // sunset
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 0.82f), // after sunset
                    new GradientColorKey(new Color(0.05f, 0.05f, 0.15f), 1.00f), // midnight
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
            return g;
        }

        private static Gradient BuildSkyTintGradient()
        {
            Gradient g = new Gradient();
            g.mode = GradientMode.Blend;
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.02f, 0.03f, 0.12f), 0.00f), // midnight
                    new GradientColorKey(new Color(0.02f, 0.03f, 0.12f), 0.20f), // night
                    new GradientColorKey(new Color(0.55f, 0.30f, 0.15f), 0.25f), // sunrise
                    new GradientColorKey(new Color(0.38f, 0.62f, 0.85f), 0.35f), // morning
                    new GradientColorKey(new Color(0.26f, 0.52f, 0.80f), 0.50f), // noon
                    new GradientColorKey(new Color(0.38f, 0.62f, 0.85f), 0.65f), // afternoon
                    new GradientColorKey(new Color(0.55f, 0.28f, 0.12f), 0.75f), // sunset
                    new GradientColorKey(new Color(0.02f, 0.03f, 0.12f), 0.82f), // dusk→midnight
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static Gradient BuildGroundColorGradient()
        {
            Gradient g = new Gradient();
            g.mode = GradientMode.Blend;
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.01f, 0.01f, 0.05f), 0.00f),
                    new GradientColorKey(new Color(0.20f, 0.15f, 0.08f), 0.25f),
                    new GradientColorKey(new Color(0.22f, 0.20f, 0.15f), 0.40f),
                    new GradientColorKey(new Color(0.28f, 0.25f, 0.18f), 0.50f),
                    new GradientColorKey(new Color(0.22f, 0.20f, 0.15f), 0.60f),
                    new GradientColorKey(new Color(0.20f, 0.12f, 0.06f), 0.75f),
                    new GradientColorKey(new Color(0.01f, 0.01f, 0.05f), 0.82f),
                    new GradientColorKey(new Color(0.01f, 0.01f, 0.05f), 1.00f),
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static Gradient BuildEquatorColorGradient()
        {
            Gradient g = new Gradient();
            g.mode = GradientMode.Blend;
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.00f),
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.20f),
                    new GradientColorKey(new Color(0.65f, 0.42f, 0.20f), 0.25f),
                    new GradientColorKey(new Color(0.50f, 0.65f, 0.75f), 0.35f),
                    new GradientColorKey(new Color(0.40f, 0.58f, 0.72f), 0.50f),
                    new GradientColorKey(new Color(0.50f, 0.65f, 0.75f), 0.65f),
                    new GradientColorKey(new Color(0.65f, 0.38f, 0.15f), 0.75f),
                    new GradientColorKey(new Color(0.02f, 0.02f, 0.08f), 0.82f),
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static Gradient BuildFogColorGradient()
        {
            Gradient g = new Gradient();
            g.mode = GradientMode.Blend;
            g.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.05f, 0.06f, 0.14f), 0.00f),
                    new GradientColorKey(new Color(0.05f, 0.06f, 0.14f), 0.20f),
                    new GradientColorKey(new Color(0.72f, 0.52f, 0.38f), 0.25f),
                    new GradientColorKey(new Color(0.68f, 0.76f, 0.84f), 0.35f),
                    new GradientColorKey(new Color(0.62f, 0.74f, 0.84f), 0.50f),
                    new GradientColorKey(new Color(0.68f, 0.76f, 0.84f), 0.65f),
                    new GradientColorKey(new Color(0.72f, 0.44f, 0.28f), 0.75f),
                    new GradientColorKey(new Color(0.05f, 0.06f, 0.14f), 0.82f),
                },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        private static AnimationCurve BuildFogDensityCurve()
        {
            // Dense at night, lighter at day
            return new AnimationCurve(
                new Keyframe(0.00f, 1.0f),
                new Keyframe(0.20f, 1.0f),
                new Keyframe(0.30f, 0.2f),
                new Keyframe(0.50f, 0.0f),
                new Keyframe(0.70f, 0.2f),
                new Keyframe(0.80f, 1.0f),
                new Keyframe(1.00f, 1.0f));
        }
    }
}
