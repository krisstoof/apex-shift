using UnityEngine;

namespace ApexShift.Runtime.Config
{
    [CreateAssetMenu(menuName = "Apex Shift/Balance/Resource Balance Config", fileName = "ResourceBalanceConfig")]
    public sealed class ResourceBalanceConfig : ScriptableObject
    {
        [SerializeField] private float bushFoodValue = 6f;
        [SerializeField] private float berryBushFoodValue = 8f;
        [SerializeField] private float grassFoodValue = 5f;
        [SerializeField] private float denseGrassFoodValue = 10f;
        [SerializeField] private float meatFoodValue = 10f;
        [SerializeField] private int grassRegrowthDays = 1;
        [SerializeField] private int berryBushRegrowthDays = 2;

        public float BushFoodValue => Mathf.Max(0f, bushFoodValue);
        public float BerryBushFoodValue => Mathf.Max(0f, berryBushFoodValue);
        public float GrassFoodValue => Mathf.Max(0f, grassFoodValue);
        public float DenseGrassFoodValue => Mathf.Max(0f, denseGrassFoodValue);
        public float MeatFoodValue => Mathf.Max(0f, meatFoodValue);
        public int GrassRegrowthDays => Mathf.Max(0, grassRegrowthDays);
        public int BerryBushRegrowthDays => Mathf.Max(0, berryBushRegrowthDays);

        public void Configure(float bush, float berry, float grass, float denseGrass, float meat, int grassRegrowth, int berryRegrowth)
        {
            bushFoodValue = Mathf.Max(0f, bush);
            berryBushFoodValue = Mathf.Max(0f, berry);
            grassFoodValue = Mathf.Max(0f, grass);
            denseGrassFoodValue = Mathf.Max(0f, denseGrass);
            meatFoodValue = Mathf.Max(0f, meat);
            grassRegrowthDays = Mathf.Max(0, grassRegrowth);
            berryBushRegrowthDays = Mathf.Max(0, berryRegrowth);
        }

        public static ResourceBalanceConfig CreateDefault()
        {
            ResourceBalanceConfig config = CreateInstance<ResourceBalanceConfig>();
            config.Configure(6f, 8f, 5f, 10f, 10f, 1, 2);
            return config;
        }
    }
}
