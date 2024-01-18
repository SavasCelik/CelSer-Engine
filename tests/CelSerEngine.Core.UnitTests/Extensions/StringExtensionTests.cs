using CelSerEngine.Core.Extensions;
using Xunit;

namespace CelSerEngine.Core.UnitTests.Extensions;


public class StringExtensionTests
{
    [Theory]
    [MemberData(nameof(Short_TestData))]
    public void ParseNumber_Short_ValidInput_ReturnsParsedValue(string input, short expected)
    {
        short result = input.ParseNumber<short>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(Int_TestData))]
    public void ParseNumber_Int_ValidInput_ReturnsParsedValue(string input, int expected)
    {
        int result = input.ParseNumber<int>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(Long_TestData))]
    public void ParseNumber_Long_ValidInput_ReturnsParsedValue(string input, long expected)
    {
        long result = input.ParseNumber<long>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(Float_TestData))]
    public void ParseNumber_Float_ValidInput_ReturnsParsedValue(string input, float expected)
    {
        float result = input.ParseNumber<float>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(Double_TestData))]
    public void ParseNumber_Double_ValidInput_ReturnsParsedValue(string input, double expected)
    {
        double result = input.ParseNumber<double>();
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(Invalid_TestData))]
    public void ParseNumber_InvalidInput_ThrowsFormatException(string input)
    {
        Assert.Throws<FormatException>(() => input.ParseNumber<double>());
    }

    public static IEnumerable<object[]> Short_TestData()
    {
        yield return new object[] { "123", (short)123 };
        yield return new object[] { "-1337", (short)-1337 };
        yield return new object[] { "0", (short)0 };
    }

    public static IEnumerable<object[]> Int_TestData()
    {
        yield return new object[] { "47", 47 };
        yield return new object[] { "-452", -452 };
        yield return new object[] { "0", 0 };
    }

    public static IEnumerable<object[]> Long_TestData()
    {
        yield return new object[] { "123456789", 123456789L };
        yield return new object[] { "-987654321", -987654321L };
        yield return new object[] { "0", 0L };
    }

    public static IEnumerable<object[]> Float_TestData()
    {
        yield return new object[] { "123.45", 123.45f };
        yield return new object[] { "-67.89", -67.89f };
        yield return new object[] { "0.987", 0.987f };
    }

    public static IEnumerable<object[]> Double_TestData()
    {
        yield return new object[] { "47.45", 47.45d };
        yield return new object[] { "-452.89", -452.89d };
        yield return new object[] { "0.001", 0.001d };
    }

    public static IEnumerable<object[]> Invalid_TestData()
    {
        yield return new object[] { "abc" };
        yield return new object[] { "12a3" };
        yield return new object[] { "123.45.67" };
    }
}
