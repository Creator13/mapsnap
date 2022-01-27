using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OsmTimelapse;

public static class Tiles
{
    public const int TILE_SIZE = 256;

    public static Image<Rgba32> BlankTile => new(TILE_SIZE, TILE_SIZE, new Rgba32(255, 0, 255));

    public static uint LongToTileX(double lon, int z)
    {
        return (uint)Math.Floor((lon + 180.0) / 360.0 * (1 << z));
    }

    public static uint LatToTileY(double lat, int z)
    {
        return (uint)Math.Floor((1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * (1 << z));
    }

    public static double TileXToLong(int x, int z)
    {
        return x / (double)(1 << z) * 360.0 - 180;
    }

    public static double TileYToLat(int y, int z)
    {
        var n = Math.PI - 2.0 * Math.PI * y / (1 << z);
        return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
    }

    private static double ToRadians(double num)
    {
        return Math.PI / 180 * num;
    }
}
