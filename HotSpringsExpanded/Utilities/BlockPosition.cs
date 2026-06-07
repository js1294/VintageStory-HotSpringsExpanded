using System;

namespace HotSpringsExpanded.Utilities
{
    /// <summary>
    /// Wrapper struct around Vintage Story's BlockPos.
    /// Provides implicit conversion operators so it can be passed directly to API methods.
    /// </summary>
    public readonly struct BlockPosition(int x, int y, int z) : IEquatable<BlockPosition>
    {
        private readonly Vintagestory.API.MathTools.BlockPos _pos = new(x, y, z);

        /// <summary>
        /// The X coordinate of the block position.
        /// </summary>
        public int X => _pos.X;

        /// <summary>
        /// The Y coordinate of the block position.
        /// </summary>
        public int Y => _pos.Y;

        /// <summary>
        /// The Z coordinate of the block position.
        /// </summary>
        public int Z => _pos.Z;

        /// <summary>
        /// Calculates the squared Euclidean distance to another position.
        /// Avoids the cost of a square root when only relative comparison is needed.
        /// </summary>
        public double SquaredDistanceTo(BlockPosition other)
        {
            int dx = X - other.X;
            int dy = Y - other.Y;
            int dz = Z - other.Z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Calculates the Euclidean distance to another position.
        /// </summary>
        public double DistanceTo(BlockPosition other) => Math.Sqrt(SquaredDistanceTo(other));

        public override bool Equals(object? obj) => obj is BlockPosition other && Equals(other);

        public bool Equals(BlockPosition other) => _pos.Equals(other._pos);

        public override int GetHashCode() => _pos.GetHashCode();

        public override string ToString() => _pos.ToString();

        public static bool operator ==(BlockPosition left, BlockPosition right) => left.Equals(right);

        public static bool operator !=(BlockPosition left, BlockPosition right) => !left.Equals(right);

        /// <summary>
        /// Implicitly converts to the API's BlockPos for passing to game methods.
        /// </summary>
        public static implicit operator Vintagestory.API.MathTools.BlockPos(BlockPosition pos) => pos._pos;

        /// <summary>
        /// Implicitly converts from the API's BlockPos for receiving from game methods.
        /// </summary>
        public static implicit operator BlockPosition(Vintagestory.API.MathTools.BlockPos pos) => new(pos.X, pos.Y, pos.Z);
    }
}
