using ApexShift.Core.Resources;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Resources
{
    public sealed class ResourceDefinitionParityTests
    {
        [Test]
        public void BerryBushIsHerbivoreEdibleAndRegrows()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("berry_bush");
            ResourceState state = definition.CreateState();

            Assert.IsFalse(definition.PlayerHarvestable);
            Assert.IsTrue(definition.EdibleByHerbivores);
            Assert.Greater(definition.FoodValue, 0f);
            Assert.Greater(definition.RegrowthDays, 0);
            Assert.IsTrue(state.IsFoodAvailableForHerbivores);
        }

        [Test]
        public void GrassPatchIsRenderOnlyFoodSource()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("grass_patch");
            ResourceState state = definition.CreateState();

            Assert.IsTrue(definition.RenderOnly);
            Assert.IsTrue(definition.EdibleByHerbivores);
            Assert.IsFalse(state.IsInteractable);
            Assert.IsTrue(state.IsFoodAvailableForHerbivores);
        }

        [Test]
        public void MeatDropHasPickupPriorityAndIsDrop()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("meat_drop");
            ResourceState state = definition.CreateState();

            Assert.AreEqual("meat", definition.ItemId);
            Assert.Greater(definition.PickupPriority, 0);
            Assert.IsTrue(state.IsDrop);
        }

        [Test]
        public void DepletedRegrowingResourceRestoresAfterEnoughDays()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("berry_bush");
            ResourceState state = definition.CreateState();

            state.MarkDepleted();

            Assert.IsTrue(state.IsDepleted);
            Assert.IsTrue(state.AdvanceGrowthDays(definition.RegrowthDays));
            Assert.IsFalse(state.IsDepleted);
            Assert.AreEqual(1f, state.GrowthProgress, 0.001f);
        }

        [Test]
        public void CatalogContainsGodotResourceKinds()
        {
            string[] kinds =
            {
                "conifer_tree",
                "leafy_tree",
                "dry_tree",
                "rock",
                "bush",
                "dry_bush",
                "small_bush",
                "berry_bush",
                "grass_patch",
                "dense_grass",
                "meat_drop",
                "bone_drop",
                "item_drop"
            };

            foreach (string kind in kinds)
            {
                Assert.DoesNotThrow(() => ResourceDefinition.CreateDefault(kind), kind);
            }
        }
    }
}
