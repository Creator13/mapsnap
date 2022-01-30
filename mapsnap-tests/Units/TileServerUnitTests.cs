using System;
using System.Collections.Generic;
using mapsnap;
using Xunit;

namespace mapsnapTests.Units;

public class TileServerUnitTests
{
    //TODO create object creating unit tests (slightly unnecessary since this code can't be affected by users (for now))

    // NOTE: MirrorUrl was previously a public field
    // [Fact]
    // public void CorrectMirrorUrl()
    // {
    //     var server = new TileServer(
    //             "https://mock.tiles.org/",
    //             1, 19,
    //             250, 13,
    //             2)
    //         { MirrorCount = 3 };
    //
    //     Assert.Equal("https://[].mock.tiles.org/", server.MirrorUrl);
    // }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(15, true)]
    [InlineData(26, true)]
    public void HasMirrorsCorrect(uint mirrorCount, bool expected)
    {
        var server = new TileServer(
                "https://mock.tiles.org/",
                1, 19,
                250, 13,
                2)
            { MirrorCount = mirrorCount };

        Assert.Equal(expected, server.HasMirrors);
    }

    [Fact]
    public void MirrorCountLimitedToAlphabet()
    {
        void ConstructServer()
        {
            _ = new TileServer(
                    "https://mock.tiles.org/",
                    1, 19,
                    250, 13,
                    2)
                { MirrorCount = 27 };
        }

        Assert.Throws<ArgumentException>(ConstructServer);
    }

    [Theory]
    [InlineData("file://mock.tiles.org")]
    [InlineData("www.openstreetmap.org")]
    public void InvalidServerUrl(string url)
    {
        void ConstructServer()
        {
            _ = new TileServer(
                url,
                1, 19,
                250, 13,
                2);
        }

        Assert.Throws<ArgumentException>(ConstructServer);
    }

    [Theory]
    [InlineData("https://mock.tiles.org/")]
    [InlineData("https://mock.tiles.org")]
    [InlineData("https://mock.tiles.org/tiles/")]
    [InlineData("https://mock.tiles.org/tiles")]
    public void ServerUrlEndsWithSlash(string url)
    {
        var server = new TileServer(
            url,
            1, 19,
            250, 13,
            2);

        Assert.EndsWith("/", server.ServerUrl);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(2)]
    [InlineData(19)]
    public void ZoomValidationValidInput(int zoom)
    {
        var server = new TileServer(
            "https://mock.tiles.org/",
            1, 19,
            250, 13,
            2);

        Assert.True(server.IsValidZoomLevel(zoom));
    }

    [Theory]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(20)]
    [InlineData(39)]
    [InlineData(-10)]
    [InlineData(-1)]
    public void ZoomValidationInvalidInput(int zoom)
    {
        var server = new TileServer(
            "https://mock.tiles.org/",
            1, 19,
            250, 13,
            2);

        Assert.False(server.IsValidZoomLevel(zoom));
    }

    // TODO test object construction with invalid zoom ranges


    [Theory]
    // The following inputs expect the mock object to have parameters of max area: 250, max zoom: 13 
    [InlineData(10, 15, true)] // zoomed in, but small enough
    [InlineData(250, 15, true)] // Zoomed in, but on limit
    [InlineData(-10, 15, false)] // Negative area -> these are undefined behavior as of now, so return false
    [InlineData(10, -15, false)] // Negative zoom -> see "Negative area"
    [InlineData(251, 13, false)] // Over limit area, on limit zoom
    [InlineData(int.MaxValue, 1, true)] // Area is unlimited under zoom limit
    [InlineData(0, 4, true)] // Area of zero is illegal, but not invalid
    public void AreaSizeValidation(int area, int zoom, bool expected)
    {
        var server = new TileServer(
            "https://mock.tiles.org/",
            1, 19,
            250, 13,
            2);

        Assert.Equal(expected, server.IsValidAreaSize(area, zoom));
    }

    [Theory]
    [InlineData(23, 5, 15)]
    [InlineData(0, 0, 4)]
    [InlineData(55345, 234223, 5)]
    public void TileUrl(uint x, uint y, int zoom)
    {
        const string url = "https://mock.tiles.org/";
        var expected = $"https://mock.tiles.org/{zoom}/{x}/{y}.png";

        var server = new TileServer(
            url, 1, 19,
            250, 13,
            2);

        var result = server.GetTileUrl(x, y, zoom);

        // These assertions are merely for error localization and not necessary.
        Assert.False(string.IsNullOrEmpty(result));
        Assert.StartsWith(url, result);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(23, 5, 15)]
    [InlineData(0, 0, 4)]
    [InlineData(55345, 234223, 5)]
    public void TileUrlOverloads(uint x, uint y, int zoom)
    {
        // Overloads should return the same result as the base method, not necessarily a correct one

        const string url = "https://mock.tiles.org/";
        var coords = new Coordinates(Tiles.TileYToLat((int)y, zoom), Tiles.TileXToLong((int)x, zoom));

        var server = new TileServer(
            url, 1, 19,
            250, 13,
            2);

        var baseMethodResult = server.GetTileUrl(x, y, zoom);

        // Overload GetTileUrl((uint x, uint y) tile, int zoom)
        Assert.Equal(baseMethodResult, server.GetTileUrl((x, y), zoom));
        // Overload GetTileUrl(Coordinates coords, int zoom)
        Assert.Equal(baseMethodResult, server.GetTileUrl(coords, zoom));
    }

    // [Fact]
    // public void

    [Theory]
    [InlineData(23, 5, 15)]
    [InlineData(0, 0, 4)]
    [InlineData(55345, 234223, 5)]
    public void MirrorTileUrlStartsWithA(uint x, uint y, int zoom)
    {
        const string url = "https://mock.tiles.org/";

        var server = new TileServer(
            url, 1, 19,
            250, 13,
            2) {
            MirrorCount = 3
        };

        var expected = $"https://a.mock.tiles.org/{zoom}/{x}/{y}.png";
        Assert.Equal(expected, server.GetMirrorTileUrl(x, y, zoom));
    }

    [Theory]
    [InlineData(23, 5, 15, 3)]
    [InlineData(0, 0, 4, 10)]
    [InlineData(55345, 234223, 5, 1)]
    [InlineData(55345, 234223, 5, 5)]
    public void MirrorTileUrlsHaveEvenDistribution(uint x, uint y, int zoom, uint mirrorCount)
    {
        const string url = "https://mock.tiles.org/";

        var server = new TileServer(
            url, 1, 19,
            250, 13,
            2) {
            MirrorCount = mirrorCount
        };

        var expectedCount = 1999 / (int)mirrorCount;

        var urlCounts = new Dictionary<string, int>((int)server.MirrorCount);
        for (var i = 0; i < server.MirrorCount * expectedCount; i++)
        {
            var result = server.GetMirrorTileUrl(x, y, zoom);

            if (!urlCounts.TryAdd(result, 1))
            {
                urlCounts[result]++;
            }
        }

        // There should be as many different URLs generated as there are mirrors
        Assert.Equal((int)server.MirrorCount, urlCounts.Count);

        // Assert that all values are the same, and an even proportion of the total urls generated.
        // This is ensured by running the loop {MirrorCount * expectedCounts} number of times.
        foreach (var count in urlCounts.Values)
        {
            Assert.Equal(expectedCount, count);
        }
    }

    [Theory]
    [InlineData(23, 5, 15)]
    [InlineData(0, 0, 4)]
    [InlineData(55345, 234223, 5)]
    public void MirrorTileUrlOverloads(uint x, uint y, int zoom)
    {
        // Overloads should return the same result as the base method, not necessarily a correct one

        const string url = "https://mock.tiles.org/";

        TileServer NewServer() => new(
            url, 1, 19,
            250, 13,
            2) {
            MirrorCount = 3
        };

        // Test twice to catch possible errors in overload diverging from the base method

        var baseMethodServer = NewServer();
        var overloadMethodServer = NewServer();

        var baseMethodResult = baseMethodServer.GetMirrorTileUrl(x, y, zoom);
        var overloadMethodResult = overloadMethodServer.GetMirrorTileUrl((x, y), zoom);

        Assert.Equal(baseMethodResult, overloadMethodResult);
        Assert.Equal(baseMethodServer.GetMirrorTileUrl(x, y, zoom), overloadMethodServer.GetMirrorTileUrl((x, y), zoom));
    }
}

// This class tests that the default tile server implementation is still correct. Should be kept up to date based on updates from the
// OSMF/whoever runs the default tile server.
// See: https://wiki.openstreetmap.org/wiki/Tile_servers
// Usage policy: https://operations.osmfoundation.org/policies/tiles
public class DefaultTileServerTests
{
    private const string SERVER_URL = "https://tile.openstreetmap.org/";
    private const int MIN_ZOOM = 1;
    private const int MAX_ZOOM = 19;
    private const int PARALLEL_LIMIT = 2;
    private const uint MIRROR_COUNT = 3;

    [Fact]
    public void DefaultTileServerUpToDate()
    {
        Assert.Equal(SERVER_URL, TileServer.defaultTileServer.ServerUrl);
        Assert.Equal(MIN_ZOOM, TileServer.defaultTileServer.MinZoom);
        Assert.Equal(MAX_ZOOM, TileServer.defaultTileServer.MaxZoom);
        Assert.Equal(PARALLEL_LIMIT, TileServer.defaultTileServer.ParallelLimit);
        Assert.Equal(MIRROR_COUNT, TileServer.defaultTileServer.MirrorCount);
    }
}
