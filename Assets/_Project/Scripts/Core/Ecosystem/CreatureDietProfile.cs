using System;

namespace ApexShift.Core.Ecosystem
{
    public class CreatureDietProfile
    {
        public float PlantPreference { get; }
        public float MeatPreference { get; }
        public float ScavengerPreference { get; }

        // Compatibility booleans for existing tests / simple runtime checks.
        public bool PlantDiet => PlantPreference > 0f;
        public bool MeatDiet => MeatPreference > 0f;
        public bool ScavengerDiet => ScavengerPreference > 0f;

        public CreatureDietProfile(float plant, float meat, float scavenger)
        {
            PlantPreference = Math.Max(0f, plant);
            MeatPreference = Math.Max(0f, meat);
            ScavengerPreference = Math.Max(0f, scavenger);
        }

        public float GetPreference(FoodKind kind)
        {
            return kind switch
            {
                FoodKind.Plants => PlantPreference,
                FoodKind.Meat => MeatPreference,
                FoodKind.Scavenger => ScavengerPreference,
                _ => 0f
            };
        }

        public static CreatureDietProfile SmallPrey() => new CreatureDietProfile(1.0f, 0.0f, 0.0f);
        public static CreatureDietProfile Grazer() => new CreatureDietProfile(0.85f, 0.05f, 0.10f);
        public static CreatureDietProfile Varnak() => new CreatureDietProfile(0.0f, 1.0f, 0.45f);

        public static CreatureDietProfile GetDefault(string creatureId)
        {
            return (creatureId ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "small_prey" => SmallPrey(),
                "grazer" => Grazer(),
                "varnak" => Varnak(),
                _ => SmallPrey()
            };
        }
    }
}
