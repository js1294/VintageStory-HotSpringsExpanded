using System;
using System.Collections.Generic;
using HotSpringsExpanded.Models;
using HotSpringsExpanded.Tracking;
using HotSpringsExpanded.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace HotSpringsExpanded.Processing
{
    /// <summary>
    /// Processes hot spring melting on a tick basis.
    /// Iterates all tracked snow/ice blocks and probabilistically melts them.
    /// </summary>
    public class MeltingProcessor
    {
        /// <summary>
        /// Maximum melting chance cap (90%). Prevents guaranteed melting even at distance 0.
        /// </summary>
        private const double MaxMeltChance = 0.9;

        /// <summary>
        /// Minimum random multiplier applied to melting chance.
        /// Combined with RandomMultiplierRange, produces a 0.9–1.1 variance per tick.
        /// </summary>
        private const double RandomMultiplierMin = 0.9;

        /// <summary>
        /// Range of the random multiplier (added to min).
        /// </summary>
        private const double RandomMultiplierRange = 0.2;

        private readonly ICoreServerAPI api;
        private readonly HotSpringTracker hotSpringTracker;
        private readonly SnowBlockTracker snowBlockTracker;

        /// <summary>
        /// Initializes a new instance of the MeltingProcessor class.
        /// </summary>
        /// <param name="api">The server API.</param>
        /// <param name="hotSpringTracker">The hot spring tracker.</param>
        /// <param name="snowBlockTracker">The snow block tracker.</param>
        public MeltingProcessor(ICoreServerAPI api, HotSpringTracker hotSpringTracker, SnowBlockTracker snowBlockTracker)
        {
            this.api = api;
            this.hotSpringTracker = hotSpringTracker;
            this.snowBlockTracker = snowBlockTracker;
        }

        /// <summary>
        /// Processes melting for all tracked snow blocks.
        /// Each block rolls against its melting chance with a small random variance (0.9x–1.1x).
        /// Blocks that fully melt are collected and untracked after iteration completes.
        /// </summary>
        public void TickMeltTrackedSnow()
        {
            IReadOnlyCollection<SnowBlock> trackedSnow = snowBlockTracker.GetTrackedSnowBlocks();

            // Cannot modify the collection during enumeration, so collect melted positions.
            List<BlockPosition>? toUntrack = null;

            foreach (SnowBlock snowData in trackedSnow)
            {
                if (snowData.MeltingChance == 0.0)
                    continue;

                // Apply a small random variance so melting doesn't feel perfectly uniform.
                double randomMultiplier = RandomMultiplierMin + (api.World.Rand.NextDouble() * RandomMultiplierRange);
                double adjustedChance = Math.Min(snowData.MeltingChance * randomMultiplier, MaxMeltChance);

                if (TryMeltBlock(snowData.Position, adjustedChance, snowData.IsIce))
                {
                    toUntrack ??= new();
                    toUntrack.Add(snowData.Position);
                }
            }

            // Remove fully melted blocks from tracking.
            if (toUntrack != null)
                foreach (BlockPosition pos in toUntrack)
                    snowBlockTracker.UntrackSnowBlock(pos);
        }

        /// <summary>
        /// Attempts to melt a snow/ice block based on the given chance.
        /// Rolls a random number and only proceeds if the roll succeeds.
        /// </summary>
        /// <param name="position">Position of the block.</param>
        /// <param name="meltingChance">The melting chance (0.0 to 0.9).</param>
        /// <param name="isIce">True if the block is ice.</param>
        /// <returns>True if the block was fully melted and should be untracked.</returns>
        private bool TryMeltBlock(BlockPosition position, double meltingChance, bool isIce)
        {
            // Random roll — most ticks will fail for low-chance blocks.
            if (api.World.Rand.NextDouble() >= meltingChance)
                return false;

            IBlockAccessor blockAccessor = api.World.BlockAccessor;
            Block block = blockAccessor.GetBlock(position);

            // If the block is no longer meltable (removed by weather or another system), untrack it.
            if (!BlockIdentifiers.IsMeltableBlock(block))
                return true;

            if (isIce)
                return MeltIceBlock(position, blockAccessor);

            int newLevel = DecrementSnowLayer(position, block, blockAccessor);

            // Level 0 means the block was completely removed.
            return newLevel == 0;
        }

        /// <summary>
        /// Melts an ice block by breaking it without drops.
        /// Uses BreakBlock for proper layer cleanup (ice sits above water layer).
        /// The dropQuantityMultiplier of 0 suppresses item drops.
        /// </summary>
        /// <param name="position">Position of the ice block.</param>
        /// <param name="blockAccessor">Block accessor for world access.</param>
        /// <returns>True if the ice was successfully removed.</returns>
        private bool MeltIceBlock(BlockPosition position, IBlockAccessor blockAccessor)
        {
            api.World.BlockAccessor.BreakBlock(position, null, 0f);
            return true;
        }

        /// <summary>
        /// Reduces the snow level of a block by one layer.
        /// If the block is at level 1 or the replacement block can't be found,
        /// the block is removed entirely (set to air).
        /// After changing the block, notifies neighbors so blocks above can fall.
        /// </summary>
        /// <param name="position">Position of the block.</param>
        /// <param name="block">The current block at this position.</param>
        /// <param name="blockAccessor">Block accessor for world access.</param>
        /// <returns>The new snow level (0 if block was removed).</returns>
        private int DecrementSnowLayer(BlockPosition position, Block block, IBlockAccessor blockAccessor)
        {
            int currentLevel = GetSnowLevel(block);

            // If the block is no longer snow (already melted by another system), do nothing.
            if (currentLevel == 0)
                return 0;

            // At minimum level, remove the block entirely.
            if (currentLevel <= 1)
            {
                blockAccessor.SetBlock(0, position);
                blockAccessor.MarkBlockDirty(position);
                blockAccessor.TriggerNeighbourBlockUpdate(position);
                return 0;
            }

            int newLevel = currentLevel - 1;
            Block? newBlock = GetSnowLayerBlock(newLevel);

            // If the target snow layer block doesn't exist in the registry, don't change anything.
            if (newBlock == null)
            {
                api.Logger.Warning($"Cannot decrement snow at {position}: snowlayer-{newLevel} not found in block registry.");
                return currentLevel;
            }

            blockAccessor.SetBlock(newBlock.Id, position);
            blockAccessor.MarkBlockDirty(position);
            blockAccessor.TriggerNeighbourBlockUpdate(position);
            return newLevel;
        }

        /// <summary>
        /// Gets the snow level of a block by parsing its code path.
        /// Full snow blocks are level 8. Snow layers encode their level in the path suffix.
        /// </summary>
        /// <param name="block">The block to check.</param>
        /// <returns>The snow level (1-8), or 0 if not a snow block.</returns>
        private int GetSnowLevel(Block block)
        {
            if (block == null)
                return 0;

            string path = block.Code.Path;

            // Full snow block is always level 8.
            if (path == BlockIdentifiers.Blocks.SnowBlock)
                return 8;

            if (path.StartsWith(BlockIdentifiers.Blocks.SnowLayerPrefix))
            {
                // Extract the number after "snowlayer-" (e.g., "snowlayer-3" → 3).
                string levelStr = path.Substring(BlockIdentifiers.Blocks.SnowLayerPrefix.Length);
                if (int.TryParse(levelStr, out int level))
                    return level;

                // Fallback if parsing fails — treat as minimum layer.
                api.Logger.Warning($"Could not parse snow level from block path '{path}'.");
                return 1;
            }

            // Log unexpected block codes to help diagnose issues.
            return 0;
        }

        /// <summary>
        /// Resolves a snow layer block by level number.
        /// Looks up the block by constructing its asset location with the "game" domain.
        /// </summary>
        /// <param name="level">The snow level (1-8).</param>
        /// <returns>The snow layer block, or null if not found in the registry.</returns>
        private Block? GetSnowLayerBlock(int level)
        {
            if (level >= 8)
                return api.World.GetBlock(new AssetLocation("game", BlockIdentifiers.Blocks.SnowBlock));

            if (level < 1)
                return null;

            return api.World.GetBlock(new AssetLocation("game", $"{BlockIdentifiers.Blocks.SnowLayerPrefix}{level}"));
        }
    }
}
