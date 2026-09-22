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
        public readonly float       NormalizedElevation;
        public readonly float       SlopeDegrees;
        public readonly float       Moisture01;
        public readonly float       Temperature01;
        public float Dryness01 => 1f - Moisture01;
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
        public readonly bool IsSafeForPlayerSpawn;
        public readonly bool IsSafeForCreatureSpawn;
        public readonly bool IsSafeForResourceSpawn;

        // ── Constructor ───────────────────────────────────────────────────────
        public TopographyCell(
            int gridX, int gridZ,
            Vector3 worldCenter,
            float height,
            float normalizedElevation,
            float slopeDegrees,
            string biomeId,
            TerrainType terrainType,
            bool isShoreline,
            float moisture01 = 0.5f,
            float temperature01 = 0.5f,
            float playerSafeSlopeDegrees = 14f,
            float creatureSafeSlopeDegrees = 24f,
            float resourceSafeSlopeDegrees = 20f)
        {
            GridX       = gridX;
            GridZ       = gridZ;
            WorldCenter = worldCenter;
            Height      = height;
            NormalizedElevation = normalizedElevation;
            SlopeDegrees = slopeDegrees;
            BiomeId     = biomeId;
            TerrainType = terrainType;
            IsShoreline = isShoreline;
            Moisture01 = Mathf.Clamp01(moisture01);
            Temperature01 = Mathf.Clamp01(temperature01);
            IsSafeForPlayerSpawn = IsLand && !IsShoreline && TerrainType != TerrainType.Ridge && slopeDegrees <= playerSafeSlopeDegrees;
            IsSafeForCreatureSpawn = IsLand && !IsShoreline && slopeDegrees <= creatureSafeSlopeDegrees;
            IsSafeForResourceSpawn = IsLand && !IsShoreline && slopeDegrees <= resourceSafeSlopeDegrees;
        }
    }
}
