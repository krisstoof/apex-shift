using System;
using System.Collections.Generic;

namespace ApexShift.Core.World.Biomes
{
    public sealed class BiomeSpawnProfile
    {
        public BiomeSpawnProfile(
            IEnumerable<VegetationSpawnEntry> vegetation,
            IEnumerable<CreatureSpawnEntry> creatures)
        {
            Vegetation = new List<VegetationSpawnEntry>(vegetation ?? Array.Empty<VegetationSpawnEntry>()).AsReadOnly();
            Creatures = new List<CreatureSpawnEntry>(creatures ?? Array.Empty<CreatureSpawnEntry>()).AsReadOnly();
        }

        public IReadOnlyList<VegetationSpawnEntry> Vegetation { get; }
        public IReadOnlyList<CreatureSpawnEntry> Creatures { get; }

        public int GetVegetationCount(string roleId)
        {
            string normalized = BiomeId.NormalizeId(roleId);
            int total = 0;

            foreach (VegetationSpawnEntry entry in Vegetation)
            {
                if (entry.RoleId == normalized)
                {
                    total += entry.Count;
                }
            }

            return total;
        }
}
}
