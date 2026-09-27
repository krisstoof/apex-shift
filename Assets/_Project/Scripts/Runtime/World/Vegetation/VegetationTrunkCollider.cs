using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    /// <summary>Mark a prefab collider explicitly when it should survive TrunkOnly collision policy.</summary>
    [DisallowMultipleComponent]
    public sealed class VegetationTrunkCollider : MonoBehaviour { }
}
