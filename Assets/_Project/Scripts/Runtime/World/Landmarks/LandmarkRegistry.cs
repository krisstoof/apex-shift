using System.Collections.Generic;
using ApexShift.Core.Save;
using UnityEngine;

namespace ApexShift.Runtime.World.Landmarks
{
    public static class LandmarkRegistry
    {
        private static readonly List<LandmarkRuntime> landmarks = new List<LandmarkRuntime>();

        public static IReadOnlyList<LandmarkRuntime> Landmarks { get { Cleanup(); return landmarks; } }
        public static int LandmarkCount { get { Cleanup(); return landmarks.Count; } }

        public static int DiscoveredCount
        {
            get
            {
                Cleanup();
                int count = 0;
                for (int i = 0; i < landmarks.Count; i++)
                {
                    LandmarkRuntime landmark = landmarks[i];
                    if (landmark != null && landmark.IsDiscovered) count++;
                }
                return count;
            }
        }

        public static void Register(LandmarkRuntime landmark)
        {
            if (landmark == null || landmarks.Contains(landmark)) return;
            landmarks.Add(landmark);
        }

        public static void Unregister(LandmarkRuntime landmark)
        {
            if (landmark == null) return;
            landmarks.Remove(landmark);
        }

        public static List<LandmarkSaveData> CaptureSaveData()
        {
            Cleanup();
            List<LandmarkSaveData> result = new List<LandmarkSaveData>(landmarks.Count);
            for (int i = 0; i < landmarks.Count; i++)
            {
                LandmarkRuntime landmark = landmarks[i];
                if (landmark == null || landmark.gameObject == null) continue;
                result.Add(landmark.ToSaveData());
            }
            return result;
        }

        public static void RestoreFromSaveData(IReadOnlyList<LandmarkSaveData> saved, Transform parent = null)
        {
            if (saved == null || saved.Count == 0) return;
            Cleanup();
            for (int i = 0; i < saved.Count; i++)
            {
                LandmarkSaveData data = saved[i];
                if (data == null) continue;
                LandmarkRuntime existing = FindById(data.LandmarkId);
                if (existing != null)
                {
                    existing.ApplySaveData(data);
                    continue;
                }
                LandmarkWorldGenerator.CreateLandmarkObject(parent, data.LandmarkId, LandmarkRuntime.ParseType(data.LandmarkType), data.DisplayName, data.Description, new Vector3(data.X, data.Y, data.Z), data.Discovered);
            }
        }

        public static LandmarkRuntime FindById(string id)
        {
            string expected = Normalize(id);
            if (string.IsNullOrWhiteSpace(expected)) return null;
            Cleanup();
            for (int i = 0; i < landmarks.Count; i++)
            {
                LandmarkRuntime landmark = landmarks[i];
                if (landmark != null && landmark.LandmarkId == expected) return landmark;
            }
            return null;
        }

        public static void ClearForWorldRegeneration() => landmarks.Clear();
        public static void ClearForTests() => landmarks.Clear();

        private static void Cleanup()
        {
            for (int i = landmarks.Count - 1; i >= 0; i--)
                if (landmarks[i] == null) landmarks.RemoveAt(i);
        }

        private static string Normalize(string value)
            => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
