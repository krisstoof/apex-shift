using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>Owns and deterministically destroys objects created by one generation.</summary>
    public sealed class WorldRuntimeOwner : MonoBehaviour
    {
        public const string GenerationRootName = "GenerationRoot";
        public WorldGenerationContext CurrentContext { get; private set; }
        public Transform GenerationRoot => CurrentContext != null ? CurrentContext.GenerationRoot : null;
        private bool destroyImmediately = true;

        public void Configure(bool destroyObjectsImmediately)
        {
            destroyImmediately = destroyObjectsImmediately;
        }

        public WorldGenerationContext BeginGeneration(int seed)
        {
            if (CurrentContext == null) AdoptLegacyGenerationChildren();
            Clear();
            var root = new GameObject(GenerationRootName).transform;
            root.SetParent(transform, false);
            CurrentContext = new WorldGenerationContext(seed, root);
            return CurrentContext;
        }

        // RuntimeWorld scenes created before the ownership refactor contain generated
        // objects directly below WorldGeneratorRuntime. Adopt only the known generated
        // roots once, then normal lifecycle operations use GenerationRoot exclusively.
        private void AdoptLegacyGenerationChildren()
        {
            // Unit-test owners and newly-created empty generators have no legacy
            // scene graph to migrate. This guard also protects unrelated scene roots.
            if (GetComponent<WorldGeneratorRuntime>() == null) return;
            var candidates = new List<Transform>();
            foreach (Transform child in transform) candidates.Add(child);
            foreach (GameObject rootObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (rootObject != gameObject) candidates.Add(rootObject.transform);
            }

            bool hasLegacyObjects = false;
            foreach (Transform candidate in candidates)
                if (IsGeneratedName(candidate.name)) { hasLegacyObjects = true; break; }
            if (!hasLegacyObjects) return;
            var root = new GameObject(GenerationRootName).transform;
            root.SetParent(transform, false);
            foreach (Transform child in candidates)
            {
                if (child != root && child != transform && IsGeneratedName(child.name))
                    child.SetParent(root, true);
            }
            CurrentContext = new WorldGenerationContext(-1, root);
        }

        private static bool IsGeneratedName(string name)
        {
            switch (name)
            {
                case "GenerationRoot": case "TerrainRoot": case "BiomeRoot": case "ResourceRoot": case "CreatureRoot":
                case "BuildingRoot": case "LandmarkRoot": case "Player": case "Main Camera":
                case "PlayerFollowCamera": case "WorldBounds": case "GameBootstrapper":
                case "EcosystemRuntime": case "DayNightRuntime": case "DayNightSkyRuntime":
                case "GameSnapshotProvider": case "DebugPanelPresenter": case "WorldMapDebugWindow":
                case "IslandTopographyRuntime": case "CreatureIslandBoundsRuntime":
                case "AmbientMusicRuntime": case "AmbientSoundController": case "Directional Light":
                case "ActionBarUI": case "IslandTerrainMesh": case "WaterSurfaceMesh":
                case "SeabedMesh": case "CliffWallsMesh":
                    return true;
                default:
                    return false;
            }
        }

        public void Register(GameObject instance)
        {
            if (instance != null && GenerationRoot != null && instance.transform.parent == null)
            {
                instance.transform.SetParent(GenerationRoot, true);
            }
        }

        public void Clear()
        {
            if (CurrentContext != null && CurrentContext.GenerationRoot != null)
            {
                DestroyObject(CurrentContext.GenerationRoot.gameObject);
            }

            CurrentContext = null;
        }

        private void DestroyObject(GameObject instance)
        {
            if (instance == null) return;
            if (Application.isPlaying && !destroyImmediately) Object.Destroy(instance);
            else Object.DestroyImmediate(instance);
        }
    }
}
