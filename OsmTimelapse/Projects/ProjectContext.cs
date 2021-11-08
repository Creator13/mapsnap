#nullable enable
using System;

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
