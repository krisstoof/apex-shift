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
            Func<Vector3, string, float> getTerrainHeight,
            Func<Vector3, string> determineBiome)
        {
            int resolution   = gridSize * SubdivPerTile;
            float cellSize   = tileSize / SubdivPerTile;
            Vector3 halfSize = new Vector3(gridSize * tileSize * 0.5f, 0f, gridSize * tileSize * 0.5f);
            int vWidth       = resolution + 1;

            // ── Pass 1: compute per-vertex heights, land flags, and original heights ──
            var heightArr    = new float[vWidth, vWidth];
            var origHeight   = new float[vWidth, vWidth];   // unsmoothed, used for cliff detection
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
                        float h = Mathf.Max(getTerrainHeight(p, determineBiome(p)), 0.01f);
                        heightArr[vx, vz] = h;
                        origHeight[vx, vz] = h;
                    }
                }
            }

            // ── Pass 2: coastal beach smoothing (beach only – cliffs are exempt) ──────
            // Low-lying coastal vertices blend to y=0 → gentle beach ramp.
            // High-elevation coastal vertices keep their height → cliff wall covers transition.
            for (int pass = 0; pass < 4; pass++)
            {
                for (int vz = 0; vz <= resolution; vz++)
                {
                    for (int vx = 0; vx <= resolution; vx++)
                    {
                        if (!isLandArr[vx, vz]) continue;
                        if (origHeight[vx, vz] > CliffHeightThreshold) continue;  // cliff – skip

                        bool adjWater =
                            (vx > 0          && !isLandArr[vx - 1, vz]) ||
                            (vx < resolution && !isLandArr[vx + 1, vz]) ||
                            (vz > 0          && !isLandArr[vx, vz - 1]) ||
                            (vz < resolution && !isLandArr[vx, vz + 1]) ||
                            (vx == 0 || vx == resolution || vz == 0 || vz == resolution);

                        if (adjWater)
                            heightArr[vx, vz] = Mathf.Lerp(heightArr[vx, vz], 0f, 0.72f);
                    }
                }
            }

            // ── Pass 3: build vertex and UV arrays ────────────────────────────
            Vector3[] vertices = new Vector3[vWidth * vWidth];
            Vector2[] uvs      = new Vector2[vWidth * vWidth];

            for (int vz = 0; vz <= resolution; vz++)
            {
                for (int vx = 0; vx <= resolution; vx++)
                {
                    float wx  = vx * cellSize - halfSize.x;
                    float wz  = vz * cellSize - halfSize.z;
                    int   idx = vz * vWidth + vx;
                    vertices[idx] = new Vector3(wx, heightArr[vx, vz], wz);
                    uvs[idx]      = new Vector2(wx / tileSize, wz / tileSize);
                }
            }

            // ── Pass 4: triangle lists grouped by biome ───────────────────────
            var biomeTriangles = new Dictionary<string, List<int>>();
            foreach (string b in BiomeSubmeshOrder)
                biomeTriangles[b] = new List<int>();

            for (int cz = 0; cz < resolution; cz++)
            {
                for (int cx = 0; cx < resolution; cx++)
                {
                    float ccx = (cx + 0.5f) * cellSize - halfSize.x;
                    float ccz = (cz + 0.5f) * cellSize - halfSize.z;

                    if (!isInsideIsland(ccx, ccz)) continue;

                    string biomeId = determineBiome(new Vector3(ccx, 0f, ccz));
                    if (!biomeTriangles.ContainsKey(biomeId))
                        biomeId = "south_thicket";

                    int v00 = cz       * vWidth + cx;
                    int v10 = cz       * vWidth + (cx + 1);
                    int v01 = (cz + 1) * vWidth + cx;
                    int v11 = (cz + 1) * vWidth + (cx + 1);

                    var tris = biomeTriangles[biomeId];
                    tris.Add(v00); tris.Add(v01); tris.Add(v11);
                    tris.Add(v00); tris.Add(v11); tris.Add(v10);
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
            mesh.vertices     = vertices;
            mesh.uv           = uvs;
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
            int vWidth       = resolution + 1;

            Vector3[] vertices = new Vector3[vWidth * vWidth];
            Vector2[] uvs      = new Vector2[vWidth * vWidth];

            for (int vz = 0; vz <= resolution; vz++)
            {
                for (int vx = 0; vx <= resolution; vx++)
                {
                    float wx = vx * cellSize - halfSize.x;
                    float wz = vz * cellSize - halfSize.z;

                    // Slight Perlin ripple in the vertex grid so the water isn't perfectly flat
                    float ripple = (Mathf.PerlinNoise((wx + 200f) * 0.12f, (wz + 200f) * 0.12f) - 0.5f) * 0.04f;

                    int idx    = vz * vWidth + vx;
                    vertices[idx] = new Vector3(wx, waterY + ripple, wz);
                    uvs[idx]      = new Vector2(wx / tileSize, wz / tileSize);
                }
            }

            // Two submeshes: shallow (within ~2 tiles of shore) and deep water
            var shallowTris = new List<int>();
            var deepTris    = new List<int>();

            for (int cz = 0; cz < resolution; cz++)
            {
                for (int cx = 0; cx < resolution; cx++)
                {
                    float ccx = (cx + 0.5f) * cellSize - halfSize.x;
                    float ccz = (cz + 0.5f) * cellSize - halfSize.z;

                    if (isInsideIsland(ccx, ccz)) continue;   // land – skip

                    float distToLand = ApproxDistanceToLand(ccx, ccz, isInsideIsland, cellSize);
                    bool shallow     = distToLand < tileSize * 1.8f;

                    int v00 = cz       * vWidth + cx;
                    int v10 = cz       * vWidth + (cx + 1);
                    int v01 = (cz + 1) * vWidth + cx;
                    int v11 = (cz + 1) * vWidth + (cx + 1);

                    var tris = shallow ? shallowTris : deepTris;
                    tris.Add(v00); tris.Add(v01); tris.Add(v11);
                    tris.Add(v00); tris.Add(v11); tris.Add(v10);
                }
            }

            int subCount = (shallowTris.Count > 0 ? 1 : 0) + (deepTris.Count > 0 ? 1 : 0);
            if (subCount == 0) return null;

            Mesh mesh = new Mesh
            {
                name        = "WaterSurfaceMesh",
                indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
            };
            mesh.vertices     = vertices;
            mesh.uv           = uvs;
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

            // ── Physics floor: flat BoxCollider instead of MeshCollider ─────────
            // MeshCollider on a large irregular mesh creates invisible micro-edges
            // at tile boundaries that the CharacterController interprets as walls,
            // preventing the player from crossing from land onto the water surface.
            // A single flat BoxCollider has no such edges – the player walks onto it
            // smoothly from any direction.  Top face is at waterY so it sits exactly
            // at the water surface; the land MeshCollider (always above waterY on
            // land) takes priority there so the player never "floats" above terrain.
            float worldSpan = gridSize * tileSize;
            BoxCollider waterFloor = go.AddComponent<BoxCollider>();
            waterFloor.center = new Vector3(0f, waterY - 0.05f, 0f);  // top face = waterY
            waterFloor.size   = new Vector3(worldSpan, 0.1f, worldSpan);

            // Trigger for PlayerWaterDetector – covers the entire water area at surface height
            Bounds b = mesh.bounds;
            if (Application.isPlaying)
            {
                BoxCollider trigger = go.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.center    = new Vector3(0f, 0.7f, 0f);    // slightly above surface
                trigger.size      = new Vector3(b.size.x, 1.4f, b.size.z);
                go.AddComponent<WaterVolume>();
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
            Func<Vector3, string, float> getTerrainHeight,
            Func<Vector3, string> determineBiome,
            float cliffBaseY = -0.6f)
        {
            Vector3 halfSize = new Vector3(gridSize * tileSize * 0.5f, 0f, gridSize * tileSize * 0.5f);

            var verts = new List<Vector3>();
            var tris  = new List<int>();
            var uvs   = new List<Vector2>();

            for (int gz = 0; gz < gridSize; gz++)
            {
                for (int gx = 0; gx < gridSize; gx++)
                {
                    // Tile world-space centre and corners
                    float cx = gx * tileSize - halfSize.x + tileSize * 0.5f;
                    float cz = gz * tileSize - halfSize.z + tileSize * 0.5f;

                    if (!isInsideIsland(cx, cz)) continue;

                    Vector3 p = new Vector3(cx, 0f, cz);
                    float h = Mathf.Max(getTerrainHeight(p, determineBiome(p)), 0.01f);

                    if (h <= CliffHeightThreshold) continue;  // beach area – no cliff

                    float x0 = gx * tileSize - halfSize.x;
                    float x1 = x0 + tileSize;
                    float z0 = gz * tileSize - halfSize.z;
                    float z1 = z0 + tileSize;

                    // +X edge
                    if (!isInsideIsland(cx + tileSize, cz))
                        AddWallQuad(verts, tris, uvs,
                            new Vector3(x1, h, z0), new Vector3(x1, h, z1), cliffBaseY);

                    // -X edge
                    if (!isInsideIsland(cx - tileSize, cz))
                        AddWallQuad(verts, tris, uvs,
                            new Vector3(x0, h, z1), new Vector3(x0, h, z0), cliffBaseY);

                    // +Z edge
                    if (!isInsideIsland(cx, cz + tileSize))
                        AddWallQuad(verts, tris, uvs,
                            new Vector3(x1, h, z1), new Vector3(x0, h, z1), cliffBaseY);

                    // -Z edge
                    if (!isInsideIsland(cx, cz - tileSize))
                        AddWallQuad(verts, tris, uvs,
                            new Vector3(x0, h, z0), new Vector3(x1, h, z0), cliffBaseY);
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

