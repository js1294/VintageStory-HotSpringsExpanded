using HotSpringsExpanded.Utilities;
using Vintagestory.API.Common;
using Xunit;

namespace HotSpringsExpanded.Tests.Utilities
{
    public class BlockIdentifiersTests
    {
        // --- IsHotBacteria (string overload) ---

        [Theory]
        [InlineData("game:hotspringbacteriasmooth-55deg", true)]
        [InlineData("game:hotspringbacteria-87deg", true)]
        [InlineData("game:snow", false)]
        [InlineData("game:stone", false)]
        [InlineData("", false)]
        public void IsHotBacteria_GivenBlockCode_ReturnsExpected(string blockCode, bool expected)
        {
            // Act
            bool result = BlockIdentifiers.IsHotBacteria(blockCode);

            // Assert
            Assert.Equal(expected, result);
        }

        // --- IsMeltableBlock ---

        [Fact]
        public void IsMeltableBlock_NullBlock_ReturnsFalse()
        {
            // Act
            bool result = BlockIdentifiers.IsMeltableBlock(null);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("snowblock", true)]
        [InlineData("snowlayer-1", true)]
        [InlineData("snowlayer-8", true)]
        [InlineData("ice", true)]
        [InlineData("lakeice", true)]
        [InlineData("glacierice", true)]
        [InlineData("stone", false)]
        [InlineData("dirt", false)]
        [InlineData("snowberry", false)]
        [InlineData("device", false)]
        public void IsMeltableBlock_GivenPath_ReturnsExpected(string path, bool expected)
        {
            // Arrange
            Block block = CreateBlockWithPath(path);

            // Act
            bool result = BlockIdentifiers.IsMeltableBlock(block);

            // Assert
            Assert.Equal(expected, result);
        }

        // --- IsIceBlock ---

        [Fact]
        public void IsIceBlock_NullBlock_ReturnsFalse()
        {
            // Act
            bool result = BlockIdentifiers.IsIceBlock(null);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("ice", true)]
        [InlineData("lakeice", true)]
        [InlineData("glacierice", true)]
        [InlineData("snowblock", false)]
        [InlineData("snowlayer-3", false)]
        public void IsIceBlock_GivenPath_ReturnsExpected(string path, bool expected)
        {
            // Arrange
            Block block = CreateBlockWithPath(path);

            // Act
            bool result = BlockIdentifiers.IsIceBlock(block);

            // Assert
            Assert.Equal(expected, result);
        }

        // --- GetTemperature ---

        [Fact]
        public void GetTemperature_NullBlock_ReturnsNegativeOne()
        {
            // Act
            int result = BlockIdentifiers.GetTemperature(null);

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void GetTemperature_NonBacteriaBlock_ReturnsNegativeOne()
        {
            // Arrange
            Block block = CreateBlockWithFullCode("game:stone");

            // Act
            int result = BlockIdentifiers.GetTemperature(block);

            // Assert
            Assert.Equal(-1, result);
        }

        [Theory]
        [InlineData("game:hotspringbacteriasmooth-55deg", 55)]
        [InlineData("game:hotspringbacteriasmooth-65deg", 65)]
        [InlineData("game:hotspringbacteriasmooth-74deg", 74)]
        [InlineData("game:hotspringbacteria-87deg", 87)]
        public void GetTemperature_BacteriaBlock_ReturnsTemperature(string fullCode, int expectedTemperature)
        {
            // Arrange
            Block block = CreateBlockWithFullCode(fullCode);

            // Act
            int result = BlockIdentifiers.GetTemperature(block);

            // Assert
            Assert.Equal(expectedTemperature, result);
        }

        // --- Helper methods ---

        private static Block CreateBlockWithPath(string path) => new FakeBlock(path);

        private static Block CreateBlockWithFullCode(string fullCode)
        {
            string[] parts = fullCode.Split(':');
            return new FakeBlock(parts[0], parts[1]);
        }
    }
}
