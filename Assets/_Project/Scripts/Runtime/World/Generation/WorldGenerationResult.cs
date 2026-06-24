using System.Collections.Generic;

namespace ApexShift.Runtime.World.Generation
{
    public sealed class WorldGenerationResult
    {
        public int Seed { get; set; }
        public int BiomeCount { get; set; }
        public int ResourceCount { get; set; }
        public int SpawnAttempts { get; set; }
        public List<GeneratedBiomeRegion> Regions { get; } = new List<GeneratedBiomeRegion>();
    }
}
