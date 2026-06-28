using System.Collections.Generic;
using ApexShift.Core.Save;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Generation;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Ecosystem
{
    public sealed class EcosystemDirectorRuntimeTests
    {
        [Test]
        public void InitializeFromRegionsCreatesBiomeStatesAndPositionLookup()
        {
            GameObject directorObject = new GameObject("EcosystemDirector");
            BiomeDefinitionAsset biome = ScriptableObject.CreateInstance<BiomeDefinitionAsset>();
            try
            {
                biome.Configure("westwood", "Westwood", Color.green, false, new List<VegetationSpawnEntryAsset>());
                EcosystemDirectorRuntime director = directorObject.AddComponent<EcosystemDirectorRuntime>();
                director.InitializeFromRegions(new[]
                {
                    new GeneratedBiomeRegion(biome, new Bounds(Vector3.zero, new Vector3(10f, 2f, 10f)))
                });

                Assert.IsTrue(director.Initialized);
                Assert.IsNotNull(director.GetBiomeState("westwood"));
                Assert.AreEqual("westwood", director.GetBiomeIdForPosition(new Vector3(1f, 0f, 1f)));
            }
            finally
            {
                Object.DestroyImmediate(directorObject);
                Object.DestroyImmediate(biome);
            }
        }

        [Test]
        public void TickDayAdvancesResourceRegrowth()
        {
            GameObject directorObject = new GameObject("EcosystemDirector");
            GameObject resourceObject = new GameObject("BerryBush");
            try
            {
                EcosystemDirectorRuntime director = directorObject.AddComponent<EcosystemDirectorRuntime>();
                director.InitializeFromRegions(null);

                ResourceNodeView node = resourceObject.AddComponent<ResourceNodeView>();
                node.ConfigureDefault("berry_bush");
                node.LoadState(0, depleted: true, growthProgress: 0f);

                director.TickDay(1);
                Assert.IsTrue(node.State.IsDepleted);
                Assert.AreEqual(0.5f, node.State.GrowthProgress, 0.001f);

                director.TickDay(1);
                Assert.IsFalse(node.State.IsDepleted);
                Assert.AreEqual(1f, node.State.GrowthProgress, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(resourceObject);
                Object.DestroyImmediate(directorObject);
            }
        }

        [Test]
        public void SaveDataRestoresBiomeStatesIntoDirector()
        {
            GameObject directorObject = new GameObject("EcosystemDirector");
            try
            {
                EcosystemDirectorRuntime director = directorObject.AddComponent<EcosystemDirectorRuntime>();
                BiomeEcosystemSaveData saveData = new BiomeEcosystemSaveData(
                    "south_thicket",
                    "South Thicket",
                    40f,
                    100f,
                    6f,
                    0f,
                    0f,
                    60f,
                    5f,
                    2f,
                    1f,
                    1,
                    1,
                    1,
                    "HERBIVORE",
                    "stressed");

                director.LoadSaveData(new[] { saveData });

                Assert.IsTrue(director.Initialized);
                Assert.AreEqual("save", director.EcosystemStateSource);
                Assert.AreEqual(40f, director.GetBiomeState("south_thicket").PlantBiomass, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(directorObject);
            }
        }
    }
}
