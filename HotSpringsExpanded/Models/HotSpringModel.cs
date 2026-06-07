using HotSpringsExpanded.Utilities;

namespace HotSpringsExpanded.Models
{
    /// <summary>
    /// Data class representing a hot spring with its temperature and melting properties.
    /// </summary>
    public class HotSpring
    {
        /// <summary>
        /// Position of the hot spring bacteria block in the world.
        /// </summary>
        public BlockPosition Position { get; set; }

        /// <summary>
        /// Temperature of the hot spring in degrees Celsius (55, 65, 74, or 87).
        /// </summary>
        public int Temperature { get; set; }

        /// <summary>
        /// Melting radius in blocks, calculated from temperature.
        /// Snow/ice within this radius (plus extension) can be melted.
        /// </summary>
        public int Radius { get; set; }

        /// <summary>
        /// Base melting chance per tick for snow blocks (0.10 for 55°C, up to 0.25 for 87°C).
        /// Actual chance decreases linearly with distance from the hot spring.
        /// </summary>
        public double BaseSnowMeltingChance { get; set; }

        /// <summary>
        /// Base melting chance per tick for ice blocks (0.05 for 55°C, up to 0.125 for 87°C).
        /// Ice melts at half the rate of snow.
        /// </summary>
        public double BaseIceMeltingChance { get; set; }
    }
}
