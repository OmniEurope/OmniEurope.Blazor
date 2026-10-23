// Guard for the timing of the tests: a fixed pause (Task.Delay with a finite duration) races the
// machine. Before a presence assertion, or inside a fake that answers after it, a saturated machine
// can outlast it and fail a sound test (the grid's slow loader, 2026-10-23). The sound forms are:
// - a fake whose answer the test releases (a TaskCompletionSource), never a fake that sleeps;
// - a bounded poll (a for or while loop around the pause) for a condition the renderer meets on its
//   own schedule;
// - a deadline (Task.WhenAny with a delay) that only decides a hang.
// A plain pause is admitted only where it can hide a defect and never invent one: before an absence
// assertion, or to widen the window of a concurrency fake. Each such file is listed below with the
// number of its pauses and why they are sound; a new pause, or one that moved, fails until it is
// rewritten in a sound form or listed with its reason.

using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Tests.Guards;

public sealed class TestTimingGuardTests
{
    private static readonly Dictionary<string, (int Pauses, string Reason)> AdmittedPauses = new(StringComparer.Ordinal)
    {
        ["tests/OmniEurope.Blazor.Tests/AppearanceStateTests.cs"] = (1, "after the awaited third write, before asserting no fourth one"),
        ["tests/OmniEurope.Blazor.Tests/DataScriptLifecycleTests.cs"] = (3, "SettleAsync and two pauses before asserting that a released or failed script is not called"),
        ["tests/OmniEurope.Blazor.Tests/DisposedWhileLoadingTests.cs"] = (1, "SettledAsync, before asserting that a disposed component calls nothing"),
        ["tests/OmniEurope.Blazor.Tests/FormsSelectionFeedbackDefectTests.cs"] = (1, "before asserting that a submit read the model no further"),
        ["tests/OmniEurope.Blazor.Tests/InteractionComponentTests.cs"] = (2, "before asserting that a stale validation added no message"),
        ["tests/OmniEurope.Blazor.Tests/PreferenceFakes.cs"] = (1, "widens the window in which the concurrency fake detects two overlapping operations"),
        ["tests/OmniEurope.Blazor.Tests/ThemeScopeFollowStateTests.cs"] = (1, "before asserting that the prerender is still held"),
    };

    [Fact]
    public void FixedPauses_AreOnlyBoundedPollsDeadlinesOrAdmittedAbsenceChecks()
    {
        var root = RepositoryScan.Root;
        var guardFile = Path.Combine(root, "tests", "OmniEurope.Blazor.Tests", "Guards", "TestTimingGuardTests.cs");
        var measured = RepositoryScan.Enumerate(Path.Combine(root, "tests"), "*.cs")
            .Where(path => !string.Equals(Path.GetFullPath(path), Path.GetFullPath(guardFile), StringComparison.OrdinalIgnoreCase))
            .Select(path => (File: Path.GetRelativePath(root, path).Replace('\\', '/'), Pauses: PlainPauses(File.ReadAllLines(path))))
            .Where(entry => entry.Pauses > 0)
            .ToDictionary(entry => entry.File, entry => entry.Pauses, StringComparer.Ordinal);

        var unexplained = measured
            .Where(entry => !AdmittedPauses.TryGetValue(entry.Key, out var admitted) || admitted.Pauses != entry.Value)
            .Select(entry => $"{entry.Key}: {entry.Value} plain pause(s), {(AdmittedPauses.TryGetValue(entry.Key, out var admitted) ? admitted.Pauses : 0)} admitted")
            .Concat(AdmittedPauses.Keys.Where(file => !measured.ContainsKey(file)).Select(file => $"{file}: admitted but has no plain pause any more"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(unexplained.Length == 0,
            "A fixed pause races the machine. Release the fake's answer from the test, poll in a bounded loop, "
            + "or, before an absence assertion only, list the file with its reason:\n" + string.Join('\n', unexplained));
    }

    [Fact]
    public void PlainPauses_CountsOnlyPausesOutsideLoopsAndDeadlines()
    {
        string[] lines =
        [
            "await Task.Delay(100, token);",
            "for (var wait = 0; wait < 500 && !done; wait++)",
            "{",
            "    await Task.Delay(10, token);",
            "}",
            "while (pending)",
            "{",
            "    await other.InvokeAsync(() => { });",
            "    await Task.Delay(5, token);",
            "}",
            "await Task.WhenAny(work, Task.Delay(Patience, token));",
            "await Task.Delay(Timeout.Infinite, token);",
            "return Task.Delay(TimeSpan.FromMilliseconds(20));",
        ];

        Assert.Equal(2, PlainPauses(lines));
    }

    private static int PlainPauses(IReadOnlyList<string> lines)
    {
        var count = 0;
        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var pauses = DelayCall.Matches(line).Count(match => !match.Groups["argument"].Value.StartsWith("Timeout.Infinite", StringComparison.Ordinal));
            if (pauses == 0 || line.Contains("WhenAny(", StringComparison.Ordinal) || InsideLoop(lines, index))
            {
                continue;
            }

            count += pauses;
        }

        return count;
    }

    // A pause in the body of a for or while loop a few lines up, the body not closed in between.
    private static bool InsideLoop(IReadOnlyList<string> lines, int index)
    {
        for (var above = index - 1; above >= Math.Max(0, index - 5); above--)
        {
            var text = lines[above].Trim();
            if (text.StartsWith('}'))
            {
                return false;
            }

            if (text.StartsWith("for (", StringComparison.Ordinal) || text.StartsWith("while (", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static readonly Regex DelayCall = new(@"Task\.Delay\((?<argument>[^,)]*)", RegexOptions.Compiled);
}
