using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ApexShift.Runtime.UI.Snapshots
{
    [Serializable]
    public sealed class WorldDebugSnapshot
    {
        public int seed;
        public Vector3 playerPosition;
        public bool hasPlayer;
        public int resourceCount;
        public int creatureCount;
        public int foodSourceCount;
        public int plantFoodSourceCount;
        public int meatFoodSourceCount;
        public int navAgentsOnMesh;
        public int navAgentsOffMesh;
        public int hungryCreatureCount;
        public int storageContainerCount;
        public int pickupCount;
        public float fps;
        public float realtimeSinceStartup;
        public string[] recentEvents = Array.Empty<string>();

        public static WorldDebugSnapshot Empty => new WorldDebugSnapshot();
        public WorldDebugSnapshot() { }
        public WorldDebugSnapshot(int seed, Vector3 playerPosition, bool hasPlayer, int resourceCount, int creatureCount, int foodSourceCount, int plantFoodSourceCount, int meatFoodSourceCount, int navAgentsOnMesh, int navAgentsOffMesh, int hungryCreatureCount, float fps, float realtimeSinceStartup)
            : this(seed, playerPosition, hasPlayer, resourceCount, creatureCount, foodSourceCount, plantFoodSourceCount, meatFoodSourceCount, navAgentsOnMesh, navAgentsOffMesh, hungryCreatureCount, 0, 0, fps, realtimeSinceStartup, Array.Empty<string>())
        {
        }

        public WorldDebugSnapshot(int seed, Vector3 playerPosition, bool hasPlayer, int resourceCount, int creatureCount, int foodSourceCount, int plantFoodSourceCount, int meatFoodSourceCount, int navAgentsOnMesh, int navAgentsOffMesh, int hungryCreatureCount, float fps, float realtimeSinceStartup, IReadOnlyList<string> recentEvents)
            : this(seed, playerPosition, hasPlayer, resourceCount, creatureCount, foodSourceCount, plantFoodSourceCount, meatFoodSourceCount, navAgentsOnMesh, navAgentsOffMesh, hungryCreatureCount, 0, 0, fps, realtimeSinceStartup, recentEvents)
        {
        }

        public WorldDebugSnapshot(int seed, Vector3 playerPosition, bool hasPlayer, int resourceCount, int creatureCount, int foodSourceCount, int plantFoodSourceCount, int meatFoodSourceCount, int navAgentsOnMesh, int navAgentsOffMesh, int hungryCreatureCount, int storageContainerCount, int pickupCount, float fps, float realtimeSinceStartup, IReadOnlyList<string> recentEvents)
        {
            this.seed = seed;
            this.playerPosition = playerPosition;
            this.hasPlayer = hasPlayer;
            this.resourceCount = Math.Max(0, resourceCount);
            this.creatureCount = Math.Max(0, creatureCount);
            this.foodSourceCount = Math.Max(0, foodSourceCount);
            this.plantFoodSourceCount = Math.Max(0, plantFoodSourceCount);
            this.meatFoodSourceCount = Math.Max(0, meatFoodSourceCount);
            this.navAgentsOnMesh = Math.Max(0, navAgentsOnMesh);
            this.navAgentsOffMesh = Math.Max(0, navAgentsOffMesh);
            this.hungryCreatureCount = Math.Max(0, hungryCreatureCount);
            this.storageContainerCount = Math.Max(0, storageContainerCount);
            this.pickupCount = Math.Max(0, pickupCount);
            this.fps = Mathf.Max(0f, fps);
            this.realtimeSinceStartup = Mathf.Max(0f, realtimeSinceStartup);
            this.recentEvents = recentEvents != null ? recentEvents.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray() : Array.Empty<string>();
        }
    }
}
