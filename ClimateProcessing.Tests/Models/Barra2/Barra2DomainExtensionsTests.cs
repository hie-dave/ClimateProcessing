using System;
using ClimateProcessing.Models.Barra2;
using Xunit;

namespace ClimateProcessing.Tests.Models.Barra2;

public class Barra2DomainExtensionsTests
{
    [Theory]
    [InlineData(Barra2Domain.Aus11, "AUS-11")]
    [InlineData(Barra2Domain.Aus22, "AUS-22")]
    [InlineData(Barra2Domain.Aust04, "AUST-04")]
    [InlineData(Barra2Domain.Aust11, "AUST-11")]
    public void ToString_ValidValues_ReturnsExpected(Barra2Domain input, string expected)
    {
        string actual = Barra2DomainExtensions.ToString(input);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ToString_InvalidEnumValue_ThrowsArgumentException()
    {
        var invalid = (Barra2Domain)999;

        Assert.Throws<ArgumentException>(() => Barra2DomainExtensions.ToString(invalid));
    }

    [Theory]
    [InlineData("AUS-11", Barra2Domain.Aus11)]
    [InlineData("AUS-22", Barra2Domain.Aus22)]
    [InlineData("AUST-04", Barra2Domain.Aust04)]
    [InlineData("AUST-11", Barra2Domain.Aust11)]
    public void FromString_ValidStrings_ReturnsExpected(string input, Barra2Domain expected)
    {
        var actual = Barra2DomainExtensions.FromString(input);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("aus-11")] // case sensitive
    [InlineData("AUS11")]  // missing hyphen
    [InlineData("AUST-99")] // unknown code
    [InlineData("something-else")] 
    public void FromString_InvalidStrings_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => Barra2DomainExtensions.FromString(input));
    }
}
