using System.Globalization;
using System.Text.RegularExpressions;

namespace mapsnap;

public struct Coordinates
{
    // Regex courtesy of https://stackoverflow.com/a/18690202/2274782 with a few tweaks
    private const string DECIMAL_COORDINATE_PATTERN =
        @"(^[-+]?(?:[1-8]?\d(?:\.\d+)?|90(?:\.0+)?)[°]?)\s*[, ;]\s*([-+]?(?:180(?:\.0+)?|(?:(?:1[0-7]\d)|(?:[1-9]?\d))(?:\.\d+)?)[°]?)$";

    public double latitude;
    public double longitude;

    public Coordinates(string latLongCoords)
    {
        var parsed = ParseCoordinateString(latLongCoords);

        latitude = parsed[0];
        longitude = parsed[1];
    }

    public Coordinates(double latitude, double longitude)
    {
        this.latitude = latitude;
        this.longitude = longitude;
    }

    public override string ToString()
    {
        // \u00b0 = °
        return $"{latitude.ToString(CultureInfo.InvariantCulture)}\u00B0 {longitude.ToString(CultureInfo.InvariantCulture)}\u00B0";
    }

    private static double[] ParseCoordinateString(string latLongCoords)
    {
        var result = new double[2];

        var match = Regex.Match(latLongCoords, DECIMAL_COORDINATE_PATTERN);
        result[0] = double.Parse(match.Groups[1].Value);
        result[1] = double.Parse(match.Groups[2].Value);

        return result;
    }

    public static bool IsValidCoordinateString(string coordString)
    {
        return Regex.IsMatch(coordString, DECIMAL_COORDINATE_PATTERN);
    }
}
