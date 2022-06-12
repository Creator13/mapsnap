using System;

namespace mapsnap.Projects;

public record PixelOffsets
{
    public int top, right, bottom, left;

    /**
     * Create offsets with specified x-y values.
     */
    public PixelOffsets(int top, int right, int bottom, int left)
    {
        if (top < 0 || right < 0 ||
            bottom < 0 || left < 0)
        {
            throw new ArgumentOutOfRangeException($"Pixel offsets can not be negative. Values were {top} {right} {bottom} {left}.");
        }

        if (top >= Tiles.TILE_SIZE || right >= Tiles.TILE_SIZE ||
            bottom >= Tiles.TILE_SIZE || left >= Tiles.TILE_SIZE)
        {
            throw new ArgumentOutOfRangeException(
                $"Pixel offsets can not be greater than {Tiles.TILE_SIZE - 1}. Values were {top} {right} {bottom} {left}.");
        }

        this.top = top;
        this.bottom = bottom;
        this.left = left;
        this.right = right;
    }

    /**
     * Create offsets from coordinate pairs.
     */
    public PixelOffsets(CartesianCoordinates topLeft, CartesianCoordinates bottomRight) :
        this(topLeft.y, Tiles.TILE_SIZE - bottomRight.x - 1, Tiles.TILE_SIZE - bottomRight.y - 1, topLeft.x) { }

    /**
     * Creates offsets from world coordinates. Coordinates a and b can be in arbitrary order.
     */
    public PixelOffsets(Coordinates a, Coordinates b, int zoomLvl)
    {
        var pixelsA = Tiles.CoordinatesToTilePixel(a, zoomLvl);
        var pixelsB = Tiles.CoordinatesToTilePixel(b, zoomLvl);

        left = a.longitude < b.longitude ? pixelsA.x : pixelsB.x;
        top = a.latitude > b.latitude ? pixelsA.y : pixelsB.y;
        bottom = Tiles.TILE_SIZE - 1 - (a.latitude < b.latitude ? pixelsA.y : pixelsB.y);
        right = Tiles.TILE_SIZE - 1 - (a.longitude > b.longitude ? pixelsA.x : pixelsB.x);
    }

    public override string ToString()
    {
        return $"{{top {top}, right {right}, bottom {bottom}, left {left}}}";
    }
}
