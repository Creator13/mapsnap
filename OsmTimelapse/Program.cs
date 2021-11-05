using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OsmTimelapse
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var cornerA = new Coordinates("51.9761;4.1288");
            var cornerB = new Coordinates("52.0533;4.4113");
            var zoom = 15;

            (uint x, uint y) a = (Tiles.LongToTileX(cornerA.longitude, zoom), Tiles.LatToTileY(cornerA.latitude, zoom));
            (uint x, uint y) b = (Tiles.LongToTileX(cornerB.longitude, zoom), Tiles.LatToTileY(cornerB.latitude, zoom));

            // (uint x, uint y) a = (16755, 10827);
            // (uint x, uint y) b = (16759, 10833);

            Console.WriteLine($"Corner A: ({a}) -> ({a.x},{a.y})");
            Console.WriteLine($"Corner B: ({b}) -> ({b.x},{b.y})");

            Console.WriteLine($"Corner A URL: {Tiles.GetTileUrl(a, zoom)}");
            Console.WriteLine($"Corner B URL: {Tiles.GetTileUrl(b, zoom)}");

            var box = new BoundingBox(a, b);
            Console.WriteLine(box.ToString());
            Console.WriteLine($"Final image size: {box.Width * 256}x{box.Height * 256}px ({box.Area * 256 * 256/1_000_000.0:F1}MP)");

            await DownloadTiles(box, zoom);
        }

        public static async Task DownloadTiles(BoundingBox box, int zoom)
        {
            var tiles = new Image<Rgba32>[box.Area];

            var httpClient = new HttpClient();
            // httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
            // httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");
            var i = 0;

            long downloaded = 0;
            for (var y = box.Origin.y; y < box.Origin.y + box.Height; y++)
            {
                for (var x = box.Origin.x; x < box.Origin.x + box.Width; x++)
                {
                    using var response = await httpClient.GetAsync(new Uri(Tiles.GetTileUrl(x, y, zoom)));
                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    downloaded += bytes.Length;
                    Console.WriteLine($"Tile {i+1}/{box.Area} {bytes.Length/1024f:#,##0.0}kB");
                    tiles[i++] = Image.Load<Rgba32>(bytes);
                }
            }
            Console.WriteLine($"Downloaded {downloaded/1024f:#,##0.0}kB");

            using var newImage = new Image<Rgba32>((int) box.Width * 256, (int) box.Height * 256);
            for (var j = 0; j < tiles.Length; j++)
            {
                newImage.Mutate(o =>
                {
                    o.DrawImage(tiles[j], new Point((j % (int) box.Width) * 256, (j / (int) box.Width) * 256), 1f);
                });
            }

            var encoder = new PngEncoder();
            encoder.CompressionLevel = PngCompressionLevel.BestCompression;
            encoder.BitDepth = PngBitDepth.Bit8;
            newImage.Save("output.png", encoder);
        }
    }
}
