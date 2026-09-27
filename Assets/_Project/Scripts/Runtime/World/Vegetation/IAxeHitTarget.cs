using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    public readonly struct AxeHitContext
    {
        public readonly GameObject Actor;
        public readonly float Damage;
        public readonly Vector3 HitPoint;
        public readonly Vector3 Direction;

        public AxeHitContext(GameObject actor, float damage, Vector3 hitPoint, Vector3 direction)
        {
            Actor = actor;
            Damage = damage;
            HitPoint = hitPoint;
            Direction = direction;
        }
    }

    /// <summary>Receives one discrete primary-attack hit from an axe.</summary>
    public interface IAxeHitTarget
    {
        bool TryAxeHit(in AxeHitContext hit);
    }
}
