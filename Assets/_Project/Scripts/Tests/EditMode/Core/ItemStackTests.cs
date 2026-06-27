using ApexShift.Core.Inventory;
using NUnit.Framework;

namespace ApexShift.Tests.EditMode.Core
{
    public sealed class ItemStackTests
    {
        [Test]
        public void AddAmount_FillsEmptyStackAndReturnsRemainder()
        {
            ItemStack stack = new ItemStack();

            int remainder = stack.AddAmount("wood", 25, maxStack: 20);

            Assert.AreEqual("wood", stack.ItemId);
            Assert.AreEqual(20, stack.Amount);
            Assert.AreEqual(5, remainder);
        }

        [Test]
        public void AddAmount_DoesNotMixDifferentItems()
        {
            ItemStack stack = new ItemStack();
            stack.AddAmount("wood", 10, maxStack: 20);

            int remainder = stack.AddAmount("stone", 5, maxStack: 20);

            Assert.AreEqual("wood", stack.ItemId);
            Assert.AreEqual(10, stack.Amount);
            Assert.AreEqual(5, remainder);
        }

        [Test]
        public void RemoveAmount_ClearsStackWhenAmountReachesZero()
        {
            ItemStack stack = new ItemStack();
            stack.AddAmount("wood", 5, maxStack: 20);

            int removed = stack.RemoveAmount(5);

            Assert.AreEqual(5, removed);
            Assert.IsTrue(stack.IsEmpty);
            Assert.AreEqual(0, stack.Amount);
        }

        [Test]
        public void SetStack_WithInvalidAmount_ClearsStack()
        {
            ItemStack stack = new ItemStack();
            stack.SetStack("wood", 3);

            stack.SetStack("wood", 0);

            Assert.IsTrue(stack.IsEmpty);
        }
    }
}
