using System.Collections.Generic;
using HotSpringsExpanded.Models;
using HotSpringsExpanded.Utilities;
using Vintagestory.API.Common;

namespace HotSpringsExpanded.Tracking
{
    /// <summary>
    /// Tracks snow/ice blocks that are within melting range of a hot spring.
    /// Each snow block is tracked once, associated with the closest and hottest hot spring.
    /// </summary>
    public class SnowBlockTracker
    {
        /// <summary>
        /// Minimum melting chance threshold. Values below this are treated as zero
        /// to avoid tracking blocks with negligible melt probability.
        /// </summary>
        private const double MinMeltingChanceThreshold = 0.001;

        /// <summary>
        /// Maximum melting chance cap. No block can exceed 90% chance per tick.
        /// </summary>
        private const double MaxMeltingChance = 0.9;

        /// <summary>
        /// Extra blocks beyond the hot spring's base radius where melting still applies.
        /// This creates a gradual falloff zone at the edge of the melting area.
        /// </summary>
        private const double MeltingRadiusExtension = 2.0;

        /// <summary>
        /// Storage for snow block data. Uses an (int, int, int) tuple as key
        /// instead of BlockPosition for faster dictionary lookups (avoids struct boxing).
        /// </summary>
        private readonly Dictionary<(int X, int Y, int Z), SnowBlock> trackedSnow = [];

        /// <summary>
        /// Attempts to start tracking a new snow/ice block.
        /// If the block is already tracked, delegates to update logic which may
        /// reassign it to a closer or hotter hot spring.
        /// </summary>
        /// <param name="position">Position of the snow/ice block.</param>
        /// <param name="block">The block object.</param>
        /// <param name="blockAccessor">Block accessor for world access.</param>
        /// <param name="hotSpringTracker">Hot spring tracker to find closest/hottest hot spring.</param>
        /// <param name="hotSpringPos">Position of the hot spring that triggered this tracking.</param>
        public void TrackSnowBlock(BlockPosition position, Block block, IBlockAccessor? blockAccessor, HotSpringTracker? hotSpringTracker, BlockPosition? hotSpringPos = null)
        {
            if (block == null)
                return;

            bool isIce = BlockIdentifiers.IsIceBlock(block);
            (int X, int Y, int Z) key = (position.X, position.Y, position.Z);

            if (trackedSnow.TryGetValue(key, out SnowBlock? existingData))
            {
                TryUpdateExistingSnowBlock(existingData, position, isIce, hotSpringTracker, hotSpringPos);
                return;
            }

            TryTrackNewSnowBlock(key, position, isIce, hotSpringTracker, hotSpringPos);
        }

        /// <summary>
        /// Attempts to update an existing tracked snow block if the new hot spring
        /// is closer (higher priority) or hotter (tiebreaker at equal distance).
        /// </summary>
        /// <param name="existingData">The existing tracking data.</param>
        /// <param name="position">Position of the snow/ice block.</param>
        /// <param name="isIce">Whether the block is ice.</param>
        /// <param name="hotSpringTracker">Hot spring tracker.</param>
        /// <param name="hotSpringPos">Position of the candidate hot spring.</param>
        private static void TryUpdateExistingSnowBlock(SnowBlock existingData, BlockPosition position, bool isIce, HotSpringTracker? hotSpringTracker, BlockPosition? hotSpringPos)
        {
            if (!hotSpringPos.HasValue || hotSpringTracker == null)
                return;

            double newDistance = position.DistanceTo(hotSpringPos.Value);
            bool shouldUpdate = false;

            // Closer hot spring always wins.
            if (newDistance < existingData.ClosestHotSpringDistance)
            {
                shouldUpdate = true;
            }
            else if (newDistance == existingData.ClosestHotSpringDistance)
            {
                // At equal distance, prefer the hotter spring (higher melting chance).
                HotSpring? newHotSpringData = hotSpringTracker.GetHotSpringData(hotSpringPos.Value);
                if (newHotSpringData != null && newHotSpringData.Temperature > existingData.HotSpringTemperature)
                    shouldUpdate = true;
            }

            if (!shouldUpdate)
                return;

            // Reassign this snow block to the new hot spring.
            existingData.ClosestHotSpringDistance = newDistance;
            HotSpring? hotSpringData = hotSpringTracker.GetHotSpringData(hotSpringPos.Value);
            if (hotSpringData != null)
            {
                existingData.HotSpringTemperature = hotSpringData.Temperature;
                existingData.MeltingChance = CalculateMeltingChance(newDistance, hotSpringData, isIce);
            }
        }

        /// <summary>
        /// Attempts to track a new snow block. Skips if the calculated melting chance
        /// is effectively zero (block is too far from the hot spring).
        /// If no hotSpringPos is provided, searches all tracked hot springs for the closest one.
        /// </summary>
        /// <param name="key">Dictionary key tuple.</param>
        /// <param name="position">Position of the snow/ice block.</param>
        /// <param name="isIce">Whether the block is ice.</param>
        /// <param name="hotSpringTracker">Hot spring tracker.</param>
        /// <param name="hotSpringPos">Position of the hot spring (null to search all).</param>
        private void TryTrackNewSnowBlock((int X, int Y, int Z) key, BlockPosition position, bool isIce, HotSpringTracker? hotSpringTracker, BlockPosition? hotSpringPos)
        {
            if (hotSpringTracker == null)
                return;

            double bestDistance = double.MaxValue;
            int bestTemperature = 0;
            double bestMeltingChance = 0.0;

            if (hotSpringPos.HasValue)
            {
                // Use the specific hot spring provided.
                HotSpring? hotSpringData = hotSpringTracker.GetHotSpringData(hotSpringPos.Value);
                if (hotSpringData != null)
                {
                    bestDistance = position.DistanceTo(hotSpringPos.Value);
                    bestTemperature = hotSpringData.Temperature;
                    bestMeltingChance = CalculateMeltingChance(bestDistance, hotSpringData, isIce);
                }
            }
            else
            {
                // No specific hot spring provided — find the closest one that gives a non-zero chance.
                foreach (HotSpring spring in hotSpringTracker.GetAllHotSprings())
                {
                    double distance = position.DistanceTo(spring.Position);
                    double chance = CalculateMeltingChance(distance, spring, isIce);

                    if (chance > bestMeltingChance || (chance == bestMeltingChance && distance < bestDistance))
                    {
                        bestDistance = distance;
                        bestTemperature = spring.Temperature;
                        bestMeltingChance = chance;
                    }
                }
            }

            // Don't waste memory tracking blocks that will never melt.
            if (bestMeltingChance == 0.0)
                return;

            SnowBlock snowData = new()
            {
                Position = position,
                IsIce = isIce,
                ClosestHotSpringDistance = bestDistance,
                HotSpringTemperature = bestTemperature,
                MeltingChance = bestMeltingChance
            };

            trackedSnow[key] = snowData;
        }

        /// <summary>
        /// Stops tracking a snow/ice block (e.g., after it has fully melted).
        /// </summary>
        /// <param name="position">Position of the snow/ice block.</param>
        public void UntrackSnowBlock(BlockPosition position) => trackedSnow.Remove((position.X, position.Y, position.Z));

        /// <summary>
        /// Gets all tracked snow blocks as a read-only view of the internal collection.
        /// No allocation occurs — callers iterate the dictionary values directly.
        /// </summary>
        /// <returns>The tracked snow block values.</returns>
        public IReadOnlyCollection<SnowBlock> GetTrackedSnowBlocks() => trackedSnow.Values;

        /// <summary>
        /// Calculates the melting chance for a snow block based on its distance to a hot spring.
        /// Uses linear falloff from the base chance at distance 0 to zero at the effective radius.
        /// </summary>
        /// <param name="distance">Euclidean distance to the hot spring.</param>
        /// <param name="hotSpringData">The hot spring data containing radius and base chances.</param>
        /// <param name="isIce">True if the block is ice (uses lower base chance).</param>
        /// <returns>The melting chance (0.0 to 0.9), or 0.0 if below threshold.</returns>
        private static double CalculateMeltingChance(double distance, HotSpring hotSpringData, bool isIce)
        {
            // Blocks directly on the bacteria get the maximum possible chance.
            if (distance == 0)
                return MaxMeltingChance;

            // The effective radius is the base radius plus the extension zone.
            // Beyond this distance, melting chance drops to zero.
            double effectiveRadius = hotSpringData.Radius + MeltingRadiusExtension;
            double distanceRatio = distance / effectiveRadius;

            // Beyond effective radius — no melting.
            if (distanceRatio >= 1.0)
                return 0.0;

            // Ice melts at a lower base rate than snow.
            double baseChance = isIce
                ? hotSpringData.BaseIceMeltingChance
                : hotSpringData.BaseSnowMeltingChance;

            // Linear falloff: full base chance at distance 0, zero at effective radius edge.
            double distanceFactor = 1.0 - distanceRatio;
            double finalChance = baseChance * distanceFactor;

            // Cap at maximum and discard negligible values.
            if (finalChance > MaxMeltingChance)
                finalChance = MaxMeltingChance;

            return finalChance < MinMeltingChanceThreshold ? 0.0 : finalChance;
        }

        /// <summary>
        /// Gets the total count of tracked snow blocks.
        /// </summary>
        public int Count => trackedSnow.Count;
    }
}
