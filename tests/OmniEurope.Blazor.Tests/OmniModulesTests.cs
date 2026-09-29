using System.Reflection;
using System.Text.RegularExpressions;
using OmniEurope.Blazor.Internal;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// Holds <see cref="OmniModules"/> to the files it names: every constant is a module of the library's
/// <c>wwwroot</c>, and no module address is written out anywhere else in the library.
/// </summary>
public sealed partial class OmniModulesTests
{
    private const string Prefix = "./_content/OmniEurope.Blazor/";

    private static IReadOnlyList<string> Constants =>
    [
        .. typeof(OmniModules).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(constant => constant is { IsLiteral: true } && constant.Name != "Root")
            .Select(constant => (string)constant.GetRawConstantValue()!)
    ];

    [Fact]
    public void EveryConstant_NamesAFileOfTheLibraryWebRoot()
    {
        var webRoot = Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor", "wwwroot");

        Assert.NotEmpty(Constants);
        Assert.Equal(Constants.Count, Constants.Distinct(StringComparer.Ordinal).Count());
        foreach (var module in Constants)
        {
            Assert.StartsWith(Prefix, module, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(webRoot, module[Prefix.Length..])), $"{module} names no file of wwwroot.");
        }
    }

    [Fact]
    public void NoModuleAddressIsWrittenOutsideOmniModules()
    {
        var source = Path.Combine(ShippedLookTests.RepositoryRoot(), "src", "OmniEurope.Blazor");
        var home = Path.Combine(source, "Internal", "OmniModules.cs");
        var written = Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Where(path => Path.GetExtension(path) is ".cs" or ".razor")
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !string.Equals(Path.GetFullPath(path), Path.GetFullPath(home), StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => ModuleAddress().Matches(File.ReadAllText(path))
                .Select(match => $"{Path.GetRelativePath(source, path)}: {match.Value}"))
            .ToArray();

        Assert.True(written.Length == 0, $"Module addresses written outside OmniModules (use its constant): {string.Join(", ", written)}");
    }

    [GeneratedRegex(@"_content/OmniEurope\.Blazor/[A-Za-z0-9._-]+\.js")]
    private static partial Regex ModuleAddress();
}
