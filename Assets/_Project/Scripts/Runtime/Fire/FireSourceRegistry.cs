using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Runtime.Fire
{
    public static class FireSourceRegistry
    {
        private static readonly List<FireSourceRuntime> sources = new List<FireSourceRuntime>();

        public static int SourceCount => sources.Count;

        public static int ActiveSourceCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < sources.Count; i++)
                {
                    FireSourceRuntime source = sources[i];
                    if (source != null && source.IsActiveFire)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public static void Register(FireSourceRuntime source)
        {
            if (source != null && !sources.Contains(source))
            {
                sources.Add(source);
            }
        }

        public static void Unregister(FireSourceRuntime source)
        {
            if (source != null)
            {
                sources.Remove(source);
            }
        }

        public static bool TryGetStrongestSource(Vector3 position, out FireSourceRuntime result)
        {
            return TryGetStrongestSource(position, 1f, out result);
        }

        public static bool TryGetStrongestSource(Vector3 position, float radiusMultiplier, out FireSourceRuntime result)
        {
            CleanupDeadSources();
            result = null;
            float bestScore = float.NegativeInfinity;
            float multiplier = Mathf.Max(0.1f, radiusMultiplier);

            for (int i = 0; i < sources.Count; i++)
            {
                FireSourceRuntime source = sources[i];
                if (source == null || !source.IsActiveFire)
                {
                    continue;
                }

                float radius = Mathf.Max(0.1f, source.ProtectionRadius) * multiplier;
                Vector3 delta = source.transform.position - position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance > radius)
                {
                    continue;
                }

                float score = (1f - Mathf.Clamp01(distance / radius)) * Mathf.Max(0.1f, source.Intensity);
                if (score > bestScore)
                {
                    bestScore = score;
                    result = source;
                }
            }

            return result != null;
        }

        private static void CleanupDeadSources()
        {
            for (int i = sources.Count - 1; i >= 0; i--)
            {
                if (sources[i] == null)
                {
                    sources.RemoveAt(i);
                }
            }
        }
    }
}
