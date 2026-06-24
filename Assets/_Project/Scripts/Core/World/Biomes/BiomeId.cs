using System;

namespace ApexShift.Core.World.Biomes
{
    public readonly struct BiomeId : IEquatable<BiomeId>
    {
        private readonly string value;

        public BiomeId(string value)
        {
            string normalized = NormalizeId(value);
            if (normalized.Length == 0)
            {
                throw new ArgumentException("Biome id cannot be empty.", nameof(value));
            }

            this.value = normalized;
        }

        public bool IsValid => value != null;
        public override string ToString() => value ?? string.Empty;
        public bool Equals(BiomeId other) => string.Equals(value, other.value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is BiomeId other && Equals(other);
        public override int GetHashCode() => value != null ? StringComparer.Ordinal.GetHashCode(value) : 0;
        public static bool operator ==(BiomeId left, BiomeId right) => left.Equals(right);
        public static bool operator !=(BiomeId left, BiomeId right) => !left.Equals(right);

        public static bool TryCreate(string value, out BiomeId biomeId)
        {
            string normalized = NormalizeId(value);
            if (normalized.Length == 0)
            {
                biomeId = default;
                return false;
            }

            biomeId = new BiomeId(normalized);
            return true;
        }

        public static string NormalizeId(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "_")
                .Replace("-", "_");
        }
}
}
