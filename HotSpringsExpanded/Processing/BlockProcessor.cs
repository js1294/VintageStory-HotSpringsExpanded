using System;
using System.Collections.Generic;
using HotSpringsExpanded.Tracking;
using HotSpringsExpanded.Utilities;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace HotSpringsExpanded.Processing
{
    /// <summary>
    /// Scans the world around players for hot spring bacteria and nearby meltable blocks.
    /// Handles registration, unregistration, and event-driven tracking of hot springs and snow/ice.
    /// </summary>
    public class BlockProcessor
    {
        /// <summary>
        /// Horizontal scan radius around each player (in blocks).
        /// </summary>
        private const int ScanRadiusBlocks = 100;

        /// <summary>
        /// Vertical scan depth above and below each player (in blocks).
        /// </summary>
        private const int ScanDepthBlocks = 10;

        /// <summary>
        /// Interval between melting ticks (milliseconds).
        /// </summary>
        private const int MeltTickIntervalMs = 1000;

        /// <summary>
        /// Initial delay before the first melting tick (milliseconds).
        /// Allows the world to finish loading before processing begins.
        /// </summary>
        private const int MeltTickInitialDelayMs = 5000;

        /// <summary>
        /// Interval between full hot spring scans (milliseconds).
        /// </summary>
        private const int ScanTickIntervalMs = 10000;

        /// <summary>
        /// Initial delay before the first scan (milliseconds).
        /// Allows the world and chunks to fully load before scanning for hot springs.
        /// </summary>
        private const int ScanTickInitialDelayMs = 10000;

        /// <summary>
        /// Extra blocks beyond the hot spring's base radius to include in the snow scan.
        /// Must match the extension used in SnowBlockTracker's melting chance calculation
        /// so that all blocks with a non-zero chance are discovered.
        /// </summary>
        private const int MeltingRadiusExtensionBlocks = 2;

        private ICoreServerAPI? serverApi;
        private HotSpringTracker? hotSpringTracker;
        private SnowBlockTracker? snowBlockTracker;
        private MeltingProcessor? meltingProcessor;

        /// <summary>
        /// Initializes the system with the core API (called on both sides).
        /// Only stores the server API reference; client-side is a no-op.
        /// </summary>
        /// <param name="api">The core API.</param>
        public void Initialize(ICoreAPI api)
        {
            if (api.Side == EnumAppSide.Server)
                serverApi = api as ICoreServerAPI;
        }

        /// <summary>
        /// Initializes server-side systems: trackers, processor, and event handlers.
        /// </summary>
        /// <param name="api">The server API.</param>
        public void InitializeServer(ICoreServerAPI api)
        {
            serverApi = api;
            hotSpringTracker = new();
            snowBlockTracker = new();
            meltingProcessor = new(api, hotSpringTracker, snowBlockTracker);

            RegisterEventHandlers(api);
        }

        /// <summary>
        /// Registers all event handlers for block changes and tick processing.
        /// </summary>
        /// <param name="api">The server API.</param>
        private void RegisterEventHandlers(ICoreServerAPI api)
        {
            api.Event.DidPlaceBlock += (byPlayer, oldblockId, blockSel, withItemStack) =>
                HandleBlockPlaced(byPlayer, oldblockId, blockSel, withItemStack);

            api.Event.DidBreakBlock += (byPlayer, oldblockId, blockSel) =>
                HandleBlockBroken(byPlayer, oldblockId, blockSel);

            // Melt tick runs every second. Delayed to allow the scan to populate tracked blocks first.
            api.Event.RegisterGameTickListener(_ => meltingProcessor!.TickMeltTrackedSnow(), MeltTickIntervalMs, MeltTickInitialDelayMs);

            // Full scan runs every 10 seconds. Delayed on startup to allow chunks to load.
            api.Event.RegisterGameTickListener(_ => ScanForHotSpringsNearPlayers(), ScanTickIntervalMs, ScanTickInitialDelayMs);
        }

        /// <summary>
        /// Scans the world around each online player for hot spring bacteria and nearby snow.
        /// Only scans loaded chunks within the configured radius.
        /// </summary>
        private void ScanForHotSpringsNearPlayers()
        {
            if (serverApi == null)
                return;

            IBlockAccessor blockAccessor = serverApi.World.BlockAccessor;

            foreach (IServerPlayer player in serverApi.Server.Players)
            {
                if (player?.Entity?.Pos == null)
                    continue;

                ScanAroundPlayer(player, blockAccessor);
            }
        }

        /// <summary>
        /// Scans a bounding box around a single player for hot spring bacteria blocks.
        /// The box extends ScanRadiusBlocks horizontally and ScanDepthBlocks vertically.
        /// </summary>
        /// <param name="player">The player to scan around.</param>
        /// <param name="blockAccessor">Block accessor for world access.</param>
        private void ScanAroundPlayer(IServerPlayer player, IBlockAccessor blockAccessor)
        {
            EntityPos pos = player.Entity.Pos;
            int playerX = (int)Math.Round(pos.X);
            int playerY = (int)Math.Round(pos.Y);
            int playerZ = (int)Math.Round(pos.Z);

            int minX = playerX - ScanRadiusBlocks;
            int maxX = playerX + ScanRadiusBlocks;
            int minY = playerY - ScanDepthBlocks;
            int maxY = playerY + ScanDepthBlocks;
            int minZ = playerZ - ScanRadiusBlocks;
            int maxZ = playerZ + ScanRadiusBlocks;

            for (int x = minX; x <= maxX; x++)
                for (int y = minY; y <= maxY; y++)
                    for (int z = minZ; z <= maxZ; z++)
                        CheckBlockForHotSpring(blockAccessor, new BlockPosition(x, y, z));
        }

        /// <summary>
        /// Checks a single block position for hot spring bacteria decorations.
        /// Bacteria are stored as sub-decors on the block, not as the block itself.
        /// If found, registers the hot spring and scans for nearby meltable blocks.
        /// </summary>
        /// <param name="blockAccessor">Block accessor for world access.</param>
        /// <param name="position">The position to check.</param>
        private void CheckBlockForHotSpring(IBlockAccessor blockAccessor, BlockPosition position)
        {
            // Bacteria mats are stored as sub-decorations, not as the main block.
            Dictionary<int, Block>? decors = blockAccessor.GetSubDecors(position);
            if (decors is null || decors.Count == 0)
                return;

            foreach (Block? decor in decors.Values)
            {
                if (decor is null)
                    continue;

                if (!decor.Code.Path.Contains("hotspringbacteria"))
                    continue;

                int temperature = BlockIdentifiers.GetTemperature(decor);
                hotSpringTracker!.RegisterHotSpring(position, temperature);

                // Include the melting extension in the scan radius so all blocks
                // that could have a non-zero melting chance are discovered.
                int springRadius = HotSpringTracker.GetRadiusForTemperature(temperature) + MeltingRadiusExtensionBlocks;
                FindAndTrackSnowNearHotSpring(blockAccessor, position, springRadius);
            }
        }

        /// <summary>
        /// Finds and tracks snow/ice blocks within a cubic radius of a hot spring.
        /// </summary>
        /// <param name="blockAccessor">Block accessor for world access.</param>
        /// <param name="hotSpringPos">Position of the hot spring.</param>
        /// <param name="radius">Search radius (includes melting extension).</param>
        private void FindAndTrackSnowNearHotSpring(IBlockAccessor blockAccessor, BlockPosition hotSpringPos, int radius)
        {
            int minX = hotSpringPos.X - radius;
            int maxX = hotSpringPos.X + radius;
            int minY = hotSpringPos.Y - radius;
            int maxY = hotSpringPos.Y + radius;
            int minZ = hotSpringPos.Z - radius;
            int maxZ = hotSpringPos.Z + radius;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        BlockPosition checkPos = new(x, y, z);
                        Block block = blockAccessor.GetBlock(checkPos);

                        if (BlockIdentifiers.IsMeltableBlock(block))
                            snowBlockTracker!.TrackSnowBlock(checkPos, block, blockAccessor, hotSpringTracker, hotSpringPos);
                    }
                }
            }
        }

        /// <summary>
        /// Handles a block being placed. Registers new hot springs or tracks new snow/ice.
        /// </summary>
        private void HandleBlockPlaced(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel, ItemStack withItemStack)
        {
            Block? block = blockSel?.Block;
            if (block == null)
                return;

            Vintagestory.API.MathTools.BlockPos? pos = blockSel?.Position;
            if (pos == null)
                return;

            BlockPosition blockPos = new(pos.X, pos.Y, pos.Z);

            if (BlockIdentifiers.IsHotBacteria(block))
            {
                int temperature = BlockIdentifiers.GetTemperature(block);
                hotSpringTracker!.RegisterHotSpring(blockPos, temperature);

                // Immediately scan for nearby snow so melting starts without waiting for the next full scan.
                int radius = HotSpringTracker.GetRadiusForTemperature(temperature) + MeltingRadiusExtensionBlocks;
                FindAndTrackSnowNearHotSpring(serverApi!.World.BlockAccessor, blockPos, radius);
            }
            else if (BlockIdentifiers.IsMeltableBlock(block))
            {
                // Snow placed near an existing hot spring should start melting immediately.
                snowBlockTracker!.TrackSnowBlock(blockPos, block, serverApi!.World.BlockAccessor, hotSpringTracker);
            }
        }

        /// <summary>
        /// Handles a block being broken. Unregisters hot springs or untracks snow/ice.
        /// </summary>
        private void HandleBlockBroken(IServerPlayer byPlayer, int oldblockId, BlockSelection blockSel)
        {
            Block? block = blockSel?.Block;
            if (block == null)
                return;

            Vintagestory.API.MathTools.BlockPos? pos = blockSel?.Position;
            if (pos == null)
                return;

            BlockPosition blockPos = new(pos.X, pos.Y, pos.Z);

            if (BlockIdentifiers.IsHotBacteria(block))
                hotSpringTracker!.UnregisterHotSpring(blockPos);
            else if (BlockIdentifiers.IsMeltableBlock(block))
                snowBlockTracker!.UntrackSnowBlock(blockPos);
        }

        /// <summary>
        /// Gets the hot spring tracker (for testing).
        /// </summary>
        public HotSpringTracker? HotSpringTracker => hotSpringTracker;

        /// <summary>
        /// Gets the snow block tracker (for testing).
        /// </summary>
        public SnowBlockTracker? SnowBlockTracker => snowBlockTracker;

        /// <summary>
        /// Gets the melting processor (for testing).
        /// </summary>
        public MeltingProcessor? MeltingProcessor => meltingProcessor;
    }
}
