using Bunit;
using OmniEurope.Blazor.Components;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniAnsiText"/> and the <see cref="OmniLogViewer.AnsiMode"/> of the log viewer: what is
/// drawn for a line with escape sequences in each <see cref="OmniAnsiMode"/>, and that a search, its marks
/// and the line numbers run on the displayed text while the line stays as written.
/// </summary>
public sealed class AnsiRenderTests : OmniBunitContext
{
    private const string Esc = "\u001b";

    /// <summary>The line a client application showed with its escape characters invisible and its brackets visible.</summary>
    private const string ClientLine = Esc + "[90m3:27PM" + Esc + "[0m " + Esc + "[32mINF" + Esc + "[0m " + Esc + "[1mno leaks found" + Esc + "[0m";

    private const string Plain = "3:27PM INF no leaks found";

    private BunitJSModuleInterop Module() => JSInterop.SetupModule(OmniModules.LogViewer);

    private IRenderedComponent<OmniLogViewer> RenderViewer(OmniAnsiMode? mode, string? search = null, string text = ClientLine, OmniLogLevel level = OmniLogLevel.Information)
    {
        Module();
        return Render<OmniLogViewer>(parameters =>
        {
            parameters.Add(viewer => viewer.Lines, [new OmniLogLine(text, level)]).Add(viewer => viewer.TimeZone, TimeZoneInfo.Utc);
            if (mode is { } value)
            {
                parameters.Add(viewer => viewer.AnsiMode, value);
            }

            if (search is not null)
            {
                parameters.Add(viewer => viewer.ShowSearch, true).Add(viewer => viewer.SearchText, search);
            }
        });
    }

    // ---- OmniAnsiText -------------------------------------------------------------------------------

    [Fact]
    public void AnsiText_Renders_ByDefault_ColoursAndBold_WithoutAnyBracketOrEscape()
    {
        var cut = Render<OmniAnsiText>(parameters => parameters.Add(text => text.Text, ClientLine));

        Assert.Equal(Plain, cut.Find(".omni-ansi").TextContent);
        Assert.Equal("INF", cut.Find(".omni-ansi-fg-2").TextContent);
        Assert.Equal("3:27PM", cut.Find(".omni-ansi-fg-8").TextContent);
        Assert.Equal("no leaks found", cut.Find(".omni-ansi-bold").TextContent);
        Assert.DoesNotContain("[", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Esc, cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AnsiText_Hide_ShowsPlainText_AndDrawsNothing()
    {
        var cut = Render<OmniAnsiText>(parameters => parameters.Add(text => text.Text, ClientLine).Add(text => text.Mode, OmniAnsiMode.Hide));

        Assert.Equal(Plain, cut.Find(".omni-ansi").TextContent);
        Assert.Empty(cut.FindAll(".omni-ansi span"));
        Assert.DoesNotContain("[", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Esc, cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AnsiText_Raw_ShowsTheLineAsWritten_CodesVisible()
    {
        var cut = Render<OmniAnsiText>(parameters => parameters.Add(text => text.Text, ClientLine).Add(text => text.Mode, OmniAnsiMode.Raw));

        Assert.Equal(ClientLine, cut.Find(".omni-ansi").TextContent);
        Assert.Contains("[90m", cut.Find(".omni-ansi").TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".omni-ansi span"));
    }

    [Fact]
    public void AnsiText_DrawsEveryAttribute_AndAMarkupCharacterOfTheTextIsEncoded()
    {
        var cut = Render<OmniAnsiText>(parameters => parameters.Add(
            text => text.Text,
            Esc + "[2;3;4;31;44m<b>&" + Esc + "[0m"));

        var run = cut.Find(".omni-ansi > span");
        Assert.Equal("<b>&", run.TextContent);
        Assert.Equal(["omni-ansi-dim", "omni-ansi-italic", "omni-ansi-underline", "omni-ansi-fg-1", "omni-ansi-bg-4"], run.ClassList);
        Assert.Empty(cut.FindAll(".omni-ansi b"));
    }

    [Fact]
    public void AnsiText_WithoutText_IsAnEmptySpan_AndKeepsTheComponentAttributes()
    {
        var cut = Render<OmniAnsiText>(parameters => parameters
            .Add(text => text.Id, "cell")
            .Add(text => text.Class, "mine")
            .AddUnmatched("data-row", "7"));

        var span = cut.Find("span.omni-ansi");
        Assert.Equal(string.Empty, span.TextContent);
        Assert.Equal("cell", span.Id);
        Assert.Contains("mine", span.ClassList);
        Assert.Equal("7", span.GetAttribute("data-row"));
    }

    [Fact]
    public void AnsiText_ChangesWithItsMode()
    {
        var cut = Render<OmniAnsiText>(parameters => parameters.Add(text => text.Text, ClientLine));
        Assert.Equal(Plain, cut.Find(".omni-ansi").TextContent);

        cut.Render(parameters => parameters.Add(text => text.Text, ClientLine).Add(text => text.Mode, OmniAnsiMode.Raw));

        Assert.Equal(ClientLine, cut.Find(".omni-ansi").TextContent);
    }

    // ---- OmniLogViewer ------------------------------------------------------------------------------

    [Fact]
    public void LogViewer_DefaultsToRender()
    {
        Assert.Equal(OmniAnsiMode.Render, new OmniLogViewer().AnsiMode);

        var viewer = RenderViewer(null);

        Assert.Equal(Plain, viewer.Find(".omni-log-viewer__text").TextContent);
        Assert.Equal("INF", viewer.Find(".omni-log-viewer__text .omni-ansi-fg-2").TextContent);
        Assert.Equal("no leaks found", viewer.Find(".omni-log-viewer__text .omni-ansi-bold").TextContent);
    }

    [Theory]
    [InlineData(OmniAnsiMode.Render)]
    [InlineData(OmniAnsiMode.Hide)]
    public void LogViewer_RenderAndHide_ShowNeitherABracketNorTheEscape(OmniAnsiMode mode)
    {
        var viewer = RenderViewer(mode);

        var text = viewer.Find(".omni-log-viewer__text").TextContent;
        Assert.Equal(Plain, text);
        Assert.DoesNotContain("[", viewer.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain(Esc, viewer.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void LogViewer_Hide_DrawsNothing()
    {
        var viewer = RenderViewer(OmniAnsiMode.Hide);

        Assert.Empty(viewer.FindAll(".omni-log-viewer__text span"));
    }

    [Fact]
    public void LogViewer_Raw_ShowsTheCodes()
    {
        var viewer = RenderViewer(OmniAnsiMode.Raw);

        var text = viewer.Find(".omni-log-viewer__text").TextContent;
        Assert.Equal(ClientLine, text);
        Assert.Contains("[32mINF", text, StringComparison.Ordinal);
        Assert.Empty(viewer.FindAll(".omni-log-viewer__text span"));
    }

    [Fact]
    public void LogViewer_TheLineStaysAsWritten_WhateverTheMode()
    {
        var lines = new[] { new OmniLogLine(ClientLine) };
        Module();
        foreach (var mode in Enum.GetValues<OmniAnsiMode>())
        {
            Render<OmniLogViewer>(parameters => parameters
                .Add(viewer => viewer.Lines, lines)
                .Add(viewer => viewer.AnsiMode, mode)
                .Add(viewer => viewer.TimeZone, TimeZoneInfo.Utc));
        }

        Assert.Equal(ClientLine, lines[0].Text);
    }

    [Fact]
    public void LogViewer_ASearchRunsOnTheTextWithoutCodes_AndMarksAcrossRuns()
    {
        var viewer = RenderViewer(OmniAnsiMode.Render, "inf no");

        Assert.Equal("0 sur 1", viewer.Find(".omni-log-viewer__matches").TextContent);
        Assert.False(viewer.Find(".omni-log-viewer__previous").HasAttribute("disabled"));
        Assert.Equal("INF no", string.Concat(viewer.FindAll("mark.omni-log-viewer__match").Select(mark => mark.TextContent)));
        Assert.Equal(Plain, viewer.Find(".omni-log-viewer__text").TextContent);
        Assert.Equal("INF", viewer.Find(".omni-log-viewer__text .omni-ansi-fg-2 mark").TextContent);
    }

    [Theory]
    [InlineData(OmniAnsiMode.Render)]
    [InlineData(OmniAnsiMode.Hide)]
    public void LogViewer_ACodeIsNotFound_WhenItIsNotDisplayed(OmniAnsiMode mode)
    {
        var viewer = RenderViewer(mode, "[32m");

        Assert.Equal("Aucune correspondance", viewer.Find(".omni-log-viewer__matches").TextContent);
        Assert.Empty(viewer.FindAll("mark"));
    }

    [Fact]
    public void LogViewer_ACodeIsFound_InRaw()
    {
        var viewer = RenderViewer(OmniAnsiMode.Raw, "[32m");

        Assert.Equal("[32m", Assert.Single(viewer.FindAll("mark.omni-log-viewer__match")).TextContent);
        Assert.False(viewer.Find(".omni-log-viewer__previous").HasAttribute("disabled"));
    }

    [Fact]
    public void LogViewer_ChangingTheModeSearchesAgain()
    {
        var viewer = RenderViewer(OmniAnsiMode.Render, "[32m");
        Assert.True(viewer.Find(".omni-log-viewer__previous").HasAttribute("disabled"));

        viewer.Render(parameters => parameters
            .Add(component => component.Lines, [new OmniLogLine(ClientLine)])
            .Add(component => component.TimeZone, TimeZoneInfo.Utc)
            .Add(component => component.ShowSearch, true)
            .Add(component => component.SearchText, "[32m")
            .Add(component => component.AnsiMode, OmniAnsiMode.Raw));

        Assert.False(viewer.Find(".omni-log-viewer__previous").HasAttribute("disabled"));
        Assert.Single(viewer.FindAll("mark.omni-log-viewer__match"));
    }

    [Fact]
    public void LogViewer_LineNumbersAndLevelsAreUntouched()
    {
        Module();
        var viewer = Render<OmniLogViewer>(parameters => parameters
            .Add(component => component.Lines,
            [
                new OmniLogLine(ClientLine),
                new OmniLogLine(Esc + "[31mdisk " + Esc + "[1mfull" + Esc + "[0m", OmniLogLevel.Warning),
            ])
            .Add(component => component.TimeZone, TimeZoneInfo.Utc));

        Assert.Equal(["1", "2"], viewer.FindAll(".omni-log-viewer__number").Select(number => number.TextContent));
        Assert.Equal(["3:27PM INF no leaks found", "disk full"], viewer.FindAll(".omni-log-viewer__text").Select(text => text.TextContent));
        Assert.Single(viewer.FindAll(".omni-log-viewer__line--warning"));
    }

    // ---- The markup helper --------------------------------------------------------------------------

    [Fact]
    public void Pieces_CutTheMatchesAtTheRunsBoundaries()
    {
        var pieces = OmniAnsiMarkup.Pieces(Esc + "[31mab" + Esc + "[32mcd" + Esc + "[0mef", OmniAnsiMode.Render, "bcd");

        Assert.Equal(
            [("a", false), ("b", true), ("cd", true), ("ef", false)],
            pieces.Select(piece => (piece.Text, piece.Matched)));
        Assert.Equal([1, 1, 2, null], pieces.Select(piece => piece.Style?.Foreground));
    }

    [Fact]
    public void Pieces_OfAPlainLineWithoutSearch_AreOneUnstyledRun()
    {
        var piece = Assert.Single(OmniAnsiMarkup.Pieces("plain", OmniAnsiMode.Render, null));

        Assert.Equal("plain", piece.Text);
        Assert.Null(OmniAnsiMarkup.ClassOf(piece.Style!));
    }

    [Fact]
    public void DisplayText_OfNullInRawMode_IsEmpty_AndOtherwiseTheLineAsWritten()
    {
        Assert.Equal(string.Empty, OmniAnsiMarkup.DisplayText(null, OmniAnsiMode.Raw));
        Assert.Equal(ClientLine, OmniAnsiMarkup.DisplayText(ClientLine, OmniAnsiMode.Raw));
        Assert.Equal(Plain, OmniAnsiMarkup.DisplayText(ClientLine, OmniAnsiMode.Hide));
    }

    [Fact]
    public void ClassOf_IgnoresAColourOutsideTheSixteen_ButKeepsTheOtherAttributes()
    {
        Assert.Null(OmniAnsiMarkup.ClassOf(new OmniAnsiSegment("x", Foreground: 16, Background: -1)));
        Assert.Null(OmniAnsiMarkup.ClassOf(new OmniAnsiSegment("x", Foreground: -1, Background: 16)));
        Assert.Equal("omni-ansi-bold omni-ansi-fg-15 omni-ansi-bg-0", OmniAnsiMarkup.ClassOf(new OmniAnsiSegment("x", Bold: true, Foreground: 15, Background: 0)));
    }
}
