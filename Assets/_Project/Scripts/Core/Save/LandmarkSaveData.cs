using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class LandmarkSaveData
    {
        public string landmarkId;
        public string landmarkType;
        public string displayName;
        public string description;
        public float x;
        public float y;
        public float z;
        public bool discovered = true;

        public string LandmarkId => string.IsNullOrWhiteSpace(landmarkId) ? "unknown" : landmarkId;
        public string LandmarkType => string.IsNullOrWhiteSpace(landmarkType) ? "Unknown" : landmarkType;
        public string DisplayName => displayName ?? string.Empty;
        public string Description => description ?? string.Empty;
        public float X => x;
        public float Y => y;
        public float Z => z;
        public bool Discovered => discovered;

        public LandmarkSaveData()
        {
        }

        public LandmarkSaveData(string landmarkId, string landmarkType, string displayName, string description, float x, float y, float z, bool discovered)
        {
            this.landmarkId = string.IsNullOrWhiteSpace(landmarkId) ? "unknown" : landmarkId.Trim().ToLowerInvariant();
            this.landmarkType = string.IsNullOrWhiteSpace(landmarkType) ? "Unknown" : landmarkType.Trim();
            this.displayName = displayName ?? string.Empty;
            this.description = description ?? string.Empty;
            this.x = x;
            this.y = y;
            this.z = z;
            this.discovered = discovered;
        }
    }
}
