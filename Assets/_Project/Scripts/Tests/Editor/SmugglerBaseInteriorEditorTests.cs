using System.Linq;
using ApexShift.Core.Save;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Interiors;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class SmugglerBaseInteriorEditorTests
    {
        [Test]
        public void FixedLayoutIsOwnedInactiveCompleteAndDoesNotConsumeRandom()
        {
            var a = new GameObject("GenerationA"); var b = new GameObject("GenerationB");
            var random = Random.state;
            try
            {
                var first = Build(a, 1); var second = Build(b, 999);
                Assert.AreEqual(random, Random.state);
                Assert.IsFalse(first.InteriorRoot.gameObject.activeSelf);
                Assert.IsTrue(first.ValidateLayout());
                foreach (string name in new[] { "EntryTunnel", "StorageRoom", "OperationsRoom", "DockChamber",
                    "EntrySpawn", "IslandExitInteraction", "StorageLootAnchor", "OperationsClueAnchor", "FuelAnchor",
                    "BatteryAnchor", "BoatKeyAnchor", "BoatAnchor", "SmugglerBoatPlaceholder" })
                {
                    var x = first.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
                    var y = second.GetComponentsInChildren<Transform>(true).Single(t => t.name == name);
                    Assert.AreEqual(x.localPosition, y.localPosition, name);
                }
                Assert.AreEqual("DockChamber", first.BoatAnchor.parent.name);
                Assert.AreEqual(1, a.GetComponentsInChildren<SmugglerBaseInteriorRuntime>(true).Length);
                Assert.AreEqual(1, a.GetComponentsInChildren<BaseInteriorExitRuntime>(true).Length);
                foreach (var floor in first.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Floor"))
                    Assert.NotNull(floor.GetComponent<Collider>());
                Assert.IsFalse(first.TryEnter(null)); Assert.IsFalse(first.TryEnter(b)); Assert.IsFalse(first.TryExit(null));
            }
            finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); Random.state = random; }
        }

        [Test]
        public void OldSaveDefaultsToIslandAndSchemaRoundTrips()
        {
            var old = JsonUtility.FromJson<WorldSaveData>("{\"seed\":5}");
            Assert.AreEqual(PlayerAreaIds.Island, old.PlayerLocation.AreaId);
            Assert.IsFalse(old.PlayerLocation.HasLocalPosition);
            var data = new PlayerLocationSaveData { areaId = PlayerAreaIds.SmugglerBase, hasLocalPosition = true,
                localX = 1f, localY = 0.1f, localZ = 14f, hasIslandReturnPosition = true, islandReturnX = 10f };
            var restored = JsonUtility.FromJson<PlayerLocationSaveData>(JsonUtility.ToJson(data));
            Assert.AreEqual(data.AreaId, restored.AreaId); Assert.AreEqual(data.LocalZ, restored.LocalZ);
            Assert.IsTrue(restored.HasIslandReturnPosition);
        }

        private static SmugglerBaseInteriorRuntime Build(GameObject root, int seed)
        {
            var interiorRoot = new GameObject("InteriorRoot").transform; interiorRoot.SetParent(root.transform);
            var context = new WorldGenerationContext(seed, root.transform) { InteriorRoot = interiorRoot };
            var runtime = SmugglerBaseInteriorBuilder.Build(context);
            Assert.AreSame(runtime, SmugglerBaseInteriorBuilder.Build(context));
            return runtime;
        }
    }
}
