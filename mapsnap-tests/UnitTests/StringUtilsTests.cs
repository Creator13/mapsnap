using mapsnap.Utils;
using Xunit;

namespace mapsnapTests.UnitTests;

public class StringUtilsTests
{
    [Theory]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("helloWorld", "hello_world")]
    [InlineData("helloworld", "helloworld")]
    [InlineData("", "")]
    public void ConvertToSnakeCase(string input, string expectedOutput)
    {
        Assert.Equal(expectedOutput, input.ToSnakeCase());
    }
}
