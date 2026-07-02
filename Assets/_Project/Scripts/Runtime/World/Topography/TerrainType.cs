namespace ApexShift.Runtime.World.Topography
{
    /// <summary>
    /// Classification of a topography grid cell.
    /// Ordered roughly by elevation / traversal difficulty.
    /// </summary>
    public enum TerrainType
    {
        /// <summary>Outside the island boundary – no solid ground.</summary>
        Water = 0,

        /// <summary>Land cell directly adjacent to a water cell (shoreline / coast).</summary>
        Beach = 1,

        /// <summary>Low-lying flat terrain (hearth_meadow, redfang_wilds).</summary>
        Plain = 2,

        /// <summary>Wooded terrain (westwood, south_thicket).</summary>
        Forest = 3,

        /// <summary>Rolling hills – moderate elevation, no specific forest biome.</summary>
        Hills = 4,

        /// <summary>High rocky terrain (stoneback_ridge or height > 0.55 units).</summary>
        Ridge = 5,
    }
}
