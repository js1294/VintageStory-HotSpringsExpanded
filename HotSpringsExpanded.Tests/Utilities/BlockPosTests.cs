using HotSpringsExpanded.Utilities;
using Xunit;

namespace HotSpringsExpanded.Tests.Utilities
{
    public class BlockPosTests
    {
        [Fact]
        public void Constructor_ValidCoordinates_SetsProperties()
        {
            // Act
            BlockPosition pos = new(1, 2, 3);

            // Assert
            Assert.Equal(1, pos.X);
            Assert.Equal(2, pos.Y);
            Assert.Equal(3, pos.Z);
        }

        [Fact]
        public void DistanceTo_SamePosition_ReturnsZero()
        {
            // Arrange
            BlockPosition a = new(5, 5, 5);
            BlockPosition b = new(5, 5, 5);

            // Act
            double distance = a.DistanceTo(b);

            // Assert
            Assert.Equal(0.0, distance);
        }

        [Fact]
        public void DistanceTo_KnownOffset_ReturnsCorrectValue()
        {
            // Arrange — distance from (0,0,0) to (3,4,0) should be 5.
            BlockPosition a = new(0, 0, 0);
            BlockPosition b = new(3, 4, 0);

            // Act
            double distance = a.DistanceTo(b);

            // Assert
            Assert.Equal(5.0, distance, precision: 10);
        }

        [Fact]
        public void SquaredDistanceTo_KnownOffset_ReturnsCorrectValue()
        {
            // Arrange — squared distance from (0,0,0) to (1,2,3) = 1+4+9 = 14.
            BlockPosition a = new(0, 0, 0);
            BlockPosition b = new(1, 2, 3);

            // Act
            double squaredDistance = a.SquaredDistanceTo(b);

            // Assert
            Assert.Equal(14.0, squaredDistance);
        }

        [Fact]
        public void Equals_SameCoordinates_ReturnsTrue()
        {
            // Arrange
            BlockPosition a = new(10, 20, 30);
            BlockPosition b = new(10, 20, 30);

            // Act & Assert
            Assert.Equal(a, b);
            Assert.True(a == b);
        }

        [Fact]
        public void Equals_DifferentCoordinates_ReturnsFalse()
        {
            // Arrange
            BlockPosition a = new(1, 2, 3);
            BlockPosition b = new(4, 5, 6);

            // Act & Assert
            Assert.NotEqual(a, b);
            Assert.True(a != b);
        }

        [Fact]
        public void GetHashCode_SameCoordinates_ReturnsSameValue()
        {
            // Arrange
            BlockPosition a = new(7, 8, 9);
            BlockPosition b = new(7, 8, 9);

            // Act & Assert
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void ImplicitToApi_ValidPos_PreservesCoordinates()
        {
            // Arrange
            BlockPosition pos = new(11, 22, 33);

            // Act — implicit conversion to the API type.
            Vintagestory.API.MathTools.BlockPos apiPos = pos;

            // Assert
            Assert.Equal(11, apiPos.X);
            Assert.Equal(22, apiPos.Y);
            Assert.Equal(33, apiPos.Z);
        }

        [Fact]
        public void ImplicitFromApi_ValidPos_PreservesCoordinates()
        {
            // Arrange
            Vintagestory.API.MathTools.BlockPos apiPos = new(44, 55, 66);

            // Act — implicit conversion from the API type.
            BlockPosition pos = apiPos;

            // Assert
            Assert.Equal(44, pos.X);
            Assert.Equal(55, pos.Y);
            Assert.Equal(66, pos.Z);
        }
    }
}
