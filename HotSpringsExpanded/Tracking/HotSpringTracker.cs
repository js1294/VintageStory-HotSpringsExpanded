using System.Collections.Generic;
using HotSpringsExpanded.Models;
using HotSpringsExpanded.Utilities;

namespace HotSpringsExpanded.Tracking
{
    /// <summary>
    /// Tracks hot spring bacterial blocks and their melting properties.
    /// Provides registration, lookup, and radius calculation for tracked hot springs.
    /// </summary>
    public class HotSpringTracker
    {
        /// <summary>
        /// Maps each known bacteria temperature to its gameplay properties.
        /// Radius determines how far the melting effect reaches.
        /// SnowChance/IceChance are the base per-tick melting probabilities at distance 0.
        /// </summary>
        private static readonly Dictionary<int, (int Radius, float SnowChance, float IceChance)> TemperatureMapping = new()
        {
            { 55, (1, 0.10f, 0.05f) },
            { 65, (2, 0.15f, 0.075f) },
            { 74, (3, 0.20f, 0.10f) },
            { 87, (4, 0.25f, 0.125f) }
        };

        /// <summary>
        /// Storage for hot spring data keyed by block position.
        /// </summary>
        private readonly Dictionary<BlockPosition, HotSpring> hotSprings = new();

        /// <summary>
        /// Registers a hot spring for tracking.
        /// If a hot spring already exists at this position, it is overwritten.
        /// </summary>
        /// <param name="position">Position of the hot spring bacteria block.</param>
        /// <param name="temperature">Temperature of the hot spring (55, 65, 74, or 87).</param>
        public void RegisterHotSpring(BlockPosition position, int temperature)
        {
            // Ignore unknown temperatures that don't have a mapping entry.
            if (!TemperatureMapping.TryGetValue(temperature, out (int Radius, float SnowChance, float IceChance) mapping))
                return;

            HotSpring hotSpringData = new()
            {
                Position = position,
                Temperature = temperature,
                Radius = mapping.Radius,
                BaseSnowMeltingChance = mapping.SnowChance,
                BaseIceMeltingChance = mapping.IceChance
            };

            hotSprings[position] = hotSpringData;
        }

        /// <summary>
        /// Unregisters a hot spring from tracking.
        /// </summary>
        /// <param name="position">Position of the hot spring bacteria block.</param>
        public void UnregisterHotSpring(BlockPosition position) => hotSprings.Remove(position);

        /// <summary>
        /// Gets the hot spring data for a given position.
        /// </summary>
        /// <param name="position">Position of the hot spring.</param>
        /// <returns>The HotSpring data if found, null otherwise.</returns>
        public HotSpring? GetHotSpringData(BlockPosition position) =>
            hotSprings.TryGetValue(position, out HotSpring? data) ? data : null;

        /// <summary>
        /// Gets all tracked hot springs as a new list.
        /// </summary>
        /// <returns>A list of all tracked hot springs.</returns>
        public List<HotSpring> GetAllHotSprings() => new(hotSprings.Values);

        /// <summary>
        /// Gets the melting radius for a given temperature from the authoritative mapping.
        /// This is the single source of truth for temperature-to-radius conversion.
        /// </summary>
        /// <param name="temperature">The temperature (55, 65, 74, or 87).</param>
        /// <returns>The melting radius in blocks, or 1 if temperature is unknown.</returns>
        public static int GetRadiusForTemperature(int temperature)
        {
            if (TemperatureMapping.TryGetValue(temperature, out (int Radius, float SnowChance, float IceChance) mapping))
                return mapping.Radius;

            return 1;
        }

        /// <summary>
        /// Gets the total count of tracked hot springs.
        /// </summary>
        public int Count => hotSprings.Count;
    }
}
