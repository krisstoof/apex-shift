using System;
using UnityEngine;

namespace ApexShift.Runtime.Config
{
    [Serializable]
    public sealed class CreaturePopulationRules
    {
        [SerializeField] private int initialMinCount = 4;
        [SerializeField] private int initialMaxCount = 8;
        [SerializeField] private int absolutePopulationCap = 8;
        [SerializeField] private int firstGrowthDay = 3;
        [SerializeField] private int addEveryDays = 2;
        [SerializeField] private int addCountPerInterval;
        [SerializeField] private int dailySpawnAttempts = 48;
        [SerializeField] private float dailySpawnChance = 0.5f;
        [SerializeField] private float minPlayerSpawnDistance = 30f;

        public int InitialMinCount => initialMinCount;
        public int InitialMaxCount => initialMaxCount;
        public int AbsolutePopulationCap => absolutePopulationCap;
        public int DailySpawnAttempts => dailySpawnAttempts;
        public float DailySpawnChance => dailySpawnChance;
        public float MinPlayerSpawnDistance => minPlayerSpawnDistance;
        public int GetPopulationCapForDay(int day)
        {
            long intervals = day < firstGrowthDay ? 0 : 1L + ((long)day - firstGrowthDay) / Math.Max(1, addEveryDays);
            return (int)Math.Max(0L, Math.Min(absolutePopulationCap, initialMaxCount + intervals * addCountPerInterval));
        }
        public void Configure(int min, int max, int cap, int firstDay = 3, int everyDays = 2,
            int growth = 0, int attempts = 48, float chance = 0.5f, float playerDistance = 30f)
        {
            initialMinCount = min; initialMaxCount = max; absolutePopulationCap = cap;
            firstGrowthDay = firstDay; addEveryDays = everyDays; addCountPerInterval = growth;
            dailySpawnAttempts = attempts; dailySpawnChance = chance; minPlayerSpawnDistance = playerDistance;
        }
        public bool IsValid => initialMinCount >= 0 && initialMaxCount >= initialMinCount
            && absolutePopulationCap >= initialMaxCount && firstGrowthDay >= 1 && addEveryDays >= 1
            && addCountPerInterval >= 0 && dailySpawnAttempts >= 0 && dailySpawnChance >= 0f
            && dailySpawnChance <= 1f && minPlayerSpawnDistance >= 0f;
    }
}
