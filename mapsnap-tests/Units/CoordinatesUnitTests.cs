using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using mapsnap;
using Xunit;

namespace mapsnapTests.UnitTests;

public class CoordinateTestStringGenerator : IEnumerable<object[]>
{
    private readonly IEnumerable<string> separators;
    private readonly IEnumerable<(string, string)> coordinatePairs;

    public CoordinateTestStringGenerator(IEnumerable<string> separators, IEnumerable<(string, string)> coordinatePairs)
    {
        this.separators = separators;
        this.coordinatePairs = coordinatePairs;
    }

    public IEnumerator<object[]> GetEnumerator() => (
        from separator in separators
        from coords in coordinatePairs
        select new object[] { $"{coords.Item1}{separator}{coords.Item2}" }
    ).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public class CoordinatesUnitTests
{
    // TODO write tests that test exhaustively for any valid/invalid coordinate
    public static CoordinateTestStringGenerator ValidCoordinates => new(new[] {
            " ", "      ",
            ",", ";", "/",
            " ,", " ;", " /",
            ", ", "; ", "/ ",
            " , ", " ; ", " / ",
        },
        new[] {
            ("+90.0", "-127.554334"),
            ("45", "180"),
            ("-90", "-180"),
            ("-90.000", "-180.0000"),
            ("67", "25.1234"),
            ("57.432°", "25.1°"),
            ("57.432°", "25.1"),
            ("47.1231231", "179.99999999"),
            (" 7.343", "17.343"),
            ("7.343", "17.343 "),
            (" 7.343", "17.343 "),
            ("0", "0")
        });

    [Theory]
    [MemberData(nameof(ValidCoordinates))]
    public void ValidCoordinateStringsAreValid(string coordString)
    {
        Assert.True(Coordinates.IsValidCoordinateString(coordString));
    }

    public static IEnumerable<object[]> InvalidCoordinates => new List<object[]> {
        new object[] { "-90., -180." },
        new object[] { "+90.1, -100.111" },
        new object[] { "045, 180" },
        new object[] { "45, 181" },
        new object[] { "-91, 123.456" },
        new object[] { "-91, 123.456a" },
        new object[] { "asdf" },
        new object[] { "" },
        new object[] { " " },
        new object[] { " , " },
        new object[] { ";" },
    };

    [Theory]
    [MemberData(nameof(InvalidCoordinates))]
    public void InvalidCoordinateStringsAreInvalid(string coordString)
    {
        Assert.False(Coordinates.IsValidCoordinateString(coordString));
    }

    [Theory]
    [MemberData(nameof(ValidCoordinates))]
    public void CoordinateObjectConstructionThrowsNoErrorsOnValidStrings(string coordString)
    {
        // Implicitly testing that no exception is thrown;
        _ = new Coordinates(coordString);
    }
    
    [Theory]
    [MemberData(nameof(InvalidCoordinates))]
    public void CoordinateObjectConstructionThrowsErrorsOnInvalidStrings(string coordString)
    {
        Assert.Throws<ArgumentException>(() => new Coordinates(coordString));
    }

    [Theory]
    [InlineData(-90.1, -180.0)]
    [InlineData(-90.1, -180.1)]
    [InlineData(double.MaxValue, double.MaxValue)]
    [InlineData(double.MaxValue, double.MinValue)]
    [InlineData(6.453, 200)]
    [InlineData(200, 6.453)]
    public void LiteralConstructorRejectsOutOfBoundsCoordinates(double lat, double lon)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Coordinates(lat, lon));
    }
    
    [Theory]
    [InlineData(42, 170)]
    [InlineData(42.235, 170.3462)]
    [InlineData(90, 180)]
    [InlineData(-90, -180)]
    [InlineData(-90.0, -180.0)]
    [InlineData(0, 0)]
    public void LiteralConstructorAcceptsCoordinatesInBounds(double lat, double lon)
    {
        // Implicitly testing that no exception is thrown
        _ = new Coordinates(lat, lon);
    }

    [Theory]
    [InlineData("67, 25.1234", 67, 25.1234)]
    [InlineData("67.54512, -25", 67.54512, -25)]
    [InlineData("-67; 25", -67, 25)]
    [InlineData("-90/ 25", -90, 25)]
    [InlineData("-67 25", -67, 25)]
    [InlineData("-67° 25°", -67, 25)]
    [InlineData("-67, 25.1234", -67, 25.1234)]
    [InlineData("-67, -25.1234", -67, -25.1234)]
    [InlineData("67, -25.1234", 67, -25.1234)]
    [InlineData("0, 0", 0, 0)]
    public void ParsingConvertsToCorrectCoordinates(string input, double expectedLat, double expectedLong)
    {
        var coords = new Coordinates(input);
        
        Assert.Equal(expectedLat, coords.latitude);
        Assert.Equal(expectedLong, coords.longitude);
    }

    [Theory]
    [InlineData(42.3545, 174.23)]
    [InlineData(-42.3545, -174.23)]
    [InlineData(0, 0)]
    public void LiteralConstructorStoresValuesUnaltered(double lat, double lon)
    {
        var coords = new Coordinates(lat, lon);
        
        Assert.Equal(lat, coords.latitude);
        Assert.Equal(lon, coords.longitude);
    }
    
    [Theory]
    [InlineData(42.3545, 174.23, "42.3545° 174.23°")]
    [InlineData(-42.3545, -174.23, "-42.3545° -174.23°")]
    [InlineData(42.354576, 174.230023, "42.35458° 174.23002°")]
    [InlineData(-42.3545, -174.230025, "-42.3545° -174.23003°")]
    [InlineData(0, 0, "0.0° 0.0°")]    
    public void StringConversion(double lat, double lon, string expected)
    {
        var coords = new Coordinates(lat, lon);
        Assert.Equal(coords.ToString(), expected);
    }
}
