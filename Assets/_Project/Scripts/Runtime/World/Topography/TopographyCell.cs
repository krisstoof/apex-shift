using UnityEngine;

namespace ApexShift.Runtime.World.Topography
{
    /// <summary>
    /// Immutable data record for one cell in the island topography grid.
    /// Created once during world generation; queried by spawners, bounds, minimap and debug tools.
    /// </summary>
    public sealed class TopographyCell
    {
        // ── Identity ─────────────────────────────────────────────────────────
        public readonly int   GridX;
        public readonly int   GridZ;

        /// <summary>World-space center of this cell (Y = terrain surface height).</summary>
        public readonly Vector3 WorldCenter;

        // ── Terrain data ─────────────────────────────────────────────────────
        public readonly float       Height;
        public readonly string      BiomeId;
        public readonly TerrainType TerrainType;

        /// <summary>True when this land cell directly borders at least one water cell.</summary>
        public readonly bool IsShoreline;

        // ── Convenience flags ────────────────────────────────────────────────
        public bool IsLand  => TerrainType != TerrainType.Water;
        public bool IsWater => TerrainType == TerrainType.Water;
        public bool IsRidge => TerrainType == TerrainType.Ridge;
        public bool IsBeach => TerrainType == TerrainType.Beach;

        // ── Spawn safety ─────────────────────────────────────────────────────
        /// <summary>Safe for player starting position: flat land, not a shoreline or ridge.</summary>
        public bool IsSafeForPlayerSpawn =>
            IsLand && !IsShoreline && TerrainType != TerrainType.Ridge;

        /// <summary>Safe for creature spawn: land that is not a shoreline (no water-exit risk).</summary>
        public bool IsSafeForCreatureSpawn => IsLand && !IsShoreline;

        /// <summary>Safe for resource spawn: land cell that is not on the shoreline.</summary>
        public bool IsSafeForResourceSpawn => IsLand && !IsShoreline;

        // ── Constructor ───────────────────────────────────────────────────────
        public TopographyCell(
            int gridX, int gridZ,
            Vector3 worldCenter,
            float height,
            string biomeId,
            TerrainType terrainType,
            bool isShoreline)
        {
            GridX       = gridX;
            GridZ       = gridZ;
            WorldCenter = worldCenter;
            Height      = height;
            BiomeId     = biomeId;
            TerrainType = terrainType;
            IsShoreline = isShoreline;
        }
    }
}
