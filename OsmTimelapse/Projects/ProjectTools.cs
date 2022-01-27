using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using cvanbattum.Utils;

namespace mapsnap.Projects;

public static class ProjectTools
{
    public enum ProjectExistenceMatch
    {
        NoMatch,
        MatchingFolderEmpty,
        MatchingFolderNotEmpty,
        MatchingFile,
        MatchingFileInvalid,
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

    public static bool HasProject(string name)
    {
        return ProjectExists(name) == ProjectExistenceMatch.MatchingFileAndName;
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

    public static void SaveProject(ProjectContext project)
    {
        var path = CreateProjectFilePath(project.Name);
        try
        {
            Directory.CreateDirectory(project.Name);
            using var fs = File.Create(path);
            JsonSerializer.Serialize(fs, project, serializerOptions);

            Console.WriteLine($"Successfully created project \"{project.Name}\" in folder {project.Name}/");
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
    }

    public static bool LoadProject(string path, out ProjectContext project)
    {
        try
        {
            var jsonBytes = new ReadOnlySpan<byte>(File.ReadAllBytes(path));
            project = JsonSerializer.Deserialize<ProjectContext>(jsonBytes, serializerOptions);
            return true;
        }
        catch (Exception e) when (e is NotSupportedException or JsonException)
        {
            Console.WriteLine(e);
            project = null;
            return false;
        }
    }

    public static bool LoadProject(out ProjectContext project)
    {
        return LoadProject(PROJECT_FILE_NAME, out project);
    }

    private static string CreateProjectFilePath(string projectName)
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
