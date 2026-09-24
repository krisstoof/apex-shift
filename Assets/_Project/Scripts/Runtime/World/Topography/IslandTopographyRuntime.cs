using System;
using System.Collections.Generic;
using UnityEngine;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Biomes;

namespace ApexShift.Runtime.World.Topography
{
    /// <summary>
    /// Single source of truth for island topography data.
    ///
    /// Built once by WorldGeneratorRuntime after the grid pass completes.
    /// All other systems – spawning, WorldBounds, CreatureIslandBounds, minimap,
    /// debug tools – query this runtime instead of recomputing Perlin noise themselves.
    ///
    /// Godot parity note: mirrors the role of WorldTopography / IslandData in the Godot
    /// prototype, which provided height-map, terrain-type and safe-point queries to every
    /// subsystem that needed spatial awareness of the island.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IslandTopographyRuntime : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static IslandTopographyRuntime Active { get; private set; }

        private void OnEnable()  { Active = this; }
        private void OnDisable() { if (Active == this) Active = null; }

        // ── Grid ──────────────────────────────────────────────────────────────
        private TopographyCell[,] _grid;
        private int   _gridSize;
        private float _tileSize;
        private float _originX;   // world X of grid cell (0,0) left edge
        private float _originZ;
        private string[,] _biomeMap;
        private int _biomeMapSize;
        private float _biomeMapCellSize;

        // ── Statistics (read by debug overlay) ───────────────────────────────
        public int LandCellCount      { get; private set; }
        public int WaterCellCount     { get; private set; }
        public int ShoreCellCount     { get; private set; }
        public int RidgeCellCount     { get; private set; }
        public int SafePlayerCells    { get; private set; }
        public int SafeCreatureCells  { get; private set; }

        public bool IsBuilt => _grid != null;
        public int DenseBiomeMapResolutionPerTile => _gridSize > 0 ? _biomeMapSize / _gridSize : 0;
        public float DenseBiomeMapCellSize => _biomeMapCellSize;

        // ── Build ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Populates the topography grid from the same functions used by WorldGeneratorRuntime.
        /// Must be called during world generation before any spawn system runs.
        /// </summary>
        public void Build(
            int   gridSize,
            float tileSize,
            Func<float, float, bool>         isInsideIsland,
            Func<Vector3, float>              getTerrainHeight,
            Func<Vector3, string>             determineBiome,
            TerrainHeightfieldSettings       terrainSettings = null,
            BiomeFieldGenerator              biomeField = null,
            BiomeClassifier                   biomeClassifier = null,
            int                               biomeResolutionPerTile = 12)
        {
            _gridSize = gridSize;
            _tileSize = tileSize;
            _originX  = -(gridSize * tileSize * 0.5f);
            _originZ  = -(gridSize * tileSize * 0.5f);
            terrainSettings = terrainSettings ?? new TerrainHeightfieldSettings();

            // ── Pass 1: classify each cell ────────────────────────────────────
            var tempType      = new TerrainType[gridSize, gridSize];
            var tempHeight    = new float[gridSize, gridSize];
            var tempBiome     = new string[gridSize, gridSize];
            var tempIsLand    = new bool[gridSize, gridSize];

            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    float wx = _originX + x * tileSize + tileSize * 0.5f;
                    float wz = _originZ + z * tileSize + tileSize * 0.5f;

                    bool isLand = isInsideIsland(wx, wz);
                    tempIsLand[x, z] = isLand;

                    string biomeId;
                    float  height;
                    if (isLand)
                    {
                        Vector3 p = new Vector3(wx, 0f, wz);
                        biomeId = biomeField == null ? determineBiome(p) : null;
                        height  = getTerrainHeight(p);
                    }
                    else
                    {
                        biomeId = "water";
                        height  = -0.35f;
                    }

                    tempBiome[x, z]  = biomeId;
                    tempHeight[x, z] = height;
                }
            }

            float minLandHeight = float.MaxValue;
            float maxLandHeight = float.MinValue;
            for (int z = 0; z < gridSize; z++)
                for (int x = 0; x < gridSize; x++)
                    if (tempIsLand[x, z])
                    {
                        minLandHeight = Mathf.Min(minLandHeight, tempHeight[x, z]);
                        maxLandHeight = Mathf.Max(maxLandHeight, tempHeight[x, z]);
                    }

            float heightRange = Mathf.Max(0.0001f, maxLandHeight - minLandHeight);
            if (biomeField != null && biomeClassifier != null)
            {
                for (int z = 0; z < gridSize; z++)
                    for (int x = 0; x < gridSize; x++)
                        if (tempIsLand[x, z])
                        {
                            float elevation = Mathf.Clamp01((tempHeight[x, z] - minLandHeight) / heightRange);
                            float moisture = biomeField.SampleMoisture(_originX + x * tileSize + tileSize * 0.5f, _originZ + z * tileSize + tileSize * 0.5f);
                            float temperature = biomeField.SampleTemperature(_originX + x * tileSize + tileSize * 0.5f, _originZ + z * tileSize + tileSize * 0.5f, elevation);
                            var sample = new BiomeEnvironmentSample(elevation, CalculateSlope(tempHeight, x, z, gridSize, tileSize), moisture, temperature);
                            tempBiome[x, z] = biomeClassifier.Classify(new Vector3(_originX + x * tileSize + tileSize * 0.5f, 0f, _originZ + z * tileSize + tileSize * 0.5f), sample);
                        }

            }

            // Classification follows final climate/biome results. Shoreline's Beach
            // override is applied after this pass, while steep/ridge cliffs stay Ridge.
            for (int z = 0; z < gridSize; z++)
                for (int x = 0; x < gridSize; x++)
                    tempType[x, z] = ClassifyTerrain(
                        tempBiome[x, z], tempIsLand[x, z] ? Mathf.Clamp01((tempHeight[x, z] - minLandHeight) / heightRange) : 0f,
                        CalculateSlope(tempHeight, x, z, gridSize, tileSize), tempIsLand[x, z]);

            // Shoreline classification follows terrain-type classification.
            var tempShore = new bool[gridSize, gridSize];
            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    if (!tempIsLand[x, z]) continue;
                    if (HasWaterNeighbor(x, z, tempIsLand, gridSize))
                    {
                        tempShore[x, z] = true;
                        // Coastal land → Beach unless it is already a ridge cliff
                        if (tempType[x, z] != TerrainType.Ridge)
                            tempType[x, z] = TerrainType.Beach;
                    }
                }
            }

            // ── Pass 3: build immutable cell objects ─────────────────────────
            _grid = new TopographyCell[gridSize, gridSize];
            LandCellCount = WaterCellCount = ShoreCellCount = RidgeCellCount = 0;
            SafePlayerCells = SafeCreatureCells = 0;

            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    float wx = _originX + x * tileSize + tileSize * 0.5f;
                    float wz = _originZ + z * tileSize + tileSize * 0.5f;

                    var cell = new TopographyCell(
                        x, z,
                        new Vector3(wx, tempHeight[x, z], wz),
                        tempHeight[x, z],
                        tempIsLand[x, z] ? Mathf.Clamp01((tempHeight[x, z] - minLandHeight) / heightRange) : 0f,
                        CalculateSlope(tempHeight, x, z, gridSize, tileSize),
                        tempBiome[x, z],
                        tempType[x, z],
                        tempShore[x, z],
                        biomeField != null && tempIsLand[x, z] ? biomeField.SampleMoisture(wx, wz) : 0.5f,
                        biomeField != null && tempIsLand[x, z] ? biomeField.SampleTemperature(wx, wz, tempIsLand[x, z] ? Mathf.Clamp01((tempHeight[x, z] - minLandHeight) / heightRange) : 0f) : 0.5f,
                        terrainSettings.PlayerSafeSlopeDegrees,
                        terrainSettings.CreatureSafeSlopeDegrees,
                        terrainSettings.ResourceSafeSlopeDegrees);

                    _grid[x, z] = cell;

                    if (cell.IsWater) { WaterCellCount++; continue; }
                    LandCellCount++;
                    if (cell.IsShoreline)           ShoreCellCount++;
                    if (cell.IsRidge)               RidgeCellCount++;
                    if (cell.IsSafeForPlayerSpawn)  SafePlayerCells++;
                    if (cell.IsSafeForCreatureSpawn) SafeCreatureCells++;
                }
            }

            BuildDenseBiomeMap(gridSize, tileSize, isInsideIsland, getTerrainHeight, minLandHeight, heightRange,
                biomeField, biomeClassifier, biomeResolutionPerTile);

            Debug.Log($"[Topography] Built {gridSize}×{gridSize} grid. " +
                      $"Land={LandCellCount} Water={WaterCellCount} " +
                      $"Shore={ShoreCellCount} Ridge={RidgeCellCount} " +
                      $"SafePlayer={SafePlayerCells} SafeCreature={SafeCreatureCells}");
        }

        // ── Query API ─────────────────────────────────────────────────────────

        /// <summary>Returns the cell that contains world-space (wx, wz), or null if out of range.</summary>
        public TopographyCell GetCellAt(float wx, float wz)
        {
            if (_grid == null) return null;
            int x = Mathf.Clamp(Mathf.FloorToInt((wx - _originX) / _tileSize), 0, _gridSize - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((wz - _originZ) / _tileSize), 0, _gridSize - 1);
            return _grid[x, z];
        }

        public TopographyCell GetCellAt(Vector3 worldPos) => GetCellAt(worldPos.x, worldPos.z);

        public string GetBiomeIdAt(Vector3 worldPos)
        {
            if (_biomeMap == null) return GetCellAt(worldPos)?.BiomeId ?? "default";
            int x = Mathf.Clamp(Mathf.FloorToInt((worldPos.x - _originX) / _biomeMapCellSize), 0, _biomeMapSize - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((worldPos.z - _originZ) / _biomeMapCellSize), 0, _biomeMapSize - 1);
            return _biomeMap[x, z] ?? "water";
        }

        /// <summary>Returns the cell at grid indices, or null if out of range.</summary>
        public TopographyCell GetCell(int gx, int gz)
        {
            if (_grid == null || gx < 0 || gx >= _gridSize || gz < 0 || gz >= _gridSize)
                return null;
            return _grid[gx, gz];
        }

        public bool IsLandAt(float wx, float wz)      => GetCellAt(wx, wz)?.IsLand  ?? false;
        public bool IsWaterAt(float wx, float wz)     => GetCellAt(wx, wz)?.IsWater ?? true;
        public bool IsShorelineAt(float wx, float wz) => GetCellAt(wx, wz)?.IsShoreline ?? false;

        public bool IsSafeForCreatureAt(float wx, float wz)
            => GetCellAt(wx, wz)?.IsSafeForCreatureSpawn ?? false;

        public bool IsSafeForResourceAt(float wx, float wz)
            => GetCellAt(wx, wz)?.IsSafeForResourceSpawn ?? false;

        /// <summary>
        /// Returns the world-space center of the safest player spawn point.
        /// Prioritises hearth_meadow/plain cells closest to world origin.
        /// Falls back to nearest safe land cell if none found in inner radius.
        /// </summary>
        public Vector3 GetSafePlayerSpawnPoint()
        {
            if (_grid == null) return Vector3.up * 0.1f;

            TopographyCell best = null;
            float bestSqr = float.MaxValue;

            for (int z = 0; z < _gridSize; z++)
            {
                for (int x = 0; x < _gridSize; x++)
                {
                    var c = _grid[x, z];
                    if (!c.IsSafeForPlayerSpawn) continue;

                    // Prefer hearth_meadow / plain for starting area
                    if (c.TerrainType != TerrainType.Plain && c.TerrainType != TerrainType.Forest)
                        continue;

                    float sqr = c.WorldCenter.x * c.WorldCenter.x + c.WorldCenter.z * c.WorldCenter.z;
                    if (sqr < bestSqr) { bestSqr = sqr; best = c; }
                }
            }

            // Fallback: any safe land cell
            if (best == null)
            {
                bestSqr = float.MaxValue;
                for (int z = 0; z < _gridSize; z++)
                {
                    for (int x = 0; x < _gridSize; x++)
                    {
                        var c = _grid[x, z];
                        if (!c.IsSafeForPlayerSpawn) continue;
                        float sqr = c.WorldCenter.x * c.WorldCenter.x + c.WorldCenter.z * c.WorldCenter.z;
                        if (sqr < bestSqr) { bestSqr = sqr; best = c; }
                    }
                }
            }

            return best?.WorldCenter ?? Vector3.up * 0.1f;
        }

        /// <summary>Returns all land cell world centres (used by WorldBounds).</summary>
        public List<Vector3> GetAllLandCenters()
        {
            var list = new List<Vector3>(LandCellCount);
            if (_grid == null) return list;
            for (int z = 0; z < _gridSize; z++)
                for (int x = 0; x < _gridSize; x++)
                    if (_grid[x, z].IsLand) list.Add(_grid[x, z].WorldCenter);
            return list;
        }

        /// <summary>
        /// Returns centres of all cells the player is allowed to enter:
        /// land cells + water cells within <paramref name="shallowWaterTiles"/> tiles of the shore.
        /// Used by WorldBounds so the player can wade / swim in shallow water.
        /// </summary>
        public List<Vector3> GetNavigableCenters(int shallowWaterTiles = 3)
        {
            var list = new List<Vector3>(LandCellCount + ShoreCellCount * shallowWaterTiles * 2);
            if (_grid == null) return list;

            for (int z = 0; z < _gridSize; z++)
            {
                for (int x = 0; x < _gridSize; x++)
                {
                    var c = _grid[x, z];
                    if (c.IsLand)
                    {
                        list.Add(c.WorldCenter);
                        continue;
                    }

                    // Water cell: include if within shallowWaterTiles Manhattan steps of land
                    bool nearLand = false;
                    for (int dz = -shallowWaterTiles; dz <= shallowWaterTiles && !nearLand; dz++)
                    {
                        for (int dx = -shallowWaterTiles; dx <= shallowWaterTiles && !nearLand; dx++)
                        {
                            if (Mathf.Abs(dx) + Mathf.Abs(dz) > shallowWaterTiles) continue;
                            int nx = x + dx, nz = z + dz;
                            if (nx >= 0 && nx < _gridSize && nz >= 0 && nz < _gridSize
                                && _grid[nx, nz].IsLand)
                                nearLand = true;
                        }
                    }
                    if (nearLand) list.Add(c.WorldCenter);
                }
            }

            return list;
        }

        /// <summary>Returns land centres that are safe for creature movement (no shoreline).</summary>
        public List<Vector3> GetSafeCreatureLandCenters()
        {
            var list = new List<Vector3>(SafeCreatureCells);
            if (_grid == null) return list;
            for (int z = 0; z < _gridSize; z++)
                for (int x = 0; x < _gridSize; x++)
                {
                    var c = _grid[x, z];
                    if (c.IsSafeForCreatureSpawn) list.Add(c.WorldCenter);
                }
            return list;
        }

        /// <summary>
        /// Returns an array of all cells for minimap/debug rendering.
        /// Caller should not modify the array contents.
        /// </summary>
        public TopographyCell[,] GetGridReadOnly() => _grid;
        public int GridSize  => _gridSize;
        public float TileSize => _tileSize;

        // ── Private helpers ───────────────────────────────────────────────────

        private static float CalculateSlope(float[,] heights, int x, int z, int size, float tileSize)
        {
            if (size <= 1 || tileSize <= 0f) return 0f;
            int left = Mathf.Max(0, x - 1);
            int right = Mathf.Min(size - 1, x + 1);
            int down = Mathf.Max(0, z - 1);
            int up = Mathf.Min(size - 1, z + 1);
            float dx = (heights[right, z] - heights[left, z]) / ((right - left) * tileSize);
            float dz = (heights[x, up] - heights[x, down]) / ((up - down) * tileSize);
            return Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
        }

        private void BuildDenseBiomeMap(int gridSize, float tileSize, Func<float, float, bool> isInsideIsland,
            Func<Vector3, float> getTerrainHeight, float minHeight, float heightRange,
            BiomeFieldGenerator field, BiomeClassifier classifier, int resolutionPerTile)
        {
            if (field == null || classifier == null) { _biomeMap = null; return; }
            _biomeMapSize = Mathf.Max(1, gridSize * Mathf.Max(1, resolutionPerTile));
            _biomeMapCellSize = tileSize / Mathf.Max(1, resolutionPerTile);
            _biomeMap = new string[_biomeMapSize, _biomeMapSize];
            for (int z = 0; z < _biomeMapSize; z++)
                for (int x = 0; x < _biomeMapSize; x++)
                {
                    float wx = _originX + (x + 0.5f) * _biomeMapCellSize;
                    float wz = _originZ + (z + 0.5f) * _biomeMapCellSize;
                    if (!isInsideIsland(wx, wz)) { _biomeMap[x, z] = "water"; continue; }
                    float h = getTerrainHeight(new Vector3(wx, 0f, wz));
                    float hx = getTerrainHeight(new Vector3(wx + _biomeMapCellSize, 0f, wz)) - getTerrainHeight(new Vector3(wx - _biomeMapCellSize, 0f, wz));
                    float hz = getTerrainHeight(new Vector3(wx, 0f, wz + _biomeMapCellSize)) - getTerrainHeight(new Vector3(wx, 0f, wz - _biomeMapCellSize));
                    float slope = Mathf.Atan(Mathf.Sqrt(hx * hx + hz * hz) / Mathf.Max(0.01f, 2f * _biomeMapCellSize)) * Mathf.Rad2Deg;
                    float elevation = Mathf.Clamp01((h - minHeight) / heightRange);
                    var sample = new BiomeEnvironmentSample(elevation, slope, field.SampleMoisture(wx, wz), field.SampleTemperature(wx, wz, elevation));
                    _biomeMap[x, z] = classifier.Classify(new Vector3(wx, 0f, wz), sample);
                }

            // Region centers are the authoritative coarse samples. Pin those
            // cache entries to the cell IDs so all public query paths agree.
            for (int z = 0; z < gridSize; z++)
                for (int x = 0; x < gridSize; x++)
                {
                    TopographyCell cell = _grid[x, z];
                    int mapX = Mathf.Clamp(Mathf.FloorToInt((cell.WorldCenter.x - _originX) / _biomeMapCellSize), 0, _biomeMapSize - 1);
                    int mapZ = Mathf.Clamp(Mathf.FloorToInt((cell.WorldCenter.z - _originZ) / _biomeMapCellSize), 0, _biomeMapSize - 1);
                    _biomeMap[mapX, mapZ] = cell.BiomeId;
                }
        }

        private static TerrainType ClassifyTerrain(string biomeId, float height, float slopeDegrees, bool isLand)
        {
            if (!isLand) return TerrainType.Water;
            if (biomeId == "stoneback_ridge" || height > 0.55f || slopeDegrees > 28f) return TerrainType.Ridge;
            if (height > 0.18f || slopeDegrees > 12f)                                  return TerrainType.Hills;
            if (biomeId == "westwood" || biomeId == "south_thicket") return TerrainType.Forest;
            return TerrainType.Plain;  // hearth_meadow, redfang_wilds, generic
        }

        private static bool HasWaterNeighbor(int x, int z, bool[,] isLand, int size)
        {
            if (x == 0 || x == size - 1 || z == 0 || z == size - 1) return true; // world edge
            return !isLand[x + 1, z] || !isLand[x - 1, z]
                || !isLand[x, z + 1] || !isLand[x, z - 1];
        }

#if UNITY_EDITOR
        // ── Editor gizmos ─────────────────────────────────────────────────────
        [SerializeField] private bool drawGizmos;

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || _grid == null) return;

            for (int z = 0; z < _gridSize; z++)
            {
                for (int x = 0; x < _gridSize; x++)
                {
                    var c = _grid[x, z];
                    Gizmos.color = GizmoColor(c);
                    Gizmos.DrawWireCube(
                        c.WorldCenter + Vector3.up * 0.15f,
                        new Vector3(_tileSize * 0.8f, 0.05f, _tileSize * 0.8f));
                }
            }
        }

        private static Color GizmoColor(TopographyCell c)
        {
            return c.TerrainType switch
            {
                TerrainType.Water  => new Color(0.2f, 0.4f, 0.9f, 0.4f),
                TerrainType.Beach  => new Color(0.9f, 0.85f, 0.5f, 0.6f),
                TerrainType.Plain  => new Color(0.5f, 0.85f, 0.4f, 0.5f),
                TerrainType.Forest => new Color(0.1f, 0.5f, 0.1f, 0.5f),
                TerrainType.Hills  => new Color(0.7f, 0.6f, 0.3f, 0.5f),
                TerrainType.Ridge  => new Color(0.5f, 0.4f, 0.3f, 0.6f),
                _                  => Color.white
            };
        }
#endif
    }
}
