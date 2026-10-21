using Xunit;

namespace FastGithub.Tests;

public class ProductionVersionTests
{
    [Theory]
    [InlineData("2.1.5", "2.1.5", "")]
    [InlineData("2.1.5+abcdef", "2.1.5", "")]
    [InlineData("2.1.5-beta.1", "2.1.5", "-beta.1")]
    [InlineData("2.1.5-beta.1+abcdef", "2.1.5", "-beta.1")]
    [InlineData("2.1.5.0", "2.1.5.0", "")]
    public void ParsesAssemblyVersionFormats(string text, string version, string suffix)
    {
        var result = ProductionVersion.Parse(text);
        Assert.Equal(Version.Parse(version), result.Version);
        Assert.Equal(suffix, result.SubVersion);
        Assert.Equal(version + suffix, result.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-version")]
    [InlineData("2.1.5-")]
    public void InvalidMetadataCanBeHandledWithoutCrashingStartup(string? value)
    {
        Assert.False(ProductionVersion.TryParse(value, out var result));
        Assert.Null(result);
        Assert.Throws<FormatException>(() => ProductionVersion.Parse(value!));
    }

    [Fact]
    public void BuildMetadataDoesNotChangeReleasePrecedence()
    {
        var release = ProductionVersion.Parse("2.1.5+build123");
        Assert.Equal(0, release.CompareTo(ProductionVersion.Parse("2.1.5+build456")));
        Assert.True(release.CompareTo(ProductionVersion.Parse("2.1.5-beta.1")) > 0);
    }
}
