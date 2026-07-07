using ApexShift.Core.Save;
using UnityEngine;

namespace ApexShift.Runtime.World.Landmarks
{
    [DisallowMultipleComponent]
    public sealed class LandmarkRuntime : MonoBehaviour
    {
        [SerializeField] private string landmarkId = "unknown";
        [SerializeField] private LandmarkType landmarkType = LandmarkType.Unknown;
        [SerializeField] private string displayName = "Unknown landmark";
        [TextArea]
        [SerializeField] private string description = string.Empty;
        [SerializeField] private bool discovered = true;

        public string LandmarkId => Normalize(landmarkId, "unknown");
        public LandmarkType Type => landmarkType;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? LandmarkId : displayName;
        public string Description => description ?? string.Empty;
        public bool IsDiscovered => discovered;

        private void OnEnable() => LandmarkRegistry.Register(this);
        private void OnDisable() => LandmarkRegistry.Unregister(this);
        private void OnDestroy() => LandmarkRegistry.Unregister(this);

        public void Configure(string id, LandmarkType type, string name, string details = null, bool isDiscovered = true)
        {
            landmarkId = Normalize(id, "unknown");
            landmarkType = type;
            displayName = string.IsNullOrWhiteSpace(name) ? BuildDefaultName(type, landmarkId) : name.Trim();
            description = details ?? string.Empty;
            discovered = isDiscovered;
            LandmarkRegistry.Register(this);
        }

        public void SetDiscovered(bool value) => discovered = value;

        public LandmarkSaveData ToSaveData()
        {
            Vector3 p = transform.position;
            return new LandmarkSaveData(LandmarkId, landmarkType.ToString(), DisplayName, Description, p.x, p.y, p.z, discovered);
        }

        public void ApplySaveData(LandmarkSaveData data)
        {
            if (data == null) return;
            landmarkId = Normalize(data.LandmarkId, LandmarkId);
            landmarkType = ParseType(data.LandmarkType, landmarkType);
            displayName = string.IsNullOrWhiteSpace(data.DisplayName) ? BuildDefaultName(landmarkType, landmarkId) : data.DisplayName.Trim();
            description = data.Description ?? string.Empty;
            discovered = data.Discovered;
            transform.position = new Vector3(data.X, data.Y, data.Z);
        }

        public static LandmarkType ParseType(string value, LandmarkType fallback = LandmarkType.Unknown)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            if (System.Enum.TryParse(value.Trim(), true, out LandmarkType parsed)) return parsed;
            string normalized = value.Trim().ToLowerInvariant().Replace("_", string.Empty).Replace("-", string.Empty);
            switch (normalized)
            {
                case "oldtree": return LandmarkType.OldTree;
                case "ruins": return LandmarkType.Ruins;
                case "pond": return LandmarkType.Pond;
                case "camp": return LandmarkType.Camp;
                case "caveplaceholder":
                case "cave": return LandmarkType.CavePlaceholder;
                default: return fallback;
            }
        }

        private static string BuildDefaultName(LandmarkType type, string id)
        {
            switch (type)
            {
                case LandmarkType.OldTree: return "Great Old Tree";
                case LandmarkType.Ruins: return "Ruins";
                case LandmarkType.Pond: return "Pond";
                case LandmarkType.Camp: return "Abandoned Camp";
                case LandmarkType.CavePlaceholder: return "Cave";
                default: return string.IsNullOrWhiteSpace(id) ? "Unknown landmark" : id;
            }
        }

        private static string Normalize(string value, string fallback)
            => string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
    }
}
