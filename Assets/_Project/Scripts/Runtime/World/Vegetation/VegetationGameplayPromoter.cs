using System.Collections.Generic;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Resources;
using UnityEngine;

namespace ApexShift.Runtime.World.Vegetation
{
    /// <summary>Promotes only explicitly harvestable tree species into resource gameplay.</summary>
    public static class VegetationGameplayPromoter
    {
        public static HarvestableTreeRuntime Promote(GameObject instance, VegetationInstanceRuntime marker,
            VegetationSpeciesAsset species)
        {
            if (instance == null || marker == null || species == null || !species.Harvestable
                || (species.Category != VegetationCategory.Tree && species.Category != VegetationCategory.DeadTree)) return null;
            if (string.IsNullOrWhiteSpace(species.ResourceKind))
            {
                Debug.LogError($"Harvestable tree species '{species.SpeciesId}' has no ResourceDefinition kind.", instance);
                return null;
            }

            ResourceNodeView node = instance.GetComponent<ResourceNodeView>();
            if (node == null) node = instance.AddComponent<ResourceNodeView>();
            node.ConfigureDefault(species.ResourceKind);
            node.ConfigureToolRequirement("axe");
            node.SetShowOnResourceMap(false);
            node.SetDirectInteractionEnabled(false);

            FoodSourceView[] foodSources = instance.GetComponentsInChildren<FoodSourceView>(true);
            for (int i = 0; i < foodSources.Length; i++)
                if (foodSources[i] != null) foodSources[i].enabled = false;

            ConfigureTrunkCollider(instance, species, node);

            HarvestableTreeRuntime tree = instance.GetComponent<HarvestableTreeRuntime>();
            if (tree == null) tree = instance.AddComponent<HarvestableTreeRuntime>();
            tree.Configure(marker, species, node);
            return tree;
        }

        private static void ConfigureTrunkCollider(GameObject instance, VegetationSpeciesAsset species, ResourceNodeView node)
        {
            VegetationTrunkCollider[] markedTrunks = instance.GetComponentsInChildren<VegetationTrunkCollider>(true);
            var ownedTrunkColliders = new HashSet<Collider>();
            for (int i = 0; i < markedTrunks.Length; i++)
            {
                VegetationTrunkCollider marker = markedTrunks[i];
                if (marker == null) continue;
                Collider[] colliders = marker.GetComponents<Collider>();
                if (colliders.Length == 0)
                {
                    CapsuleCollider added = marker.gameObject.AddComponent<CapsuleCollider>();
                    ConfigureCapsule(added, species);
                    ownedTrunkColliders.Add(added);
                }
                else
                {
                    for (int c = 0; c < colliders.Length; c++)
                    {
                        colliders[c].enabled = true;
                        colliders[c].isTrigger = false;
                        ownedTrunkColliders.Add(colliders[c]);
                    }
                }
            }

            if (ownedTrunkColliders.Count == 0)
            {
                var trunkObject = new GameObject("ApexShiftCombatTrunk");
                trunkObject.transform.SetParent(instance.transform, false);
                trunkObject.transform.localPosition = new Vector3(0f, species.TrunkCenterY, 0f);
                trunkObject.transform.localRotation = Quaternion.identity;
                trunkObject.transform.localScale = Vector3.one;
                trunkObject.AddComponent<VegetationTrunkCollider>();
                CapsuleCollider capsule = trunkObject.AddComponent<CapsuleCollider>();
                ConfigureCapsule(capsule, species);
                ownedTrunkColliders.Add(capsule);
            }

            Collider interactionTrigger = node != null ? node.GetComponent<SphereCollider>() : null;
            Collider[] allColliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < allColliders.Length; i++)
            {
                Collider collider = allColliders[i];
                if (collider == null || collider == interactionTrigger) continue;
                collider.enabled = ownedTrunkColliders.Contains(collider);
            }
        }

        private static void ConfigureCapsule(CapsuleCollider capsule, VegetationSpeciesAsset species)
        {
            capsule.enabled = true;
            capsule.isTrigger = false;
            capsule.direction = 1;
            capsule.radius = species.TrunkRadius;
            capsule.height = Mathf.Max(species.TrunkHeight, species.TrunkRadius * 2f);
            capsule.center = Vector3.zero;
        }
    }
}
