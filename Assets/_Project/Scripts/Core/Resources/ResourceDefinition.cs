using System;
using ApexShift.Core.Items;

namespace ApexShift.Core.Resources
{
    public sealed class ResourceDefinition
    {
        public ResourceDefinition(ResourceId id, string displayName, string itemId, int harvestAmount, bool playerHarvestable = true, bool removeWhenHarvested = true)
        {
            if (!id.IsValid) throw new ArgumentException("Resource id must be valid.", nameof(id));
            if (!ApexShift.Core.Items.ItemId.TryCreate(itemId, out ApexShift.Core.Items.ItemId normalizedItemId)) throw new ArgumentException("Drop item id must be valid.", nameof(itemId));
            if (harvestAmount < 1) throw new ArgumentOutOfRangeException(nameof(harvestAmount));

            Id = id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? id.ToString() : displayName.Trim();
            ItemId = normalizedItemId.ToString();
            HarvestAmount = harvestAmount;
            PlayerHarvestable = playerHarvestable;
            RemoveWhenHarvested = removeWhenHarvested;
        }

        public ResourceId Id { get; }
        public string DisplayName { get; }
        public string ItemId { get; }
        public int HarvestAmount { get; }
        public bool PlayerHarvestable { get; }
        public bool RemoveWhenHarvested { get; }

        public ResourceState CreateState() => new ResourceState(this);

        public static ResourceDefinition CreateDefault(string resourceKind)
        {
            string normalized = ResourceId.Normalize(resourceKind);
            switch (normalized)
            {
                case "tree":
                case "conifer_tree":
                    return new ResourceDefinition(new ResourceId("conifer_tree"), "Tree", "wood", 4);
                case "leafy_tree":
                    return new ResourceDefinition(new ResourceId("leafy_tree"), "Leafy Tree", "wood", 4);
                case "dry_tree":
                    return new ResourceDefinition(new ResourceId("dry_tree"), "Dry Tree", "wood", 3);
                case "rock":
                    return new ResourceDefinition(new ResourceId("rock"), "Rock", "stone", 2);
                case "bush":
                    return new ResourceDefinition(new ResourceId("bush"), "Bush", "fiber", 2);
                case "dry_bush":
                    return new ResourceDefinition(new ResourceId("dry_bush"), "Dry Bush", "fiber", 1);
                case "small_bush":
                    return new ResourceDefinition(new ResourceId("small_bush"), "Small Bush", "fiber", 1);
                case "berry_bush":
                    return new ResourceDefinition(new ResourceId("berry_bush"), "Berry Bush", "berries", 1, playerHarvestable: false);
                case "meat_drop":
                    return new ResourceDefinition(new ResourceId("meat_drop"), "Meat", "meat", 1);
                case "bone_drop":
                    return new ResourceDefinition(new ResourceId("bone_drop"), "Bone", "bone", 1);
                default:
                    if (ResourceId.TryCreate(normalized, out ResourceId id) && ApexShift.Core.Items.ItemId.TryCreate(normalized, out ApexShift.Core.Items.ItemId normalizedItemId2))
                    {
                        return new ResourceDefinition(id, ToDisplayName(normalized), normalizedItemId2.ToString(), 1);
                    }

                    throw new ArgumentException("Resource kind cannot be empty.", nameof(resourceKind));
            }
        }

        private static string ToDisplayName(string normalized)
        {
            if (string.IsNullOrWhiteSpace(normalized)) return string.Empty;
            string[] parts = normalized.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
                }
            }
            return string.Join(" ", parts);
        }
    }
}
