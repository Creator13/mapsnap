using System;

namespace mapsnap;

public class TileServer
{
    public static readonly TileServer defaultTileServer =
        new("https://tile.openstreetmap.org/", 1, 19, 250, 13, 2) {
            MirrorCount = 3
        };

    public TileServer(string serverUrl, int minZoom, int maxZoom, int maxArea, int unlimitedAreaMaxZoom, int parallelLimit)
    {
        ServerUrl = serverUrl;

        // Add a / to the end of the url if user failed to initialize it.
        // TODO validate this more carefully, preferably the whole url (integrate with future testing suite?)
        if (!IsValidUrl(ServerUrl))
        {
            throw new ArgumentException($"Constructor called with invalid url: {ServerUrl}");
        }

        if (!ServerUrl.EndsWith('/'))
        {
            ServerUrl = $"{serverUrl}/";
        }

        MinZoom = minZoom;
        MaxZoom = maxZoom;
        MaxArea = maxArea;
        UnlimitedAreaMaxZoom = unlimitedAreaMaxZoom;
        ParallelLimit = parallelLimit;
    }

    public string ServerUrl { get; }

    public int MinZoom { get; }
    public int MaxZoom { get; }
    public int MaxArea { get; }
    public int UnlimitedAreaMaxZoom { get; }
    public int ParallelLimit { get; }

    public uint MirrorCount
    {
        get => mirrorCount;
        init
        {
            if (value > 26)
            {
                throw new ArgumentException($"Mirror count cannot be higher than there are letters in the alphabet. Was {mirrorCount}");
            }

            mirrorCount = value;

            var split = ServerUrl.Split("//");
            MirrorUrl = $"{split[0]}//[].{split[1]}";
        }
    }

    private string MirrorUrl { get; init; }

    public bool HasMirrors => MirrorCount > 0;

    private ulong urlIndex = 0;
    private readonly uint mirrorCount;

    public string GetTileUrl(uint x, uint y, int zoom)
    {
        return $@"{ServerUrl}{zoom}/{x}/{y}.png";
    }

    public string GetMirrorTileUrl(uint x, uint y, int zoom)
    {
        var mirrorName = ((char)('a' + urlIndex++ % MirrorCount)).ToString();
        return $@"{MirrorUrl.Replace("[]", mirrorName)}{zoom}/{x}/{y}.png";
    }

    public string GetMirrorTileUrl((uint x, uint y) tile, int zoom)
    {
        return GetMirrorTileUrl(tile.x, tile.y, zoom);
    }

    public string GetTileUrl((uint x, uint y) tile, int zoom)
    {
        return GetTileUrl(tile.x, tile.y, zoom);
    }

    public string GetTileUrl(Coordinates coords, int zoom)
    {
        var x = Tiles.LongToTileX(coords.longitude, zoom);
        var y = Tiles.LatToTileY(coords.latitude, zoom);

        return GetTileUrl(x, y, zoom);
    }

    public bool IsValidAreaSize(int area, int zoom)
    {
        // As per the usage policy of the default tile server (tile.openstreetmap.org), "downloading areas of over 250 tiles at zoom 13 or
        // higher is prohibited," citing unfair server load. Prevent this scenario from happening by validating that the downloaded area is
        // never larger than 250 tiles.
        // Creating requests like this might result in 400 Bad Request errors.
        // Source (Jan. 2022): https://operations.osmfoundation.org/policies/tiles/#bulk-downloading

        return zoom >= 0 && area >= 0 && (area <= MaxArea || (area > MaxArea && zoom < UnlimitedAreaMaxZoom));
    }

    public bool IsValidZoomLevel(int zoom)
    {
        return zoom >= MinZoom && zoom <= MaxZoom;
    }

    private static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) &&
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }
}
