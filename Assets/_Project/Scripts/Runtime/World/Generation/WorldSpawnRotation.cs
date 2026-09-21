using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>
    /// Combines procedural world-Y variation with the authored orientation of a prefab root.
    /// </summary>
    public static class WorldSpawnRotation
    {
        public static Quaternion ComposeYawWithPrefabRotation(GameObject prefab, float yaw)
        {
            return ComposeYawWithPrefabRotation(prefab != null ? prefab.transform : null, yaw);
        }

        public static Quaternion ComposeYawWithPrefabRotation(Transform prefabTransform, float yaw)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);
            return yawRotation * (prefabTransform != null ? prefabTransform.rotation : Quaternion.identity);
        }
    }
}
