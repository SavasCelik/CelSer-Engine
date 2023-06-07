using CelSerEngine.Core.Extensions;
using Xunit;

namespace CelSerEngine.Core.UnitTests.Extensions;


public class StringExtensionTests
{
    [Theory]
    [InlineData("123", (short)123)]
    [InlineData("-456", (short)-456)]
    [InlineData("0", (short)0)]
    public void ParseNumber_Short_ValidInput_ReturnsParsedValue(string input, short expected)
    {
        short result = input.ParseNumber<short>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("47", 47)]
    [InlineData("-452", -452)]
    [InlineData("0", 0)]
    public void ParseNumber_Int_ValidInput_ReturnsParsedValue(string input, int expected)
    {
        int result = input.ParseNumber<int>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123456789", 123456789L)]
    [InlineData("-987654321", -987654321L)]
    [InlineData("0", 0L)]
    public void ParseNumber_Long_ValidInput_ReturnsParsedValue(string input, long expected)
    {
        long result = input.ParseNumber<long>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("123.45", 123.45f)]
    [InlineData("-67.89", -67.89f)]
    [InlineData("0.987", 0.987f)]
    public void ParseNumber_Float_ValidInput_ReturnsParsedValue(string input, float expected)
    {
        float result = input.ParseNumber<float>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("47.45", 47.45d)]
    [InlineData("-452.89", -452.89d)]
    [InlineData("0.001", 0.001d)]
    public void ParseNumber_Double_ValidInput_ReturnsParsedValue(string input, double expected)
    {
        double result = input.ParseNumber<double>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12a3")]
    [InlineData("123.45.67")]
    public void ParseNumber_InvalidInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => input.ParseNumber<double>());
    }
}
