using System;

namespace ApexShift.Core.Items
{
    public readonly struct ItemId : IEquatable<ItemId>
    {
        private readonly string value;

        public ItemId(string value)
        {
            string normalized = Normalize(value);
            if (normalized.Length == 0)
            {
                throw new ArgumentException("Item id cannot be empty.", nameof(value));
            }

            this.value = normalized;
        }

        public bool IsValid => value != null;

        public override string ToString() => value ?? string.Empty;

        public bool Equals(ItemId other) => string.Equals(value, other.value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is ItemId other && Equals(other);

        public override int GetHashCode() => value != null ? StringComparer.Ordinal.GetHashCode(value) : 0;

        public static bool operator ==(ItemId left, ItemId right) => left.Equals(right);

        public static bool operator !=(ItemId left, ItemId right) => !left.Equals(right);

        public static bool TryCreate(string value, out ItemId itemId)
        {
            string normalized = Normalize(value);
            if (normalized.Length == 0)
            {
                itemId = default;
                return false;
            }

            itemId = new ItemId(normalized);
            return true;
        }

        internal static string Normalize(string value)
        {
            return value == null ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}

