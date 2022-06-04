using mapsnap;
using Xunit;

namespace mapsnapTests;

public class BoundingBoxCoordinateIntegration
{
    [Fact]
    public void SingleTileBBoxHasArea1()
    {
        // Tile chosen is in Istanbul at zoom 14; both coordinates lie in the same tile
        // The tile number is 9510, 6142
        var coordA = new Coordinates("41.01105,28.96541");
        var coordB = new Coordinates("40.99893,28.97811");
        const int zoom = 14;

        (uint x, uint y) a = (Tiles.LongToTileX(coordA.longitude, zoom), Tiles.LatToTileY(coordA.latitude, zoom));
        (uint x, uint y) b = (Tiles.LongToTileX(coordB.longitude, zoom), Tiles.LatToTileY(coordB.latitude, zoom));

        var bbox = new BoundingBox(a, b);

        Assert.Equal(1, bbox.Area);
    }

    [Fact]
    public void SingleTileBBoxHasSameCorners()
    {
        // Tile chosen is in Istanbul at zoom 14; both coordinates lie in the same tile
        // The tile coordinates are (9510, 6142)
        var coordA = new Coordinates("41.01105,28.96541");
        var coordB = new Coordinates("40.99893,28.97811");
        const int zoom = 14;

        (uint x, uint y) a = (Tiles.LongToTileX(coordA.longitude, zoom), Tiles.LatToTileY(coordA.latitude, zoom));
        (uint x, uint y) b = (Tiles.LongToTileX(coordB.longitude, zoom), Tiles.LatToTileY(coordB.latitude, zoom));

        var bbox = new BoundingBox(a, b);

        Assert.Equal((9510U, 6142U), bbox.TopLeft);
        Assert.Equal((9510U, 6142U), bbox.TopRight);
        Assert.Equal((9510U, 6142U), bbox.BottomLeft);
        Assert.Equal((9510U, 6142U), bbox.BottomRight);
    }

    private static BoundingBox QuadTileBBox
    {
        get
        {
            // Tiles chosen are in Istanbul at zoom 14; the coordinates span 4 tiles
            // The tile coordinates are (9510, 6142) for the origin
            var coordA = new Coordinates("41.02764,28.96190");
            var coordB = new Coordinates("40.99817,29.00147");
            const int zoom = 14;

            (uint x, uint y) a = (Tiles.LongToTileX(coordA.longitude, zoom), Tiles.LatToTileY(coordA.latitude, zoom));
            (uint x, uint y) b = (Tiles.LongToTileX(coordB.longitude, zoom), Tiles.LatToTileY(coordB.latitude, zoom));

            return new BoundingBox(a, b);
        }
    }

    [Fact]
    public void QuadTileBBoxHasArea4()
    {
        Assert.Equal(4, QuadTileBBox.Area);
    }
    
    [Fact]
    public void QuadTileBBoxWidthAndHeight()
    {
        var bbox = QuadTileBBox;
        Assert.Equal(2U, bbox.Width);
        Assert.Equal(2U, bbox.Height);
    }

    [Fact]
    public void QuadTileBBoxCorners()
    {
        var bbox = QuadTileBBox;
        
        var expectedTopLeft = (9510U, 6141U);
        var expectedTopRight = (9511U, 6141U);
        var expectedBottomLeft = (9510U, 6142U);
        var expectedBottomRight = (9511U, 6142U);
        
        Assert.Equal(expectedTopLeft, bbox.TopLeft);
        Assert.Equal(expectedTopRight, bbox.TopRight);
        Assert.Equal(expectedBottomLeft, bbox.BottomLeft);
        Assert.Equal(expectedBottomRight, bbox.BottomRight);
    }
}
