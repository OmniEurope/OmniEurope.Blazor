// Guard for the import of JavaScript modules: every component loads its module through
// OmniScriptModule, which retries a failed import once and leaves the lost circuit and the
// cancellation to the caller. A direct InvokeAsync<IJSObjectReference>("import", ...) anywhere else
// in the package skips that retry and drifts from the others (the 2026-10-08 audit found the same
// import written forty-odd times, each with its own handling). The "import" literal is therefore
// admitted in the loader file only.

using System.Text.RegularExpressions;

namespace OmniEurope.Blazor.Tests.Guards;

public sealed class ScriptModuleImportGuardTests
{
    private const string LoaderFile = "src/OmniEurope.Blazor/Internal/OmniScriptModule.cs";

    private static readonly Regex ImportLiteral = new("\"import\"", RegexOptions.CultureInvariant);

    [Fact]
    public void ModuleImports_OnlyHappenInTheLoader()
    {
        var root = RepositoryScan.Root;
        var package = Path.Combine(root, "src", "OmniEurope.Blazor");
        var files = RepositoryScan.Enumerate(package, "*.cs").Concat(RepositoryScan.Enumerate(package, "*.razor")).ToArray();
        Assert.Contains(files, path => Relative(root, path) == LoaderFile);

        var offenders = files
            .Where(path => Relative(root, path) != LoaderFile)
            .SelectMany(path => ImportLines(File.ReadAllLines(path)).Select(line => $"{Relative(root, path)}:{line}"))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "A JavaScript module is imported through OmniScriptModule.ImportAsync, never by a direct \"import\" call:\n"
            + string.Join('\n', offenders));
    }

    [Fact]
    public void ImportLines_FindsEveryFormOfTheDirectCall_AndNothingElse()
    {
        string[] lines =
        [
            "var a = await js.InvokeAsync<IJSObjectReference>(\"import\", Path);",
            "var b = await js.InvokeAsync<IJSObjectReference>(\"import\", token, Path);",
            "_importing ??= JavaScript.InvokeAsync<IJSObjectReference>(\"import\", GridModulePath).AsTask();",
            "await js.InvokeVoidAsync(\"import\", Path);",
            "var c = await OmniScriptModule.ImportAsync(js, Path);",
            "// the import was pending, the module is released on arrival",
            "await module.InvokeVoidAsync(\"importTable\", file);",
        ];

        Assert.Equal([1, 2, 3, 4], ImportLines(lines));
    }

    private static IReadOnlyList<int> ImportLines(IReadOnlyList<string> lines)
        => Enumerable.Range(0, lines.Count).Where(index => ImportLiteral.IsMatch(lines[index])).Select(index => index + 1).ToArray();

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
}
