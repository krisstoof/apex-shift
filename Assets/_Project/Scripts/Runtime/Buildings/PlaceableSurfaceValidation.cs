using ApexShift.Runtime.World;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Topography;
using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    public static class PlaceableSurfaceValidation
    {
        public static bool IsInLaunchBand(EnvironmentSample sample, float minDistance, float maxDistance)
            => sample.IsWater && !sample.IsLand && sample.DistanceToCoast >= minDistance && sample.DistanceToCoast <= maxDistance;

        public static PlacementValidationResult ValidateWater(Vector3 position, IslandTopographyRuntime topography,
            WorldBounds bounds, float minDistance, float maxDistance)
        {
            if (bounds == null || !bounds.Contains(position))
                return PlacementValidationResult.Invalid("outside playable launch bounds");
            if (topography == null || !topography.TryGetEnvironmentAt(position, out EnvironmentSample sample))
                return PlacementValidationResult.Invalid("missing generated environment");
            return IsInLaunchBand(sample, minDistance, maxDistance)
                ? PlacementValidationResult.Valid : PlacementValidationResult.Invalid("requires near-shore water");
        }
    }
}
