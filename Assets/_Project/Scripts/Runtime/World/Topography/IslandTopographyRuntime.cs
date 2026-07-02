using System;
using System.Collections.Generic;
using UnityEngine;

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

        // ── Statistics (read by debug overlay) ───────────────────────────────
        public int LandCellCount      { get; private set; }
        public int WaterCellCount     { get; private set; }
        public int ShoreCellCount     { get; private set; }
        public int RidgeCellCount     { get; private set; }
        public int SafePlayerCells    { get; private set; }
        public int SafeCreatureCells  { get; private set; }

        public bool IsBuilt => _grid != null;

        // ── Build ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Populates the topography grid from the same functions used by WorldGeneratorRuntime.
        /// Must be called during world generation before any spawn system runs.
        /// </summary>
        public void Build(
            int   gridSize,
            float tileSize,
            Func<float, float, bool>         isInsideIsland,
            Func<Vector3, string, float>     getTerrainHeight,
            Func<Vector3, string>            determineBiome)
        {
            _gridSize = gridSize;
            _tileSize = tileSize;
            _originX  = -(gridSize * tileSize * 0.5f);
            _originZ  = -(gridSize * tileSize * 0.5f);

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
                        biomeId = determineBiome(p);
                        height  = getTerrainHeight(p, biomeId);
                    }
                    else
                    {
                        biomeId = "water";
                        height  = -0.35f;
                    }

                    tempBiome[x, z]  = biomeId;
                    tempHeight[x, z] = height;
                    tempType[x, z]   = ClassifyTerrain(biomeId, height, isLand);
                }
            }

            // ── Pass 2: detect shoreline, reclassify beach cells ─────────────
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
                        tempBiome[x, z],
                        tempType[x, z],
                        tempShore[x, z]);

                    _grid[x, z] = cell;

                    if (cell.IsWater) { WaterCellCount++; continue; }
                    LandCellCount++;
                    if (cell.IsShoreline)           ShoreCellCount++;
                    if (cell.IsRidge)               RidgeCellCount++;
                    if (cell.IsSafeForPlayerSpawn)  SafePlayerCells++;
                    if (cell.IsSafeForCreatureSpawn) SafeCreatureCells++;
                }
            }

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

        private static TerrainType ClassifyTerrain(string biomeId, float height, bool isLand)
        {
            if (!isLand) return TerrainType.Water;
            if (biomeId == "stoneback_ridge" || height > 0.55f) return TerrainType.Ridge;
            if (height > 0.18f)                                  return TerrainType.Hills;
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
