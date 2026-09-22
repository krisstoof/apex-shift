using System.Collections.Generic;
using System.Reflection;
using ApexShift.Core.Crafting;
using ApexShift.Core.Items;
using ApexShift.Presentation.Crafting;
using ApexShift.Presentation.HUD;
using ApexShift.Presentation.Icons;
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
        public void ActionBarViewTracksActiveStateAndRebindsWithoutOldRuntime()
        {
            GameObject playerA = new GameObject("PlayerA");
            GameObject playerB = new GameObject("PlayerB");
            playerA.transform.SetParent(root.transform, false);
            playerB.transform.SetParent(root.transform, false);
            ActionBarRuntime runtimeA = playerA.AddComponent<ActionBarRuntime>();
            ActionBarRuntime runtimeB = playerB.AddComponent<ActionBarRuntime>();
            runtimeA.SetSlotCountForTests(3);
            runtimeB.SetSlotCountForTests(9);
            runtimeA.AssignItemToSlot(0, "spear");
            runtimeA.AssignItemToSlot(1, "bow");
            ActionBarView view = new GameObject("ActionBarView").AddComponent<ActionBarView>();
            view.transform.SetParent(root.transform, false);

            view.Bind(runtimeA);
            runtimeA.SetActiveSlot(1);
            ActionBarSlotView active = FindSlot(view, 1);
            Assert.AreEqual("bow", active.ItemId);
            view.Bind(runtimeB);
            Assert.AreEqual(9, view.SlotCount);
            runtimeA.AssignItemToSlot(0, "axe");
            Assert.IsEmpty(FindSlot(view, 0).ItemId);
            runtimeB.AssignItemToSlot(0, "pickaxe");
            Assert.AreEqual("pickaxe", FindSlot(view, 0).ItemId);
            runtimeB.SetSlotCountForTests(2);
            view.Bind(runtimeB);
            Assert.AreEqual(2, view.SlotCount);
        }

        [Test]
        public void ItemIconCatalogSupportsCaseInsensitiveKnownAndFallbackLookup()
        {
            ItemIconCatalog catalog = ScriptableObject.CreateInstance<ItemIconCatalog>();
            Texture2D texture = new Texture2D(2, 2);
            Sprite known = Sprite.Create(texture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            Sprite fallback = Sprite.Create(new Texture2D(2, 2), new Rect(0, 0, 2, 2), Vector2.one * 0.5f);
            FieldInfo fallbackField = typeof(ItemIconCatalog).GetField("fallbackIcon", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo entriesField = typeof(ItemIconCatalog).GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic);
            fallbackField.SetValue(catalog, fallback);
            entriesField.SetValue(catalog, new List<ItemIconEntry> { new ItemIconEntry { itemId = "Spear", sprite = known } });

            Assert.IsTrue(catalog.TryGetIcon("sPeAr", out Sprite knownResult));
            Assert.AreSame(known, knownResult);
            Assert.IsTrue(catalog.TryGetIcon("unknown", out Sprite fallbackResult));
            Assert.AreSame(fallback, fallbackResult);
            Object.DestroyImmediate(known.texture);
            Object.DestroyImmediate(fallback.texture);
            Object.DestroyImmediate(catalog);
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

        [Test]
        public void CraftingPanelViewUsesBoundRuntimeAndRebinds()
        {
            GameObject first = new GameObject("CraftingPlayerA");
            GameObject second = new GameObject("CraftingPlayerB");
            first.transform.SetParent(root.transform, false);
            second.transform.SetParent(root.transform, false);
            PlayerInventoryRuntime firstInventory = first.AddComponent<PlayerInventoryRuntime>();
            PlayerCraftingRuntime firstCrafting = first.AddComponent<PlayerCraftingRuntime>();
            firstCrafting.SetInventoryRuntime(firstInventory);
            firstInventory.Inventory.AddItem("wood", 2);
            firstInventory.Inventory.AddItem("stone", 1);
            firstInventory.Inventory.AddItem("fiber", 1);
            CraftingPanelView view = new GameObject("CraftingPanelView").AddComponent<CraftingPanelView>();
            view.transform.SetParent(root.transform, false);
            view.Bind(firstCrafting, firstInventory, null);

            CraftingResult result = view.CraftRecipe("spear");
            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(1, firstInventory.Inventory.GetAmount("spear"));
            Assert.That(view.StatusMessage, Does.Contain("Crafted"));

            PlayerInventoryRuntime secondInventory = second.AddComponent<PlayerInventoryRuntime>();
            PlayerCraftingRuntime secondCrafting = second.AddComponent<PlayerCraftingRuntime>();
            secondCrafting.SetInventoryRuntime(secondInventory);
            view.Bind(secondCrafting, secondInventory, null);
            Assert.IsFalse(view.CraftRecipe("spear").Succeeded);
            Assert.AreEqual(1, firstInventory.Inventory.GetAmount("spear"));
            Object.DestroyImmediate(first);
        }

        private static ActionBarSlotView FindSlot(ActionBarView view, int index)
        {
            foreach (ActionBarSlotView slot in view.GetComponentsInChildren<ActionBarSlotView>(true))
                if (slot.SlotIndex == index) return slot;
            return null;
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
