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
using mapsnap.Utils;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace mapsnap;

public interface ITileDownloaderHttpClient
{
    long BytesDownloaded { get; }
    long DownloadTime { get; }
    Task<Image<Rgba32>[]> DownloadTiles(TileServer server, BoundingBox box, int zoom, int requestLimit = 100, int limitingPeriod = 0);
}

public class TileDownloaderHttpClient : ITileDownloaderHttpClient
{
    private readonly HttpClient httpClient;

    private int done;

    public TileDownloaderHttpClient(HttpClient client)
    {
        httpClient = client;
        httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("Mapsnap", $"v{Assembly.GetExecutingAssembly().GetName().Version}"));
        httpClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true, MaxAge = TimeSpan.Zero };
        httpClient.DefaultRequestHeaders.Host = "tile.openstreetmap.org";
        httpClient.DefaultRequestHeaders.Pragma.Add(new NameValueHeaderValue("no-cache"));
    }

    public long BytesDownloaded { get; private set; }
    public long DownloadTime { get; private set; }

    private delegate string UrlFormatter(uint x, uint y, int zoom);

    public async Task<Image<Rgba32>[]> DownloadTiles(TileServer tileServer, BoundingBox box, int zoom, int requestLimit, int limitingPeriod = 0)
    {
        BytesDownloaded = 0;
        done = 0;

        var tileData = CreateTileData(box);

        var semaphore = new SemaphoreSlim(requestLimit);

        UrlFormatter getUrl;
        if (tileServer.HasMirrors)
        {
            getUrl = tileServer.GetMirrorTileUrl;
        }
        else
        {
            getUrl = tileServer.GetTileUrl;
        }

        Console.Write($"Downloading {box.Area} tiles: ");
        var progressBar = new ProgressBar(box.Area);
        var tasks = tileData.Select(async data =>
        {
            await semaphore.WaitAsync();

            var ((x, y), i) = data;
            var tileUrl = getUrl(x, y, zoom);

            var task = httpClient.GetAsync(new Uri(tileUrl));
            _ = task.ContinueWith(async _ =>
            {
                if (limitingPeriod > 0) await Task.Delay(limitingPeriod);
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
                    return Tiles.BlankTile;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();

                // Console.WriteLine(response.Headers);
                // if (response.Headers.Age > TimeSpan.Zero || response.Headers.GetValues("x-cache").Contains("HIT"))
                // {
                //     Console.WriteLine($"Tile ({x}, {y}, z{zoom}) was served from proxy cache and may not be entirely up to date! Tile age was: {response.Headers.Age:c}.");
                // }

                stopwatch.Stop();
                DownloadTime += stopwatch.ElapsedMilliseconds;

                BytesDownloaded += bytes.Length;

                // Console.WriteLine(
                // $"Tile ({x},{y}) {i + 1}/{box.Area} {FormatKB(bytes.Length),6} (took {downloadTime:#,0}ms{(downloadTime > 5000 ? " !!!" : "")})");
                done++;
                progressBar.Report(done);

                return Image.Load<Rgba32>(bytes);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Could not reach the OSM tile server! Are you connected to the internet?");
                Console.WriteLine($"\tAt tile {tileUrl}");
                Console.WriteLine(e);
            }

            return Tiles.BlankTile;
        });

        var result = await Task.WhenAll(tasks);

        progressBar.Dispose();

        return result;
    }

    public static async Task<Image<Rgba32>[]> DownloadTiles(TileServer server, BoundingBox box, int zoom)
    {
        var client = MapSnapProgram.ServiceProvider.GetService<ITileDownloaderHttpClient>();

        if (client == null) return null;

        var stopwatch = new Stopwatch();

        stopwatch.Start();
        var tiles = await client.DownloadTiles(server, box, zoom, server.ParallelLimit);
        stopwatch.Stop();

        var totalSize = StringUtils.FormatKB(client.BytesDownloaded);
        var totalTime = StringUtils.FormatElapsedTime(stopwatch.ElapsedMilliseconds);
        var avgSize = StringUtils.FormatKB(client.BytesDownloaded / box.Area);
        var avgTime = client.DownloadTime / (double)box.Area;
        Console.WriteLine($"Downloaded {box.Area} tiles ({totalSize}) in {totalTime} (average size {avgSize}, average time {avgTime:#,0}ms)");

        return tiles;
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
