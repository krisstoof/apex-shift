using ApexShift.Core.Items;
using NUnit.Framework;

namespace ApexShift.Tests.Unit.Items
{
    public sealed class ItemDatabaseFoodTests
    {
        [Test]
        public void DefaultDatabaseMarksBerriesAndMeatAsEdible()
        {
            ItemDatabase database = ItemDatabase.CreateDefault();

            Assert.IsTrue(database.IsEdible("berries"));
            Assert.IsTrue(database.IsEdible("meat"));
            Assert.IsFalse(database.IsEdible("wood"));
        }

        [Test]
        public void EdibleDefinitionsExposeNutritionValues()
        {
            ItemDatabase database = ItemDatabase.CreateDefault();

            ItemDefinition berries = database.GetDefinition("berries");
            ItemDefinition meat = database.GetDefinition("meat");

            Assert.Greater(berries.HungerRestore, 0f);
            Assert.Greater(meat.HungerRestore, berries.HungerRestore);
            Assert.IsTrue(meat.IsUnsafeRawFood);
        }
    }
}
