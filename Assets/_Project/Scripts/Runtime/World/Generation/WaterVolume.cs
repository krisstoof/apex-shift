using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>
    /// Lightweight marker component placed on water trigger volumes.
    /// PlayerWaterDetector checks for this component instead of relying on a Unity tag
    /// (tags must be registered in the TagManager before use, which can't be done from code).
    /// </summary>
    public sealed class WaterVolume : MonoBehaviour { }
}
