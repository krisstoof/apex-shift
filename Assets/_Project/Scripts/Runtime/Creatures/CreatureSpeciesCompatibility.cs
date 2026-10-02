namespace ApexShift.Runtime.Creatures
{
    /// <summary>Legacy identifiers are accepted only at configuration/load boundaries.</summary>
    public static class CreatureSpeciesCompatibility
    {
        public static string Canonicalize(string id)
        {
            switch ((id ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "": case "smallprey": case "small-prey": case "small_prey": return "island_small_prey";
                case "grazer": return "island_forager";
                case "varnak": return "island_predator";
                default: return id.Trim().ToLowerInvariant();
            }
        }
    }
}
