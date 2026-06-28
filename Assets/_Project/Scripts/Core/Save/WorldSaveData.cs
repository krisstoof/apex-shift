using System;
using System.Collections.Generic;
using System.Linq;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class WorldSaveData
    {
        public int seed;
        public int day = 1;
        public float timeOfDay;
        public List<ResourceSaveData> resources = new List<ResourceSaveData>();
        public List<BiomeEcosystemSaveData> biomeStates = new List<BiomeEcosystemSaveData>();

        public int Seed => seed;
        public int Day => day;
        public float TimeOfDay => timeOfDay;
        public IReadOnlyList<ResourceSaveData> Resources => resources;
        public IReadOnlyList<BiomeEcosystemSaveData> BiomeStates => biomeStates;

        public static WorldSaveData Empty => new WorldSaveData(0, 1, 0f, Array.Empty<ResourceSaveData>(), Array.Empty<BiomeEcosystemSaveData>());

        public WorldSaveData()
        {
        }

        public WorldSaveData(int seed, int day, float timeOfDay, IReadOnlyList<ResourceSaveData> resources)
            : this(seed, day, timeOfDay, resources, Array.Empty<BiomeEcosystemSaveData>())
        {
        }

        public WorldSaveData(
            int seed,
            int day,
            float timeOfDay,
            IReadOnlyList<ResourceSaveData> resources,
            IReadOnlyList<BiomeEcosystemSaveData> biomeStates)
        {
            this.seed = seed;
            this.day = Math.Max(1, day);
            this.timeOfDay = Math.Max(0f, timeOfDay);
            this.resources = resources != null
                ? resources.Where(resource => resource != null).ToList()
                : new List<ResourceSaveData>();
            this.biomeStates = biomeStates != null
                ? biomeStates.Where(state => state != null).ToList()
                : new List<BiomeEcosystemSaveData>();
        }
    }
}
