using ApexShift.Core.Common;
using UnityEngine;

namespace ApexShift.Runtime.World
{
    public static class WorldPositionMapper
    {
        public static Vector3 ToUnity(WorldPosition position, float height = 0f)
        {
            return new Vector3(position.X, height, position.Z);
        }

        public static WorldPosition ToWorldPosition(Vector3 unityPosition)
        {
            return new WorldPosition(unityPosition.x, unityPosition.z);
        }
    }
}

