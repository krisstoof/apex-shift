using System;
using ApexShift.Core.Items;

namespace ApexShift.Core.Resources
{
    public sealed class ResourceDefinition
    {
        public ResourceDefinition(
            ResourceId id,
            string displayName,
            string itemId,
            int harvestAmount,
            bool playerHarvestable = true,
            bool removeWhenHarvested = true,
            bool edibleByHerbivores = false,
            float foodValue = 0f,
            bool renderOnly = false,
            bool pondVegetation = false,
            int pickupPriority = 0,
            int regrowthDays = 0)
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
            EdibleByHerbivores = edibleByHerbivores;
            FoodValue = Math.Max(0f, foodValue);
            RenderOnly = renderOnly;
            PondVegetation = pondVegetation;
            PickupPriority = Math.Max(0, pickupPriority);
            RegrowthDays = Math.Max(0, regrowthDays);
        }

        public ResourceId Id { get; }
        public string DisplayName { get; }
        public string ItemId { get; }
        public int HarvestAmount { get; }
        public bool PlayerHarvestable { get; }
        public bool RemoveWhenHarvested { get; }
        public bool EdibleByHerbivores { get; }
        public float FoodValue { get; }
        public bool RenderOnly { get; }
        public bool PondVegetation { get; }
        public int PickupPriority { get; }
        public int RegrowthDays { get; }

        public ResourceState CreateState() => new ResourceState(this);

        public static ResourceDefinition CreateDefault(string resourceKind)
        {
            string normalized = ResourceId.Normalize(resourceKind);
            switch (normalized)
            {
                case "tree":
                case "conifer_tree":
                    return new ResourceDefinition(new ResourceId("conifer_tree"), "Tree", "wood", 4);
                case "small_tree":
                    return new ResourceDefinition(new ResourceId("small_tree"), "Small Tree", "wood", 2);
                case "big_tree":
                    return new ResourceDefinition(new ResourceId("big_tree"), "Big Tree", "wood", 7);
                case "leafy_tree":
                    return new ResourceDefinition(new ResourceId("leafy_tree"), "Leafy Tree", "wood", 4);
                case "dry_tree":
                    return new ResourceDefinition(new ResourceId("dry_tree"), "Dry Tree", "wood", 3);
                case "rock":
                    return new ResourceDefinition(new ResourceId("rock"), "Rock", "stone", 2);
                case "small_rock":
                    return new ResourceDefinition(new ResourceId("small_rock"), "Small Rock", "stone", 1);
                case "big_rock":
                    return new ResourceDefinition(new ResourceId("big_rock"), "Big Rock", "stone", 5);
                case "bush":
                    return new ResourceDefinition(new ResourceId("bush"), "Bush", "fiber", 2, edibleByHerbivores: true, foodValue: 6f);
                case "dry_bush":
                    return new ResourceDefinition(new ResourceId("dry_bush"), "Dry Bush", "fiber", 1);
                case "small_bush":
                    return new ResourceDefinition(new ResourceId("small_bush"), "Small Bush", "fiber", 1, edibleByHerbivores: true, foodValue: 3f);
                case "berry_bush":
                    return new ResourceDefinition(new ResourceId("berry_bush"), "Berry Bush", "berries", 1, playerHarvestable: false, removeWhenHarvested: false, edibleByHerbivores: true, foodValue: 8f, regrowthDays: 2);
                case "grass_patch":
                    return new ResourceDefinition(new ResourceId("grass_patch"), "Grass Patch", "grass", 1, playerHarvestable: false, removeWhenHarvested: false, edibleByHerbivores: true, foodValue: 5f, renderOnly: true, regrowthDays: 1);
                case "dense_grass":
                    return new ResourceDefinition(new ResourceId("dense_grass"), "Dense Grass", "grass", 1, playerHarvestable: false, removeWhenHarvested: false, edibleByHerbivores: true, foodValue: 10f, renderOnly: true, regrowthDays: 1);
                case "meat_drop":
                    return new ResourceDefinition(new ResourceId("meat_drop"), "Meat", "meat", 1, pickupPriority: 20);
                case "bone_drop":
                    return new ResourceDefinition(new ResourceId("bone_drop"), "Bone", "bone", 1, pickupPriority: 20);
                case "item_drop":
                    return new ResourceDefinition(new ResourceId("item_drop"), "Item Drop", "wood", 1, pickupPriority: 30);
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
