using System.Collections.Generic;
using ApexShift.Runtime.Creatures;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApexShift.EditorTools
{
    public static class CreatureBehaviorMigrationTools
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/Game.unity",
            "Assets/_Project/Scenes/RuntimeWorld.unity",
            "Assets/_Project/Scenes/BiomeWorldTest.unity",
            "Assets/_Recovery/0.unity",
        };

        [MenuItem("Tools/Apex Shift/Creatures/Scan Behavior Brain Migration")]
        public static void Scan()
        {
            var report = new List<string>();
            foreach (string scenePath in ScenePaths)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        ScanHierarchy(root.transform, scenePath, report);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            if (report.Count == 0)
            {
                Debug.Log("Creature behavior migration scan: no stale runtime-only creature setups found.");
                return;
            }

            foreach (string line in report)
            {
                Debug.LogWarning(line);
            }
        }

        [MenuItem("Tools/Apex Shift/Creatures/Migrate Creature Behavior Brain")]
        public static void Migrate()
        {
            var report = new List<string>();
            int migratedCount = 0;
            int removedCount = 0;
            foreach (string scenePath in ScenePaths)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                bool changed = false;
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        changed |= MigrateHierarchy(root.transform, scenePath, report, ref migratedCount, ref removedCount);
                    }

                    if (changed)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            if (report.Count == 0)
            {
                Debug.Log("Creature behavior migration: nothing to migrate.");
            }
            else
            {
                Debug.Log($"Creature behavior migration summary: added {migratedCount} brain component(s), removed {removedCount} legacy runtime component(s).");
                foreach (string line in report)
                {
                    Debug.Log(line);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Apex Shift/Creatures/Migrate All Creature Components")]
        public static void MigrateAllCreatureComponents()
        {
            Migrate();
            ScanAnimationDrivers();
            ReportMissingAnimationDrivers();
        }

        private static void ScanHierarchy(Transform root, string scenePath, ICollection<string> report)
        {
            GameObject go = root.gameObject;
            if (!IsCreature(go))
            {
                return;
            }

            bool hasRuntime = go.GetComponent<CreatureBehaviorRuntime>() != null;
            bool hasBrain = go.GetComponent<CreatureBehaviorBrain>() != null;
            if (hasRuntime && !hasBrain)
            {
                report.Add($"{scenePath} :: {GetHierarchyPath(root)} still uses CreatureBehaviorRuntime without CreatureBehaviorBrain");
            }

            for (int i = 0; i < root.childCount; i++)
            {
                ScanHierarchy(root.GetChild(i), scenePath, report);
            }
        }

        private static bool MigrateHierarchy(Transform root, string scenePath, ICollection<string> report, ref int migratedCount, ref int removedCount)
        {
            bool changed = false;
            GameObject go = root.gameObject;
            if (IsCreature(go))
            {
                CreatureBehaviorRuntime runtime = go.GetComponent<CreatureBehaviorRuntime>();
                CreatureBehaviorBrain brain = go.GetComponent<CreatureBehaviorBrain>();
                if (brain == null && runtime != null)
                {
                    brain = Undo.AddComponent<CreatureBehaviorBrain>(go);
                    changed = true;
                    migratedCount++;
                    report.Add($"{scenePath} :: {GetHierarchyPath(root)} added CreatureBehaviorBrain");
                }

                if (brain != null && runtime != null)
                {
                    Undo.DestroyObjectImmediate(runtime);
                    changed = true;
                    removedCount++;
                    report.Add($"{scenePath} :: {GetHierarchyPath(root)} removed CreatureBehaviorRuntime");
                }
            }

            for (int i = 0; i < root.childCount; i++)
            {
                changed |= MigrateHierarchy(root.GetChild(i), scenePath, report, ref migratedCount, ref removedCount);
            }

            return changed;
        }

        [MenuItem("Tools/Apex Shift/Creatures/Scan Creature Animation Drivers")]
        public static void ScanAnimationDrivers()
        {
            var report = new List<string>();
            foreach (string scenePath in ScenePaths)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        ScanAnimationDriverHierarchy(root.transform, scenePath, report);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            if (report.Count == 0)
            {
                Debug.Log("Creature animation scan: every scanned creature has a CreatureAnimationDriver.");
                return;
            }

            foreach (string line in report)
            {
                Debug.LogWarning(line);
            }
        }

        private static void ReportMissingAnimationDrivers()
        {
            var report = new List<string>();
            foreach (string scenePath in ScenePaths)
            {
                if (!System.IO.File.Exists(scenePath))
                {
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try
                {
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        CollectMissingDriverReport(root.transform, scenePath, report);
                    }
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }

            if (report.Count == 0)
            {
                Debug.Log("Creature animation report: no creatures are missing CreatureAnimationDriver.");
                return;
            }

            Debug.LogWarning("Creature animation report: some creatures are still missing CreatureAnimationDriver:");
            foreach (string line in report)
            {
                Debug.LogWarning(line);
            }
        }

        private static bool IsCreature(GameObject go)
        {
            return go != null && go.name.StartsWith("Creature_", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void ScanAnimationDriverHierarchy(Transform root, string scenePath, ICollection<string> report)
        {
            GameObject go = root.gameObject;
            if (IsCreature(go) && go.GetComponent<CreatureAnimationDriver>() == null)
            {
                report.Add($"{scenePath} :: {GetHierarchyPath(root)} is missing CreatureAnimationDriver");
            }

            for (int i = 0; i < root.childCount; i++)
            {
                ScanAnimationDriverHierarchy(root.GetChild(i), scenePath, report);
            }
        }

        private static void CollectMissingDriverReport(Transform root, string scenePath, ICollection<string> report)
        {
            GameObject go = root.gameObject;
            if (IsCreature(go) && go.GetComponent<CreatureAnimationDriver>() == null)
            {
                report.Add($"{scenePath} :: {GetHierarchyPath(root)} missing CreatureAnimationDriver");
            }

            for (int i = 0; i < root.childCount; i++)
            {
                CollectMissingDriverReport(root.GetChild(i), scenePath, report);
            }
        }

        private static string GetHierarchyPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }

            return string.Join("/", stack);
        }
    }
}
