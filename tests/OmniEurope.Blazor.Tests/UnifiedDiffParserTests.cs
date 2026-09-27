using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// <see cref="OmniUnifiedDiffParser"/> reads external text: a hunk header whose numbers do not fit is
/// malformed input to skip, never an exception thrown out of the component that renders the diff.
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
}
