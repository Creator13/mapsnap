using System.Collections.Generic;
using System.Linq;
using mapsnap;
using Xunit;

namespace mapsnapTests.UnitTests;

public class BoundingBoxTests
{
    public static IEnumerable<object[]> OriginTestData => new List<object[]> {
        new object[] {
            new BoundingBox((3, 3), (20, 20)),
            (3U, 3U)
        },
        new object[] {
            new BoundingBox((20, 20), (3, 3)),
            (3U, 3U)
        },
        new object[] {
            new BoundingBox((554, 40), (40, 554)),
            (40U, 40U)
        },
    };

    [Theory]
    [MemberData(nameof(OriginTestData))]
    public void OriginIsSmallestCoordinates(BoundingBox bbox, (uint, uint) origin)
    {
        Assert.Equal(origin, bbox.Origin);
    }

    [Theory]
    [InlineData(0, 0, 1, 1, 4)]
    [InlineData(1, 1, 0, 0, 4)]
    [InlineData(0, 0, 2, 2, 9)]
    [InlineData(0, 0, 0, 0, 1)]
    [InlineData(33, 33, 33, 33, 1)]
    [InlineData(5, 2, 25, 2, 21)]
    // TODO Add test that checks for very large areas, challenging max int values. Area might need to be a long, considering zoom level 19 has a maximum of 200+ billion tile area.
    public void Area(uint ax, uint ay, uint bx, uint by, int expectedArea)
    {
        var bbox = new BoundingBox((ax, ay), (bx, by));

        Assert.Equal(expectedArea, bbox.Area);
    }

    [Theory]
    [InlineData(0, 0, 1, 1, 2)]
    [InlineData(1, 1, 0, 0, 2)]
    [InlineData(0, 0, 2, 2, 3)]
    [InlineData(0, 0, 0, 0, 1)]
    [InlineData(33, 33, 33, 33, 1)]
    [InlineData(5, 2, 25, 2, 21)]
    [InlineData(2, 5, 2, 25, 1)]
    public void Width(uint ax, uint ay, uint bx, uint by, uint expectedWidth)
    {
        var bbox = new BoundingBox((ax, ay), (bx, by));

        Assert.Equal(expectedWidth, bbox.Width);
    }
    
    [Theory]
    [InlineData(0, 0, 1, 1, 2)]
    [InlineData(1, 1, 0, 0, 2)]
    [InlineData(0, 0, 2, 2, 3)]
    [InlineData(0, 0, 0, 0, 1)]
    [InlineData(33, 33, 33, 33, 1)]
    [InlineData(5, 2, 25, 2, 1)]
    [InlineData(2, 5, 2, 25, 21)]
    public void Height(uint ax, uint ay, uint bx, uint by, uint expectedHeight)
    {
        var bbox = new BoundingBox((ax, ay), (bx, by));

        Assert.Equal(expectedHeight, bbox.Height);
    }

    [Theory]
    [InlineData(0, 0, 1, 1)]
    [InlineData(1, 1, 0, 0)]
    [InlineData(0, 0, 2, 2)]
    [InlineData(5, 2, 25, 2)]
    public void Corners(uint ax, uint ay, uint bx, uint by)
    {
        var bbox = new BoundingBox((ax, ay), (bx, by));

        var expectedTopLeft = (ax, ay);
        var expectedTopRight = (bx, ay);
        var expectedBottomLeft = (ax, by);
        var expectedBottomRight = (bx, by);

        Assert.Equal(expectedTopLeft, bbox.TopLeft);
        Assert.Equal(expectedTopRight, bbox.TopRight);
        Assert.Equal(expectedBottomLeft, bbox.BottomLeft);
        Assert.Equal(expectedBottomRight, bbox.BottomRight);
    }

    [Theory]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(33, 33, 33, 33, 33, 33)]
    public void SingleTileCorners(uint ax, uint ay, uint bx, uint by, uint expectedX, uint expectedY)
    {
        var bbox = new BoundingBox((ax, ay), (bx, by));

        Assert.Equal((expectedX, expectedY), bbox.TopLeft);
        Assert.Equal((expectedX, expectedY), bbox.TopRight);
        Assert.Equal((expectedX, expectedY), bbox.BottomRight);
        Assert.Equal((expectedX, expectedY), bbox.BottomLeft);
    }

    public static IEnumerable<object[]> EqualityData => new List<object[]> {
        new object[] {
            new BoundingBox((3, 3), (29, 50)),
            new BoundingBox((3, 3), (29, 50)),
        }, // Same parameter order
        new object[] {
            new BoundingBox((3, 3), (29, 50)),
            new BoundingBox((29, 50), (3, 3)),
        } // Reversed parameter order (parameter order should be invariant)
    };

    public static IEnumerable<object[]> InequalityData => new List<object[]> {
        new object[] {
            new BoundingBox((3, 3), (29, 50)),
            new BoundingBox((40, 654), (23, 45)),
        }, // Different boxes
        new object[] {
            new BoundingBox((3, 3), (29, 50)),
            new BoundingBox((3, 3), (20, 20)),
        }, // Same origin
        new object[] {
            new BoundingBox((3, 3), (29, 50)),
            new BoundingBox((4, 4), (30, 51)),
        }, // Same area
        new object[] {
            new BoundingBox((10, 8), (20, 10)), // 10x2
            new BoundingBox((10, 8), (20, 200)), // 10x192
        }, // Same width
        new object[] {
            new BoundingBox((10, 8), (20, 10)), // 10x2
            new BoundingBox((10, 8), (20, 12)), // 8x2
        }, // Same height
    };

    [Theory]
    [MemberData(nameof(EqualityData))]
    public void EqualBoxesAreEqual(BoundingBox a, BoundingBox b)
    {
        Assert.Equal(a, b);
    }

    [Theory]
    [MemberData(nameof(InequalityData))]
    public void InequalBoxesAreInequal(BoundingBox a, BoundingBox b)
    {
        Assert.NotEqual(a, b);
    }

    [Theory]
    [MemberData(nameof(EqualityData))]
    public void EqualBoxesHaveEqualHashCodes(BoundingBox a, BoundingBox b)
    {
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Theory]
    [MemberData(nameof(InequalityData))]
    public void InequalBoxesHaveInequalHashCodes(BoundingBox a, BoundingBox b)
    {
        Assert.NotEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void EqualityOperatorHasSameFunctionalityAsEquals()
    {
        var bbox1 = (BoundingBox)EqualityData.First()[0];
        var bbox2 = (BoundingBox)EqualityData.First()[1];

        // == is expected to give the same result as .Equals()
        Assert.Equal(bbox1.Equals(bbox2), bbox1 == bbox2);
    }

    [Fact]
    public void EqualityOperatorSymmetry()
    {
        var bbox1 = (BoundingBox)EqualityData.First()[0];
        var bbox2 = (BoundingBox)EqualityData.First()[1];

        Assert.Equal(bbox1 == bbox2, bbox2 == bbox1);
    }

    [Fact]
    public void InequalityOperatorSymmetry()
    {
        var bbox1 = (BoundingBox)EqualityData.First()[0];
        var bbox2 = (BoundingBox)EqualityData.First()[1];

        Assert.Equal(bbox1 != bbox2, bbox2 != bbox1);
    }

    [Fact]
    public void EqualsSymmetry()
    {
        var bbox1 = (BoundingBox)EqualityData.First()[0];
        var bbox2 = (BoundingBox)EqualityData.First()[1];

        Assert.Equal(bbox1.Equals(bbox2), bbox2.Equals(bbox1));
    }

    [Fact]
    public void InequalityOperatorOppositeOfEquals()
    {
        var bbox1 = (BoundingBox)EqualityData.First()[0];
        var bbox2 = (BoundingBox)EqualityData.First()[1];

        // != is expected to give the opposite result of a call to .Equals()
        Assert.Equal(!bbox1.Equals(bbox2), bbox1 != bbox2);
    }
}
