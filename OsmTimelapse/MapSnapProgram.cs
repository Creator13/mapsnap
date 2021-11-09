using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using ConsoleTools;
using Microsoft.Extensions.DependencyInjection;
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

    public static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection().AddHttpClient();
        services.AddHttpClient<ITileDownloaderHttpClient, TileDownloaderHttpClient>();
        ServiceProvider = services.BuildServiceProvider();

        // Parser.Default.ParseArguments<NewOptions, object>(args).MapResult(
        //     (NewOptions o) => NewCommand(o),
        //     errors => 1);

        var rootCommand = new RootCommand();
        var initCommand = new Command("init") {
            new Argument<string>("name"),
            new Argument<string>("coordA"),
            new Argument<string>("coordB"),
            new Argument<int>("zoom"),
            new Option<ProjectContext.FileType>(
                new[] { "--file-type", "-t" },
                () => ProjectContext.FileType.Png,
                "File type for output files."),
            new Option<ProjectContext.FilenamePolicy>(
                new[] { "--name-format", "-n", "-f" },
                () => ProjectContext.FilenamePolicy.Date,
                "How should the output files be named.")
        };
        initCommand.AddAlias("i");
        initCommand.AddAlias("create");
        initCommand.AddAlias("c");
        initCommand.Handler = CommandHandler.Create(InitCommandHandler);
        rootCommand.Add(initCommand);

        return rootCommand.InvokeAsync(args).Result;
        var cornerA = new Coordinates("51.9761;4.1288");
        var cornerB = new Coordinates("52.0533;4.4113");
        var zoom = 14;

        (uint x, uint y) a = (Tiles.LongToTileX(cornerA.longitude, zoom), Tiles.LatToTileY(cornerA.latitude, zoom));
        (uint x, uint y) b = (Tiles.LongToTileX(cornerB.longitude, zoom), Tiles.LatToTileY(cornerB.latitude, zoom));

        var box = new BoundingBox(a, b);

        // if (args is { Length: > 0 } && args[0] != null)
        // {
        //     if (args[0].ToLower() == "new")
        //     {
        //         var project = ProjectTools.GenerateProject("delft", box);
        //         ProjectTools.SaveProject(project);
        //
        //         return 0;
        //     }
        //     else if (args[0].ToLower() == "load")
        //     {
        //         if (!ProjectTools.HasProject())
        //         {
        //             Console.WriteLine("No project found!");
        //             return 0;
        //         }
        //
        //         ProjectContext = ProjectTools.LoadProject();
        //         ProjectContext.LogDetectedProject();
        //
        //         box = ProjectContext.Area;
        //     }
        // }

        Console.WriteLine($"Corner A URL: {Tiles.GetTileUrl(a, zoom)}");
        Console.WriteLine($"Corner B URL: {Tiles.GetTileUrl(b, zoom)}");

        Console.WriteLine(box.ToString());
        Console.WriteLine($"Final image size: {box.Width * 256}x{box.Height * 256}px ({box.Area * 256L * 256L / 1_000_000.0:#,0.0}MP)");

        if (box.Area * 256L * 256L > 100_000_000L)
        {
            var result = ConsoleFunctions.ConfirmPrompt(
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

    // public static int NewCommand(NewOptions o)
    // {
    //     Console.WriteLine($"Name: {o.Name}");
    //     Console.WriteLine($"CoordA: {o.CoordA}");
    //     Console.WriteLine($"CoordB: {o.CoordB}");
    //     Console.WriteLine($"FileType: {o.FileType}");
    //     Console.WriteLine($"NameFormat: {o.NameFormat}");
    //
    //     if (!(ValidateCoordinate(o.CoordA) && ValidateCoordinate(o.CoordB)))
    //     {
    //         return 1;
    //     }
    //     
    //     var coordA = new Coordinates(o.CoordA);
    //     var coordB = new Coordinates(o.CoordB);
    //     
    //     if (!Enum.TryParse(o.FileType, out ProjectContext.FileType fileType))
    //     {
    //         var allowed = string.Join(", ", Enum.GetNames<ProjectContext.FileType>()).ToLower();
    //         Console.WriteLine($"Invalid file type parameter: {o.FileType}. Allowed values: {allowed}");
    //         return 1;
    //     }
    //     
    //     if (!Enum.TryParse(o.NameFormat, out ProjectContext.FilenamePolicy filenamePolicy))
    //     {
    //         var allowed = string.Join(", ", Enum.GetNames<ProjectContext.FilenamePolicy>()).ToLower();
    //         Console.WriteLine($"Invalid file name format parameter: {o.NameFormat}. Allowed values: {allowed}");
    //         return 1;
    //     }
    //
    //     return 0;
    // }

    private static int InitCommandHandler(string name, string coordA, string coordB, int zoom, 
        ProjectContext.FileType fileType, ProjectContext.FilenamePolicy nameFormat)
    {
        Console.WriteLine($"Name: {name}");
        Console.WriteLine($"CoordA: {coordA}");
        Console.WriteLine($"CoordB: {coordB}");
        Console.WriteLine($"FileType: {fileType}");
        Console.WriteLine($"NameFormat: {nameFormat}");

        // var project = ProjectTools.LoadProject(name);

        switch (ProjectTools.ProjectExists(name))
        {
            case ProjectTools.ProjectExistenceMatch.NoMatch:
                break;
            case ProjectTools.ProjectExistenceMatch.MatchingFolderEmpty:
                if (!ConsoleFunctions.ConfirmPrompt($"An empty folder named /{name} already exists. Create project inside?", true))
                {
                    return 0;
                }
                break;
            case ProjectTools.ProjectExistenceMatch.MatchingFolderNotEmpty:
                if (!ConsoleFunctions.ConfirmPrompt($"An folder named {name} already exists and contains files. Create project inside?", true))
                {
                    return 0;
                }
                break;
            case ProjectTools.ProjectExistenceMatch.MatchingFile:
                if (!ConsoleFunctions.ConfirmPrompt($"A different project already exists in folder /{name}. Overwrite?", false))
                {
                    return 0;
                }
                break;
            case ProjectTools.ProjectExistenceMatch.MatchingFileInvalid:
                if (!ConsoleFunctions.ConfirmPrompt($"Found a corrupted project in folder /{name}. Overwrite?", true))
                {
                    return 0;
                }
                break;
            case ProjectTools.ProjectExistenceMatch.MatchingFileAndName:
                if (!ConsoleFunctions.ConfirmPrompt($"A project named \"{name}\" already exists! Overwrite with new settings?", false))
                {
                    return 0;
                }
                break;
            default:
                throw new Exception("Unreachable default reached in enum switch statement.");
        }
        
        if (!(ValidateCoordinate(coordA) && ValidateCoordinate(coordB))) return 1;

        ProjectContext = new ProjectContext(coordA, coordB, zoom) {
            Name = name,
            OutputFileType = fileType,
            OutputFilenamePolicy = nameFormat
        };

        ProjectTools.SaveProject(ProjectContext);

        return 0;
    }

    private static bool ValidateCoordinate(string coords)
    {
        if (!Coordinates.IsValidCoordinateString(coords))
        {
            Console.Error.WriteLine($"Invalid coordinates: {coords}");
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
            newImage.Mutate(o =>
            {
                o.DrawImage(tiles[jCopy], new Point(jCopy % width, jCopy / width), 1f);
            });
        }

        var encoder = new PngEncoder {
            CompressionLevel = PngCompressionLevel.DefaultCompression,
            BitDepth = PngBitDepth.Bit8
        };

        newImage.Save(ProjectContext != null ? ProjectContext.GetNextFilename() : "output.png", encoder);
    }
}
