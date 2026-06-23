using System.Collections.Generic;
using ApexShift.Core.Items;
using ApexShift.Infrastructure.Data.Items;
using ApexShift.Infrastructure.Data.Mapping;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Data
{
    public class ItemDatabaseAssetMapperTests
    {
        [Test]
        public void ItemDatabaseAssetMapperCreatesCoreDatabase()
        {
            ItemDefinitionAsset wood = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            wood.ConfigureForTests("wood", "Wood", 20);
            ItemDefinitionAsset stone = ScriptableObject.CreateInstance<ItemDefinitionAsset>();
            stone.ConfigureForTests("stone", "Stone", 20);

            ItemDatabase itemDatabase = ItemDatabaseAssetMapper.ToCoreDatabase(new[] { wood, stone });

            Assert.IsTrue(itemDatabase.HasItem("wood"));
            Assert.AreEqual(20, itemDatabase.GetMaxStack("wood"));
            Assert.IsTrue(itemDatabase.HasItem("stone"));
            Assert.AreEqual(20, itemDatabase.GetMaxStack("stone"));
        }
    }
}
