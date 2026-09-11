using System.Reflection;
using System.Text.RegularExpressions;
using OmniEurope.Blazor.Showcase.Theming;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The stylesheet a host downloads is a minified copy built from the commented source. These tests
/// read the copy the build actually produced, so a minifier that dropped a meaningful space or a
/// reader that needed line breaks would fail here rather than in a host's browser.
/// </summary>
public sealed class ShippedStylesheetTests
{
    private static readonly string Root = RepositoryRoot();

    [Fact]
    public void ShippedStylesheet_DiffersFromTheSourceOnlyByCommentsAndMeaninglessWhitespace()
    {
        var source = Source();
        var shipped = Shipped();

        Assert.DoesNotContain("/*", shipped, StringComparison.Ordinal);
        Assert.DoesNotContain('\n', shipped);
        Assert.True(shipped.Length < source.Length, "The shipped stylesheet is not smaller than its source.");
        Assert.Equal(Normalise(source), shipped);
    }

    [Fact]
    public void TokenReader_ReadsTheSameCatalogueFromTheShippedStylesheetAsFromTheSource()
    {
        var fromSource = ThemeTokenReader.Parse(Source());
        var fromShipped = ThemeTokenReader.Parse(Shipped());

        Assert.NotEmpty(fromSource);
        Assert.Equal(fromSource.Select(token => token.Name), fromShipped.Select(token => token.Name));
        Assert.Equal(
            fromSource.Select(token => Regex.Replace(token.DefaultValue, @"\s+", string.Empty)),
            fromShipped.Select(token => Regex.Replace(token.DefaultValue, @"\s+", string.Empty)));
    }

    [Fact]
    public void TokenReader_ReadsADeclarationThatHasNoTrailingSemicolon()
    {
        var tokens = ThemeTokenReader.Parse(":root{--omni-radius:2px;--omni-color-text:#000}.x{color:red}");

        Assert.Equal(["--omni-radius", "--omni-color-text"], tokens.Select(token => token.Name));
        Assert.Equal("#000", tokens[1].DefaultValue);
    }

    // An independent restatement of what the minifier may remove, written with regular expressions
    // rather than the build task's character scanner, so the two have to agree.
    private static string Normalise(string css)
    {
        var text = Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"\s+", " ");
        text = Regex.Replace(text, @"\s*([{};,>])\s*", "$1");
        text = Regex.Replace(text, @":\s+", ":");
        return text.Replace(";}", "}", StringComparison.Ordinal).Trim();
    }

    private static string Source() =>
        File.ReadAllText(Path.Combine(Root, "src", "OmniEurope.Blazor", "wwwroot", "omnieurope.blazor.css"));

    // The library is built in the same configuration as this test assembly, and the minified copy is
    // produced in every configuration even though only Release ships it.
    private static string Shipped()
    {
        var configuration = typeof(ShippedStylesheetTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var path = Path.Combine(Root, "src", "OmniEurope.Blazor", "obj", configuration, "net10.0", "omni-stylesheet", "omnieurope.blazor.css");
        Assert.True(File.Exists(path), $"The build did not produce the minified stylesheet at {path}.");
        return File.ReadAllText(path);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmniEurope.Blazor.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
