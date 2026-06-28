using ApexShift.Core.Resources;
using ApexShift.Runtime.Resources;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.GodotParity
{
    public sealed class ResourcesGodotParityTests
    {
        [Test]
        public void EdibleVegetationCanBeConsumedByHerbivores()
        {
            ResourceState berryBush = ResourceDefinition.CreateDefault("berry_bush").CreateState();
            ResourceState grass = ResourceDefinition.CreateDefault("grass_patch").CreateState();

            Assert.IsTrue(berryBush.IsFoodAvailableForHerbivores);
            Assert.IsTrue(grass.IsFoodAvailableForHerbivores);
            Assert.Greater(berryBush.FoodValue, 0f);
            Assert.Greater(grass.FoodValue, 0f);
        }

        [Test]
        public void DepletedVegetationIsNotEdible()
        {
            ResourceState berryBush = ResourceDefinition.CreateDefault("berry_bush").CreateState();

            berryBush.MarkDepleted();

            Assert.IsTrue(berryBush.IsDepleted);
            Assert.IsFalse(berryBush.IsFoodAvailableForHerbivores);
        }

        [Test]
        public void MeatDropHasPickupPriorityAndIsNotHerbivoreVegetation()
        {
            ResourceState meat = ResourceDefinition.CreateDefault("meat_drop").CreateState();

            Assert.IsTrue(meat.IsDrop);
            Assert.AreEqual("meat", meat.ItemId);
            Assert.Greater(meat.PickupPriority, 0);
            Assert.IsFalse(meat.IsFoodAvailableForHerbivores);
        }

        [Test]
        public void RegrowthAdvancesOverDaysAndRestoresResource()
        {
            ResourceState grass = ResourceDefinition.CreateDefault("grass_patch").CreateState();
            grass.MarkDepleted();

            bool changed = grass.AdvanceGrowthDays(1);

            Assert.IsTrue(changed);
            Assert.IsFalse(grass.IsDepleted);
            Assert.AreEqual(1f, grass.GrowthProgress, 0.001f);
            Assert.AreEqual(grass.MaxAmount, grass.Amount);
        }

        [Test]
        public void ResourceNodeLoadStateRestoresGrowthState()
        {
            GameObject go = new GameObject("BerryBush");
            try
            {
                ResourceNodeView view = go.AddComponent<ResourceNodeView>();
                view.ConfigureDefault("berry_bush");

                view.LoadState(0, true, 0.5f);

                Assert.IsTrue(view.State.IsDepleted);
                Assert.AreEqual(0.5f, view.State.GrowthProgress, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
