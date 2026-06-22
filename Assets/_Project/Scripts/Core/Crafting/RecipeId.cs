using System;

namespace ApexShift.Core.Crafting
{
    public readonly struct RecipeId : IEquatable<RecipeId>
    {
        private readonly string value;

        public RecipeId(string value)
        {
            string normalized = Normalize(value);
            if (normalized.Length == 0)
            {
                throw new ArgumentException("Recipe id cannot be empty.", nameof(value));
            }

            this.value = normalized;
        }

        public bool IsValid => value != null;

        public override string ToString() => value ?? string.Empty;

        public bool Equals(RecipeId other) => string.Equals(value, other.value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is RecipeId other && Equals(other);

        public override int GetHashCode() => value != null ? StringComparer.Ordinal.GetHashCode(value) : 0;

        public static bool operator ==(RecipeId left, RecipeId right) => left.Equals(right);

        public static bool operator !=(RecipeId left, RecipeId right) => !left.Equals(right);

        public static bool TryCreate(string value, out RecipeId recipeId)
        {
            string normalized = Normalize(value);
            if (normalized.Length == 0)
            {
                recipeId = default;
                return false;
            }

            recipeId = new RecipeId(normalized);
            return true;
        }

        internal static string Normalize(string value)
        {
            return value == null ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
