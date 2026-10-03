using System.Collections.Generic;
using System.Linq;
using ApexShift.Core.Save;
using UnityEngine;

namespace ApexShift.Runtime.Story.Clues
{
    public static class StoryClueRegistry
    {
        private static readonly List<StoryClueRuntime> clues = new List<StoryClueRuntime>();
        public static IReadOnlyList<StoryClueRuntime> Clues { get { Cleanup(); return clues; } }

        public static void Register(StoryClueRuntime clue)
        {
            if (clue == null || string.IsNullOrEmpty(clue.ClueId) || clues.Contains(clue)) return;
            if (FindById(clue.ClueId) != null)
            {
                Debug.LogWarning("[StoryClue] Duplicate clue ID: " + clue.ClueId, clue);
                return;
            }
            clues.Add(clue);
        }

        public static void Unregister(StoryClueRuntime clue) => clues.Remove(clue);
        public static StoryClueRuntime FindById(string id)
        {
            Cleanup();
            string normalized = StoryMilestoneIds.Normalize(id);
            return clues.FirstOrDefault(c => c.ClueId == normalized);
        }

        public static List<StoryClueSaveData> CaptureSaveData()
            => Clues.OrderBy(c => c.ClueId, System.StringComparer.Ordinal).Select(c => c.ToSaveData()).ToList();

        public static void RestoreFromSaveData(IReadOnlyList<StoryClueSaveData> saved)
        {
            // Missing additive state in old saves means undiscovered, not a new object.
            foreach (var clue in Clues) clue.SetDiscovered(false);
            if (saved == null) return;
            foreach (var data in saved)
            {
                if (data == null) continue;
                var existing = FindById(data.ClueId);
                if (existing != null) existing.ApplySaveData(data);
                else Debug.LogWarning("[StoryClue] Saved clue is no longer available; skipping '" + data.ClueId + "'.");
            }
        }

        public static void ClearForWorldRegeneration() => clues.Clear();
        public static void ClearForTests() => clues.Clear();
        private static void Cleanup() => clues.RemoveAll(c => c == null);
    }
}
