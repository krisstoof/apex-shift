using System;

namespace ApexShift.Core.World.Biomes
{
    public sealed class BiomeDefinition
    {
        public BiomeDefinition(
            BiomeId id,
            string displayName,
            BiomeSpawnProfile spawnProfile,
            bool isStarterBiome = false)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("Biome id must be valid.", nameof(id));
            }

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id.ToString() : displayName.Trim();
            SpawnProfile = spawnProfile ?? new BiomeSpawnProfile(null, null);
            IsStarterBiome = isStarterBiome;
        }

        public BiomeId Id { get; }
        public string DisplayName { get; }
        public BiomeSpawnProfile SpawnProfile { get; }
        public bool IsStarterBiome { get; }
    }
}
