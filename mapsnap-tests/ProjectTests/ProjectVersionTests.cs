using System.Text.Json;
using mapsnap;
using mapsnap.Projects;
using Xunit;

namespace mapsnapTests.ProjectTests;

/**
 * Tests whether all versions of project files are interpreted correctly.
 * These tests only test whether all the rules for a file version are correctly implemented, not whether the parsing is correct.
 */
// TODO these tests are not exhaustive, make them
// TODO split out each project property into a separate test for each version
public class ProjectVersionTests
{
    [Fact]
    public void SupportsVersion1()
    {
        const string inputJson = @"{
    ""name"": ""tajo-es"",
    ""area"": {
        ""origin"": {
            ""item1"": 8033,
            ""item2"": 6197
        },
        ""width"": 6,
        ""height"": 5
    },
    ""zoom"": 14,
    ""output_filename_policy"": ""date"",
    ""output_file_type"": ""png""
}";

        var json = JsonDocument.Parse(inputJson);
        var parsedProject = ProjectTools.ProjectFromJson(json.RootElement);

        // Test values that come from json directly
        Assert.Equal("tajo-es", parsedProject.Name);
        Assert.Equal(14, parsedProject.Zoom);
        Assert.Equal(new BoundingBox() {
            Height = 5,
            Width = 6,
            Origin = (8033, 6197)
        }, parsedProject.Area);
        Assert.Equal(MapsnapProject.FilenamePolicy.Date, parsedProject.OutputFilenamePolicy);
        Assert.Equal(MapsnapProject.FileType.Png, parsedProject.OutputFileType);
    }

    [Fact]
    public void Version1AssumesNoPixelPrecision()
    {
        const string inputJson = @"{
    ""name"": ""tajo-es"",
    ""area"": {
        ""origin"": {
            ""item1"": 8033,
            ""item2"": 6197
        },
        ""width"": 6,
        ""height"": 5
    },
    ""zoom"": 14,
    ""output_filename_policy"": ""date"",
    ""output_file_type"": ""png""
}";

        var json = JsonDocument.Parse(inputJson);
        var parsedProject = ProjectTools.ProjectFromJson(json.RootElement);

        // Test assumed values
        Assert.False(parsedProject.UsePixelPrecision);
        Assert.Null(parsedProject.PixelOffsets);
    }

    [Fact]
    public void Version2AssumesNoPixelPrecision()
    {
        const string inputJson = @"{
    ""version"": 2,
    ""name"": ""tajo-es"",
    ""area"": {
        ""origin"": {
            ""item1"": 8033,
            ""item2"": 6197
        },
        ""width"": 6,
        ""height"": 5
    },
    ""zoom"": 14,
    ""output_filename_policy"": ""date"",
    ""output_file_type"": ""png""
}";

        var json = JsonDocument.Parse(inputJson);
        var parsedProject = ProjectTools.ProjectFromJson(json.RootElement);

        // Test assumed values
        Assert.False(parsedProject.UsePixelPrecision);
        Assert.Null(parsedProject.PixelOffsets);
    }

    [Fact]
    public void SupportsVersion2()
    {
        const string inputJson = @"{
    ""version"": 2,
    ""name"": ""tajo-es"",
    ""area"": {
        ""origin"": {
            ""item1"": 8033,
            ""item2"": 6197
        },
        ""width"": 6,
        ""height"": 5
    },
    ""zoom"": 14,
    ""output_filename_policy"": ""date"",
    ""output_file_type"": ""png""
}";

        var json = JsonDocument.Parse(inputJson);
        var parsedProject = ProjectTools.ProjectFromJson(json.RootElement);

        // Unnecessary
        Assert.Equal(2, json.RootElement.GetProperty("version").GetInt32());

        // Test values that come from json directly
        Assert.Equal("tajo-es", parsedProject.Name);
        Assert.Equal(14, parsedProject.Zoom);
        Assert.Equal(new BoundingBox() {
            Height = 5,
            Width = 6,
            Origin = (8033, 6197)
        }, parsedProject.Area);
        Assert.Equal(MapsnapProject.FilenamePolicy.Date, parsedProject.OutputFilenamePolicy);
        Assert.Equal(MapsnapProject.FileType.Png, parsedProject.OutputFileType);
    }

    [Fact]
    public void SupportsVersion3()
    {
        const string inputJson = @"{
    ""version"": 3,
    ""name"": ""bathurst"",
    ""zoom"": 16,
    ""output_file_type"": ""png"",
    ""output_filename_policy"": ""date"",
    ""coordinates"": [
        {
            ""latitude"": 47.6989,
            ""longitude"": -65.7012
        },
        {
            ""latitude"": 47.6972,
            ""longitude"": -65.6909
        }
    ],
    ""pixel_precision"": true
}";

        var json = JsonDocument.Parse(inputJson);
        var parsedProject = ProjectTools.ProjectFromJson(json.RootElement);
        Assert.Equal("bathurst", parsedProject.Name);
        Assert.Equal(16, parsedProject.Zoom);
        Assert.Equal(new BoundingBox() {
            Width = 3,
            Height = 2,
            Origin = (20807, 22862)
        }, parsedProject.Area);
        Assert.Equal(MapsnapProject.FilenamePolicy.Date, parsedProject.OutputFilenamePolicy);
        Assert.Equal(MapsnapProject.FileType.Png, parsedProject.OutputFileType);
        Assert.True(parsedProject.UsePixelPrecision);
        // We don't need to test the actual pixel offsets; these are inferred and therefore tested elsewhere
    }
}
