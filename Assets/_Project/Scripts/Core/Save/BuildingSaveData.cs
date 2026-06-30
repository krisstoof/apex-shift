using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class BuildingSaveData
    {
        public string instanceId;
        public string buildingId;
        public float x;
        public float y;
        public float z;
        public float rotationY;
        public bool active = true;

        public string InstanceId => Normalize(instanceId, "building");
        public string BuildingId => Normalize(buildingId, "unknown");
        public float X => x;
        public float Y => y;
        public float Z => z;
        public float RotationY => rotationY;
        public bool Active => active;

        public BuildingSaveData()
        {
        }

        public BuildingSaveData(string instanceId, string buildingId, float x, float y, float z, float rotationY, bool active = true)
        {
            this.instanceId = Normalize(instanceId, Guid.NewGuid().ToString("N"));
            this.buildingId = Normalize(buildingId, "unknown");
            this.x = x;
            this.y = y;
            this.z = z;
            this.rotationY = NormalizeYaw(rotationY);
            this.active = active;
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static float NormalizeYaw(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            float normalized = value % 360f;
            return normalized < 0f ? normalized + 360f : normalized;
        }
    }
}
