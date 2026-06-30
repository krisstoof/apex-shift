using System.Collections.Generic;
using System.Text;
using ApexShift.Core.Crafting;
using ApexShift.Core.Items;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class CraftingPanelUI : MonoBehaviour
    {
        private static readonly string[] ItemAndWeaponRecipes = { "spear", "bow", "arrow", "torch", "axe", "pickaxe" };

        [SerializeField] private PlayerInputReader inputReader;
        [SerializeField] private PlayerCraftingRuntime craftingRuntime;
        [SerializeField] private PlayerInventoryRuntime inventoryRuntime;

        private ItemDatabase itemDatabase;
        private RecipeDatabase recipeDatabase;
        private Canvas canvas;
        private GameObject panel;
        private Text statusText;
        private readonly Dictionary<string, Text> buttonTexts = new Dictionary<string, Text>();
        private bool subscribed;

        private void Awake()
        {
            ResolveReferences();
            EnsureCore();
            EnsureUI();
            SetVisible(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            if (inputReader == reader) return;
            Unsubscribe();
            inputReader = reader;
            if (enabled) Subscribe();
        }

        public void SetCraftingRuntime(PlayerCraftingRuntime runtime) => craftingRuntime = runtime;
        public void SetInventoryRuntime(PlayerInventoryRuntime runtime) => inventoryRuntime = runtime;

        public void Toggle()
        {
            EnsureUI();
            SetVisible(panel == null || !panel.activeSelf);
        }

        private void SetVisible(bool visible)
        {
            EnsureUI();
            if (panel != null) panel.SetActive(visible);
            if (visible) Refresh();
        }

        private void Refresh()
        {
            EnsureCore();
            EnsureUI();
            inventoryRuntime?.EnsureInitialized();

            foreach (string recipeId in ItemAndWeaponRecipes)
            {
                if (buttonTexts.TryGetValue(recipeId, out Text label) && label != null)
                {
                    label.text = BuildRecipeLabel(recipeId);
                }
            }
        }

        private string BuildRecipeLabel(string recipeId)
        {
            RecipeDefinition recipe;
            try { recipe = recipeDatabase.GetRecipe(recipeId); }
            catch { return $"{recipeId}\nmissing recipe"; }

            string resultId = recipe.ResultItemId.ToString();
            string resultName = itemDatabase.GetDisplayName(resultId);
            if (string.IsNullOrWhiteSpace(resultName)) resultName = resultId;

            StringBuilder sb = new StringBuilder();
            sb.Append(resultName).Append(" x").Append(recipe.ResultAmount).AppendLine();
            sb.Append("Requires: ");
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                string itemId = ingredient.ItemId.ToString();
                string itemName = itemDatabase.GetDisplayName(itemId);
                if (string.IsNullOrWhiteSpace(itemName)) itemName = itemId;

                int owned = inventoryRuntime != null && inventoryRuntime.Inventory != null ? inventoryRuntime.Inventory.GetAmount(itemId) : 0;
                sb.Append(owned >= ingredient.Amount ? "✓ " : "✗ ");
                sb.Append(itemName).Append(" ").Append(owned).Append("/").Append(ingredient.Amount).Append("; ");
            }
            return sb.ToString();
        }

        private void Craft(string recipeId)
        {
            if (craftingRuntime == null)
            {
                if (statusText != null) statusText.text = "Crafting runtime missing.";
                return;
            }

            CraftingResult result = craftingRuntime.CraftRecipe(recipeId);
            if (statusText != null)
            {
                statusText.text = result != null && result.Succeeded
                    ? $"Crafted {result.ResultItemId} x{result.CraftedAmount}"
                    : $"Cannot craft {recipeId}: {(result != null ? result.Status.ToString() : "unknown")}";
            }
            Refresh();
        }

        private void EnsureUI()
        {
            if (canvas != null && panel != null) return;

            GameObject canvasGo = new GameObject("CraftingCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 55;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            panel = new GameObject("CraftingPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            panel.SetActive(false);
            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.055f, 0.06f, 0.88f);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(680f, 520f);
            rect.anchoredPosition = Vector2.zero;

            Text title = CreateText(panel.transform, "Title", "Crafting – Items & Weapons", 24, TextAnchor.MiddleCenter);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.sizeDelta = new Vector2(-32f, 42f);
            titleRect.anchoredPosition = new Vector2(0f, -28f);

            statusText = CreateText(panel.transform, "Status", "Select recipe.", 15, TextAnchor.MiddleLeft);
            RectTransform statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 0f);
            statusRect.anchorMax = new Vector2(1f, 0f);
            statusRect.sizeDelta = new Vector2(-40f, 42f);
            statusRect.anchoredPosition = new Vector2(0f, 26f);

            float startY = -78f;
            float rowHeight = 62f;
            int index = 0;
            foreach (string recipeId in ItemAndWeaponRecipes)
            {
                string capturedRecipeId = recipeId;
                Button button = CreateButton(panel.transform, $"Recipe_{capturedRecipeId}", capturedRecipeId);
                RectTransform buttonRect = button.GetComponent<RectTransform>();
                buttonRect.anchorMin = new Vector2(0f, 1f);
                buttonRect.anchorMax = new Vector2(1f, 1f);
                buttonRect.sizeDelta = new Vector2(-48f, 54f);
                buttonRect.anchoredPosition = new Vector2(0f, startY - index * rowHeight);
                button.onClick.AddListener(() => Craft(capturedRecipeId));
                index++;
            }
        }

        private Button CreateButton(Transform parent, string name, string recipeId)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.17f, 0.18f, 0.95f);
            Button button = go.AddComponent<Button>();

            Text text = CreateText(go.transform, "Label", recipeId, 15, TextAnchor.MiddleLeft);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(14f, 4f);
            textRect.offsetMax = new Vector2(-14f, -4f);
            buttonTexts[recipeId] = text;
            return button;
        }

        private Text CreateText(Transform parent, string name, string text, int fontSize, TextAnchor anchor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text label = go.AddComponent<Text>();
            label.text = text;
            label.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = anchor;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private void ResolveReferences()
        {
            if (inputReader == null) inputReader = GetComponent<PlayerInputReader>();
            if (craftingRuntime == null) craftingRuntime = GetComponent<PlayerCraftingRuntime>();
            if (inventoryRuntime == null) inventoryRuntime = GetComponent<PlayerInventoryRuntime>();
        }

        private void EnsureCore()
        {
            if (itemDatabase != null && recipeDatabase != null) return;
            itemDatabase = ItemDatabase.CreateDefault();
            recipeDatabase = RecipeDatabase.CreateDefault(itemDatabase);
        }

        private void Subscribe()
        {
            if (subscribed || inputReader == null) return;
            inputReader.OpenCraftingPressed += Toggle;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed || inputReader == null)
            {
                subscribed = false;
                return;
            }
            inputReader.OpenCraftingPressed -= Toggle;
            subscribed = false;
        }
    }
}
