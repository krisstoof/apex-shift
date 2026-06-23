using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.World
{
    public sealed class WorldBounds : MonoBehaviour
    {
        public static WorldBounds Active { get; private set; }

        [SerializeField]
        private float tileSize = 4f;

        [SerializeField]
        private List<Vector2> allowedTileCenters = new List<Vector2>();

        private float halfTileSize;

        private void Awake()
        {
            Active = this;
            halfTileSize = tileSize * 0.5f;
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public void Configure(float newTileSize, IEnumerable<Vector3> tileCenters)
        {
            tileSize = newTileSize;
            halfTileSize = tileSize * 0.5f;
            allowedTileCenters.Clear();

            foreach (Vector3 center in tileCenters)
            {
                allowedTileCenters.Add(new Vector2(center.x, center.z));
            }
        }

        public bool Contains(Vector3 worldPosition)
        {
            Vector2 position = new Vector2(worldPosition.x, worldPosition.z);

            for (int i = 0; i < allowedTileCenters.Count; i++)
            {
                Vector2 center = allowedTileCenters[i];
                if (Mathf.Abs(position.x - center.x) <= halfTileSize &&
                    Mathf.Abs(position.y - center.y) <= halfTileSize)
                {
                    return true;
                }
            }

            return false;
        }

        public Vector3 ClampToNearestAllowed(Vector3 worldPosition)
        {
            if (allowedTileCenters.Count == 0)
            {
                return worldPosition;
            }

            Vector2 position = new Vector2(worldPosition.x, worldPosition.z);
            Vector2 nearest = allowedTileCenters[0];
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < allowedTileCenters.Count; i++)
            {
                float distance = (position - allowedTileCenters[i]).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = allowedTileCenters[i];
                }
            }

            return new Vector3(nearest.x, worldPosition.y, nearest.y);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;

            float size = tileSize > 0f ? tileSize : 4f;
            Vector3 cubeSize = new Vector3(size, 0.05f, size);

            for (int i = 0; i < allowedTileCenters.Count; i++)
            {
                Vector2 center = allowedTileCenters[i];
                Gizmos.DrawWireCube(new Vector3(center.x, 0.05f, center.y), cubeSize);
            }
        }
#endif
    }
}
