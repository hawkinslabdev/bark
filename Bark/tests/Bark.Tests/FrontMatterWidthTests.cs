using Bark.Models;

namespace Bark.Tests;

public sealed class FrontMatterWidthTests
{
    [Theory]
    [InlineData("80%", 80)]
    [InlineData("80", 80)]
    [InlineData(" 100% ", 100)]
    [InlineData("10%", 10)]
    [InlineData("9%", null)]
    [InlineData("101%", null)]
    [InlineData("-50%", null)]
    [InlineData("80.5%", null)]
    [InlineData("80px", null)]
    [InlineData("80%; color: red", null)]
    [InlineData(null, null)]
    public void WidthPercent_AcceptsIntegerPercentagesFrom10To100(string? width, int? expected)
    {
        Assert.Equal(expected, new FrontMatter { Width = width }.WidthPercent);
    }
}
