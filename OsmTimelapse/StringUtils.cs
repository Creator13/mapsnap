using System.Linq;

namespace mapsnap.Utils;

public static class StringUtils
{
    public const string ATTRIBUTION_TEXT = "Thank you for using mapsnap!" +
                                           "\nMap data & tiles are (©) OpenStreetMap contributors. Images created with mapsnap are CC BY-SA 2.0." +
                                               "\n    More license info: https://www.openstreetmap.org/copyright" +
                                               "\n    Want to contribute? https://www.openstreetmap.org/fixthemap" +
                                           "\nThis free and open-source (GPL-3.0) tool may be used for small-scale, personal use only." +
                                           "\nPlease respect the OpenStreetMap project, its contributors and the developers of this tool by not using it for bulk downloads." +
                                           "\nThis app respects the tile server usage policy as closely as possible: https://operations.osmfoundation.org/policies/tiles/.";

    public static string ToSnakeCase(this string str)
    {
        return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x : x.ToString())).ToLower();
    }

    public static string FormatElapsedTime(long elapsedMilliseconds) => elapsedMilliseconds < 1000
        ? $"{elapsedMilliseconds:#,0}ms"
        : $"{elapsedMilliseconds / 1000d:#,0.000}s";

    public static string FormatKB(long bytes) => $"{bytes / 1024f:#,0.0}kB";
}
