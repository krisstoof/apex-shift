using System;

namespace ApexShift.Core.Save
{
    [Serializable]
    public sealed class StoryClueSaveData
    {
        public string clueId;
        public bool discovered;
        public float x, y, z;
        public string ClueId => string.IsNullOrWhiteSpace(clueId) ? string.Empty : clueId.Trim().ToLowerInvariant();
        public bool Discovered => discovered;
        public float X => x;
        public float Y => y;
        public float Z => z;

        public StoryClueSaveData() { }
        public StoryClueSaveData(string id, bool isDiscovered, float x, float y, float z)
        {
            clueId = id;
            discovered = isDiscovered;
            this.x = x; this.y = y; this.z = z;
        }
    }
}
