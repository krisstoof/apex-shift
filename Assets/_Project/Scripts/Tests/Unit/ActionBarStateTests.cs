using NUnit.Framework;
using ApexShift.Runtime.Player;

namespace ApexShift.Tests.Unit
{
    public sealed class ActionBarStateTests
    {
        [Test]
        public void InitialStateHasNineEmptySlotsAndNoActiveItem()
        {
            ActionBarState state = new ActionBarState();
            Assert.AreEqual(9, state.SlotCount);
            Assert.AreEqual(-1, state.ActiveSlotIndex);
            Assert.IsEmpty(state.ActiveItemId);
        }

        [Test]
        public void AssignAndActivateActionItemRaisesStateEvents()
        {
            ActionBarState state = new ActionBarState();
            int changed = 0;
            int activeSlot = -2;
            state.Changed += () => changed++;
            state.ActiveSlotChanged += (slot, item) => activeSlot = slot;

            Assert.IsTrue(state.AssignItem(2, "SpEaR"));
            Assert.AreEqual("spear", state.GetItem(2));
            Assert.AreEqual(2, state.ActiveSlotIndex);
            Assert.AreEqual(2, activeSlot);
            Assert.GreaterOrEqual(changed, 2);
        }

        [Test]
        public void RejectsResourcesAndSupportsClearing()
        {
            ActionBarState state = new ActionBarState();
            Assert.IsFalse(state.AssignItem(0, "wood"));
            Assert.IsTrue(state.AssignItem(0, "axe"));
            state.ClearSlot(0);
            Assert.IsEmpty(state.GetItem(0));
            state.ClearActiveSlot();
            Assert.AreEqual(-1, state.ActiveSlotIndex);
        }
    }
}
