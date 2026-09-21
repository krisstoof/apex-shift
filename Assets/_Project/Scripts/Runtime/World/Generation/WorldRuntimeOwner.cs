using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    /// <summary>Owns and deterministically destroys objects created by one generation.</summary>
    public sealed class WorldRuntimeOwner : MonoBehaviour
    {
        public const string GenerationRootName = "GenerationRoot";
        [SerializeField, HideInInspector]
        private Transform generationRoot;

        public WorldGenerationContext CurrentContext { get; private set; }
        public Transform GenerationRoot => generationRoot;
        private bool destroyImmediately = true;

        public void Configure(bool destroyObjectsImmediately)
        {
            destroyImmediately = destroyObjectsImmediately;
        }

        public WorldGenerationContext BeginGeneration(int seed)
        {
            Clear();
            var root = new GameObject(GenerationRootName).transform;
            root.SetParent(transform, false);
            generationRoot = root;
            CurrentContext = new WorldGenerationContext(seed, root);
            return CurrentContext;
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
            if (generationRoot != null)
            {
                DestroyObject(generationRoot.gameObject);
            }

            generationRoot = null;
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
