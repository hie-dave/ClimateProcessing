using System;
using ClimateProcessing.Models.Barra2;
using Xunit;

namespace ClimateProcessing.Tests.Models.Barra2;

public class Barra2SourceExtensionsTests
{
    [Theory]
    [InlineData(Barra2Grid.R2, "BARRA-R2")]
    [InlineData(Barra2Grid.RE2, "BARRA-RE2")]
    [InlineData(Barra2Grid.C2, "BARRA-C2")]
    public void ToString_ValidValues_ReturnsExpected(Barra2Grid input, string expected)
    {
        string actual = Barra2GridExtensions.ToString(input);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToString_InvalidEnumValue_ThrowsArgumentException()
    {
        var invalid = (Barra2Grid)999;

        Assert.Throws<ArgumentException>(() => Barra2GridExtensions.ToString(invalid));
    }

    [Theory]
    [InlineData("BARRA-R2", Barra2Grid.R2)]
    [InlineData("BARRA-RE2", Barra2Grid.RE2)]
    [InlineData("BARRA-C2", Barra2Grid.C2)]
    public void FromString_ValidStrings_ReturnsExpected(string input, Barra2Grid expected)
    {
        var actual = Barra2GridExtensions.FromString(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("barra-r2")] // case sensitive
    [InlineData("BARRAR2")]  // missing hyphen
    [InlineData("BARRA-X1")] // unknown
    public void FromString_InvalidStrings_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => Barra2GridExtensions.FromString(input));
    }
}
