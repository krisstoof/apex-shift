using ApexShift.Runtime.Resources;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    [DisallowMultipleComponent]
    public sealed class HarvestableTreeRuntime : MonoBehaviour, IAxeHitTarget
    {
        [SerializeField] private string treeId = string.Empty;
        [SerializeField] private string speciesId = string.Empty;
        [SerializeField] private string biomeId = string.Empty;
        [SerializeField] private string resourceKind = string.Empty;
        [SerializeField] private ResourceNodeView resourceNode;

        public string TreeId => treeId;
        public string SpeciesId => speciesId;
        public string BiomeId => biomeId;
        public string ResourceKind => resourceKind;
        public ResourceNodeView ResourceNode => resourceNode;

        public void Configure(VegetationInstanceRuntime marker, string kind, ResourceNodeView node)
        {
            if (marker == null || string.IsNullOrWhiteSpace(marker.InstanceId))
            {
                Debug.LogError("HarvestableTreeRuntime requires a configured VegetationInstanceRuntime with a stable InstanceId.", this);
                return;
            }

            HarvestableTreeRegistry.Unregister(this);
            treeId = marker.InstanceId;
            speciesId = marker.SpeciesId;
            biomeId = marker.BiomeId;
            resourceKind = string.IsNullOrWhiteSpace(kind) ? marker.ResourceKind : kind.Trim().ToLowerInvariant();
            resourceNode = node;
            HarvestableTreeRegistry.Register(this);
        }

        public bool TryAxeHit(GameObject actor)
        {
            return resourceNode != null && resourceNode.CanInteract(actor) && resourceNode.Interact(actor);
        }

        private void OnEnable()
        {
            // Also restores registry ownership for trees serialized into editor-built scenes.
            if (!string.IsNullOrWhiteSpace(treeId)) HarvestableTreeRegistry.Register(this);
        }

        private void OnDestroy()
        {
            HarvestableTreeRegistry.Unregister(this);
        }
    }
}
