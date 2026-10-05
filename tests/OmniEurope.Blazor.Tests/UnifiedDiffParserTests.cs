using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniUnifiedDiffParser"/> reads external text: git and plain unified diffs, every kind of
/// line and file header, and malformed input skipped, never an exception thrown out of the component
/// that renders the diff.
/// </summary>
public sealed class UnifiedDiffParserTests
{
    [Theory]
    [InlineData("@@ -2147483648 +1 @@")]
    [InlineData("@@ -1,99999999999 +1 @@")]
    [InlineData("@@ -1 +2147483647,2 @@")]
    public void HunkHeader_WithNumbersOutOfRange_IsSkipped_WithoutThrowing(string header)
    {
        var diff = $"--- a/f.txt\n+++ b/f.txt\n{header}\n-old\n+new\n@@ -5 +5 @@\n-five\n+FIVE\n";

        var files = OmniUnifiedDiffParser.Parse(diff);

        var file = Assert.Single(files);
        var hunk = Assert.Single(file.Hunks);
        Assert.Equal(5, hunk.OldStart);
        Assert.Equal(["five", "FIVE"], hunk.Lines.Select(line => line.Text));
    }

    private static IReadOnlyList<(OmniDiffLineKind Kind, string Text, int? Old, int? New)> LinesOf(OmniDiffHunk hunk) =>
        [.. hunk.Lines.Select(line => (line.Kind, line.Text, line.OldNumber, line.NewNumber))];

    private static string Lines(params string[] lines) => string.Join("\n", lines);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyText_HasNoFile(string? diff) => Assert.Empty(OmniUnifiedDiffParser.Parse(diff));

    [Fact]
    public void Hunk_NumbersContextAddedAndRemovedLines_OnTheirSides()
    {
        // Windows line ends, and a last context line whose leading space an editor trimmed.
        var diff = "--- a/f.txt\r\n+++ b/f.txt\r\n@@ -10,3 +10,3 @@ section\r\n a\r\n-b\r\n+B\r\n\r\n";

        var hunk = Assert.Single(Assert.Single(OmniUnifiedDiffParser.Parse(diff)).Hunks);

        Assert.Equal("@@ -10,3 +10,3 @@ section", hunk.Header);
        Assert.Equal(
            [(OmniDiffLineKind.Context, "a", 10, 10), (OmniDiffLineKind.Removed, "b", 11, null), (OmniDiffLineKind.Added, "B", null, 11),
             (OmniDiffLineKind.Context, "", 12, 12)],
            LinesOf(hunk));
    }

    [Fact]
    public void NoNewlineNote_InsideOrRightAfterAHunk_IsANote()
    {
        var diff = Lines("--- a/f", "+++ b/f", "@@ -1,2 +1,2 @@", "-a", @"\ No newline at end of file", "+b", " x", @"\ No newline at end of file");

        var hunk = Assert.Single(Assert.Single(OmniUnifiedDiffParser.Parse(diff)).Hunks);

        Assert.Equal(
            [OmniDiffLineKind.Removed, OmniDiffLineKind.Note, OmniDiffLineKind.Added, OmniDiffLineKind.Context, OmniDiffLineKind.Note],
            hunk.Lines.Select(line => line.Kind));
        Assert.Equal("No newline at end of file", hunk.Lines[1].Text);
        Assert.Null(hunk.Lines[1].OldNumber);
    }

    [Fact]
    public void HunkShorterThanItsHeader_EndsAtTheNextHeader()
    {
        // The header declares five lines; the third line is already the next file. (A "--- " line would
        // still be a removed line "-- ...", the hunk awaiting old lines.)
        var diff = Lines("--- a/one", "+++ b/one", "@@ -1,5 +1,5 @@", "-a", "+A", "diff --git a/two b/two", "--- a/two", "+++ b/two", "@@ -1 +1 @@", "-x", "+y");

        var files = OmniUnifiedDiffParser.Parse(diff);

        Assert.Equal(["one", "two"], files.Select(file => file.Path));
        Assert.Equal(2, files[0].Hunks[0].Lines.Count);
        Assert.Equal(["x", "y"], files[1].Hunks[0].Lines.Select(line => line.Text));
    }

    [Fact]
    public void GitDiff_ReadsItsFilesFromTheirHeaders_PathsWithSpacesIncluded()
    {
        var diff = Lines(
            "diff --git a/docs/read me.md b/docs/read me.md",
            "index 1..2 100644",
            "--- a/docs/read me.md",
            "+++ b/docs/read me.md",
            "@@ -1 +1 @@",
            "-old",
            "+new",
            "diff --git a/kept.txt b/kept.txt",
            "index 3..4 100644");

        var files = OmniUnifiedDiffParser.Parse(diff);

        Assert.Equal(2, files.Count);
        Assert.Equal(("docs/read me.md", "docs/read me.md", OmniDiffFileStatus.Modified), (files[0].OldPath, files[0].NewPath, files[0].Status));
        Assert.Equal(1, files[0].Additions);
        Assert.Equal(1, files[0].Deletions);
        // A file with no --- line keeps the paths of its git header.
        Assert.Equal(("kept.txt", "kept.txt"), (files[1].OldPath, files[1].NewPath));
        Assert.Empty(files[1].Hunks);
    }

    [Fact]
    public void GitHeader_WithoutASecondSide_LeavesThePathsToTheLinesThatFollow()
    {
        var files = OmniUnifiedDiffParser.Parse(Lines("diff --git onlyone", "--- a/x", "+++ b/x", "@@ -1 +1 @@", "-1", "+2"));

        Assert.Equal(("x", "x"), (files[0].OldPath, files[0].NewPath));
    }

    [Fact]
    public void ExtendedHeaders_TellAddedDeletedRenamedAndBinaryFiles()
    {
        var diff = Lines(
            "diff --git a/new.txt b/new.txt",
            "new file mode 100644",
            "diff --git a/gone.txt b/gone.txt",
            "deleted file mode 100644",
            "diff --git a/old name.txt b/new name.txt",
            "similarity index 90%",
            "rename from \"old name.txt\"",
            "rename to \"new name.txt\"",
            "diff --git a/logo.png b/logo.png",
            "Binary files a/logo.png and b/logo.png differ",
            "diff --git a/icon.png b/icon.png",
            "GIT binary patch");

        var files = OmniUnifiedDiffParser.Parse(diff);

        Assert.Equal((null, "new.txt", OmniDiffFileStatus.Added), (files[0].OldPath, files[0].NewPath, files[0].Status));
        Assert.Equal(("gone.txt", null, OmniDiffFileStatus.Deleted), (files[1].OldPath, files[1].NewPath, files[1].Status));
        Assert.Equal(("old name.txt", "new name.txt", OmniDiffFileStatus.Renamed), (files[2].OldPath, files[2].NewPath, files[2].Status));
        Assert.True(files[3].IsBinary);
        Assert.True(files[4].IsBinary);
        Assert.False(files[0].IsBinary);
    }

    [Fact]
    public void PlainDiff_ReadsDevNullAsAddedOrDeleted_AndDropsTimestampsAndQuotes()
    {
        var diff = Lines(
            "--- /dev/null\t2026-10-04 12:00:00",
            "+++ \"b/a.txt\"\t2026-10-04 12:00:00",
            "@@ -0,0 +1 @@",
            "+a",
            "--- a/b.txt",
            "+++ /dev/null",
            "@@ -1 +0,0 @@",
            "-b",
            "--- c",
            "+++ plain.txt",
            "@@ -1 +1 @@",
            "-c",
            "+d");

        var files = OmniUnifiedDiffParser.Parse(diff);

        Assert.Equal((null, "a.txt", OmniDiffFileStatus.Added), (files[0].OldPath, files[0].NewPath, files[0].Status));
        Assert.Equal(("b.txt", null, OmniDiffFileStatus.Deleted), (files[1].OldPath, files[1].NewPath, files[1].Status));
        // Paths without a side prefix, short or long, are kept as written.
        Assert.Equal(("c", "plain.txt"), (files[2].OldPath, files[2].NewPath));
    }

    [Fact]
    public void HunkWithoutFileHeaders_StillMakesAFile_AndALoneNewSideHeaderIsIgnored()
    {
        var files = OmniUnifiedDiffParser.Parse(Lines("+++ b/orphan", "@@ -1 +1 @@", "-a", "+b", "+++ b/late"));

        var file = Assert.Single(files);
        Assert.Null(file.Path);
        Assert.Single(file.Hunks);
    }

    [Fact]
    public void NoteOutsideAHunk_AndLinesBeforeAnyHeader_AreIgnored()
    {
        var files = OmniUnifiedDiffParser.Parse(Lines("Commit message", @"\ stray", "index 1..2"));

        Assert.Empty(files);
    }
}
