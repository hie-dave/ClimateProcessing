using System;
using ClimateProcessing.Models.Barra2;
using Xunit;

namespace ClimateProcessing.Tests.Models.Barra2;

public class Barra2VariantExtensionsTests
{
    [Theory]
    [InlineData(Barra2Variant.HRes, "hres")]
    [InlineData(Barra2Variant.Eda, "eda")]
    public void ToString_ValidValues_ReturnsExpected(Barra2Variant input, string expected)
    {
        string actual = Barra2VariantExtensions.ToString(input);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToString_InvalidEnumValue_ThrowsArgumentException()
    {
        var invalid = (Barra2Variant)999;

        Assert.Throws<ArgumentException>(() => Barra2VariantExtensions.ToString(invalid));
    }

    [Theory]
    [InlineData("hres", Barra2Variant.HRes)]
    [InlineData("eda", Barra2Variant.Eda)]
    public void FromString_ValidStrings_ReturnsExpected(string input, Barra2Variant expected)
    {
        var actual = Barra2VariantExtensions.FromString(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("HRES")] // case sensitive
    [InlineData("edA")]  // case sensitive
    [InlineData("hr-es")] // malformed
    [InlineData("unknown")] 
    public void FromString_InvalidStrings_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => Barra2VariantExtensions.FromString(input));
    }
}
