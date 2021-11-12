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
    private static ProjectContext ProjectContext { get; set; }

    public static async Task<int> Main(string[] args)
    {
        var services = new ServiceCollection().AddHttpClient();
        services.AddHttpClient<ITileDownloaderHttpClient, TileDownloaderHttpClient>();
        ServiceProvider = services.BuildServiceProvider();

        var rootCommand = new RootCommand();

        var initCommand = new Command("init") {
            new Argument<string>("name") {
                Description = "Five your project a name."
            },
            new Argument<string>("coordA") {
                Description = "A corner of the bounding box of the area you want to capture (decimal coordinates)."
            },
            new Argument<string>("coordB"){
                Description = "A corner of the bounding box of the area you want to capture (decimal coordinates)."
            },
            new Argument<int>("zoom") {
                Description = "The zoom level of the image you want to capture."
            },
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

        var snapCommnand = new Command("snap") {
            new Option<string>(
                new[] { "--project", "-p" },
                "Specify which project to work in relative to the current working directory")
        };
        snapCommnand.AddAlias("s");
        snapCommnand.Handler = CommandHandler.Create(SnapCommandHandler);
        rootCommand.Add(snapCommnand);

        return await rootCommand.InvokeAsync(args);
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
        // Check if the coordinates are valid
        if (!(ValidateCoordinate(coordA) && ValidateCoordinate(coordB))) return 1;

        // Confirm what to do if the project already exists in some way.
        var existence = ProjectTools.ProjectExists(name);
        if (existence != ProjectTools.ProjectExistenceMatch.NoMatch)
        {
            var (prompt, defaultVal) = existence switch {
                ProjectTools.ProjectExistenceMatch.MatchingFolderEmpty =>
                    ($"An empty folder named /{name} already exists. Create project inside?", true),
                ProjectTools.ProjectExistenceMatch.MatchingFolderNotEmpty =>
                    ($"An folder named {name} already exists and contains files. Create project inside?", true),
                ProjectTools.ProjectExistenceMatch.MatchingFile =>
                    ($"A different project already exists in folder /{name}. Overwrite?", false),
                ProjectTools.ProjectExistenceMatch.MatchingFileInvalid =>
                    ($"Found an invalid project in folder /{name}. Overwrite?", true),
                ProjectTools.ProjectExistenceMatch.MatchingFileAndName =>
                    ($"A project named \"{name}\" already exists! Overwrite with new settings?", false),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!ConsoleFunctions.ConfirmPrompt(prompt, defaultVal))
            {
                return 0;
            }   
        }

        ProjectContext = new ProjectContext(coordA, coordB, zoom) {
            Name = name,
            OutputFileType = fileType,
            OutputFilenamePolicy = nameFormat
        };

        var box = ProjectContext.Area;

        Console.WriteLine(box.ToString());
        Console.WriteLine($"Final image size: {box.Width * 256}x{box.Height * 256}px ({box.Area * 256L * 256L / 1_000_000.0:#,0.0}MP)");

        // Ask user to confirm creating if the output resolution >100MP
        // Converting the Area to long will prevent possible overflow
        // The largest theoretical size of this number is 2^(2*19) * 256^2 = 2^54, well above int.MaxValue
        if ((long)ProjectContext.Area.Area * Tiles.TILE_SIZE * Tiles.TILE_SIZE > 100_000_000L)
        {
            var result = ConsoleFunctions.ConfirmPrompt(
                "The output images will be over 100MP, resulting in very large files. Are you sure you want to continue?",
                false);

            if (!result)
            {
                Console.WriteLine("Try lowering the zoom value to get a smaller image, or choose a smaller region.");
                return 0;
            }
        }

        ProjectTools.SaveProject(ProjectContext);

        return 0;
    }

    private static async Task<int> SnapCommandHandler(string project)
    {
        ProjectContext projectCtx;
        bool result;
        if (!string.IsNullOrEmpty(project))
        {
            if (!ProjectTools.HasProject(project))
            {
                Console.Error.WriteLine($"No project named {project} found!");
                return 1;
            }

            // Simplest way to prefix all files with the project directory is to just switch the program to work in that directory.
            Environment.CurrentDirectory = project;
            
            result = ProjectTools.LoadProject(out projectCtx);
        }
        else
        {
            if (!ProjectTools.HasProject())
            {
                Console.Error.WriteLine("Helo u r not in a project");
                return 1;
            }

            result = ProjectTools.LoadProject(out projectCtx);
        }

        if (!result)
        {
            Console.Error.WriteLine("Invalid project file.");
            return 1;
        }

        ProjectContext = projectCtx;

        var box = ProjectContext.Area;

        Console.WriteLine(box.ToString());
        Console.WriteLine($"Final image size: {box.Width * 256}x{box.Height * 256}px ({box.Area * 256L * 256L / 1_000_000.0:#,0.0}MP)");

        var tiles = await TileDownloaderHttpClient.DownloadTiles(box, ProjectContext.Zoom);

        Console.WriteLine("Saving image (this may take a while if you're snapping a large area)...");
        using var image = MakeImage((int)box.Width, (int)box.Height, tiles);

        var encoder = new PngEncoder {
            CompressionLevel = PngCompressionLevel.DefaultCompression,
            BitDepth = PngBitDepth.Bit8
        };

        var name = ProjectContext != null ? ProjectContext.GetNextFilename() : "output.png";

        await image.SaveAsync(name, encoder);

        Console.WriteLine($"Saved snapshot as {name}!");

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

    private static Image<Rgba32> MakeImage(int tileCountX, int tileCountY, Image<Rgba32>[] tiles)
    {
        var width = tileCountX * Tiles.TILE_SIZE;
        var height = tileCountY * Tiles.TILE_SIZE;

        var newImage = new Image<Rgba32>(width, height);
        for (var j = 0; j < tiles.Length; j++)
        {
            var jCopy = j;
            newImage.Mutate(o =>
                o.DrawImage(tiles[jCopy], new Point(jCopy % tileCountX * Tiles.TILE_SIZE, jCopy / tileCountX * Tiles.TILE_SIZE), 1f));
        }

        return newImage;
    }
}
