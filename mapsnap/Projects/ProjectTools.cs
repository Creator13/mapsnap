#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using mapsnap.Utils;

namespace mapsnap.Projects;

public static class ProjectTools
{
    // Version history:
    // 1: Added "name", "area: {origin: {item1, item2}, width, height}", "zoom", "output_filename_policy", "output_file_type"
    // {
    //     "name": "tajo-es",
    //     "area": {
    //         "origin": {
    //             "item1": 8033,
    //             "item2": 6197
    //         },
    //         "width": 6,
    //         "height": 5
    //     },
    //     "zoom": 14,
    //     "output_filename_policy": "date",
    //     "output_file_type": "png"
    // }
    // 2: Added "version"
    // {
    //     "version": 1,
    //     "name": "tajo-es",
    //     "area": {
    //         "origin": {
    //             "item1": 8033,
    //             "item2": 6197
    //         },
    //         "width": 6,
    //         "height": 5
    //     },
    //     "zoom": 14,
    //     "output_filename_policy": "date",
    //     "output_file_type": "png"
    // }
    // 3: Save coordinates instead of area
    // {
    //     "version": 1,
    //     "name": "tajo-es",
    //     "coordinates":[
    //         {
    //             "lon": 6.234,
    //             "lat": 7.324
    //         },
    //         {
    //             "lon": 6.234,
    //             "lat": 7.324
    //         }
    //     ],
    //     "zoom": 14,
    //     "output_filename_policy": "date",
    //     "output_file_type": "png"
    // }
    public const int CURRENT_SAVE_VERSION = 3;
    
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

    private static readonly JsonNamingPolicy namingPolicy = new SnakeCaseNamingPolicy();
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

    public static bool SaveProject(MapsnapProject project)
    {
        var path = ConcatProjectFilePath(project.Name);
        if (!Directory.Exists(project.Name))
        {
            Directory.CreateDirectory(project.Name);
        }
        
        return SaveProject(project, path);
    }

    private static bool SaveProject(MapsnapProject project, string path)
    {
        try
        {
            var projectJsonObj = new JsonObject {
                [namingPolicy.ConvertName(nameof(MapsnapProject.Version))] = project.Version,
                [namingPolicy.ConvertName(nameof(MapsnapProject.Name))] = project.Name,
                [namingPolicy.ConvertName(nameof(MapsnapProject.Zoom))] = project.Zoom,
                [namingPolicy.ConvertName(nameof(MapsnapProject.OutputFileType))] = namingPolicy.ConvertName(project.OutputFileType.ToString()),
                [namingPolicy.ConvertName(nameof(MapsnapProject.OutputFilenamePolicy))] = namingPolicy.ConvertName(project.OutputFilenamePolicy.ToString()),
            };

            if (project.Version <= 2)
            {
                // Honestly this is so dirty but it's literally the only simple way.
                var areaJsonObject = JsonSerializer.SerializeToNode(project.Area, serializerOptions);
                projectJsonObj[namingPolicy.ConvertName(nameof(MapsnapProject.Area))] = areaJsonObject;
            }
            else // Version >= 3
            {
                var coordArray = new JsonArray();
                foreach (var coords in new[] {project.coordsA, project.coordsB})
                {
                    coordArray.Add(JsonSerializer.SerializeToNode(coords, serializerOptions));
                }

                projectJsonObj[namingPolicy.ConvertName("Coordinates")] = coordArray;
            }
            
            using var fs = File.Create(path);
            JsonSerializer.Serialize(fs, projectJsonObj, serializerOptions);

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

    public static bool LoadProject(string path, out MapsnapProject project)
    {
        try
        {
            using var fileStream = File.OpenRead(path);
            using var document = JsonDocument.Parse(fileStream);
            var root = document.RootElement;

            // Default version is 1; this version does not yet have the version property.
            var version = root.TryGetProperty("version", out var versionElement) ? versionElement.GetInt32() : 1;

            var name = root.GetProperty("name").GetString();
            var zoom = root.GetProperty("zoom").GetInt32();
            var fileType = Enum.Parse<MapsnapProject.FileType>(root.GetProperty("output_file_type").GetString()!, serializerOptions.PropertyNameCaseInsensitive);
            var filenamePolicy = Enum.Parse<MapsnapProject.FilenamePolicy>(root.GetProperty("output_filename_policy").GetString()!, serializerOptions.PropertyNameCaseInsensitive);
            
            if (version <= 2)
            {
                var area = root.GetProperty("area").Deserialize<BoundingBox>(serializerOptions);
                project = new MapsnapProject {
                    Version = version,
                    Name = name,
                    Zoom = zoom,
                    OutputFileType = fileType,
                    OutputFilenamePolicy = filenamePolicy,
                    Area = area,
                    PixelOffsets = new PixelOffsets(0, 0, 0, 0),
                };
            }
            else // version >= 3 
            {
                var coords = root.GetProperty("coordinates").EnumerateArray();
                coords.MoveNext();
                var coordsA = coords.Current.Deserialize<Coordinates>(serializerOptions);
                coords.MoveNext();
                var coordsB = coords.Current.Deserialize<Coordinates>(serializerOptions);

                project = new MapsnapProject(coordsA, coordsB, zoom) {
                    Name = name,
                    OutputFileType = fileType,
                    OutputFilenamePolicy = filenamePolicy,
                };
            }
            
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            project = null!;
            return false;
        }
    }

    public static bool LoadProject(out MapsnapProject project)
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
