﻿using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConsoleTools;
using mapsnap.Projects;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace mapsnap;

public static class MapSnapProgram
{
    public static ServiceProvider ServiceProvider { get; private set; }
    private static ProjectContext ProjectContext { get; set; }
    private static TileServer TileServer => TileServer.defaultTileServer;

    public static async Task<int> Main(string[] args)
    {
        // GifColors();
        // return 0; 
        
        return await GifCommandHandler(null);

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
            new Argument<string>("coordB") {
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

        var validation = ValidateProjectContextBeforeCommand(ProjectContext);
        if (validation != 0) return validation;

        Console.WriteLine($"Corner A tile URL: {TileServer.GetTileUrl(box.TopLeft, zoom)}");
        Console.WriteLine($"Corner B tile URL: {TileServer.GetTileUrl(box.BottomRight, zoom)}");

        Console.WriteLine(box.ToString());
        Console.WriteLine($"Final image size: {box.Width * 256}x{box.Height * 256}px ({box.Area * 256L * 256L / 1_000_000.0:#,0.0}MP)");

        // Ask user to confirm creating if the output resolution >100MP
        // Converting the Area to long will prevent possible overflow
        // (The largest theoretical size of this number is 2^(2*19) * 256^2 = 2^54, well above int.MaxValue)
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

    private static int LoadProjectContext(string project = null)
    {
        ProjectContext projectCtx;
        bool result;

        // If a project name is provided in the command, try to find it.
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
        // If no name is given, detect a project in the current folder (assumes this is a project root folder), and fail if no project was found.
        else
        {
            if (!ProjectTools.WorkingDirIsProjectRoot())
            {
                Console.Error.WriteLine("Not in a project directory!");
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

        return 0;
    }

    private static async Task<int> SnapCommandHandler(string project)
    {
        if (ProjectContext == null)
        {
            var result = LoadProjectContext(project);
            if (result != 0)
            {
                return result;
            }
        }

        var box = ProjectContext.Area;

        var validation = ValidateProjectContextBeforeCommand(ProjectContext);
        if (validation != 0) return validation;

        Console.WriteLine(box.ToString());
        Console.WriteLine($"Final image size: {box.Width * 256}x{box.Height * 256}px ({box.Area * 256L * 256L / 1_000_000.0:#,0.0}MP)");

        var tiles = await TileDownloaderHttpClient.DownloadTiles(TileServer, box, ProjectContext.Zoom);

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

    private static async Task<int> GifCommandHandler(string project)
    {
        if (ProjectContext == null)
        {
            var result = LoadProjectContext(project);
            if (result != 0)
            {
                return result;
            }
        }

        var regex = ProjectContext.OutputFilenamePolicy switch {
            ProjectContext.FilenamePolicy.Index => @"[\s\S]*" + ProjectContext.Name + @"\d+.(?:jpg|png)",
            ProjectContext.FilenamePolicy.Date => @"[\s\S]*" + ProjectContext.Name + @" \d{4}-\d{2}-\d{2} \d{2}_\d{2}_\d{2}.(?:jpg|png)",
            _ => "(?!)" // regex matches nothing, but this is in theory unreachable unless someone fucks with enums.
        };
        
        var files = Directory.EnumerateFiles(Environment.CurrentDirectory)
                             .Where(file => Regex.IsMatch(file, regex))
                             .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No snapshots found in current project. Please capture a few snapshots and try again later.");
            return 1;
        }

        if (files.Count == 1)
        {
            // TODO might prompt user to take a snapshot now, or might include that as a command parameter.
            Console.WriteLine("Only 1 snapshot found in current project. Please capture one or more snapshots and try again.");
            return 1;
        }

        Console.Write($"Found {files.Count} snapshots in this project. Creating GIF: ");
        var progressBar = new ProgressBar(files.Count + 1);

        var width = (int) ProjectContext.Area.Width * Tiles.TILE_SIZE;
        var height = (int) ProjectContext.Area.Height * Tiles.TILE_SIZE;
        
        var outputGif = MakeGif(width, height, files, ref progressBar);
        progressBar.Dispose();
        
        Console.WriteLine("Saving...");
        await outputGif.SaveAsGifAsync("outputGif.gif");
        Console.WriteLine("Done.");

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

    private static int ValidateProjectContextBeforeCommand(ProjectContext ctx)
    {
        if (!TileServer.ValidateZoom(ctx.Zoom))
        {
            Console.Error.WriteLine(
                $"Tile server {TileServer.ServerUrl} does not support requested zoom level of {ctx.Zoom}. Must be between {TileServer.MinZoom} and {TileServer.MaxZoom} (inclusive)");
            return 1;
        }

        if (!TileServer.ValidateAreaSize(ctx.Area.Area, ctx.Zoom))
        {
            Console.Error.WriteLine(
                $"This area has a size of {ctx.Area.Area}. The OSM server usage policy disallows downloading areas of over {TileServer.MaxArea} at zoom level {TileServer.UnlimitedAreaMaxZoom} or higher. Please choose a smaller area.");
            Console.Error.WriteLine("More info: https://operations.osmfoundation.org/policies/tiles/#bulk-downloading");
            return 1;
        }

        return 0;
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

    private static Image<Rgba32> MakeGif(int width, int height, IEnumerable<string> sourceFilePaths, ref ProgressBar progressBar, int frameDelay = 50)
    {
        Image<Rgba32> gif = new(width, height, Color.Magenta);
        gif.Metadata.GetGifMetadata().RepeatCount = 0;

        var count = 0;
        foreach (var filePath in sourceFilePaths)
        {
            using var frame = Image.Load<Rgba32>(filePath);

            frame.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelay;

            gif.Frames.AddFrame(frame.Frames.RootFrame);
            
            progressBar.Report(++count);
        }

        gif.Frames.RemoveFrame(0);
        
        gif.Frames[^1].Metadata.GetGifMetadata().FrameDelay = frameDelay * 2;
        gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelay * 2;
        
        return gif;
    }

    private static IEnumerator<Image<Rgba32>> GetNextLoadedFrame()
    {
        return null;
    }
}
