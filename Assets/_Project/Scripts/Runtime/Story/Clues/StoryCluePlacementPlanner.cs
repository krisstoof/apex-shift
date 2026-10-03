using System;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Topography;
using UnityEngine;

namespace ApexShift.Runtime.Story.Clues
{
    /// <summary>Deterministic local search over the cached environment; never consumes Unity Random.</summary>
    public sealed class StoryCluePlacementPlanner
    {
        public bool TryPlan(int seed, StoryClueDefinition definition, IslandTopographyRuntime topography,
            Vector3 anchor, Vector3 crash, Vector3? destination, out Vector3 position,
            Func<Vector3, float> surfaceHeight = null)
        {
            position = default;
            if (definition == null || topography == null || !topography.IsBuilt) return false;
            uint hash = StableHash(seed, definition.ClueId);
            float angle = hash / (float)uint.MaxValue * Mathf.PI * 2f;
            if (definition.IsRoute)
            {
                if (!destination.HasValue || Distance(anchor, destination.Value) < 16f) return false;
                float fraction = Mathf.Lerp(definition.MinRouteFraction, definition.MaxRouteFraction,
                    (hash & 65535) / 65535f);
                Vector3 target = Vector3.Lerp(anchor, destination.Value, fraction);
                if (!TryRings(target, 0f, 12f, angle, definition, topography, anchor, crash, destination, out position)
                    && !TryRings(anchor, definition.MinAnchorDistance, definition.MaxAnchorDistance, angle,
                        definition, topography, anchor, crash, destination, out position)) return false;
            }
            else if (!TryRings(anchor, definition.MinAnchorDistance, definition.MaxAnchorDistance, angle,
                definition, topography, anchor, crash, null, out position)) return false;
            if (surfaceHeight != null) position.y = surfaceHeight(position);
            return true;
        }

        private static bool TryRings(Vector3 center, float minRadius, float maxRadius, float angle,
            StoryClueDefinition definition, IslandTopographyRuntime topography, Vector3 anchor, Vector3 crash,
            Vector3? destination, out Vector3 position)
        {
            for (float radius = minRadius; radius <= maxRadius + 0.001f; radius += 0.5f)
            for (int i = 0; i < 32; i++)
            {
                float theta = angle + i * Mathf.PI * 2f / 32f;
                Vector3 candidate = center + new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta)) * radius;
                if (!topography.TryGetEnvironmentAt(candidate, out EnvironmentSample sample)
                    || !sample.IsLand || sample.IsWater || sample.SlopeDegrees > definition.MaxSlopeDegrees
                    || Distance(candidate, crash) < definition.MinCrashDistance) continue;
                if (definition.IsRoute)
                {
                    if (sample.IsShoreline || !destination.HasValue) continue;
                    Vector3 basePoint = destination.Value;
                    if (Distance(candidate, basePoint) >= Distance(anchor, basePoint)
                        || Distance(candidate, basePoint) <= 16f
                        || Vector3.Dot(Vector3.ProjectOnPlane(candidate - anchor, Vector3.up),
                            Vector3.ProjectOnPlane(basePoint - anchor, Vector3.up)) <= 0f) continue;
                }
                candidate.y = sample.Height;
                position = candidate;
                return true;
            }
            position = default;
            return false;
        }

        public static float Distance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        private static uint StableHash(int seed, string id)
        {
            unchecked
            {
                uint hash = 2166136261u ^ (uint)seed;
                foreach (char value in id) hash = (hash ^ value) * 16777619u;
                return hash;
            }
        }
    }
}
