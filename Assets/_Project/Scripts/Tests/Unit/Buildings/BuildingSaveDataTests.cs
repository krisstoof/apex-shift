using ApexShift.Core.Save;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Buildings
{
    public sealed class BuildingSaveDataTests
    {
        [Test]
        public void BuildingSaveData_NormalizesIdsAndYaw()
        {
            BuildingSaveData data = new BuildingSaveData(" id ", " CampFire ", 1f, 2f, 3f, -90f);

            Assert.AreEqual("id", data.InstanceId);
            Assert.AreEqual("CampFire", data.BuildingId);
            Assert.AreEqual(270f, data.RotationY, 0.001f);
            Assert.IsTrue(data.Active);
        }

        [Test]
        public void BuildingPlacementRuntime_CanSelectByBuildingId()
        {
            GameObject go = new GameObject("Placement");
            try
            {
                var runtime = go.AddComponent<ApexShift.Runtime.Buildings.BuildingPlacementRuntime>();
                bool selected = runtime.SelectBuilding("campfire");

                Assert.IsTrue(selected);
                Assert.AreEqual("campfire", runtime.SelectedBuildingId);
                Assert.IsNotEmpty(runtime.GetSelectedHint());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
