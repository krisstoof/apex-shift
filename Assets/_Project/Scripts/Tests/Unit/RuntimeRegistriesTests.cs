using ApexShift.Runtime.Items;
using ApexShift.Runtime.Resources;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit
{
    public sealed class RuntimeRegistriesTests
    {
        [SetUp]
        public void SetUp()
        {
            ResourceRegistry.ClearForTests();
            ItemPickupRegistry.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            ResourceRegistry.ClearForTests();
            ItemPickupRegistry.ClearForTests();
        }

        [Test]
        public void ResourceRegistryRegistersAndUnregistersNodes()
        {
            GameObject go = new GameObject("resource");
            ResourceNodeView node = go.AddComponent<ResourceNodeView>();

            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(1));
            Assert.That(ResourceRegistry.Resources, Does.Contain(node));

            Object.DestroyImmediate(go);
            ResourceRegistry.Cleanup();

            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(0));
        }

        [Test]
        public void ResourceRegistryKeepsDisabledNodesForSaveState()
        {
            GameObject go = new GameObject("resource");
            ResourceNodeView node = go.AddComponent<ResourceNodeView>();

            go.SetActive(false);

            Assert.That(ResourceRegistry.Resources, Does.Contain(node));
            Assert.That(ResourceRegistry.ResourceCount, Is.EqualTo(1));
            Assert.That(ResourceRegistry.ActiveResourceCount, Is.EqualTo(0));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ItemPickupRegistryUnregistersDisabledPickups()
        {
            GameObject go = new GameObject("pickup");
            ItemPickupView pickup = go.AddComponent<ItemPickupView>();
            pickup.Configure("wood", 1);

            Assert.That(ItemPickupRegistry.PickupCount, Is.EqualTo(1));

            go.SetActive(false);

            Assert.That(ItemPickupRegistry.PickupCount, Is.EqualTo(0));
            CollectionAssert.DoesNotContain(ItemPickupRegistry.Pickups, pickup);

            Object.DestroyImmediate(go);
        }
    }
}
