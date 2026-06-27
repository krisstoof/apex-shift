using System;
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
        public float fps;
        public float realtimeSinceStartup;

        public static WorldDebugSnapshot Empty => new WorldDebugSnapshot();
        public WorldDebugSnapshot() { }
        public WorldDebugSnapshot(int seed, Vector3 playerPosition, bool hasPlayer, int resourceCount, int creatureCount, int foodSourceCount, int plantFoodSourceCount, int meatFoodSourceCount, int navAgentsOnMesh, int navAgentsOffMesh, int hungryCreatureCount, float fps, float realtimeSinceStartup)
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
            this.fps = Mathf.Max(0f, fps);
            this.realtimeSinceStartup = Mathf.Max(0f, realtimeSinceStartup);
        }
    }
}
