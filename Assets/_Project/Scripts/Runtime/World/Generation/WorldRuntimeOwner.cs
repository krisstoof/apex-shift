using UnityEngine;

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
            Clear();
            var root = new GameObject(GenerationRootName).transform;
            root.SetParent(transform, false);
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
