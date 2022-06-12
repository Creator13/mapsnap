using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace mapsnap;

public static class Tiles
{
    public const int TILE_SIZE = 256;

    public static Image<Rgba32> BlankTile => new(TILE_SIZE, TILE_SIZE, new Rgba32(255, 0, 255));

    public static uint LongToTileX(double lon, int z)
    {
        return (uint) Math.Floor(LongToTileXUnrounded(lon, z));
    }

    public static uint LatToTileY(double lat, int z)
    {
        return (uint)Math.Floor(LatToTileYUnrounded(lat, z));
    }
    
    private static double LongToTileXUnrounded(double lon, int z)
    {
        return (lon + 180.0) / 360.0 * (1 << z);
    }

    private static double LatToTileYUnrounded(double lat, int z)
    {
        return (1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * (1 << z);
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

    public static CartesianCoordinates CoordinatesToTilePixel(Coordinates coord, int zoom)
    {
        var tileXUnrounded = LongToTileXUnrounded(coord.longitude, zoom);
        var tileYUnrounded = LatToTileYUnrounded(coord.latitude, zoom);
        
        var dx = (int) Math.Floor((tileXUnrounded - Math.Floor(tileXUnrounded)) * 256);
        var dy = (int) Math.Floor((tileYUnrounded - Math.Floor(tileYUnrounded)) * 256);
        
        return (dx, dy);
    }

    private static double ToRadians(double num)
    {
        return Math.PI / 180 * num;
    }
}
