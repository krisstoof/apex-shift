using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>
    /// Runtime gameplay bounds for creature movement.
    /// NavMesh alone is not enough here because generated terrain/water/edge tiles can still
    /// produce sampled positions that are technically reachable but outside intended island land.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CreatureIslandBoundsRuntime : MonoBehaviour
    {
        public static CreatureIslandBoundsRuntime Active { get; private set; }

        [SerializeField] private float maxDistanceFromLandCenter = 5.85f;
        [SerializeField] private float clampInset = 0.82f;
        private readonly List<Vector3> landCenters = new List<Vector3>();

        private void OnEnable()
        {
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public void Configure(IEnumerable<Vector3> centers, float maxDistance = 5.85f)
        {
            landCenters.Clear();
            if (centers != null)
            {
                foreach (Vector3 center in centers)
                {
                    landCenters.Add(center);
                }
            }

            maxDistanceFromLandCenter = Mathf.Max(1f, maxDistance);
        }

        public bool HasLand => landCenters.Count > 0;

        public bool IsNearLand(Vector3 position)
        {
            if (landCenters.Count == 0)
            {
                return true;
            }

            Vector3 nearest = FindNearestLandCenter(position, out float sqrDistance);
            return sqrDistance <= maxDistanceFromLandCenter * maxDistanceFromLandCenter;
        }

        public bool TryClampToLand(Vector3 position, out Vector3 clamped)
        {
            if (landCenters.Count == 0)
            {
                clamped = position;
                return true;
            }

            Vector3 nearest = FindNearestLandCenter(position, out float sqrDistance);
            float maxDistance = maxDistanceFromLandCenter * Mathf.Clamp01(clampInset);
            Vector2 offset = new Vector2(position.x - nearest.x, position.z - nearest.z);

            if (sqrDistance <= maxDistanceFromLandCenter * maxDistanceFromLandCenter)
            {
                if (offset.magnitude > maxDistance)
                {
                    offset = offset.normalized * maxDistance;
                }

                clamped = new Vector3(nearest.x + offset.x, nearest.y, nearest.z + offset.y);
                return true;
            }

            clamped = new Vector3(nearest.x, nearest.y, nearest.z);
            return true;
        }

        private Vector3 FindNearestLandCenter(Vector3 position, out float bestSqrDistance)
        {
            Vector3 best = landCenters.Count > 0 ? landCenters[0] : position;
            bestSqrDistance = float.PositiveInfinity;

            for (int i = 0; i < landCenters.Count; i++)
            {
                Vector3 center = landCenters[i];
                float dx = position.x - center.x;
                float dz = position.z - center.z;
                float sqr = dx * dx + dz * dz;
                if (sqr < bestSqrDistance)
                {
                    bestSqrDistance = sqr;
                    best = center;
                }
            }

            return best;
        }
    }
}
