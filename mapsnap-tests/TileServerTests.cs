using System;
using mapsnap;
using Xunit;

namespace mapsnapTests;

public class TileServerTests
{
    [Fact]
    public void CorrectMirrorUrl()
    {
        var server = new TileServer(
                "https://mock.tiles.org/",
                1, 19,
                250, 13,
                2)
            { MirrorCount = 3 };

        Assert.Equal("https://[].mock.tiles.org/", server.MirrorUrl);
    }

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
    [InlineData("https://mock.tiles.org/")]
    [InlineData("https://mock.tiles.org")]
    [InlineData("https://mock.tiles.org/tiles/")]
    [InlineData("https://mock.tiles.org/tiles")]
    public void MirrorUrlEndsWithSlash(string url)
    {
        var server = new TileServer(
            url,
            1, 19,
            250, 13,
            2) {
            MirrorCount = 3
        };

        Assert.EndsWith("/", server.MirrorUrl);
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
        
        Assert.True(server.ValidateZoom(zoom));
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
        
        Assert.False(server.ValidateZoom(zoom));
    }
    
    // TODO test object construction with invalid zoom ranges
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
