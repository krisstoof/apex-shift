namespace ApexShift.Core.Ecosystem
{
    public class CreatureDietProfile
    {
        public bool PlantDiet { get; }
        public bool MeatDiet { get; }
        public bool ScavengerDiet { get; }

        public CreatureDietProfile(bool plant, bool meat, bool scavenger)
        {
            PlantDiet = plant;
            MeatDiet = meat;
            ScavengerDiet = scavenger;
        }

        public static CreatureDietProfile SmallPrey() => new CreatureDietProfile(true, false, false);
        public static CreatureDietProfile Grazer() => new CreatureDietProfile(true, false, false);
        public static CreatureDietProfile Varnak() => new CreatureDietProfile(false, true, true);
        
        public static CreatureDietProfile GetDefault(string creatureId)
        {
            return creatureId switch
            {
                "small_prey" => SmallPrey(),
                "grazer" => Grazer(),
                "varnak" => Varnak(),
                _ => SmallPrey()
            };
        }
    }
}
