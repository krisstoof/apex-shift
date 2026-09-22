using System.Collections.Generic;
using ApexShift.Core.Crafting;
using ApexShift.Core.Items;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.Crafting
{
    public sealed class CraftingPanelView : MonoBehaviour
    {
        private static readonly string[] RecipeIds = { "spear", "bow", "arrow", "torch", "axe", "pickaxe" };
        private PlayerInputReader inputReader;
        private PlayerCraftingRuntime craftingRuntime;
        private PlayerInventoryRuntime inventoryRuntime;
        private ItemDatabase itemDatabase;
        private RecipeDatabase recipeDatabase;
        private GameObject panel;
        private Text statusText;
        private readonly Dictionary<string, Text> labels = new Dictionary<string, Text>();
        public bool IsVisible => panel != null && panel.activeSelf;
        public string StatusMessage => statusText != null ? statusText.text : string.Empty;

        public void Bind(PlayerCraftingRuntime crafting, PlayerInventoryRuntime inventory, PlayerInputReader input)
        {
            Unbind();
            craftingRuntime = crafting; inventoryRuntime = inventory; inputReader = input;
            EnsureCore(); EnsureUI(); Subscribe(); Refresh();
        }

        public void Unbind()
        {
            if (inputReader != null) inputReader.OpenCraftingPressed -= Toggle;
            inputReader = null; craftingRuntime = null; inventoryRuntime = null;
        }

        public void Toggle()
        {
            EnsureUI(); panel.SetActive(!panel.activeSelf); if (panel.activeSelf) Refresh();
        }

        public CraftingResult CraftRecipe(string recipeId)
        {
            if (craftingRuntime == null) return null;
            CraftingResult result = craftingRuntime.CraftRecipe(recipeId);
            if (statusText != null)
                statusText.text = result != null && result.Succeeded ? $"Crafted {result.ResultItemId} x{result.CraftedAmount}" : $"Cannot craft {recipeId}";
            Refresh();
            return result;
        }

        private void OnDestroy() => Unbind();
        private void Subscribe() { if (inputReader != null) { inputReader.OpenCraftingPressed -= Toggle; inputReader.OpenCraftingPressed += Toggle; } }
        private void EnsureCore() { if (itemDatabase == null) { itemDatabase = ItemDatabase.CreateDefault(); recipeDatabase = RecipeDatabase.CreateDefault(itemDatabase); } }

        private void Refresh()
        {
            EnsureCore();
            foreach (string recipeId in RecipeIds)
            {
                if (!labels.TryGetValue(recipeId, out Text label)) continue;
                RecipeDefinition recipe = recipeDatabase.GetRecipe(recipeId);
                if (recipe == null) continue;
                string text = $"{recipe.ResultItemId} x{recipe.ResultAmount}\nRequires: ";
                foreach (RecipeIngredient ingredient in recipe.Ingredients)
                {
                    int owned = inventoryRuntime != null && inventoryRuntime.Inventory != null ? inventoryRuntime.Inventory.GetAmount(ingredient.ItemId.ToString()) : 0;
                    text += $"{ingredient.ItemId} {owned}/{ingredient.Amount}; ";
                }
                label.text = text;
            }
        }

        private void EnsureUI()
        {
            if (panel != null) return;
            panel = new GameObject("CraftingPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false); panel.SetActive(false);
            panel.GetComponent<Image>().color = new Color(0.05f, 0.055f, 0.06f, 0.88f);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(680f, 520f);
            CreateText("Title", "Crafting - Items & Weapons", 24, TextAnchor.MiddleCenter, new Vector2(0f, 220f), panel.transform);
            statusText = CreateText("Status", "Select recipe.", 15, TextAnchor.MiddleLeft, new Vector2(0f, -230f), panel.transform);
            for (int i = 0; i < RecipeIds.Length; i++)
            {
                string recipeId = RecipeIds[i]; Button button = CreateButton(recipeId, panel.transform);
                button.onClick.AddListener(() => CraftRecipe(recipeId));
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0f, 1f); buttonRect.anchorMax = new Vector2(1f, 1f); buttonRect.sizeDelta = new Vector2(-48f, 54f); buttonRect.anchoredPosition = new Vector2(0f, -78f - i * 62f);
            }
        }

        private Button CreateButton(string recipeId, Transform parent)
        {
            GameObject go = new GameObject($"Recipe_{recipeId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false); go.GetComponent<Image>().color = new Color(0.16f, 0.17f, 0.18f, 0.95f);
            labels[recipeId] = CreateText("Label", recipeId, 15, TextAnchor.MiddleLeft, Vector2.zero, go.transform);
            return go.GetComponent<Button>();
        }

        private Text CreateText(string name, string value, int size, TextAnchor anchor, Vector2 position, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.sizeDelta = new Vector2(-40f, 44f); rect.anchoredPosition = position;
            Text text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value; text.fontSize = size; text.alignment = anchor; text.color = Color.white; text.horizontalOverflow = HorizontalWrapMode.Wrap; return text;
        }
    }
}
