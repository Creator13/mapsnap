#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using mapsnap.Utils;

namespace mapsnap.Projects;

/**
 * Intermediate class for converting JSON to ProjectContext, with added logic for project file version interoperability.
 * TODO: merge this with context using a custom serialization method, and/or refactor this to ProjectSettings
 */
internal class ProjectSaveData
{
    // Version history:
    // 1: Added "name", "area: {origin: {item1, item2}, width, height}", "zoom", "output_filename_policy", "output_file_type"
    // 2: Added "version"
    public const int CURRENT_VERSION = 2;

    public int? Version { get; set; }
    public string Name { get; set; } = "";
    public BoundingBox Area { get; set; }
    public int Zoom { get; set; }
    public ProjectContext.FilenamePolicy OutputFilenamePolicy { get; set; } = ProjectContext.FilenamePolicy.Date;
    public ProjectContext.FileType OutputFileType { get; set; } = ProjectContext.FileType.Png;

    public static implicit operator ProjectSaveData(ProjectContext ctx) =>
        new() {
            Version = ctx.Version,
            Name = ctx.Name,
            Area = ctx.Area,
            Zoom = ctx.Zoom,
            OutputFilenamePolicy = ctx.OutputFilenamePolicy,
            OutputFileType = ctx.OutputFileType,
        };

    public static explicit operator ProjectContext(ProjectSaveData saveData) =>
        new() {
            Version = saveData.Version ?? 1,
            Name = saveData.Name,
            Area = saveData.Area,
            Zoom = saveData.Zoom,
            OutputFilenamePolicy = saveData.OutputFilenamePolicy,
            OutputFileType = saveData.OutputFileType,
        };
}

public static class ProjectTools
{
    public enum ProjectExistenceMatch
    {
        /**
         * No mapsnap.json file was found in the current working directory, nor was a directory with the project name found.
         */
        NoMatch,

        /**
         * A folder with the name of this project was found in the current working directory, but it was completely empty.
         */
        MatchingFolderEmpty,

        /**
         * A folder with the name of this project was found in the current working directory, and it is not empty.
         */
        MatchingFolderNotEmpty,

        /**
         * A valid mapsnap.json file was found in the current working directory, but the name recorded in the json did not match the provided project name.
         */
        MatchingFile,

        /**
         * An invalid mapsnap.json file was found in the current working directory.
         */
        MatchingFileInvalid,

        /**
         * A valid mapsnap.json project that matches the provided project name was found in the current working directory.
         */
        MatchingFileAndName
    }

    public const string PROJECT_FILE_NAME = "mapsnap.json";

    private static readonly JsonSerializerOptions serializerOptions = new() {
        PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
        Converters = { new JsonStringEnumConverter(new SnakeCaseNamingPolicy()) },
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        IncludeFields = true,
        IgnoreReadOnlyProperties = true
    };

    public static bool WorkingDirIsProjectRoot()
    {
        return File.Exists($@"{PROJECT_FILE_NAME}");
    }

    public static bool InProjectDirectory(string projectName)
    {
        return ProjectExists(projectName) == ProjectExistenceMatch.MatchingFileAndName;
    }

    public static ProjectExistenceMatch ProjectExists(string projectName)
    {
        var path = $@"{projectName}/{PROJECT_FILE_NAME}";

        if (File.Exists(path))
        {
            if (LoadProject(path, out var project))
            {
                if (project.Name == projectName)
                {
                    return ProjectExistenceMatch.MatchingFileAndName;
                }

                return ProjectExistenceMatch.MatchingFile;
            }

            return ProjectExistenceMatch.MatchingFileInvalid;
        }

        if (Directory.Exists(projectName))
        {
            if (Directory.EnumerateFiles(projectName).Any())
            {
                return ProjectExistenceMatch.MatchingFolderNotEmpty;
            }

            return ProjectExistenceMatch.MatchingFolderEmpty;
        }

        return ProjectExistenceMatch.NoMatch;
    }

    public static bool SaveProject(ProjectContext project)
    {
        var path = ConcatProjectFilePath(project.Name);
        if (!Directory.Exists(project.Name))
        {
            Directory.CreateDirectory(project.Name);
        }
        
        return SaveProject((ProjectSaveData)project, path);
    }

    private static bool SaveProject(ProjectSaveData project, string path)
    {
        try
        {
            using var fs = File.Create(path);
            JsonSerializer.Serialize(fs, project, serializerOptions);

            return true;
        }
        catch (Exception e) when (e is NotSupportedException or PathTooLongException)
        {
            Console.Error.WriteLine($"Invalid project name: {project.Name}!");
        }
        catch (UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Couldn't get access to project file at {path}!");
        }
        catch (Exception)
        {
            Console.Error.WriteLine($"An error occured while creating project at {path}:");
            throw;
        }

        return false;
    }

    public static bool LoadProject(string path, out ProjectContext project)
    {
        try
        {
            var jsonBytes = new ReadOnlySpan<byte>(File.ReadAllBytes(path));
            var data = JsonSerializer.Deserialize<ProjectSaveData>(jsonBytes, serializerOptions)!;
            
            project = (ProjectContext)data;

            // TODO this is an ugly side effect for this function, should be moved or refactored...
            if (project.Version != ProjectSaveData.CURRENT_VERSION)
            {
                Console.WriteLine("Updating project file to current version...");
                Console.WriteLine($"Versiobn {data.Version} {project.Version}");
                SaveProject((ProjectSaveData) project, path);
            }
            
            return true;
        }
        catch (Exception e) when (e is NotSupportedException or JsonException)
        {
            Console.WriteLine(e);
            project = null!;
            return false;
        }
    }

    public static bool LoadProject(out ProjectContext project)
    {
        return LoadProject(PROJECT_FILE_NAME, out project);
    }

    private static string ConcatProjectFilePath(string projectName)
    {
        return $@"{projectName}/{PROJECT_FILE_NAME}";
    }

    private class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToSnakeCase();
        }
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
