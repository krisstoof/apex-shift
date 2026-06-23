using System;

namespace ApexShift.Core.Resources
{
    public readonly struct ResourceId : IEquatable<ResourceId>
    {
        private readonly string value;

        public ResourceId(string value)
        {
            string normalized = Normalize(value);
            if (normalized.Length == 0)
            {
                throw new ArgumentException("Resource id cannot be empty.", nameof(value));
            }

            this.value = normalized;
        }

        public bool IsValid => value != null;
        public override string ToString() => value ?? string.Empty;
        public bool Equals(ResourceId other) => string.Equals(value, other.value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is ResourceId other && Equals(other);
        public override int GetHashCode() => value != null ? StringComparer.Ordinal.GetHashCode(value) : 0;
        public static bool operator ==(ResourceId left, ResourceId right) => left.Equals(right);
        public static bool operator !=(ResourceId left, ResourceId right) => !left.Equals(right);

        public static bool TryCreate(string value, out ResourceId resourceId)
        {
            string normalized = Normalize(value);
            if (normalized.Length == 0)
            {
                resourceId = default;
                return false;
            }

            resourceId = new ResourceId(normalized);
            return true;
        }

        public static string Normalize(string value)
        {
            return value == null ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
