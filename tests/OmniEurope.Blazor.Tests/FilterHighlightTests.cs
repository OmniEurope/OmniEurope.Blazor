using Bunit;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary><see cref="OmniFilterHighlight"/>: the matches of the filter are marked, literally, and the text is encoded.</summary>
public sealed class FilterHighlightTests : OmniBunitContext
{
    private IRenderedComponent<OmniFilterHighlight> Draw(string? text, string? filter, bool ignoreCase = true)
        => Render<OmniFilterHighlight>(parameters => parameters
            .Add(component => component.Text, text)
            .Add(component => component.Filter, filter)
            .Add(component => component.IgnoreCase, ignoreCase));

    private static string[] Marks(IRenderedComponent<OmniFilterHighlight> cut)
        => [.. cut.FindAll("mark.omni-combo__match").Select(mark => mark.TextContent)];

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithoutAFilter_TheTextIsPlain(string? filter)
    {
        var cut = Draw("Brussels", filter);

        Assert.Empty(cut.FindAll("mark"));
        Assert.Equal("Brussels", cut.Find("span").TextContent);
    }

    [Fact]
    public void ANullText_DrawsNothing()
    {
        var cut = Draw(null, "a");

        Assert.Empty(cut.FindAll("mark"));
        Assert.Equal(string.Empty, cut.Find("span").TextContent);
    }

    [Fact]
    public void TheMatchIsCaseInsensitive_AndKeepsTheCaseOfTheText()
    {
        var cut = Draw("Brussels", "BRU");

        Assert.Equal(["Bru"], Marks(cut));
        Assert.Equal("Brussels", cut.Find("span").TextContent);
    }

    [Fact]
    public void IgnoreCaseOff_MatchesTheExactCaseOnly()
    {
        Assert.Empty(Marks(Draw("Brussels", "BRU", ignoreCase: false)));
        Assert.Equal(["Bru"], Marks(Draw("Brussels", "Bru", ignoreCase: false)));
    }

    [Fact]
    public void EveryOccurrenceIsMarked()
    {
        var cut = Draw("banana", "an");

        Assert.Equal(["an", "an"], Marks(cut));
        Assert.Equal("banana", cut.Find("span").TextContent);
    }

    [Fact]
    public void TheFilterIsTrimmed()
    {
        Assert.Equal(["ana"], Marks(Draw("banana", "  ana ")));
    }

    [Theory]
    [InlineData("a.c", "a.c abc", "a.c")]
    [InlineData("(x", "f(x)", "(x")]
    [InlineData("[a-z]+", "[a-z]+ az", "[a-z]+")]
    [InlineData(".*", "a.*b", ".*")]
    public void SpecialRegexCharacters_AreLiteral(string filter, string text, string marked)
    {
        var cut = Draw(text, filter);

        Assert.Equal([marked], Marks(cut));
    }

    [Fact]
    public void HtmlInTheText_IsEncoded_NeverWrittenAsMarkup()
    {
        var cut = Draw("<img src=x onerror=alert(1)> bold", "bold");

        Assert.Empty(cut.FindAll("img"));
        Assert.Equal(["bold"], Marks(cut));
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void HtmlInTheFilter_MatchesLiterally()
    {
        var cut = Draw("a <b> c", "<b>");

        Assert.Empty(cut.FindAll("b"));
        Assert.Equal(["<b>"], Marks(cut));
    }
}
