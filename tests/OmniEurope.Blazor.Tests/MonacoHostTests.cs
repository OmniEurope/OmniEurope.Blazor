using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The checks that the code editor and the diff viewer share through <see cref="MonacoHost"/>: the same
/// height syntax and the same origin rule for the Monaco location.
/// </summary>
public sealed class MonacoHostTests
{
    [Theory]
    [InlineData("20rem")]
    [InlineData("320px")]
    [InlineData("50vh")]
    [InlineData("1.5em")]
    [InlineData("100%")]
    [InlineData("0.5rem")]
    public void AHeightMadeOfANumberAndAKnownUnit_IsAccepted(string height)
    {
        var error = Record.Exception(() => MonacoHost.RequireParameters("OmniCodeEditor", height, MonacoHost.DefaultPath));

        Assert.Null(error);
    }

    [Fact]
    public void ANullHeight_IsAccepted()
    {
        Assert.Null(Record.Exception(() => MonacoHost.RequireParameters("OmniDiffViewer", null, MonacoHost.DefaultPath)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("20")]
    [InlineData("rem")]
    [InlineData("1.rem")]
    [InlineData(".5rem")]
    [InlineData("1.5")]
    [InlineData("20pt")]
    [InlineData("-1px")]
    [InlineData("1.5.5px")]
    [InlineData("20 rem")]
    [InlineData("20REM")]
    public void AHeightThatIsNotANumberAndAKnownUnit_IsRefusedWithTheComponentName(string height)
    {
        var error = Assert.Throws<ArgumentException>(() => MonacoHost.RequireParameters("OmniDiffViewer", height, MonacoHost.DefaultPath));

        Assert.Equal("Height", error.ParamName);
        Assert.Contains("OmniDiffViewer", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://example.com/vs")]
    [InlineData("//example.com/vs")]
    public void AMonacoPathOutsideTheOrigin_IsRefused(string path)
    {
        var error = Assert.Throws<ArgumentException>(() => MonacoHost.RequireParameters("OmniCodeEditor", null, path));

        Assert.Equal("MonacoPath", error.ParamName);
    }
}
