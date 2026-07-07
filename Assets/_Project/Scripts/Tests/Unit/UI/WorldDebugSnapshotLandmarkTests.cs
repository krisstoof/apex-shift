using ApexShift.Runtime.UI.Snapshots;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.UI
{
    public sealed class WorldDebugSnapshotLandmarkTests
    {
        [Test]
        public void ConstructorStoresLandmarkCounts()
        {
            WorldDebugSnapshot snapshot = new WorldDebugSnapshot(
                seed: 42,
                playerPosition: Vector3.zero,
                hasPlayer: true,
                resourceCount: 1,
                creatureCount: 2,
                foodSourceCount: 3,
                plantFoodSourceCount: 4,
                meatFoodSourceCount: 5,
                navAgentsOnMesh: 6,
                navAgentsOffMesh: 7,
                hungryCreatureCount: 8,
                storageContainerCount: 9,
                pickupCount: 10,
                fireSourceCount: 11,
                activeFireSourceCount: 12,
                landmarkCount: 5,
                discoveredLandmarkCount: 3,
                fps: 60f,
                realtimeSinceStartup: 1f,
                recentEvents: null);

            Assert.AreEqual(5, snapshot.landmarkCount);
            Assert.AreEqual(3, snapshot.discoveredLandmarkCount);
        }

        [Test]
        public void ConstructorClampsNegativeLandmarkCounts()
        {
            WorldDebugSnapshot snapshot = new WorldDebugSnapshot(
                0,
                Vector3.zero,
                false,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                -10,
                -5,
                0f,
                0f,
                null);

            Assert.AreEqual(0, snapshot.landmarkCount);
            Assert.AreEqual(0, snapshot.discoveredLandmarkCount);
        }
    }
}
