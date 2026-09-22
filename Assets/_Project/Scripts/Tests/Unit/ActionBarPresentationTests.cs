using ApexShift.Presentation.Crafting;
using ApexShift.Presentation.HUD;
using ApexShift.Runtime.Player;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit
{
    public sealed class ActionBarPresentationTests
    {
        private GameObject root;

        [SetUp]
        public void SetUp() => root = new GameObject("PresentationTestRoot");

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void ActionBarViewBuildsNineSlotsAndReflectsRuntimeState()
        {
            GameObject player = new GameObject("Player").transform.SetParentAndReturn(root.transform);
            ActionBarRuntime runtime = player.AddComponent<ActionBarRuntime>();
            ActionBarView view = new GameObject("ActionBarView").AddComponent<ActionBarView>();
            view.transform.SetParent(root.transform, false);

            view.Bind(runtime);
            Assert.AreEqual(9, view.SlotCount);

            runtime.AssignItemToSlot(0, "spear");
            Assert.AreEqual("spear", view.GetComponentInChildren<ActionBarSlotView>(true).ItemId);
        }

        [Test]
        public void CraftingPanelViewOwnsPresentationVisibility()
        {
            CraftingPanelView view = new GameObject("CraftingPanelView").AddComponent<CraftingPanelView>();
            view.transform.SetParent(root.transform, false);

            view.Bind(null, null, null);
            Assert.IsFalse(view.IsVisible);
            view.Toggle();
            Assert.IsTrue(view.IsVisible);
            view.Toggle();
            Assert.IsFalse(view.IsVisible);
        }
    }

    internal static class TransformTestExtensions
    {
        public static GameObject SetParentAndReturn(this Transform child, Transform parent)
        {
            child.SetParent(parent, false);
            return child.gameObject;
        }
    }
}
