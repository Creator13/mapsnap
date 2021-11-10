using System;
using System.IO;

namespace OsmTimelapse.Projects;

internal interface IFilenameFormatter
{
    string Format(string basename, ProjectContext.FileType type);
}

internal class DateFilenameFormatter : IFilenameFormatter
{
    public string Format(string baseName, ProjectContext.FileType type)
    {
        var date = DateTime.Now;
        var dateString = $"{date:s}".Replace(':', '_').Replace('T', ' ');
        return $"{baseName}{(!string.IsNullOrEmpty(baseName) ? " " : "")}{dateString}";
    }
}

internal class IndexFilenameFormatter : IFilenameFormatter
{
    public string Format(string baseName, ProjectContext.FileType type)
    {
        var index = 0;
        string filename;

        do
        {
            filename = $"{baseName}{index++}";
        } while (File.Exists(filename.AddExtension(type)));

        return filename;
    }
}
