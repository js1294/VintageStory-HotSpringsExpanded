using HotSpringsExpanded.Models;
using HotSpringsExpanded.Tracking;
using HotSpringsExpanded.Utilities;
using Xunit;

namespace HotSpringsExpanded.Tests.Tracking
{
    public class HotSpringTrackerTests
    {
        // --- RegisterHotSpring ---

        [Fact]
        public void Register_ValidTemperature_IncreasesCount()
        {
            // Arrange
            HotSpringTracker tracker = new();
            BlockPosition position = new(10, 20, 30);

            // Act
            tracker.RegisterHotSpring(position, 55);

            // Assert
            Assert.Equal(1, tracker.Count);
        }

        [Fact]
        public void Register_InvalidTemperature_DoesNotTrack()
        {
            // Arrange
            HotSpringTracker tracker = new();
            BlockPosition position = new(10, 20, 30);

            // Act
            tracker.RegisterHotSpring(position, 99);

            // Assert
            Assert.Equal(0, tracker.Count);
        }

        [Fact]
        public void Register_SamePositionTwice_OverwritesExisting()
        {
            // Arrange
            HotSpringTracker tracker = new();
            BlockPosition position = new(10, 20, 30);

            // Act
            tracker.RegisterHotSpring(position, 55);
            tracker.RegisterHotSpring(position, 87);

            // Assert — still only one entry, but with updated temperature.
            Assert.Equal(1, tracker.Count);
            HotSpring? data = tracker.GetHotSpringData(position);
            Assert.NotNull(data);
            Assert.Equal(87, data.Temperature);
        }

        [Theory]
        [InlineData(55, 1, 0.10, 0.05)]
        [InlineData(65, 2, 0.15, 0.075)]
        [InlineData(74, 3, 0.20, 0.10)]
        [InlineData(87, 4, 0.25, 0.125)]
        public void Register_KnownTemperature_SetsCorrectProperties(int temperature, int expectedRadius, double expectedSnowChance, double expectedIceChance)
        {
            // Arrange
            HotSpringTracker tracker = new();
            BlockPosition position = new(0, 0, 0);

            // Act
            tracker.RegisterHotSpring(position, temperature);

            // Assert
            HotSpring? data = tracker.GetHotSpringData(position);
            Assert.NotNull(data);
            Assert.Equal(expectedRadius, data.Radius);
            Assert.Equal(expectedSnowChance, data.BaseSnowMeltingChance, precision: 3);
            Assert.Equal(expectedIceChance, data.BaseIceMeltingChance, precision: 3);
        }

        // --- UnregisterHotSpring ---

        [Fact]
        public void Unregister_ExistingPosition_RemovesEntry()
        {
            // Arrange
            HotSpringTracker tracker = new();
            BlockPosition position = new(5, 5, 5);
            tracker.RegisterHotSpring(position, 74);

            // Act
            tracker.UnregisterHotSpring(position);

            // Assert
            Assert.Equal(0, tracker.Count);
            Assert.Null(tracker.GetHotSpringData(position));
        }

        [Fact]
        public void Unregister_NonExistentPosition_DoesNotThrow()
        {
            // Arrange
            HotSpringTracker tracker = new();
            BlockPosition position = new(99, 99, 99);

            // Act & Assert — should not throw.
            tracker.UnregisterHotSpring(position);
            Assert.Equal(0, tracker.Count);
        }

        // --- GetHotSpringData ---

        [Fact]
        public void GetData_NonExistentPosition_ReturnsNull()
        {
            // Arrange
            HotSpringTracker tracker = new();

            // Act
            HotSpring? result = tracker.GetHotSpringData(new BlockPosition(1, 2, 3));

            // Assert
            Assert.Null(result);
        }

        // --- GetRadiusForTemperature ---

        [Theory]
        [InlineData(55, 1)]
        [InlineData(65, 2)]
        [InlineData(74, 3)]
        [InlineData(87, 4)]
        public void GetRadius_KnownTemperature_ReturnsExpected(int temperature, int expectedRadius)
        {
            // Act
            int radius = HotSpringTracker.GetRadiusForTemperature(temperature);

            // Assert
            Assert.Equal(expectedRadius, radius);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(-1)]
        public void GetRadius_UnknownTemperature_ReturnsDefault(int temperature)
        {
            // Act
            int radius = HotSpringTracker.GetRadiusForTemperature(temperature);

            // Assert — default is 1.
            Assert.Equal(1, radius);
        }
    }
}
