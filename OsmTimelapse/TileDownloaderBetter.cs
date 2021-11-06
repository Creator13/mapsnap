using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OsmTimelapse;

public class TileDownloaderBetter
{
    private ServiceProvider serviceProvider;

    public TileDownloaderBetter()
    {
        
    }
    
    // private int bytesDownloaded;

    // private readonly BoundingBox box;
    // private readonly int zoom;
    //
    // private Image<Rgba32>[] downloadedTiles;
    //
    // public TileDownloaderBetter(BoundingBox box, int zoom)
    // {
    //     this.box = box;
    //     this.zoom = zoom;
    //
    //     downloadedTiles = new Image<Rgba32>[box.Area];
    // }

    // private async Task<Image<Rgba32>[]> DownloadTiles(HttpClient client)
    // {
    //     var tileData = new ((uint x, uint y), int i)[box.Area];
    //     var i = 0;
    //     for (var y = box.Origin.y; y < box.Origin.y + box.Height; y++)
    //     {
    //         for (var x = box.Origin.x; x < box.Origin.x + box.Width; x++, i++)
    //         {
    //             tileData[i] = ((x, y), i);
    //         }
    //     }
    //
    //     var semaphore = new SemaphoreSlim(5000);
    //
    //     var tasks = tileData.Select(async data =>
    //     {
    //         await semaphore.WaitAsync();
    //
    //         var ((x, y), i) = data;
    //         var tileUrl = Tiles.GetMirrorTileUrl(x, y, zoom);
    //
    //         var task = client.GetAsync(new Uri(tileUrl));
    //         _ = task.ContinueWith(async t =>
    //         {
    //             // await Task.Delay(1000);
    //             semaphore.Release();
    //         });
    //
    //         try
    //         {
    //             var stopwatch = new Stopwatch();
    //             stopwatch.Start();
    //
    //             var response = await task;
    //             if (!response.IsSuccessStatusCode)
    //             {
    //                 await Console.Error.WriteLineAsync(
    //                     $"Unexpected response from OSM tile server: {(int)response.StatusCode} {response.StatusCode}. Please report to the developers.");
    //                 return new Image<Rgba32>(256, 256, new Rgba32(255, 0, 255));
    //             }
    //
    //             var bytes = await response.Content.ReadAsByteArrayAsync();
    //
    //             stopwatch.Stop();
    //             var downloadTime = stopwatch.ElapsedMilliseconds;
    //
    //             bytesDownloaded += bytes.Length;
    //
    //             Console.WriteLine(
    //                 $"Tile ({x},{y}) {i + 1}/{box.Area} {FormatKB(bytes.Length)} (download {downloadTime:#,0}ms{(downloadTime > 5000 ? " !!!" : "")})");
    //             return Image.Load<Rgba32>(bytes);
    //         }
    //         catch (HttpRequestException e)
    //         {
    //             Console.WriteLine($"Could not reach the OSM tile server ({e.StatusCode})! Are you connected to the internet?");
    //             Console.WriteLine($"\tAt tile {tileUrl}");
    //             Console.WriteLine(e);
    //         }
    //
    //         return new Image<Rgba32>(256, 256, new Rgba32(255, 0, 255));
    //     });
    //
    //     return await Task.WhenAll(tasks);
    // }

    // public async Task<Image<Rgba32>[]> DownloadTiles(BoundingBox box, int zoom)
    // {
    //     var client = serviceProvider.GetService<ITileDownloaderHttpClient>();
    //
    //     return await client?.DownloadTiles(box, zoom, 250);
    //     
    //     // var stopwatch = new Stopwatch();
    //     //
    //     // stopwatch.Start();
    //     // var tiles = await downloader.DownloadTiles(httpClient);
    //     // stopwatch.Stop();
    //     //
    //     // Console.WriteLine(
    //     //     $"Downloaded {box.Area} tiles ({FormatKB(downloader.bytesDownloaded)}) in {stopwatch.ElapsedMilliseconds:#,0}ms (average size {FormatKB(downloader.bytesDownloaded / box.Area)})");
    //     // return tiles;
    //     return null;
    // }
}
