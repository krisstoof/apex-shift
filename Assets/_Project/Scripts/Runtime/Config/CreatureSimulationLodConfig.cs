using UnityEngine;

namespace ApexShift.Runtime.Config
{
    [CreateAssetMenu(menuName = "Apex Shift/Balance/Creature Simulation LOD Config", fileName = "CreatureSimulationLodConfig")]
    public sealed class CreatureSimulationLodConfig : ScriptableObject
    {
        [SerializeField] private float nearDistance = 90f;
        [SerializeField] private float mediumDistance = 180f;
        [SerializeField] private float forceVarnakNearDistance = 70f;
        [SerializeField] private float mediumAiIntervalMultiplier = 3f;
        [SerializeField] private float mediumSpatialUpdateMultiplier = 2f;
        [SerializeField] private float farUpdateIntervalSeconds = 3f;
        [SerializeField] private float backgroundSimulationIntervalSeconds = 3f;
        [SerializeField] private bool debugEnabled = true;

        public float NearDistance => Mathf.Max(0.01f, nearDistance);
        public float MediumDistance => Mathf.Max(NearDistance, mediumDistance);
        public float ForceVarnakNearDistance => Mathf.Max(0f, forceVarnakNearDistance);
        public float MediumAiIntervalMultiplier => Mathf.Max(1f, mediumAiIntervalMultiplier);
        public float MediumSpatialUpdateMultiplier => Mathf.Max(1f, mediumSpatialUpdateMultiplier);
        public float FarUpdateIntervalSeconds => Mathf.Max(0.05f, farUpdateIntervalSeconds);
        public float BackgroundSimulationIntervalSeconds => Mathf.Max(0.05f, backgroundSimulationIntervalSeconds);
        public bool DebugEnabled => debugEnabled;

        public void Configure(float near, float medium, float forceVarnakNear, float mediumAiMultiplier, float mediumSpatialMultiplier, float farInterval, float backgroundInterval, bool debug)
        {
            nearDistance = Mathf.Max(0.01f, near);
            mediumDistance = Mathf.Max(nearDistance, medium);
            forceVarnakNearDistance = Mathf.Max(0f, forceVarnakNear);
            mediumAiIntervalMultiplier = Mathf.Max(1f, mediumAiMultiplier);
            mediumSpatialUpdateMultiplier = Mathf.Max(1f, mediumSpatialMultiplier);
            farUpdateIntervalSeconds = Mathf.Max(0.05f, farInterval);
            backgroundSimulationIntervalSeconds = Mathf.Max(0.05f, backgroundInterval);
            debugEnabled = debug;
        }

        public static CreatureSimulationLodConfig CreateDefault()
        {
            CreatureSimulationLodConfig config = CreateInstance<CreatureSimulationLodConfig>();
            config.Configure(90f, 180f, 70f, 3f, 2f, 3f, 3f, true);
            return config;
        }
    }
}
