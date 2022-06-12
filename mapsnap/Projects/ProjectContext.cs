using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace mapsnap.Projects;

[Serializable]
public record ProjectContext
{
    public enum FilenamePolicy { Index, Date }

    public enum FileType { Png, Jpg }
    
    internal int Version { get; init; } = ProjectSaveData.CURRENT_VERSION;
    public string Name { get; init; } = "";
    public BoundingBox Area { get; init; }
    public int Zoom { get; init; }
    public FilenamePolicy OutputFilenamePolicy { get; init; } = FilenamePolicy.Date;
    public FileType OutputFileType { get; init; } = FileType.Png;
    public PixelOffsets PixelOffsets { get; init; }

    internal ProjectContext() { }

    public ProjectContext(Coordinates coordA, Coordinates coordB, int zoom)
    {
        (uint x, uint y) a = (Tiles.LongToTileX(coordA.longitude, zoom), Tiles.LatToTileY(coordA.latitude, zoom));
        (uint x, uint y) b = (Tiles.LongToTileX(coordB.longitude, zoom), Tiles.LatToTileY(coordB.latitude, zoom));

        PixelOffsets = new PixelOffsets(coordA, coordB, zoom);
        
        Area = new BoundingBox(a, b);
        Zoom = zoom;
    }

    public ProjectContext(string coordAStr, string coordBStr, int zoom) : this(new Coordinates(coordAStr), new Coordinates(coordBStr), zoom) { }

    public string GetNextImageName()
    {
        IFilenameFormatter formatter = OutputFilenamePolicy switch {
            FilenamePolicy.Index => new IndexFilenameFormatter(),
            FilenamePolicy.Date => new DateFilenameFormatter(),
            _ => throw new ArgumentOutOfRangeException()
        };

        return formatter.Format(Name, OutputFileType);
    }

    public string GetNextGifName()
    {
        return new IndexFilenameFormatter().Format(Name, "gif");
    }

    public List<string> GetImageFilePaths()
    {
        var regex = OutputFilenamePolicy switch {
            FilenamePolicy.Index => @"[\s\S]*" + Name + @"\d+.(?:jpg|png)",
            FilenamePolicy.Date => @"[\s\S]*" + Name + @" \d{4}-\d{2}-\d{2} \d{2}_\d{2}_\d{2}.(?:jpg|png)",
            _ => "(?!)" // regex matches nothing, but this is in theory unreachable unless someone fucks with enums.
        };

        return Directory.EnumerateFiles(Environment.CurrentDirectory)
                        .Where(file => Regex.IsMatch(file, regex))
                        .ToList();
    }

    public static bool IsValidProjectName(string name) => Regex.IsMatch(name, "[a-zA-Z0-9-_]+");
}
