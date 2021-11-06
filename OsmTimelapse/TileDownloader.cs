using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Timer = System.Timers.Timer;

namespace OsmTimelapse
{
    public class TileDownloader
    {
        private int bytesDownloaded;

        private readonly BoundingBox box;
        private readonly int zoom;

        private Image<Rgba32>[] downloadedTiles;

        public TileDownloader(BoundingBox box, int zoom)
        {
            this.box = box;
            this.zoom = zoom;

            downloadedTiles = new Image<Rgba32>[box.Area];
        }

        private async ValueTask DownloadTile(((uint x, uint y) t, int i) data, CancellationToken token)
        {
            var (t, i) = data;
            try
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                    $"Mapsnap v{Assembly.GetExecutingAssembly().GetName().Version}");
                using var response = await httpClient.GetAsync(new Uri(Tiles.GetMirrorTileUrl(t, zoom)));
                if (!response.IsSuccessStatusCode)
                {
                    await Console.Error.WriteLineAsync(
                        $"Unexpected response from OSM tile server: {(int) response.StatusCode} {response.StatusCode}. Please report to the developers.");
                    return;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                bytesDownloaded += bytes.Length;
                Console.WriteLine($"Tile ({t.x},{t.y}) {i + 1}/{box.Area} {FormatKB(bytes.Length)}");
                downloadedTiles[i] = Image.Load<Rgba32>(bytes);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Could not reach the OSM tile server! Are you connected to the internet?");
                Console.WriteLine(e.StackTrace);
            }
        }

        private async Task<Image<Rgba32>[]> DownloadTiles()
        {
            var tileData = new ((uint x, uint y), int i)[box.Area];
            var i = 0;
            for (var y = box.Origin.y; y < box.Origin.y + box.Height; y++)
            {
                for (var x = box.Origin.x; x < box.Origin.x + box.Width; x++, i++)
                {
                    tileData[i] = ((x, y), i);
                }
            }

            await Parallel.ForEachAsync(tileData, new ParallelOptions { MaxDegreeOfParallelism = 100 }, DownloadTile);
            return downloadedTiles;
        }

        public static async Task<Image<Rgba32>[]> DownloadTiles(BoundingBox box, int zoom)
        {
            var downloader = new TileDownloader(box, zoom);

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var tiles = await downloader.DownloadTiles();
            stopwatch.Stop();
            
            Console.WriteLine($"Downloaded {box.Area}tiles ({FormatKB(downloader.bytesDownloaded)}) in {stopwatch.ElapsedMilliseconds:#,0}ms (average size {FormatKB(downloader.bytesDownloaded / box.Area)})");
            return tiles;
            // for (var y = box.Origin.y; y < box.Origin.y + box.Height; y++)
            // {
            //     for (var x = box.Origin.x; x < box.Origin.x + box.Width; x++)
            //     {
            //         try
            //         {
            //             using var response = await httpClient.GetAsync(new Uri(Tiles.GetTileUrl(x, y, zoom)));
            //             if (!response.IsSuccessStatusCode)
            //             {
            //                 Console.Error.WriteLine(
            //                     $"Unexpected response from OSM tile server: {(int) response.StatusCode} {response.StatusCode}. Please report to the developers.");
            //                 return null;
            //             }
            //
            //             var bytes = await response.Content.ReadAsByteArrayAsync();
            //             bytesDownloaded += bytes.Length;
            //             Console.WriteLine($"Tile ({x},{y}) {i + 1}/{box.Area} {FormatKB(bytes.Length)}");
            //             tiles[i++] = Image.Load<Rgba32>(bytes);
            //         }
            //         catch (HttpRequestException e)
            //         {
            //             Console.WriteLine("Could not reach the OSM tile server! Are you connected to the internet?");
            //             Console.WriteLine(e.StackTrace);
            //             return null;
            //         }
            //     }
            // }
            //
            // Console.WriteLine($"Downloaded {FormatKB(bytesDownloaded)} (average size {FormatKB(bytesDownloaded / box.Area)})");

            // return tiles;
            // return null;
        }

        private static string FormatKB(long bytes)
        {
            return string.Format("{0:#,0.0}kB", bytes / 1024f);
        }
    }
}
