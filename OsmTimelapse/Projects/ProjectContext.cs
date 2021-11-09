#nullable enable
using System;
using System.Text.Json.Serialization;

namespace OsmTimelapse.Projects;

[Serializable]
public record ProjectContext
{
    public enum FilenamePolicy { Index, Date }

    public enum FileType { Png, Jpg }

    public string Name { get; init; } = "";
    public BoundingBox Area { get; init; }

    public FilenamePolicy OutputFilenamePolicy { get; init; } = FilenamePolicy.Date;
    public FileType OutputFileType { get; init; } = FileType.Png;

    [JsonConstructor]
    public ProjectContext(string name, BoundingBox area, FilenamePolicy outputFilenamePolicy, FileType outputFileType)
    {
        Name = name;
        Area = area;
        OutputFilenamePolicy = outputFilenamePolicy;
        OutputFileType = outputFileType;
    }
    
    public ProjectContext(Coordinates coordA, Coordinates coordB, int zoom)
    {
        (uint x, uint y) a = (Tiles.LongToTileX(coordA.longitude, zoom), Tiles.LatToTileY(coordA.latitude, zoom));
        (uint x, uint y) b = (Tiles.LongToTileX(coordB.longitude, zoom), Tiles.LatToTileY(coordB.latitude, zoom));

        Area = new BoundingBox(a, b);
    }

    public ProjectContext(string coordAStr, string coordBStr, int zoom) : this(new Coordinates(coordAStr), new Coordinates(coordBStr), zoom) { }

    public string GetNextFilename()
    {
        IFilenameFormatter formatter = OutputFilenamePolicy switch {
            FilenamePolicy.Index => new IndexFilenameFormatter(),
            FilenamePolicy.Date => new DateFilenameFormatter(),
            _ => throw new ArgumentOutOfRangeException()
        };

        return formatter.Format(Name, OutputFileType).AddExtension(OutputFileType);
    }
}

internal static class StringExtensions
{
    public static string AddExtension(this string filename, ProjectContext.FileType type)
    {
        return $"{filename}.{type.ToString().ToLower()}";
    }
}
