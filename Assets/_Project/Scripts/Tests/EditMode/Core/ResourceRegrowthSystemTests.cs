using ApexShift.Core.Resources;
using NUnit.Framework;

namespace ApexShift.Tests.EditMode.Core
{
    public sealed class ResourceRegrowthSystemTests
    {
        [Test]
        public void MarkHarvested_DepletesResourceAndMarksGrowthHarvested()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("bush");
            ResourceState resource = definition.CreateState();
            ResourceGrowthState growth = new ResourceGrowthState(resource.ResourceId, "bush", resource.MaxAmount);
            ResourceRegrowthSystem system = new ResourceRegrowthSystem();

            system.MarkHarvested(resource, growth);

            Assert.IsTrue(resource.IsDepleted);
            Assert.IsTrue(growth.IsHarvested);
            Assert.AreEqual(ResourceGrowthStage.Harvested, growth.GrowthStage);
        }

        [Test]
        public void AdvanceDays_RestoresResourceWhenGrowthReachesMature()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("bush");
            ResourceState resource = definition.CreateState();
            ResourceGrowthState growth = new ResourceGrowthState(resource.ResourceId, "bush", resource.MaxAmount);
            ResourceRegrowthSystem system = new ResourceRegrowthSystem();
            system.MarkHarvested(resource, growth);

            ResourceRegrowthResult result = system.AdvanceDays(resource, growth, days: 10f);

            Assert.IsTrue(result.BecameMature);
            Assert.AreEqual(ResourceGrowthStage.Mature, growth.GrowthStage);
            Assert.IsFalse(resource.IsDepleted);
            Assert.AreEqual(resource.MaxAmount, resource.Amount);
        }

        [Test]
        public void AdvanceDays_DoesNotRegrowNonRegrowingResource()
        {
            ResourceDefinition definition = ResourceDefinition.CreateDefault("rock");
            ResourceState resource = definition.CreateState();
            ResourceGrowthState growth = new ResourceGrowthState(resource.ResourceId, "rock", resource.MaxAmount);
            ResourceRegrowthSystem system = new ResourceRegrowthSystem();
            system.MarkHarvested(resource, growth);

            ResourceRegrowthResult result = system.AdvanceDays(resource, growth, days: 100f);

            Assert.IsFalse(result.StageChanged);
            Assert.IsFalse(result.BecameMature);
            Assert.IsTrue(resource.IsDepleted);
            Assert.AreEqual(ResourceGrowthStage.Harvested, growth.GrowthStage);
        }
    }
}
