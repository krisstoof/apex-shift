using System.Collections.Generic;
using ApexShift.Core.Items;

namespace ApexShift.Runtime.Escape
{
    public sealed class BoatRequirementDefinition
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public string SourceId { get; }
        private BoatRequirementDefinition(string item, string name, string source)
        { ItemId = item; DisplayName = name; SourceId = source; }
        public static IReadOnlyList<BoatRequirementDefinition> Production { get; } =
            System.Array.AsReadOnly(new[] {
                new BoatRequirementDefinition(EscapeBoatItemIds.Fuel, "Boat Fuel", "smuggler_base_fuel"),
                new BoatRequirementDefinition(EscapeBoatItemIds.Battery, "Boat Battery", "smuggler_base_battery")
            });
    }
    public static class EscapeBoatMilestoneIds
    {
        public const string BoatDiscovered = "boat_discovered";
    }
    public enum EscapeBoatState { Unavailable, Discovered, MissingRequirements, Ready, Escaped }
}
