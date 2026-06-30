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
        public List<PickupSaveData> pickups = new List<PickupSaveData>();
        public List<BiomeEcosystemSaveData> biomeStates = new List<BiomeEcosystemSaveData>();
        public List<CreatureSaveData> creatureStates = new List<CreatureSaveData>();
        public List<BuildingSaveData> buildingStates = new List<BuildingSaveData>();
        public float ecosystemTickTimer;
        public string ecosystemStateSource = "generated";

        public int Seed => seed;
        public int Day => day;
        public float TimeOfDay => timeOfDay;
        public IReadOnlyList<ResourceSaveData> Resources => resources ?? (resources = new List<ResourceSaveData>());
        public IReadOnlyList<PickupSaveData> Pickups => pickups ?? (pickups = new List<PickupSaveData>());
        public IReadOnlyList<BiomeEcosystemSaveData> BiomeStates => biomeStates ?? (biomeStates = new List<BiomeEcosystemSaveData>());
        public IReadOnlyList<CreatureSaveData> CreatureStates => creatureStates ?? (creatureStates = new List<CreatureSaveData>());
        public IReadOnlyList<BuildingSaveData> BuildingStates => buildingStates ?? (buildingStates = new List<BuildingSaveData>());
        public float EcosystemTickTimer => ecosystemTickTimer;
        public string EcosystemStateSource => string.IsNullOrWhiteSpace(ecosystemStateSource) ? "generated" : ecosystemStateSource;

        public static WorldSaveData Empty => new WorldSaveData(0, 1, 0f, Array.Empty<ResourceSaveData>(), Array.Empty<PickupSaveData>(), Array.Empty<BiomeEcosystemSaveData>());

        public WorldSaveData()
        {
        }

        public WorldSaveData(int seed, int day, float timeOfDay, IReadOnlyList<ResourceSaveData> resources)
            : this(seed, day, timeOfDay, resources, Array.Empty<PickupSaveData>(), Array.Empty<BiomeEcosystemSaveData>())
        {
        }

        public WorldSaveData(
            int seed,
            int day,
            float timeOfDay,
            IReadOnlyList<ResourceSaveData> resources,
            IReadOnlyList<PickupSaveData> pickups,
            IReadOnlyList<BiomeEcosystemSaveData> biomeStates,
            IReadOnlyList<CreatureSaveData> creatureStates,
            IReadOnlyList<BuildingSaveData> buildingStates,
            float ecosystemTickTimer,
            string ecosystemStateSource)
            : this(seed, day, timeOfDay, resources, pickups, biomeStates)
        {
            this.creatureStates = creatureStates != null ? creatureStates.Where(state => state != null).ToList() : new List<CreatureSaveData>();
            this.buildingStates = buildingStates != null ? buildingStates.Where(state => state != null).ToList() : new List<BuildingSaveData>();
            this.ecosystemTickTimer = Math.Max(0f, ecosystemTickTimer);
            this.ecosystemStateSource = string.IsNullOrWhiteSpace(ecosystemStateSource) ? "generated" : ecosystemStateSource.Trim();
        }

        public WorldSaveData(
            int seed,
            int day,
            float timeOfDay,
            IReadOnlyList<ResourceSaveData> resources,
            IReadOnlyList<BiomeEcosystemSaveData> biomeStates,
            IReadOnlyList<CreatureSaveData> creatureStates,
            IReadOnlyList<BuildingSaveData> buildingStates,
            float ecosystemTickTimer,
            string ecosystemStateSource)
            : this(seed, day, timeOfDay, resources, Array.Empty<PickupSaveData>(), biomeStates, creatureStates, buildingStates, ecosystemTickTimer, ecosystemStateSource)
        {
        }

        public WorldSaveData(
            int seed,
            int day,
            float timeOfDay,
            IReadOnlyList<ResourceSaveData> resources,
            IReadOnlyList<PickupSaveData> pickups,
            IReadOnlyList<BiomeEcosystemSaveData> biomeStates)
        {
            this.seed = seed;
            this.day = Math.Max(1, day);
            this.timeOfDay = NormalizeTimeOfDay(timeOfDay);
            this.resources = resources != null
                ? resources.Where(resource => resource != null).ToList()
                : new List<ResourceSaveData>();
            this.pickups = pickups != null
                ? pickups.Where(pickup => pickup != null).ToList()
                : new List<PickupSaveData>();
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

    [Serializable]
    public sealed class PickupSaveData
    {
        public string itemId;
        public int amount;
        public float x;
        public float y;
        public float z;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW = 1f;

        public string ItemId => itemId;
        public int Amount => amount;
        public float X => x;
        public float Y => y;
        public float Z => z;
        public float RotX => rotX;
        public float RotY => rotY;
        public float RotZ => rotZ;
        public float RotW => rotW;

        public PickupSaveData()
        {
        }

        public PickupSaveData(string itemId, int amount, float x, float y, float z, float rotX, float rotY, float rotZ, float rotW)
        {
            this.itemId = itemId ?? string.Empty;
            this.amount = Math.Max(0, amount);
            this.x = x;
            this.y = y;
            this.z = z;
            this.rotX = rotX;
            this.rotY = rotY;
            this.rotZ = rotZ;
            this.rotW = rotW;
        }
    }
}
