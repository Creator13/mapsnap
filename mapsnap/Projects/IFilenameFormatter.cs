using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace mapsnap.Projects;

internal interface IFilenameFormatter
{
    string Format(string basename, MapsnapProject.FileType type);
    string Format(string baseName, string extension);
}

internal class DateFilenameFormatter : IFilenameFormatter
{
    public string Format(string baseName, MapsnapProject.FileType type)
    {
        return Format(baseName, type.FileExtension());
    }

    public string Format(string baseName, string extension)
    {
        var date = DateTime.Now;
        var dateString = $"{date:s}".Replace(':', '_').Replace('T', ' ');
        return $"{baseName}{(!string.IsNullOrEmpty(baseName) ? " " : "")}{dateString}.{extension}";
    }
}

internal class IndexFilenameFormatter : IFilenameFormatter
{
    // FIXME It would be nicer if this class was initialized with the current number, although that doesn't allow it to respond dynamically
    // (in the current context of a console application, dynamic isn't needed at all)
    
    public string Format(string baseName, MapsnapProject.FileType type)
    {
        return Format(baseName, type.FileExtension());
    }

    public string Format(string baseName, string extension)
    {
        var files = Directory.EnumerateFiles(Environment.CurrentDirectory)
                             .Where(path => Regex.IsMatch(path, $"[\\s\\S]*{baseName}\\d+.(?:jpg|png)"))
                             .ToList();
        var number = 0;
        if (files.Count > 0)
        {
            var lastFile = files
                           .OrderByDescending(s => s, StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreCase))
                           .First();
            number = int.Parse(Regex.Match(lastFile, $"{baseName}(\\d+).(?:jpg|png)").Groups[1].Value);
        }
        
        return $"{baseName}{number + 1}.{extension}";
    }
}

internal static class FileTypeExtensions
{
    /**
     * Returns the file extension for this file type.
     */
    public static string FileExtension(this MapsnapProject.FileType fileType)
    {
        return fileType.ToString().ToLower();
    }
}
