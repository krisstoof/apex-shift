using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexShift.Presentation.Icons
{
    [Serializable]
    public sealed class ItemIconEntry
    {
        public string itemId;
        public Sprite sprite;
    }

    [CreateAssetMenu(menuName = "Apex Shift/UI/Item Icon Catalog", fileName = "ItemIconCatalog")]
    public sealed class ItemIconCatalog : ScriptableObject
    {
        [SerializeField] private Sprite fallbackIcon;
        [SerializeField] private List<ItemIconEntry> entries = new List<ItemIconEntry>();
        private Dictionary<string, Sprite> lookup;

        public Sprite FallbackIcon => fallbackIcon;

        public bool TryGetIcon(string itemId, out Sprite sprite)
        {
            BuildLookup();
            string key = Normalize(itemId);
            if (lookup.TryGetValue(key, out sprite) && sprite != null) return true;
            sprite = fallbackIcon;
            return sprite != null;
        }

        private void BuildLookup()
        {
            if (lookup != null) return;
            lookup = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (ItemIconEntry entry in entries)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.itemId) && entry.sprite != null)
                    lookup[Normalize(entry.itemId)] = entry.sprite;
            }
        }

        private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
    }
}
