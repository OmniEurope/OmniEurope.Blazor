using System.Diagnostics;
using OmniEurope.Blazor.Tests.Guards;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Runs the node unit tests of the package scripts (<c>tests/js/*.test.mjs</c>, the built-in
/// <c>node --test</c> runner against a hand-written DOM, no dependency) as part of the suite, so a
/// script's own logic (a menu that gives up, a tooltip that moves a title aside) is locked like the
/// rest. They prove the logic of a script, not its layout or its real events, which stay with the
/// browser probes. Node is required, as it already is by the probes; its absence fails the test.
/// </summary>
public sealed class JavaScriptUnitTests
{
    private static readonly TimeSpan Patience = TimeSpan.FromMinutes(2);

    [Fact]
    public async Task ScriptUnitTests_AllPass()
    {
        var root = RepositoryScan.Root;
        // The floor: a folder with no test file would make node report a green run over nothing.
        var files = RepositoryScan.Enumerate(Path.Combine(root, "tests", "js"), "*.test.mjs");
        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("--test");
        foreach (var file in files)
        {
            start.ArgumentList.Add(Path.GetRelativePath(root, file));
        }

        using var node = Process.Start(start) ?? throw new InvalidOperationException("node does not start.");
        var output = node.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var errors = node.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(Patience);
        await node.WaitForExitAsync(deadline.Token);

        Assert.True(node.ExitCode == 0, $"node --test failed:\n{await output}\n{await errors}");
    }
}
