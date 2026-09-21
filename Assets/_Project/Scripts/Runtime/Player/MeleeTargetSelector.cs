using ApexShift.Runtime.Creatures;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    /// <summary>Authoritative, allocation-free melee target selection.</summary>
    public sealed class MeleeTargetSelector
    {
        public struct Result
        {
            public CreatureHitboxRuntime Hitbox;
            public CreatureHealthRuntime Health;
            public Vector3 Point;
            public float Distance;
            public float Angle;
        }

        private readonly Collider[] overlapBuffer;
        private readonly CreatureHitboxRuntime[] seenHitboxes;

        public MeleeTargetSelector(int capacity = 32)
        {
            capacity = Mathf.Max(4, capacity);
            overlapBuffer = new Collider[capacity];
            seenHitboxes = new CreatureHitboxRuntime[capacity];
        }

        public bool TrySelectTarget(
            Vector3 origin,
            Vector3 attackDirection,
            float range,
            float arcDegrees,
            LayerMask creatureMask,
            Transform attacker,
            out Result result)
        {
            result = default;
            Vector3 flatDirection = attackDirection;
            flatDirection.y = 0f;
            if (flatDirection.sqrMagnitude <= 0.0001f || range <= 0f)
            {
                return false;
            }

            flatDirection.Normalize();
            int count = Physics.OverlapSphereNonAlloc(
                origin,
                range,
                overlapBuffer,
                creatureMask,
                QueryTriggerInteraction.Collide);

            int seenCount = 0;
            float halfArc = Mathf.Clamp(arcDegrees, 0f, 360f) * 0.5f;
            bool found = false;
            int bestId = int.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider candidate = overlapBuffer[i];
                if (candidate == null)
                {
                    continue;
                }

                CreatureHitboxRuntime hitbox = candidate.GetComponentInParent<CreatureHitboxRuntime>();
                if (hitbox == null || hitbox.CombatCollider != candidate || Contains(seenHitboxes, seenCount, hitbox))
                {
                    continue;
                }

                if (seenCount < seenHitboxes.Length)
                {
                    seenHitboxes[seenCount++] = hitbox;
                }

                CreatureHealthRuntime health = hitbox.GetComponentInParent<CreatureHealthRuntime>();
                if (health == null || health.IsDead || IsSelf(hitbox.transform, health.transform, attacker))
                {
                    continue;
                }

                Vector3 targetPoint = candidate.ClosestPoint(origin);
                Vector3 targetDelta = targetPoint - origin;
                targetDelta.y = 0f;
                if (targetDelta.sqrMagnitude <= 0.0001f)
                {
                    targetDelta = health.transform.position - origin;
                    targetDelta.y = 0f;
                    targetPoint = health.transform.position;
                }

                float distance = targetDelta.magnitude;
                if (distance > range + 0.001f || targetDelta.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                float angle = Vector3.Angle(flatDirection, targetDelta / distance);
                if (angle > halfArc)
                {
                    continue;
                }

                int instanceId = health.GetHashCode();
                bool better = !found || distance < result.Distance - 0.0001f ||
                    (Mathf.Abs(distance - result.Distance) <= 0.0001f &&
                     (angle < result.Angle - 0.0001f ||
                      (Mathf.Abs(angle - result.Angle) <= 0.0001f && instanceId < bestId)));
                if (!better)
                {
                    continue;
                }

                found = true;
                bestId = instanceId;
                result = new Result
                {
                    Hitbox = hitbox,
                    Health = health,
                    Point = targetPoint,
                    Distance = distance,
                    Angle = angle
                };
            }

            for (int i = 0; i < seenCount; i++)
            {
                seenHitboxes[i] = null;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (count == overlapBuffer.Length)
            {
                Debug.LogWarning("Melee target overlap buffer was full; increase MeleeTargetSelector capacity if targets are being missed.");
            }
#endif
            return found;
        }

        private static bool Contains(CreatureHitboxRuntime[] values, int count, CreatureHitboxRuntime value)
        {
            for (int i = 0; i < count; i++)
            {
                if (values[i] == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSelf(Transform hitboxTransform, Transform healthTransform, Transform attacker)
        {
            return attacker != null &&
                (hitboxTransform == attacker || healthTransform == attacker ||
                 hitboxTransform.IsChildOf(attacker) || healthTransform.IsChildOf(attacker));
        }
    }
}
