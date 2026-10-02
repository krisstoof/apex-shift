using ApexShift.Editor.World;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Topography;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Editor
{
    public sealed class RuntimeWorldCleanupTests
    {
        private GameObject owned;
        private GameObject unrelated;

        [TearDown]
        public void TearDown()
        {
            if (owned != null) Object.DestroyImmediate(owned);
            if (unrelated != null) Object.DestroyImmediate(unrelated);
        }

        [Test]
        public void CleanupPreservesTerrainWaterTopographyAndAuthoredObjectsButRemovesOwnedContent()
        {
            owned = new GameObject("GenerationRoot");
            unrelated = new GameObject("ResourceRoot");
            unrelated.AddComponent<ResourceNodeView>();
            Transform terrain = Child(owned.transform, "TerrainRoot");
            Transform land = Child(terrain, "Terrain");
            Transform water = Child(terrain, "WaterSurfaceMesh");
            // Even component-marked objects inside the explicitly protected terrain remain untouched.
            water.gameObject.AddComponent<ResourceNodeView>();
            Transform biome = Child(owned.transform, "BiomeRoot");
            Transform topography = Child(owned.transform, "IslandTopographyRuntime");
            topography.gameObject.AddComponent<IslandTopographyRuntime>();
            string[] roots = { "ResourceRoot", "VegetationRoot", "BuildingRoot", "LandmarkRoot", "CreatureRoot" };
            string[] objects = { "Rock", "Tree", "Shelter", "Ruins", "Creature" };
            for (int i = 0; i < roots.Length; i++) Child(Child(owned.transform, roots[i]), objects[i]);
            Transform player = Child(owned.transform, "Player");
            Transform camera = Child(owned.transform, "Main Camera");
            camera.gameObject.AddComponent<Camera>();
            Child(owned.transform, "WorldBounds");
            Child(owned.transform, "EcosystemRuntime");
            Transform orphan = Child(biome, "NameDoesNotIdentifyContent");
            orphan.gameObject.AddComponent<ResourceNodeView>();
            Transform unrelatedBiomeData = Child(biome, "PreviewData");

            var report = RuntimeWorldCleanupTool.CleanGenerationRoot(owned.transform);

            Assert.That(terrain != null && land != null && water != null, Is.True);
            Assert.That(biome != null && topography != null && unrelatedBiomeData != null, Is.True);
            Assert.That(unrelated != null, Is.True);
            Assert.That(player == null && camera == null && orphan == null, Is.True);
            foreach (string root in roots)
            {
                Assert.That(owned.transform.Find(root), Is.Null, root);
                Assert.That(report.RemovedContent[root], Is.EqualTo(1));
            }
            Assert.That(report.OrphanRuntimeObjects, Is.EqualTo(5));
            Assert.That(report.TerrainPreserved, Is.True);
            var repeat = RuntimeWorldCleanupTool.CleanGenerationRoot(owned.transform);
            Assert.That(repeat.OrphanRuntimeObjects, Is.Zero);
            foreach (int count in repeat.RemovedContent.Values) Assert.That(count, Is.Zero);
            Assert.That(repeat.TerrainPreserved, Is.True);
        }

        [Test]
        public void CleanupRefusesContentContainerThatOwnsPreservedTopographyBeforeDeletingAnything()
        {
            owned = new GameObject("GenerationRoot");
            Child(Child(owned.transform, "ResourceRoot"), "Rock");
            Transform content = Child(owned.transform, "CreatureRoot");
            Child(content, "UnexpectedTopography").gameObject.AddComponent<IslandTopographyRuntime>();
            Assert.Throws<System.InvalidOperationException>(() => RuntimeWorldCleanupTool.CleanGenerationRoot(owned.transform));
            Assert.That(owned.transform.Find("ResourceRoot"), Is.Not.Null, "Safety validation must precede mutations.");
            Assert.That(content != null, Is.True);
        }

        private static Transform Child(Transform parent, string name)
        {
            var item = new GameObject(name).transform;
            item.SetParent(parent, false);
            return item;
        }
    }
}
