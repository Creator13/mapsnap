﻿using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ConsoleTools;

namespace OsmTimelapse;

#nullable enable
[Serializable]
public record ProjectContext
{
    private delegate string FilenameFormatter(string basename, FileType type);

    public enum FilenamePolicy { Index, Date }

    public enum FileType { Png, Jpg, }

    public string Name { get; init; } = "";
    public BoundingBox Area { get; init; }

    public FilenamePolicy OutputFilenamePolicy { get; init; } = FilenamePolicy.Date;
    public FileType OutputFileType { get; init; } = FileType.Png;

    public string GetNextFilename()
    {
        FilenameFormatter formatter;

        switch (OutputFilenamePolicy)
        {
            case FilenamePolicy.Index:
                formatter = GetIndexFilename;
                break;
            case FilenamePolicy.Date:
            default:
                formatter = GetDateFilename;
                break;
        }

        return AddExtension(formatter(Name, OutputFileType), OutputFileType);
    }

    private static string GetIndexFilename(string baseName, FileType type)
    {
        var index = 0;
        string filename;

        do
        {
            filename = $"{baseName}{index++}";
        } while (File.Exists(AddExtension(filename, type)));

        return filename;
    }

    private static string GetDateFilename(string baseName, FileType type)
    {
        var date = DateTime.Now;
        var dateString = $"{date:s}".Replace('_', '_').Replace('T', ' ');
        return $"{baseName}{(!string.IsNullOrEmpty(baseName) ? " " : "")}{dateString}";
    }

    private static string AddExtension(string filename, FileType type)
    {
        return $"{filename}.{type.ToString().ToLower()}";
    }
}
#nullable restore

public class SnakeCaseNamingPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name) => name.ToSnakeCase();
}

public static class ProjectTools
{
    private const string PROJECT_FILE_NAME = "mapsnap.json";

    private static readonly JsonSerializerOptions serializerOptions = new() {
        PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        Converters = { new JsonStringEnumConverter(new SnakeCaseNamingPolicy()) },
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        IncludeFields = true,
        IgnoreReadOnlyProperties = true
    };

    private static string ApplicationPath => Environment.CurrentDirectory;

    public static bool HasProject()
    {
        return File.Exists($@"{PROJECT_FILE_NAME}");
    }

    public static ProjectContext GenerateProject(string name, Coordinates cornerA, Coordinates cornerB, int zoom)
    {
        (uint x, uint y) a = (Tiles.LongToTileX(cornerA.longitude, zoom), Tiles.LatToTileY(cornerA.latitude, zoom));
        (uint x, uint y) b = (Tiles.LongToTileX(cornerB.longitude, zoom), Tiles.LatToTileY(cornerB.latitude, zoom));

        var box = new BoundingBox(a, b);

        return GenerateProject(name, box);
    }

    public static ProjectContext GenerateProject(string name, BoundingBox area)
    {
        return new ProjectContext {
            Name = name,
            Area = area
        };
    }

    public static void SaveProject(ProjectContext context)
    {
        // TODO move this somewhere else because user interaction is not supposed to be a responsibility of this class
        if (HasProject())
        {
            if (!ConsoleFunctions.ConsoleConfirm("Are you sure you want to create a new project in an existing project folder?", true))
            {
                return;
            }
        }

        using var fs = File.Create($@"{context.Name}/{PROJECT_FILE_NAME}");
        JsonSerializer.SerializeAsync(fs, context, serializerOptions);

        Console.WriteLine($"Successfully created project \"{context.Name}\"!");
    }

    public static ProjectContext LoadProject()
    {
        var jsonBytes = new ReadOnlySpan<byte>(File.ReadAllBytes(PROJECT_FILE_NAME));
        var deserializedProject = JsonSerializer.Deserialize<ProjectContext>(jsonBytes, serializerOptions);
        return deserializedProject;
    }
}

public static class ProjectContextExtensions
{
    public static void LogDetectedProject(this ProjectContext project)
    {
        Console.WriteLine($"Detected project called {project.Name}! Using project settings.");
        Console.WriteLine($"{project.OutputFilenamePolicy.ToString()} {project.OutputFileType.ToString()}");
        Console.WriteLine(project.Area);
    }
}
