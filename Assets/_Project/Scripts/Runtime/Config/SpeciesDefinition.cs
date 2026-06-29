using ApexShift.Core.Ecosystem;
using UnityEngine;

namespace ApexShift.Runtime.Config
{
    [CreateAssetMenu(menuName = "Apex Shift/Balance/Species Definition", fileName = "SpeciesDefinition")]
    public sealed class SpeciesDefinition : ScriptableObject
    {
        [SerializeField] private string speciesId = "small_prey";
        [SerializeField] private string displayName = "Small Prey";

        [Header("Health")]
        [SerializeField] private float maxHealth = 20f;

        [Header("Hunger")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float hungerGrowthRate = 20f;
        [SerializeField] private float hungryThreshold = 35f;
        [SerializeField] private float starvingThreshold = 60f;
        [SerializeField] private float desperateThreshold = 82f;
        [SerializeField] private float foodSearchRadius = 110f;
        [SerializeField] private float desperateFoodSearchRadius = 160f;
        [SerializeField] private float preySeekHungerThreshold = 50f;
        [SerializeField] private float fleeHungerThreshold = 24f;
        [SerializeField] private float initialHungerMin = 36f;
        [SerializeField] private float initialHungerMax = 48f;

        [Header("Diet")]
        [SerializeField] private float plantPreference = 1f;
        [SerializeField] private float meatPreference;
        [SerializeField] private float scavengerPreference;

        public string SpeciesId => NormalizeSpeciesId(speciesId);
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? SpeciesId : displayName.Trim();
        public float MaxHealth => Mathf.Max(0.01f, maxHealth);
        public float MaxHunger => Mathf.Max(0.01f, maxHunger);
        public float HungerGrowthRate => Mathf.Max(0f, hungerGrowthRate);
        public float HungryThreshold => Mathf.Clamp(hungryThreshold, 0.01f, MaxHunger);
        public float StarvingThreshold => Mathf.Clamp(Mathf.Max(starvingThreshold, HungryThreshold + 0.01f), 0.02f, MaxHunger);
        public float DesperateThreshold => Mathf.Clamp(Mathf.Max(desperateThreshold, StarvingThreshold + 0.01f), 0.03f, MaxHunger);
        public float FoodSearchRadius => Mathf.Max(0f, foodSearchRadius);
        public float DesperateFoodSearchRadius => Mathf.Max(FoodSearchRadius, desperateFoodSearchRadius);
        public float PreySeekHungerThreshold => Mathf.Clamp(preySeekHungerThreshold, 0f, MaxHunger);
        public float FleeHungerThreshold => Mathf.Clamp(fleeHungerThreshold, 0f, MaxHunger);
        public float InitialHungerMin => Mathf.Clamp(initialHungerMin, 0f, MaxHunger);
        public float InitialHungerMax => Mathf.Clamp(Mathf.Max(initialHungerMax, InitialHungerMin), 0f, MaxHunger);
        public float PlantPreference => Mathf.Max(0f, plantPreference);
        public float MeatPreference => Mathf.Max(0f, meatPreference);
        public float ScavengerPreference => Mathf.Max(0f, scavengerPreference);

        public bool Matches(string id)
        {
            return SpeciesId == NormalizeSpeciesId(id);
        }

        public CreatureDietProfile CreateDietProfile()
        {
            return new CreatureDietProfile(PlantPreference, MeatPreference, ScavengerPreference);
        }

        public void Configure(
            string id,
            string name,
            float maxHealthValue,
            float maxHungerValue,
            float hungerGrowth,
            float hungry,
            float starving,
            float desperate,
            float foodSearch,
            float desperateFoodSearch,
            float preySeekThreshold,
            float fleeThreshold,
            float initialMin,
            float initialMax,
            float plant,
            float meat,
            float scavenger)
        {
            speciesId = NormalizeSpeciesId(id);
            displayName = string.IsNullOrWhiteSpace(name) ? speciesId : name.Trim();
            maxHealth = Mathf.Max(0.01f, maxHealthValue);
            maxHunger = Mathf.Max(0.01f, maxHungerValue);
            hungerGrowthRate = Mathf.Max(0f, hungerGrowth);
            hungryThreshold = Mathf.Clamp(hungry, 0.01f, maxHunger);
            starvingThreshold = Mathf.Clamp(Mathf.Max(starving, hungryThreshold + 0.01f), 0.02f, maxHunger);
            desperateThreshold = Mathf.Clamp(Mathf.Max(desperate, starvingThreshold + 0.01f), 0.03f, maxHunger);
            foodSearchRadius = Mathf.Max(0f, foodSearch);
            desperateFoodSearchRadius = Mathf.Max(FoodSearchRadius, desperateFoodSearch);
            preySeekHungerThreshold = Mathf.Clamp(preySeekThreshold, 0f, maxHunger);
            fleeHungerThreshold = Mathf.Clamp(fleeThreshold, 0f, maxHunger);
            initialHungerMin = Mathf.Clamp(initialMin, 0f, maxHunger);
            initialHungerMax = Mathf.Clamp(Mathf.Max(initialMax, initialHungerMin), 0f, maxHunger);
            plantPreference = Mathf.Max(0f, plant);
            meatPreference = Mathf.Max(0f, meat);
            scavengerPreference = Mathf.Max(0f, scavenger);
        }

        public static SpeciesDefinition CreateDefault(string id)
        {
            SpeciesDefinition definition = CreateInstance<SpeciesDefinition>();
            string normalized = NormalizeSpeciesId(id);

            switch (normalized)
            {
                case "grazer":
                    definition.Configure("grazer", "Grazer", 45f, 100f, 30f, 35f, 60f, 82f, 120f, 170f, 46f, 26f, 36f, 52f, 0.85f, 0.05f, 0.10f);
                    break;
                case "varnak":
                    definition.Configure("varnak", "Varnak", 90f, 100f, 18f, 32f, 58f, 80f, 140f, 200f, 38f, 22f, 36f, 58f, 0f, 1f, 0.45f);
                    break;
                case "small_prey":
                default:
                    definition.Configure("small_prey", "Small Prey", 20f, 100f, 20f, 35f, 60f, 82f, 110f, 160f, 50f, 24f, 36f, 48f, 1f, 0f, 0f);
                    break;
            }

            return definition;
        }

        public static string NormalizeSpeciesId(string id)
        {
            string normalized = string.IsNullOrWhiteSpace(id) ? "small_prey" : id.Trim().ToLowerInvariant();
            if (normalized == "smallprey" || normalized == "small-prey")
            {
                return "small_prey";
            }

            return normalized;
        }

        private void OnValidate()
        {
            speciesId = NormalizeSpeciesId(speciesId);
            maxHealth = Mathf.Max(0.01f, maxHealth);
            maxHunger = Mathf.Max(0.01f, maxHunger);
            hungerGrowthRate = Mathf.Max(0f, hungerGrowthRate);
            hungryThreshold = Mathf.Clamp(hungryThreshold, 0.01f, maxHunger);
            starvingThreshold = Mathf.Clamp(Mathf.Max(starvingThreshold, hungryThreshold + 0.01f), 0.02f, maxHunger);
            desperateThreshold = Mathf.Clamp(Mathf.Max(desperateThreshold, starvingThreshold + 0.01f), 0.03f, maxHunger);
            foodSearchRadius = Mathf.Max(0f, foodSearchRadius);
            desperateFoodSearchRadius = Mathf.Max(FoodSearchRadius, desperateFoodSearchRadius);
            preySeekHungerThreshold = Mathf.Clamp(preySeekHungerThreshold, 0f, maxHunger);
            fleeHungerThreshold = Mathf.Clamp(fleeHungerThreshold, 0f, maxHunger);
            initialHungerMin = Mathf.Clamp(initialHungerMin, 0f, maxHunger);
            initialHungerMax = Mathf.Clamp(Mathf.Max(initialHungerMax, initialHungerMin), 0f, maxHunger);
            plantPreference = Mathf.Max(0f, plantPreference);
            meatPreference = Mathf.Max(0f, meatPreference);
            scavengerPreference = Mathf.Max(0f, scavengerPreference);
        }
    }
}
