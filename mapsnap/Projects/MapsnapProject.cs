using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace mapsnap.Projects;

public record MapsnapProject
{
    public enum FilenamePolicy { Index, Date }

    public enum FileType { Png, Jpg }

    internal readonly Coordinates coordsA, coordsB;

    internal int Version { get; init; } = ProjectTools.CURRENT_SAVE_VERSION;
    public string Name { get; init; } = "";
    public BoundingBox Area { get; init; }
    public int Zoom { get; init; }
    public FilenamePolicy OutputFilenamePolicy { get; init; } = FilenamePolicy.Date;
    public FileType OutputFileType { get; init; } = FileType.Png;
    public bool UsePixelPrecision { get; init; }

    public PixelOffsets PixelOffsets { get; init; }

    public int ImageWidth
    {
        get
        {
            var width = (int)Area.Width * Tiles.TILE_SIZE;
            if (UsePixelPrecision)
            {
                width -= PixelOffsets.left + PixelOffsets.right;
            }

            return width;
        }
    }

    public int ImageHeight
    {
        get
        {
            var height = (int)Area.Height * Tiles.TILE_SIZE;
            if (UsePixelPrecision)
            {
                height -= PixelOffsets.top + PixelOffsets.bottom;
            }

            return height;
        }
    }

    internal MapsnapProject(BoundingBox bbox, int zoom)
    {
        Area = bbox;
        coordsA = new Coordinates(Tiles.TileYToLat(bbox.TopLeft.y, zoom), Tiles.TileXToLong(bbox.TopLeft.x, zoom));
        coordsB = new Coordinates(Tiles.TileYToLat(bbox.BottomRight.y + 1, zoom), Tiles.TileXToLong(bbox.BottomRight.x + 1, zoom));
    }

    public MapsnapProject(Coordinates coordA, Coordinates coordB, int zoom)
    {
        coordsA = coordA;
        coordsB = coordB;

        (uint x, uint y) a = (Tiles.LongToTileX(coordsA.longitude, zoom), Tiles.LatToTileY(coordsA.latitude, zoom));
        (uint x, uint y) b = (Tiles.LongToTileX(coordsB.longitude, zoom), Tiles.LatToTileY(coordsB.latitude, zoom));

        PixelOffsets = new PixelOffsets(coordsA, coordsB, Zoom);

        Area = new BoundingBox(a, b);
        Zoom = zoom;
    }

    public MapsnapProject(string coordAStr, string coordBStr, int zoom) : this(new Coordinates(coordAStr), new Coordinates(coordBStr), zoom) { }

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

    public static bool IsValidProjectName(string name)
    {
        return Regex.IsMatch(name, "[a-zA-Z0-9-_]+");
    }
}
