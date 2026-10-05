using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// The plain text, the standalone file and the first heading of an editor value: every block and every
/// class the export knows, and the empty cases.
/// </summary>
public sealed class OmniHtmlTextTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void EmptyValue_HasNoText(string? html) => Assert.Equal(string.Empty, OmniHtmlText.ToPlainText(html));

    [Fact]
    public void PlainText_BreaksLines_RulesBlocks_AndKeepsPreformattedSpaces()
    {
        var text = OmniHtmlText.ToPlainText("<p>Un<br>deux</p><hr><pre>  a   b\n  <strong>c   d</strong></pre><p>e    f</p>");

        Assert.Equal("Un\ndeux\n\n----\n\n  a   b\n  c   d\n\ne f", text);
    }

    [Fact]
    public void PlainText_NumbersOrderedItems_AndBulletsTheOthers()
    {
        var text = OmniHtmlText.ToPlainText("<ol><li>Un</li><li>Deux</li></ol><ul><li>A</li></ul>");

        Assert.Equal("1. Un\n2. Deux\n\n- A", text);
    }

    [Fact]
    public void PlainText_OfACommentOrAnEmptyElement_IsEmpty()
    {
        Assert.Equal("x", OmniHtmlText.ToPlainText("<p><!-- note -->x<span></span></p>"));
    }

    [Theory]
    [InlineData("omni-align-center", "text-align: center")]
    [InlineData("omni-align-end", "text-align: end")]
    [InlineData("omni-align-justify", "text-align: justify")]
    [InlineData("omni-font-size-small", "font-size: 0.85em")]
    [InlineData("omni-font-size-normal", "font-size: 1em")]
    [InlineData("omni-font-size-large", "font-size: 1.3em")]
    [InlineData("omni-font-size-xlarge", "font-size: 1.7em")]
    public void StandaloneFile_WritesEachPackageClassAsItsDeclaration(string className, string declaration)
    {
        var file = OmniHtmlText.ToStandaloneDocument($"<p class=\"{className}\">x</p>", "Titre", "fr");

        Assert.Contains($"<p style=\"{declaration}\">x</p>", file, StringComparison.Ordinal);
    }

    [Fact]
    public void StandaloneFile_DropsTheDefaultAlignment_AndEncodesTitleAndLanguage()
    {
        // Left is the default alignment: the class goes and no declaration replaces it.
        var file = OmniHtmlText.ToStandaloneDocument("<p class=\"omni-align-left\">x</p>", "A & B", "fr\"");

        Assert.Contains("<p>x</p>", file, StringComparison.Ordinal);
        Assert.Contains("<title>A &amp; B</title>", file, StringComparison.Ordinal);
        Assert.Contains("lang=\"fr&quot;\"", file, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(" ", null)]
    [InlineData("<p>Sans titre</p>", null)]
    [InlineData("<h1>  </h1>", null)]
    [InlineData("<h2>Sous</h2><h1> Rapport </h1>", "Rapport")]
    public void FirstHeading_IsTheTrimmedTextOfTheFirstH1(string? html, string? heading) =>
        Assert.Equal(heading, OmniHtmlText.FirstHeading(html));
}
