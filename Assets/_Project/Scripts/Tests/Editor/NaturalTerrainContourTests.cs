using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Generation;

namespace ApexShift.Tests.Editor
{
    public sealed class NaturalTerrainContourTests
    {
        private static readonly Vector2[] Cell =
        {
            new Vector2(-1f, -1f), new Vector2(-1f, 1f),
            new Vector2(1f, 1f), new Vector2(1f, -1f)
        };

        [TestCaseSource(nameof(AllMarchingSquaresCases))]
        public void MarchingSquares_AllMasksPartitionCellWithPositiveWinding(int mask, bool centerInside)
        {
            Func<float, float, bool> field = BuildMaskField(mask, centerInside);
            List<Vector3> landVertices;
            List<int> landTriangles;
            List<Vector3> waterVertices;
            List<int> waterTriangles;
            AppendCell(true, field, out landVertices, out landTriangles);
            AppendCell(false, field, out waterVertices, out waterTriangles);
            List<Vector3> repeatedLandVertices;
            List<int> repeatedLandTriangles;
            List<Vector3> repeatedWaterVertices;
            List<int> repeatedWaterTriangles;
            AppendCell(true, field, out repeatedLandVertices, out repeatedLandTriangles);
            AppendCell(false, field, out repeatedWaterVertices, out repeatedWaterTriangles);

            AssertTrianglesValid(landVertices, landTriangles);
            AssertTrianglesValid(waterVertices, waterTriangles);
            Assert.That(TriangleAreaXZ(landVertices, landTriangles) + TriangleAreaXZ(waterVertices, waterTriangles),
                Is.EqualTo(4f).Within(0.01f), $"mask={mask}, centerInside={centerInside}");
            CollectionAssert.AreEqual(CoastlineKeys(landVertices), CoastlineKeys(waterVertices),
                $"land/water coastline mismatch for mask={mask}, centerInside={centerInside}");
            CollectionAssert.AreEqual(landVertices, repeatedLandVertices);
            CollectionAssert.AreEqual(landTriangles, repeatedLandTriangles);
            CollectionAssert.AreEqual(waterVertices, repeatedWaterVertices);
            CollectionAssert.AreEqual(waterTriangles, repeatedWaterTriangles);
        }

        [TestCase(5, false)]
        [TestCase(5, true)]
        [TestCase(10, false)]
        [TestCase(10, true)]
        public void AmbiguousCliffContourUsesLandWaterCenterPairing(int mask, bool centerInside)
        {
            Func<float, float, bool> field = BuildMaskField(mask, centerInside);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            MethodInfo method = typeof(NaturalTerrainBuilder).GetMethod("AppendContourCliffSegments", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            Func<Vector3, float> height = _ => 0.5f;
            method.Invoke(null, new object[] { Cell, field, height, -0.6f, vertices, triangles, uvs });

            bool pairAroundCorners01And23 = (mask == 5 && centerInside) || (mask == 10 && !centerInside);
            Vector2[] crossings = EdgeCrossings(mask, field);
            var expected = pairAroundCorners01And23
                ? new[] { UnorderedPair(crossings[0], crossings[1]), UnorderedPair(crossings[2], crossings[3]) }
                : new[] { UnorderedPair(crossings[3], crossings[0]), UnorderedPair(crossings[1], crossings[2]) };
            var actual = new List<string>();
            for (int i = 0; i + 3 < vertices.Count; i += 4)
                actual.Add(UnorderedPair(new Vector2(vertices[i].x, vertices[i].z), new Vector2(vertices[i + 1].x, vertices[i + 1].z)));

            CollectionAssert.AreEquivalent(expected, actual, $"mask={mask}, centerInside={centerInside}");
        }

        public static IEnumerable<TestCaseData> AllMarchingSquaresCases()
        {
            for (int mask = 0; mask < 16; mask++)
            {
                if (mask == 5 || mask == 10)
                {
                    yield return new TestCaseData(mask, false).SetName($"MarchingSquares_Mask{mask}_CenterOutside");
                    yield return new TestCaseData(mask, true).SetName($"MarchingSquares_Mask{mask}_CenterInside");
                }
                else
                {
                    yield return new TestCaseData(mask, false).SetName($"MarchingSquares_Mask{mask}");
                }
            }
        }

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

        private static Func<float, float, bool> BuildMaskField(int mask, bool centerInside)
        {
            return (x, z) =>
            {
                if (Mathf.Abs(x) < 0.0001f && Mathf.Abs(z) < 0.0001f)
                    return centerInside;
                for (int i = 0; i < Cell.Length; i++)
                    if ((new Vector2(x, z) - Cell[i]).sqrMagnitude < 0.0000001f)
                        return (mask & (1 << i)) != 0;

                float u = (x + 1f) * 0.5f;
                float v = (z + 1f) * 0.5f;
                float[] values = new float[4];
                for (int i = 0; i < 4; i++) values[i] = (mask & (1 << i)) != 0 ? 1f : -1f;
                float c0 = Mathf.Lerp(values[0], values[3], u);
                float c1 = Mathf.Lerp(values[1], values[2], u);
                return Mathf.Lerp(c0, c1, v) >= 0f;
            };
        }

        private static void AppendCell(bool includeLand, Func<float, float, bool> field,
            out List<Vector3> vertices, out List<int> triangles)
        {
            vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            triangles = new List<int>();
            MethodInfo method = typeof(NaturalTerrainBuilder).GetMethod("AppendContourCell", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);
            Func<Vector2, float> flatHeight = _ => 0.25f;
            method.Invoke(null, new object[] { Cell, includeLand, field, flatHeight, flatHeight, vertices, uvs, triangles, 1f });
        }

        private static Vector2[] EdgeCrossings(int mask, Func<float, float, bool> field)
        {
            var result = new Vector2[4];
            for (int edge = 0; edge < 4; edge++)
            {
                Vector2 a = Cell[edge];
                Vector2 b = Cell[(edge + 1) & 3];
                bool state = field(a.x, a.y);
                Vector2 lo = a;
                Vector2 hi = b;
                for (int i = 0; i < 10; i++)
                {
                    Vector2 mid = (lo + hi) * 0.5f;
                    if (field(mid.x, mid.y) == state) lo = mid;
                    else hi = mid;
                }
                result[edge] = (lo + hi) * 0.5f;
            }
            return result;
        }

        private static string UnorderedPair(Vector2 a, Vector2 b)
        {
            string aKey = $"{a.x:F5},{a.y:F5}";
            string bKey = $"{b.x:F5},{b.y:F5}";
            return string.CompareOrdinal(aKey, bKey) < 0 ? aKey + "|" + bKey : bKey + "|" + aKey;
        }

        private static void AssertTrianglesValid(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> triangles)
        {
            Assert.That(triangles.Count % 3, Is.EqualTo(0));
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a);
                Assert.That(normal.y, Is.GreaterThan(0.000001f), $"Triangle {i / 3} has non-positive Y winding.");
                Assert.That(normal.sqrMagnitude, Is.GreaterThan(0.00000001f), $"Triangle {i / 3} is degenerate.");
            }
        }

        private static float TriangleAreaXZ(IReadOnlyList<Vector3> vertices, IReadOnlyList<int> triangles)
        {
            float area = 0f;
            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                area += Mathf.Abs(Vector3.Cross(b - a, c - a).y) * 0.5f;
            }
            return area;
        }

        private static List<string> CoastlineKeys(IReadOnlyList<Vector3> vertices)
        {
            var keys = new SortedSet<string>();
            foreach (Vector3 point in vertices)
            {
                bool corner = false;
                foreach (Vector2 cellCorner in Cell)
                    if ((new Vector2(point.x, point.z) - cellCorner).sqrMagnitude < 0.000001f)
                    { corner = true; break; }
                if (!corner) keys.Add($"{point.x:F5},{point.z:F5}");
            }
            return new List<string>(keys);
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
