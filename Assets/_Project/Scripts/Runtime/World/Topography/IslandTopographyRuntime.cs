using System;
using System.Collections.Generic;
using UnityEngine;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Biomes;
using ApexShift.Runtime.World.Vegetation;
using ApexShift.Runtime.World.Environment;

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
        private string[,] _habitatMap;
        private EnvironmentSample[,] _environmentMap;
        private int _habitatMapSize;
        private float _habitatMapCellSize;

        // ── Statistics (read by debug overlay) ───────────────────────────────
        public int LandCellCount      { get; private set; }
        public int WaterCellCount     { get; private set; }
        public int ShoreCellCount     { get; private set; }
        public int RidgeCellCount     { get; private set; }
        public int SafePlayerCells    { get; private set; }
        public int SafeCreatureCells  { get; private set; }

        public bool IsBuilt => _grid != null;
        public int DenseHabitatMapResolutionPerTile => _gridSize > 0 ? _habitatMapSize / _gridSize : 0;
        public float DenseHabitatMapCellSize => _habitatMapCellSize;
        public int DenseBiomeMapResolutionPerTile => DenseHabitatMapResolutionPerTile;
        public float DenseBiomeMapCellSize => DenseHabitatMapCellSize;
        public Bounds WorldBounds => new Bounds(
            new Vector3(_originX + _gridSize * _tileSize * 0.5f, 0f, _originZ + _gridSize * _tileSize * 0.5f),
            new Vector3(_gridSize * _tileSize, 0.1f, _gridSize * _tileSize));

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
            int                               biomeResolutionPerTile = 12,
            HabitatClassificationSettings    habitatSettings = null,
            HabitatClassifier                 habitatClassifier = null)
        {
            _gridSize = gridSize;
            _tileSize = tileSize;
            _originX  = -(gridSize * tileSize * 0.5f);
            _originZ  = -(gridSize * tileSize * 0.5f);
            terrainSettings = terrainSettings ?? new TerrainHeightfieldSettings();

            // ── Pass 1: classify each cell ────────────────────────────────────
            var tempType      = new TerrainType[gridSize, gridSize];
            var tempHeight    = new float[gridSize, gridSize];
            var tempIsLand    = new bool[gridSize, gridSize];
            var tempMoisture  = new float[gridSize, gridSize];
            var tempTemperature = new float[gridSize, gridSize];
            var tempHabitat = new string[gridSize, gridSize];

            for (int z = 0; z < gridSize; z++)
            {
                for (int x = 0; x < gridSize; x++)
                {
                    float wx = _originX + x * tileSize + tileSize * 0.5f;
                    float wz = _originZ + z * tileSize + tileSize * 0.5f;

                    bool isLand = isInsideIsland(wx, wz);
                    tempIsLand[x, z] = isLand;

                    float  height;
                    if (isLand)
                    {
                        Vector3 p = new Vector3(wx, 0f, wz);
                        height  = getTerrainHeight(p);
                        tempMoisture[x, z] = biomeField != null ? biomeField.SampleMoisture(wx, wz) : 0.5f;
                    }
                    else
                    {
                        height  = -0.35f;
                        tempMoisture[x, z] = 0.5f;
                    }
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
            for (int z = 0; z < gridSize; z++)
                for (int x = 0; x < gridSize; x++)
                {
                    float elevation = tempIsLand[x, z] ? Mathf.Clamp01((tempHeight[x, z] - minLandHeight) / heightRange) : 0f;
                    float wx = _originX + (x + 0.5f) * tileSize, wz = _originZ + (z + 0.5f) * tileSize;
                    tempTemperature[x, z] = tempIsLand[x, z] && biomeField != null
                        ? biomeField.SampleTemperature(wx, wz, elevation) : 0.5f;
                }

            // Terrain identity derives only from physical and climate fields.
            for (int z = 0; z < gridSize; z++)
                for (int x = 0; x < gridSize; x++)
                    tempType[x, z] = ClassifyTerrain(
                        tempIsLand[x, z] ? Mathf.Clamp01((tempHeight[x, z] - minLandHeight) / heightRange) : 0f,
                        CalculateSlope(tempHeight, x, z, gridSize, tileSize), tempMoisture[x, z], tempIsLand[x, z]);

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

            float[,] tempDistanceToCoast = CalculateDistanceToCoast(tempIsLand, tempShore, gridSize, tileSize);
            var classifier = habitatClassifier ?? new HabitatClassifier(habitatSettings);
            for (int z = 0; z < gridSize; z++)
                for (int x = 0; x < gridSize; x++)
                    tempHabitat[x, z] = classifier.Classify(tempIsLand[x, z], tempShore[x, z], tempDistanceToCoast[x, z],
                        tempType[x, z], tempIsLand[x, z] ? Mathf.Clamp01((tempHeight[x, z] - minLandHeight) / heightRange) : 0f,
                        CalculateSlope(tempHeight, x, z, gridSize, tileSize), tempMoisture[x, z]);

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
                        tempHabitat[x, z],
                        tempType[x, z],
                        tempShore[x, z],
                        biomeField != null && tempIsLand[x, z] ? biomeField.SampleMoisture(wx, wz) : 0.5f,
                        biomeField != null && tempIsLand[x, z] ? biomeField.SampleTemperature(wx, wz, tempIsLand[x, z] ? Mathf.Clamp01((tempHeight[x, z] - minLandHeight) / heightRange) : 0f) : 0.5f,
                        terrainSettings.PlayerSafeSlopeDegrees,
                        terrainSettings.CreatureSafeSlopeDegrees,
                        terrainSettings.ResourceSafeSlopeDegrees,
                        tempHabitat[x, z], tempDistanceToCoast[x, z]);

                    _grid[x, z] = cell;

                    if (cell.IsWater) { WaterCellCount++; continue; }
                    LandCellCount++;
                    if (cell.IsShoreline)           ShoreCellCount++;
                    if (cell.IsRidge)               RidgeCellCount++;
                    if (cell.IsSafeForPlayerSpawn)  SafePlayerCells++;
                    if (cell.IsSafeForCreatureSpawn) SafeCreatureCells++;
                }
            }

            BuildDenseHabitatMap(gridSize, tileSize, isInsideIsland, getTerrainHeight, minLandHeight, heightRange,
                biomeField, classifier, Mathf.Max(1, biomeResolutionPerTile));

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

        public string GetHabitatIdAt(Vector3 worldPos)
        {
            if (!IsInsideWorld(worldPos) || _habitatMap == null) return HabitatIds.Water;
            int x = Mathf.Clamp(Mathf.FloorToInt((worldPos.x - _originX) / _habitatMapCellSize), 0, _habitatMapSize - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((worldPos.z - _originZ) / _habitatMapCellSize), 0, _habitatMapSize - 1);
            return _habitatMap[x, z] ?? HabitatIds.Water;
        }

        /// <summary>Compatibility API for systems migrating in #98/#99.</summary>
        public string GetBiomeIdAt(Vector3 worldPos) => LegacyBiomeCompatibility.ToLegacyBiomeId(GetHabitatIdAt(worldPos));

        public TerrainType GetTerrainTypeAt(Vector3 worldPos) => GetCellAt(worldPos)?.TerrainType ?? TerrainType.Water;
        public float GetDistanceToCoastAt(Vector3 worldPos) => GetCellAt(worldPos)?.DistanceToCoast ?? 0f;

        /// <summary>Returns the generated environment cache without resampling noise.</summary>
        public bool TryGetEnvironmentAt(Vector3 worldPos, out EnvironmentSample sample)
        {
            sample = default;
            if (!IsInsideWorld(worldPos) || _environmentMap == null) return false;
            int x = Mathf.Clamp(Mathf.FloorToInt((worldPos.x - _originX) / _habitatMapCellSize), 0, _habitatMapSize - 1);
            int z = Mathf.Clamp(Mathf.FloorToInt((worldPos.z - _originZ) / _habitatMapCellSize), 0, _habitatMapSize - 1);
            sample = _environmentMap[x, z];
            return true;
        }

        /// <summary>Legacy vegetation adapter. Habitat remains the authoritative topography identity.</summary>
        public bool TryGetEnvironmentAt(Vector3 worldPos, out VegetationEnvironmentSample sample)
        {
            sample = default;
            if (!TryGetEnvironmentAt(worldPos, out EnvironmentSample environment)) return false;
            sample = new VegetationEnvironmentSample(environment.IsLand, environment.IsWater, environment.IsShoreline,
                LegacyBiomeCompatibility.ToLegacyBiomeId(environment.HabitatId), environment.NormalizedElevation,
                environment.SlopeDegrees, environment.Moisture01);
            return true;
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
        /// Returns a deterministic safe land point in the near-coastal band.
        /// The score depends on cached physical distance only; origin and legacy
        /// biome/profile identities do not define the starting area.
        /// </summary>
        public Vector3 GetSafePlayerSpawnPoint()
        {
            if (_grid == null) return Vector3.up * 0.1f;

            TopographyCell best = null;
            float bestScore = float.MaxValue;

            for (int z = 0; z < _gridSize; z++)
            {
                for (int x = 0; x < _gridSize; x++)
                {
                    var c = _grid[x, z];
                    if (!c.IsSafeForPlayerSpawn) continue;

                    float score = Mathf.Abs(c.DistanceToCoast - 24f);
                    if (score < bestScore || (Mathf.Approximately(score, bestScore)
                        && (best == null || c.GridZ < best.GridZ || (c.GridZ == best.GridZ && c.GridX < best.GridX))))
                    { bestScore = score; best = c; }
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

        private void BuildDenseHabitatMap(int gridSize, float tileSize, Func<float, float, bool> isInsideIsland,
            Func<Vector3, float> getTerrainHeight, float minHeight, float heightRange,
            BiomeFieldGenerator field, HabitatClassifier classifier, int resolutionPerTile)
        {
            _habitatMapSize = Mathf.Max(1, gridSize * Mathf.Max(1, resolutionPerTile));
            _habitatMapCellSize = tileSize / Mathf.Max(1, resolutionPerTile);
            _habitatMap = new string[_habitatMapSize, _habitatMapSize];
            _environmentMap = new EnvironmentSample[_habitatMapSize, _habitatMapSize];
            var land = new bool[_habitatMapSize, _habitatMapSize];
            var terrainTypes = new TerrainType[_habitatMapSize, _habitatMapSize];
            var heights = new float[_habitatMapSize, _habitatMapSize];
            var moisture = new float[_habitatMapSize, _habitatMapSize];
            var temperature = new float[_habitatMapSize, _habitatMapSize];

            for (int z = 0; z < _habitatMapSize; z++)
                for (int x = 0; x < _habitatMapSize; x++)
                {
                    float wx = _originX + (x + 0.5f) * _habitatMapCellSize;
                    float wz = _originZ + (z + 0.5f) * _habitatMapCellSize;
                    bool isLand = isInsideIsland(wx, wz);
                    land[x, z] = isLand;
                    heights[x, z] = isLand ? getTerrainHeight(new Vector3(wx, 0f, wz)) : -0.35f;
                    float elevation = isLand ? Mathf.Clamp01((heights[x, z] - minHeight) / heightRange) : 0f;
                    moisture[x, z] = isLand && field != null ? field.SampleMoisture(wx, wz) : 0.5f;
                    temperature[x, z] = isLand && field != null ? field.SampleTemperature(wx, wz, elevation) : 0.5f;
                }

            for (int z = 0; z < _habitatMapSize; z++)
                for (int x = 0; x < _habitatMapSize; x++)
                {
                    int left = Mathf.Max(0, x - 1), right = Mathf.Min(_habitatMapSize - 1, x + 1);
                    int down = Mathf.Max(0, z - 1), up = Mathf.Min(_habitatMapSize - 1, z + 1);
                    float dx = (heights[right, z] - heights[left, z]) / Mathf.Max(0.01f, (right - left) * _habitatMapCellSize);
                    float dz = (heights[x, up] - heights[x, down]) / Mathf.Max(0.01f, (up - down) * _habitatMapCellSize);
                    float slope = Mathf.Atan(Mathf.Sqrt(dx * dx + dz * dz)) * Mathf.Rad2Deg;
                    bool shoreline = land[x, z] && (!land[left, z] || !land[right, z] || !land[x, down] || !land[x, up]);
                    float elevation = land[x, z] ? Mathf.Clamp01((heights[x, z] - minHeight) / heightRange) : 0f;
                    TerrainType terrainType = ClassifyTerrain(elevation, slope, moisture[x, z], land[x, z]);
                    if (shoreline && terrainType != TerrainType.Ridge) terrainType = TerrainType.Beach;
                    Vector3 position = new Vector3(_originX + (x + 0.5f) * _habitatMapCellSize, heights[x, z], _originZ + (z + 0.5f) * _habitatMapCellSize);
                    TopographyCell coarse = GetCellAt(position);
                    float distance = coarse != null ? coarse.DistanceToCoast : 0f;
                    string habitat = classifier.Classify(land[x, z], shoreline, distance, terrainType, elevation, slope, moisture[x, z]);
                    _habitatMap[x, z] = habitat;
                    _environmentMap[x, z] = new EnvironmentSample(land[x, z], !land[x, z], shoreline, habitat,
                        terrainType, heights[x, z], elevation, slope, moisture[x, z], temperature[x, z], distance);
                }

            // Coarse cell centers are canonical for gameplay region/spawn queries.
            for (int z = 0; z < gridSize; z++)
                for (int x = 0; x < gridSize; x++)
                {
                    TopographyCell cell = _grid[x, z];
                    int mapX = Mathf.Clamp(Mathf.FloorToInt((cell.WorldCenter.x - _originX) / _habitatMapCellSize), 0, _habitatMapSize - 1);
                    int mapZ = Mathf.Clamp(Mathf.FloorToInt((cell.WorldCenter.z - _originZ) / _habitatMapCellSize), 0, _habitatMapSize - 1);
                    _habitatMap[mapX, mapZ] = cell.HabitatId;
                    _environmentMap[mapX, mapZ] = cell.ToEnvironmentSample();
                }
        }

        private static float[,] CalculateDistanceToCoast(bool[,] isLand, bool[,] shoreline, int size, float tileSize)
        {
            var coast = new List<Vector2Int>();
            for (int z = 0; z < size; z++)
                for (int x = 0; x < size; x++)
                    if (shoreline[x, z]) coast.Add(new Vector2Int(x, z));
            var result = new float[size, size];
            float noCoast = size * tileSize;
            for (int z = 0; z < size; z++)
                for (int x = 0; x < size; x++)
                {
                    if (!isLand[x, z]) { result[x, z] = 0f; continue; }
                    float best = float.MaxValue;
                    for (int i = 0; i < coast.Count; i++)
                    {
                        float dx = x - coast[i].x, dz = z - coast[i].y;
                        best = Mathf.Min(best, Mathf.Sqrt(dx * dx + dz * dz) * tileSize);
                    }
                    result[x, z] = best == float.MaxValue ? noCoast : best;
                }
            return result;
        }

        private static TerrainType ClassifyTerrain(float elevation, float slopeDegrees, float moisture, bool isLand)
        {
            if (!isLand) return TerrainType.Water;
            if (elevation >= 0.67f || slopeDegrees >= 28f) return TerrainType.Ridge;
            if (elevation > 0.18f || slopeDegrees > 12f) return TerrainType.Hills;
            return moisture >= 0.56f ? TerrainType.Forest : TerrainType.Plain;
        }

        private bool IsInsideWorld(Vector3 position)
        {
            return _grid != null && position.x >= _originX && position.z >= _originZ
                && position.x < _originX + _gridSize * _tileSize
                && position.z < _originZ + _gridSize * _tileSize;
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
