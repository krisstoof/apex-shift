using System;
using System.Collections.Generic;
using UnityEngine;
using ApexShift.Runtime.World.Biomes;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>
    /// Generates fully procedural terrain and water surface meshes.
    ///
    /// Key design goals:
    ///  - No visible tile grid: vertices are placed every 2 world-units (4× subdivision
    ///    of the 8-unit logical tile), so the Perlin-noise coastline has enough resolution
    ///    to look organic rather than square.
    ///  - One unified land mesh (submesh per biome → per-biome GroundMaterial).
    ///  - One unified water surface mesh (replaces the diamond-grid water cubes).
    ///  - One seabed mesh visible through the semi-transparent water.
    ///  - Invisible per-tile BoxCollider triggers for swim-state detection.
    /// </summary>
    public static class NaturalTerrainBuilder
    {
        /// <summary>
        /// How many mesh vertices to place across one logical tile (8 units).
        /// 6 → one vertex every ~1.33 units → finer coastlines and biome borders.
        /// </summary>
        private const int TerrainInteriorSubdivPerTile = 6;
        private const int CoastlineSubdivPerTile = 12;

        private static readonly string[] BiomeSubmeshOrder =
        {
            "hearth_meadow", "westwood", "south_thicket", "stoneback_ridge", "redfang_wilds"
        };

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Builds the island land mesh. Each biome zone gets its own submesh so the
        /// biome GroundMaterials are applied directly. A MeshCollider handles physics.
        /// The coastline follows the scalar field's zero contour, locally refined to
        /// twice the interior sampling density. Interior terrain remains at 6 samples/tile.
        /// </summary>
    /// <summary>Terrain above this Y is treated as a cliff instead of a beach.</summary>
        private const float CliffHeightThreshold = 0.18f;

        public static void BuildIslandTerrain(
            Transform parent,
            int gridSize,
            float tileSize,
            BiomeCatalogAsset catalog,
            Func<float, float, float> sampleIslandField,
            Func<Vector3, float> getTerrainHeight,
            Func<Vector3, string> getBiomeId)
        {
            int resolution   = gridSize * TerrainInteriorSubdivPerTile;
            float cellSize   = tileSize / TerrainInteriorSubdivPerTile;
            Vector3 halfSize = new Vector3(gridSize * tileSize * 0.5f, 0f, gridSize * tileSize * 0.5f);
            bool[,] refinedCells = BuildCoastlineRefinementMask(resolution, cellSize, halfSize, sampleIslandField);

            // ── Pass 1: compute authoritative per-vertex surface heights and land flags ──
            // ── Pass 3: build vertex and UV arrays ────────────────────────────
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();

            // ── Pass 4: triangle lists grouped by biome ───────────────────────
            var biomeTriangles = new Dictionary<string, List<int>>();
            foreach (string b in BiomeSubmeshOrder)
                biomeTriangles[b] = new List<int>();
            var vertexCache = new ContourVertexCache();

            for (int cz = 0; cz < resolution; cz++)
            {
                for (int cx = 0; cx < resolution; cx++)
                {
                    Vector2[] corners = CellCorners(cx, cz, cellSize, halfSize);
                    int mask = LandMask(corners, sampleIslandField);
                    if (mask == 0 && !refinedCells[cx, cz]) continue;

                    float ccx = (cx + 0.5f) * cellSize - halfSize.x;
                    float ccz = (cz + 0.5f) * cellSize - halfSize.z;
                    string biomeId = getBiomeId(new Vector3(ccx, 0f, ccz));
                    if (!biomeTriangles.ContainsKey(biomeId))
                        biomeId = "south_thicket";

                    var tris = biomeTriangles[biomeId];
                    int domain = Array.IndexOf(BiomeSubmeshOrder, biomeId);
                    Func<Vector2, float> getHeight = p => getTerrainHeight(new Vector3(p.x, 0f, p.y));
                    if (refinedCells[cx, cz])
                    {
                        float fineCellSize = cellSize / (CoastlineSubdivPerTile / TerrainInteriorSubdivPerTile);
                        for (int sz = 0; sz < 2; sz++)
                            for (int sx = 0; sx < 2; sx++)
                            {
                                Vector2[] fineCorners = CellCorners(cx * 2 + sx, cz * 2 + sz,
                                    fineCellSize, halfSize);
                                AppendContourCell(fineCorners, true, sampleIslandField,
                                    getHeight, null, vertices, uvs, tris, tileSize, vertexCache, domain);
                            }
                    }
                    else
                    {
                        AppendFullInteriorCell(corners, getHeight, null,
                            HasRefinedNeighbor(refinedCells, cx, cz, 0),
                            HasRefinedNeighbor(refinedCells, cx, cz, 1),
                            HasRefinedNeighbor(refinedCells, cx, cz, 2),
                            HasRefinedNeighbor(refinedCells, cx, cz, 3),
                            vertices, uvs, tris, tileSize, vertexCache, domain);
                    }
                }
            }

            // ── Collect active biomes + materials ────────────────────────────────
            var activeBiomes    = new List<string>();
            var activeMaterials = new List<Material>();

            foreach (string b in BiomeSubmeshOrder)
            {
                if (biomeTriangles[b].Count == 0) continue;

                BiomeDefinitionAsset bDef = catalog.GetBiome(b);
                Material mat;
                if (bDef?.GroundMaterial != null)
                {
                    mat = bDef.GroundMaterial;
                }
                else
                {
                    mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    if (bDef != null && mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", bDef.GroundColor);
                }

                activeBiomes.Add(b);
                activeMaterials.Add(mat);
            }

            // ── Build mesh ───────────────────────────────────────────────────────
            if (activeBiomes.Count == 0) return;

            Mesh mesh = new Mesh
            {
                name        = "IslandTerrainMesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = activeBiomes.Count;
            for (int i = 0; i < activeBiomes.Count; i++)
                mesh.SetTriangles(biomeTriangles[activeBiomes[i]], i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject("IslandTerrainMesh");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            go.AddComponent<MeshFilter>().sharedMesh   = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = activeMaterials.ToArray();
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// Builds the unified water surface mesh. Uses the same scalar field and local
        /// contour refinement as land so the boundary between them aligns perfectly. The mesh
        /// covers all non-land sub-cells, naturally tracing the organic island border.
        ///
        /// The returned GameObject has WaterSurfaceAnimator attached for gentle wave bobbing.
        /// A MeshCollider (non-trigger) lets the player walk on it. A BoxCollider trigger
        /// tagged "Water" covers the same area so PlayerWaterDetector can detect swimming.
        /// </summary>
        public static GameObject BuildUnifiedWaterSurface(
            Transform parent,
            int gridSize,
            float tileSize,
            Material shallowWaterMat,
            Material deepWaterMat,
            Func<float, float, float> sampleIslandField,
            float waterY = 0f)
        {
            int resolution   = gridSize * TerrainInteriorSubdivPerTile;
            float cellSize   = tileSize / TerrainInteriorSubdivPerTile;
            Vector3 halfSize = new Vector3(gridSize * tileSize * 0.5f, 0f, gridSize * tileSize * 0.5f);
            bool[,] refinedCells = BuildCoastlineRefinementMask(resolution, cellSize, halfSize, sampleIslandField);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();

            // Two submeshes: shallow (within ~2 tiles of shore) and deep water
            var shallowTris = new List<int>();
            var deepTris    = new List<int>();
            var vertexCache = new ContourVertexCache();

            for (int cz = 0; cz < resolution; cz++)
            {
                for (int cx = 0; cx < resolution; cx++)
                {
                    float ccx = (cx + 0.5f) * cellSize - halfSize.x;
                    float ccz = (cz + 0.5f) * cellSize - halfSize.z;

                    Vector2[] corners = CellCorners(cx, cz, cellSize, halfSize);
                    int mask = LandMask(corners, sampleIslandField);
                    if (mask == 15 && !refinedCells[cx, cz]) continue;

                    Func<float, float, bool> isInsideIsland = (x, z) => sampleIslandField(x, z) >= 0f;
                    float distToLand = ApproxDistanceToLand(ccx, ccz, isInsideIsland, cellSize);
                    bool shallow     = distToLand < tileSize * 1.8f;

                    var tris = shallow ? shallowTris : deepTris;
                    int domain = shallow ? 0 : 1;
                    Func<Vector2, float> getHeight = p => WaterSurfaceHeight(p, waterY);
                    if (refinedCells[cx, cz])
                    {
                        float fineCellSize = cellSize / (CoastlineSubdivPerTile / TerrainInteriorSubdivPerTile);
                        for (int sz = 0; sz < 2; sz++)
                            for (int sx = 0; sx < 2; sx++)
                            {
                                Vector2[] fineCorners = CellCorners(cx * 2 + sx, cz * 2 + sz,
                                    fineCellSize, halfSize);
                                AppendContourCell(fineCorners, false, sampleIslandField, null,
                                    getHeight, vertices, uvs, tris, tileSize, vertexCache, domain);
                            }
                    }
                    else
                    {
                        AppendFullInteriorCell(corners, null, getHeight,
                            HasRefinedNeighbor(refinedCells, cx, cz, 0),
                            HasRefinedNeighbor(refinedCells, cx, cz, 1),
                            HasRefinedNeighbor(refinedCells, cx, cz, 2),
                            HasRefinedNeighbor(refinedCells, cx, cz, 3),
                            vertices, uvs, tris, tileSize, vertexCache, domain);
                    }
                }
            }

            int subCount = (shallowTris.Count > 0 ? 1 : 0) + (deepTris.Count > 0 ? 1 : 0);
            if (subCount == 0) return null;

            Mesh mesh = new Mesh
            {
                name        = "WaterSurfaceMesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = subCount;

            var mats = new List<Material>();
            int subIdx = 0;
            if (shallowTris.Count > 0)
            {
                mesh.SetTriangles(shallowTris, subIdx++);
                mats.Add(BuildWaterMaterial(shallowWaterMat, new Color(0.14f, 0.50f, 0.66f, 0.72f)));
            }
            if (deepTris.Count > 0)
            {
                mesh.SetTriangles(deepTris, subIdx++);
                mats.Add(BuildWaterMaterial(deepWaterMat, new Color(0.03f, 0.22f, 0.38f, 0.86f)));
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject go = new GameObject("WaterSurfaceMesh");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            go.AddComponent<MeshFilter>().sharedMesh          = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials   = mats.ToArray();

            // ── Swim support collider, not a walkable water-floor ───────────────
            // Previous version put the top face exactly at waterY. CharacterController
            // then stood on water like a normal floor, so the player visually walked
            // across the surface. Keep a broad support collider below the surface only:
            // it catches the controller if detection fails, but the player can be
            // visually lowered into a swimming pose.
            float worldSpan = gridSize * tileSize;
            const float swimSupportDepth = 1.15f;
            const float swimSupportThickness = 0.10f;
            BoxCollider waterFloor = go.AddComponent<BoxCollider>();
            waterFloor.center = new Vector3(0f, waterY - swimSupportDepth - swimSupportThickness * 0.5f, 0f);
            waterFloor.size   = new Vector3(worldSpan, swimSupportThickness, worldSpan);

            // Trigger for PlayerWaterDetector. It is intentionally non-solid; actual
            // water state is confirmed by IslandTopographyRuntime on the player, because
            // this unified mesh has rectangular bounds even when its triangles are only water.
            Bounds b = mesh.bounds;
            if (Application.isPlaying)
            {
                GameObject triggerGo = new GameObject("WaterSurfaceTrigger");
                triggerGo.transform.SetParent(go.transform, false);
                triggerGo.layer = go.layer;

                BoxCollider trigger = triggerGo.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center    = new Vector3(0f, 0.7f, 0f);    // slightly above surface
                trigger.size      = new Vector3(b.size.x, 1.4f, b.size.z);
                triggerGo.AddComponent<WaterVolume>();
            }

            return go;
        }

        /// <summary>
        /// Creates a seabed mesh visible below the semi-transparent water surface.
        /// Depth varies from shallow (-1.2f near shore) to deep (-2.8f) with Perlin noise.
        /// Purely visual – no collider.
        /// </summary>
        public static void BuildSeabed(
            Transform parent,
            int gridSize,
            float tileSize,
            Material seabedMaterial,
            Func<float, float, bool> isInsideIsland,
            float shallowDepth = -1.2f,
            float deepDepth    = -2.8f)
        {
            Vector3 halfSize = new Vector3(gridSize * tileSize * 0.5f, 0f, gridSize * tileSize * 0.5f);
            int vWidth       = gridSize + 1;

            Vector3[] vertices = new Vector3[vWidth * vWidth];
            Vector2[] uvs      = new Vector2[vWidth * vWidth];

            for (int vz = 0; vz <= gridSize; vz++)
            {
                for (int vx = 0; vx <= gridSize; vx++)
                {
                    float wx = vx * tileSize - halfSize.x;
                    float wz = vz * tileSize - halfSize.z;

                    float distToLand = ApproxDistanceToLand(wx, wz, isInsideIsland, tileSize);
                    float t          = Mathf.Clamp01(distToLand / (tileSize * 3.5f));
                    float noise      = (Mathf.PerlinNoise((wx + 700f) * 0.055f, (wz + 300f) * 0.055f) - 0.5f) * 0.35f;
                    float height     = Mathf.Lerp(shallowDepth, deepDepth, t) + noise;

                    int idx    = vz * vWidth + vx;
                    vertices[idx] = new Vector3(wx, height, wz);
                    uvs[idx]      = new Vector2(wx / tileSize, wz / tileSize);
                }
            }

            var triangles = new List<int>();
            for (int cz = 0; cz < gridSize; cz++)
            {
                for (int cx = 0; cx < gridSize; cx++)
                {
                    int v00 = cz       * vWidth + cx;
                    int v10 = cz       * vWidth + (cx + 1);
                    int v01 = (cz + 1) * vWidth + cx;
                    int v11 = (cz + 1) * vWidth + (cx + 1);

                    triangles.Add(v00); triangles.Add(v01); triangles.Add(v11);
                    triangles.Add(v00); triangles.Add(v11); triangles.Add(v10);
                }
            }

            Mesh mesh = new Mesh
            {
                name        = "SeabedMesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh.vertices  = vertices;
            mesh.uv        = uvs;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Material mat = seabedMaterial;
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", new Color(0.56f, 0.50f, 0.38f));
            }

            GameObject go = new GameObject("SeabedMesh");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            go.AddComponent<MeshFilter>().sharedMesh     = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial    = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = true;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static Vector2[] CellCorners(int cx, int cz, float cellSize, Vector3 halfSize)
        {
            float x0 = cx * cellSize - halfSize.x;
            float x1 = x0 + cellSize;
            float z0 = cz * cellSize - halfSize.z;
            float z1 = z0 + cellSize;
            // Clockwise around the cell as viewed from +Y (Unity's terrain winding).
            return new[] { new Vector2(x0, z0), new Vector2(x0, z1), new Vector2(x1, z1), new Vector2(x1, z0) };
        }

        private static int LandMask(Vector2[] corners, Func<float, float, float> sampleIslandField)
        {
            int mask = 0;
            for (int i = 0; i < 4; i++)
                if (sampleIslandField(corners[i].x, corners[i].y) >= 0f) mask |= 1 << i;
            return mask;
        }

        private static bool[,] BuildCoastlineRefinementMask(int resolution, float cellSize,
            Vector3 halfSize, Func<float, float, float> sampleIslandField)
        {
            var mixed = new bool[resolution, resolution];
            var refined = new bool[resolution, resolution];
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                {
                    int mask = LandMask(CellCorners(x, z, cellSize, halfSize), sampleIslandField);
                    mixed[x, z] = mask != 0 && mask != 15;
                }

            // Refine the actual contour cells plus a one-cell transition ring.
            // The ring lets coarse interior cells split only the shared boundary
            // edge, avoiding T-junction cracks without doubling interior density.
            for (int z = 0; z < resolution; z++)
                for (int x = 0; x < resolution; x++)
                {
                    if (!mixed[x, z]) continue;
                    for (int dz = -1; dz <= 1; dz++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx >= 0 && nx < resolution && nz >= 0 && nz < resolution)
                                refined[nx, nz] = true;
                        }
                }
            return refined;
        }

        // Edge indices follow CellCorners' clockwise order: 0=left, 1=far,
        // 2=right, 3=near.
        private static bool HasRefinedNeighbor(bool[,] refined, int x, int z, int edge)
        {
            switch (edge)
            {
                case 0: x--; break;
                case 1: z++; break;
                case 2: x++; break;
                default: z--; break;
            }
            return x >= 0 && x < refined.GetLength(0) && z >= 0 && z < refined.GetLength(1)
                   && refined[x, z];
        }

        private static void AppendFullInteriorCell(Vector2[] corners,
            Func<Vector2, float> getLandHeight, Func<Vector2, float> getSurfaceHeight,
            bool splitEdge0, bool splitEdge1, bool splitEdge2, bool splitEdge3,
            List<Vector3> vertices, List<Vector2> uvs, List<int> triangles, float tileSize,
            ContourVertexCache vertexCache, int vertexDomain)
        {
            var perimeter = new List<Vector2>(8);
            for (int edge = 0; edge < 4; edge++)
            {
                perimeter.Add(corners[edge]);
                bool split = edge == 0 ? splitEdge0 : edge == 1 ? splitEdge1 : edge == 2 ? splitEdge2 : splitEdge3;
                if (split) perimeter.Add((corners[edge] + corners[(edge + 1) & 3]) * 0.5f);
            }

            if (perimeter.Count == 4)
            {
                int a = AddCachedVertex(perimeter[0], getLandHeight, getSurfaceHeight, vertices, uvs,
                    tileSize, vertexCache, vertexDomain);
                int b = AddCachedVertex(perimeter[1], getLandHeight, getSurfaceHeight, vertices, uvs,
                    tileSize, vertexCache, vertexDomain);
                int c = AddCachedVertex(perimeter[2], getLandHeight, getSurfaceHeight, vertices, uvs,
                    tileSize, vertexCache, vertexDomain);
                int d = AddCachedVertex(perimeter[3], getLandHeight, getSurfaceHeight, vertices, uvs,
                    tileSize, vertexCache, vertexDomain);
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(a); triangles.Add(c); triangles.Add(d);
                return;
            }

            Vector2 center = (corners[0] + corners[2]) * 0.5f;
            int centerIndex = AddCachedVertex(center, getLandHeight, getSurfaceHeight,
                vertices, uvs, tileSize, vertexCache, vertexDomain);
            for (int i = 0; i < perimeter.Count; i++)
            {
                int a = AddCachedVertex(perimeter[i], getLandHeight, getSurfaceHeight,
                    vertices, uvs, tileSize, vertexCache, vertexDomain);
                int b = AddCachedVertex(perimeter[(i + 1) % perimeter.Count], getLandHeight, getSurfaceHeight,
                    vertices, uvs, tileSize, vertexCache, vertexDomain);
                triangles.Add(centerIndex); triangles.Add(a); triangles.Add(b);
            }
        }

        private static int AddCachedVertex(Vector2 point, Func<Vector2, float> getLandHeight,
            Func<Vector2, float> getSurfaceHeight, List<Vector3> vertices, List<Vector2> uvs,
            float tileSize, ContourVertexCache vertexCache, int vertexDomain)
        {
            float height = getSurfaceHeight != null ? getSurfaceHeight(point) : getLandHeight(point);
            return vertexCache.GetOrAdd(point, height, tileSize, vertexDomain, vertices, uvs);
        }

        private static Vector2 FindBoundary(Vector2 edgeA, Vector2 edgeB, Func<float, float, float> sampleIslandField)
        {
            if (edgeA.x > edgeB.x || (Mathf.Approximately(edgeA.x, edgeB.x) && edgeA.y > edgeB.y))
            {
                Vector2 swap = edgeA;
                edgeA = edgeB;
                edgeB = swap;
            }
            float a = sampleIslandField(edgeA.x, edgeA.y);
            float b = sampleIslandField(edgeB.x, edgeB.y);
            float denominator = a - b;
            float t = Mathf.Abs(denominator) < 0.000001f ? 0.5f : Mathf.Clamp01(a / denominator);
            return Vector2.Lerp(edgeA, edgeB, t);
        }

        private static void AppendContourCell(
            Vector2[] corners,
            bool includeLand,
            Func<float, float, float> sampleIslandField,
            Func<Vector2, float> getLandHeight,
            Func<Vector2, float> getSurfaceHeight,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            float tileSize,
            ContourVertexCache vertexCache,
            int vertexDomain)
        {
            bool[] target = new bool[4];
            int mask = 0;
            for (int i = 0; i < 4; i++)
            {
                bool isLand = sampleIslandField(corners[i].x, corners[i].y) >= 0f;
                target[i] = includeLand ? isLand : !isLand;
                if (target[i]) mask |= 1 << i;
            }
            if (mask == 0) return;

            var polygons = new List<List<Vector2>>();
            Vector2[] crossings = new Vector2[4];
            bool[] hasCrossing = new bool[4];
            for (int edge = 0; edge < 4; edge++)
            {
                int next = (edge + 1) & 3;
                bool edgeCrosses = (sampleIslandField(corners[edge].x, corners[edge].y) >= 0f)
                                   != (sampleIslandField(corners[next].x, corners[next].y) >= 0f);
                hasCrossing[edge] = edgeCrosses;
                if (edgeCrosses)
                    crossings[edge] = FindBoundary(corners[edge], corners[next], sampleIslandField);
            }

            if (mask == 5 || mask == 10)
            {
                Vector2 center = (corners[0] + corners[2]) * 0.5f;
                bool centerLand = sampleIslandField(center.x, center.y) >= 0f;
                bool centerTarget = includeLand ? centerLand : !centerLand;
                BuildAmbiguousPolygons(mask, centerTarget, corners, crossings, polygons);
            }
            else
            {
                var polygon = new List<Vector2>(6);
                for (int i = 0; i < 4; i++)
                {
                    int next = (i + 1) % 4;
                    if (target[i]) polygon.Add(corners[i]);
                    if (hasCrossing[i]) polygon.Add(crossings[i]);
                }
                polygons.Add(polygon);
            }

            foreach (List<Vector2> polygon in polygons)
            {
                if (polygon.Count < 3) continue;
                Vector2 origin = polygon[0];
                for (int i = 1; i < polygon.Count - 1; i++)
                {
                    if (Mathf.Abs(CrossXZ(polygon[i] - origin, polygon[i + 1] - origin)) < 0.0001f)
                        continue;
                    AddContourVertex(origin, getLandHeight, getSurfaceHeight, vertices, uvs, triangles, tileSize, vertexCache, vertexDomain);
                    AddContourVertex(polygon[i], getLandHeight, getSurfaceHeight, vertices, uvs, triangles, tileSize, vertexCache, vertexDomain);
                    AddContourVertex(polygon[i + 1], getLandHeight, getSurfaceHeight, vertices, uvs, triangles, tileSize, vertexCache, vertexDomain);
                }
            }
        }

        private static void BuildAmbiguousPolygons(
            int mask,
            bool centerTarget,
            Vector2[] corners,
            Vector2[] crossings,
            List<List<Vector2>> polygons)
        {
            GetAmbiguousCrossingPairs(mask, centerTarget,
                out int pairA0, out int pairB0, out _, out _);
            bool targetConnected = mask == 5
                ? pairA0 == 0 && pairB0 == 1
                : pairA0 == 3 && pairB0 == 0;
            if (mask == 5)
            {
                if (targetConnected)
                    polygons.Add(new List<Vector2> { corners[0], crossings[0], crossings[1], corners[2], crossings[2], crossings[3] });
                else
                {
                    polygons.Add(new List<Vector2> { corners[0], crossings[0], crossings[3] });
                    polygons.Add(new List<Vector2> { corners[2], crossings[2], crossings[1] });
                }
            }
            else
            {
                if (targetConnected)
                    polygons.Add(new List<Vector2> { corners[1], crossings[1], crossings[2], corners[3], crossings[3], crossings[0] });
                else
                {
                    polygons.Add(new List<Vector2> { corners[1], crossings[1], crossings[0] });
                    polygons.Add(new List<Vector2> { corners[3], crossings[3], crossings[2] });
                }
            }
        }

        private static float CrossXZ(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static void GetAmbiguousCrossingPairs(int landMask, bool centerLand,
            out int firstPairA, out int firstPairB, out int secondPairA, out int secondPairB)
        {
            if ((landMask == 5 && centerLand) || (landMask == 10 && !centerLand))
            {
                firstPairA = 0; firstPairB = 1;
                secondPairA = 2; secondPairB = 3;
            }
            else
            {
                firstPairA = 3; firstPairB = 0;
                secondPairA = 1; secondPairB = 2;
            }
        }

        private static void AddContourVertex(
            Vector2 point,
            Func<Vector2, float> getLandHeight,
            Func<Vector2, float> getSurfaceHeight,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            float tileSize,
            ContourVertexCache vertexCache,
            int vertexDomain)
        {
            float height = getSurfaceHeight != null ? getSurfaceHeight(point) : getLandHeight(point);
            int index = vertexCache.GetOrAdd(point, height, tileSize, vertexDomain, vertices, uvs);
            triangles.Add(index);
        }

        private sealed class ContourVertexCache
        {
            private const float Quantization = 100000f;
            private readonly Dictionary<VertexKey, int> indices = new Dictionary<VertexKey, int>();

            public int GetOrAdd(Vector2 point, float height, float tileSize, int domain,
                List<Vector3> vertices, List<Vector2> uvs)
            {
                var key = new VertexKey(
                    Mathf.RoundToInt(point.x * Quantization),
                    Mathf.RoundToInt(point.y * Quantization), domain);
                if (indices.TryGetValue(key, out int existing)) return existing;

                int index = vertices.Count;
                vertices.Add(new Vector3(point.x, height, point.y));
                uvs.Add(new Vector2(point.x / tileSize, point.y / tileSize));
                indices.Add(key, index);
                return index;
            }
        }

        private readonly struct VertexKey : IEquatable<VertexKey>
        {
            public readonly int X;
            public readonly int Z;
            private readonly int domain;

            public VertexKey(int x, int z, int domain)
            {
                X = x;
                Z = z;
                this.domain = domain;
            }

            public bool Equals(VertexKey other) => X == other.X && Z == other.Z && domain == other.domain;
            public override bool Equals(object obj) => obj is VertexKey other && Equals(other);
            public override int GetHashCode() => unchecked((X * 397 ^ Z) * 397 ^ domain);
        }

        private static float WaterSurfaceHeight(Vector2 point, float waterY)
        {
            float ripple = (Mathf.PerlinNoise((point.x + 200f) * 0.12f, (point.y + 200f) * 0.12f) - 0.5f) * 0.04f;
            return waterY + ripple;
        }

        private static void AppendContourCliffSegments(
            Vector2[] corners,
            Func<float, float, float> sampleIslandField,
            Func<Vector3, float> getTerrainHeight,
            float cliffBaseY,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            var crossings = new List<Vector2>(4);
            for (int i = 0; i < 4; i++)
            {
                int next = (i + 1) % 4;
                bool a = sampleIslandField(corners[i].x, corners[i].y) >= 0f;
                bool b = sampleIslandField(corners[next].x, corners[next].y) >= 0f;
                if (a != b)
                    crossings.Add(FindBoundary(corners[i], corners[next], sampleIslandField));
            }

            if (crossings.Count == 2)
            {
                AddContourWall(crossings[0], crossings[1], sampleIslandField, getTerrainHeight,
                    cliffBaseY, vertices, triangles, uvs);
                return;
            }

            if (crossings.Count != 4) return;

            Vector2 center = (corners[0] + corners[2]) * 0.5f;
            bool centerLand = sampleIslandField(center.x, center.y) >= 0f;
            // Select the same diagonal resolution as AppendContourCell so cliff
            // segments and land/water triangles share exactly the same contour.
            int landMask = LandMask(corners, sampleIslandField);
            GetAmbiguousCrossingPairs(landMask, centerLand,
                out int a0, out int b0, out int a1, out int b1);
            AddContourWall(crossings[a0], crossings[b0], sampleIslandField, getTerrainHeight, cliffBaseY, vertices, triangles, uvs);
            AddContourWall(crossings[a1], crossings[b1], sampleIslandField, getTerrainHeight, cliffBaseY, vertices, triangles, uvs);
        }

        private static void AddContourWall(
            Vector2 a,
            Vector2 b,
            Func<float, float, float> sampleIslandField,
            Func<Vector3, float> getTerrainHeight,
            float cliffBaseY,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            Vector2 midpoint = (a + b) * 0.5f;
            Vector2 tangent = (b - a).normalized;
            Vector2 left = new Vector2(-tangent.y, tangent.x);
            bool leftIsLand = sampleIslandField(midpoint.x + left.x * 0.05f, midpoint.y + left.y * 0.05f) >= 0f;
            if (!leftIsLand)
            {
                Vector2 temp = a;
                a = b;
                b = temp;
            }

            float heightA = getTerrainHeight(new Vector3(a.x, 0f, a.y));
            float heightB = getTerrainHeight(new Vector3(b.x, 0f, b.y));
            if (Mathf.Max(heightA, heightB) <= CliffHeightThreshold) return;
            AddWallQuad( vertices, triangles, uvs,
                new Vector3(a.x, heightA, a.y), new Vector3(b.x, heightB, b.y), cliffBaseY);
        }

        private static Material BuildWaterMaterial(Material source, Color fallbackColor)
        {
            Material mat = source != null
                ? new Material(source)
                : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

            // Ensure URP Transparent alpha-blend mode so seabed shows through
            if (mat.HasProperty("_Surface"))   mat.SetFloat("_Surface", 1f);   // Transparent
            if (mat.HasProperty("_Blend"))     mat.SetFloat("_Blend",   0f);   // Alpha blend
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;

            if (mat.HasProperty("_BaseColor"))  mat.SetColor("_BaseColor", fallbackColor);
            if (mat.HasProperty("_Color"))      mat.SetColor("_Color", fallbackColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.88f);
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0.0f);

            return mat;
        }

        /// <summary>Returns approximate XZ distance to the nearest land point.</summary>
        private static float ApproxDistanceToLand(
            float wx, float wz,
            Func<float, float, bool> isInsideIsland,
            float cellSize)
        {
            if (isInsideIsland(wx, wz)) return 0f;

            float step = cellSize * 1.5f;
            for (float d = step; d <= cellSize * 18f; d += step)
            {
                for (int a = 0; a < 8; a++)
                {
                    float angle = a * Mathf.PI * 0.25f;
                    if (isInsideIsland(wx + Mathf.Cos(angle) * d, wz + Mathf.Sin(angle) * d))
                        return d;
                }
            }
            return cellSize * 20f;
        }

        // ── Cliff Walls ──────────────────────────────────────────────────────────

        /// <summary>
        /// Generates vertical cliff wall panels at tile boundaries where the land
        /// elevation exceeds <see cref="CliffHeightThreshold"/>.
        /// Works at the logical tile resolution (8-unit grid) for dramatic low-poly
        /// cliff faces. Each wall runs the full width of a tile and goes from the
        /// original terrain height down to <paramref name="cliffBaseY"/> (below sea level
        /// so there is no gap between the cliff and the seabed).
        /// </summary>
        public static void BuildCliffWalls(
            Transform parent,
            int gridSize,
            float tileSize,
            Material cliffMaterial,
            BiomeCatalogAsset catalog,
            Func<float, float, float> sampleIslandField,
            Func<Vector3, float> getTerrainHeight,
            Func<Vector3, string> getBiomeId,
            float cliffBaseY = -0.6f)
        {
            Vector3 halfSize = new Vector3(gridSize * tileSize * 0.5f, 0f, gridSize * tileSize * 0.5f);

            var verts = new List<Vector3>();
            var tris  = new List<int>();
            var uvs   = new List<Vector2>();

            int contourResolution = gridSize * TerrainInteriorSubdivPerTile;
            float contourCellSize = tileSize / TerrainInteriorSubdivPerTile;
            bool[,] refinedCells = BuildCoastlineRefinementMask(contourResolution, contourCellSize,
                halfSize, sampleIslandField);
            for (int cz = 0; cz < contourResolution; cz++)
            {
                for (int cx = 0; cx < contourResolution; cx++)
                {
                    if (!refinedCells[cx, cz]) continue;
                    Vector2[] corners = CellCorners(cx, cz, contourCellSize, halfSize);
                    float fineCellSize = contourCellSize / (CoastlineSubdivPerTile / TerrainInteriorSubdivPerTile);
                    for (int sz = 0; sz < 2; sz++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            Vector2[] fineCorners = CellCorners(cx * 2 + sx, cz * 2 + sz,
                                fineCellSize, halfSize);
                            int mask = LandMask(fineCorners, sampleIslandField);
                            if (mask == 0 || mask == 15) continue;
                            AppendContourCliffSegments(fineCorners, sampleIslandField, getTerrainHeight,
                                cliffBaseY, verts, tris, uvs);
                        }
                }
            }

            if (verts.Count == 0) return;

            Mesh mesh = new Mesh { name = "CliffWallsMesh" };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Resolve cliff material: prefer explicit, then stoneback_ridge, then grey fallback
            Material mat = cliffMaterial;
            if (mat == null) mat = catalog?.GetBiome("stoneback_ridge")?.GroundMaterial;
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", new Color(0.42f, 0.38f, 0.32f));  // dark grey-brown stone
            }

            GameObject go = new GameObject("CliffWallsMesh");
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        /// <summary>
        /// Adds a single rectangular cliff wall panel (two triangles) with normals
        /// pointing outward (away from land). The winding is chosen so that the
        /// cross-product of the two edge vectors points outward for every direction.
        /// </summary>
        private static void AddWallQuad(
            List<Vector3> verts, List<int> tris, List<Vector2> uvs,
            Vector3 topA, Vector3 topB, float bottomY)
        {
            int i = verts.Count;

            var botA = new Vector3(topA.x, bottomY, topA.z);
            var botB = new Vector3(topB.x, bottomY, topB.z);

            float wallW = Vector3.Distance(topA, topB);
            float wallH = topA.y - bottomY;
            const float uvScale = 8f;  // tile size for UV tiling

            verts.Add(topA); uvs.Add(new Vector2(0f,           wallH / uvScale));
            verts.Add(topB); uvs.Add(new Vector2(wallW / uvScale, wallH / uvScale));
            verts.Add(botA); uvs.Add(new Vector2(0f,           0f));
            verts.Add(botB); uvs.Add(new Vector2(wallW / uvScale, 0f));

            tris.Add(i);     tris.Add(i + 1); tris.Add(i + 2);
            tris.Add(i + 1); tris.Add(i + 3); tris.Add(i + 2);
        }
    }
}

