using ApexShift.Core.Crafting;
using ApexShift.Core.Items;
using ApexShift.Runtime.Debugging;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerCraftingRuntime : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private PlayerInventoryRuntime inventoryRuntime;

        [SerializeField]
        private string defaultRecipeId = "spear";

        [SerializeField]
        private bool logToConsole = true;

        private ItemDatabase itemDatabase;
        private RecipeDatabase recipeDatabase;
        private CraftingSystem craftingSystem;
        private bool subscribed;

        public string DefaultRecipeId => string.IsNullOrWhiteSpace(defaultRecipeId) ? "spear" : defaultRecipeId.Trim();
        public CraftingResult LastResult { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            EnsureCore();
        }

        private void OnEnable()
        {
            ResolveReferences();
            SubscribeInput();
        }

        private void OnDisable()
        {
            UnsubscribeInput();
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            if (inputReader == reader)
            {
                return;
            }

            UnsubscribeInput();
            inputReader = reader;
            if (enabled)
            {
                SubscribeInput();
            }
        }

        public void SetInventoryRuntime(PlayerInventoryRuntime runtime)
        {
            inventoryRuntime = runtime;
            if (inventoryRuntime != null)
            {
                inventoryRuntime.EnsureInitialized();
            }
        }

        public void SetDefaultRecipeId(string recipeId)
        {
            defaultRecipeId = string.IsNullOrWhiteSpace(recipeId) ? "spear" : recipeId.Trim();
        }

        public CraftingResult CraftDefaultRecipe()
        {
            return CraftRecipe(DefaultRecipeId);
        }

        public CraftingResult CraftRecipe(string recipeId)
        {
            ResolveReferences();
            EnsureCore();

            string resolvedRecipeId = string.IsNullOrWhiteSpace(recipeId) ? DefaultRecipeId : recipeId.Trim();
            RecipeId normalizedRecipeId = recipeDatabase.NormalizeRecipeId(resolvedRecipeId);

            if (inventoryRuntime == null)
            {
                Debug.LogWarning("[Crafting] Could not craft because PlayerInventoryRuntime is missing.", this);
                LastResult = CraftingResult.Failed(CraftingResultStatus.InvalidRecipe, normalizedRecipeId);
                LogCraftingResult(LastResult);
                return LastResult;
            }

            inventoryRuntime.EnsureInitialized();
            if (RuntimeDebugSettings.FreeCraftingEnabled)
            {
                LastResult = CraftRecipeWithoutCosts(resolvedRecipeId, normalizedRecipeId);
                return LastResult;
            }

            LastResult = craftingSystem.Craft(inventoryRuntime.Inventory, resolvedRecipeId);
            LogCraftingResult(LastResult);
            return LastResult;
        }

        private void OnOpenCraftingPressed()
        {
            CraftingPanelUI panel = GetComponent<CraftingPanelUI>();
            if (panel == null) CraftDefaultRecipe();
        }

        private void ResolveReferences()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (inventoryRuntime == null)
            {
                inventoryRuntime = GetComponent<PlayerInventoryRuntime>();
            }

            if (inventoryRuntime != null)
            {
                inventoryRuntime.EnsureInitialized();
            }
        }

        private void EnsureCore()
        {
            if (itemDatabase != null && recipeDatabase != null && craftingSystem != null)
            {
                return;
            }

            itemDatabase = ItemDatabase.CreateDefault();
            recipeDatabase = RecipeDatabase.CreateDefault(itemDatabase);
            craftingSystem = new CraftingSystem(recipeDatabase, itemDatabase);
        }

        private CraftingResult CraftRecipeWithoutCosts(string resolvedRecipeId, RecipeId normalizedRecipeId)
        {
            if (recipeDatabase == null)
            {
                EnsureCore();
            }

            RecipeDefinition recipe = recipeDatabase.GetRecipe(resolvedRecipeId);
            if (recipe == null)
            {
                LastResult = CraftingResult.Failed(CraftingResultStatus.InvalidRecipe, normalizedRecipeId);
                LogCraftingResult(LastResult);
                return LastResult;
            }

            if (inventoryRuntime != null && inventoryRuntime.Inventory != null)
            {
                if (!inventoryRuntime.Inventory.AddItemFullStack(recipe.ResultItemId, recipe.ResultAmount))
                {
                    LastResult = CraftingResult.Failed(CraftingResultStatus.InventoryFull, normalizedRecipeId);
                    LogCraftingResult(LastResult);
                    return LastResult;
                }
            }

            LastResult = CraftingResult.Success(normalizedRecipeId, recipe.ResultItemId, recipe.ResultAmount, new RecipeIngredient[0]);
            LogCraftingResult(LastResult);
            return LastResult;
        }

        private void SubscribeInput()
        {
            if (subscribed || inputReader == null)
            {
                return;
            }

            inputReader.OpenCraftingPressed += OnOpenCraftingPressed;
            subscribed = true;
        }

        private void UnsubscribeInput()
        {
            if (!subscribed || inputReader == null)
            {
                subscribed = false;
                return;
            }

            inputReader.OpenCraftingPressed -= OnOpenCraftingPressed;
            subscribed = false;
        }

        private void LogCraftingResult(CraftingResult result)
        {
            if (!logToConsole || result == null)
            {
                return;
            }

            if (result.Succeeded)
            {
                Debug.Log($"[Crafting] Crafted {result.ResultItemId} x{result.CraftedAmount} using recipe '{result.RecipeId}'.", this);
                return;
            }

            Debug.Log($"[Crafting] Could not craft recipe '{result.RecipeId}'. Status: {result.Status}.", this);
        }
    }
}
