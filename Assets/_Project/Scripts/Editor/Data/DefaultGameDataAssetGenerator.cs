using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApexShift.Infrastructure.Data.Items;
using ApexShift.Infrastructure.Data.Mapping;
using ApexShift.Infrastructure.Data.Recipes;
using UnityEditor;
using UnityEngine;

namespace ApexShift.EditorTools.Data
{
    public static class DefaultGameDataAssetGenerator
    {
        private const string ItemsFolder = "Assets/_Project/Data/Items";
        private const string RecipesFolder = "Assets/_Project/Data/Recipes";

        [MenuItem("Tools/Apex Shift/Data/Create Default Item And Recipe Assets")]
        public static void CreateDefaultItemAndRecipeAssets()
        {
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder(ItemsFolder);
            EnsureFolder(RecipesFolder);

            List<ItemDefinitionAsset> itemAssets = CreateOrUpdateDefaultItems();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            List<RecipeDefinitionAsset> recipeAssets = CreateOrUpdateDefaultRecipes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ItemDatabaseAssetMapper.ToCoreDatabase(itemAssets);
            RecipeDatabaseAssetMapper.ToCoreDatabase(recipeAssets, ItemDatabaseAssetMapper.ToCoreDatabase(itemAssets));
        }

        private static List<ItemDefinitionAsset> CreateOrUpdateDefaultItems()
        {
            return DefaultItemSpecs.Select(spec => CreateOrUpdateAsset<ItemDefinitionAsset>(
                Path.Combine(ItemsFolder, spec.FileName + ".asset").Replace('\\', '/'),
                asset =>
                {
                    asset.ConfigureForTests(spec.ItemId, spec.DisplayName, spec.MaxStackSize);
                })).ToList();
        }

        private static List<RecipeDefinitionAsset> CreateOrUpdateDefaultRecipes()
        {
            return DefaultRecipeSpecs.Select(spec => CreateOrUpdateAsset<RecipeDefinitionAsset>(
                Path.Combine(RecipesFolder, spec.FileName + ".asset").Replace('\\', '/'),
                asset =>
                {
                    List<RecipeIngredientAsset> ingredients = spec.Ingredients.Select(pair =>
                    {
                        RecipeIngredientAsset ingredient = new RecipeIngredientAsset();
                        ingredient.ConfigureForTests(pair.ItemId, pair.Amount);
                        return ingredient;
                    }).ToList();
                    asset.ConfigureForTests(spec.RecipeId, spec.ResultItemId, spec.ResultAmount, ingredients);
                })).ToList();
        }

        private static T CreateOrUpdateAsset<T>(string assetPath, System.Action<T> configure)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            configure(asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static readonly (string ItemId, string DisplayName, int MaxStackSize, string FileName)[] DefaultItemSpecs =
        {
            ("wood", "Wood", 20, "wood"),
            ("stone", "Stone", 20, "stone"),
            ("fiber", "Fiber", 20, "fiber"),
            ("meat", "Meat", 20, "meat"),
            ("hide", "Hide", 20, "hide"),
            ("bone", "Bone", 20, "bone"),
            ("torch", "Torch", 1, "torch"),
            ("spear", "Spear", 1, "spear"),
            ("bow", "Bow", 1, "bow"),
            ("campfire", "Campfire", 1, "campfire"),
            ("trap", "Trap", 1, "trap"),
            ("wall", "Wall", 20, "wall"),
            ("storage_box", "Storage Box", 1, "storage_box"),
            ("berries", "Berries", 20, "berries"),
            ("grass", "Grass", 20, "grass"),
            ("tent", "Tent", 1, "tent")
        };

        private static readonly (string RecipeId, string ResultItemId, int ResultAmount, (string ItemId, int Amount)[] Ingredients, string FileName)[] DefaultRecipeSpecs =
        {
            ("campfire", "campfire", 1, new[] { ("wood", 3), ("stone", 2) }, "campfire"),
            ("spear", "spear", 1, new[] { ("wood", 2), ("stone", 1), ("fiber", 1) }, "spear"),
            ("torch", "torch", 1, new[] { ("wood", 1), ("fiber", 1) }, "torch"),
            ("bow", "bow", 1, new[] { ("wood", 3), ("fiber", 4), ("bone", 1) }, "bow"),
            ("trap", "trap", 1, new[] { ("wood", 2), ("fiber", 2) }, "trap"),
            ("wall", "wall", 1, new[] { ("wood", 3) }, "wall"),
            ("storage_box", "storage_box", 1, new[] { ("wood", 4) }, "storage_box"),
            ("tent", "tent", 1, new[] { ("wood", 4), ("fiber", 3) }, "tent")
        };
    }
}
