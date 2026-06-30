using System;
using System.Collections.Generic;
using System.Linq;

namespace ApexShift.Core.Items
{
    public sealed class ItemDatabase
    {
        private readonly Dictionary<string, ItemDefinition> definitionsById;
        private readonly Dictionary<string, ItemDefinition> definitionsByName;

        private ItemDatabase(IEnumerable<ItemDefinition> definitions)
        {
            definitionsById = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
            definitionsByName = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (ItemDefinition definition in definitions)
            {
                definitionsById.Add(definition.Id.ToString(), definition);
                definitionsByName[definition.DisplayName] = definition;
            }
        }

        public static ItemDatabase CreateDefault()
        {
            return new ItemDatabase(new[]
            {
                new ItemDefinition(new ItemId("wood"), "Wood", 20),
                new ItemDefinition(new ItemId("stone"), "Stone", 20),
                new ItemDefinition(new ItemId("fiber"), "Fiber", 20),
                new ItemDefinition(new ItemId("meat"), "Meat", 20),
                new ItemDefinition(new ItemId("hide"), "Hide", 20),
                new ItemDefinition(new ItemId("bone"), "Bone", 20),
                new ItemDefinition(new ItemId("torch"), "Torch", 1),
                new ItemDefinition(new ItemId("spear"), "Spear", 1),
                new ItemDefinition(new ItemId("bow"), "Bow", 1),
                new ItemDefinition(new ItemId("arrow"), "Arrow", 20),
                new ItemDefinition(new ItemId("axe"), "Axe", 1),
                new ItemDefinition(new ItemId("pickaxe"), "Pickaxe", 1),
                new ItemDefinition(new ItemId("campfire"), "Campfire", 1),
                new ItemDefinition(new ItemId("trap"), "Trap", 1),
                new ItemDefinition(new ItemId("wall"), "Wall", 20),
                new ItemDefinition(new ItemId("storage_box"), "Storage Box", 1),
                new ItemDefinition(new ItemId("berries"), "Berries", 20),
                new ItemDefinition(new ItemId("grass"), "Grass", 20),
                new ItemDefinition(new ItemId("tent"), "Tent", 1)
            });
        }

        public static ItemDatabase FromDefinitions(IEnumerable<ItemDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            return new ItemDatabase(definitions);
        }

        public bool HasItem(string itemId) => HasItem(NormalizeItemId(itemId));

        public bool HasItem(ItemId itemId) => itemId.IsValid && definitionsById.ContainsKey(itemId.ToString());

        public ItemId NormalizeItemId(string value)
        {
            string normalized = ItemId.Normalize(value);
            if (normalized.Length == 0)
            {
                return default;
            }

            if (definitionsByName.TryGetValue(value?.Trim() ?? string.Empty, out ItemDefinition byName))
            {
                return byName.Id;
            }

            if (definitionsById.TryGetValue(normalized, out ItemDefinition byId))
            {
                return byId.Id;
            }

            return new ItemId(normalized);
        }

        public ItemDefinition GetDefinition(string itemId) => GetDefinition(NormalizeItemId(itemId));

        public ItemDefinition GetDefinition(ItemId itemId)
        {
            if (!itemId.IsValid || !definitionsById.TryGetValue(itemId.ToString(), out ItemDefinition definition))
            {
                throw new KeyNotFoundException($"Unknown item id '{itemId}'.");
            }

            return definition;
        }

        public int GetMaxStack(string itemId)
        {
            ItemId itemIdValue = NormalizeItemId(itemId);
            return HasItem(itemIdValue) ? GetDefinition(itemIdValue).MaxStackSize : 0;
        }

        public string GetDisplayName(string itemId)
        {
            ItemId itemIdValue = NormalizeItemId(itemId);
            return HasItem(itemIdValue) ? GetDefinition(itemIdValue).DisplayName : string.Empty;
        }

        public IReadOnlyCollection<ItemDefinition> GetAllDefinitions() => definitionsById.Values.ToArray();
    }
}
