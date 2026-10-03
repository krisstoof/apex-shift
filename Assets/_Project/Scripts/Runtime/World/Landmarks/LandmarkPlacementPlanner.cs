using System;
using System.Collections.Generic;
using ApexShift.Runtime.World.Environment;
using ApexShift.Runtime.World.Topography;
using UnityEngine;

namespace ApexShift.Runtime.World.Landmarks
{
    public readonly struct LandmarkPlacementCandidate
    {
        public readonly Vector3 Position;
        public readonly EnvironmentSample Environment;
        public LandmarkPlacementCandidate(Vector3 position, EnvironmentSample environment)
        {
            Position = position;
            Environment = environment;
        }
    }

    public sealed class LandmarkPlacementResult
    {
        public string LandmarkId { get; }
        public Vector3 Position { get; }
        public string HabitatId { get; }
        public TerrainType TerrainType { get; }
        public float Slope { get; }
        public float Elevation { get; }
        public float Moisture { get; }
        public float DistanceToCoast { get; }
        public float Score { get; }
        public bool UsedFallback { get; }
        public float MinimumSeparation { get; }

        public LandmarkPlacementResult(LandmarkPlacementProfile profile, LandmarkPlacementCandidate candidate,
            float score, bool usedFallback)
        {
            LandmarkId = profile.LandmarkId;
            Position = candidate.Position;
            HabitatId = candidate.Environment.HabitatId;
            TerrainType = candidate.Environment.TerrainType;
            Slope = candidate.Environment.SlopeDegrees;
            Elevation = candidate.Environment.NormalizedElevation;
            Moisture = candidate.Environment.Moisture01;
            DistanceToCoast = candidate.Environment.DistanceToCoast;
            Score = score;
            UsedFallback = usedFallback;
            MinimumSeparation = profile.MinimumSeparation;
        }
    }

    /// <summary>Pure, deterministic placement; does not instantiate objects or touch Unity Random.</summary>
    public sealed class LandmarkPlacementPlanner
    {
        public LandmarkPlacementResult Plan(int seed, IReadOnlyList<LandmarkPlacementCandidate> candidates,
            LandmarkPlacementProfile profile, IReadOnlyList<LandmarkPlacementResult> placed,
            Vector3? start = null, Vector3? anchor = null)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                LandmarkPlacementResult best = null;
                for (int i = 0; i < candidates.Count; i++)
                {
                    LandmarkPlacementCandidate candidate = candidates[i];
                    if (!IsSafe(candidate, profile, placed, start, anchor)) continue;
                    EnvironmentSample e = candidate.Environment;
                    if (pass == 0 && (
                        (profile.PreferredHabitats.Length > 0 && Array.IndexOf(profile.PreferredHabitats, e.HabitatId) < 0) ||
                        (profile.PreferredTerrain.Length > 0 && Array.IndexOf(profile.PreferredTerrain, e.TerrainType) < 0) ||
                        e.DistanceToCoast < profile.PreferredMinCoast ||
                        e.DistanceToCoast > profile.PreferredMaxCoast ||
                        (start.HasValue && Distance(candidate.Position, start.Value) < profile.PreferredStartDistance)))
                        continue;
                    float score = Score(seed, candidate, profile, start, anchor);
                    // Coordinate tie-break makes results independent of input iteration order.
                    if (best == null || score > best.Score || (score == best.Score &&
                        (candidate.Position.x < best.Position.x ||
                         (candidate.Position.x == best.Position.x && candidate.Position.z < best.Position.z))))
                        best = new LandmarkPlacementResult(profile, candidate, score, pass != 0);
                }
                if (best != null) return best;
            }
            return null; // Never collapse unsafe/unplaceable landmarks onto a shared spawn fallback.
        }

        private static bool IsSafe(LandmarkPlacementCandidate c, LandmarkPlacementProfile p,
            IReadOnlyList<LandmarkPlacementResult> placed, Vector3? start, Vector3? anchor)
        {
            EnvironmentSample e = c.Environment;
            if (!e.IsLand || e.IsWater || e.IsShoreline ||
                e.SlopeDegrees < p.MinSlope || e.SlopeDegrees > p.MaxSlope ||
                e.NormalizedElevation < p.MinElevation || e.NormalizedElevation > p.MaxElevation ||
                e.Moisture01 < p.MinMoisture || e.Moisture01 > p.MaxMoisture ||
                e.DistanceToCoast < p.MinCoastDistance || e.DistanceToCoast > p.MaxCoastDistance ||
                !Allowed(p.AllowedHabitats, e.HabitatId) || !Allowed(p.AllowedTerrain, e.TerrainType))
                return false;
            if (start.HasValue && Distance(c.Position, start.Value) < p.MinStartDistance) return false;
            if (anchor.HasValue)
            {
                float distance = Distance(c.Position, anchor.Value);
                if (distance < p.MinAnchorDistance || distance > p.MaxAnchorDistance) return false;
            }
            if (placed != null)
                for (int i = 0; i < placed.Count; i++)
                    if (Distance(c.Position, placed[i].Position) <
                        Mathf.Max(p.MinimumSeparation, placed[i].MinimumSeparation)) return false;
            return true;
        }

        private static bool Allowed<T>(T[] values, T value) => values.Length == 0 || Array.IndexOf(values, value) >= 0;

        private static float Score(int seed, LandmarkPlacementCandidate c, LandmarkPlacementProfile p,
            Vector3? start, Vector3? anchor)
        {
            EnvironmentSample e = c.Environment;
            float score = (Array.IndexOf(p.PreferredHabitats, e.HabitatId) >= 0 ? p.HabitatWeight : 0f)
                + (Array.IndexOf(p.PreferredTerrain, e.TerrainType) >= 0 ? p.TerrainWeight : 0f)
                - Mathf.Abs(e.NormalizedElevation - p.PreferredElevation) * p.ElevationWeight
                - Mathf.Abs(e.SlopeDegrees - p.PreferredSlope) / 24f * p.SlopeWeight
                - Mathf.Abs(e.Moisture01 - p.PreferredMoisture) * p.MoistureWeight;
            float coastPenalty = Mathf.Max(p.PreferredMinCoast - e.DistanceToCoast, 0f)
                + Mathf.Max(e.DistanceToCoast - p.PreferredMaxCoast, 0f);
            score -= coastPenalty / 40f * p.CoastWeight;
            if (start.HasValue && p.PreferredStartDistance > 0f)
                score -= Mathf.Abs(Distance(c.Position, start.Value) - p.PreferredStartDistance) / 100f;
            if (anchor.HasValue)
                score -= Mathf.Abs(Distance(c.Position, anchor.Value) - p.PreferredAnchorDistance) / 60f * p.AnchorWeight;
            uint hash = unchecked((uint)seed);
            foreach (char ch in p.LandmarkId) hash = unchecked((hash ^ ch) * 16777619u);
            hash = unchecked((hash ^ (uint)Mathf.RoundToInt(c.Position.x * 100f)) * 16777619u);
            hash = unchecked((hash ^ (uint)Mathf.RoundToInt(c.Position.z * 100f)) * 16777619u);
            hash ^= hash >> 16; hash = unchecked(hash * 2246822519u); hash ^= hash >> 13;
            return score + (hash & 65535u) / 65535f * 0.08f;
        }

        public static float Distance(Vector3 a, Vector3 b)
            => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }
}
