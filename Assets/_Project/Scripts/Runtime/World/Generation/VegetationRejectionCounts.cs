namespace ApexShift.Runtime.World.Generation
{
    /// <summary>Aggregated deterministic counts of vegetation placement rejection reasons.</summary>
    public sealed class VegetationRejectionCounts
    {
        public int Water { get; internal set; }
        public int ExcessiveSlope { get; internal set; }
        public int HabitatMismatch { get; internal set; }
        public int Terrain { get; internal set; }
        public int CoastDistance { get; internal set; }
        public int Elevation { get; internal set; }
        public int Moisture { get; internal set; }
        public int ShorelineOrClearing { get; internal set; }
        public int SpacingOrCollision { get; internal set; }

        public void Reset()
        {
            Water = ExcessiveSlope = HabitatMismatch = Elevation = Moisture = 0;
            ShorelineOrClearing = SpacingOrCollision = Terrain = CoastDistance = 0;
        }
    }
}
