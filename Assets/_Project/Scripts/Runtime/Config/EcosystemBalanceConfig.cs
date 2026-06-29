using UnityEngine;

namespace ApexShift.Runtime.Config
{
    [CreateAssetMenu(menuName = "Apex Shift/Balance/Ecosystem Balance Config", fileName = "EcosystemBalanceConfig")]
    public sealed class EcosystemBalanceConfig : ScriptableObject
    {
        [SerializeField] private float defaultMaxPlantBiomass = 100f;
        [SerializeField] private float defaultPlantRegrowthPerDay = 6f;
        [SerializeField] private float defaultSmallPreyPopulation = 4f;
        [SerializeField] private float defaultGrazerPopulation = 3f;
        [SerializeField] private float defaultVarnakPopulation = 1f;

        public float DefaultMaxPlantBiomass => Mathf.Max(1f, defaultMaxPlantBiomass);
        public float DefaultPlantRegrowthPerDay => Mathf.Max(0f, defaultPlantRegrowthPerDay);
        public float DefaultSmallPreyPopulation => Mathf.Max(0f, defaultSmallPreyPopulation);
        public float DefaultGrazerPopulation => Mathf.Max(0f, defaultGrazerPopulation);
        public float DefaultVarnakPopulation => Mathf.Max(0f, defaultVarnakPopulation);

        public void Configure(float maxBiomass, float regrowthPerDay, float smallPrey, float grazer, float varnak)
        {
            defaultMaxPlantBiomass = Mathf.Max(1f, maxBiomass);
            defaultPlantRegrowthPerDay = Mathf.Max(0f, regrowthPerDay);
            defaultSmallPreyPopulation = Mathf.Max(0f, smallPrey);
            defaultGrazerPopulation = Mathf.Max(0f, grazer);
            defaultVarnakPopulation = Mathf.Max(0f, varnak);
        }

        public static EcosystemBalanceConfig CreateDefault()
        {
            EcosystemBalanceConfig config = CreateInstance<EcosystemBalanceConfig>();
            config.Configure(100f, 6f, 4f, 3f, 1f);
            return config;
        }
    }
}
