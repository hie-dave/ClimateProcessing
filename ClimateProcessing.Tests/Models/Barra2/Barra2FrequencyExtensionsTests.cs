using System;
using ClimateProcessing.Models.Barra2;
using Xunit;

namespace ClimateProcessing.Tests.Models.Barra2;

public class Barra2FrequencyExtensionsTests
{
    [Theory]
    [InlineData(Barra2Frequency.Hour1, "1hr")]
    [InlineData(Barra2Frequency.Hour3, "3hr")]
    [InlineData(Barra2Frequency.Hour6, "6hr")]
    [InlineData(Barra2Frequency.Daily, "day")]
    [InlineData(Barra2Frequency.Monthly, "mon")]
    [InlineData(Barra2Frequency.Constant, "fx")]
    public void ToString_ValidValues_ReturnsExpected(Barra2Frequency input, string expected)
    {
        string actual = Barra2FrequencyExtensions.ToString(input);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToString_InvalidEnumValue_ThrowsArgumentException()
    {
        var invalid = (Barra2Frequency)999;

        Assert.Throws<ArgumentException>(() => Barra2FrequencyExtensions.ToString(invalid));
    }

    [Theory]
    [InlineData("1hr", Barra2Frequency.Hour1)]
    [InlineData("3hr", Barra2Frequency.Hour3)]
    [InlineData("6hr", Barra2Frequency.Hour6)]
    [InlineData("day", Barra2Frequency.Daily)]
    [InlineData("mon", Barra2Frequency.Monthly)]
    [InlineData("fx", Barra2Frequency.Constant)]
    public void FromString_ValidStrings_ReturnsExpected(string input, Barra2Frequency expected)
    {
        var actual = Barra2FrequencyExtensions.FromString(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1HR")] // case sensitive
    [InlineData("01hr")]  // extra 0
    [InlineData("weekly")] // unknown
    [InlineData("days")] 
    public void FromString_InvalidStrings_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => Barra2FrequencyExtensions.FromString(input));
    }
}
