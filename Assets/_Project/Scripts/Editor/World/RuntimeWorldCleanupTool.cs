using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Generation;
using ApexShift.Runtime.World.Landmarks;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Vegetation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApexShift.Editor.World
{
    public static class RuntimeWorldCleanupTool
    {
        public const string ScenePath = "Assets/_Project/Scenes/RuntimeWorld.unity";
        private static readonly string[] ContentRoots =
        {
            "ResourceRoot", "VegetationRoot", "BuildingRoot", "LandmarkRoot", "CreatureRoot"
        };

        public sealed class CleanupReport
        {
            // Counts all GameObjects below each content container, excluding the container itself.
            public readonly Dictionary<string, int> RemovedContent = ContentRoots.ToDictionary(name => name, _ => 0);
            public int OrphanRuntimeObjects { get; internal set; }
            public bool TerrainPreserved { get; internal set; }

            public override string ToString() => string.Join(", ", RemovedContent.Select(pair => pair.Key + "=" + pair.Value))
                + ", orphan/runtime GameObjects=" + OrphanRuntimeObjects + ", TerrainRoot preserved=" + TerrainPreserved;
        }

        [MenuItem("Tools/Apex Shift/World/Clean Runtime World Content")]
        public static void CleanFromMenu() => CleanRuntimeWorld();

        public static CleanupReport CleanRuntimeWorld()
        {
            Scene scene = OpenRuntimeWorld();
            WorldGeneratorRuntime generator = GetGenerator(scene);
            var owner = generator.GetComponent<WorldRuntimeOwner>();
            Transform ownedRoot = owner != null ? owner.GenerationRoot : null;
            if (ownedRoot != null && (!ownedRoot.IsChildOf(generator.transform) || ownedRoot.gameObject.scene != scene))
                throw new InvalidOperationException("GenerationRoot does not belong to the RuntimeWorld generator; cleanup aborted.");
            CleanupReport report = CleanGenerationRoot(ownedRoot);
            Save(scene);
            Debug.Log("RuntimeWorld cleanup: " + report);
            return report;
        }

        [MenuItem("Tools/Apex Shift/World/Generate Bare Island Preview")]
        public static void GenerateBareIslandPreview()
        {
            Scene scene = OpenRuntimeWorld();
            WorldGeneratorRuntime generator = GetGenerator(scene);
            generator.Generate();
            CleanupReport report = CleanGenerationRoot(generator.GetComponent<WorldRuntimeOwner>().GenerationRoot);
            Save(scene);
            Debug.Log("RuntimeWorld bare island preview: " + report);
        }

        /// <summary>Editor-only removal confined to an explicit generated ownership container.
        /// Never scans other scene roots or deletes source assets.</summary>
        public static CleanupReport CleanGenerationRoot(Transform generationRoot)
        {
            EnsureEditMode();
            var report = new CleanupReport();
            if (generationRoot == null) return report;
            if (EditorUtility.IsPersistent(generationRoot) || !generationRoot.gameObject.scene.IsValid())
                throw new ArgumentException("Cleanup requires a scene instance, not a prefab/asset.", nameof(generationRoot));

            Transform terrain = generationRoot.Find("TerrainRoot");
            Transform biome = generationRoot.Find("BiomeRoot");
            var preserved = new List<Transform>();
            if (terrain != null) preserved.Add(terrain);
            if (biome != null) preserved.Add(biome);
            preserved.AddRange(generationRoot.GetComponentsInChildren<IslandTopographyRuntime>(true)
                .Select(component => component.transform));

            // Remove known content containers first for distinct, non-overlapping category counts.
            foreach (string name in ContentRoots)
            {
                Transform content = generationRoot.Find(name);
                if (content == null) continue;
                if (preserved.Any(item => item == content || item.IsChildOf(content)))
                    throw new InvalidOperationException(name + " unexpectedly owns preserved terrain/topography; cleanup aborted.");
            }
            foreach (string name in ContentRoots)
            {
                Transform content = generationRoot.Find(name);
                if (content == null) continue;
                report.RemovedContent[name] = content.GetComponentsInChildren<Transform>(true).Length - 1;
                UnityEngine.Object.DestroyImmediate(content.gameObject);
            }

            RemoveOtherOwnedChildren(generationRoot, preserved, report);
            // Legacy component-marked content may be nested inside the retained biome container.
            if (biome != null)
                foreach (Transform item in biome.GetComponentsInChildren<Transform>(true))
                {
                    if (item == null || item == biome || IsProtected(item, preserved.Where(root => root != biome)))
                        continue;
                    if (HasWorldContent(item.gameObject))
                    {
                        report.OrphanRuntimeObjects += item.GetComponentsInChildren<Transform>(true).Length;
                        UnityEngine.Object.DestroyImmediate(item.gameObject);
                    }
                }
            report.TerrainPreserved = terrain != null && terrain.IsChildOf(generationRoot);
            return report;
        }

        private static void RemoveOtherOwnedChildren(Transform parent, List<Transform> preserved, CleanupReport report)
        {
            foreach (Transform child in parent.Cast<Transform>().ToArray())
            {
                if (preserved.Contains(child)) continue;
                if (preserved.Any(item => item.IsChildOf(child)))
                    RemoveOtherOwnedChildren(child, preserved, report);
                else
                {
                    report.OrphanRuntimeObjects += child.GetComponentsInChildren<Transform>(true).Length;
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static bool IsProtected(Transform item, IEnumerable<Transform> preserved)
            => preserved.Any(root => item == root || item.IsChildOf(root) || root.IsChildOf(item));

        private static bool HasWorldContent(GameObject item)
            => item.GetComponent<VegetationInstanceRuntime>() != null
               || item.GetComponent<HarvestableTreeRuntime>() != null
               || item.GetComponent<ResourceNodeView>() != null
               || item.GetComponent<PlaceableStructureRuntime>() != null
               || item.GetComponent<LandmarkRuntime>() != null
               || item.GetComponent<CreatureAgentView>() != null;

        private static Scene OpenRuntimeWorld()
        {
            EnsureEditMode();
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                throw new InvalidOperationException("RuntimeWorld has unsaved edits. Save or discard them before cleanup.");
            if (!scene.IsValid() || !scene.isLoaded)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            return scene;
        }

        private static WorldGeneratorRuntime GetGenerator(Scene scene)
        {
            WorldGeneratorRuntime[] generators = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<WorldGeneratorRuntime>(true)).ToArray();
            if (generators.Length != 1)
                throw new InvalidOperationException("RuntimeWorld must contain exactly one WorldGeneratorRuntime; cleanup aborted.");
            return generators[0];
        }

        private static void EnsureEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("RuntimeWorld cleanup/preview is available only in Edit Mode.");
        }

        private static void Save(Scene scene)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Could not save RuntimeWorld after cleanup.");
        }
    }
}
