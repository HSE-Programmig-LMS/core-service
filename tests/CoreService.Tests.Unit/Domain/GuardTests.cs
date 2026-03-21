using CoreService.Domain.Common;
using Xunit;

namespace CoreService.Tests.Unit.Domain;

public sealed class GuardTests
{
    [Fact]
    public void NotNull_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => Guard.NotNull(null, "x"));
        Assert.Equal("x", ex.ParamName);
    }

    [Fact]
    public void NotNull_WhenValueIsNotNull_ShouldNotThrow()
    {
        Guard.NotNull(new object(), "x");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NotNullOrWhiteSpace_WhenInvalid_ShouldThrowArgumentException(string? value)
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhiteSpace(value, "name"));
        Assert.Equal("name", ex.ParamName);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("  a  ")]
    [InlineData("0")]
    public void NotNullOrWhiteSpace_WhenValid_ShouldNotThrow(string value)
    {
        Guard.NotNullOrWhiteSpace(value, "name");
    }

    [Fact]
    public void InRange_WhenBelowMin_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.InRange(0, 1, 10, "v"));
        Assert.Equal("v", ex.ParamName);
    }

    [Fact]
    public void InRange_WhenAboveMax_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => Guard.InRange(11, 1, 10, "v"));
        Assert.Equal("v", ex.ParamName);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void InRange_WhenWithinBoundsInclusive_ShouldNotThrow(int value)
    {
        Guard.InRange(value, 1, 10, "v");
    }
}