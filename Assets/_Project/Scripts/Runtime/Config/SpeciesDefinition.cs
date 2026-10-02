using ApexShift.Core.Ecosystem;
using UnityEngine;
using System.Collections.Generic;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Topography;

namespace ApexShift.Runtime.Config
{
    [CreateAssetMenu(menuName = "Apex Shift/Balance/Species Definition", fileName = "SpeciesDefinition")]
    public sealed class SpeciesDefinition : ScriptableObject
    {
        [SerializeField] private string speciesId = "island_small_prey";
        [SerializeField] private string displayName = "Island Small Prey";

        [SerializeField] private CreatureRole role;
        [SerializeField] private CreaturePopulationRules populationRules = new CreaturePopulationRules();
        [SerializeField] private string[] allowedHabitatIds = { HabitatIds.Coast, HabitatIds.LowlandJungle, HabitatIds.JungleInterior, HabitatIds.WetJungle };
        [SerializeField] private TerrainType[] allowedTerrainTypes = { TerrainType.Plain, TerrainType.Forest, TerrainType.Hills, TerrainType.Ridge, TerrainType.Beach };
        [SerializeField] private float minElevation01, minMoisture01, minSlopeDegrees, minDistanceToCoast = 2f;
        [SerializeField] private float maxElevation01 = 1f, maxMoisture01 = 1f, maxSlopeDegrees = 28f, maxDistanceToCoast = 10000f;
        [SerializeField] private bool allowShoreline;
        [SerializeField] private float hitboxRadius = 0.35f, hitboxHeight = 0.70f, hitboxCenterY = 0.38f;
        [SerializeField] private int meatDropAmount = 1, boneDropMin, boneDropMax;
        public CreatureRole Role => role;
        public CreaturePopulationRules PopulationRules => populationRules ?? (populationRules = new CreaturePopulationRules());
        public IReadOnlyList<string> AllowedHabitatIds => allowedHabitatIds ?? System.Array.Empty<string>();
        public IReadOnlyList<TerrainType> AllowedTerrainTypes => allowedTerrainTypes ?? System.Array.Empty<TerrainType>();
        public float MinElevation01 => minElevation01;
        public float MaxElevation01 => maxElevation01;
        public float MinMoisture01 => minMoisture01;
        public float MaxMoisture01 => maxMoisture01;
        public float MinSlopeDegrees => minSlopeDegrees;
        public float MaxSlopeDegrees => maxSlopeDegrees;
        public float MinDistanceToCoast => minDistanceToCoast;
        public float MaxDistanceToCoast => maxDistanceToCoast;
        public bool AllowShoreline => allowShoreline;
        public float HitboxRadius => hitboxRadius;
        public float HitboxHeight => hitboxHeight;
        public float HitboxCenterY => hitboxCenterY;
        public int MeatDropAmount => meatDropAmount;
        public int BoneDropMin => boneDropMin;
        public int BoneDropMax => boneDropMax;
        public void ConfigureRole(CreatureRole value) => role = value;
        public void ConfigureEnvironment(string[] habitats, TerrainType[] terrain, float minElevation = 0f,
            float maxElevation = 1f, float minMoisture = 0f, float maxMoisture = 1f,
            float minSlope = 0f, float maxSlope = 28f, float minCoast = 2f, float maxCoast = 10000f, bool shoreline = false)
        {
            allowedHabitatIds = habitats; allowedTerrainTypes = terrain;
            minElevation01 = minElevation; maxElevation01 = maxElevation;
            minMoisture01 = minMoisture; maxMoisture01 = maxMoisture;
            minSlopeDegrees = minSlope; maxSlopeDegrees = maxSlope;
            minDistanceToCoast = minCoast; maxDistanceToCoast = maxCoast; allowShoreline = shoreline;
        }
        public IEnumerable<string> ValidateProfile()
        {
            if (string.IsNullOrWhiteSpace(speciesId)) yield return "Missing SpeciesId";
            if (!System.Enum.IsDefined(typeof(CreatureRole), role)) yield return "Invalid role";
            if (!PopulationRules.IsValid) yield return "Invalid population rules";
            if (AllowedHabitatIds.Count == 0) yield return "No allowed habitats";
            foreach (string habitat in AllowedHabitatIds)
                if (habitat == HabitatIds.Water || HabitatIds.Normalize(habitat) != habitat)
                    yield return "Invalid land habitat: " + habitat;
            if (AllowedTerrainTypes.Count == 0) yield return "No allowed terrain";
            if (minElevation01 < 0f || maxElevation01 > 1f || minElevation01 > maxElevation01
                || minMoisture01 < 0f || maxMoisture01 > 1f || minMoisture01 > maxMoisture01
                || minSlopeDegrees < 0f || maxSlopeDegrees > 90f || minSlopeDegrees > maxSlopeDegrees
                || minDistanceToCoast < 0f || minDistanceToCoast > maxDistanceToCoast)
                yield return "Invalid environment ranges";
            if (hitboxRadius <= 0f || hitboxHeight < hitboxRadius * 2f || meatDropAmount < 0 || boneDropMin < 0 || boneDropMax < boneDropMin)
                yield return "Invalid physical/drop profile";
        }

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
                case "island_forager":
                    definition.Configure(normalized, "Island Forager", 45f, 100f, 30f, 35f, 60f, 82f, 120f, 170f, 46f, 26f, 36f, 52f, 0.85f, 0.05f, 0.10f);
                    break;
                case "island_predator":
                    definition.Configure(normalized, "Island Predator", 90f, 100f, 18f, 32f, 58f, 80f, 140f, 200f, 38f, 22f, 36f, 58f, 0f, 1f, 0.45f);
                    break;
                case "island_small_prey":
                default:
                    definition.Configure(normalized, "Island Small Prey", 20f, 100f, 20f, 35f, 60f, 82f, 110f, 160f, 50f, 24f, 36f, 48f, 1f, 0f, 0f);
                    break;
            }

            if (normalized == "island_forager")
            {
                definition.role = CreatureRole.HerbivoreOmnivore;
                definition.allowedHabitatIds = new[] { HabitatIds.LowlandJungle, HabitatIds.JungleInterior, HabitatIds.WetJungle };
                definition.populationRules.Configure(3, 6, 6);
                definition.hitboxRadius = 0.65f; definition.hitboxHeight = 1.35f; definition.hitboxCenterY = 0.72f;
                definition.meatDropAmount = 2;
            }
            else if (normalized == "island_predator")
            {
                definition.role = CreatureRole.Predator;
                definition.allowedHabitatIds = new[] { HabitatIds.JungleInterior, HabitatIds.WetJungle, HabitatIds.RockyUpland };
                definition.populationRules.Configure(0, 0, 5, 2, 2, 1, 64, 0.65f, 48f);
                definition.hitboxRadius = 0.70f; definition.hitboxHeight = 1.65f; definition.hitboxCenterY = 0.9f;
                definition.meatDropAmount = 3; definition.boneDropMin = 1; definition.boneDropMax = 2;
            }
            return definition;
        }

        public static string NormalizeSpeciesId(string id)
        {
            return CreatureSpeciesCompatibility.Canonicalize(id);
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
