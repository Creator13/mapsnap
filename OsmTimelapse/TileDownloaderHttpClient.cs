using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ConsoleTools;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace OsmTimelapse;

public interface ITileDownloaderHttpClient
{
    long BytesDownloaded { get; }
    Task<Image<Rgba32>[]> DownloadTiles(BoundingBox box, int zoom, int requestLimit = 100, int limitingPeriod = 0);
}

public class TileDownloaderHttpClient : ITileDownloaderHttpClient
{
    private readonly HttpClient httpClient;
    public long BytesDownloaded { get; private set; }

    private int done;

    public TileDownloaderHttpClient(HttpClient client)
    {
        httpClient = client;
        httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Mapsnap", $"v{Assembly.GetExecutingAssembly().GetName().Version}"));
    }

    public async Task<Image<Rgba32>[]> DownloadTiles(BoundingBox box, int zoom, int requestLimit = 100, int limitingPeriod = 0)
    {
        BytesDownloaded = 0;
        done = 0;

        var tileData = CreateTileData(box);

        var semaphore = new SemaphoreSlim(requestLimit);

        Console.Write($"Downloading {box.Area} tiles: ");
        var progressBar = new ProgressBar(box.Area);
        var tasks = tileData.Select(async data =>
        {
            await semaphore.WaitAsync();

            var ((x, y), i) = data;
            var tileUrl = Tiles.GetMirrorTileUrl(x, y, zoom);

            var task = httpClient.GetAsync(new Uri(tileUrl));
            _ = task.ContinueWith(async _ =>
            {
                await Task.Delay(limitingPeriod);
                semaphore.Release();
            });

            try
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var response = await task;
                if (!response.IsSuccessStatusCode)
                {
                    await Console.Error.WriteLineAsync(
                        $"Unexpected response from OSM tile server: {(int)response.StatusCode} {response.StatusCode}. Please report to the developers.");
                    return new Image<Rgba32>(256, 256, new Rgba32(255, 0, 255));
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();

                stopwatch.Stop();
                var downloadTime = stopwatch.ElapsedMilliseconds;

                BytesDownloaded += bytes.Length;

                // Console.WriteLine(
                //     $"Tile ({x},{y}) {i + 1}/{box.Area} {FormatKB(bytes.Length)} (download {downloadTime:#,0}ms{(downloadTime > 5000 ? " !!!" : "")})");
                done++;
                progressBar.Report(done);

                return Image.Load<Rgba32>(bytes);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Could not reach the OSM tile server ({e.StatusCode})! Are you connected to the internet?");
                Console.WriteLine($"\tAt tile {tileUrl}");
                Console.WriteLine(e);
            }

            return new Image<Rgba32>(256, 256, new Rgba32(255, 0, 255));
        });

        var result = await Task.WhenAll(tasks);

        progressBar.Dispose();
        Console.WriteLine("Done.");

        return result;
    }

    // private async Task<Image<Rgba32>> DownloadTile(((uint x, uint y), int i) data)
    // {
    //     
    // }

    public static async Task<Image<Rgba32>[]> DownloadTiles(BoundingBox box, int zoom)
    {
        var client = Program.serviceProvider.GetService<ITileDownloaderHttpClient>();

        if (client == null) return null;

        var stopwatch = new Stopwatch();

        stopwatch.Start();
        var tiles = await client.DownloadTiles(box, zoom, 400, 1000);
        stopwatch.Stop();

        Console.WriteLine(
            $"Downloaded {box.Area} tiles ({FormatKB(client.BytesDownloaded)}) in {stopwatch.ElapsedMilliseconds:#,0}ms (average size {FormatKB(client.BytesDownloaded / box.Area)})");
        return tiles;
    }

    private static string FormatKB(long bytes)
    {
        return $"{bytes / 1024f:#,0.0}kB";
    }

    private static IEnumerable<((uint x, uint y), int i)> CreateTileData(BoundingBox box)
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

        return tileData;
    }
}
