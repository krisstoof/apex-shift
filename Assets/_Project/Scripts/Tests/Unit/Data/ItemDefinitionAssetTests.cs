using ApexShift.Core.Items;
using ApexShift.Infrastructure.Data.Items;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Data
{
    public class ItemDefinitionAssetTests
    {
        [Test]
        public void ItemDefinitionAssetMapsToCore()
        {
            ItemDefinitionAsset asset = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            asset.ConfigureForTests("wood", "Wood", 20);

            ItemDefinition definition = asset.ToCoreDefinition();

            Assert.AreEqual("wood", definition.Id.ToString());
            Assert.AreEqual("Wood", definition.DisplayName);
            Assert.AreEqual(20, definition.MaxStackSize);
        }
    }
}
