using HotSpringsExpanded.Utilities;

namespace HotSpringsExpanded.Models
{
    /// <summary>
    /// Data class representing a tracked snow/ice block.
    /// Each snow block is tracked once, associated with the closest and hottest hot spring.
    /// </summary>
    public class SnowBlock
    {
        /// <summary>
        /// Position of the snow/ice block in the world.
        /// </summary>
        public BlockPosition Position { get; set; }

        /// <summary>
        /// True if this is an ice block, false if it's a snow block or snow layer.
        /// </summary>
        public bool IsIce { get; set; }

        /// <summary>
        /// Euclidean distance to the closest hot spring (0 when directly on bacteria).
        /// Used to determine priority when multiple hot springs overlap.
        /// </summary>
        public double ClosestHotSpringDistance { get; set; }

        /// <summary>
        /// Temperature of the closest/hottest hot spring affecting this block.
        /// Used as a tiebreaker when two hot springs are equidistant.
        /// </summary>
        public int HotSpringTemperature { get; set; }

        /// <summary>
        /// Pre-calculated melting chance per tick based on distance and hot spring temperature.
        /// Ranges from 0.0 (no chance) to 0.9 (maximum cap).
        /// </summary>
        public double MeltingChance { get; set; }
    }
}
