using System;
using System.Threading.Tasks;
using CommandLine;
using ConsoleTools;
using Microsoft.Extensions.DependencyInjection;
using OsmTimelapse.CommandLine;
using OsmTimelapse.Projects;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace OsmTimelapse;

public static class MapSnapProgram
{
    public static ServiceProvider ServiceProvider { get; private set; }
    public static ProjectContext ProjectContext { get; private set; }

    public class Options { }

    public static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection().AddHttpClient();
        services.AddHttpClient<ITileDownloaderHttpClient, TileDownloaderHttpClient>();
        ServiceProvider = services.BuildServiceProvider();

        Parser.Default.ParseArguments<NewOptions, object>(args).MapResult(
            (NewOptions o) => NewCommand(o),
            errors => 1);

        return 0;
        var cornerA = new Coordinates("51.9761;4.1288");
        var cornerB = new Coordinates("52.0533;4.4113");
        var zoom = 14;

        (uint x, uint y) a = (Tiles.LongToTileX(cornerA.longitude, zoom), Tiles.LatToTileY(cornerA.latitude, zoom));
        (uint x, uint y) b = (Tiles.LongToTileX(cornerB.longitude, zoom), Tiles.LatToTileY(cornerB.latitude, zoom));

        var box = new BoundingBox(a, b);

        if (args is { Length: > 0 } && args[0] != null)
        {
            if (args[0].ToLower() == "new")
            {
                var project = ProjectTools.GenerateProject("delft", box);
                ProjectTools.SaveProject(project);

                return 0;
            }
            else if (args[0].ToLower() == "load")
            {
                if (!ProjectTools.HasProject())
                {
                    Console.WriteLine("No project found!");
                    return 0;
                }

                ProjectContext = ProjectTools.LoadProject();
                ProjectContext.LogDetectedProject();

                box = ProjectContext.Area;
            }
        }

        Console.WriteLine($"Corner A URL: {Tiles.GetTileUrl(a, zoom)}");
        Console.WriteLine($"Corner B URL: {Tiles.GetTileUrl(b, zoom)}");

        Console.WriteLine(box.ToString());
        Console.WriteLine($"Final image size: {box.Width * 256}x{box.Height * 256}px ({box.Area * 256L * 256L / 1_000_000.0:#,0.0}MP)");

        if (box.Area * 256L * 256L > 100_000_000L)
        {
            var result = ConsoleFunctions.ConsoleConfirm(
                "The requested image will be over 100MP, likely resulting in very large files. Are you sure you want to continue?",
                false);

            if (!result)
            {
                Console.WriteLine("Try lowering the zoom value to get a smaller image, or choose a smaller region.");
                Environment.Exit(0);
            }
        }

        var tiles = await TileDownloaderHttpClient.DownloadTiles(box, zoom);
        // if (tiles == null) return;
        MakeImage((int)box.Width, (int)box.Height, tiles);

        return 0;
    }

    public static int NewCommand(NewOptions o)
    {
        Console.WriteLine($"Name: {o.Name}");
        Console.WriteLine($"CoordA: {o.CoordA}");
        Console.WriteLine($"CoordB: {o.CoordB}");
        Console.WriteLine($"FileType: {o.FileType}");
        Console.WriteLine($"NameFormat: {o.NameFormat}");

        if (!(ValidateCoordinate(o.CoordA) && ValidateCoordinate(o.CoordB)))
        {
            return 1;
        }
        
        var coordA = new Coordinates(o.CoordA);
        var coordB = new Coordinates(o.CoordB);
        
        if (!Enum.TryParse(o.FileType, out ProjectContext.FileType fileType))
        {
            var allowed = string.Join(", ", Enum.GetNames<ProjectContext.FileType>()).ToLower();
            Console.WriteLine($"Invalid file type parameter: {o.FileType}. Allowed values: {allowed}");
            return 1;
        }
        
        if (!Enum.TryParse(o.NameFormat, out ProjectContext.FilenamePolicy filenamePolicy))
        {
            var allowed = string.Join(", ", Enum.GetNames<ProjectContext.FilenamePolicy>()).ToLower();
            Console.WriteLine($"Invalid file name format parameter: {o.NameFormat}. Allowed values: {allowed}");
            return 1;
        }

        return 0;
    }

    private static bool ValidateCoordinate(string coord)
    {
        if (!Coordinates.IsValidCoordinateString(coord))
        {
            Console.WriteLine($"Invalid coordinates: {coord}");
            return false;
        }

        return true;
    }
    
    public static void MakeImage(int tileCountX, int tileCountY, Image<Rgba32>[] tiles)
    {
        var width = tileCountX * Tiles.TILE_SIZE;
        var height = tileCountY * Tiles.TILE_SIZE;

        using var newImage = new Image<Rgba32>(width, height);
        for (var j = 0; j < tiles.Length; j++)
        {
            var jCopy = j;
            newImage.Mutate(o => o.DrawImage(tiles[jCopy], new Point(jCopy % width, jCopy / width), 1f));
        }

        var encoder = new PngEncoder {
            CompressionLevel = PngCompressionLevel.BestCompression,
            BitDepth = PngBitDepth.Bit8
        };

        newImage.Save(ProjectContext != null ? ProjectContext.GetNextFilename() : "output.png", encoder);
    }
}
