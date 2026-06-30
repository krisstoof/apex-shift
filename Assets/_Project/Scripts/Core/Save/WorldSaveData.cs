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
        public List<CreatureSaveData> creatureStates = new List<CreatureSaveData>();
        public float ecosystemTickTimer;
        public string ecosystemStateSource = "generated";

        public int Seed => seed;
        public int Day => day;
        public float TimeOfDay => timeOfDay;
        public IReadOnlyList<ResourceSaveData> Resources => resources ?? (resources = new List<ResourceSaveData>());
        public IReadOnlyList<BiomeEcosystemSaveData> BiomeStates => biomeStates ?? (biomeStates = new List<BiomeEcosystemSaveData>());
        public IReadOnlyList<CreatureSaveData> CreatureStates => creatureStates ?? (creatureStates = new List<CreatureSaveData>());
        public float EcosystemTickTimer => ecosystemTickTimer;
        public string EcosystemStateSource => string.IsNullOrWhiteSpace(ecosystemStateSource) ? "generated" : ecosystemStateSource;

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
            IReadOnlyList<BiomeEcosystemSaveData> biomeStates,
            IReadOnlyList<CreatureSaveData> creatureStates,
            float ecosystemTickTimer,
            string ecosystemStateSource)
            : this(seed, day, timeOfDay, resources, biomeStates)
        {
            this.creatureStates = creatureStates != null ? creatureStates.Where(state => state != null).ToList() : new List<CreatureSaveData>();
            this.ecosystemTickTimer = Math.Max(0f, ecosystemTickTimer);
            this.ecosystemStateSource = string.IsNullOrWhiteSpace(ecosystemStateSource) ? "generated" : ecosystemStateSource.Trim();
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
            this.timeOfDay = NormalizeTimeOfDay(timeOfDay);
            this.resources = resources != null
                ? resources.Where(resource => resource != null).ToList()
                : new List<ResourceSaveData>();
            this.biomeStates = biomeStates != null
                ? biomeStates.Where(state => state != null).ToList()
                : new List<BiomeEcosystemSaveData>();
        }

        private static float NormalizeTimeOfDay(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            float normalized = value % 1f;
            return normalized < 0f ? normalized + 1f : normalized;
        }
    }
}
