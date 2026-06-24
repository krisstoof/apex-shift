using System.Collections.Generic;

namespace ApexShift.Core.Resources
{
    public sealed class ResourceRegrowthRules
    {
        public const int DefaultMaxStage = 3;

        private readonly Dictionary<string, float> totalRegrowthDays = new Dictionary<string, float>
        {
            { "conifer_tree", 3f },
            { "leafy_tree", 3f },
            { "tree", 3f },
            { "dry_tree", 15f },
            { "bush", 2f },
            { "small_bush", 2f },
            { "berry_bush", 2f },
            { "dry_bush", 3f },
            { "grass_patch", 1f },
            { "dense_grass", 1f },
            { "rock", 0f } // 0 means no regrowth
        };

        public float GetTotalRegrowthDays(string resourceKind)
        {
            if (string.IsNullOrEmpty(resourceKind)) return 0f;
            string normalized = ResourceId.Normalize(resourceKind);
            return totalRegrowthDays.TryGetValue(normalized, out float days) ? days : 0f;
        }

        public bool CanRegrow(string resourceKind)
        {
            return GetTotalRegrowthDays(resourceKind) > 0f;
        }

        public float GetDaysPerStage(string resourceKind, int maxStage)
        {
            if (maxStage <= 0) return 0f;
            float totalDays = GetTotalRegrowthDays(resourceKind);
            return totalDays / maxStage;
        }
    }
}
