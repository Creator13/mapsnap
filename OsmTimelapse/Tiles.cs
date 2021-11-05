using System;

namespace OsmTimelapse
{
    public static class Tiles
    {
        public static uint LongToTileX(double lon, int z)
        {
            return (uint) Math.Floor((lon + 180.0) / 360.0 * (1 << z));
        }

        public static uint LatToTileY(double lat, int z)
        {
            return (uint) Math.Floor((1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * (1 << z));
        }

        public static double TileXToLong(int x, int z)
        {
            return x / (double) (1 << z) * 360.0 - 180;
        }

        public static double TileYToLat(int y, int z)
        {
            var n = Math.PI - 2.0 * Math.PI * y / (double) (1 << z);
            return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
        }

        public static string GetTileUrl(uint x, uint y, int zoom)
        {
            return $"https://a.tile.openstreetmap.org/{zoom}/{x}/{y}.png";
        }

        public static string GetTileUrl((uint x, uint y) tile, int zoom) => GetTileUrl(tile.x, tile.y, zoom);
        
        public static string GetTileUrl(Coordinates coords, int zoom)
        {
            var x = LongToTileX(coords.longitude, zoom);
            var y = LatToTileY(coords.latitude, zoom);

            return GetTileUrl(x, y, zoom);
        }

        private static double ToRadians(double num)
        {
            return Math.PI / 180 * num;
        }
    }
}
