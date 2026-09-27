using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    /// <summary>Receives one discrete primary-attack hit from an axe.</summary>
    public interface IAxeHitTarget
    {
        bool TryAxeHit(GameObject actor);
    }
}
