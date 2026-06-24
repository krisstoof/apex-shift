using System;

namespace ApexShift.Core.Ecosystem
{
    public readonly struct DietProfile
    {
        public DietProfile(float plantDiet, float meatDiet, float scavengerDiet)
        {
            PlantDiet = Math.Max(0f, plantDiet);
            MeatDiet = Math.Max(0f, meatDiet);
            ScavengerDiet = Math.Max(0f, scavengerDiet);
        }

        public float PlantDiet { get; }
        public float MeatDiet { get; }
        public float ScavengerDiet { get; }

        public float GetPreference(FoodKind kind)
        {
            return kind switch
            {
                FoodKind.Plants => PlantDiet,
                FoodKind.Meat => MeatDiet,
                FoodKind.Scavenger => ScavengerDiet,
                _ => 0f
            };
        }

        public CreatureDietProfile ToCreatureDietProfile()
        {
            return new CreatureDietProfile(PlantDiet, MeatDiet, ScavengerDiet);
        }

        public static DietProfile FromCreatureDietProfile(CreatureDietProfile profile)
        {
            if (profile == null)
            {
                return ForCreature("small_prey");
            }

            return new DietProfile(
                profile.PlantPreference,
                profile.MeatPreference,
                profile.ScavengerPreference);
        }

        public static DietProfile ForCreature(string creatureId)
        {
            return FromCreatureDietProfile(CreatureDietProfile.GetDefault(creatureId));
        }
    }
}
