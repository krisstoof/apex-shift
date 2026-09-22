using System;
using NUnit.Framework;
using UnityEngine;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Generation;

namespace ApexShift.Tests.Editor
{
    public sealed class NaturalTerrainContourTests
    {
        [Test]
        public void LandAndWaterMeshesUseTheSameDeterministicNonGridCoastline()
        {
            BiomeCatalogAsset catalog = ScriptableObject.CreateInstance<BiomeCatalogAsset>();
            BiomeDefinitionAsset biome = ScriptableObject.CreateInstance<BiomeDefinitionAsset>();
            biome.Configure("south_thicket", "South Thicket", Color.green, false, Array.Empty<VegetationSpawnEntryAsset>());
            catalog.SetBiomes(new[] { biome });

            GameObject firstRoot = new GameObject("ContourFirst");
            GameObject secondRoot = new GameObject("ContourSecond");
            try
            {
                Func<float, float, bool> island = (x, z) => x * x + z * z <= 64f;
                Func<Vector3, float> height = p => 0.25f + p.x * 0.01f + p.z * 0.005f;

                NaturalTerrainBuilder.BuildIslandTerrain(firstRoot.transform, 4, 5f, catalog, island, height, p => "south_thicket");
                NaturalTerrainBuilder.BuildUnifiedWaterSurface(firstRoot.transform, 4, 5f, null, null, island);
                NaturalTerrainBuilder.BuildIslandTerrain(secondRoot.transform, 4, 5f, catalog, island, height, p => "south_thicket");
                NaturalTerrainBuilder.BuildUnifiedWaterSurface(secondRoot.transform, 4, 5f, null, null, island);

                Mesh landA = firstRoot.transform.Find("IslandTerrainMesh").GetComponent<MeshFilter>().sharedMesh;
                Mesh waterA = firstRoot.transform.Find("WaterSurfaceMesh").GetComponent<MeshFilter>().sharedMesh;
                Mesh landB = secondRoot.transform.Find("IslandTerrainMesh").GetComponent<MeshFilter>().sharedMesh;
                Mesh waterB = secondRoot.transform.Find("WaterSurfaceMesh").GetComponent<MeshFilter>().sharedMesh;

                Assert.That(landA.triangles.Length, Is.GreaterThan(0));
                Assert.That(waterA.triangles.Length, Is.GreaterThan(0));
                Assert.That(ContainsNonGridVertex(landA, 5f / 6f), Is.True);
                Assert.That(ContainsNonGridVertex(waterA, 5f / 6f), Is.True);
                AssertMeshesEqual(landA, landB);
                AssertMeshesEqual(waterA, waterB);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstRoot);
                UnityEngine.Object.DestroyImmediate(secondRoot);
                UnityEngine.Object.DestroyImmediate(catalog);
                UnityEngine.Object.DestroyImmediate(biome);
            }
        }

        private static bool ContainsNonGridVertex(Mesh mesh, float cellSize)
        {
            foreach (Vector3 vertex in mesh.vertices)
            {
                float x = Mathf.Abs(vertex.x / cellSize - Mathf.Round(vertex.x / cellSize));
                float z = Mathf.Abs(vertex.z / cellSize - Mathf.Round(vertex.z / cellSize));
                if (x > 0.001f || z > 0.001f) return true;
            }
            return false;
        }

        private static void AssertMeshesEqual(Mesh expected, Mesh actual)
        {
            Assert.That(actual.vertexCount, Is.EqualTo(expected.vertexCount));
            for (int i = 0; i < expected.vertexCount; i++)
                Assert.That(actual.vertices[i], Is.EqualTo(expected.vertices[i]));
            CollectionAssert.AreEqual(expected.triangles, actual.triangles);
        }
    }
}
