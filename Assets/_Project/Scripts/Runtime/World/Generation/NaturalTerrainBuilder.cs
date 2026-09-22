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
        private const int SubdivPerTile = 6;

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
        /// The coastline follows the IsInsideIsland() Perlin-noise border at 2-unit
        /// resolution, producing irregular, natural-looking shores.
        /// </summary>
    /// <summary>Terrain above this Y is treated as a cliff instead of a beach.</summary>
        private const float CliffHeightThreshold = 0.18f;

        public static void BuildIslandTerrain(
            Transform parent,
            int gridSize,
            float tileSize,
            BiomeCatalogAsset catalog,
            Func<float, float, bool> isInsideIsland,
            Func<Vector3, float> getTerrainHeight,
            Func<Vector3, string> getBiomeId)
        {
            int resolution   = gridSize * SubdivPerTile;
            float cellSize   = tileSize / SubdivPerTile;
            Vector3 halfSize = new Vector3(gridSize * tileSize * 0.5f, 0f, gridSize * tileSize * 0.5f);
            int vWidth = resolution + 1;

            // ── Pass 1: compute authoritative per-vertex surface heights and land flags ──
            var heightArr    = new float[vWidth, vWidth];
            var isLandArr    = new bool[vWidth, vWidth];

            for (int vz = 0; vz <= resolution; vz++)
            {
                for (int vx = 0; vx <= resolution; vx++)
                {
                    float wx = vx * cellSize - halfSize.x;
                    float wz = vz * cellSize - halfSize.z;

                    bool land = isInsideIsland(wx, wz);
                    isLandArr[vx, vz] = land;

                    if (land)
                    {
                        Vector3 p = new Vector3(wx, 0f, wz);
                        float h = getTerrainHeight(p);
                        heightArr[vx, vz] = h;
                    }
                }
            }

            // ── Pass 3: build vertex and UV arrays ────────────────────────────
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();

            // ── Pass 4: triangle lists grouped by biome ───────────────────────
            var biomeTriangles = new Dictionary<string, List<int>>();
            foreach (string b in BiomeSubmeshOrder)
                biomeTriangles[b] = new List<int>();

            for (int cz = 0; cz < resolution; cz++)
            {
                for (int cx = 0; cx < resolution; cx++)
                {
                    Vector2[] corners = CellCorners(cx, cz, cellSize, halfSize);
                    int mask = LandMask(corners, isInsideIsland);
                    if (mask == 0) continue;

                    float ccx = (cx + 0.5f) * cellSize - halfSize.x;
                    float ccz = (cz + 0.5f) * cellSize - halfSize.z;
                    string biomeId = getBiomeId(new Vector3(ccx, 0f, ccz));
                    if (!biomeTriangles.ContainsKey(biomeId))
                        biomeId = "south_thicket";

                    var tris = biomeTriangles[biomeId];
                    AppendContourCell(corners, true, isInsideIsland,
                        p => getTerrainHeight(new Vector3(p.x, 0f, p.y)),
                        null, vertices, uvs, tris, tileSize);
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
        /// Builds the unified water surface mesh. Uses the same 2-unit resolution as the
        /// land mesh so the coastline boundary between them aligns perfectly. The mesh
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
            Func<float, float, bool> isInsideIsland,
            float waterY = 0f)
        {
            int resolution   = gridSize * SubdivPerTile;   // 152
            float cellSize   = tileSize / SubdivPerTile;   // 2 units
            Vector3 halfSize = new Vector3(gridSize * tileSize * 0.5f, 0f, gridSize * tileSize * 0.5f);
            int vWidth = resolution + 1;

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();

            // Two submeshes: shallow (within ~2 tiles of shore) and deep water
            var shallowTris = new List<int>();
            var deepTris    = new List<int>();

            for (int cz = 0; cz < resolution; cz++)
            {
                for (int cx = 0; cx < resolution; cx++)
                {
                    float ccx = (cx + 0.5f) * cellSize - halfSize.x;
                    float ccz = (cz + 0.5f) * cellSize - halfSize.z;

                    Vector2[] corners = CellCorners(cx, cz, cellSize, halfSize);
                    int mask = LandMask(corners, isInsideIsland);
                    if (mask == 15) continue;                 // land – skip

                    float distToLand = ApproxDistanceToLand(ccx, ccz, isInsideIsland, cellSize);
                    bool shallow     = distToLand < tileSize * 1.8f;

                    var tris = shallow ? shallowTris : deepTris;
                    AppendContourCell(corners, false, isInsideIsland, null,
                        p => WaterSurfaceHeight(p, waterY), vertices, uvs, tris, tileSize);
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
            return new[] { new Vector2(x0, z0), new Vector2(x1, z0), new Vector2(x1, z1), new Vector2(x0, z1) };
        }

        private static int LandMask(Vector2[] corners, Func<float, float, bool> isInsideIsland)
        {
            int mask = 0;
            for (int i = 0; i < 4; i++)
                if (isInsideIsland(corners[i].x, corners[i].y)) mask |= 1 << i;
            return mask;
        }

        private static Vector2 FindBoundary(Vector2 land, Vector2 water, Func<float, float, bool> isInsideIsland)
        {
            bool landState = isInsideIsland(land.x, land.y);
            Vector2 lo = land;
            Vector2 hi = water;
            for (int i = 0; i < 10; i++)
            {
                Vector2 mid = (lo + hi) * 0.5f;
                if (isInsideIsland(mid.x, mid.y) == landState) lo = mid;
                else hi = mid;
            }
            Vector2 boundary = (lo + hi) * 0.5f;
            // Quantize the result so the same shared edge, visited in reverse
            // order by its neighboring cell, produces bit-identical vertices.
            return new Vector2(
                Mathf.Round(boundary.x * 100000f) / 100000f,
                Mathf.Round(boundary.y * 100000f) / 100000f);
        }

        private static void AppendContourCell(
            Vector2[] corners,
            bool includeLand,
            Func<float, float, bool> isInsideIsland,
            Func<Vector2, float> getLandHeight,
            Func<Vector2, float> getSurfaceHeight,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            float tileSize)
        {
            bool[] land = new bool[4];
            bool[] target = new bool[4];
            int mask = 0;
            for (int i = 0; i < 4; i++)
            {
                land[i] = isInsideIsland(corners[i].x, corners[i].y);
                target[i] = includeLand ? land[i] : !land[i];
                if (target[i]) mask |= 1 << i;
            }
            if (mask == 0) return;

            Vector2[] perimeter = { corners[0], corners[3], corners[2], corners[1] };
            var polygons = new List<List<Vector2>>();

            // The two diagonal cases need a center decision to cover the cell
            // completely without an overlap or a central hole.
            if (mask == 5 || mask == 10)
            {
                Vector2 e0 = FindBoundary(corners[0], corners[1], isInsideIsland);
                Vector2 e1 = FindBoundary(corners[1], corners[2], isInsideIsland);
                Vector2 e2 = FindBoundary(corners[2], corners[3], isInsideIsland);
                Vector2 e3 = FindBoundary(corners[3], corners[0], isInsideIsland);
                Vector2 center = (corners[0] + corners[2]) * 0.5f;
                bool centerTarget = includeLand
                    ? isInsideIsland(center.x, center.y)
                    : !isInsideIsland(center.x, center.y);

                if (mask == 5)
                {
                    polygons.Add(centerTarget
                        ? new List<Vector2> { corners[0], e0, center, e3 }
                        : new List<Vector2> { corners[0], e0, e3 });
                    polygons.Add(centerTarget
                        ? new List<Vector2> { corners[2], e2, center, e1 }
                        : new List<Vector2> { corners[2], e2, e1 });
                }
                else
                {
                    polygons.Add(centerTarget
                        ? new List<Vector2> { corners[1], e1, center, e0 }
                        : new List<Vector2> { corners[1], e1, e0 });
                    polygons.Add(centerTarget
                        ? new List<Vector2> { corners[3], e3, center, e2 }
                        : new List<Vector2> { corners[3], e3, e2 });
                }
            }
            else
            {
                var polygon = new List<Vector2>(6);
                for (int i = 0; i < 4; i++)
                {
                    int next = (i + 1) % 4;
                    if (target[i]) polygon.Add(perimeter[i]);
                    if (target[i] != target[next])
                        polygon.Add(FindBoundary(perimeter[i], perimeter[next], isInsideIsland));
                }
                polygons.Add(polygon);
            }

            foreach (List<Vector2> polygon in polygons)
            {
                if (polygon.Count < 3) continue;
                Vector2 origin = polygon[0];
                for (int i = 1; i < polygon.Count - 1; i++)
                {
                    AddContourVertex(origin, getLandHeight, getSurfaceHeight, vertices, uvs, triangles, tileSize);
                    AddContourVertex(polygon[i], getLandHeight, getSurfaceHeight, vertices, uvs, triangles, tileSize);
                    AddContourVertex(polygon[i + 1], getLandHeight, getSurfaceHeight, vertices, uvs, triangles, tileSize);
                }
            }
        }

        private static void AddContourVertex(
            Vector2 point,
            Func<Vector2, float> getLandHeight,
            Func<Vector2, float> getSurfaceHeight,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<int> triangles,
            float tileSize)
        {
            float height = getSurfaceHeight != null ? getSurfaceHeight(point) : getLandHeight(point);
            int index = vertices.Count;
            vertices.Add(new Vector3(point.x, height, point.y));
            uvs.Add(new Vector2(point.x / tileSize, point.y / tileSize));
            triangles.Add(index);
        }

        private static float WaterSurfaceHeight(Vector2 point, float waterY)
        {
            float ripple = (Mathf.PerlinNoise((point.x + 200f) * 0.12f, (point.y + 200f) * 0.12f) - 0.5f) * 0.04f;
            return waterY + ripple;
        }

        private static void AppendContourCliffSegments(
            Vector2[] corners,
            Func<float, float, bool> isInsideIsland,
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
                bool a = isInsideIsland(corners[i].x, corners[i].y);
                bool b = isInsideIsland(corners[next].x, corners[next].y);
                if (a != b)
                    crossings.Add(FindBoundary(corners[i], corners[next], isInsideIsland));
            }

            if (crossings.Count == 2)
            {
                AddContourWall(crossings[0], crossings[1], isInsideIsland, getTerrainHeight,
                    cliffBaseY, vertices, triangles, uvs);
                return;
            }

            if (crossings.Count != 4) return;

            Vector2 center = (corners[0] + corners[2]) * 0.5f;
            bool centerLand = isInsideIsland(center.x, center.y);
            // Select the same diagonal resolution as AppendContourCell so cliff
            // segments and land/water triangles share exactly the same contour.
            if (centerLand)
            {
                AddContourWall(crossings[0], crossings[1], isInsideIsland, getTerrainHeight, cliffBaseY, vertices, triangles, uvs);
                AddContourWall(crossings[2], crossings[3], isInsideIsland, getTerrainHeight, cliffBaseY, vertices, triangles, uvs);
            }
            else
            {
                AddContourWall(crossings[3], crossings[0], isInsideIsland, getTerrainHeight, cliffBaseY, vertices, triangles, uvs);
                AddContourWall(crossings[1], crossings[2], isInsideIsland, getTerrainHeight, cliffBaseY, vertices, triangles, uvs);
            }
        }

        private static void AddContourWall(
            Vector2 a,
            Vector2 b,
            Func<float, float, bool> isInsideIsland,
            Func<Vector3, float> getTerrainHeight,
            float cliffBaseY,
            List<Vector3> vertices,
            List<int> triangles,
            List<Vector2> uvs)
        {
            Vector2 midpoint = (a + b) * 0.5f;
            Vector2 tangent = (b - a).normalized;
            Vector2 left = new Vector2(-tangent.y, tangent.x);
            bool leftIsLand = isInsideIsland(midpoint.x + left.x * 0.05f, midpoint.y + left.y * 0.05f);
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
            Func<float, float, bool> isInsideIsland,
            Func<Vector3, float> getTerrainHeight,
            Func<Vector3, string> getBiomeId,
            float cliffBaseY = -0.6f)
        {
            Vector3 halfSize = new Vector3(gridSize * tileSize * 0.5f, 0f, gridSize * tileSize * 0.5f);

            var verts = new List<Vector3>();
            var tris  = new List<int>();
            var uvs   = new List<Vector2>();

            int contourResolution = gridSize * SubdivPerTile;
            float contourCellSize = tileSize / SubdivPerTile;
            for (int cz = 0; cz < contourResolution; cz++)
            {
                for (int cx = 0; cx < contourResolution; cx++)
                {
                    Vector2[] corners = CellCorners(cx, cz, contourCellSize, halfSize);
                    int mask = LandMask(corners, isInsideIsland);
                    if (mask == 0 || mask == 15) continue;

                    AppendContourCliffSegments(corners, isInsideIsland, getTerrainHeight, cliffBaseY,
                        verts, tris, uvs);
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

