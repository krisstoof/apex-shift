using ApexShift.Core.Resources;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Resources
{
    public class ResourceRegrowthSystemTests
    {
        private ResourceRegrowthSystem regrowthSystem;
        private ResourceRegrowthRules rules;

        [SetUp]
        public void SetUp()
        {
            rules = new ResourceRegrowthRules();
            regrowthSystem = new ResourceRegrowthSystem(rules);
        }

        [Test]
        public void HarvestedResourceMovesThroughStages()
        {
            ResourceState resource = ResourceDefinition.CreateDefault("tree").CreateState();
            ResourceGrowthState growth = new ResourceGrowthState("tree-1", "tree", resource.MaxAmount);

            regrowthSystem.MarkHarvested(resource, growth);

            Assert.IsTrue(resource.IsDepleted);
            Assert.AreEqual(ResourceGrowthStage.Harvested, growth.GrowthStage);

            // Advance 1 day (should move to Sprout if tree takes 3 days total for 3 stages)
            // tree: 3 days / 3 stages = 1 day per stage
            regrowthSystem.AdvanceDays(resource, growth, 1f);
            Assert.AreEqual(ResourceGrowthStage.Sprout, growth.GrowthStage);
            Assert.IsFalse(resource.IsInteractable);

            // Advance 1 more day
            regrowthSystem.AdvanceDays(resource, growth, 1f);
            Assert.AreEqual(ResourceGrowthStage.Young, growth.GrowthStage);

            // Advance 1 more day
            regrowthSystem.AdvanceDays(resource, growth, 1f);
            Assert.AreEqual(ResourceGrowthStage.Mature, growth.GrowthStage);
            
            // Check if resource is restored
            Assert.IsFalse(resource.IsDepleted);
            Assert.AreEqual(resource.MaxAmount, resource.Amount);
            Assert.IsTrue(resource.IsInteractable);
        }

        [Test]
        public void RocksDoNotRegrow()
        {
            ResourceState resource = ResourceDefinition.CreateDefault("rock").CreateState();
            ResourceGrowthState growth = new ResourceGrowthState("rock-1", "rock", resource.MaxAmount);

            regrowthSystem.MarkHarvested(resource, growth);
            Assert.IsTrue(resource.IsDepleted);

            // Advance many days
            regrowthSystem.AdvanceDays(resource, growth, 100f);
            
            Assert.AreEqual(ResourceGrowthStage.Harvested, growth.GrowthStage);
            Assert.IsTrue(resource.IsDepleted);
        }

        [Test]
        public void AdvanceTimeCalculatesDaysCorrectily()
        {
            ResourceState resource = ResourceDefinition.CreateDefault("bush").CreateState();
            ResourceGrowthState growth = new ResourceGrowthState("bush-1", "bush", resource.MaxAmount);
            // bush: 2 days total, 3 stages -> 2/3 days per stage = 0.666
            
            regrowthSystem.MarkHarvested(resource, growth);

            // 1 day in seconds (assuming 60s per day for test)
            // Add a tiny bit extra to handle floating point precision
            regrowthSystem.AdvanceTime(resource, growth, 60.01f, 60f);
            
            // 1 day passed, bush needs 0.66 per stage, so it should be at least Young (Stage 2)
            // 1 / (2/3) = 1.5 stages. So Stage 0 -> Stage 1 (Sprout). 
            Assert.AreEqual(ResourceGrowthStage.Sprout, growth.GrowthStage);
            
            // Pass another 1 day
            regrowthSystem.AdvanceTime(resource, growth, 60.01f, 60f);
            Assert.AreEqual(ResourceGrowthStage.Mature, growth.GrowthStage);
}

        [Test]
        public void SaveDataPreservesState()
        {
            ResourceGrowthState growth = new ResourceGrowthState("tree-1", "tree", 4);
            growth.MarkHarvested();
            growth.SetGrowthStage(ResourceGrowthStage.Young);
            growth.AdvanceProgress(0.5f, 1.0f);

            ResourceGrowthSaveData data = growth.ToSaveData();
            
            ResourceGrowthState newGrowth = new ResourceGrowthState("temp", "temp", 0);
            newGrowth.FromSaveData(data);

            Assert.AreEqual(growth.ResourceId, newGrowth.ResourceId);
            Assert.AreEqual(growth.ResourceKind, newGrowth.ResourceKind);
            Assert.AreEqual(growth.GrowthStage, newGrowth.GrowthStage);
            Assert.AreEqual(growth.GrowthProgressDays, newGrowth.GrowthProgressDays);
            Assert.AreEqual(growth.IsHarvested, newGrowth.IsHarvested);
        }

        [Test]
        public void ForceFullRegrowthWorks()
        {
            ResourceState resource = ResourceDefinition.CreateDefault("tree").CreateState();
            ResourceGrowthState growth = new ResourceGrowthState("tree-1", "tree", resource.MaxAmount);

            regrowthSystem.MarkHarvested(resource, growth);
            Assert.IsTrue(resource.IsDepleted);

            regrowthSystem.ForceFullRegrowth(resource, growth);

            Assert.IsFalse(resource.IsDepleted);
            Assert.AreEqual(ResourceGrowthStage.Mature, growth.GrowthStage);
            Assert.IsTrue(resource.IsInteractable);
        }
    }
}
