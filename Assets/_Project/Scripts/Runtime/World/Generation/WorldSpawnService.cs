using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>Centralizes instantiation rules for generated world objects.</summary>
    public sealed class WorldSpawnService
    {
        public GameObject Spawn(GameObject prefab, Vector3 position, float yaw, Transform parent)
        {
            if (prefab == null) return null;
            Quaternion rotation = WorldSpawnRotation.ComposeYawWithPrefabRotation(prefab, yaw);
            return Object.Instantiate(prefab, position, rotation, parent);
        }
    }
}
