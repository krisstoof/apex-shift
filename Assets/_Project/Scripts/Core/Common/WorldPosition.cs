using System;

namespace ApexShift.Core.Common
{
    public readonly struct WorldPosition : IEquatable<WorldPosition>
    {
        public float X { get; }
        public float Z { get; }

        public WorldPosition(float x, float z)
        {
            X = x;
            Z = z;
        }

        public static WorldPosition Zero => new WorldPosition(0f, 0f);

        public float DistanceTo(WorldPosition other)
        {
            float dx = other.X - X;
            float dz = other.Z - Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        public bool Equals(WorldPosition other) => X.Equals(other.X) && Z.Equals(other.Z);

        public override bool Equals(object obj) => obj is WorldPosition other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Z);
    }
}

