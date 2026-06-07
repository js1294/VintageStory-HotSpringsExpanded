using HotSpringsExpanded.Models;
using HotSpringsExpanded.Tracking;
using HotSpringsExpanded.Utilities;
using Vintagestory.API.Common;
using Xunit;

namespace HotSpringsExpanded.Tests.Tracking
{
    public class SnowBlockTrackerTests
    {
        private const int Temperature87 = 87;
        private const double MaxMeltingChance = 0.9;

        // --- TrackSnowBlock ---

        [Fact]
        public void Track_NullBlock_DoesNotTrack()
        {
            // Arrange
            SnowBlockTracker tracker = new();
            BlockPosition position = new(0, 0, 0);

            // Act
            tracker.TrackSnowBlock(position, null!, null, null);

            // Assert
            Assert.Equal(0, tracker.Count);
        }

        [Fact]
        public void Track_SnowInRange_IncreasesCount()
        {
            // Arrange
            SnowBlockTracker snowTracker = new();
            HotSpringTracker hotSpringTracker = CreateTrackerWithHotSpring(new BlockPosition(0, 0, 0), Temperature87);
            Block snowBlock = CreateBlockWithPath("snowblock");
            BlockPosition snowPos = new(1, 0, 0);

            // Act
            snowTracker.TrackSnowBlock(snowPos, snowBlock, null, hotSpringTracker, new BlockPosition(0, 0, 0));

            // Assert
            Assert.Equal(1, snowTracker.Count);
        }

        [Fact]
        public void Track_SnowOutOfRange_DoesNotTrack()
        {
            // Arrange — place snow far beyond the effective radius (4 + 2 = 6 blocks).
            SnowBlockTracker snowTracker = new();
            HotSpringTracker hotSpringTracker = CreateTrackerWithHotSpring(new BlockPosition(0, 0, 0), 55);
            Block snowBlock = CreateBlockWithPath("snowblock");
            BlockPosition farPos = new(100, 0, 0);

            // Act
            snowTracker.TrackSnowBlock(farPos, snowBlock, null, hotSpringTracker, new BlockPosition(0, 0, 0));

            // Assert — too far, melting chance is 0, so not tracked.
            Assert.Equal(0, snowTracker.Count);
        }

        [Fact]
        public void Track_IceBlock_SetsIsIceTrue()
        {
            // Arrange
            SnowBlockTracker snowTracker = new();
            HotSpringTracker hotSpringTracker = CreateTrackerWithHotSpring(new BlockPosition(0, 0, 0), Temperature87);
            Block iceBlock = CreateBlockWithPath("ice");
            BlockPosition icePos = new(1, 0, 0);

            // Act
            snowTracker.TrackSnowBlock(icePos, iceBlock, null, hotSpringTracker, new BlockPosition(0, 0, 0));

            // Assert
            SnowBlock tracked = snowTracker.GetTrackedSnowBlocks().First();
            Assert.True(tracked.IsIce);
        }

        [Fact]
        public void Track_CloserSpring_UpdatesEntry()
        {
            // Arrange — track snow near a far hot spring, then re-track near a closer one.
            SnowBlockTracker snowTracker = new();
            BlockPosition farSpring = new(5, 0, 0);
            BlockPosition closeSpring = new(1, 0, 0);
            BlockPosition snowPos = new(2, 0, 0);

            HotSpringTracker hotSpringTracker = new();
            hotSpringTracker.RegisterHotSpring(farSpring, 55);
            hotSpringTracker.RegisterHotSpring(closeSpring, Temperature87);

            Block snowBlock = CreateBlockWithPath("snowblock");

            // Act — first track with far spring, then with close spring.
            snowTracker.TrackSnowBlock(snowPos, snowBlock, null, hotSpringTracker, farSpring);
            snowTracker.TrackSnowBlock(snowPos, snowBlock, null, hotSpringTracker, closeSpring);

            // Assert — still one entry, but updated to the closer spring.
            Assert.Equal(1, snowTracker.Count);
            SnowBlock tracked = snowTracker.GetTrackedSnowBlocks().First();
            Assert.Equal(Temperature87, tracked.HotSpringTemperature);
        }

        [Fact]
        public void Track_EqualDistanceHotterSpring_UpdatesEntry()
        {
            // Arrange — two springs equidistant from the snow block, but at different temperatures.
            // The hotter spring should win the tiebreaker.
            SnowBlockTracker snowTracker = new();
            BlockPosition coldSpring = new(3, 0, 0);
            BlockPosition hotSpring = new(0, 3, 0);
            BlockPosition snowPos = new(0, 0, 0);

            HotSpringTracker hotSpringTracker = new();
            hotSpringTracker.RegisterHotSpring(coldSpring, 55);
            hotSpringTracker.RegisterHotSpring(hotSpring, Temperature87);

            Block snowBlock = CreateBlockWithPath("snowblock");

            // Act — first track with cold spring, then with equidistant hot spring.
            snowTracker.TrackSnowBlock(snowPos, snowBlock, null, hotSpringTracker, coldSpring);
            snowTracker.TrackSnowBlock(snowPos, snowBlock, null, hotSpringTracker, hotSpring);

            // Assert — updated to the hotter spring.
            Assert.Equal(1, snowTracker.Count);
            SnowBlock tracked = snowTracker.GetTrackedSnowBlocks().First();
            Assert.Equal(Temperature87, tracked.HotSpringTemperature);
        }

        // --- UntrackSnowBlock ---

        [Fact]
        public void Untrack_TrackedPosition_RemovesEntry()
        {
            // Arrange
            SnowBlockTracker snowTracker = new();
            HotSpringTracker hotSpringTracker = CreateTrackerWithHotSpring(new BlockPosition(0, 0, 0), Temperature87);
            Block snowBlock = CreateBlockWithPath("snowblock");
            BlockPosition snowPos = new(1, 0, 0);
            snowTracker.TrackSnowBlock(snowPos, snowBlock, null, hotSpringTracker, new BlockPosition(0, 0, 0));

            // Act
            snowTracker.UntrackSnowBlock(snowPos);

            // Assert
            Assert.Equal(0, snowTracker.Count);
        }

        // --- GetTrackedSnowBlocks ---

        [Fact]
        public void GetTracked_EmptyTracker_ReturnsEmpty()
        {
            // Arrange
            SnowBlockTracker tracker = new();

            // Act
            IReadOnlyCollection<SnowBlock> result = tracker.GetTrackedSnowBlocks();

            // Assert
            Assert.Empty(result);
        }

        // --- MeltingChance calculation ---

        [Fact]
        public void MeltingChance_AtDistanceZero_ReturnsMax()
        {
            // Arrange — snow at the exact same position as the hot spring.
            SnowBlockTracker snowTracker = new();
            BlockPosition springPos = new(5, 5, 5);
            HotSpringTracker hotSpringTracker = CreateTrackerWithHotSpring(springPos, Temperature87);
            Block snowBlock = CreateBlockWithPath("snowlayer-3");

            // Act
            snowTracker.TrackSnowBlock(springPos, snowBlock, null, hotSpringTracker, springPos);

            // Assert
            SnowBlock tracked = snowTracker.GetTrackedSnowBlocks().First();
            Assert.Equal(MaxMeltingChance, tracked.MeltingChance);
        }

        [Fact]
        public void MeltingChance_AtHalfRadius_ReturnsHalfBase()
        {
            // Arrange — temperature 87 has radius 4, effective radius = 4 + 2 = 6.
            // Place snow at distance 3 (half of 6) so distanceFactor = 0.5.
            // Expected: 0.25 (base snow chance) * 0.5 = 0.125.
            SnowBlockTracker snowTracker = new();
            BlockPosition springPos = new(0, 0, 0);
            HotSpringTracker hotSpringTracker = CreateTrackerWithHotSpring(springPos, Temperature87);
            Block snowBlock = CreateBlockWithPath("snowblock");
            BlockPosition snowPos = new(3, 0, 0);

            // Act
            snowTracker.TrackSnowBlock(snowPos, snowBlock, null, hotSpringTracker, springPos);

            // Assert
            SnowBlock tracked = snowTracker.GetTrackedSnowBlocks().First();
            Assert.Equal(0.125, tracked.MeltingChance, precision: 3);
        }

        [Fact]
        public void MeltingChance_IceVsSnow_IceIsHalf()
        {
            // Arrange — at distance 3, effective radius 6:
            // Snow: 0.25 * 0.5 = 0.125, Ice: 0.125 * 0.5 = 0.0625.
            SnowBlockTracker snowTracker = new();
            BlockPosition springPos = new(0, 0, 0);
            HotSpringTracker hotSpringTracker = CreateTrackerWithHotSpring(springPos, Temperature87);
            Block snowBlock = CreateBlockWithPath("snowblock");
            Block iceBlock = CreateBlockWithPath("ice");
            BlockPosition snowPos = new(3, 0, 0);
            BlockPosition icePos = new(0, 3, 0);

            // Act
            snowTracker.TrackSnowBlock(snowPos, snowBlock, null, hotSpringTracker, springPos);
            snowTracker.TrackSnowBlock(icePos, iceBlock, null, hotSpringTracker, springPos);

            // Assert
            SnowBlock snowData = snowTracker.GetTrackedSnowBlocks().First(d => !d.IsIce);
            SnowBlock iceData = snowTracker.GetTrackedSnowBlocks().First(d => d.IsIce);
            Assert.Equal(0.125, snowData.MeltingChance, precision: 3);
            Assert.Equal(0.0625, iceData.MeltingChance, precision: 4);
        }

        [Fact]
        public void MeltingChance_NearEdgeOfRadius_ReturnsSmallNonZero()
        {
            // Arrange — temperature 87, effective radius = 6.
            // Place snow at distance 5 (just inside). distanceRatio = 5/6 ≈ 0.833.
            // distanceFactor = 1 - 0.833 = 0.167. Expected: 0.25 * 0.167 ≈ 0.0417.
            SnowBlockTracker snowTracker = new();
            BlockPosition springPos = new(0, 0, 0);
            HotSpringTracker hotSpringTracker = CreateTrackerWithHotSpring(springPos, Temperature87);
            Block snowBlock = CreateBlockWithPath("snowblock");
            BlockPosition snowPos = new(5, 0, 0);

            // Act
            snowTracker.TrackSnowBlock(snowPos, snowBlock, null, hotSpringTracker, springPos);

            // Assert — chance is small but non-zero.
            SnowBlock tracked = snowTracker.GetTrackedSnowBlocks().First();
            Assert.True(tracked.MeltingChance > 0.0);
            Assert.True(tracked.MeltingChance < 0.1);
        }

        // --- Helper methods ---

        private static HotSpringTracker CreateTrackerWithHotSpring(BlockPosition position, int temperature)
        {
            HotSpringTracker tracker = new();
            tracker.RegisterHotSpring(position, temperature);
            return tracker;
        }

        private static Block CreateBlockWithPath(string path) => new FakeBlock(path);
    }
}
