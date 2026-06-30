using ApexShift.Runtime.UI.Debugging;
using ApexShift.Runtime.UI.Snapshots;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.UI
{
    public class DebugPanelPresenterTests
    {
        [Test]
        public void FormatSnapshot_UsesReadableSectionsAndValues()
        {
            GameSnapshot snapshot = new GameSnapshot(
                new InventorySnapshot(8, 5, new[]
                {
                    new InventoryItemSnapshot("apple", 3),
                    new InventoryItemSnapshot("torch", 1)
                }),
                new SurvivalSnapshot(75f, 40f, 20f, 88f, "healthy", true, false),
                new WorldDebugSnapshot(1234, new Vector3(1f, 2f, 3f), true, 9, 4, 2, 1, 1, 3, 1, 2, 58.2f, 12.5f),
                new DayNightSnapshot(3, 0.75f, 18f, false, 0f, "Evening"),
                12.5f);

            string formatted = DebugPanelPresenter.FormatSnapshot(snapshot);

            StringAssert.Contains("=== GAME SNAPSHOT ===", formatted);
            StringAssert.Contains("seed: 1234", formatted);
            StringAssert.Contains("day: 3  time: 18:00  phase: Evening", formatted);
            StringAssert.Contains("resources: 9", formatted);
            StringAssert.Contains("creatures: 4  hungry: 2", formatted);
            StringAssert.Contains("hp/hun/sta/rest: 75/40/20/88", formatted);
            StringAssert.Contains("slots: 3/8  empty: 5", formatted);
            StringAssert.Contains("- apple: 3", formatted);
            StringAssert.Contains("- torch: 1", formatted);
        }

        [Test]
        public void FormatSnapshot_ReplacesNullWithEmptySnapshot()
        {
            string formatted = DebugPanelPresenter.FormatSnapshot(null);

            StringAssert.Contains("=== GAME SNAPSHOT ===", formatted);
            StringAssert.Contains("seed: 0", formatted);
            StringAssert.Contains("slots: 0/0  empty: 0", formatted);
        }

        [Test]
        public void FormatSnapshot_OrdersInventoryItemsAlphabeticallyById()
        {
            GameSnapshot snapshot = new GameSnapshot(
                new InventorySnapshot(4, 2, new[]
                {
                    new InventoryItemSnapshot("zebra", 1),
                    new InventoryItemSnapshot("apple", 2),
                    new InventoryItemSnapshot("mushroom", 3)
                }),
                SurvivalSnapshot.Empty,
                WorldDebugSnapshot.Empty,
                0f);

            string formatted = DebugPanelPresenter.FormatSnapshot(snapshot);
            int appleIndex = formatted.IndexOf("- apple: 2", System.StringComparison.Ordinal);
            int mushroomIndex = formatted.IndexOf("- mushroom: 3", System.StringComparison.Ordinal);
            int zebraIndex = formatted.IndexOf("- zebra: 1", System.StringComparison.Ordinal);

            Assert.That(appleIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(mushroomIndex, Is.GreaterThan(appleIndex));
            Assert.That(zebraIndex, Is.GreaterThan(mushroomIndex));
        }
    }
}
