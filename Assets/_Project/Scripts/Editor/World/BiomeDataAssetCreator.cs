using System.Collections.Generic;
using System.IO;
using ApexShift.Runtime.World.Biomes;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.World
{
    public static class BiomeDataAssetCreator
    {
        private const string DataPath = "Assets/_Project/Data/Biomes";
        private const string CatalogPath = DataPath + "/BiomeCatalog.asset";

        [MenuItem("Tools/Apex Shift/World/Create Default Biome Data Assets")]
        public static void CreateDefaultAssets()
        {
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
                AssetDatabase.Refresh();
            }

            var assets = new List<BiomeDefinitionAsset>();

            assets.Add(CreateOrUpdateBiome("westwood", "Westwood", new Color(0.055f, 0.18f, 0.075f), false, new List<VegetationSpawnEntryAsset>
            {
                new VegetationSpawnEntryAsset(VegetationSpawnKind.ConiferTree, 95, 1f, 0.9f, 1.35f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.GreenBush, 28, 1f, 0.45f, 0.9f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.GrassOrFlower, 18, 1f, 0.35f, 0.7f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.BerryBush, 12, 1f, 0.45f, 0.95f, "", false)
            }));

            assets.Add(CreateOrUpdateBiome("south_thicket", "South Thicket", new Color(0.20f, 0.43f, 0.13f), false, new List<VegetationSpawnEntryAsset>
            {
                new VegetationSpawnEntryAsset(VegetationSpawnKind.LeafyTree, 55, 1f, 0.75f, 1.2f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.GreenBush, 45, 1f, 0.45f, 0.9f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.GrassOrFlower, 38, 1f, 0.35f, 0.7f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.BerryBush, 8, 1f, 0.45f, 0.95f, "", false)
            }));

            assets.Add(CreateOrUpdateBiome("hearth_meadow", "Hearth Meadow", new Color(0.50f, 0.68f, 0.36f), true, new List<VegetationSpawnEntryAsset>
            {
                new VegetationSpawnEntryAsset(VegetationSpawnKind.LeafyTree, 8, 1f, 0.7f, 1.1f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.GreenBush, 10, 1f, 0.45f, 0.9f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.GrassOrFlower, 32, 1f, 0.35f, 0.7f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.BerryBush, 6, 1f, 0.45f, 0.95f, "", false)
            }));

            assets.Add(CreateOrUpdateBiome("stoneback_ridge", "Stoneback Ridge", new Color(0.30f, 0.32f, 0.30f), false, new List<VegetationSpawnEntryAsset>
            {
                new VegetationSpawnEntryAsset(VegetationSpawnKind.Rock, 85, 1f, 0.8f, 1.35f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.DryBush, 14, 1f, 0.45f, 0.85f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.ConiferTree, 10, 1f, 0.75f, 1.25f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.DryTree, 4, 1f, 0.75f, 1.25f, "", false)
            }));

            assets.Add(CreateOrUpdateBiome("redfang_wilds", "Redfang Wilds", new Color(0.55f, 0.36f, 0.16f), false, new List<VegetationSpawnEntryAsset>
            {
                new VegetationSpawnEntryAsset(VegetationSpawnKind.DryBush, 48, 1f, 0.45f, 0.85f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.DryTree, 45, 1f, 0.75f, 1.25f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.Rock, 24, 1f, 0.8f, 1.35f, "", false),
                new VegetationSpawnEntryAsset(VegetationSpawnKind.GrassOrFlower, 3, 1f, 0.35f, 0.7f, "", false)
            }));

            CreateOrUpdateCatalog(assets);

            AssetDatabase.SaveAssets();
            Debug.Log("Biome data assets and catalog created/updated successfully.");
        }

        private static BiomeDefinitionAsset CreateOrUpdateBiome(string id, string name, Color color, bool isStarter, List<VegetationSpawnEntryAsset> vegetation)
        {
            string path = $"{DataPath}/{id}.asset";
            BiomeDefinitionAsset asset = AssetDatabase.LoadAssetAtPath<BiomeDefinitionAsset>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BiomeDefinitionAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Configure(id, name, color, isStarter, vegetation);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void CreateOrUpdateCatalog(List<BiomeDefinitionAsset> biomes)
        {
            BiomeCatalogAsset catalog = AssetDatabase.LoadAssetAtPath<BiomeCatalogAsset>(CatalogPath);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BiomeCatalogAsset>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetBiomes(biomes);
            EditorUtility.SetDirty(catalog);
        }
    }
}
