using System;
using System.Collections.Generic;
using mapsnap;
using mapsnap.Projects;
using Xunit;

namespace mapsnapTests.UnitTests;

public class PixelOffsetsTests
{
    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255)]
    [InlineData(0, 255, 0, 255)]
    [InlineData(255, 0, 255, 0)]
    [InlineData(255, 0, 0, 255)]
    [InlineData(0, 255, 255, 0)]
    [InlineData(40, 20, 56, 21)]
    public void ValuesInRangeAccepted(int x1, int y1, int x2, int y2)
    {
        // Implicitly testing that the constructor does not throw an ArgumentOutOfRangeException at values that should be accepted.
        _ = new PixelOffsets(x1, x2, y1, y2);
    }
    
    [Theory]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(144, 89, 23, 256)]
    [InlineData(-5, 89, 23, 256)]
    [InlineData(int.MaxValue, int.MinValue, int.MinValue, int.MaxValue)]
    public void ValuesOutOfRangeRejected(int x1, int y1, int x2, int y2)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new PixelOffsets(x1, x2, y1, y2));
    }

    [Fact]
    public void ValuesRemainUnchanged()
    {
        var offsets = new PixelOffsets(28, 70, 144, 53);
        
        Assert.Equal(28, offsets.top);
        Assert.Equal(70, offsets.right);
        Assert.Equal(144, offsets.bottom);
        Assert.Equal(53, offsets.left);
    }
    
    public static IEnumerable<object[]> GetPixelOffsetData()
    {
        const int zoom = 14;
        const double lat1 = 42.6814;
        const double lon1 = 21.1362;
        const double lat2 = 42.6357;
        const double lon2 = 21.2052;

        var coords1 = new Coordinates(lat1, lon1);
        var coords2 = new Coordinates(lat2, lon2);
        var coords3 = new Coordinates(lat1, lon2);
        var coords4 = new Coordinates(lat2, lon1);

        var topleftPixel = Tiles.CoordinatesToTilePixel(coords1, zoom);
        var bottomRightPixel = Tiles.CoordinatesToTilePixel(coords2, zoom);

        yield return new object[] { coords1, coords2, zoom, new PixelOffsets(topleftPixel, bottomRightPixel) };
        yield return new object[] { coords2, coords1, zoom, new PixelOffsets(topleftPixel, bottomRightPixel) };
        yield return new object[] { coords3, coords4, zoom, new PixelOffsets(topleftPixel, bottomRightPixel) };
        yield return new object[] { coords4, coords3, zoom, new PixelOffsets(topleftPixel, bottomRightPixel) };
    }

    [Theory]
    [MemberData(nameof(GetPixelOffsetData))]
    public void CoordinateConstructorInvariantOrder(Coordinates coordA, Coordinates coordB, int zoom, PixelOffsets expectedOffsets)
    {
        var actualOffsets = new PixelOffsets(coordA, coordB, zoom);
        Assert.Equal(expectedOffsets, actualOffsets);
    }
}
