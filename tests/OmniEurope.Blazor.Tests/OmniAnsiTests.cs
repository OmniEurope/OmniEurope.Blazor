using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniAnsi"/>: the runs a line of terminal output is cut into, the text it keeps, and what
/// becomes of sequences that are not a graphic rendition, are cut off or are malformed.
/// </summary>
public sealed class OmniAnsiTests
{
    private const string Esc = "\u001b";

    /// <summary>The line a client application showed with its escape characters invisible and its brackets visible.</summary>
    private const string ClientLine = Esc + "[90m3:27PM" + Esc + "[0m " + Esc + "[32mINF" + Esc + "[0m " + Esc + "[1mno leaks found" + Esc + "[0m";

    private static string Joined(IEnumerable<OmniAnsiSegment> segments) => string.Concat(segments.Select(segment => segment.Text));

    // ---- The client example -----------------------------------------------------------------------

    [Fact]
    public void TheClientLine_ReadsWithoutCodes_GreenInfoAndBoldMessage()
    {
        var segments = OmniAnsi.Parse(ClientLine);

        Assert.Equal("3:27PM INF no leaks found", Joined(segments));
        Assert.Equal(
            [
                new OmniAnsiSegment("3:27PM", Foreground: 8),
                new OmniAnsiSegment(" "),
                new OmniAnsiSegment("INF", Foreground: 2),
                new OmniAnsiSegment(" "),
                new OmniAnsiSegment("no leaks found", Bold: true),
            ],
            segments);
        Assert.Equal("3:27PM INF no leaks found", OmniAnsi.Strip(ClientLine));
    }

    [Fact]
    public void ABracketCodeWithoutItsEscape_IsOrdinaryText_NothingIsGuessed()
    {
        // "[90m" cannot be told from a bracketed word of the log: it is kept, so no line loses a character.
        const string lost = "[90m3:27PM[0m [32mINF[0m";

        Assert.Equal(lost, OmniAnsi.Strip(lost));
        Assert.Equal([new OmniAnsiSegment(lost)], OmniAnsi.Parse(lost));
    }

    // ---- Plain text ---------------------------------------------------------------------------------

    [Fact]
    public void ALineWithoutAnEscape_IsOneRun_AndStripReturnsTheSameString()
    {
        const string line = "GET /health 200 [ok] 12ms\ttab and 'quotes' é";

        Assert.Equal([new OmniAnsiSegment(line)], OmniAnsi.Parse(line));
        Assert.Same(line, OmniAnsi.Strip(line));
    }

    [Fact]
    public void NullAndEmpty_GiveNothing()
    {
        Assert.Empty(OmniAnsi.Parse(null));
        Assert.Empty(OmniAnsi.Parse(string.Empty));
        Assert.Equal(string.Empty, OmniAnsi.Strip(null));
        Assert.Equal(string.Empty, OmniAnsi.Strip(string.Empty));
    }

    [Fact]
    public void SequencesAlone_GiveNoEmptyRun()
    {
        Assert.Empty(OmniAnsi.Parse(Esc + "[31m" + Esc + "[0m"));
        Assert.Equal(string.Empty, OmniAnsi.Strip(Esc + "[31m" + Esc + "[0m"));
    }

    [Fact]
    public void TheCharactersOutsideTheCodes_AreKeptAsWritten()
    {
        const string text = "a\tb\r\nc  d [x] {0} é ✓ \u0001";

        Assert.Equal(text, Joined(OmniAnsi.Parse(Esc + "[31m" + text + Esc + "[0m")));
        Assert.Equal(text, OmniAnsi.Strip(Esc + "[31m" + text));
    }

    // ---- Select Graphic Rendition -------------------------------------------------------------------

    [Fact]
    public void Reset_ClearsEveryAttribute_AndAnEmptyParameterIsAReset()
    {
        var segments = OmniAnsi.Parse(Esc + "[1;3;4;31;42mall" + Esc + "[0mnone" + Esc + "[1mbold" + Esc + "[mnone2");

        Assert.Equal(
            [
                new OmniAnsiSegment("all", Bold: true, Foreground: 1, Background: 2, Italic: true, Underline: true),
                new OmniAnsiSegment("none"),
                new OmniAnsiSegment("bold", Bold: true),
                new OmniAnsiSegment("none2"),
            ],
            segments);
    }

    [Fact]
    public void CombinedCodes_AccumulateAndALaterColourReplacesAnEarlierOne()
    {
        var segments = OmniAnsi.Parse(Esc + "[1m" + Esc + "[31mred" + Esc + "[32mgreen" + Esc + "[39mdefault" + Esc + "[22mlight");

        Assert.Equal(
            [
                new OmniAnsiSegment("red", Bold: true, Foreground: 1),
                new OmniAnsiSegment("green", Bold: true, Foreground: 2),
                new OmniAnsiSegment("default", Bold: true),
                new OmniAnsiSegment("light"),
            ],
            segments);
    }

    [Fact]
    public void Faint_Italic_Underline_AndTheirResets()
    {
        var segments = OmniAnsi.Parse(Esc + "[2;3;4mx" + Esc + "[23my" + Esc + "[24mz" + Esc + "[22mw");

        Assert.Equal(
            [
                new OmniAnsiSegment("x", Dim: true, Italic: true, Underline: true),
                new OmniAnsiSegment("y", Dim: true, Underline: true),
                new OmniAnsiSegment("z", Dim: true),
                new OmniAnsiSegment("w"),
            ],
            segments);
    }

    [Theory]
    [InlineData(30, 0)]
    [InlineData(31, 1)]
    [InlineData(37, 7)]
    [InlineData(90, 8)]
    [InlineData(97, 15)]
    public void ForegroundCodes_MapToTheSixteenColours(int code, int index) =>
        Assert.Equal(index, Assert.Single(OmniAnsi.Parse(Esc + "[" + code + "mx")).Foreground);

    [Theory]
    [InlineData(40, 0)]
    [InlineData(44, 4)]
    [InlineData(47, 7)]
    [InlineData(100, 8)]
    [InlineData(107, 15)]
    public void BackgroundCodes_MapToTheSixteenColours(int code, int index) =>
        Assert.Equal(index, Assert.Single(OmniAnsi.Parse(Esc + "[" + code + "mx")).Background);

    [Fact]
    public void ADefaultBackgroundCode_ClearsTheBackground()
    {
        var segments = OmniAnsi.Parse(Esc + "[41mon" + Esc + "[49moff");

        Assert.Equal([new OmniAnsiSegment("on", Background: 1), new OmniAnsiSegment("off")], segments);
    }

    [Fact]
    public void AdjacentRunsWithTheSameAttributes_AreMerged()
    {
        Assert.Equal([new OmniAnsiSegment("ab")], OmniAnsi.Parse("a" + Esc + "[1m" + Esc + "[22mb"));
        Assert.Equal([new OmniAnsiSegment("ab", Foreground: 1)], OmniAnsi.Parse(Esc + "[31ma" + Esc + "[31mb"));
    }

    [Fact]
    public void CodesWithNoGraphicMeaningHere_AreConsumedAndIgnored()
    {
        // Blink (5), inverse (7), conceal (8), strike (9), a font (11) and an unknown code (999): the text stays plain.
        Assert.Equal([new OmniAnsiSegment("text")], OmniAnsi.Parse(Esc + "[5;7;8;9;11;999mtext"));
    }

    [Fact]
    public void TheSingleCharacterIntroducer_IsReadLikeEscapeBracket()
    {
        Assert.Equal([new OmniAnsiSegment("red", Foreground: 1)], OmniAnsi.Parse("\u009b31mred"));
    }

    // ---- Extended colours --------------------------------------------------------------------------

    [Theory]
    [InlineData("38;5;2", 2)]
    [InlineData("38;5;9", 9)]
    [InlineData("38;5;196", 9)]
    [InlineData("38;5;46", 10)]
    [InlineData("38;5;21", 4)]
    [InlineData("38;5;16", 0)]
    [InlineData("38;5;231", 15)]
    [InlineData("38;5;244", 8)]
    [InlineData("38;2;255;0;0", 9)]
    [InlineData("38;2;0;205;205", 6)]
    [InlineData("38;2;10;10;10", 0)]
    [InlineData("38:5:196", 9)]
    [InlineData("38:2::255:0:0", 9)]
    [InlineData("38:2:255:0:0", 9)]
    public void AnExtendedForeground_IsReadWholeAndReducedToTheNearestOfTheSixteen(string parameters, int index)
    {
        var segment = Assert.Single(OmniAnsi.Parse(Esc + "[" + parameters + "mtext"));

        Assert.Equal("text", segment.Text);
        Assert.Equal(index, segment.Foreground);
    }

    [Fact]
    public void AnExtendedBackground_IsReducedToo_AndFollowedByMoreCodes()
    {
        var segment = Assert.Single(OmniAnsi.Parse(Esc + "[48;2;0;0;238;1;32mtext"));

        Assert.Equal(new OmniAnsiSegment("text", Bold: true, Foreground: 2, Background: 4), segment);
    }

    [Theory]
    [InlineData("38")]
    [InlineData("38;5")]
    [InlineData("38;2")]
    [InlineData("38;2;1;2")]
    [InlineData("48;5")]
    [InlineData("58;2;1;2;3")]
    public void AnIncompleteExtendedColour_IsConsumed_NoDigitLeaks(string parameters)
    {
        Assert.Equal([new OmniAnsiSegment("text")], OmniAnsi.Parse(Esc + "[" + parameters + "mtext"));
        Assert.Equal("text", OmniAnsi.Strip(Esc + "[" + parameters + "mtext"));
    }

    [Fact]
    public void AnUnderlineColour_AndUnderlineStyles_AreConsumed()
    {
        Assert.Equal([new OmniAnsiSegment("a", Underline: true)], OmniAnsi.Parse(Esc + "[4:3;58:2::255:0:0ma"));
        Assert.Equal([new OmniAnsiSegment("a")], OmniAnsi.Parse(Esc + "[4m" + Esc + "[4:0ma"));
    }

    // ---- Other sequences -----------------------------------------------------------------------------

    [Theory]
    [InlineData("[2K")]
    [InlineData("[1A")]
    [InlineData("[H")]
    [InlineData("[10;20H")]
    [InlineData("[?25l")]
    [InlineData("[?1049h")]
    [InlineData("[0K")]
    [InlineData("[31;1;5 q")]
    [InlineData("c")]
    [InlineData("7")]
    [InlineData("(B")]
    public void AnyOtherControlSequence_IsRemoved(string sequence)
    {
        Assert.Equal("before after", OmniAnsi.Strip("before " + Esc + sequence + "after"));
        Assert.Equal([new OmniAnsiSegment("before after")], OmniAnsi.Parse("before " + Esc + sequence + "after"));
    }

    [Fact]
    public void AMalformedGraphicSequence_IsNotAGraphicRendition()
    {
        // "?" and "<" are private parameter bytes: the sequence is removed whole but changes nothing.
        Assert.Equal([new OmniAnsiSegment("x")], OmniAnsi.Parse(Esc + "[?1mx"));
        Assert.Equal([new OmniAnsiSegment("x")], OmniAnsi.Parse(Esc + "[>4;2mx"));
    }

    [Fact]
    public void StringSequences_AreRemovedUpToTheirTerminator()
    {
        Assert.Equal("title", OmniAnsi.Strip(Esc + "]0;window" + "\a" + "title"));
        Assert.Equal("link", OmniAnsi.Strip(Esc + "]8;;https://example.test" + Esc + "\\link" + Esc + "]8;;" + Esc + "\\"));
        Assert.Equal("x", OmniAnsi.Strip(Esc + "Pq#0;2;0;0;0" + Esc + "\\x"));
        Assert.Equal("x", OmniAnsi.Strip("\u001b]2;t\u009cx"));
    }

    [Fact]
    public void AnUnterminatedStringSequence_LosesItsIntroducerOnly_TheRestIsKept()
    {
        Assert.Equal("0;window title", OmniAnsi.Strip(Esc + "]0;window title"));
    }

    // ---- Malformed and truncated -------------------------------------------------------------------

    [Theory]
    [InlineData("abc" + Esc, "abc")]
    [InlineData("abc" + Esc + "[", "abc")]
    [InlineData("abc" + Esc + "[3", "abc")]
    [InlineData("abc" + Esc + "[38;5;19", "abc")]
    [InlineData("abc" + Esc + Esc + "[31mdef", "abcdef")]
    public void ASequenceCutOffByTheEndOfTheLine_IsRemovedWhole(string text, string expected)
    {
        Assert.Equal(expected, OmniAnsi.Strip(text));
        Assert.Equal(expected, Joined(OmniAnsi.Parse(text)));
    }

    [Fact]
    public void ASequenceBrokenByAControlCharacter_LosesItsIntroducerOnly()
    {
        Assert.Equal("31\u0001x", OmniAnsi.Strip(Esc + "[31\u0001x"));
        Assert.Equal("é", OmniAnsi.Strip(Esc + "[é"));
    }

    [Fact]
    public void ASequenceLongerThanItsBound_IsNotASequence_AndTheScanStaysLinear()
    {
        var digits = new string('1', 400);
        Assert.Equal(digits + "mtext", OmniAnsi.Strip(Esc + "[" + digits + "mtext"));

        // 30 000 introducers, each broken by the next one: one pass, no backtracking. The last one meets "e",
        // a final byte, and is removed with it.
        var pathological = string.Concat(Enumerable.Repeat(Esc + "[1;", 30_000)) + "end";
        Assert.Equal(string.Concat(Enumerable.Repeat("1;", 30_000 - 1)) + "nd", OmniAnsi.Strip(pathological));
    }

    [Fact]
    public void StripAndParse_AgreeOnTheText()
    {
        string[] lines =
        [
            ClientLine,
            Esc + "[2K" + Esc + "[31;1mfail" + Esc + "[0m: " + Esc + "]0;t\a" + "details" + Esc + "[",
            "no sequence",
            Esc + "[38;5;196mred" + Esc + "[48;2;1;2;3mon" + Esc + "[0m",
            "x" + Esc + "[31",
        ];

        Assert.All(lines, line => Assert.Equal(OmniAnsi.Strip(line), Joined(OmniAnsi.Parse(line))));
    }
}
